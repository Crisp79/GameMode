using System.Diagnostics;
using System.Windows.Forms;
using GameMode.Native;

namespace GameMode.Services;

public class TrayController : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _controllerItem;
    private readonly ToolStripMenuItem _stateItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _debugItem;
    private readonly string _logDir;
    private readonly Logger _logger;
    private bool _enabled = true;
    private bool _consoleShown;

    public bool Enabled => _enabled;

    public event Action? EnableToggled;
    public event Action? QuitRequested;

    public TrayController(string logDir, Logger logger, bool consoleShown)
    {
        _logDir = logDir;
        _logger = logger;
        _consoleShown = consoleShown;

        _controllerItem = new ToolStripMenuItem("Controller: Unknown") { Enabled = false };
        _stateItem = new ToolStripMenuItem("State: Unknown") { Enabled = false };
        _toggleItem = new ToolStripMenuItem("Disable Game Mode");
        _debugItem = new ToolStripMenuItem(_consoleShown ? "Hide Console" : "Show Console");

        var logsItem = new ToolStripMenuItem("Open Logs");
        var settingsItem = new ToolStripMenuItem("Open Settings");
        var quitItem = new ToolStripMenuItem("Quit");

        _toggleItem.Click += (_, _) => Toggle();
        _debugItem.Click += (_, _) => ToggleConsole();
        logsItem.Click += (_, _) => OpenInExplorer(_logDir);
        settingsItem.Click += (_, _) => OpenWithShell(Path.Combine(AppContext.BaseDirectory, "config.json"));
        quitItem.Click += (_, _) => Quit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_controllerItem);
        menu.Items.Add(_stateItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_debugItem);
        menu.Items.Add(logsItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quitItem);

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "GameMode",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private void ToggleConsole()
    {
        if (_consoleShown)
        {
            Kernel32.FreeConsole();
            _consoleShown = false;
            _debugItem.Text = "Show Console";
        }
        else
        {
            Kernel32.AllocConsole();
            _consoleShown = true;
            _debugItem.Text = "Hide Console";
        }
        _logger.SetConsoleEnabled(_consoleShown);
    }

    public void UpdateController(string status)
    {
        _controllerItem.Text = $"Controller: {status}";
        UpdateTooltip();
    }

    public void UpdateState(string state)
    {
        _stateItem.Text = $"State: {state}";
        UpdateTooltip();
    }

    public void ShowNotification(string title, string text)
    {
        _icon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
    }

    private void UpdateTooltip()
    {
        _icon.Text = $"GameMode\n{_controllerItem.Text}\n{_stateItem.Text}";
    }

    private void Toggle()
    {
        _enabled = !_enabled;
        _toggleItem.Text = _enabled ? "Disable Game Mode" : "Enable Game Mode";
        EnableToggled?.Invoke();
    }

    private static void OpenInExplorer(string path)
    {
        try { Process.Start("explorer.exe", path); } catch { }
    }

    private static void OpenWithShell(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
    }

    private void Quit()
    {
        _icon.Visible = false;
        QuitRequested?.Invoke();
    }

    public void Dispose()
    {
        _icon.Dispose();
    }
}
