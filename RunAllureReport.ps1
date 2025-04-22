# Check if Allure CLI is installed
if (!(Get-Command allure -ErrorAction SilentlyContinue))
{
    Write-Host "Allure CLI not found. Please install Allure CLI and ensure it's in your PATH."
    exit 1
}

# Define the path to the allure-results directory relative to the project root
$allureResultsPath = "bin/Debug/net8.0/allure-results"

# Check if the allure-results folder exists
if (!(Test-Path $allureResultsPath))
{
    Write-Host "Allure results folder not found at $allureResultsPath. Please run tests to generate results."
    exit 1
}

Write-Host "Allure results folder found at $allureResultsPath."

# Generate the Allure report using the correct results path
Write-Host "Generating Allure report..."
allure generate $allureResultsPath --clean -o allure-reports

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Error: Failed to generate Allure report."
    exit 1
}

# Start Caddy in the background so the script can continue
Write-Host "Starting Caddy to serve Allure report at https://localhost"
Start-Process caddy -ArgumentList "run" -NoNewWindow

# Brief delay to allow Caddy to start up (adjust if necessary)
Start-Sleep -Seconds 5

# Open the default web browser to view the report
Write-Host "Opening browser to view Allure report at https://localhost"
Start-Process "https://localhost"

# End of script: Caddy is running in the background, serving the report.