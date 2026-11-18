# Ball Quest 3D 0.8.0 - gradual swipe speed and centered camera

This revision addresses two specific playtest problems: one short swipe added too much speed, and the camera could leave the ball off-center while looking across the direction of travel. [Development plan](DEVELOPMENT_PLAN_V8.md).

**[Download BallQuest3D-Centered-v0.8.0.apk](../BallQuest3D-Centered-v0.8.0.apk)** - 43,863,861 bytes / 41.83 MiB, version 0.8.0/code 8. The Android build and all package checks passed. SHA-256: `76A086DCD87092D89A4E1BF2B6B4C7AA778ADC614BF02D45729B560360692C05`. [Verified release record](../TestResults/centered-release.json).

The latest results for all **115 distinct Unity cases passed**. All 28 EditMode cases passed; the complete PlayMode run passed 86/87 in 643.838 seconds, followed by an 11/11 targeted GameplayTests run after correcting one outdated mouse fixture to use repeated strokes. Runtime code is unchanged after the complete run, and its original raw XML is retained. All 55 campaign courses and 15 adventure route variants completed without falls.

## Gradual speed buildup

Touch gain decreases from 2 to 0.65. Responsive swipe uses a restrained speed multiplier of 0.85-1.15 and caps each deliberate stroke at 0.09 normalized force-seconds. A fast stroke starts the ball rolling; repeated strokes accumulate momentum toward its normal powered limit. One oversized stroke has the same modest limit regardless of how many hardware samples deliver it.

New touch, resumed motion after a neutral pause, or a coherent directional change makes fresh drive available. Direction changes do not relatch the captured camera frame during the same touch, so a turning view cannot bend an ongoing stroke into a spiral. Holding still and bounded sensor jitter add no drive; physics consumes each accepted budget once. Movement still uses actual force, torque, gravity, contact resistance and coasting.

Existing saves select the revised swipe/follow defaults once. Excessive saved sensitivity is reduced to the normal setting while currency, unlocks, records and skins are preserved. Sensitivity selected after this migration persists normally.

## Camera follows and centers the ball

The previous 40-degree engage gate and 0.42-second delay are removed. Meaningful right/left motion starts a smooth heading adjustment in the current camera update. Rotation remains bounded at 240 degrees/second with acceleration/deceleration rather than snapping to a heading.

Translation follows the interpolated ball position immediately. Heading, gravity orientation and obstruction-arm recovery are smoothed independently, so lateral travel cannot leave the ball behind an old camera position. The camera aims from its actual cleared lens position to keep the ball at viewport x=0.50/y=0.40, including FOV changes, turns and jumps. This places it in the central gameplay area with road visible ahead. Reverse turns, manual orbit, the optional fixed heading mode and gravity transport remain supported.

The hidden one-finger camera region at the screen's right edge is removed. A one-finger swipe that starts outside UI controls drives the ball across the whole screen. Manual look uses two fingers or the right mouse button, preventing an ordinary rightward stroke from unexpectedly rotating the view.

## Acceptance evidence

The evidence includes the complete [EditMode-v8.xml](../TestResults/EditMode-v8.xml), unmodified [PlayMode-v8-full.xml](../TestResults/PlayMode-v8-full.xml), and targeted [FinalChecks-v8.xml](../TestResults/FinalChecks-v8.xml) reports. Acceptance uses the latest result per unique test name; focused reruns add no unique tests. The full raw report is retained with its original failure. New acceptance checks cover:

- Actual touchscreen speeds after one, four, eight and twelve strokes; a bounded oversized stroke across different sample cadences; stationary holds and one-time consumption.
- Actual Cinemachine output during four cardinal turns, curved motion, changing speed, a force-driven jump, and fixed/manual heading. Measurements record centering error, immediate small-turn response, settled alignment and turn-rate bounds.
- The real shipping bootstrap with saved defaults, touchscreen strokes, physical rolling and a rightward correction; this checks that the delivered scene behaves like the isolated fixtures.
- All retained campaign, signature adventure and racing regression checks, native graphics captures, reward/boost lifecycle and bounded animation allocations.

