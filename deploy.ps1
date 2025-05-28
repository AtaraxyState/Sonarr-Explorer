# Plugin configuration
$PluginDirectory = "$env:APPDATA\FlowLauncher\Plugins\SonarrFlowLauncherPlugin"
$FlowLauncherProcess = "Flow.Launcher"

Write-Host "Starting deployment process..."

# Stop Flow Launcher if it's running
Write-Host "Stopping Flow Launcher..."
$flowLauncher = Get-Process -Name $FlowLauncherProcess -ErrorAction SilentlyContinue
if ($flowLauncher) {
    Stop-Process -Name $FlowLauncherProcess -Force
    Start-Sleep -Seconds 2  # Wait for process to fully stop
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

# Build the project
Write-Host "Building project..."
dotnet build .\SonarrFlowLauncherPlugin\SonarrFlowLauncherPlugin.csproj --configuration Release

# Copy plugin files
Write-Host "Copying plugin files..."
Copy-Item ".\SonarrFlowLauncherPlugin\bin\Release\*" $PluginDirectory -Recurse -Force
Copy-Item ".\SonarrFlowLauncherPlugin\plugin.json" $PluginDirectory -Force
Copy-Item ".\SonarrFlowLauncherPlugin\plugin.yaml" $PluginDirectory -Force

Write-Host "Plugin deployed to: $PluginDirectory"

# Ask if user wants to start Flow Launcher
$startFlow = Read-Host "Would you like to start Flow Launcher now? (y/n)"
if ($startFlow -eq 'y') {
    Write-Host "Starting Flow Launcher..."
    Start-Process "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"
} else {
    Write-Host "Flow Launcher was not started. You can start it manually when ready."
}

Write-Host "Deployment complete!" 