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
using var logger = new Logger(logDir);

logger.Info("GameMode initializing");

var controllerService = new ControllerService();
var playniteService = new PlayniteService(config.PlaynitePath, logger);
var gameModeService = new GameModeService(controllerService, playniteService, logger, config);

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
