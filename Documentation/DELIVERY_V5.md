# Ball Quest 3D 0.5.0 - manual 360 delivery record

28 October 2026. This revision rebuilds manual controls, camera and contact audio in the existing Unity project, then adds five featured adventures while preserving the 55-course campaign and five race tracks. Gameplay validation is complete in the available Unity Editor environment. All 68 distinct latest test results pass, all 55 campaign courses and 15 expedition variants completed with zero falls, and the Android testing APK has passed signature, alignment, integrity and packaged-content verification. Phone/iOS validation remains separate.

## Release evidence

| Item | Status |
| --- | --- |
| Verified APK | [BallQuest3D-Manual360-v0.5.0.apk](../BallQuest3D-Manual360-v0.5.0.apk) |
| Configuration | 0.5.0/code 5; com.ballqueststudio.ballquest3d; ARM64/IL2CPP/OpenGLES3; portrait; minimum API 26, target API 36; debug development build |
| APK size | 52,324,302 bytes / 49.90 MiB; generic BallQuest3D-test.apk is identical |
| SHA-256 | A8BAC58D8542A465ED6AA94983E54B883814844DB5582148092BB043A7C8E61C |
| Verification | Signature, ZIP CRC and 16 KiB ZIP/native ELF LOAD alignment passed; both native libraries are AArch64 |
| Packaged content | 60 adventure titles, five race titles, compiled controls/gravity/audio metadata and main/PhysicsLab scenes verified; automated test-driving classes are excluded from the APK |
| Machine-readable record | [manual360-release.json](../TestResults/manual360-release.json) |
| EditMode | 19 passed in TestResults/EditMode.xml |
| Actual pointer/four-AI slice | 3 passed in TestResults/ManualPointerAndRace-v5.xml |
| Dedicated manual/lab/audio checks | 8 core physics/camera, 2 lab and 4 audio cases passed across targeted runs |
| Full PlayMode run | 49 executed: 46 passed, 3 failed; TestResults/PlayMode-v5-full.xml/log retained |
| Campaign traversal | 55/55 completed, all ordered checkpoints, zero falls under the revised controller |
| Final correction run | 7 passed in TestResults/FinalChecks-v5.xml; supersedes all three earlier full-run failures |
| Distinct latest results | 68 passed: 19 EditMode + 49 PlayMode; reruns are deduplicated |
| Final menu review | Existing UI flow passed again in MenuReview-v5.xml; final preview assets and settled menu render reviewed. This rerun adds no distinct test. |
| Expeditions | 15/15 variants, zero falls; 200 wall/514 ceiling contact steps; hidden gem and token collected |
| Physical Android / iOS | No device listed in current ADB check; Android/Windows modules installed, no iOS module; device/iOS validation not verified |

The larger Android lab scene is packaged in split scene files and registered correctly; verification accepts the actual split layout. Do not reuse the old APK's size/hash or v0.4.0 totals as current evidence. [DELIVERY_V4.md](DELIVERY_V4.md) remains historical.

## Root causes and corrected controls

The [baseline audit](MANUAL_360_AUDIT.md) found an asymmetric control architecture. Upward gestures queued forward energy, downward gestures only braked, and horizontal gestures requested lateral velocity relative to a changing authored path. Neutral lateral correction suppressed free motion. CoursePathFollower changed that frame and the camera aligned to it, producing the guided feel. Zero-input permanent forward acceleration was not the principal source defect.

Default RelativeDrag now measures symmetric camera-relative displacement from a floating anchor. A finger stationary for 0.075 seconds returns toward neutral; release/reposition clears stale input. Optional GestureVelocity measures normalized pointer speed. Both normalize by the smaller screen dimension, clamp vector magnitude and buffer render samples for physics. UI-started touches retain UI ownership; two fingers, the rightmost 16% or right mouse orbit without drive. The same manual controls operate in adventure and races.

BallController applies force and grounded torque to a dynamic sphere, preserving linear/angular momentum, friction, gravity and collision response. Bounded slip correction keeps rolling coherent without assigning angular velocity. Opposite input brakes through zero and continues backward. Neutral input has no minimum-speed or centering servo; grounded rolling/viscous resistance dissipates motion gradually. Ordinary movement does not set body poses/velocities. Explicit recovery clears motion/input/gravity at a safe checkpoint.

