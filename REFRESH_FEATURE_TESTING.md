# Sonarr Refresh Functionality - Testing Guide

## Overview

This document describes the new refresh functionality added to the Sonarr Explorer plugin. The refresh feature allows you to trigger Sonarr's "RescanSeries" command directly from Flow Launcher.

## New Features Added

### 1. RefreshCommand (`-r` flag)
- **Command Flag**: `-r`
- **Description**: Refresh all series or search for a specific series to refresh
- **Usage**: 
  - `snr -r` - Show refresh options
  - `snr -r all` - Refresh all series
  - `snr -r <series name>` - Search for series and show refresh options

### 2. SonarrService Refresh Methods
- `RefreshAllSeriesAsync()` - Sends RescanSeries command to refresh all series
- `RefreshSeriesAsync(int seriesId)` - Sends RescanSeries command for specific series

## How to Test

### Test in Flow Launcher

1. **Open Flow Launcher** (should have started automatically after deployment)

2. **Test basic refresh options**:
   ```
   snr -r
   ```
   - Should show "Refresh All Series" option
   - Should show "Refresh Options" with usage instructions

3. **Test refresh all**:
   ```
   snr -r all
   ```
   - Should show only "Refresh All Series" option
   - Clicking it will send refresh command to Sonarr

4. **Test series search for refresh**:
   ```
   snr -r your
   ```
   - Should search for series containing "your" 
   - Should show "Refresh: [Series Name]" for each matching series
   - Should also show "Refresh All Series" option at the bottom

5. **Test with non-existent series**:
   ```
   snr -r nonexistent
   ```
   - Should show "No Series Found" message
   - Should still show "Refresh All Series" option

### What Happens When You Execute Refresh

When you select a refresh option:
1. The plugin sends a POST request to `[sonarr-url]/api/v3/command`
2. For refresh all: `{"name": "RescanSeries"}`
3. For specific series: `{"name": "RescanSeries", "seriesId": [series-id]}`
4. The function returns `true` if the request was successful, `false` otherwise

### Verify in Sonarr

After triggering a refresh:
1. Go to your Sonarr web interface
2. Click on "System" > "Logs" 
3. Look for log entries about "RescanSeries" command
4. Or check "System" > "Status" to see if a rescan task is running

## API Details

### Sonarr RescanSeries Command

The refresh functionality uses Sonarr's built-in RescanSeries command:

- **Endpoint**: `POST /api/v3/command`
- **Headers**: `Content-Type: application/json`, `X-Api-Key: [your-api-key]`
- **Payload for all series**: `{"name": "RescanSeries"}`
- **Payload for specific series**: `{"name": "RescanSeries", "seriesId": [series-id]}`

This command triggers Sonarr to:
- Scan series folders for new/changed files
- Update episode file information
- Refresh metadata
- Update missing/available episode status

## Troubleshooting

### Common Issues

1. **"No API key found"**: Make sure your plugin settings are configured correctly
2. **"Failed to send refresh command"**: Check your Sonarr server URL and API key
3. **No response from Sonarr**: Verify Sonarr is running and accessible

### Debug Information

The plugin writes debug information to the console. If you're having issues:
1. Check the Flow Launcher logs
2. Look for debug messages starting with `[SonarrService]`

## Files Modified

1. **SonarrFlowLauncherPlugin/Services/SonarrService.cs**
   - Added `RefreshAllSeriesAsync()` method
   - Added `RefreshSeriesAsync(int seriesId)` method

2. **SonarrFlowLauncherPlugin/Commands/RefreshCommand.cs** (new file)
   - Implements the `-r` command functionality
   - Handles search and execution of refresh commands

3. **SonarrFlowLauncherPlugin/Commands/CommandManager.cs**
   - Added RefreshCommand to the list of available commands

## Branch Information

- **Branch**: `feature/refresh-functionality`
- **Status**: Ready for testing
- **Next Steps**: Test thoroughly, then merge to main branch

## Testing Checklist

- [ ] Plugin builds successfully
- [ ] Plugin deploys to Flow Launcher
- [ ] `-r` command shows refresh options
- [ ] `-r all` shows refresh all option
- [ ] `-r <series>` searches and shows series
- [ ] Actual refresh commands work (test carefully!)
- [ ] Error handling works (test with invalid API key/server)
- [ ] No breaking changes to existing functionality

## Notes

- The refresh functionality follows the same pattern as other commands in the plugin
- All refresh operations are asynchronous
- The plugin provides feedback through debug logs
- Remember that refresh operations in Sonarr can take time depending on library size 