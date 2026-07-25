using System.Diagnostics;
using GameMode.Native;

namespace GameMode.Services;

public class GameDetectionService
{
    public bool IsGameRunning()
    {
        var foregroundPtr = User32.GetForegroundWindow();
        if (foregroundPtr == IntPtr.Zero)
            return false;

        User32.GetWindowThreadProcessId(foregroundPtr, out var pid);

        try
        {
            var proc = Process.GetProcessById((int)pid);
            var name = proc.ProcessName;

            return name != "Playnite.FullscreenApp"
                && name != "explorer"
                && name != "GameMode"
                && name != "ApplicationFrameHost";
        }
        catch
        {
            return false;
        }
    }
}
