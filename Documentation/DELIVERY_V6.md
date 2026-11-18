# Ball Quest 3D 0.6.0 — movement, track stability and reverse camera

28 October 2026. This revision addresses the reported heavy ball response, road flickering and missing camera turnaround while travelling back. All 87 Unity tests pass: 25 EditMode and 62 PlayMode. All 55 original courses and 15 adventure variants completed without falls. The Android testing APK is built and verified; physical phone validation remains separate. Earlier APKs and reports describe their respective revisions.

**[Download BallQuest3D-Responsive-v0.6.0.apk](../BallQuest3D-Responsive-v0.6.0.apk)** - 52,439,394 bytes / 50.01 MiB, Android 8+, ARM64. Version 0.6.0/code 6, com.ballqueststudio.ballquest3d, IL2CPP/OpenGLES3, portrait, target API 36, debug development signing. SHA-256: `D5352C666E062442189A7CFD3A8EE5859B28004DAFDA994FA2F01C13B03AE9C6`. [Machine-readable release record](../TestResults/responsive-release.json).

## Changes

**Responsive rolling.** Default input now follows swipe velocity, so a brisk stroke supplies useful force immediately and repeated strokes build momentum. Standard drive increases from 18 to 27 force units; counter braking is 32. Ground resistance decreases from 0.65 + 0.65 × speed to 0.55 + 0.32 × speed. Input smoothing is 12 ms and controller response/release are 18 ms. Real mass, contact friction, rolling torque and coasting remain active. Opposite swipes brake through zero before reversing. Optional RelativeDrag has a smaller 6% screen radius and immediate deliberate reversal at the current finger position.

**Stable road rendering.** Authored collision padding and broad branch junctions previously drew overlapping coplanar road faces, allowing depth competition as the camera moved. RoadRenderGeometry clips duplicate visible patches while retaining the complete physical support meshes. Gravity Lab draws one continuous curved roadway over its existing physical support pieces. Millimetre-thin plank seam cubes are removed; grain uses trilinear mip filtering and anisotropy. Surface marker strips follow their road normal and stop casting small crawling shadows. Main and virtual camera clipping is 0.25-350; the existing 2048 shadow atlas uses two cascades and filtered shadows. Medium/high quality use 4× MSAA and 25/40-unit shadow distances. These changes address source-level causes; phone visual confirmation remains required.

**Camera follows return travel.** AdaptiveChase is the default. Travel above 1.2 units/second with an 18-degree heading difference engages after 0.16 seconds. The camera turns at up to 135 degrees/second with 540 degrees/second squared acceleration/deceleration, completing to within 0.5 degrees of travel. Exact reverse travel retains one turn direction. Manual orbit supports full circles and two seconds of look grace; StableOrbit remains selectable. Each movement stroke captures its initial world basis so a turning camera cannot make a held command spiral. A new or resumed stroke adopts the current view. Gravity changes transport both frames.

Quick replacement touches now reanchor even without a full rendered frame between releases. UI-started gestures retain ownership; obsolete UI touch IDs clear on reset. Two-finger/right-edge gestures continue to control the camera without driving the ball.

An instrumented touchscreen run exposed an additional response defect: saturating a raw one-frame velocity spike before filtering discarded most of its displacement when render frames arrived faster than touch samples. The new 25 ms velocity estimator filters before saturation. A regression supplies 60 Hz touch samples at 30/60/120/600 Hz render sampling; the real pointer fixture also uses the app's 60 Hz pacing. RelativeDrag now clamps its floating-anchor radius before applying speed gain, removing a separate sample-rate bias.

Moving-camera screenshots exposed a checkpoint header blocking the ball and world-label glyphs filling the view as the camera passed through them. Gate headers now clear the chase-camera height and nearby world labels fade before crossing the lens. The moving-junction test checks both visibility conditions.

## Defaults and compatibility

SaveData version 6 selects GestureVelocity and AdaptiveChase once for existing saves and uses them for fresh installs. Existing balances, unlocks, course records, skins and sensitivity are preserved. Later user choices of RelativeDrag or StableOrbit persist. Catalog manualControlVersion 2 seeds the revised authored profile tuning once. The 55-course campaign, five signature adventures, Physics Lab and racing content remain available. See [MANUAL_CONTROLS.md](MANUAL_CONTROLS.md) for exact controls and physics behavior.

