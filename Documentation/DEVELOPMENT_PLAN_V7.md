# v0.7 development and acceptance plan

User priority: Going Balls-like swipe feel, stable camera, substantially better native 3D tracks/coins/boosters, and fewer gameplay stalls. Deliver a new testing APK after implementation and verification.

## 1. Control contract

Only actual pointer displacement may supply drive. Replace replayable velocity/held-stick requests with a finite displacement-derived budget consumed once by physics. Stationary holding before or after a swipe contributes zero new drive, including sub-threshold sensor jitter. Faster intentional swipes produce greater response; repeated swipes build momentum within existing limits. Preserve natural coast, opposite-input braking, gravity, collision, symmetric directions, UI ownership, and camera-only gestures. Keyboard/AI are explicit continuous commands. Migrate old control settings once; all available touch modes must obey swipe-only behavior.

Acceptance: actual touchscreen stationary hold for five seconds, swipe then stationary hold, bounded jitter, equal-distance fast versus slow stroke, repeated swipes, sparse input at varied render rates, pause/resume, quick replacement, no command replay across physics ticks. Measure Rigidbody motion and delivered control, not only input vectors.

## 2. Camera contract

Maintain a stable view of the ball and upcoming road; small lateral corrections must not whip the view around. Deliberate sustained return travel completes a smooth turnaround. Continuous strokes retain a stable control basis, new strokes use the current view. Keep manual orbit, gravity transport, collision clearance and predictable respawn. Frame lead/height smoothing must not chase single-frame velocity spikes.

Acceptance: native Cinemachine output during forward/side/reverse travel, continuous visibility, bounded angular rate, no held-input spiral, stable rest, camera-independent ball spin, jumps and gravity transitions.

## 3. Native 3D presentation

Use the reference's clear casual 3D readability, finished forms and material hierarchy. Upgrade warm wood/track surfaces, beveled fascia, round rail components, raised floating islands, planted/architectural scenery and themed landmarks. Preserve established route connectivity and collision support. Retain removal of coplanar duplicate faces; avoid tiny decorative shadows or foreground objects obscuring the ball.

Replace flat coin visuals with beveled, double-faced gold coins with rim/reeding and a readable embossed motif. Build low-profile boost platforms with recessed luminous chevrons, animation and a restrained activation effect. Keep pickups and boosts mechanically correct. Reuse meshes/materials, limit effect voices/particles and avoid per-item material cloning.

Acceptance: actual portrait captures of representative adventure/campaign/race scenes, close views of coin/boost, representative curve/junction and gravity path. Review visual composition and repeat visibility/coverage tests. No mockup presented as gameplay.

## 4. Performance

Establish and compare eight-ball and dense-course Editor frame timing, renderer/draw-call counts, memory and allocation samples. Replace many primitive decorations with material/spatial mesh batches, cache repeated textures/geometry and avoid recurring HUD allocations/rebuilds. Preserve culling and mobile quality controls. Prefer a testing APK without development instrumentation when supported and verified.

Acceptance: no new per-frame managed allocations in new animation systems after warmup; bounded asset counts; loading and gameplay measurements recorded. Editor evidence is not a physical Android FPS claim; check ADB and test a phone only if available.

## 5. Integration and delivery

Root integrates version/settings, assets, presentation, profiling, tests and Android build. Input/camera, track/scenery and pickup/boost implementation are delegated with separate file ownership. Run focused checks first, fix failures, then run all EditMode/PlayMode suites and full campaign/expedition/racing regression once implementation stabilizes. Review native screenshots. Build v0.7.0/code7 APK, verify signature, CRC, alignment, manifest and packaged content. Save exact hash, test records, measurements and honest limitations in DELIVERY_V7.md.

Reference: official publisher listing https://play.google.com/store/apps/details?hl=en_US&id=com.pronetis.ironball2 describes swipes controlling speed and direction. The implementation is an original native Unity game inspired by the reference, with no access to its proprietary code or exact tuning.
