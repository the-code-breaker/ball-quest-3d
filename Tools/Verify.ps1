param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    New-Item -ItemType Directory -Path TestResults -Force | Out-Null
    & python Tools/seed_project.py
    if ($LASTEXITCODE -ne 0) { throw 'Asset metadata generation failed.' }
    & python Tools/verify_assets.py | Out-File -LiteralPath TestResults/assets.txt -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Asset validation failed. See TestResults/assets.txt.' }
    & $Dotnet run --project Tools/Verification/Verification.csproj -- $projectRoot | Tee-Object -FilePath TestResults/csharp.txt
    if ($LASTEXITCODE -ne 0) { throw 'C# verification failed.' }
    Write-Output 'Offline verification passed. Unity import, compilation, Play Mode and device tests are still required.'
}
finally { Pop-Location }
