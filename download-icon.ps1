$iconUrl = "https://raw.githubusercontent.com/Sonarr/Sonarr/develop/Logo/256.png"
$outputPath = ".\SonarrFlowLauncherPlugin\Images\icon.png"

# Create a web client
$webClient = New-Object System.Net.WebClient

try {
    Write-Host "Downloading Sonarr icon..."
    $webClient.DownloadFile($iconUrl, $outputPath)
    Write-Host "Icon downloaded successfully to $outputPath"
}
catch {
    Write-Host "Error downloading icon: $_"
}
finally {
    $webClient.Dispose()
} 