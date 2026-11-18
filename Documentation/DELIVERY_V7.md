# Ball Quest 3D 0.7.0 - swipe and presentation upgrade

This revision addresses held-finger acceleration, camera instability, basic track/reward presentation and avoidable runtime overhead. [Development plan](DEVELOPMENT_PLAN_V7.md). The reference was the [publisher's Going Balls listing](https://play.google.com/store/apps/details?hl=en_US&id=com.pronetis.ironball2); this is an original implementation, without access to that game's code or exact tuning.

Testing artifact: **[BallQuest3D-SwipeUpgrade-v0.7.0.apk](../BallQuest3D-SwipeUpgrade-v0.7.0.apk)**. **43,864,961 bytes / 41.83 MiB**, Android 8+, ARM64, portrait, IL2CPP/OpenGLES3, version 0.7.0/code 7, package `com.ballqueststudio.ballquest3d`, target API 36. This testing build omits development instrumentation. Its signing certificate was read and compared with v0.6: they match, supporting an update of the existing testing installation. The [release record](../TestResults/swipe-upgrade-release.json) contains the package checks and test counts. SHA-256: `FCB32E9017C53379E744652A0D0A4417BA3348DED3EA417E58AE5BBB8D7AC962`.

## Controls and camera

Touch displacement now supplies a finite force-time budget that physics consumes once. Holding still adds no new drive, before or after a swipe; there is no joystick displacement, reused render-frame command or smoothing tail. Bounded sensor jitter is rejected. Responsive swipe adds more momentum for faster strokes; precision swipe uses gentler constant gain. Both modes require actual movement. Keyboard and AI retain their explicit continuous controls. Physical force, rolling torque, gravity, resistance, collisions, braking and coasting remain active.

The focused touchscreen tests measured zero motion from stationary holding and bounded jitter, and exactly zero applied drive when the finger stopped after a swipe. An equal 100-pixel stroke on a 1000-pixel reference screen produced 3.990 m/s over 30 samples, 6.742 m/s over six samples, and 8.319 m/s in one sample. This also checks that a very fast single sample does not lose strength. Pause/resume clears pending motion. [Swipe measurements](../TestResults/swipe-only-v7.json), [implementation and focused test evidence](SWIPE_FEEL_V7.md).

The camera holds its heading through small steering corrections, follows sustained changes, and completes a smooth turn when returning. Continuous strokes retain their starting world direction while it turns. It uses filtered motion for framing, a quieter speed FOV, immediate obstruction clearance and smooth arm recovery. Manual orbit and gravity transport remain available. Existing saves select the revised swipe/follow defaults once while preserving currency, progression, skins and sensitivity.

## Native presentation

Tracks now have round rails, ceramic/metal fascia, supporting structures, consistent maple planks, planted floating islands, bevelled towers, roof terraces and a reusable ocean texture. Lower returning paths have scenery clearance checks. Existing road face clipping and full physical support meshes remain intact.

Coins are double-sided bevelled gold meshes with raised, correctly shaded stars and polished rims. Gems use separate cut facets. Boosters have rounded metal housings, directional light animation and activation feedback. Near-camera rewards clear the foreground visually while their collection triggers remain active. Fixed-capacity rings/particles avoid unbounded effect creation. The updated tutorial explicitly says swiping controls movement and holding still coasts.

Reviewed native 720 x 1280 captures, using the project's MSAA setting:

- [Harbor curve and coins](../TestResults/Previews/V7HarborCurveAndCoins.png)
- [Sky Maze](../TestResults/Previews/V7SkyMazePresentation.png)
- [Booster and night track](../TestResults/Previews/V7AcceleratorAndCoins.png)
- [Eight-ball race](../TestResults/Previews/V7EightBallPresentation.png)

These are real Unity camera renders, not concept mockups. The art remains stylized low-poly, with repeated scenery and simple portal frames; it is not a visual replica of the illustrated reference. Still images cannot establish motion smoothness or absence of device-specific flicker.

## Performance work

Static architecture is combined by material and spatial region while retaining road collisions. Cosmetic collider components are removed. Ball shell geometry is shared and uses four mesh renderers per ball; skin textures are cached. Coins/pads share seven meshes and ten materials per course; one central loop animates visual children while triggers remain stationary. Distant animation is throttled, menu previews render at 30 Hz and numeric HUD updates run at 10 Hz. SRP batching is enabled.

The isolated allocation check measured zero managed bytes across 600 warmed animation calls over 80 mixed rewards and an active booster. This excludes native/GPU work, initialization, whole-frame allocations and Android behavior. [Allocation evidence](../TestResults/reward-animation-allocation-v7.json).

The [eight-ball Editor measurement](../TestResults/presentation-performance-v7.json) records current renderer/collider counts, course construction time, memory and wall-frame timing. The [v0.6 baseline](../TestResults/editor-race-measurement-v6.json) used 1,135 renderer components, 1,095 collider components and a 141.02 ms build. The v0.7 report counts enabled renderers, including distance culling, so this is not a direct draw-call comparison. Render counters unavailable in batch mode are marked -1; Editor target-paced timing is not a phone GPU/FPS benchmark.

The final v0.7 sample recorded 299 enabled renderers, 276 collider components, a 55.56 ms course build, 200.59 MiB Unity allocated memory and 16.664/16.665/17.087 ms mean/median/p95 wall-frame intervals at the 60 FPS target. Across the full 55-course campaign, construction ranged from 34.40 to 114.48 ms, with a 49.69 ms median (previous median: 135.06 ms). The frame counter still recorded one generation-0 collection and a maximum 516,678 managed bytes in one sampled Editor frame; the isolated animation result is not a claim that the whole game allocates nothing.

## Validation

All **106 distinct Unity tests passed**: 28 EditMode and all 78 PlayMode cases in the complete run, which took 622.20 seconds. All 55 campaign courses and 15 adventure variants completed with zero falls. The optimized Android APK built successfully and passed signature, CRC, manifest, 16 KiB ZIP/native alignment and packaged-content verification. Both scenes, all course/race definitions, new shaders/textures and runtime presentation code are included; test-driving classes are excluded.

| Evidence | Result |
| --- | --- |
| [EditMode-v7.xml](../TestResults/EditMode-v7.xml) | 28/28, including geometry, migration, progression and resource batching |
| [PlayMode-v7-full.xml](../TestResults/PlayMode-v7-full.xml) | 78/78, including the complete retained regression suite |
| [Campaign](../TestResults/campaign-validation-summary-v7.json) | 55/55, zero falls |
| [Signature adventures](../TestResults/expedition-validation-summary-v7.json) | 15/15, zero falls, actual wall/ceiling contacts and hidden rewards |
| [Road rendering](../TestResults/road-rendering-v7.json) | Moving-camera coverage and unobstructed ball visibility |
| [Native presentation](../TestResults/native-presentation-v7.json) | Actual supported gameplay, upgraded meshes and supported shaders |
| [Standalone checks](../TestResults/csharp-v7.txt) | 124 C# syntax/domain checks; separate from Unity compilation/simulation |
| [Asset checks](../TestResults/assets-v7.txt) | 5,697 structural checks; no physical simulation |

Acceptance includes every retained campaign/adventure/race flow, actual touchscreen hold/swipe behavior, full camera turns, moving-road visibility, reward/boost lifecycle and bounded resources. The APK verifier checks version/architecture, signature, ZIP integrity, 16 KiB ZIP/native alignment, both scenes, 60 adventure definitions, five race definitions, new presentation resources and exclusion of test-driving code. Reruns do not add unique tests.

No phone was listed by ADB. Physical Android touch comfort, hardware-specific flicker and sustained thermal/frame performance remain unverified. The game preserves the existing music and temporary material-contact samples documented in [CONTACT_AUDIO.md](CONTACT_AUDIO.md). This is a testing APK, not a store submission.
