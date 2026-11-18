# Physics, gameplay and racing audit - baseline 0.3.0

Audit performed before implementation changes, 27 October 2026. Evidence: runtime/editor/test source, serialized Unity physics/time settings, package manifest and previous build/test reports. This is a native Unity 6000.0.65f1 C# project using Unity PhysX, URP17.0.4, InputSystem1.11.2, Cinemachine3.1.7 and native uGUI/TMP. It is not a web game. Existing illustrated art, twelve ball finishes, offline rewards, saves and tested Android build tooling will be retained.

## Existing systems and confirmed working choices

BallController is the only movement controller. It applies forces/impulses in FixedUpdate; ordinary travel does not set transform.position. Rigidbody.position and zero velocity are used explicitly for respawn. Bodies have interpolation and ContinuousDynamic collision detection. The serialized fixed timestep is1/60s, maximum frame catch-up0.1s, gravity(0,-9.81,0), solver iterations8/2. Dynamic sphere colliders, ball/track friction and real gravity are already present. QuestCamera extends Cinemachine independently of rolling rotation. LevelBuilder builds actual collider geometry from ScriptableObjects. Checkpoints, coins, pause/results, daily/spin/skin rewards and checksummed JSON/backup saves exist. The historical0.3.0 reports record28passing tests; these are not validation of the new work.

## Findings and correction plan

| Severity | Evidence / root cause | Planned correction |
| --- | --- | --- |
| Critical for racing | CourseTrigger accepts any BallController; an opponent would finish or checkpoint the solo player. Collectible has the same identity omission. Boosters also credit global player statistics for every ball. | Route competitor triggers through an explicit race identity; solo triggers and pickups require the current player ball. |
| High | Horizontal swipes apply uncapped lateral impulses; releasing stops input but provides no deliberate lateral settling. | Resolution-independent relative drag, bounded smoothed steering, configurable lateral acceleration/damping/speed and limited air control. |
| High | AddForceAtPosition uses a fixed0.28m lever arm per swipe. Impulse batches and angular acceleration vary with input sampling and strokes. | Shared fixed-step control commands, short controlled impulse delivery, physically coherent sphere rotation and bounded control effort. |
| High | A single layer0 spherecast supplies grounding. It ignores support contacts on other valid layers and has no explicit platform velocity. | Contact-normal grounding plus filtered support queries; dynamic support velocity and surface materials. |
| High | No shared command interface exists for human and AI control. | One BallControlCommand/physics law for both; AI chooses controls rather than driving transforms. |
| High | Falling/platform obstacles have no checkpoint reset contract; falling tiles return via a clock-driven teleport and can remain collapsed after retry. | Reset registry, controlled collapse states, safe restoration before input reactivation and checkpoint clearance checks. |
| High | GameSession has only Menu/Playing/Paused/Fallen/Complete and globally pauses time on player completion/failure. This would stop opponent finish validation/recovery. | Guarded loading/ready/falling/respawn states; independent race countdown/running/pause/results lifecycle and explicit ownership of time scale. |
| High | No racing roster, route cursor, finish validation, results, standings, tournament or persisted race rewards exist. | Validate four-AI vertical slice first; then five race courses, three difficulty behaviors and the requested formats/modes. |
| High | Twelve adventure courses use one two-drop structural recipe and only three placed moving hazard types. | Fifty-five curated compositions covering beginner through expert; staged mechanic introductions, varied obstacles, routes and speed requirements. |
| Medium | Limits22/25, impulse gains16/24, angular speed70 and camera effects are hard-coded; BallProfile.braking is unused. | Serialized tuneable physics and camera parameters with safe defaults, surface-dependent friction and optional effects. |
| Medium | Input divides by max(320,screenWidth) and clamps frame duration to1/15..1/240. This alters behavior on low-resolution and slow frames. | Logical screen normalization and consistent time integration across sampling rates, including cancellation and UI-touch exclusion. |
| Medium | Moving platforms use kinematic MovePosition but no explicit material/contact velocity policy. | Proper kinematic velocity, stable support-relative controls and verified moving/rotating support behavior. |
| Medium | Mitered visible roads use overlapping box support colliders, producing small seam/normal differences. | Audit/reduce seam discontinuities and validate ramp/curve contacts with physical traversal. |
| Medium | Camera has always-on effects and no racing crowd/impact context. | Configurable landing/impact/FOV settings, race crowd framing and collision-safe player priority. |
| Medium | HUD formats strings every frame; previews synchronously create thumbnails; every course rebuilds detailed geometry. | Change-driven HUD updates, bounded/reused AI queries and meshes/effects, measured editor loading/runtime diagnostics. |
| Medium | Existing UI level view fits twelve entries only; expanding content would place controls off-screen. | Paged world/level and race selection, mode selection, race HUD/results and tournament standings. |
| Verification gap | Previous keyboard traversal does not test natural horizontal-drag neutral behavior, racing, platform transport or all newly required mechanics. | New edit/play tests, full course/race input-driven runs, lifecycle/reward/collision checks, render evidence and a new signed APK. |
| Platform limit | Android toolchain is installed; no physical-device performance evidence or macOS/Xcode/iOS signing is available. | Verify Android build, signature/integrity/alignment; report editor measurements separately and provide exact device/iOS release checks. |

## Execution and preservation rules

Keep the current engine/art/save format migration path and established systems. Implement physics/controls first, then camera/contact/reset behavior and obstacle/content framework. Race opponents use the exact same force-driven controller with skill differences in planning and decisions, never hidden speed boosts or waypoint animation. Only explicit, validated checkpoint recovery may relocate a body. Rewards require validated finish order and persistent deduplication. Build and test an actual four-opponent race before expanding the five race courses and formats. Every shipped course must finish under ordinary movement commands; unvalidated/impossible content must be corrected or explicitly excluded from playable selection.

The planned campaign contains55adventure courses so all five requested difficulty bands are represented, plus at least5dedicated racing courses. Results, measurements, failures and release limitations will be recorded after real Unity execution; this audit is not a production-readiness claim.
