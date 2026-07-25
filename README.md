# GameMode

A Windows tray utility that integrates an Xbox controller with **Playnite Fullscreen** to create a console-like living room experience.

## How it works

GameMode sits in the system tray and monitors controller state and the foreground window. It uses a state machine to manage Playnite automatically:

| State | What happens |
|---|---|
| **Idle** | Waiting — no controller detected |
| **ControllerConnected** | Controller is connected; [launches Playnite](#auto-launch) if not running |
| **BrowsingLibrary** | Playnite Fullscreen is in the foreground; [brings to front](#bring-to-front) if needed |
| **Gaming** | A game is running (foreground window is not Playnite/explorer/GameMode) |
| **DisconnectGracePeriod** | Controller disconnected — short wait before starting the countdown |
| **DisconnectCountdown** | Timer ticking; [closes Playnite](#auto-close) when it expires |

### Auto-launch

When a controller connects, GameMode **automatically starts Playnite Fullscreen** if it isn't already running.

### Bring to front

When Playnite's Console Mode is detected (i.e., it's the foreground window), GameMode can bring it to the front if it's minimized or behind other windows.

### Auto-close

If the controller is disconnected long enough (GracePeriodSeconds + DisconnectTimeoutSeconds), GameMode **closes Playnite** automatically.

## Features

- **System tray icon** — Shows controller status and current state at a glance
- **Enable / Disable** — Pause the state machine without quitting
- **Show / Hide Console** — Toggle a live debug log window at any time
- **Open Logs** — Opens the log directory in Explorer
- **Open Settings** — Opens `config.json` in your default editor
- **Log rotation** — Date-stamped log files, auto-deleted after `LogRetentionDays`
- **Quit via tray** — Cleanly exits the application

## Configuration

Edit `config.json` in the application directory:

| Key | Default | Description |
|---|---|---|
| `PlaynitePath` | — | Path to `Playnite.FullscreenApp.exe` |
| `CheckIntervalMs` | `500` | Polling interval (100–60000 ms) |
| `GracePeriodSeconds` | `2` | Wait time after controller disconnects before starting countdown |
| `DisconnectTimeoutSeconds` | `30` | Time after grace period before closing Playnite (capped at 3600) |
| `ClosePlaynite` | `true` | Whether to close Playnite when the disconnect countdown expires |
| `BringToFront` | `true` | Bring Playnite to the foreground when entering Console Mode |
| `LogRetentionDays` | `7` | Delete log files older than this many days |

## Building

```powershell
dotnet build -c Release
```

- **Debug** (`dotnet run`): Console window is shown with live log output
- **Release** (`-c Release`): Runs silently in the background (tray icon only)
- **Release + `--debug`**: Allocates a console window for live log output

## Usage

1. Edit `config.json` to set your Playnite path
2. Build or download the Release exe
3. (Optional) Add a shortcut to `shell:startup` for auto-start with Windows
4. Connect an Xbox controller — Playnite launches automatically

## License

Licensed under the [MIT License](LICENSE).

## Requirements

- Windows 10/11
- .NET 10 runtime
- Xbox 360/Xbox One/Series X\|S controller (XInput)
- [Playnite](https://playnite.link) with Fullscreen mode
