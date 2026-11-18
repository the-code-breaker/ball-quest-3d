param([string]$Apk = 'Builds/Android/BallQuest3D-dev.apk')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$player = Join-Path $projectRoot '.tooling\UnityInstalled\Editor\Data\PlaybackEngines\AndroidPlayer'
$env:JAVA_HOME = Join-Path $player 'OpenJDK'
$apkPath = [IO.Path]::GetFullPath((Join-Path $projectRoot $Apk))
if (-not (Test-Path -LiteralPath $apkPath -PathType Leaf)) { throw 'The Unity APK build has not produced an APK yet.' }
$buildTools = Join-Path $player 'SDK\build-tools\36.0.0'
& (Join-Path $buildTools 'apksigner.bat') verify --verbose --print-certs $apkPath *> (Join-Path $projectRoot 'TestResults\apk-signature.txt')
if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed.' }
& (Join-Path $buildTools 'zipalign.exe') -c -P 16 4 $apkPath *> (Join-Path $projectRoot 'TestResults\apk-alignment.txt')
if ($LASTEXITCODE -ne 0) { throw 'APK alignment verification failed.' }
& (Join-Path $buildTools 'aapt.exe') dump badging $apkPath > (Join-Path $projectRoot 'TestResults\apk-info.txt')
if ($LASTEXITCODE -ne 0) { throw 'APK manifest verification failed.' }
$destination = Join-Path $projectRoot 'BallQuest3D-test.apk'
Copy-Item -LiteralPath $apkPath -Destination $destination -Force
$file = Get-Item -LiteralPath $destination
$hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
[pscustomobject]@{ File=$destination; Bytes=$file.Length; MiB=[math]::Round($file.Length/1MB,2); SHA256=$hash; SignatureVerified=$true; AlignmentVerified=$true } |
    ConvertTo-Json | Tee-Object -FilePath (Join-Path $projectRoot 'TestResults\apk-verification.json')
Select-String -LiteralPath (Join-Path $projectRoot 'TestResults\apk-info.txt') -Pattern '^package:','^sdkVersion:','^targetSdkVersion:','^application-label:','^native-code:','^launchable-activity:'