Current measurement files include [progressive-swipes-v8.json](../TestResults/progressive-swipes-v8.json), [centered-camera-v8.json](../TestResults/centered-camera-v8.json), [shipping-controls-v8.json](../TestResults/shipping-controls-v8.json), [swipe-only-v8.json](../TestResults/swipe-only-v8.json) and the v8 route summaries.

The final focused run passed 8/8 acceptance cases in [ControlsFinal-v8.xml](../TestResults/ControlsFinal-v8.xml). The latest complete-run measurements recorded 1.690 m/s after one fast 10%-screen stroke, 6.039 m/s after four, 10.711 m/s after eight and 13.652 m/s after twelve. The maximum speed from one oversized stroke was 2.442 m/s. Across 870 actual camera-output frames, maximum viewport error was approximately 0.00000155 horizontally and 0.000000417 vertically; a small deliberate turn began with a bounded 0.400-degree heading step. These are Unity Editor results rather than phone touch-hardware measurements.

The shipping bootstrap integration passed with 1.550 m/s after one stroke and 5.521 m/s after four. A modest rightward stroke changed the camera heading by 12.910 degrees while maximum viewport errors stayed around 0.000000060 horizontally and 0.000000656 vertically. It completed with zero falls. [Native shipping steering capture](../TestResults/Previews/V8ShippingSteering.png).

All 28 [EditMode-v8.xml](../TestResults/EditMode-v8.xml) cases passed. The full gameplay run passed all new input/camera/shipping checks and completed all 55 campaign courses plus 15 adventure route variants without falls. One old mouse test asserted speed above 2 m/s after a single continuous 45-sample drag; the intentional stroke cap let it coast to 0.956 m/s instead. The test now uses four separate forward strokes with a stronger speed-above-3 m/s assertion, retains its coast check, and uses six opposing strokes to require physical reversal below -1 m/s. The entire targeted GameplayTests fixture passed 11/11 in [FinalChecks-v8.xml](../TestResults/FinalChecks-v8.xml). No runtime behavior was changed for this fixture correction. The verifier uses the latest result by unique test name and confirms all 115 passed, without counting those 11 rerun cases again or rewriting the failed raw report.

## APK and retained presentation

The verified testing package targets Android 8+ (minimum API 26), ARM64, portrait, IL2CPP/OpenGLES3 and target API 36, under `com.ballqueststudio.ballquest3d`. BuildOptions.None omits development instrumentation; the APK is debug-signed for testing. Its certificate was read and compared with v0.7 and matches, supporting an update of the previous testing installation. Physical installation/update was not tested.

Verification passed for the manifest/version, signature, ZIP CRC, 16 KiB ZIP/native alignment, both scenes, all 60 adventure definitions and five races, upgraded graphics resources, v0.8 runtime fields and exclusion of test-only driving code. The [release record](../TestResults/centered-release.json) preserves the raw full-run failure and passing targeted rerun metadata as well as the 115 latest passing results.

The native tracks, raised coins, animated boosters, scenery, shared meshes, cached textures and batching introduced in v0.7 remain part of this revision. [DELIVERY_V7.md](DELIVERY_V7.md) records that earlier artwork/performance work; its results are historical and do not establish that v0.8 has passed. Music and the existing temporary material-contact samples remain documented in [CONTACT_AUDIO.md](CONTACT_AUDIO.md).

Fresh [v0.8 eight-ball measurements](../TestResults/presentation-performance-v8.json) recorded 299 enabled renderers, 276 collider components, a 57.94 ms build and 202.56 MiB Unity allocated memory. Mean/p95 target-paced Editor frame intervals were 16.665/16.967 ms over 240 frames, with one generation-zero collection. Whole-frame managed allocation was not zero; its recorded maximum was 544,462 bytes. Unavailable render counters are marked -1. The [campaign build median](../TestResults/campaign-validation-summary-v8.json) was 51.00 ms across all 55 courses. These measurements preserve the distinction between Editor regression evidence and sustained Android performance.

The [final ADB check](../TestResults/android-devices-v8.txt) lists no physical Android device. Editor simulations and captures cannot establish phone touch comfort, hardware-specific flicker or sustained thermal/frame performance.
