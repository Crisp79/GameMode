using System.Runtime.InteropServices;

namespace GameMode.Native;

public static class XInput
{
    private const string DllName = "xinput1_4.dll";

    public const int MaxControllers = 4;

    [DllImport(DllName)]
    private static extern int XInputGetState(int dwUserIndex, out XInputState pState);

    [DllImport(DllName)]
    private static extern int XInputGetCapabilities(int dwUserIndex, int dwFlags, out XInputCapabilities pCapabilities);

    public static bool IsConnected(int userIndex)
    {
        return XInputGetState(userIndex, out _) == 0;
    }

    public static int GetConnectedCount()
    {
        var count = 0;
        for (var i = 0; i < MaxControllers; i++)
        {
            if (IsConnected(i))
                count++;
        }
        return count;
    }

    public static List<int> GetConnectedControllers()
    {
        var connected = new List<int>();
        for (var i = 0; i < MaxControllers; i++)
        {
            if (IsConnected(i))
                connected.Add(i);
        }
        return connected;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputCapabilities
    {
        public byte Type;
        public byte SubType;
        public ushort Flags;
        public XInputGamepad Gamepad;
        public XInputVibration Vibration;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputVibration
    {
        public ushort LeftMotorSpeed;
        public ushort RightMotorSpeed;
    }
}
