param (
    [string]$ApiKey
)

if (-not $ApiKey) {
    Write-Host "Usage: .\run-tests.ps1 -ApiKey YOUR_TMDB_API_KEY" -ForegroundColor Yellow
    exit 1
}

$env:TMDB_API_KEY = $ApiKey

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore

Write-Host "Running tests..." -ForegroundColor Cyan
dotnet test --logger "trx;LogFileName=test_results.trx" --results-directory ./TestResults

Write-Host "Done. Test results in ./TestResults" -ForegroundColor Green
