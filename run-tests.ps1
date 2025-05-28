# Test runner for SonarrFlowLauncherPlugin
Write-Host "`n=== Running SonarrFlowLauncherPlugin Tests ===`n" -ForegroundColor Cyan

# Run tests directly
Write-Host ">>> Running tests...`n" -ForegroundColor Yellow
dotnet test --configuration Release --verbosity normal

# Script will exit automatically after tests complete 