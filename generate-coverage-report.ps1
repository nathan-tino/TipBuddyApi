# Run tests and generate coverage file
Write-Host "Running tests and collecting coverage..."
dotnet test TipBuddyApi.Tests --collect:"XPlat Code Coverage"

# Finds the latest coverage.cobertura.xml and runs ReportGenerator
# Dynamically find the latest installed ReportGenerator version
$reportGeneratorBase = "$env:USERPROFILE\.nuget\packages\reportgenerator"
$latestVersionDir = Get-ChildItem -Path $reportGeneratorBase -Directory | `
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+.*$' } | `
    Sort-Object { [Version]$_.Name } -Descending | `
    Select-Object -First 1
if (-not $latestVersionDir) {
    Write-Host "ReportGenerator is not installed in $reportGeneratorBase. Please install the NuGet package."
    exit 1
}
$reportGeneratorPath = Join-Path $latestVersionDir.FullName "tools\net9.0\ReportGenerator.dll"
$testResultsDir = "TipBuddyApi.Tests\TestResults"
$coverageFile = Get-ChildItem -Path $testResultsDir -Recurse -Filter coverage.cobertura.xml |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $coverageFile) {
    Write-Host "No coverage.cobertura.xml file found in $testResultsDir. Run tests with coverage first."
    exit 1
}

$reportDir = "coveragereport"

Write-Host "Generating coverage report from: $($coverageFile.FullName)"
dotnet $reportGeneratorPath -reports:$coverageFile.FullName -targetdir:$reportDir
Write-Host "Coverage report generated in $reportDir"

# Open the report file in Chrome using a relative path
$reportIndexPath = Join-Path $PSScriptRoot "$reportDir\index.html"
$fileUri = (New-Object System.Uri($reportIndexPath)).AbsoluteUri
Write-Host "Opening coverage report in Chrome..."
Start-Process "chrome" $fileUri