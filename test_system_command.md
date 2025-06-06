# System Command Test Documentation

## Overview
This document outlines the testing procedure for the new `-s` (System Health) command functionality in the SonarrExplorer plugin.

## New Features Implemented

### 1. SystemCommand (`-s`)
- **Command Flag**: `-s`
- **Purpose**: Monitor Sonarr system health checks and trigger re-tests
- **API Endpoint**: Uses `/api/v3/health` to fetch health issues
- **Test Command**: Uses `/api/v3/command` with `{"name": "CheckHealth"}` to trigger re-tests

### 2. Health Check Models
- **SonarrHealthCheck**: Model for health check data from Sonarr API
- **Properties**: Source, Type, Message, WikiUrl
- **Display Methods**: GetIcon(), GetDisplayTitle(), GetDisplaySubTitle()

### 3. Health Service
- **SonarrHealthService**: Service for health check operations
- **Methods**:
  - `GetHealthChecksAsync()`: Fetch all health issues
  - `TriggerHealthCheckAsync()`: Trigger complete health check re-test
  - `RetestHealthCheckAsync(healthCheck)`: Re-test specific health issue
  - `OpenSystemStatusInBrowser()`: Open Sonarr system status page
  - `PingAsync()`: Basic connectivity test

### 4. Context Menu Integration
- **Right-click on health issues** provides:
  - 🌐 Open in Sonarr (system status page)
  - 🔄 Re-test This Issue
  - 📋 Copy Error Message
  - ❓ View Help Documentation (if WikiUrl available)

## Testing Steps

### 1. Basic Command Test
1. Open Flow Launcher
2. Type: `snr -s`
3. Expected: Shows "🔄 Test All Health Checks" button at top
4. Expected: Shows current health issues (if any) or "✅ All Systems Healthy"

### 2. Test All Functionality
1. Click on "🔄 Test All Health Checks" button
2. Expected: Triggers health check re-test in Sonarr
3. Expected: Returns true (closes Flow Launcher)

### 3. Individual Health Issue Testing
1. If health issues are present, click on any health issue
2. Expected: Triggers re-test for that specific issue
3. Expected: Returns true (closes Flow Launcher)

### 4. Context Menu Testing
1. Right-click on any health issue
2. Expected: Shows context menu with options:
   - 🌐 Open in Sonarr
   - 🔄 Re-test This Issue
   - 📋 Copy Error Message
   - ❓ View Help Documentation (if available)

### 5. Help Integration Test
1. Type: `snr -help`
2. Expected: Shows "🏥 System Health" in the command list
3. Expected: Description includes "snr -s - Monitor health checks, view issues, and trigger re-tests"

## API Endpoints Used

### Health Check Retrieval
```
GET /api/v3/health
Headers: X-Api-Key: {apikey}
Response: Array of health check objects
```

### Health Check Re-test
```
POST /api/v3/command
Headers: X-Api-Key: {apikey}
Body: {"name": "CheckHealth"}
Response: Command execution confirmation
```

### System Status Page
```
Browser URL: {sonarr_url}/system/status
```

## Error Handling

### 1. API Connection Issues
- Shows "❌ Error Fetching Health Status" if API call fails
- Includes error message in subtitle

### 2. Settings Validation
- Shows setup error if API key not configured
- Provides guidance to configure settings

### 3. Re-test Failures
- Logs debug messages for successful/failed re-test attempts
- Graceful handling of network/API errors

## Integration Points

### 1. CommandManager
- SystemCommand added to API-dependent commands list
- Routed when query starts with `-s`

### 2. Main Plugin Class
- Context menu handling for SonarrHealthCheck objects
- Delegates to SystemCommand.CreateHealthCheckContextMenu()

### 3. SonarrService Facade
- Health operations exposed through main service
- Delegates to SonarrHealthService

## Expected Behavior

### When No Health Issues
```
🔄 Test All Health Checks
✅ All Systems Healthy
```

### When Health Issues Present
```
🔄 Test All Health Checks
⚠️ Found 2 Health Issues
❌ Download Client Connection Failed
⚠️ Indexer Unavailable
```

### Context Menu Example
```
🌐 Open in Sonarr
🔄 Re-test This Issue
📋 Copy Error Message
```

This implementation provides a comprehensive health monitoring system that integrates seamlessly with the existing plugin architecture while following established patterns for commands, services, and context menus. 