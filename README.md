# Sonarr Flow Launcher Plugin

A Flow Launcher plugin to control Sonarr directly from your launcher. Quickly check upcoming episodes, monitor downloads, and search your library.

## Features

- **Calendar View** (`snr -c`): View upcoming episodes
  - `snr -c today`: Shows today's episodes
  - `snr -c tomorrow`: Shows tomorrow's episodes
  - `snr -c week`: Shows this week's episodes
  - `snr -c next week`: Shows next week's episodes
  - `snr -c month`: Shows this month's episodes

- **Activity Monitor** (`snr -a`): View current downloads and recent history
  - `snr -a q` or `snr -a queue`: Shows active downloads with progress
  - `snr -a h` or `snr -a history`: Shows recent activity history
  - Shows download progress and quality information
  - Displays episode information (S##E##)
  - User-friendly timestamps (Today/Yesterday/Date)

- **Library Search** (`snr -s`): Search your Sonarr library
  - Quick access to series information
  - View monitored status
  - Series statistics and details
  - One-click access to series in browser

## Installation

1. Download the latest release
2. Extract to `%APPDATA%\FlowLauncher\Plugins\SonarrFlowLauncherPlugin`
3. Restart Flow Launcher

## Configuration

Configure the plugin through Flow Launcher settings:

1. Server URL (e.g., `localhost:8989`)
2. API Key (from Sonarr's Settings > General)
3. HTTPS toggle (if using SSL)

## Development

### Prerequisites
- .NET 7.0 SDK
- Flow Launcher

### Building
```powershell
dotnet build
```

### Testing
```powershell
.\run-tests.ps1
```

### Deployment
```powershell
.\deploy.ps1
```

## License

MIT License

## Credits

- Sonarr icon from [iconduck.com](https://iconduck.com/icons/253013/sonarr)
- Built for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher)

---

*This plugin is developed using [Cursor](https://cursor.sh/), the world's best IDE powered by AI.*
