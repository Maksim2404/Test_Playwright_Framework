# Check if Allure CLI is installed
if (!(Get-Command allure -ErrorAction SilentlyContinue))
{
    Write-Host "Allure CLI not found. Please install Allure CLI and ensure it's in your PATH."
    exit 1
}

# Define the path to the allure-results directory relative to the project root
$projectRoot = $PSScriptRoot
$allureResultsPath = Join-Path $projectRoot "bin/Debug/net8.0/allure-results"

# Check if the allure-results folder exists
if (!(Test-Path $allureResultsPath))
{
    Write-Host "Allure results folder not found at $allureResultsPath. Please run tests to generate results."
    exit 1
}

Write-Host "Allure results folder found at $allureResultsPath."

# Generate the Allure report using the correct results path
Write-Host "Generating Allure report..."
$allureReportsPath = Join-Path $projectRoot "allure-reports"
allure generate $allureResultsPath --clean -o $allureReportsPath

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Error: Failed to generate Allure report."
    exit 1
}

Write-Host "Allure report generated at $allureReportsPath."

# Optional: kill existing Caddy processes to avoid port conflicts
$existing = Get-Process "caddy" -ErrorAction SilentlyContinue
if ($existing)
{
    Write-Host "Stopping existing Caddy process(es)..."
    $existing | Stop-Process -Force
}

# Start Caddy in the background so the script can continue
Write-Host "Starting Caddy to serve Allure report at https://localhost:8081"
Start-Process `
    -FilePath "caddy" `
    -ArgumentList "run --config `"$projectRoot\Caddyfile`"" `
    -WorkingDirectory $projectRoot `
    -NoNewWindow

# Brief delay to allow Caddy to start up
Start-Sleep -Seconds 5

# Open the default web browser to view the report
$reportUrl = "https://localhost:8081"
Write-Host "Opening browser to view Allure report at $reportUrl"
Start-Process $reportUrl

# End of script: Caddy is running in the background, serving the report.