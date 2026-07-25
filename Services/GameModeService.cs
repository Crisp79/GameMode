using GameMode.Models;

namespace GameMode.Services;

public class GameModeService
{
    private readonly ControllerService _controller;
    private readonly PlayniteService _playnite;
    private readonly Logger _logger;
    private readonly Config _config;

    private bool _playniteLaunched;
    private DateTime? _disconnectTime;
    private CancellationTokenSource? _cts;

    public GameModeService(ControllerService controller, PlayniteService playnite, Logger logger, Config config)
    {
        _controller = controller;
        _playnite = playnite;
        _logger = logger;
        _config = config;
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
        _logger.Info("Controller Connected");

        _disconnectTime = null;

        if (_playniteLaunched)
        {
            if (_playnite.IsMinimized())
                _playnite.Restore();

            if (_config.BringToFront)
                _playnite.BringToFront();

            return;
        }

        if (!_playnite.IsRunning())
        {
            _playnite.Launch();
            _playniteLaunched = true;
        }
        else
        {
            if (_playnite.IsMinimized())
                _playnite.Restore();

            if (_config.BringToFront)
                _playnite.BringToFront();

            _playniteLaunched = true;
        }
    }

    private void OnControllerDisconnected()
    {
        _logger.Info("Controller Disconnected");
        _disconnectTime = DateTime.UtcNow;
        _ = StartDisconnectTimerAsync();
    }

    private async Task StartDisconnectTimerAsync()
    {
        var timeout = TimeSpan.FromMinutes(_config.DisconnectTimeoutMinutes);

        while (_disconnectTime.HasValue && !_cts!.Token.IsCancellationRequested)
        {
            if (DateTime.UtcNow - _disconnectTime.Value >= timeout)
            {
                if (_config.ClosePlaynite && _playnite.IsRunning())
                {
                    _playnite.Close();
                    _playniteLaunched = false;
                }
                _disconnectTime = null;
                break;
            }

            try
            {
                await Task.Delay(1000, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
