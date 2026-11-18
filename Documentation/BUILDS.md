# Build handoff — 0.11.0

[Testing APK](../BallQuest3D-ThousandWorlds-v0.11.0.apk) — 48.04 MiB, 50,374,636 bytes, ARM64 IL2CPP, Android API 26 minimum / 36 target, optimized nondevelopment testing build. Package `com.ballqueststudio.ballquest3d`, version `0.11.0` / code `11`.

SHA-256: `847900259AE6D65EC2BB89107FE40962E23DD6A0823FC05FFE527BDA75254229`. The generic `BallQuest3D-test.apk` is identical. Install over the prior testing APK to retain existing progress; the existing signing certificate is preserved.

```powershell
$unity = "$PWD\.tooling\UnityInstalled\Editor\Unity.exe"
.\Tools\UnityChecks.ps1 -Unity $unity -Check Generate
.\Tools\UnityChecks.ps1 -Unity $unity -Check EditMode
.\Tools\UnityChecks.ps1 -Unity $unity -Check PlayMode
.\Tools\UnityChecks.ps1 -Unity $unity -Check AndroidTest
.\Tools\VerifyApk.ps1 -Apk Builds/Android/BallQuest3D-test.apk
python Tools/VerifyGrandRelease.py
```

Only one Unity process can open this project. Archive current raw reports before generic files are overwritten. The release verifier uses the exact report paths in `Tools/VerifyGrandRelease.py`; a full fresh passing run may replace those slices after its scope and distinct-test count are checked. Do not substitute prior-version results. The physical campaign sample contains 49 courses, not all 1000.

Both BallQuest and PhysicsLab scenes are packaged. Native test drivers and Editor generators are excluded. [Release evidence](../TestResults/grand-release-v11.json) · [QA](QA.md) · [Changes](DELIVERY_V11.md). No physical phone test or iOS build was performed.
