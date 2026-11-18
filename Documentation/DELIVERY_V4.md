# Ball Quest 3D 0.4.0 delivery and validation

28 October 2026, local time. The existing Unity project now contains corrected shared ball physics, 55 adventure courses, five dedicated AI racing courses and an integrated racing/tournament interface. The Android development build is ready for testing. Physical-phone and iOS validation remain open; this is not a store-release certification.

## APK

[Download BallQuest3D-Racing-v0.4.0.apk](../BallQuest3D-Racing-v0.4.0.apk) — 51,308,238 bytes, 48.93 MiB. The generic BallQuest3D-test.apk contains the same build. Previous versioned APKs are retained.

| Property | Verified value |
| --- | --- |
| Application | Ball Quest 3D / com.ballqueststudio.ballquest3d |
| Version | 0.4.0, Android version code 4 |
| Runtime | Unity 6000.0.65f1, IL2CPP, ARM64, OpenGLES3 |
| Android | Minimum API 26 / Android 8.0; built with target API 36 |
| Signing | Development/debug certificate; apksigner verification passed |
| Packaging | ZIP CRC, native ELF64 AArch64 libraries and 16 KiB alignment verification passed |
| Content | All 55 adventure titles and all five race titles found in the packaged Unity assets; compiled race/control metadata present |
| SHA-256 | BD1AF495C812A05F88428189AE0FC844B237D85609CFE682613FD624FACEF355 |

Machine-readable evidence: [racing-release.json](../TestResults/racing-release.json), [APK verification](../TestResults/apk-verification.json), [Android build log](../TestResults/AndroidDev.log).

## Original problems and corrections

The [pre-implementation audit](PHYSICS_AND_RACING_AUDIT.md) records the source evidence and severity. The main movement problems were unbounded accumulated sideways impulses, no deliberate neutral steering settling, and an arbitrary off-centre force that added sampling-dependent spin. Grounding only queried one layer and did not account for moving support velocity. Braking configuration was unused and several motion limits were hard-coded.

Horizontal drag now requests a smoothed, bounded lateral response. Releasing the gesture settles steering while forward momentum coasts. Vertical flick speed determines finite impulse energy, consumed across fixed physics steps; repeated fast upward strokes add speed within configured limits. A downward gesture brakes. UI-started touches, cancellation and invalid inputs are handled. Dynamic Rigidbody motion, contact friction, gravity and rolling rotation remain physical; ordinary movement does not write transforms or force waypoint positions.

Human input and AI share BallControlCommand and BallController. Contact normals and nonalloc support probes reject walls/rival balls and accept solid track layers. Support-body point velocity allows moving-platform transport without parenting. Explicit recovery clears linear/angular velocity. A world-up, path-relative Cinemachine camera anticipates slopes, avoids geometry and adds optional impact/landing/FOV effects with modest crowd framing.

The Standard profile exposes acceleration 18, throttle speed 14, maximum flick speed 22, boost speed 25, braking 14, lateral speed 5 and steering acceleration 28, with 0.08s response and 0.12s release. These are tuneable game units. Physics runs at 60 Hz independently of rendering. Other supported adventure profiles retain their mass/grip differences; racers all use the same Standard profile for fairness.

Checkpoint and race identity bugs also required correction: an arbitrary ball could previously trigger the solo finish or collect the player's rewards. Triggers now distinguish racers from the solo player. Ordered checkpoints and ordered 3D route arcs validate progress. Solo recovery resets relevant obstacles before input reactivation; one racer's recovery never resets the shared race world.

## Obstacles and playable content

The complete [course table and module behavior](PRODUCTION_COURSES.md) lists all 55 adventure recipes and five races. Adventure bands are Beginner 1–5, Easy 6–15, Medium 16–30, Hard 31–50 and Expert 51–55. The first twelve progression IDs and asset GUIDs remain intact.

