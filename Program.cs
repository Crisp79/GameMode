using System.Text.Json;
using System.Windows.Forms;
using GameMode.Models;
using GameMode.Native;
using GameMode.Services;

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

#if !DEBUG
var debugMode = args.Contains("--debug");
if (debugMode)
    Kernel32.AllocConsole();
#else
const bool debugMode = true;
#endif

Config config;
try
{
    var json = File.ReadAllText(configPath);
    config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load config: {ex.Message}");
    config = new Config();
}

var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
using var logger = new Logger(logDir, retentionDays: config.LogRetentionDays, consoleEnabled: debugMode);

logger.Info("GameMode initializing");

if (!File.Exists(config.PlaynitePath))
    logger.Warn($"PlaynitePath not found: {config.PlaynitePath}");

if (config.CheckIntervalMs is < 100 or > 60000)
{
    logger.Warn($"CheckIntervalMs {config.CheckIntervalMs} out of range (100-60000), using default 500");
    config.CheckIntervalMs = 500;
}

if (config.GracePeriodSeconds < 1)
{
    logger.Warn($"GracePeriodSeconds {config.GracePeriodSeconds} too low, using default 2");
    config.GracePeriodSeconds = 2;
}

if (config.DisconnectTimeoutSeconds < config.GracePeriodSeconds)
{
    logger.Warn($"DisconnectTimeoutSeconds ({config.DisconnectTimeoutSeconds}) is less than GracePeriodSeconds ({config.GracePeriodSeconds}), adjusting to {config.GracePeriodSeconds + 10}");
    config.DisconnectTimeoutSeconds = config.GracePeriodSeconds + 10;
}

if (config.DisconnectTimeoutSeconds > 3600)
{
    logger.Warn($"DisconnectTimeoutSeconds ({config.DisconnectTimeoutSeconds}) exceeds maximum (3600), capping at 3600");
    config.DisconnectTimeoutSeconds = 3600;
}

var controllerService = new ControllerService();
var playniteService = new PlayniteService(config.PlaynitePath, logger);
var gameDetectionService = new GameDetectionService();
var gameModeService = new GameModeService(controllerService, playniteService, gameDetectionService, logger, config);

ApplicationConfiguration.Initialize();

using var tray = new TrayController(logDir);
var cts = new CancellationTokenSource();

tray.QuitRequested += () =>
{
    cts.Cancel();
    Application.Exit();
};

tray.EnableToggled += () =>
{
    gameModeService.SetEnabled(tray.Enabled);
    tray.ShowNotification("GameMode", tray.Enabled ? "Game Mode enabled" : "Game Mode disabled");
};

gameModeService.StateChanged += state => tray.UpdateState(state);
controllerService.Connected += () => tray.UpdateController("Connected");
controllerService.Disconnected += () => tray.UpdateController("Disconnected");

tray.UpdateController(controllerService.IsConnected() ? "Connected" : "Disconnected");
tray.UpdateState("Idle");

var gameTask = Task.Run(() => gameModeService.RunAsync(cts.Token));

Application.Run();

try
{
    await gameTask;
}
catch (OperationCanceledException)
{
}

logger.Info("GameMode exited");
