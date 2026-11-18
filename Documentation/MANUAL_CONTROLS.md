# Manual 360-degree controls and physics

The v0.10.0 update retains these progressive swipe controls and automatic direction-follow camera. [Current plan](DEVELOPMENT_PLAN_V10.md) and [delivery evidence](DELIVERY_V10.md).

## Player input

BallInput samples touch/mouse/keyboard in Update; BallController consumes each finite pointer-displacement budget once in FixedUpdate. No input script writes a Rigidbody pose or velocity. Horizontal and vertical directions use the same response, acceleration and speed limit; diagonal requests are vector-clamped rather than receiving extra power.

- Swipe in the movement region to roll in that direction relative to the view at the start of the stroke. Swipe against existing motion to brake through zero and then roll backward.
- Responsive swipe (the compatible GestureVelocity enum value) multiplies normalized displacement by base gain 0.65, user sensitivity and a restrained 0.85-1.15 speed multiplier. Each deliberate stroke is limited to 0.09 normalized force-seconds. A short stroke starts rolling gently; repeated strokes build speed. Both axes use the same calculation. Continuing an exhausted straight stroke cannot add more drive; a fresh touch, resumed neutral gesture or coherent change of direction receives a new stroke budget.
- A stationary finger adds zero new drive immediately. A spatial deadband of 0.0015 times the smaller screen dimension rejects tiny sensor jitter. Neither input smoothing nor the physics consumer replays the last motion command. A final legitimate displacement may be consumed once after release, then the ball coasts under physical resistance. After 0.11 seconds parked, resumed motion adopts the current camera frame.
- Precision swipe (the compatible RelativeDrag enum value) uses the same finite displacement model with a constant 0.9 gain. Both available modes require actual swiping; neither behaves as a held joystick.
- A new touch establishes a neutral anchor, including a replacement touch arriving in the same rendered frame as the previous release. Repositioning cannot interpret travel from the old finger position as a movement command. A pointer that begins on UI remains excluded after leaving that UI. Resetting input clears obsolete UI-touch ownership.
- A stable primary non-UI finger rolls the ball anywhere on screen, including the right edge. Extra fingers never orbit the camera or reset that primary anchor. When the primary disappears, its replacement establishes a neutral anchor before supplying movement. Circular gestures remain ball movement; no finger gesture controls the camera.
- Desktop equivalents are left- or right-drag for movement, WASD/arrows for movement, and Escape for the existing pause flow. Diagonal keyboard input is normalized. Space can request jump only on courses that explicitly allow it; ordinary authored ramp jumps remain physical.

Screen normalization uses min(width,height), not an assumed device resolution. The finite buffer accumulates accepted displacement between physics steps and is cleared by its first consumer. A large single sample is limited by physical force and the powered speed ceiling, rather than discarded by a normalized joystick clamp.

SaveData version 9 selects responsive swipe defaults once for existing saves and reduces an old sensitivity above 1 back to 1. Lower sensitivity, currency, course records and unlocks persist. Later swipe-mode/sensitivity choices persist across reloads. Normal gameplay always applies AdaptiveChase, including when an old save contains another camera enum. Historical course best times refer to earlier layouts. ManualGameplayUpgradeGenerator version 2 retains the authored ball tuning.

## Shared physical controller

BallControlCommand(throttle, steering, brake, jump) is the common normalized command contract. ControlForward tracks the current camera heading. Human input captures that heading as GestureForward at the start of a movement gesture and projects its two axes onto the contact plane. That captured basis remains fixed while the same stroke or keyboard hold continues, so an automatic camera turnaround cannot bend a held reverse command into a spiral. A new touch, a resumed stroke after parked neutral, or a fresh keyboard hold adopts the current view. The captured basis parallel-transports through gravity changes. For external AI/test drivers, PathForward supplies the planning frame. SetPathForward never moves or rotates a body and does not change the human control frame.

BallController applies force to a dynamic sphere. Grounded drive also applies torque proportional to radius and applied force; the default ratio is 0.4, matching the inertia factor of a solid sphere. Contact friction and bounded slip-correction torque reconcile translation and angular momentum. Angular velocity is not assigned to fake rolling. Airborne drive has reduced authority and does not apply grounded traction torque.

Opposite movement commands oppose current momentum before producing reverse travel. Explicit brake commands oppose the contact-plane velocity. Neutral input applies no drive or lateral velocity servo; rolling resistance follows actual platform-relative velocity, never an authored path centre or direction.

Default serialized tuning after ManualGameplayUpgradeGenerator:

| Setting | Value / meaning |
| --- | --- |
| Profile acceleration / counter braking | 27 force units / 32 acceleration units for Standard; mass affects drive acceleration |
| Powered manual speed | 14 in every direction for Standard |
| General speed ceiling | 22; excess contact-plane speed produces resistance |
| Explicit boost target / acceleration | 25 / 35, only while a real boost is active |
| Grounded rolling / viscous resistance | 0.55 + 0.32 times contact speed |
| Rolling torque ratio / slip correction gain | 0.4 / 5 |
| Command response / release | Human swipes are consumed directly without a force tail; external AI retains 0.018-second smoothing |
| Air authority | 0.20 in the authored Standard profile; new profile default 0.22 |
| Maximum angular speed | 55 radians per second |
| Linear / angular damping | 0.015 / 0.025 |
| Physics step | 1/60 second |

