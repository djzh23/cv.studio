Write-Host "Stopping local CvStudio API/Blazor processes..." -ForegroundColor Cyan
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
    Where-Object {
        $_.CommandLine -like "*CvStudio.Api.csproj*" -or
        $_.CommandLine -like "*CvStudio.Blazor.csproj*"
    } |
    ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }

Write-Host "Stopping PostgreSQL container..." -ForegroundColor Cyan
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Push-Location $repoRoot
try {
    docker compose down
}
finally {
    Pop-Location
}

Write-Host "Done." -ForegroundColor Green