Standard defaults are 18 force units, 14 powered speed in every direction, 22 general ceiling, 25 authored boost target, 0.65 rolling plus 0.65 speed-dependent resistance, 0.4 torque ratio, 5 slip-correction gain, 0.035-second command response/release and 60 Hz physics. Inspector assets expose tuning. Legacy reverse/lateral fields remain serialized but impose no asymmetric limits. [MANUAL_CONTROLS.md](MANUAL_CONTROLS.md) gives the exact model/APIs.

StableOrbit follows position independently of sphere spin and route direction; reversing does not force a 180-degree flip. Manual look changes camera/control heading. Optional AdaptiveChase waits for sustained travel, hysteresis and manual grace. The camera and support frame parallel-transport through gravity changes; collision probes and optional impact/landing/FOV feedback remain.

## Audio and diagnostic scene

Six materials (Metal, Wood, Glass, Rubber, Stone and Energy) and eight surfaces (Wood, Metal, Stone, Ice, Sand, Glass, Grass and Boost) drive real contact audio. Rolling follows support-relative tangential speed, sliding follows slip, and impacts follow collision impulse. Two rolling voices crossfade; one sliding and four pooled impact voices bound playback. Rest/airborne silence grounded loops; mute/pause/deactivation stop active transients. Audio changes no physics.

The 18 WAVs are explicitly temporary original procedural samples. Offline checks show distinct, non-silent PCM clips with zero clipped samples; final recorded Foley and human audition are not claimed. [CONTACT_AUDIO.md](CONTACT_AUDIO.md) covers provenance/replacement; [signal diagnostics](../TestResults/contact-audio-samples.json) records measurements.

The standalone PhysicsLab has twelve physical stations: flat, uphill, downhill, narrow bridge, circular platform, curve, ice, sand, moving support, collision, jump gap and reverse. It shows actual control, velocity, spin, contact, rolling ratio and coast distance and exports CSV. Open Settings > Controls & camera > Physics Lab; Back to Game returns to the main scene.

## Adventures and obstacles

All 15 authored route cases completed through the real shared force controller with zero falls. Gravity Lab established 200 grounded wall contact steps and 514 ceiling steps; hidden-reward traversal collected its gem and token. [Expedition summary](../TestResults/expedition-validation-summary-v5.json).

| ID / expedition | Implemented geometry and route cases |
| --- | --- |
| 56 Sky Maze | Narrow risk/reward, mechanical and scenic reconnecting lanes; west/east exits, hidden reversal and dead-end return: eight cases. |
| 57 Clockwork Chaos | Physical timed drawbridge, rotating brass support disc and pass-through wheel, with outside bypass: two cases. |
| 58 Gravity Lab | Bounded radial acceleration on a 180-degree floor-wall-ceiling road and inverted return: one case. |
| 59 Himalayan Pass | Snow/rock environment, suspension beam, icy switchback, tunnel and longer cliff path: two cases. |
| 60 Volcano Islands | Ramp jump, lower catch island, warning/collapsing bridge, moving-stone detour and rotor over lava: two cases. |

The existing 55 courses retain their difficulty bands and progression IDs. They include hills, narrow bridges, banked spirals, natural drops/jumps, branches, hammers/pendulums, doors, moving/elevator/circular supports, falling decks, rotors/blades, rolling hazards, push/crush walls, conveyors and boosters. [PRODUCTION_COURSES.md](PRODUCTION_COURSES.md) lists those recipes. The full v0.5.0 run completed all 55 courses through ordinary shared-controller commands with zero falls; see [current campaign summary](../TestResults/campaign-validation-summary-v5.json).

Sprint, Obstacle, Elimination, target-time Time Trial and three-round Tournament remain. AI converts planning into mass-aware force feedback with slope/resistance compensation. Its former alternate-route-only queued impulse was removed; human/AI powered limits match. Ranking still uses ordered 3D progress and validated finish time. Independent expedition reward high-water records do not skip campaign unlocks.

## File inventory

Paths are relative to Assets/_BallQuest unless noted. Matching metadata/generated assets are included. This workspace has no Git history; the table is an implementation inventory.