Contact resistance is scaled by the supporting material's friction and SurfaceResistanceMultiplier. SurfaceContactResponse may set the latter from authored surface data. No high airborne drag is used as a substitute for ground rolling resistance. Existing adventure profiles retain mass/grip differences. Racing uses the same Standard physics profile for all competitors.

Legacy fields reverseSpeed, maximumLateralSpeed, lateralDamping and airSteeringAcceleration are retained for serialized compatibility; they do not impose asymmetric limits on the new manual movement. QueueForwardImpulse and WeightSwipe remain compatibility/tooling APIs. Human touch does not invoke longitudinal impulse queues.

## Ground and gravity

BallGroundDetector combines contact normals with nonalloc support queries. Support is checked relative to MovementUp, accepts valid solid track layers and rejects rival balls as ground. SupportCollider, Point, Radius, SupportBody and SupportVelocity are exposed for surface/audio/diagnostic systems. Controls use point velocity on moving and rotating supports without transform parenting.

- SetGravityDirection(Vector3 acceleration) accepts the full acceleration vector, including strength; zero/non-finite requests are ignored and magnitude is bounded to 50.
- MovementUp is the opposite normalized direction; GravityVector is the actual acceleration. ResetGravity restores Physics.gravity.
- Gravity is applied to every dynamic body physics step even while gameplay drive is inactive. Body.useGravity is disabled to prevent double gravity.
- ControlForward, the captured GestureForward and PathForward parallel-transport when gravity changes. The detector probes along the new down direction, supporting floors, walls and ceilings.
- Respawn is an explicit recovery operation: it clears velocities and commands, places the body at the checkpoint, resets gravity/support and clears touch input. Ordinary movement has no such pose writes.

## Direction-follow camera

QuestCamera defaults to AdaptiveChase. It follows position and actual support-relative travel; the sphere's rolling rotation and the route's PathForward never drive its heading. Sustained reverse travel turns the camera around to face the return path. The ball continues moving through the shared physics controller while the camera turns. Normal settings show automatic follow; fixed/manual orbit is not selectable in the player flow.

BallInput always reports CameraGesture false, CameraDelta zero and ConsumeCameraDelta zero. QuestCamera does not consume touch orbit commands. The camera parallel-transports heading through gravity changes and smooths its visible up vector. Collision probes shorten an obstructed camera arm; geometry avoidance and optional impact/landing/FOV feedback remain configurable.

AdaptiveChase begins responding in the same camera update for travel above 0.35 units/second; it has no angular engage gate or waiting timer. Heading is limited to 240 degrees/second and 1,440 degrees/second squared. Filtered support-relative velocity controls yaw; stopping retains the last useful view. Camera translation follows the interpolated ball directly, while heading, gravity-up and obstruction clearance are smoothed. Orientation is calculated from the final lens position/FOV so the ball stays at viewport x=0.5/y=0.4 through turns, speed changes, jumps, obstructions and feedback. The offset remains 5.5 metres high and 8.2 behind, with two degrees of speed FOV.

Internal Orbit/StableOrbit APIs remain for diagnostic inspection poses and retained tests; human pointer input has no mapping to them. Snap clears positional damping and travel-follow state for recovery; ResetOrbit restores the initial inspection heading. Both the main and virtual camera use a 0.25-350 clipping range to improve mobile depth precision.

## Evidence and limits

The v9 BallOnlyGesturesV9Tests fixture injects circular touchscreen motion and adds/removes a second finger while checking actual two-axis Rigidbody movement, zero camera commands and central framing. ExtraFingerNeverOrbitsAndPrimaryFingerStillMovesTheBall verifies both touch and desktop drag ownership. Retained regression coverage includes no-input rest despite route-heading changes, symmetric four-direction/diagonal drive, reverse through zero, coast-to-stop, angular/linear rolling correspondence, circular commands, 30/60/90/120 Hz pointer sampling, stationary neutral, UI ownership, gravity-relative grounding/reset, and optional stable/manual camera behavior. New v0.6.0 tests cover short and repeated actual InputSystem strokes, fast versus gentle strokes, replacement touch handoff, resumed neutral strokes, held input during camera rotation, turnaround timing/rate bounds, full-circle orbit, wall/ceiling camera transport and a real CinemachineBrain following held reverse input. Test presence is distinct from an executed passing result; consult the delivery record.

The dedicated physics scene supplies ramps, surfaces, support motion, gaps and diagnostics. Audio and new adventure-route evidence are recorded separately by their integration tests. Editor input tests do not establish phone touch comfort, real-device latency, safe areas, sustained mobile performance or the natural difficulty of new routes; those require device testing.
