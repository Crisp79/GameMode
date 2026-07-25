namespace GameMode.Services;

public class Logger : IDisposable
{
    private readonly string _logPath;
    private readonly bool _debugEnabled;
    private bool _consoleEnabled;
    private readonly StreamWriter? _writer;
    private readonly object _lock = new();

    public Logger(string logDir, bool debugEnabled = false, int retentionDays = 7, bool consoleEnabled = true)
    {
        _debugEnabled = debugEnabled;
        _consoleEnabled = consoleEnabled;
        Directory.CreateDirectory(logDir);

        CleanupOldLogs(logDir, retentionDays);

        var date = DateTime.Now.ToString("yyyy-MM-dd");
        _logPath = Path.Combine(logDir, $"GameMode-{date}.log");

        try
        {
            _writer = new StreamWriter(_logPath, append: true)
            {
                AutoFlush = true
            };
        }
        catch
        {
            _writer = null;
        }
    }

    private static void CleanupOldLogs(string logDir, int retentionDays)
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            foreach (var file in Directory.GetFiles(logDir, "GameMode*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch
        {
        }
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warn(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    public void Debug(string message)
    {
        if (_debugEnabled)
            Write("DEBUG", message);
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} {level} {message}";
        lock (_lock)
        {
            if (_consoleEnabled)
                Console.WriteLine(line);
            _writer?.WriteLine(line);
        }
    }

    public void SetConsoleEnabled(bool enabled)
    {
        _consoleEnabled = enabled;
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}
