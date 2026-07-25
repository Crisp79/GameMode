using GameMode.Native;

namespace GameMode.Services;

public class ControllerService
{
    private bool _wasConnected;

    public event Action? Connected;
    public event Action? Disconnected;

    public bool IsConnected()
    {
        return XInput.IsConnected(0);
    }

    public int GetConnectedCount()
    {
        return XInput.GetConnectedCount();
    }

    public List<int> GetConnectedControllers()
    {
        return XInput.GetConnectedControllers();
    }

    public bool Poll()
    {
        var connected = IsConnected();

        if (connected && !_wasConnected)
            Connected?.Invoke();
        else if (!connected && _wasConnected)
            Disconnected?.Invoke();

        _wasConnected = connected;
        return connected;
    }
}
