namespace GameMode.Services;

public class Logger : IDisposable
{
    private readonly string _logPath;
    private readonly bool _debugEnabled;
    private readonly StreamWriter? _writer;
    private readonly object _lock = new();

    public Logger(string logDir, bool debugEnabled = false)
    {
        _debugEnabled = debugEnabled;
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "GameMode.log");

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
            Console.WriteLine(line);
            _writer?.WriteLine(line);
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}
