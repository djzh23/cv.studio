param(
    [string]$Passcode = ""
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$dbPort = 5433
 
# Ensure no stale local instances keep DLLs locked.
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
    Where-Object {
        $_.CommandLine -like "*CvStudio.Api.csproj*" -or
        $_.CommandLine -like "*CvStudio.Blazor.csproj*"
    } |
    ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }

if ([string]::IsNullOrWhiteSpace($Passcode)) {
    $Passcode = Read-Host "Gate Passcode (nur fuer diese lokale Session)"
}

if ([string]::IsNullOrWhiteSpace($Passcode)) {
    throw "Passcode darf nicht leer sein."
}

Write-Host "Starting PostgreSQL (docker compose)..." -ForegroundColor Cyan
Push-Location $repoRoot
try {
    docker compose up -d postgres
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up failed. Ensure Docker Desktop is running."
    }
}
finally {
    Pop-Location
}

Write-Host "Waiting for PostgreSQL on localhost:$dbPort ..." -ForegroundColor Cyan
$maxAttempts = 30
$attempt = 0
while ($attempt -lt $maxAttempts) {
    $attempt++
    $probe = Test-NetConnection -ComputerName "localhost" -Port $dbPort -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($probe) {
        break
    }
    Start-Sleep -Seconds 1
}

if (-not $probe) {
    throw "PostgreSQL did not become ready on localhost:$dbPort. Run 'docker compose ps' and 'docker compose logs postgres'."
}

$apiCmd = @"
Set-Location '$repoRoot'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = 'http://localhost:5189'
`$env:ConnectionStrings__Postgres = 'Host=localhost;Port=5433;Database=cvstudio;Username=postgres;Password=postgres'
dotnet run --no-launch-profile --project src/CvStudio.Api/CvStudio.Api.csproj
"@

$blazorCmd = @"
Set-Location '$repoRoot'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = 'http://localhost:5047'
`$env:ApiBaseUrl = 'http://localhost:5189/'
`$env:Access__Passcode = '$Passcode'
dotnet run --no-launch-profile --project src/CvStudio.Blazor/CvStudio.Blazor.csproj
"@

Write-Host "Starting API on http://localhost:5189 ..." -ForegroundColor Green
Start-Process powershell -ArgumentList @("-NoExit", "-Command", $apiCmd)

Write-Host "Waiting for API health endpoint..." -ForegroundColor Cyan
$apiReady = $false
for ($i = 1; $i -le 45; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5189/health" -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            break
        }
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $apiReady) {
    throw "API did not become healthy on http://localhost:5189/health. Check API window logs."
}

Write-Host "Starting Blazor on http://localhost:5047 ..." -ForegroundColor Green
Start-Process powershell -ArgumentList @("-NoExit", "-Command", $blazorCmd)

Write-Host "Ready. Open http://localhost:5047 and use your passcode." -ForegroundColor Yellow
