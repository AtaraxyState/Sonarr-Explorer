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

### From Release
1. Download the latest release
2. Extract to `%APPDATA%\FlowLauncher\Plugins\SonarrFlowLauncherPlugin`
3. Restart Flow Launcher

### From Source
1. Clone this repository
2. Run `.\deploy.ps1` (requires PowerShell with ExecutionPolicy allowing script execution)
3. Restart Flow Launcher if it doesn't restart automatically

## Configuration

Configure the plugin through Flow Launcher settings:

1. Server URL (e.g., `localhost:8989`)
2. API Key (from Sonarr's Settings > General)
3. HTTPS toggle (if using SSL)

## Development

### Prerequisites
- .NET 7.0 SDK
- Flow Launcher
- PowerShell (for deployment scripts)

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
# With execution policy bypass
powershell -ExecutionPolicy Bypass -File .\deploy.ps1

# Or if you trust the scripts
.\deploy.ps1
```

### Development Tips
- The plugin is automatically deployed to: `%APPDATA%\FlowLauncher\Plugins\SonarrFlowLauncherPlugin`
- Use `Flow.Launcher.Plugin.SDK` for Flow Launcher integration
- Debug logs can be found in Flow Launcher's logs directory

## Troubleshooting

1. **Plugin Not Loading**
   - Verify Flow Launcher is running
   - Check if the plugin is listed in Flow Launcher settings
   - Ensure all DLLs are properly copied to the plugin directory

2. **Connection Issues**
   - Verify Sonarr is running and accessible
   - Check API key is correct
   - Ensure URL includes port number
   - Verify HTTPS setting matches your Sonarr configuration

## License

MIT License

## Credits

- Uses official Sonarr icon from [Sonarr's repository](https://github.com/Sonarr/Sonarr)
- Built for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher)

---

*A Flow Launcher plugin for Sonarr integration*  
*Developed in [Cursor](https://cursor.sh/)*
