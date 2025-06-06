param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$CopyToPath = ""
)

# Load default copy path from config file if it exists and no path was provided
$configFile = "update-version.config.ps1"
if ($CopyToPath -eq "" -and (Test-Path $configFile)) {
    Write-Host "Loading default copy path from $configFile..." -ForegroundColor Cyan
    try {
        . $configFile
        if ($DefaultCopyPath -and $DefaultCopyPath -ne "") {
            $CopyToPath = $DefaultCopyPath
            Write-Host "  Using default path: $DefaultCopyPath" -ForegroundColor Green
        }
    }
    catch {
        Write-Warning "Failed to load config file: $($_.Exception.Message)"
    }
}

# Validate version format (basic check for x.x.x format)
if (-not ($Version -match '^\d+\.\d+\.\d+$')) {
    Write-Error "Version must be in format x.x.x (e.g., 1.0.6)"
    exit 1
}

Write-Host "Updating version to $Version..." -ForegroundColor Green

# File paths
$pluginJsonPath = "SonarrFlowLauncherPlugin/plugin.json"
$submissionJsonPath = "Sonarr Explorer-D29D1AA0-3F6A-4F2E-8D0A-A5B7C9A5EFCF.json"
$readmePath = "README.md"
$pluginReadmePath = "PLUGIN_README.md"

# Check if all files exist
$files = @($pluginJsonPath, $submissionJsonPath, $readmePath, $pluginReadmePath)
foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        Write-Error "File not found: $file"
        exit 1
    }
}

# Validate custom path if provided
if ($CopyToPath -ne "") {
    if (-not (Test-Path $CopyToPath -PathType Container)) {
        Write-Error "Custom path does not exist or is not a directory: $CopyToPath"
        exit 1
    }
}

try {
    # Update plugin.json
    Write-Host "Updating $pluginJsonPath..." -ForegroundColor Yellow
    $pluginJson = Get-Content $pluginJsonPath -Raw | ConvertFrom-Json
    $pluginJson.Version = $Version
    $pluginJson | ConvertTo-Json -Depth 10 | Set-Content $pluginJsonPath -Encoding UTF8
    
    # Update submission JSON
    Write-Host "Updating $submissionJsonPath..." -ForegroundColor Yellow
    $submissionJson = Get-Content $submissionJsonPath -Raw | ConvertFrom-Json
    $submissionJson.Version = $Version
    $submissionJson.UrlDownload = "https://github.com/AtaraxyState/Sonarr-Explorer/releases/latest/download/Sonarr Explorer-$Version.zip"
    $submissionJson | ConvertTo-Json -Depth 10 | Set-Content $submissionJsonPath -Encoding UTF8
    
    # Update README.md version badge
    Write-Host "Updating $readmePath..." -ForegroundColor Yellow
    $readmeContent = Get-Content $readmePath -Raw
    $readmeContent = $readmeContent -replace 'version-\d+\.\d+\.\d+-blue', "version-$Version-blue"
    Set-Content $readmePath -Value $readmeContent -Encoding UTF8
    
    # Update PLUGIN_README.md
    Write-Host "Updating $pluginReadmePath..." -ForegroundColor Yellow
    $pluginReadmeContent = Get-Content $pluginReadmePath -Raw
    # Update version in download URL
    $pluginReadmeContent = $pluginReadmeContent -replace 'Sonarr Explorer-\d+\.\d+\.\d+\.zip', "Sonarr Explorer-$Version.zip"
    # Update version in tag reference
    $pluginReadmeContent = $pluginReadmeContent -replace 'git tag v\d+\.\d+\.\d+', "git tag v$Version"
    $pluginReadmeContent = $pluginReadmeContent -replace 'git push origin v\d+\.\d+\.\d+', "git push origin v$Version"
    Set-Content $pluginReadmePath -Value $pluginReadmeContent -Encoding UTF8
    
    # Copy submission JSON to custom path if specified
    if ($CopyToPath -ne "") {
        Write-Host "Copying $submissionJsonPath to $CopyToPath..." -ForegroundColor Yellow
        $destinationFile = Join-Path $CopyToPath (Split-Path $submissionJsonPath -Leaf)
        
        # Check if file already exists and warn about overwrite
        if (Test-Path $destinationFile) {
            Write-Host "  ⚠️  File exists, will be overwritten: $destinationFile" -ForegroundColor Yellow
        }
        
        Copy-Item $submissionJsonPath $destinationFile -Force
        Write-Host "  ✅ Copied to: $destinationFile" -ForegroundColor Green
    }
    
    Write-Host "`n✅ Version successfully updated to $Version in all files!" -ForegroundColor Green
    Write-Host "`nUpdated files:" -ForegroundColor Cyan
    Write-Host "  • $pluginJsonPath" -ForegroundColor White
    Write-Host "  • $submissionJsonPath" -ForegroundColor White
    Write-Host "  • $readmePath" -ForegroundColor White
    Write-Host "  • $pluginReadmePath" -ForegroundColor White
    
    if ($CopyToPath -ne "") {
        Write-Host "`nCopied submission file:" -ForegroundColor Cyan
        Write-Host "  • $destinationFile" -ForegroundColor White
    }
    
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Review the changes" -ForegroundColor White
    Write-Host "  2. Commit the version bump: git add . && git commit -m `"Bump version to $Version`"" -ForegroundColor White
    Write-Host "  3. Create and push tag: git tag v$Version && git push origin v$Version" -ForegroundColor White
    
    # Show config file info if it doesn't exist
    if (-not (Test-Path $configFile)) {
        Write-Host "`n💡 Tip: Create $configFile to set a default copy path:" -ForegroundColor Cyan
        Write-Host "  Example content: `$DefaultCopyPath = 'C:\Your\Custom\Path'" -ForegroundColor White
    }
}
catch {
    Write-Error "Failed to update version: $($_.Exception.Message)"
    exit 1
} 