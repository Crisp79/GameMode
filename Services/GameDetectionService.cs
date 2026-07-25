using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameMode.Services;

public class GameDetectionService
{
    public bool IsGameRunning()
    {
        var foregroundPtr = GetForegroundWindow();
        if (foregroundPtr == IntPtr.Zero)
            return false;

        GetWindowThreadProcessId(foregroundPtr, out var pid);

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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