Implemented mechanics include ramps/hills, natural gap jumps and drops, narrow bridges, banked climbing spirals, branching lanes, slippery/ice/high-grip/bouncy surfaces, boosters and jump pads, conveyors, moving hammer arms, pendulums, sliding/pushing/crushing walls, vertical rotors, rotating blades, rolling physical hazards, timed doors/laser barriers, side/forward/elevator/circular platforms, and warning-to-gravity falling/collapsing decks. Resettable obstacles have explicit lifecycle restoration rather than leaving a checkpoint unusable.

Races are Harbor Five, Hammer Docks, Shuttle Speedway, Canyon Choice and Alpine Precision. Player plus 3–7 opponents share a frozen 3-2-1-GO start, dynamic sphere collisions and fair checkpoint recovery. AI uses curvature/look-ahead speed planning, hazard anticipation, braking, passing lanes and a physically built alternate route. Easy/Medium/Hard change decisions and reaction; they do not grant hidden speed multipliers or animate bodies along paths.

Sprint, Obstacle, checkpoint-based Elimination, target-time Time Trial and a three-round Tournament are available. The HUD shows actual rank, ordered route progress, remaining distance, timer, opponents, remaining field and round. Results validate real crossing times and report DNF for unfinished entrants. Podium/clean/cup rewards use persistent high-water records and per-result duplicate guards. An all-DNF cup cannot award a victory.

The existing arcade artwork, live 3D skin previews, music/sound, rewards and saves are retained. Campaign selection now pages through the expanded content; racing adds mode/format/difficulty/opponent selection, countdown, HUD, pause and results. [Race gameplay](../TestResults/Previews/RaceGameplay.png), [results](../TestResults/Previews/RaceResults.png), [elevated spiral](../TestResults/Previews/Course22.png) and [tournament](../TestResults/Previews/TournamentResult.png) are captures from actual Unity rendering.

## File summary

Paths below are relative to Assets/_BallQuest unless another root is shown. New source/assets include matching Unity .meta files. There is no Git history in this workspace, so this is the implementation inventory rather than a commit diff.

