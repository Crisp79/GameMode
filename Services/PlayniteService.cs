using System.Diagnostics;
using System.Runtime.InteropServices;
using GameMode.Native;

namespace GameMode.Services;

public class PlayniteService
{
    private readonly string _playnitePath;
    private readonly Logger _logger;

    private const string ProcessName = "Playnite.FullscreenApp";

    public PlayniteService(string playnitePath, Logger logger)
    {
        _playnitePath = playnitePath;
        _logger = logger;
    }

    public void Launch()
    {
        if (!File.Exists(_playnitePath))
        {
            _logger.Error($"Playnite not found at: {_playnitePath}");
            return;
        }

        try
        {
            var process = new ProcessStartInfo
            {
                FileName = _playnitePath,
                UseShellExecute = true
            };
            Process.Start(process);
            _logger.Info("Launching Playnite");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to launch Playnite: {ex.Message}");
        }
    }

    public bool IsRunning()
    {
        return Process.GetProcessesByName(ProcessName).Length > 0;
    }

    public void Close()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            try
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                {
                    process.Kill();
                }
                _logger.Info("Playnite closed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to close Playnite: {ex.Message}");
            }
        }
    }

    public void BringToFront()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(process.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(process.MainWindowHandle);
            }
        }
    }

    public void Restore()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(process.MainWindowHandle, SW_RESTORE);
            }
        }
    }

    public bool IsMinimized()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return IsIconic(process.MainWindowHandle);
            }
        }
        return false;
    }

    public bool IsConsoleModeActive()
    {
        var foregroundPtr = User32.GetForegroundWindow();
        if (foregroundPtr == IntPtr.Zero)
            return false;

        User32.GetWindowThreadProcessId(foregroundPtr, out var pid);

        try
        {
            var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName == ProcessName;
        }
        catch
        {
            return false;
        }
    }

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
}
