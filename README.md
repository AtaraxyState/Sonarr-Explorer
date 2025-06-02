# Sonarr Flow Launcher Plugin

A Flow Launcher plugin for managing and exploring your Sonarr media library. Search series, view upcoming episodes, monitor download activity, and access various utilities - all from Flow Launcher.

![Plugin Icon](SonarrFlowLauncherPlugin/Images/icon.png)

## Features

### 🔍 Core API Features
- **Library Search**: Search your Sonarr series library
- **Calendar View**: See upcoming episodes for today, tomorrow, week, or month
- **Activity Monitor**: Track download queue and history
- **Series Management**: Quick access to series information and external links

### 🛠️ Offline Utilities
Works without Sonarr API configuration:
- **Help System**: Comprehensive command help and documentation
- **Plugin Information**: Version, configuration status, and links
- **Connection Testing**: Test connectivity to your Sonarr server
- **Settings Access**: Quick access to plugin and Flow Launcher settings
- **Date/Time Tools**: Current time, timezones, and date calculations
- **External Links**: Quick access to TVDB, IMDB, Reddit, and documentation

## Installation

### From Flow Launcher Plugin Store
1. Open Flow Launcher
2. Type `pm install Sonarr Explorer`
3. Follow the installation prompts

### Manual Installation
1. Download the latest release from [GitHub Releases](https://github.com/AtaraxyState/Sonarr-Explorer/releases)
2. Extract to your Flow Launcher plugins directory
3. Restart Flow Launcher

## Setup

1. Open Flow Launcher settings
2. Navigate to Plugins → Sonarr Explorer
3. Configure your Sonarr settings:
   - **Server URL**: Your Sonarr server address (e.g., `localhost:8989`)
   - **API Key**: Your Sonarr API key (found in Settings → General → Security)
   - **Use HTTPS**: Enable if your server uses HTTPS

## Commands Reference

### Main Commands (Require API)

#### 📅 Calendar
```
snr -c                    # Today's episodes
snr -c today             # Today's episodes  
snr -c tomorrow          # Tomorrow's episodes
snr -c week              # This week's episodes
snr -c next week         # Next week's episodes
snr -c month             # This month's episodes
```

#### 📊 Activity
```
snr -a                   # Recent activity overview
snr -a q                 # Download queue
snr -a queue             # Download queue
snr -a h                 # Download history
snr -a history           # Download history
```

#### 🔍 Library Search
```
snr -l [search term]     # Search your library
snr -l                   # Show all series in your library
snr [search term]        # Search your library (default)
```

### 🛠️ Offline Commands (No API Required)

#### ❓ Help & Information
```
snr -help               # Show all available commands
snr -about              # Plugin version and information
```

#### 🔧 Utilities & Testing
```
snr -test               # Show utility options
snr -test connection    # Test server connectivity
snr -test settings      # Open plugin settings
snr -test logs          # Open Flow Launcher logs
snr -test reload        # Reload plugin settings
snr -settings          # Quick settings access
```

#### 🕒 Date & Time Tools
```
snr -date              # Current date and time
snr -date time         # World timezone view
snr -date convert      # Date conversion tools
snr -date utc          # UTC time information
snr -time              # Alternative time command
```

#### 🔗 External Links & Searches
```
snr -link              # Show all external links
snr -link tvdb [show]  # Search TheTVDB
snr -link imdb [show]  # Search IMDB  
snr -link reddit       # Open r/sonarr
snr -link docs         # Plugin documentation
snr -link sonarr       # Official Sonarr links
snr -link github       # Plugin repository

# Direct shortcuts:
snr -tvdb [show]       # Direct TVDB search
snr -imdb [show]       # Direct IMDB search
snr -reddit            # Direct Reddit access
```

## Quick Start Examples

```bash
# Search for a series
snr breaking bad

# Browse all series in your library
snr -l

# See today's episodes
snr -c

# Check download queue
snr -a q

# Get help
snr -help

# Test connection (offline)
snr -test connection

# Search TVDB (offline)
snr -tvdb breaking bad

# Current time (offline)
snr -date
```

## Features in Detail

### Library Search
- Search through your entire Sonarr library or browse all series
- Shows series title, year, status, and statistics
- Direct links to series in Sonarr web interface
- Displays cover art and overview
- Use without search term to view complete library

### Calendar View
- Upcoming episodes with air dates and times
- Episode titles and season/episode numbers
- Filter by different time periods
- Shows series poster and episode details

### Activity Monitor
- Real-time download queue with progress
- Download history with completion status
- File sizes and download speeds
- Quality and release information

### Offline Utilities
- **Connection Testing**: Verify server connectivity without API
- **Settings Management**: Quick access to configuration
- **Time Tools**: Current time, timezones, date calculations
- **External Searches**: TVDB, IMDB searches without API
- **Documentation**: Quick access to help and community resources

## Configuration

The plugin supports hot-reloading of settings. Changes to the configuration are automatically detected and applied without restarting Flow Launcher.

### Settings File Location
```
%AppData%\FlowLauncher\Settings\Plugins\Sonarr Explorer\plugin.yaml
```

### Available Settings
- `ServerUrl`: Sonarr server address
- `ApiKey`: Sonarr API key
- `UseHttps`: Use HTTPS for connections

## Troubleshooting

### Common Issues

**"API Key Not Set"**
- Configure your API key in plugin settings
- Find API key in Sonarr: Settings → General → Security

**Connection Errors**
- Use `snr -test connection` to diagnose connectivity
- Verify server URL and port
- Check if Sonarr is running and accessible

**No Results**
- Verify API key is correct
- Check Sonarr server is responsive
- Use `snr -test logs` to check for errors

### Getting Help
- Use `snr -help` for command reference
- Use `snr -about` for version and status information
- Check `snr -link docs` for detailed documentation
- Visit `snr -link reddit` for community support

## Development

### Building
```bash
dotnet build
```

### Testing
```bash
dotnet test
```

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## Links

- **GitHub Repository**: [AtaraxyState/Sonarr-Explorer](https://github.com/AtaraxyState/Sonarr-Explorer)
- **Flow Launcher**: [Flow-Launcher/Flow.Launcher](https://github.com/Flow-Launcher/Flow.Launcher)
- **Sonarr**: [Sonarr/Sonarr](https://github.com/Sonarr/Sonarr)

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Changelog

### v1.1.0 (Feature Branch)
- ✨ Added comprehensive offline functionality
- ✨ Help system with command documentation
- ✨ Plugin information and version display
- ✨ Connection testing and diagnostics
- ✨ Date/time utilities and timezone tools
- ✨ External link shortcuts (TVDB, IMDB, Reddit)
- ✨ Settings management utilities
- ✨ Alternative command shortcuts
- 🐛 Fixed series URL opening with proper slug format
- ♻️ Improved command structure and organization

### v1.0.4
- ✨ Added hot-reloading settings functionality  
- 🐛 Fixed queue progress calculation (was always showing 0%)
- ♻️ Major SonarrService refactoring and cleanup
- 📝 Fixed README library search command documentation
- 🚀 Implemented GitHub Actions for automated builds
- 📦 Prepared for official Flow Launcher plugin repository

### v1.0.3
- 🐛 Fixed activity queue progress calculation
- 🐛 Fixed series browser URL format
- ♻️ Refactored SonarrService for better organization
- 📝 Updated documentation

### v1.0.2
- 🚀 Initial stable release
- ✨ Calendar, Activity, and Library search features
- ⚙️ Plugin settings and configuration
- 🎨 Modern UI with emojis and better formatting

---

*A Flow Launcher plugin for Sonarr integration*  
*Developed in [Cursor](https://cursor.sh/)*
