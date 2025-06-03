# Sonarr Flow Launcher Plugin

A Flow Launcher plugin to control Sonarr directly from your launcher. Quickly check upcoming episodes, monitor downloads, search your library, and intelligently refresh series based on calendar data.

## Features

### Calendar View (`snr -c`)
View upcoming episodes with various time ranges:
- `snr -c today`: Shows today's episodes
- `snr -c tomorrow`: Shows tomorrow's episodes
- `snr -c week`: Shows this week's episodes
- `snr -c next week`: Shows next week's episodes
- `snr -c month`: Shows this month's episodes

### Activity Monitor (`snr -a`)
View current downloads and recent history:
- `snr -a q` or `snr -a queue`: Shows active downloads with progress
- `snr -a h` or `snr -a history`: Shows recent activity history
- Shows download progress and quality information (update frequency limited by Sonarr)
- Displays episode information (S##E##)
- User-friendly timestamps (Today/Yesterday/Date)

### Library Search (`snr -l`)
Search your Sonarr library:
- Quick access to series information
- View monitored status
- Series statistics and details
- One-click access to series in browser

### Smart Refresh System (`snr -r`)
Intelligently refresh series based on calendar data and air times:

#### Calendar-Based Refresh Options
- `snr -r c` or `snr -c`: **Today's Calendar** - Refresh all series that have episodes airing today
- `snr -r y` or `snr -y`: **Yesterday's Calendar** - Refresh all series that had episodes yesterday
- `snr -r n` or `snr -n`: **Overdue Episodes** - Refresh series with episodes that have already aired (with 10-minute buffer)
- `snr -r {days}`: **Prior Days** - Refresh series from past N days (e.g., `snr -r 3` for past 3 days, `snr -r 7` for past week)

#### Traditional Refresh Options
- `snr -r all`: Refresh all series (traditional method)
- `snr -r [series name]`: Search and refresh specific series

#### Smart Features
- **Timezone Handling**: Automatically converts UTC air times to local time
- **Buffer Time**: 10-minute grace period before considering episodes "overdue"
- **Duplicate Prevention**: Groups episodes by series ID to avoid multiple refreshes
- **API Rate Limiting**: 100ms delays between refresh commands to prevent server overload
- **Comprehensive Logging**: Detailed debug output for troubleshooting

#### Usage Examples
```
# Refresh series with episodes today
snr -c

# Refresh series with overdue episodes
snr -n

# Refresh series from past 3 days
snr -r 3

# Refresh series from past week
snr -r 7

# Traditional refresh all
snr -r all
```

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

**Settings updates are applied immediately** - no Flow Launcher restart required after changing configuration.

## Technical Details

### Threading Model
The plugin properly handles WPF threading requirements:
- UI components are created only on the main thread
- Service components can be refreshed on background threads
- Settings changes are applied without requiring restart

### Overdue Detection Algorithm
The overdue detection uses enhanced logic for accurate air time checking:

1. **DateTime Parsing**: Properly handles Sonarr's UTC timestamps
2. **Timezone Conversion**: Converts UTC air times to local time for comparison
3. **Buffer Time**: 10-minute grace period prevents false positives
4. **Detailed Logging**: Shows exactly when episodes air vs current time

### Calendar Refresh Intelligence
The calendar-based refresh system:
- Fetches calendar data for specified date ranges
- Identifies unique series with episodes in the timeframe
- Sends targeted RescanSeries commands only for relevant series
- Provides detailed success/failure tracking

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
- Enable debug logging to see detailed overdue detection and timezone conversion information

## Troubleshooting

### General Issues
1. **Plugin Not Loading**
   - Verify Flow Launcher is running
   - Check if the plugin is listed in Flow Launcher settings
   - Ensure all DLLs are properly copied to the plugin directory

2. **Connection Issues**
   - Verify Sonarr is running and accessible
   - Check API key is correct
   - Ensure URL includes port number
   - Verify HTTPS setting matches your Sonarr configuration

### Settings Issues
3. **Settings Not Updating**
   - Configuration changes are applied immediately (no restart needed)
   - If issues persist, check Flow Launcher logs for threading errors
   - Verify plugin.yaml file has write permissions

### Calendar/Refresh Issues
4. **Overdue Detection Not Working**
   - Check Flow Launcher debug logs for timezone conversion details
   - Verify Sonarr calendar data includes proper air times
   - Episodes are only considered "overdue" after 10+ minutes past air time

5. **No Episodes Found in Calendar**
   - Verify date ranges in Sonarr calendar
   - Check that series are properly monitored in Sonarr
   - Ensure episodes have proper air dates set

## Command Reference

| Command | Alias | Description |
|---------|-------|-------------|
| `snr -c` | `snr -r c` | Refresh today's calendar series |
| `snr -y` | `snr -r y` | Refresh yesterday's calendar series |
| `snr -n` | `snr -r n` | Refresh overdue episodes |
| `snr -r {number}` | | Refresh series from past N days |
| `snr -r all` | | Refresh all series |
| `snr -a` | | Show activity (queue + history) |
| `snr -l` | | Search library |

## License

MIT License

## Credits

- Uses official Sonarr icon from [Sonarr's repository](https://github.com/Sonarr/Sonarr)
- Built for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher)

---

*A Flow Launcher plugin for Sonarr integration with intelligent calendar-based refresh capabilities*  
*Developed in [Cursor](https://cursor.sh/)*
