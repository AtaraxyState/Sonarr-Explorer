# Sonarr Flow Launcher Plugin - API Documentation

## Overview

The Sonarr Flow Launcher Plugin provides seamless integration between Flow Launcher and Sonarr, enabling users to search series, view calendar events, monitor downloads, and manage their TV library directly from the launcher interface.

## Architecture

### Core Components

#### 1. Main Plugin Entry Point
- **`Main.cs`** - Primary plugin class implementing Flow Launcher interfaces
  - Implements `IPlugin`, `ISettingProvider`, `IContextMenu`
  - Manages plugin lifecycle, settings hot-reloading, and connection monitoring
  - Coordinates between command manager and context menu service

#### 2. Settings Management
- **`Settings.cs`** - Configuration persistence and validation
  - YAML-based settings storage with automatic serialization
  - Real-time validation and connection testing
  - WPF settings control with immediate feedback

#### 3. Command System
- **`CommandManager.cs`** - Central command routing and coordination
- **`BaseCommand.cs`** - Abstract base class for all commands
- Individual command implementations:
  - `LibrarySearchCommand` - Series search and browsing
  - `CalendarCommand` - Episode calendar and scheduling
  - `ActivityCommand` - Download queue and history monitoring
  - `RefreshCommand` - Series refresh and rescan operations
  - `SystemCommand` - Health check monitoring and system status
  - `SetupCommand` - Guided configuration wizard
  - `UtilityCommand` - Connection testing and diagnostics
  - `HelpCommand` - Command documentation and usage
  - `AboutCommand` - Plugin information and status
  - `DateTimeCommand` - Date/time utilities for scheduling
  - `ExternalLinksCommand` - External database and forum links

#### 4. Service Layer
- **`SonarrService.cs`** - Main service facade coordinating specialized services
- **`ISonarrApiClient.cs`** - HTTP client interface for API communication
- **`SonarrApiClient.cs`** - Concrete HTTP client implementation
- **Specialized Services:**
  - `SonarrSeriesService` - Series search, metadata, and management
  - `SonarrCalendarService` - Calendar events and episode scheduling
  - `SonarrActivityService` - Download queue and completion history
  - `SonarrHealthService` - Health check monitoring and system status
  - `ContextMenuService` - Context menu generation for series and episodes

#### 5. Data Models
- **`SonarrSeries.cs`** - Series metadata with statistics and file paths
- **`SonarrEpisode.cs`** - Episode details with status and quality information
- **`SonarrEpisodeBase.cs`** - Base class for different episode types
- **`SonarrActivity.cs`** - Download and processing activity data
- **`SonarrCalendar.cs`** - Calendar event containers
- **`SonarrHealthCheck.cs`** - Health check issue details and status information
- **`RefreshCalendarResult.cs`** - Refresh operation results

## Key Features

### 1. Series Library Management
- **Search and Browse**: Full-text search across series titles and metadata
- **Direct File Access**: Context menus with folder and episode file links
- **Poster Integration**: Series posters as Flow Launcher icons
- **Statistics Display**: Episode counts, download progress, storage usage

### 2. Calendar Integration
- **Episode Scheduling**: View upcoming and recent episodes
- **Air Date Tracking**: Overdue episode detection and monitoring
- **Refresh Shortcuts**: Quick refresh commands for calendar-based series
- **Status Indicators**: Visual episode status (downloaded, missing, unaired)

### 3. Activity Monitoring
- **Download Queue**: Real-time download progress and status
- **History Tracking**: Completed downloads and processing events
- **Error Detection**: Failed downloads and import issues
- **Quality Information**: Video quality, resolution, and language details

### 4. Context Menus
- **File System Integration**: Direct access to series folders and video files
- **Episode-Specific Actions**: Context-aware options based on item type
- **Resolution Detection**: Automatic video resolution parsing and display
- **File Size Information**: Storage usage for individual episodes

### 5. System Health Monitoring
- **Health Check Display**: View all current system health issues
- **Issue Categorization**: Errors, warnings, and informational messages
- **Re-test Functionality**: Trigger health check re-tests for all or specific issues
- **System Status Access**: Direct browser links to Sonarr's system status page
- **Context Menu Integration**: Right-click options for health check management

