# Swipe and camera implementation, v0.7

This revision replaces the previous replayable velocity request with a finite movement budget. The focused Unity run passed all 31 input/camera cases in [SwipeCamera-v7.xml](../TestResults/SwipeCamera-v7.xml), including the nine new acceptance cases. Full game/build validation is recorded separately by the root delivery report; v0.6 test results are historical.

## Why holding could feel powered

The previous ConsumeMove returned the last Move value when physics ran without a new render sample. RelativeDrag also retained a stick displacement, and input/controller smoothing created a force tail after movement stopped. Tiny sensor jitter could repeatedly postpone its neutral timer. Increasing force did not remove these mechanisms.

## Pointer contract

Each accepted change of screen position contributes normalized distance times sensitivity times a bounded swipe-speed gain. The budget is consumed once by physics as AddForce and matching rolling AddTorque. No pointer contribution is replayed on subsequent physics ticks. A stationary sample immediately has zero preview request, and one prior legitimate sample can remain pending only until its next consume. Neither stored touch mode supplies held-stick force.

Spatial hysteresis is 0.0015 times the smaller screen dimension: 1.62 pixels at 1080 pixels. Positions within that radius of the last accepted point contribute no energy; deliberate slow travel accumulates until it crosses the radius. This rejects bounded stationary jitter while retaining small intentional adjustments. A new touch clears old position history. Camera gestures and paused input discard pending movement. Keyboard and AI commands remain explicit continuous controls.

Responsive swipe uses base gain 2 and a speed multiplier from 0.75 to 1.55 as speed rises from zero to 1.5 normalized screen extents per second. Precision swipe uses a constant 0.9 multiplier. Thus equal-distance strokes can deliver different finite momentum based on deliberate swipe speed; repeat strokes add momentum within the physical speed limit. A pending budget is bounded to 0.6 normalized force-seconds and consumed once. The controller bounds the force's predicted planar velocity to the powered limit, including a sample from rest or perpendicular to existing motion. It preserves pre-existing gravity/boost overspeed and never writes velocity to enforce the cap. Ordinary drive remains 27 force units and actual mass affects response. Rolling resistance, coasting, collision, gravity and torque remain physical.

The sample interval is accumulated between accepted positions, so sparse 60 Hz touch samples are not interpreted as one-frame spikes at 120 Hz rendering. Contributions are distance-derived, not a held velocity estimate. Catch-up physics ticks consume no extra energy after the budget is empty. At any sample rate, unaccepted residual displacement is bounded by the spatial threshold. A long neutral interval starts a fresh movement stroke and adopts the current camera frame.

## Camera contract

The default adaptive camera now holds heading through small lateral corrections: 40-degree engage threshold and 0.42-second sustained direction. Deliberate reverse travel uses a 0.2-second engage delay, then a bounded 135 degrees/second turn with acceleration/deceleration. Each movement stroke keeps its initial control frame during that rotation; a new stroke uses the current view.

The camera sits 5.5 m high and 8.2 m behind, with 2.8-3.8 m look-ahead and at most 2 degrees of speed FOV change. Filtered support-relative travel drives visual prediction; its frame transports with changing gravity. Obstruction checks pull the camera in immediately and let it return gradually, including a final check after position smoothing. Landing/impact movement is restrained to avoid hiding geometry beneath camera shake.

## Acceptance evidence to collect

SwipeOnlyV7Tests checks actual touchscreen holding for at least five seconds in both modes, parked holding after a swipe, bounded sensor jitter, pause/resume while held, one-time consumption across catch-up ticks, and equal-distance strokes delivered in 30, six and one sample. The single-sample case specifically guards against a request cap making an extremely fast swipe weaker. CameraFeelV7Tests checks lateral-heading stability, portrait projection under speed changes, and obstruction recovery. Existing pointer safety, physical controls, camera turnaround, gravity and full course/race tests remain required. Measurements are written to TestResults/swipe-only-v7.json; passing compilation alone is not input validation.

The executed focused tests recorded zero horizontal speed from stationary holding, zero horizontal speed from bounded jitter, and exactly zero drive while parked after a swipe. A 100-pixel stroke on a 1000-pixel reference width produced 3.990 m/s in 30 samples, 6.742 m/s in six samples, and 8.319 m/s in one sample. The parked post-swipe case coasted from 5.814 m/s to 0.00120 m/s without new drive. [Swipe-only measurements](../TestResults/swipe-only-v7.json).

The retained actual-pointer fixture recorded 3.816 m/s after its first short stroke and 12.512 m/s after four strokes, with 92.60% speed retained after 0.2 seconds of release. Six faster strokes reached 13.652 m/s versus 1.940 m/s for one gentle stroke over the same 1.600-second window. Opposing continuous force crossed through zero from 14.289 m/s in 0.433 seconds. [Pointer measurements](../TestResults/responsive-movement-v7.json). These are Unity Editor Rigidbody measurements; physical phone touch comfort is not established by them.
