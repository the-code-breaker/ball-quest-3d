using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BallQuest
{
    public enum BallTouchMode { RelativeDrag, GestureVelocity }

    /// <summary>Samples only input. Touch never changes a body, path, speed or camera transform.</summary>
    public sealed class BallInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 CameraDelta { get; private set; }
        public float sensitivity = 1;
        [HideInInspector] public bool joystickControls;
        public bool active;
        public BallTouchMode touchMode = BallTouchMode.GestureVelocity;
        [Range(.025f,.4f)] public float dragRadius = .06f;
        [Min(.01f)] public float velocitySensitivity = .65f;
        [Range(.02f,.2f)] public float maximumStrokeDrive = .09f;
        [Min(0)] public float inputSmoothing = .012f;
        [Min(.005f)] public float velocitySmoothing = .025f;
        [Min(.02f)] public float stationaryNeutralDelay = .11f;
        [Range(.001f,.1f)] public float motionDeadZone = .006f;
        [Range(.0003f,.006f)] public float positionalDeadZone = .0015f;
        // Legacy serialized field; single-finger control now uses the full non-UI screen.
        [HideInInspector] public float cameraRegionStart = .84f;
        // Retained serialized property for older authoring assets.
        [Min(.1f)] public float steeringVelocitySensitivity = .7f;
        public bool PointerHeld { get; private set; }
        public bool MovementGestureActive => PointerHeld || keyboardHeld;
        public uint MovementGestureSerial { get; private set; }
        public bool CameraGesture { get; private set; }
        public float SteeringTarget => Move.x;
        bool tracking, heldPreviously, jump, keyboardHeld;
        int pointerId = int.MinValue;
        int heldPointerId = int.MinValue;
        Vector2 anchor, previousPosition, filtered, gestureVelocity;
        Vector2 pendingDrive, keyboardMove;
        Vector2 strokeDirection, turnTravel;
        float strokeDriveSpent;
        float sampleDuration;
        public Vector2 PendingDrive => pendingDrive;
        float stillTime;
        PointerEventData pointerData;
        EventSystem pointerSystem;
        readonly List<RaycastResult> hits = new List<RaycastResult>(16);
        readonly HashSet<int> uiTouches = new HashSet<int>();

        void Update()
        {
            if (!active || Time.timeScale == 0) { ResetInput(); return; }
            Vector2 key = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                key.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                key.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                if (keyboard.spaceKey.wasPressedThisFrame) jump = true;
            }
            bool keyDown = key.sqrMagnitude > .0001f;
            if (keyDown && !keyboardHeld) MovementGestureSerial++;
            keyboardHeld = keyDown;
            int count = 0, id = -1;
            Vector2 position = default;
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    int touchId = touch.touchId.ReadValue();
                    if (!touch.press.isPressed) { uiTouches.Remove(touchId); continue; }
                    Vector2 point = touch.position.ReadValue();
                    if (touch.press.wasPressedThisFrame && OverUI(point)) uiTouches.Add(touchId);
                    if (uiTouches.Contains(touchId)) continue;
                    count++;
                    if (id == -1 || touch.touchId.ReadValue() == pointerId) { id = touch.touchId.ReadValue(); position = point; }
                }
            }
            // Preserve the active movement finger when another finger touches. Extra
            // fingers never take camera ownership or teleport the movement anchor.
            if (count > 0) count = 1;
            if (count == 0 && Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed) { count = 1; id = -2; }
            }
            bool pressed = count == 1;
            CameraGesture = false; CameraDelta = Vector2.zero;
            bool down = pressed && (!heldPreviously || id != heldPointerId);
            heldPreviously = pressed;
            heldPointerId = pressed ? id : int.MinValue;
            if (down && !OverUI(position))
            {
                BeginPointer(position, id);
            }
            if (!pressed || pointerId != id) EndPointer();
            if (tracking) SamplePointer(position, Time.unscaledDeltaTime, Screen.width, Screen.height);
            else Move = Vector2.zero;
            keyboardMove = Vector2.ClampMagnitude(key,1);
            if (keyboardMove.sqrMagnitude > 0) { Move = keyboardMove; pendingDrive = Vector2.zero; }
        }
        public Vector2 ConsumeMove()
        {
            // A pointer contributes a finite distance-derived force-time budget. Consume it
            // once: a second physics tick must never replay the last render-frame command.
            Vector2 value = pendingDrive / Mathf.Max(.001f,Time.fixedDeltaTime);
            pendingDrive = Vector2.zero;
            return keyboardHeld ? keyboardMove : value;
        }
        // Compatibility for diagnostic callers: human pointers control only the ball.
        public Vector2 ConsumeCameraDelta() => Vector2.zero;
        public void BeginPointer(Vector2 position, int id = -2)
        {
            tracking = true; pointerId = id; anchor = previousPosition = position;
            MovementGestureSerial++;
            ResetStrokeBudget();
            stillTime = sampleDuration = 0; Move = filtered = gestureVelocity = pendingDrive = Vector2.zero; PointerHeld = true;
        }
        public void SamplePointer(Vector2 position, float duration, float width, float height)
        {
            if (!tracking) return;
            float dt = Mathf.Max(.0001f, duration), extent = Mathf.Max(1, Mathf.Min(width, height));
            sampleDuration += dt;
            Vector2 delta = (position - anchor) / extent;
            float threshold = Mathf.Max(.0003f,positionalDeadZone);
            if (delta.sqrMagnitude <= threshold * threshold)
            {
                // Spatial hysteresis rejects stationary finger jitter without accumulating
                // alternating tiny deltas into energy. There is no smoothed force tail.
                stillTime += dt; Move = filtered = Vector2.zero; previousPosition = position; return;
            }
            if (stillTime >= stationaryNeutralDelay) { MovementGestureSerial++; ResetStrokeBudget(); }
            float elapsed = Mathf.Max(.001f,stillTime >= stationaryNeutralDelay ? dt : sampleDuration);
            float speed = delta.magnitude / elapsed;
            float gain = touchMode == BallTouchMode.GestureVelocity ? Mathf.Lerp(.85f,1.15f,Mathf.Clamp01(speed/1.5f)) : .9f;
            // A deliberate change of swipe direction starts a new energy budget, without
            // changing the stroke's captured world frame while the camera turns. Require
            // coherent travel so tiny alternating sensor samples cannot reset the cap.
            if(strokeDirection.sqrMagnitude<.001f)strokeDirection=delta.normalized;
            else if(Vector2.Dot(delta.normalized,strokeDirection)<.5f)
            {
                if(turnTravel.sqrMagnitude>0&&Vector2.Dot(turnTravel.normalized,delta.normalized)<.5f)turnTravel=Vector2.zero;
                turnTravel+=delta;
                if(turnTravel.magnitude>=Mathf.Max(.006f,threshold*3))
                {strokeDriveSpent=0;strokeDirection=turnTravel.normalized;turnTravel=Vector2.zero;}
            }
            else turnTravel=Vector2.zero;
            Vector2 contribution = Vector2.ClampMagnitude(delta,.16f) * (velocitySensitivity * Mathf.Clamp(sensitivity,.4f,2.5f) * gain);
            float strokeLimit=Mathf.Clamp(maximumStrokeDrive,.02f,.2f);
            contribution=Vector2.ClampMagnitude(contribution,Mathf.Max(0,strokeLimit-strokeDriveSpent));
            strokeDriveSpent+=contribution.magnitude;
            // Each stroke adds a modest amount of momentum. Repeated accepted strokes,
            // rather than one long drag or one oversized event, build travelling speed.
            pendingDrive = Vector2.ClampMagnitude(pendingDrive + contribution,strokeLimit);
            Move = Vector2.ClampMagnitude(contribution / elapsed,1);
            anchor = previousPosition = position; sampleDuration = stillTime = 0;
        }
        void ResetStrokeBudget() { strokeDriveSpent=0;strokeDirection=turnTravel=Vector2.zero; }
        public void EndPointer() { tracking = false; PointerHeld = false; Move = filtered = gestureVelocity = Vector2.zero; stillTime = 0; }
        bool OverUI(Vector2 position)
        {
            if (!EventSystem.current) return false;
            if (pointerData == null || pointerSystem != EventSystem.current)
            {
                pointerSystem = EventSystem.current; pointerData = new PointerEventData(pointerSystem);
            }
            pointerData.Reset(); pointerData.position = position;
            hits.Clear(); pointerSystem.RaycastAll(pointerData, hits); return hits.Count > 0;
        }
        public void RequestJump() { if (active) jump = true; }
        // Compatibility helpers for tooling; human movement no longer consumes longitudinal swipe impulses.
        public static Vector2 WeightSwipe(Vector2 normalizedTravel, float duration)
        {
            float speed = normalizedTravel.magnitude / Mathf.Max(.001f, duration);
            return normalizedTravel * Mathf.Lerp(.55f,1.8f,Mathf.Sqrt(Mathf.Clamp01(speed / 4)));
        }
        public Vector2 ConsumeSwipe() => Vector2.zero;
        public bool ConsumeJump() { bool result = jump; jump = false; return result; }
        public void ResetInput()
        {
            EndPointer(); jump = false; heldPreviously = false; keyboardHeld = false; CameraGesture = false; CameraDelta = Vector2.zero;
            pendingDrive = keyboardMove = Vector2.zero; sampleDuration = 0;
            ResetStrokeBudget();
            heldPointerId = int.MinValue; uiTouches.Clear();
        }
        void OnDisable() => ResetInput();
    }
}
