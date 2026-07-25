using System.Text.Json;
using GameMode.Models;
using GameMode.Services;

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

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
using var logger = new Logger(logDir, retentionDays: config.LogRetentionDays);

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

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await gameModeService.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
}

logger.Info("GameMode exited");