| Files / asset group | Change |
| --- | --- |
| Scripts/Player/BallControlCommand.cs, BallGroundDetector.cs | Shared controls and contact/support detection |
| Scripts/Player/BallController.cs, BallInput.cs, BallProfile.cs | Steering, flick integration, braking, limits, grounding and exposed tuning |
| Scripts/Camera/QuestCamera.cs | Stable path/slope/collision/race framing and optional effects |
| Scripts/Levels/LevelDefinition.cs, GameCatalog.cs | Ordered 3D authoring, alternate routes and race content references |
| Scripts/Levels/LevelBuilder.cs, ArcadeGeometry.cs, CoursePathFollower.cs | Collider-matched road meshes, competitors, support clearance, resource lifetime, route guidance |
| Scripts/Levels/CourseTrigger.cs; Scripts/Collectibles/Collectible.cs | Correct player/racer identity, checkpoints, boosts and pickups |
| Scripts/Obstacles/ICourseResettable.cs, QuestObstacle.cs | Reset contract and expanded physical obstacle lifecycle |
| Scripts/Editor/ProductionCampaignGenerator.cs, ProjectGenerator.cs | 55-course/5-race seed, tuning/material generation, version 0.4.0 |
| Scripts/Core/GameSession.cs, GameBootstrap.cs | Guarded adventure states, cancellation, race/tournament orchestration, pause/restart |
| Scripts/Racing/RaceTrackDefinition.cs | Race formats, difficulties and authored track data |
| Scripts/Racing/RaceRoute.cs | Ordered 3D arcs/gates and physically traversed alternate routes |
| Scripts/Racing/RaceDriver.cs | Shared-controller AI planning, avoidance and overtaking |
| Scripts/Racing/RaceCompetitor.cs, RaceManager.cs | Roster, countdown, rank, finish validation, recovery, elimination and cup standings |
| Scripts/Racing/RaceRewards.cs | Deduplicated race/cup rewards and progression |
| Scripts/Progression/SaveData.cs, ArcadeProgress.cs | v4 migration, race records, settings and reward persistence |
| Scripts/UI/QuestUI.cs, QuestUI.Racing.cs | Paged campaign and integrated racing/tournament screens/HUD/results |
| ScriptableObjects/GameCatalog.asset; Levels/Level001.asset–Level055.asset; racing definitions/course assets | Current contentVersion21 and generated playable data |
| ScriptableObjects/Balls/*.asset; contact-material assets | Serialized physics tuning; unsupported Magnetic/Energy mechanics excluded from current unlock range |
| Tests/EditMode/RaceRulesTests.cs | Ordered route and reward/DNF rules |
| Tests/PlayMode/BallPhysicsV4Tests.cs, ObstacleLifecycleTests.cs, RacingIntegrationTests.cs | Motion, camera, collision, obstacle and complete race coverage |
| Tests/PlayMode/GameplayTests.cs, BootstrapFlowTests.cs, SkylineTraversalTests.cs, BallQuest.PlayModeTests.asmdef | Updated guarded-state tests, expanded UI/course runs and Cinemachine test reference |
| Tools/verify_assets.py | Expanded structural checks for the authored content |
| ProjectSettings/ProjectSettings.asset, GraphicsSettings.asset, ShaderGraphSettings.asset | Generated version/platform and imported rendering settings |
| README.md; Documentation/*.md; TestResults/* | Updated handoff, audit, authoring, reports, measurements, captures and verified build evidence |

## Executed validation

**48 distinct Unity tests passed: 17 EditMode and 31 PlayMode.** The full PlayMode report contains 30 tests. The final two-test run repeats one recovery test with an added overtaking assertion and adds the shortcut/measurement test. It is not counted as two additional distinct tests.

| Evidence | Result |
| --- | --- |
| [EditMode.xml](../TestResults/EditMode.xml) | 17 passed: saves/migration/backup, progression, rewards and ordered route rules |
| [PlayMode-v4-full.xml](../TestResults/PlayMode-v4-full.xml) | 30 passed: physics, camera, lifecycle, UI, courses, race formats and tournament |
| [RacingFinalChecks.xml](../TestResults/RacingFinalChecks.xml) | 2 passed: repeated recovery/start/pause plus observed physical AI overtake; new actual alternate-route traversal and eight-ball measurement |
| [production-traversal.csv](../TestResults/production-traversal.csv) | 55/55 adventure finishes, every ordered checkpoint, zero falls in final run |
| [RaceVerticalSlice.xml](../TestResults/RaceVerticalSlice.xml) | Initial four-AI vertical slice validated before expanded modes/tracks |
| [assets-v4.txt](../TestResults/assets-v4.txt) | 5,177 structural assertions; does not simulate physics |
| [csharp-v4.txt](../TestResults/csharp-v4.txt) | 80 standalone domain checks; Unity execution is established by the XML reports above |

Motion checks cover acceleration/caps, both steering directions/reversal/release, input subdivision, friction/rolling behavior, airborne gravity/momentum, stable landings, non-default ground layers, moving-platform carry, rear-impact momentum without tunneling/explosion, invalid inputs, camera sample-rate stability, clipping and optional effects. Lifecycle checks cover falling, ordered checkpoints, zero-motion recovery, pause/restart, collapse warning/gravity/reset, door phases and rival isolation from solo rewards.

Actual race runs cover all five tracks, simultaneous starts, physical AI, sorted real finishes, multiple field sizes through eight balls, overtaking a stationary player, falls/recovery/restart, Elimination, Time Trial, three cup rounds and saved rewards. A competitor completed the authored Canyon Choice alternate route through ordinary control commands. Automated adventure validation likewise submits throttle/steering/brake commands; it does not set body poses/velocities, disable hazards or move tracks. This is repeatable automated play, not a human touch playtest.

Failures found and resolved during validation:

1. A legacy validator assumed finish world-Z must exceed spawn-Z, rejecting valid spirals. It now validates ordered 3D route geometry.
2. An old booster test assumed forward despite an explicitly different authored default axis. The test now configures the direction being tested; directional force behavior remains verified.
3. The first aggregate 55-course run exceeded its 180s harness timeout after 49 successful courses. The bounded timeout was increased to 600s; the complete final run passed.
4. Render inspection found insufficient spiral overhead clearance and piers obscuring lower roads. Spiral rise was increased to 12m, obstructing piers excluded, content regenerated, rendering reviewed and final traversals/tests/build rerun.

Final tested flows had no unhandled NullReference/MissingReference exceptions or shader errors. This does not establish that every possible human gesture, collision or device lifecycle is bug-free.

## Measured performance and scope

[Eight-ball Editor measurement](../TestResults/editor-race-measurement.json): Windows 11, NVIDIA GTX 1650 Ti, Unity Editor batchmode with active render camera, 180 frames and a 60 FPS target.

| Metric | Recorded value |
| --- | --- |
| Mean / median / p95 wall frame | 16.67 / 16.62 / 17.49 ms |
| Unity allocated memory | 196.71 MiB |
| Course build | 142.53 ms |
| Gen0 collections during sampling | 1, Editor process |
| Renderer / collider components | 1,329 / 1,339 |

Component counts include disabled/decorative objects; they are not draw-call counts or simultaneous physics contacts. These wall frame samples include Editor/target-frame pacing and are not a GPU timing, Android FPS or long-session benchmark. The short sample cannot establish mobile thermal stability or absence of frame drops.

Across the 55-course Editor traversal, course build time was 107.56–162.14 ms, median 127.80 ms; Unity allocated memory ranged 190.73–204.81 MiB. [Summary](../TestResults/campaign-validation-summary.json). Static geometry batching, reused collectible meshes, bounded AI/contact queries, 10Hz race HUD updates and explicit generated-resource disposal are implemented. Device draw calls, managed bytes/frame, peak GPU memory, temperatures and battery use remain unmeasured.

## Rebuilding and remaining release work

See [BUILDS.md](BUILDS.md) for build commands and [QA.md](QA.md) for exact manual procedures. The local licensed Editor and Android SDK/NDK/OpenJDK are installed under .tooling/UnityInstalled. Generate, run EditMode/PlayMode, build AndroidDev, then run Tools/VerifyApk.ps1. The verified APK is debug-signed for testing, not a release AAB.

Prioritized remaining work:

1. Install on budget and midrange ARM64 Android phones. Test touch feel, safe areas, real player completion, interruption/background/resume and 15-minute eight-ball frame-time/GPU/memory/thermal/battery behavior. No device was attached to ADB in this environment.
2. Tune controls, difficulty and rendering against those measurements and human play feedback. Automated traversal does not prove natural difficulty or touch comfort. New competitors/obstacles can still create combinations absent from deterministic test runs.
3. Persist an unfinished cup's active points/round/roster if process-death continuation is required. Completed round rewards/results and progression persist now; the in-progress cup exists in memory and starts fresh after process restart.
4. Prepare a custom release keystore/AAB and complete distribution testing, store assets and final metadata. Payments/ads/backend/analytics are not implemented. Daily rewards use device UTC and are not server-backed anti-cheat. Adaptive/monochrome release icons remain future work.
5. Validate iOS on macOS with iOS Build Support, Xcode, provisioning and physical devices. That toolchain is unavailable here; no iOS build is claimed.
6. Expand beyond the validated 55 adventures only with authored/verified content. Magnetic/Energy transformation puzzles, online racing and time-trial ghosts are not shipped. Time Trial currently uses an authored target time.
