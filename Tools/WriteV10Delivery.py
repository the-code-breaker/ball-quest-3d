"""Finalize documentation only from a verified v0.10 release record."""
from pathlib import Path
import json,shutil,re
ROOT=Path(__file__).resolve().parent.parent
record=json.loads((ROOT/'TestResults/materials-release.json').read_text(encoding='utf-8'))
assert record['version']=='0.10.0' and record['signatureVerified'] and record['distinctUnityTestsPassed']==137
artifact=record['artifact'];size=record['MiB'];digest=record['SHA256']
meta=f'**[{artifact}](../{artifact})** — **{size} MiB**, {record["bytes"]:,} bytes, version 0.10.0/code 10, ARM64, Android 8+ (API 26), target API 36. SHA-256: `{digest}`.'
p=ROOT/'Documentation/DELIVERY_V10.md';text=p.read_text(encoding='utf-8');body=re.sub(r'\n{3,}','\n\n',text[text.index('## Requested fixes'):])
text='# Material and challenge update — 0.10.0\n\n'+meta+' Android build, signature, ZIP/native 16 KiB alignment, CRC, version, scenes, all 66 recorded clips, four new ball definitions and runtime/test-code checks passed. The generic BallQuest3D-test.apk is identical. [Verified release record](../TestResults/materials-release.json).\n\n'+body
text=text.replace('## Evidence recorded so far','## Executed evidence')
p.write_text(text,encoding='utf-8')
qa=ROOT/'Documentation/QA.md';old=ROOT/'Documentation/QA_V9.md'
if not old.exists():shutil.copy2(qa,old)
qa.write_text('''# Quality assurance — material/challenge update 0.10.0

All 137 distinct current Unity cases have passing latest results. All 55 campaign courses and 15 signature-adventure route variants completed force-driven physical validation with zero falls. The optimized APK passed packaging checks. [Delivery](DELIVERY_V10.md), [verified release record](../TestResults/materials-release.json).

| Evidence | Result/scope |
| --- | --- |
| [EditMode-v10.xml](../TestResults/EditMode-v10.xml) | 37/37; includes sequential chapters, exact 66 recorded clip mappings/data, material balls/save migration, route geometry and original core tests |
| [CampaignPilot-v10.xml](../TestResults/CampaignPilot-v10.xml) | 15/15; six representative physical courses, audio, native family/models, boosters and actual UI navigation |
| [CampaignTraversal-v10.xml](../TestResults/CampaignTraversal-v10.xml) | All 55 current harder courses, zero falls; 704.132 wall seconds |
| [PlayMode-v10-regressions.xml](../TestResults/PlayMode-v10-regressions.xml) | Complete 98-case run: 97 passed, one obstacle-renderer budget failure retained verbatim |
| [FinalVisuals-v10.xml](../TestResults/FinalVisuals-v10.xml) | 14/14 after fixing object-local batching; supersedes that one failure without adding distinct tests |
| [Physical route summary](../TestResults/campaign-physical-validation-v10.json), [55-row CSV](../TestResults/production-traversal-v10.csv), [15 adventure variants](../TestResults/expedition-traversal-v10.csv) | Real force/brake navigation through checkpoints and finish triggers, no teleport/velocity writes or disabled hazards |
| [Chapter buttons](../TestResults/challenge-navigation-v10.json), [native chapter view](../TestResults/Previews/V10SequentialChapter.png) | Every displayed challenge ID 1–55 appears exactly once, with proper page reset |
| [Difficulty comparison](../TestResults/challenge-difficulty-v10.json) | Exposed main segments 519 → 953, increased exposure in 50 courses; 23 each piston/sweep/drum; not a human difficulty measurement |
| [Recorded audio metrics](../TestResults/natural-contact-v10.json), [loop seams](../TestResults/natural-loop-seams-v10.json), [credits](References/V10/AUDIO_CREDITS.md) | Five CC0 sources, 66 material/pair clips; mathematical/import checks, not a human audition |
| [Material/obstacle model review](../TestResults/material-models-v10.json) | Four native ball styles, different impacts on one floor, moving machinery models and visible gate lifting |
| [Camera output](../TestResults/centered-camera-v10.json), [gestures](../TestResults/ball-only-gestures-v10.json), [swipe-only](../TestResults/swipe-only-v10.json) | Ball remains centered; touch sends no camera commands; stationary touch adds no drive |
| [Performance](../TestResults/presentation-performance-v10.json), [isolated animation](../TestResults/reward-animation-allocation-v10.json) | Eight-ball Editor mean/median/p95 16.692/16.664/17.455 ms, 301 renderers, 195.47 MiB, one Gen0 in 240 frames; isolated 600-call animation allocates zero managed bytes |

The one failed native renderer assertion exposed a real inefficiency: local meshes were split across spatial chunk boundaries. The final fix uses one material batch per small moving object; scenery retains spatial culling. Raw failed XML/logs are preserved, and latest results per unique test determine the aggregate. Focused runs and route counts are not added as extra Unity cases.

The route/core-physics snapshot matches the validated campaign. Later refinements concern recorded-loop continuity, visual shutter lifting, scenery and object-local render batching; focused regression checks cover those changes. Native overview/inspection poses show actual Unity geometry and are separate from physical completion evidence. Original offline checks also passed: 8,219 asset checks and 144 domain/syntax checks; they are not Unity test cases.

No physical phone was attached. Phone touch comfort, speaker output, subjective sound/difficulty, hardware shimmer and sustained thermal/FPS behavior remain unverified. Earlier versioned reports/APKs are historical; [v9 QA](QA_V9.md) is retained separately.
''',encoding='utf-8')
(ROOT/'README.md').write_text(f'''# Ball Quest 3D — Materials & Challenges 0.10.0

**[Download the testing APK]({artifact})** — **{size} MiB**, Android 8+, ARM64, optimized nondevelopment build with a testing certificate. [Release record](TestResults/materials-release.json) · [Changes and evidence](Documentation/DELIVERY_V10.md).

- Recorded metal/rubber rolling and material/surface impact combinations replace the noisy synthetic contact bed; quieter slip and clean loop joins reduce hiss/clicks.
- Brushed Steel, Carbon Rubber, Copper Core and Turned Walnut have matching sound identities. Steel and Rubber are free additions.
- Difficulty increases earlier through tighter exposed roads and 69 new moving pistons, sweep arms and drums. Existing machinery is remodeled and timed shutters visibly lift open.
- Eleven distinct scenery recipes replace repetitive walls: lagoon, desert, orbital, alpine, garden, volcanic, clockwork, skyline, ruins, canyon and crystal environments.
- Coins have deeper beveled bodies, raised stars and faceted rims. Boosters have ribbed dark trays, champagne plates and recessed animated lights.
- Challenge chapters display **1–14, 15–28, 29–42, 43–55**. Pagination resets correctly; finishing 55 returns to challenges. Existing progress, currency and control preferences persist.

All **137 distinct current Unity checks** passed after a rendering-budget fix. All **55 campaign courses and 15 adventure variants completed with zero falls**. Signature, manifest, CRC, 16 KiB alignment and packaged-content checks passed. The full 98-case run's single budget failure is preserved and superseded by the final 14/14 rerun; [QA](Documentation/QA.md) explains counting and evidence.

The generic BallQuest3D-test.apk is identical. No phone was connected: physical-device feel, sound preference and sustained Android performance remain unverified. Earlier APKs and reports are historical.

[Controls](Documentation/MANUAL_CONTROLS.md) · [Recorded audio and audition](Documentation/CONTACT_AUDIO_V10.md) · [Online art references](Documentation/References/V10/VISUAL_REFERENCES.md) · [Build handoff](Documentation/BUILDS.md)
''',encoding='utf-8')
(ROOT/'Documentation/BUILDS.md').write_text(f'''# Build handoff — 0.10.0

{meta}

Package com.ballqueststudio.ballquest3d, portrait, ARM64/IL2CPP/OpenGLES3, optimized BuildOptions.None with a debug testing certificate. Both BallQuest and PhysicsLab scenes are packaged. Unity 6000.0.65f1 and Android SDK/NDK/OpenJDK are installed under .tooling/UnityInstalled.

```powershell
$unity = "$PWD\\.tooling\\UnityInstalled\\Editor\\Unity.exe"
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check Generate
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check EditMode
Copy-Item TestResults/EditMode.xml TestResults/EditMode-v10.xml
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check PlayMode
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check AndroidTest
.\\Tools\\VerifyApk.ps1 -Apk Builds/Android/BallQuest3D-test.apk
python Tools/VerifyMaterialsRelease.py
```

Only one Unity process can open this project. Preserve/validate raw reports before generic names are overwritten. The current release evidence consists of EditMode-v10.xml, CampaignPilot-v10.xml, CampaignTraversal-v10.xml, PlayMode-v10-regressions.xml and FinalVisuals-v10.xml. The verifier counts latest results per unique case and explicitly permits only the retained renderer-budget failure once its final rerun passes. A complete fresh 100-case PlayMode run may supersede those slices; do not substitute old-version results.

PrepareNaturalAudioV10.py reproduces 66 WAVs from the five credited original MP3s in Documentation/References/V10/AudioSources. It uses workspace-local soundfile/scipy libraries under .tooling/audio-python. Source recordings and source hashes/licenses remain outside the player package. Editor generation imports those WAVs and migrates the six existing profile GUIDs to audio revision 10. Runtime does not generate or download contact audio.

Campaign content version 24 applies the authored precision/hazard pass once while preserving level IDs/GUIDs. Existing stars/best times persist by ID and describe prior layouts. Save version 10 adds the two free ball skins while preserving saved controls and owned selections. Human input remains ball-only with automatic camera follow.

VerifyApk checks manifest/signature/ZIP alignment. VerifyMaterialsRelease additionally verifies native libraries, CRC, packaged levels/scenes, all 66 clips, new ball definitions, implementation symbols, excluded tests, real sequential UI evidence, physical routes and current test results. [Release record](../TestResults/materials-release.json) stores exact metadata. Physical-phone behavior remains unverified; store publication requires its own signing and distribution setup.
''',encoding='utf-8')
controls=ROOT/'Documentation/MANUAL_CONTROLS.md';text=controls.read_text(encoding='utf-8');text=text.replace('This describes v0.9.0 progressive swipe controls and automatic direction-follow camera. [Plan](DEVELOPMENT_PLAN_V9.md) and [delivery evidence](DELIVERY_V9.md).','The v0.10.0 update retains these progressive swipe controls and automatic direction-follow camera. [Current plan](DEVELOPMENT_PLAN_V10.md) and [delivery evidence](DELIVERY_V10.md).')
controls.write_text(text,encoding='utf-8')
contact=ROOT/'Documentation/CONTACT_AUDIO.md';contact.write_text('# Material contact audio\n\nThe current implementation is [recorded material contact v10](CONTACT_AUDIO_V10.md). It replaces the earlier synthetic friction bed with credited CC0 recordings, 66 body/surface clips and quieter slip. The existing collision-driven audio/VFX contract and physics independence remain. [v9 contact audio](CONTACT_AUDIO_V9.md) is historical.\n',encoding='utf-8')
print('Finalized v0.10 README, build/QA/control/audio handoff and delivery metadata from the verified release record.')