## Validation and artifact

| Check | Current status |
| --- | --- |
| Unity generation / compilation | Passed, Unity 6000.0.65f1 |
| EditMode, including road coverage and settings migration | 25/25 passed: TestResults/EditMode-v6.xml |
| PlayMode, including movement, real pointer ownership and camera turnaround | 62/62 passed in the complete run: TestResults/PlayMode-v6-full.xml; 556.11 seconds |
| Existing campaign / expedition / racing regression coverage | 55/55 courses and 15/15 variants, zero falls; five racing integrations passed |
| Android development APK, version 0.6.0 / code 6 | Built; 52,439,394 bytes / 50.01 MiB |
| APK signature, integrity, alignment, packaged content and SHA-256 | Passed: signature, ZIP CRC, 16 KiB ZIP/native ELF alignment, both scenes, 60 adventure titles, five races and revised compiled gameplay code; test-driving classes excluded |
| Physical Android touch feel and moving-road visual review | Not yet verified on a phone |

New road tests check one visible surface across padded seams and maze junctions without losing coverage, separate elevations, sideways texture scale and a continuous gravity loop. Settings tests check one-time migration with preserved progression and subsequent explicit choices. Movement tests exercise short/repeated actual touch strokes, fast versus gentle strokes over the same window, coasting, physical reversal, camera-frame latching, replacement touches and resumed neutral strokes. Camera tests cover complete smooth reversal, rest/pause, full manual circles, gravity transport and the actual Cinemachine output during a held reverse command in both directions.

The latest [touch measurements](../TestResults/responsive-movement-v6.json) record 3.134 m/s after one brief 8%-screen stroke, 10.425 m/s after four strokes, and 92.44% speed retained after 0.2 seconds of coasting. Over equal 1.600-second windows, six brisk strokes reached 13.916 m/s while one gentle stroke reached 1.911 m/s. Full opposing input braked 14.289 m/s through zero in 0.433 seconds. These are measured Unity Rigidbody results, not phone touch-latency measurements.

The first targeted slice passed 21/24 tests and exposed sparse-input pulse loss plus the floating-anchor sampling bias. The corrected isolated run passed 9/9; the subsequent complete run passed every one of its 62 PlayMode cases. Reruns add no unique tests. The latest full report is the acceptance evidence.

[Campaign measurements](../TestResults/campaign-validation-summary-v6.json): course build time 114.35-175.65 ms, median 135.06 ms, Unity allocations 194.62-210.90 MiB. [Adventure measurements](../TestResults/expedition-validation-summary-v6.json): 201 wall-contact and 517 ceiling-contact steps; hidden gem and token actually collected. These automated routes issue the same force commands as gameplay.

[Moving-junction rendering](../TestResults/road-rendering-v6.json) captures seven successive native camera frames. The 90 original padded road colliders retain their complete solids; the separate visible mesh has 4,765 vertices/1,174 triangles and took 7.53 ms to build in isolation. The actual course took 119.83 ms in that slice. Camera-to-ball rays remain clear of gate headers and nearby world labels fade. The screenshots establish the corrected composition; structural tests establish single coverage rather than relying on a brittle pixel comparison.

The eight-ball Windows Editor measurement recorded 16.675/16.663/16.968 ms mean/median/p95 frames, 207.46 MiB Unity allocation and a 141.02 ms course build. It is not an Android performance benchmark. Offline checks passed 5,659 asset/metadata checks and 112 standalone C# syntax/domain checks, separately from Unity simulation.

Editor tests cannot establish phone touch comfort, hardware-specific depth/shadow stability, safe-area behavior or sustained mobile performance. Material contact audio continues to use the temporary samples identified in [CONTACT_AUDIO.md](CONTACT_AUDIO.md).

The original padded collision geometry is retained. The high-speed junction capture contains one brief unsupported sample over a collision seam, followed by a normal landing without a fall; the visible overlap and camera occlusion fixes do not claim that every collision seam is a smooth continuous physical surface. No phone was listed by the current ADB check.
