using System.Runtime.InteropServices;

namespace GameMode.Native;

public static class Kernel32
{
    [DllImport("kernel32.dll")]
    public static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    public static extern bool FreeConsole();
}