### 6. Smart Configuration
- **Hot-Reloading**: Automatic settings reload without restart
- **Connection Testing**: Real-time API connectivity monitoring
- **Guided Setup**: Step-by-step configuration wizard
- **Offline Functionality**: Utility commands work without API connection

## API Design Patterns

### 1. Command Pattern
All user interactions are implemented as command objects inheriting from `BaseCommand`:
```csharp
public abstract class BaseCommand
{
    public abstract string CommandFlag { get; }
    public abstract string CommandName { get; }  
    public abstract string CommandDescription { get; }
    public abstract List<Result> Execute(Query query);
}
```

### 2. Facade Pattern
`SonarrService` provides a unified interface to specialized services:
```csharp
public class SonarrService : IDisposable
{
    // Delegates to SonarrSeriesService
    public async Task<List<SonarrSeries>> SearchSeriesAsync(string query)
    
    // Delegates to SonarrCalendarService
    public async Task<List<SonarrCalendarItem>> GetCalendarAsync(DateTime? start, DateTime? end)
    
    // Delegates to SonarrActivityService
    public async Task<SonarrActivity> GetActivityAsync()
}
```

### 3. Dependency Injection
Services are injected through constructor parameters enabling testability:
```csharp
public CommandManager(SonarrService sonarrService, Settings settings, PluginInitContext? context = null)
```

### 4. Interface Segregation
Clean separation between API client and implementation:
```csharp
public interface ISonarrApiClient : IDisposable
{
    string BaseUrl { get; }
    HttpClient HttpClient { get; }
    Settings Settings { get; }
}
```

## Error Handling

### 1. Connection Management
- Automatic connection testing with rate limiting
- Graceful degradation when API is unavailable
- User-friendly error messages with troubleshooting guidance

### 2. Exception Handling
- Try-catch blocks around all API calls
- Detailed logging for debugging
- Fallback results for failed operations

### 3. Validation
- Settings validation before API calls
- Input sanitization and URL construction
- File path validation for context menus

## Performance Optimizations

### 1. Asynchronous Operations
All API calls use async/await patterns to prevent UI blocking:
```csharp
public async Task<List<SonarrSeries>> SearchSeriesAsync(string query)
```

### 2. Caching Strategy
- Poster URL caching for faster icon display
- Series metadata caching for context menus
- Connection status caching with timeout

### 3. Resource Management
- Proper disposal of HTTP clients and services
- Background thread execution for API calls
- File system watching for settings changes

## Extension Points

### 1. Adding New Commands
Create a new class inheriting from `BaseCommand` and register in `CommandManager`:
```csharp
public class CustomCommand : BaseCommand
{
    public override string CommandFlag => "-custom";
    public override List<Result> Execute(Query query) { /* implementation */ }
}
```

### 2. Custom Context Menus
Extend `ContextMenuService` to add new context menu options:
```csharp
private void AddCustomContextOption(List<Result> contextMenus, SonarrSeries series)
```

### 3. Additional Services
Implement specialized services following the existing pattern:
```csharp
public class CustomSonarrService
{
    private readonly ISonarrApiClient _apiClient;
    public CustomSonarrService(ISonarrApiClient apiClient) { _apiClient = apiClient; }
}
```

## Documentation Generation

This codebase is fully documented with XML documentation comments for automated documentation generation using tools like:
- **DocFX** - Microsoft's documentation generation tool
- **Sandcastle** - .NET API documentation generator  
- **Swagger/OpenAPI** - For API endpoint documentation

### XML Documentation Standards
- All public classes have `<summary>` and `<remarks>` sections
- Methods include `<param>` and `<returns>` documentation
- Complex algorithms have detailed `<remarks>` explanations
- Interfaces clearly document contracts and usage patterns

### Key Documentation Features
- **Architecture Overview** - High-level system design explanations
- **Usage Examples** - Code samples for common operations
- **Extension Guidance** - How to add new features and commands
- **Error Scenarios** - Exception handling and troubleshooting
- **Performance Notes** - Optimization strategies and considerations

This documentation structure supports automated generation of comprehensive API reference materials, developer guides, and user documentation from the codebase annotations. 