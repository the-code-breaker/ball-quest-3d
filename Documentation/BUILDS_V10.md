# Build handoff — 0.10.0

**[BallQuest3D-Materials-v0.10.0.apk](../BallQuest3D-Materials-v0.10.0.apk)** — **42.85 MiB**, 44,935,115 bytes, version 0.10.0/code 10, ARM64, Android 8+ (API 26), target API 36. SHA-256: `9E08C9A0CFFFEB9EB903654E8E1C3314BD517C30178DEF0F9F343874300F1AFE`.

Package com.ballqueststudio.ballquest3d, portrait, ARM64/IL2CPP/OpenGLES3, optimized BuildOptions.None with a debug testing certificate. Both BallQuest and PhysicsLab scenes are packaged. Unity 6000.0.65f1 and Android SDK/NDK/OpenJDK are installed under .tooling/UnityInstalled.

```powershell
$unity = "$PWD\.tooling\UnityInstalled\Editor\Unity.exe"
.\Tools\UnityChecks.ps1 -Unity $unity -Check Generate
.\Tools\UnityChecks.ps1 -Unity $unity -Check EditMode
Copy-Item TestResults/EditMode.xml TestResults/EditMode-v10.xml
.\Tools\UnityChecks.ps1 -Unity $unity -Check PlayMode
.\Tools\UnityChecks.ps1 -Unity $unity -Check AndroidTest
.\Tools\VerifyApk.ps1 -Apk Builds/Android/BallQuest3D-test.apk
python Tools/VerifyMaterialsRelease.py
```

Only one Unity process can open this project. Preserve/validate raw reports before generic names are overwritten. The current release evidence consists of EditMode-v10.xml, CampaignPilot-v10.xml, CampaignTraversal-v10.xml, PlayMode-v10-regressions.xml and FinalVisuals-v10.xml. The verifier counts latest results per unique case and explicitly permits only the retained renderer-budget failure once its final rerun passes. A complete fresh 100-case PlayMode run may supersede those slices; do not substitute old-version results.

PrepareNaturalAudioV10.py reproduces 66 WAVs from the five credited original MP3s in Documentation/References/V10/AudioSources. It uses workspace-local soundfile/scipy libraries under .tooling/audio-python. Source recordings and source hashes/licenses remain outside the player package. Editor generation imports those WAVs and migrates the six existing profile GUIDs to audio revision 10. Runtime does not generate or download contact audio.

Campaign content version 24 applies the authored precision/hazard pass once while preserving level IDs/GUIDs. Existing stars/best times persist by ID and describe prior layouts. Save version 10 adds the two free ball skins while preserving saved controls and owned selections. Human input remains ball-only with automatic camera follow.

VerifyApk checks manifest/signature/ZIP alignment. VerifyMaterialsRelease additionally verifies native libraries, CRC, packaged levels/scenes, all 66 clips, new ball definitions, implementation symbols, excluded tests, real sequential UI evidence, physical routes and current test results. [Release record](../TestResults/materials-release.json) stores exact metadata. Physical-phone behavior remains unverified; store publication requires its own signing and distribution setup.
