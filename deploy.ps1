# Plugin configuration
$PluginDirectory = "$env:APPDATA\FlowLauncher\Plugins\SonarrFlowLauncherPlugin"
$FlowLauncherProcess = "Flow.Launcher"

Write-Host "Starting deployment process..."

# Stop Flow Launcher if it's running
Write-Host "Stopping Flow Launcher..."
$flowLauncherProcess = Get-Process "Flow.Launcher" -ErrorAction SilentlyContinue
if ($flowLauncherProcess) {
    $flowLauncherProcess | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Remove existing plugin directory if it exists
Write-Host "Cleaning up old plugin installation..."
if (Test-Path $PluginDirectory) {
    Remove-Item $PluginDirectory -Recurse -Force
}

# Create plugin directory
Write-Host "Creating plugin directory..."
New-Item -ItemType Directory -Path $PluginDirectory -Force | Out-Null

# Create Images directory if it doesn't exist
if (!(Test-Path "$PluginDirectory\Images")) {
    New-Item -ItemType Directory -Path "$PluginDirectory\Images" -Force | Out-Null
}

# Clean up any temporary files
Write-Host "Cleaning up temporary files..."
Get-ChildItem -Path "." -Recurse -Include "*.new", "*.bak", "*.tmp" | Remove-Item -Force

# Build the project
Write-Host "Building project..."
dotnet build .\SonarrFlowLauncherPlugin\SonarrFlowLauncherPlugin.csproj --configuration Release

# Copy only necessary files
Write-Host "Copying plugin files..."
Copy-Item ".\SonarrFlowLauncherPlugin\bin\Release\*" $PluginDirectory -Recurse -Force
Copy-Item ".\SonarrFlowLauncherPlugin\plugin.json" $PluginDirectory -Force
Copy-Item ".\SonarrFlowLauncherPlugin\plugin.yaml" $PluginDirectory -Force
Copy-Item ".\SonarrFlowLauncherPlugin\Images\*" "$PluginDirectory\Images" -Force

Write-Host "Plugin deployed to: $PluginDirectory"

# Start Flow Launcher
Write-Host "Starting Flow Launcher..."
Start-Process "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"

Write-Host "Deployment complete!" 