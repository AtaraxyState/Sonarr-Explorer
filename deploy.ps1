# Read version from plugin.json
$pluginJsonPath = ".\SonarrFlowLauncherPlugin\plugin.json"
$pluginJson = Get-Content $pluginJsonPath | ConvertFrom-Json
$version = $pluginJson.Version

# Plugin configuration
$PluginDirectory = "$env:APPDATA\FlowLauncher\Plugins\Sonarr-Explorer-" + $version
$FlowLauncherProcess = "Flow.Launcher"

Write-Host "Starting deployment process..."
Write-Host "Plugin version: $version"

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

# Update the deployed plugin.yaml with API key from plugin.local.yaml
$localConfigPath = ".\SonarrFlowLauncherPlugin\plugin.local.yaml"
$deployedPluginYamlPath = "$PluginDirectory\plugin.yaml"

if (Test-Path $localConfigPath) {
    Write-Host "Reading API key from plugin.local.yaml..."
    
    # Read the API key from plugin.local.yaml
    $localConfigContent = Get-Content $localConfigPath -Raw
    $apiKey = ""
    
    # Simple YAML parsing to extract API key
    $lines = $localConfigContent -split "`n"
    foreach ($line in $lines) {
        if ($line.Trim() -match '^ApiKey:\s*["'']?([^"'']+)["'']?') {
            $apiKey = $matches[1].Trim()
            break
        }
    }
    
    if ($apiKey -ne "") {
        Write-Host "Updating deployed plugin.yaml with API key..."
        
        # Read the deployed plugin.yaml
        $deployedConfig = Get-Content $deployedPluginYamlPath -Raw
        
        # Replace the empty API key with the actual one
        $deployedConfig = $deployedConfig -replace 'ApiKey:\s*["'']?[^"'']*["'']?', "ApiKey: `"$apiKey`""
        
        # Write back to deployed plugin.yaml
        Set-Content -Path $deployedPluginYamlPath -Value $deployedConfig -Encoding UTF8
        
        Write-Host "API key successfully injected into deployed plugin.yaml"
    } else {
        Write-Host "Warning: Could not extract API key from plugin.local.yaml"
    }
} else {
    Write-Host "Warning: plugin.local.yaml not found. You'll need to configure API settings manually."
    Write-Host "Tip: Copy plugin.local.yaml.example to plugin.local.yaml and add your API key"
}

Write-Host "Plugin deployed to: $PluginDirectory"

# Start Flow Launcher
Write-Host "Starting Flow Launcher..."
Start-Process "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"

Write-Host "Deployment complete!" 