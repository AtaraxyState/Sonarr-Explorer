﻿# Sonarr Flow Launcher Plugin

A comprehensive Flow Launcher plugin for Sonarr integration. Search your library, monitor downloads, view upcoming episodes, and intelligently refresh series - all from your launcher.

![Version](https://img.shields.io/badge/version-1.0.6-blue) ![Flow Launcher](https://img.shields.io/badge/Flow%20Launcher-compatible-green) ![.NET](https://img.shields.io/badge/.NET-7.0-purple)

## ðŸš€ Quick Start

<details>
<summary><b>ðŸ“‹ Setup Guide</b></summary>

### Guided Setup (Recommended)
1. Install the plugin
2. Type `snr` in Flow Launcher
3. Follow the **"ðŸ”§ Setup Required"** prompt
4. Type `snr -setup` and follow the interactive wizard

### Manual Setup
1. Open Flow Launcher Settings â†’ Plugins â†’ Sonarr Explorer
2. Configure:
   - **Server URL**: `localhost:8989` (adjust port as needed)
   - **API Key**: Found in Sonarr â†’ Settings â†’ General â†’ API Key
   - **HTTPS**: Toggle if using SSL

### Finding Your API Key
1. Open Sonarr web interface
2. Go to Settings â†’ General
3. Copy the long string from the "API Key" field

</details>

<details>
<summary><b>âš¡ Installation Options</b></summary>

### From Release (Recommended)
1. Download latest release from GitHub
2. Extract to `%APPDATA%\FlowLauncher\Plugins\SonarrFlowLauncherPlugin`
3. Restart Flow Launcher

### From Source
```powershell
git clone https://github.com/AtaraxyState/Sonarr-Explorer.git
cd Sonarr-Explorer
.\deploy.ps1
```

</details>

---

## ðŸ“– Core Features

<details>
<summary><b>ðŸ“… Calendar & Episode Tracking</b></summary>

### View Upcoming Episodes (`snr -c`)
- **Today**: `snr -c today` - Episodes airing today
- **Tomorrow**: `snr -c tomorrow` - Tomorrow's schedule  
- **This Week**: `snr -c week` - Week overview
- **Next Week**: `snr -c "next week"` - Upcoming week
- **This Month**: `snr -c month` - Monthly view

### Smart Episode Information
- **Air Times**: Automatic timezone conversion (UTC â†’ Local)
- **Episode Details**: Season/Episode numbers, titles, overviews
- **Status Indicators**: Monitored/unmonitored, downloaded status
- **User-Friendly Dates**: "Today", "Tomorrow", specific dates

</details>

<details>
<summary><b>ðŸ“Š Activity & Download Monitoring</b></summary>

### Current Downloads (`snr -a q`)
- **Live Progress**: Real-time download percentages
- **Quality Info**: Resolution, codec, release group
- **Episode Context**: Series name, season/episode numbers
- **Status Tracking**: Downloading, importing, completed

### Download History (`snr -a h`)
- **Recent Activity**: Last completed downloads
- **Success/Failure Status**: Color-coded indicators
- **Time Context**: "Today", "Yesterday", specific dates
- **Quality Details**: Final file quality and specifications

### Combined View (`snr -a`)
Shows both queue and recent history for complete activity overview.

</details>

<details>
<summary><b>ðŸ” Library Search & Management</b></summary>

### Series Search (`snr -l [search term]`)
- **Instant Search**: Type to find series in your library
- **Series Details**: Status, statistics, poster images
- **Quick Access**: One-click to open series in Sonarr web UI
- **Status Overview**: Monitored status, episode counts

### Library Overview (`snr -l`)
- Browse entire library
- Filter by status (monitored/unmonitored)
- Quick series information access

</details>

<details>
<summary><b>ðŸ”„ Intelligent Refresh System</b></summary>

### Calendar-Based Refresh (Smart)
- **Today's Episodes**: `snr -c` or `snr -r c` - Refresh series with today's episodes
- **Yesterday's Episodes**: `snr -y` or `snr -r y` - Catch up on yesterday's shows
- **Overdue Episodes**: `snr -n` or `snr -r n` - Refresh shows with episodes that already aired
- **Past N Days**: `snr -r 3` - Refresh series from past 3 days (any number)

### Traditional Refresh Options
- **All Series**: `snr -r all` - Full library refresh
- **Specific Series**: `snr -r [series name]` - Search and refresh individual shows

### Advanced Features
- **Timezone Intelligence**: Automatic UTC to local time conversion
- **Grace Period**: 10-minute buffer before considering episodes "overdue"
- **Duplicate Prevention**: Avoids multiple refreshes of the same series
- **Rate Limiting**: Prevents server overload with 100ms delays
- **Detailed Logging**: Comprehensive debug information

</details>

---

## ðŸŽ¯ Command Reference

<details>
<summary><b>ðŸ“‹ Complete Command List</b></summary>

| Command | Alternative | Description |
|---------|-------------|-------------|
| **Calendar & Episodes** |
| `snr -c` | | ðŸ“º View upcoming episodes (options below) |
| `snr -c today` | | ðŸ“º View today's episodes |
| `snr -c week` | | ðŸ“º View this week's episodes |
| `snr -c month` | | ðŸ“º View this month's episodes |
| `snr -r c` | | ðŸ“… Refresh today's calendar series |
| `snr -y` | `snr -r y` | ðŸ“… Refresh yesterday's calendar series |
| `snr -n` | `snr -r n` | â° Refresh overdue episodes |
| `snr -r 3` | | ðŸ“… Refresh series from past 3 days |
| **Activity & Downloads** |
| `snr -a` | | ðŸ“Š Show activity overview |
| `snr -a q` | `snr -a queue` | ðŸ“¥ Show download queue |
| `snr -a h` | `snr -a history` | ðŸ“œ Show download history |
| **Library & Search** |
| `snr -l` | | ðŸ” Browse library |
| `snr -l [term]` | | ðŸ” Search for series |
| `snr [series]` | | ðŸ” Quick series search |
| **Management** |
| `snr -r all` | | ðŸ”„ Refresh all series |
| `snr -r [series]` | | ðŸ”„ Refresh specific series |
| **Utilities** |
| `snr -setup` | | ðŸ”§ Guided setup wizard |
| `snr -help` | | â“ Show help information |
| `snr -about` | | â„¹ï¸ Plugin information |
| `snr -test` | | ðŸ§ª Test connection & settings |

</details>

<details>
<summary><b>ðŸ’¡ Usage Examples</b></summary>

### Daily Workflow
```
# Check what's airing today
snr -c today

# Refresh today's shows for new episodes
snr -r c

# Check download progress
snr -a q

# Look for a specific series
snr breaking bad
```

### Weekly Maintenance
```
# Refresh past week's shows
snr -r 7

# Check what's coming this week
snr -c week

# Review recent download history
snr -a h
```

### Troubleshooting
```
# Test your connection
snr -test

# Get help with commands
snr -help

# Check plugin information
snr -about
```

</details>

---

## ðŸ”§ Technical Details

<details>
<summary><b>âš™ï¸ Architecture & Performance</b></summary>

### Threading Model
- **UI Thread Safety**: All UI components created on main thread
- **Background Processing**: Service operations run asynchronously
- **Settings Hot-Reload**: Configuration changes applied immediately
- **No Restart Required**: Settings updates work without Flow Launcher restart

### API Integration
- **Rate Limiting**: 100ms delays between refresh commands
- **Error Handling**: Graceful degradation on connection issues
- **Timeout Management**: Proper handling of slow responses
- **Batch Operations**: Efficient grouping of related requests

### Data Processing
- **Timezone Handling**: Automatic UTC to local time conversion
- **Date Parsing**: Robust handling of various date formats
- **Memory Management**: Efficient caching and cleanup
- **Performance Optimization**: Minimal UI blocking operations

</details>

<details>
<summary><b>ðŸ§  Smart Algorithms</b></summary>

### Overdue Detection Logic
1. **Parse Air Dates**: Handle Sonarr's UTC timestamps
2. **Convert Timezones**: UTC â†’ Local time for accurate comparison
3. **Apply Buffer**: 10-minute grace period prevents false positives
4. **Current Time Check**: Compare against actual local time
5. **Debug Logging**: Detailed output for troubleshooting

### Calendar Intelligence
1. **Date Range Fetching**: Efficient calendar API calls
2. **Series Grouping**: Prevent duplicate refresh commands
3. **Targeted Refresh**: Only refresh series with relevant episodes
4. **Success Tracking**: Monitor and report refresh status

### Search Optimization
- **Fuzzy Matching**: Find series even with partial/inexact names
- **Relevance Scoring**: Best matches appear first
- **Context Awareness**: Recent searches get priority
- **Performance Caching**: Reduce repeated API calls

</details>

<details>
<summary><b>ðŸ› ï¸ Development Information</b></summary>

### Prerequisites
- .NET 7.0 SDK
- Flow Launcher installed
- PowerShell (for deployment scripts)
- Sonarr instance for testing

### Build Commands
```powershell
# Build the project
dotnet build

# Run in debug mode
dotnet build --configuration Debug

# Create release build
dotnet build --configuration Release
```

### Deployment
```powershell
# Deploy with execution policy bypass
powershell -ExecutionPolicy Bypass -File .\deploy.ps1

# Deploy if scripts are trusted
.\deploy.ps1
```

### Project Structure
```
SonarrFlowLauncherPlugin/
â”œâ”€â”€ Commands/           # Command handlers
â”œâ”€â”€ Services/          # API and business logic
â”œâ”€â”€ Models/            # Data models
â”œâ”€â”€ Images/            # Plugin icons
â””â”€â”€ plugin.json        # Plugin manifest
```

</details>

---

## ðŸš¨ Troubleshooting

<details>
<summary><b>ðŸ” Common Issues & Solutions</b></summary>

### Plugin Not Working
**Symptoms**: Plugin doesn't appear or respond
- âœ… Verify Flow Launcher is running
- âœ… Check Plugins list in Flow Launcher settings  
- âœ… Ensure all files copied to plugin directory
- âœ… Restart Flow Launcher completely

### Connection Problems
**Symptoms**: "Connection failed" or timeout errors
- âœ… Verify Sonarr is running and accessible
- âœ… Test URL in browser: `http://localhost:8989`
- âœ… Check API key is correct (copy from Sonarr settings)
- âœ… Verify port number matches your Sonarr config
- âœ… Ensure HTTPS setting matches your setup

### Setup Issues
**Symptoms**: Can't configure or settings not saving
- âœ… Try guided setup: `snr -setup`
- âœ… Check Flow Launcher has write permissions
- âœ… Verify plugin.json file isn't corrupted
- âœ… Use manual settings panel as alternative

### Calendar/Refresh Problems
**Symptoms**: No episodes found or refresh not working
- âœ… Check Sonarr calendar has data for the date range
- âœ… Verify series are monitored in Sonarr
- âœ… Ensure episodes have proper air dates set
- âœ… Check Flow Launcher debug logs for detailed info

</details>

<details>
<summary><b>ðŸ› Debug Information</b></summary>

### Enable Debug Logging
1. Open Flow Launcher settings
2. Go to General â†’ Logging
3. Enable Debug level logging
4. Restart Flow Launcher
5. Check logs in `%APPDATA%\FlowLauncher\Logs`

### What to Look For
- **Connection attempts**: API call success/failure
- **Timezone conversions**: UTC to local time calculations  
- **Episode detection**: Which episodes are found/considered overdue
- **Threading issues**: UI thread violations or deadlocks

### Common Log Messages
```
âœ… [INFO] Successfully connected to Sonarr
âŒ [ERROR] Failed to connect: Connection refused
ðŸ”„ [DEBUG] Converting UTC time 2024-01-15T20:00:00Z to local
â° [DEBUG] Episode aired at 15:00, current time 15:05 - NOT overdue (within buffer)
```

</details>

---

## ðŸ“ž Support & Community

<details>
<summary><b>ðŸ’¬ Getting Help</b></summary>

### GitHub Issues
- **Bug Reports**: Use issue templates for detailed reports
- **Feature Requests**: Suggest new functionality
- **Questions**: Ask for help with setup or usage

### Self-Help Resources
- **Built-in Help**: `snr -help` for command reference
- **Test Connection**: `snr -test` for diagnostics
- **Plugin Info**: `snr -about` for version details

### Contributing
- Fork the repository
- Create feature branches
- Submit pull requests
- Follow existing code style

</details>

<details>
<summary><b>ðŸ† Credits & License</b></summary>

### Acknowledgments
- **Sonarr Team**: For the excellent API and official icons
- **Flow Launcher**: For the fantastic launcher platform
- **Community**: For testing, feedback, and feature suggestions

### Technologies Used
- **.NET 7.0**: Core framework
- **Flow Launcher SDK**: Plugin integration
- **Newtonsoft.Json**: JSON processing
- **System.Net.Http**: API communication

### License
MIT License - Free to use, modify, and distribute

### Development Environment
- **Cursor**: Primary development IDE
- **GitHub**: Source control and releases
- **PowerShell**: Deployment automation

</details>

---

<div align="center">

**ðŸŽ¯ A comprehensive Flow Launcher plugin for Sonarr integration**  
*Search â€¢ Monitor â€¢ Refresh â€¢ Track*

[![GitHub](https://img.shields.io/badge/GitHub-Repository-blue)](https://github.com/AtaraxyState/Sonarr-Explorer) 
[![Flow Launcher](https://img.shields.io/badge/Flow%20Launcher-Plugin-green)](https://www.flowlauncher.com/)

</div>