| Files / group | Change |
| --- | --- |
| Scripts/Player/BallInput.cs, BallController.cs, BallProfile.cs, BallControlCommand.cs, BallGroundDetector.cs | Buffered symmetric input, force/torque/resistance, gravity-relative support and tuning |
| Scripts/Player/SurfaceContactResponse.cs | Physical surface response independent of sound |
| Scripts/Camera/QuestCamera.cs | Stable/manual orbit and delayed adaptive chase |
| Scripts/Audio/QuestAudio.cs; new BallAudioProfile.cs, BallSurfaceTag.cs, BallContactAudio.cs, TemporaryBallAudio.cs | Contact audio and temporary synthesis |
| Scripts/Editor/ManualGameplayUpgradeGenerator.cs, BallAudioAssetGenerator.cs, PhysicsLabGenerator.cs, SignatureAdventureGenerator.cs, ManualRaceCourseGenerator.cs | One-time tuning, profiles/samples, lab, expedition v3 seed and isolated Race 4 fork correction |
| Scripts/Editor/ProjectGenerator.cs, BuildAutomation.cs | Version/generation and main/lab scene lists |
| Scripts/Levels/GameCatalog.cs, LevelDefinition.cs, LevelBuilder.cs, CoursePathFollower.cs | Featured data, alternate finishes, surfaces and progress-only path metadata |
| Scripts/Levels/PhysicsLabTelemetry.cs | Actual physical instrumentation/reset/CSV |
| Scripts/Levels/SignatureAdventurePaths.cs, SignatureCourseRuntime.cs, SignatureGravityField.cs, SignatureRuntimeResources.cs, ClockworkMotor.cs | Branches, real machinery/gravity, themes and resource lifetime |
| Scripts/Core/GameBootstrap.cs, GameSession.cs | Featured loading, lab navigation, saved modes and expedition completion |
| Scripts/Progression/SaveData.cs, ProgressionRules.cs | v5 migration, independent rewards and settings |
| Scripts/UI/QuestUI.cs, QuestUI.Racing.cs, QuestUI.Manual.cs | Featured Play, settings/guidance and retained racing interface |
| Scripts/Racing/RaceDriver.cs | Shared force navigation; removed AI-only energy |
| Tests/PlayMode/ManualPhysicsV5Tests.cs, ManualPointerSafetyTests.cs, MaterialAudioTests.cs, PhysicsLabTests.cs, ExpeditionTraversalTests.cs, SignatureGeometryTests.cs | Core, actual touch/audio/lab and route acceptance |
| Tests/PlayMode/GameplayTests.cs, BallPhysicsV4Tests.cs, BootstrapFlowTests.cs, SkylineTraversalTests.cs | Updated manual semantics, UI/settings and command-only legacy traversal |
| Tests/EditMode/ExpeditionProgressTests.cs | Rewards, migration and real save/reload |
| ScriptableObjects/Expeditions/*.asset; GameCatalog.asset; Balls/*.asset; Resources/BallAudio/*.asset; Audio/SFX/TemporaryContacts/*.wav; Scenes/Gameplay/PhysicsLab.unity | Content/tuning, six profiles, 18 clips and diagnostic scene |
| Resources/ExpeditionPreviews/56.png-60.png | Five native-rendered featured-menu previews |
| Tools/VerifyManualRelease.py; Tools verification; ProjectSettings; README/Documentation/TestResults | Versioned artifact/report verification, scene/version settings, checks, handoff and evidence |

## Executed checks and failures

The full [PlayMode-v5-full.xml](../TestResults/PlayMode-v5-full.xml) retains its original 49-case result: 46 passed and three failed. After authoring fixes, [FinalChecks-v5.xml](../TestResults/FinalChecks-v5.xml) passed all seven targeted cases: five RacingIntegration methods, all expedition variants and signature edge crossings. The final results supersede all three failures and recheck four existing cases. With 19 EditMode passes, the latest-result union is 68 distinct passing tests (19 EditMode + 49 PlayMode). The seven reruns are not additional unique tests. Both original failures and successful reruns remain transparent.

Coverage includes no-input rest/path independence, symmetric directions, reverse through zero, coast-stop, physical rolling ratio, circular control, 30/60/90/120 Hz sampling, stationary neutral, actual UI-touch ownership/reanchoring, camera-only gestures, wall grounding/reset, stable/manual camera, actual contact audio and the generated lab. The four-AI slice confirms simultaneous physical release and validated real finishes under the new controller.

Early checks corrected edit-mode use of runtime Awake properties, scene restoration unloading a catalog before SetDirty, a missing lab build-scene entry, a thin disc incorrectly using the primitive's CapsuleCollider, UI fixtures running before layout or sufficient physics duration, and ongoing contacts refreshing an audio cooldown fixture. The changes fix generator/collider behavior or test setup; they do not suppress gameplay failures. Any route failures must be corrected and rerun before completion is claimed.

Resolved full-run gameplay failures:

1. The Himalayan cliff branch stalled where its tunnel rejoin met a raised road edge. Expedition generator version 3 completes the grade before merging, and the full fifteen-route rerun now passes with zero falls.
2. AllFiveTracksAndThreeFormatsUsePhysicalCompetitors initially reported a Race 4 DNF.
3. RaceMenuAlternativeAndEightBallEditorMeasurement initially failed the same shortcut acceptance. ManualRaceCourseGenerator applies isolated manualRaceVersion1 authoring: the fork/merge choices share a contact plane and crossing guardrails are removed. The elevated spiral and 55-course campaign are preserved. Both previously failing integrations now pass with ordinary shared physics commands; no AI-only energy, speed multiplier or body teleportation was introduced.

The final seven-case run demonstrates the corrected routes and four explicit edge crossings. It does not erase the original XML failures or imply that physical-device validation has occurred.

## Actual measurements and limits

[The 47-sample lab trace](../TestResults/physics-lab-v5.csv) records sampled peak contact speed 9.1700. Its last 4.8667-second sample has speed 0.1579, rolling ratio 1.0002 and coast distance 12.6435 metres. Those are specific Editor observations; the last sample does not claim zero velocity. The lab test separately asserts the subsequent stop threshold.

The final passing v0.5.0 [eight-ball sample](../TestResults/editor-race-measurement.json) used Windows 11 / NVIDIA GTX 1650 Ti, an active native camera, a 60 FPS target and 180 frames. The Race 4 shortcut acceptance passed in the same final run.

| Current sample metric | Value |
| --- | --- |
| Mean / median / p95 wall frame | 16.6755 / 16.6779 / 17.0898 ms |
| Unity allocated memory | 197.86 MiB |
| Course build | 132.2719 ms |
| Gen0 collections | 1 in the Editor process |
| Renderer / collider components | 1,160 / 1,119 |

Component counts include disabled/decorative objects and are not draw calls or active contacts. Wall frames include Editor/target-frame pacing; this is not GPU timing, Android FPS or a long-session/thermal benchmark.

Across the [55-course run](../TestResults/campaign-validation-summary-v5.json), Editor build times were 110.51-155.47 ms, median 129.15 ms; Unity allocation ranged 196.74-212.45 MiB. Structural/domain reports currently contain 5,639 and 104 checks; neither simulates gameplay. Static batching, resource disposal, bounded queries, buffered input and the 10 Hz race HUD are implemented but do not establish device performance. The [current ADB listing](../TestResults/android-devices-v5.txt) has no attached device. Android and Windows playback modules are installed, with no iOS module. Android GPU/thermal/battery and iOS data remain unavailable.

## Build and prioritized release work

[BUILDS.md](BUILDS.md) supplies exact commands; [QA.md](QA.md) supplies phone procedures. Gameplay and APK checks are complete; exact artifact metadata is recorded in the release-evidence table. Tools/VerifyManualRelease.py verifies the exact v0.5.0 package, scene/native/content entries, latest-name test union and traversal CSVs, and writes TestResults/manual360-release.json. It requires this delivery's archived full/final reports; fresh source revisions require fresh validation evidence.

1. Install the verified testing APK on budget and midrange Android phones. Check touch/camera feel, safe areas, every branch, interruptions and at least 15 minutes of eight-ball/signature performance.
2. Tune difficulty/rendering from device and human evidence. Command-only traversal establishes a possible physical completion, not universal collision fairness or touch comfort.
3. Replace temporary clips with recorded/licensed Foley and audition speakers/headphones/Bluetooth/calls.
4. Persist active cup state if process-death continuation is required. Completed round records/rewards persist; active points currently remain in memory.
5. Prepare release keystore/AAB, adaptive/monochrome icons, store media/metadata and distribution testing. Ads/payments, online racing, time-trial ghosts and backend anti-cheat are absent; daily rewards use device UTC.
6. Validate iOS on macOS with the Unity module, Xcode, provisioning and physical devices. No iOS build/device result is claimed.
