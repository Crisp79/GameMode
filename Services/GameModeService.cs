using GameMode.Models;

namespace GameMode.Services;

public class GameModeService
{
    private enum State { Idle, Connected, ConsoleActive, Gaming, GracePeriod, Countdown }

    private readonly ControllerService _controller;
    private readonly PlayniteService _playnite;
    private readonly GameDetectionService _gameDetection;
    private readonly Logger _logger;
    private readonly Config _config;

    private State _state = State.Idle;
    private State _previousState;
    private DateTime? _graceStart;
    private DateTime? _countdownStart;
    private CancellationTokenSource? _cts;
    private bool _enabled = true;

    public event Action<string>? StateChanged;

    public GameModeService(ControllerService controller, PlayniteService playnite, GameDetectionService gameDetection, Logger logger, Config config)
    {
        _controller = controller;
        _playnite = playnite;
        _gameDetection = gameDetection;
        _logger = logger;
        _config = config;
    }

    public void SetEnabled(bool enabled)
    {
        if (_enabled == enabled) return;
        _enabled = enabled;
        if (enabled)
        {
            _logger.Info("Game Mode resumed");
            _state = State.Idle;
            TransitionTo(_controller.IsConnected() ? State.Connected : State.Idle);
        }
        else
        {
            _logger.Info("Game Mode paused");
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _controller.Connected += OnControllerConnected;
        _controller.Disconnected += OnControllerDisconnected;

        _logger.Info("GameMode started");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                _controller.Poll();
                UpdateContext();
                await Task.Delay(_config.CheckIntervalMs, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _controller.Connected -= OnControllerConnected;
            _controller.Disconnected -= OnControllerDisconnected;
        }

        _logger.Info("GameMode stopped");
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private void OnControllerConnected()
    {
        if (!_enabled) return;

        if (_state is State.GracePeriod or State.Countdown)
        {
            _logger.Info("Controller reconnected");
            _logger.Info("Disconnect timer cancelled");
            _graceStart = null;
            _countdownStart = null;
            TransitionTo(_previousState);
            return;
        }

        if (_state == State.Idle)
            TransitionTo(State.Connected);
    }

    private void OnControllerDisconnected()
    {
        if (!_enabled) return;

        if (_state == State.Idle)
            return;

        _logger.Info("Controller disconnected");
        _previousState = _state;
        _graceStart = DateTime.UtcNow;
        TransitionTo(State.GracePeriod);
    }

    private void UpdateContext()
    {
        if (!_enabled) return;

        var consoleActive = _playnite.IsConsoleModeActive();
        var gameRunning = _gameDetection.IsGameRunning();

        switch (_state)
        {
            case State.Connected:
                if (gameRunning)
                    TransitionTo(State.Gaming);
                else if (consoleActive)
                    TransitionTo(State.ConsoleActive);
                break;

            case State.ConsoleActive:
                if (gameRunning)
                    TransitionTo(State.Gaming);
                else if (!consoleActive)
                    TransitionTo(State.Connected);
                break;

            case State.Gaming:
                if (!gameRunning)
                    TransitionTo(consoleActive ? State.ConsoleActive : State.Connected);
                break;

            case State.GracePeriod:
                if (_graceStart.HasValue && DateTime.UtcNow - _graceStart.Value >= TimeSpan.FromSeconds(_config.GracePeriodSeconds))
                    HandleGracePeriodExpired();
                break;

            case State.Countdown:
                if (_countdownStart.HasValue && DateTime.UtcNow - _countdownStart.Value >= TimeSpan.FromSeconds(_config.DisconnectTimeoutSeconds))
                    HandleCountdownExpired();
                break;
        }
    }

    private void HandleGracePeriodExpired()
    {
        if (_playnite.IsConsoleModeActive() && !_gameDetection.IsGameRunning())
        {
            _logger.Info("Disconnect timer started");
            _countdownStart = DateTime.UtcNow;
            TransitionTo(State.Countdown);
        }
        else
        {
            _graceStart = null;
            TransitionTo(State.Idle);
        }
    }

    private void HandleCountdownExpired()
    {
        if (!_controller.IsConnected() && !_gameDetection.IsGameRunning() && _playnite.IsConsoleModeActive())
        {
            if (_config.ClosePlaynite && _playnite.IsRunning())
            {
                _logger.Info("Closing Playnite");
                _playnite.Close();
            }
        }
        else
        {
            _logger.Info("Disconnect timer cancelled — console mode lost or game running");
        }

        _countdownStart = null;
        TransitionTo(State.Idle);
    }

    private void TransitionTo(State newState)
    {
        if (_state == newState)
            return;

        var oldState = _state;
        _state = newState;

        LogTransition(oldState, newState);

        if (newState == State.Connected && oldState == State.Idle && !_playnite.IsRunning())
        {
            try
            {
                _playnite.Launch();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to launch Playnite: {ex.Message}");
            }
        }

        if (newState == State.ConsoleActive)
        {
            _logger.Info("Console Mode entered");
            if (_config.BringToFront)
            {
                try
                {
                    if (_playnite.IsMinimized())
                        _playnite.Restore();
                    _playnite.BringToFront();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to bring Playnite to front: {ex.Message}");
                }
            }
        }

        if (oldState == State.ConsoleActive && newState != State.ConsoleActive)
            _logger.Info("Console Mode exited");

        if (newState == State.Gaming)
            _logger.Info("Game detected");

        if (oldState == State.Gaming && newState != State.Gaming)
            _logger.Info("Game exited");

        if (newState == State.GracePeriod)
            _logger.Info("Grace period started");
    }

    private void LogTransition(State from, State to)
    {
        var fromName = FormatStateName(from);
        var toName = FormatStateName(to);
        _logger.Info($"{fromName} -> {toName}");
        StateChanged?.Invoke(toName);
    }

    private static string FormatStateName(State s) => s switch
    {
        State.Idle => "Idle",
        State.Connected => "ControllerConnected",
        State.ConsoleActive => "BrowsingLibrary",
        State.Gaming => "Gaming",
        State.GracePeriod => "DisconnectGracePeriod",
        State.Countdown => "DisconnectCountdown",
        _ => s.ToString()
    };
}
