# v0.8 control and centered camera correction

User feedback: one swipe is too sensitive; speed should accumulate through repeated strokes. The view should begin following right/left motion immediately, smoothly align behind travel, and keep the ball centered.

1. Reduce touch gain, restrain swipe-speed amplification and cap the momentum from one deliberate stroke. Repeated strokes accumulate physical speed. New turns and braking must remain available after a stroke reaches its cap. Stationary touch and sensor jitter continue to add no drive.
2. Remove the camera's 40-degree gate and 0.42-second delay. Start smooth bounded heading adjustment in the current camera update. Anchor translation to the interpolated ball position; smooth orbit and clearance rather than leaving the camera behind the ball. Target viewport x=0.5 and y=0.4 so the ball stays in the central gameplay area with road visible ahead.
3. Let a single non-UI finger steer anywhere on screen, removing the hidden right-edge camera zone. Two fingers/right mouse retain orbit. Validate actual Rigidbody speeds after one/four/eight strokes, large-stroke limits at different sample rates, stationary holds, braking and directional steering. Validate actual Cinemachine output during lateral, curved and reverse motion, jumps/slopes/gravity, centering and bounded turn rate.
4. Migrate old excessive sensitivity once, preserve progression, regenerate scenes, run focused checks then the full Unity suites, build version 0.8.0/code 8, verify the APK and provide it directly.

Scope is the input/camera correction. Existing native graphics, tracks, rewards, races and performance improvements remain part of this build. Physical-phone comfort cannot be established without a connected device.
