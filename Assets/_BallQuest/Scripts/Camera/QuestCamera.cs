using Unity.Cinemachine;
using UnityEngine;

namespace BallQuest
{
    public enum BallCameraMode { StableOrbit, AdaptiveChase }

    /// <summary>Independent orbit frame. Sphere spin and authored route headings never drive this camera.</summary>
    public sealed class QuestCamera : CinemachineExtension
    {
        public BallController target;
        public BallCameraMode mode = BallCameraMode.AdaptiveChase;
        public Vector3 offset = new Vector3(0,5.5f,-8.2f);
        [Min(.01f)] public float damping = .14f;
        [HideInInspector] public float lookAhead = 2.8f;
        [HideInInspector] public float fastLookAhead = 3.8f;
        [Range(.3f,.5f)] public float ballViewportHeight = .4f;
        [Range(35,80)] public float baseFieldOfView = 50;
        [Range(0,15)] public float speedFieldOfView = 2;
        [Range(0,8)] public float crowdedFieldOfView = 3;
        [Range(30,360)] public float orbitSensitivity = 180;
        [HideInInspector] public float adaptiveDelay;
        [HideInInspector] public float adaptiveHysteresis;
        [Min(.1f)] public float adaptiveSpeedThreshold = .35f;
        [Range(30,360)] public float adaptiveTurnSpeed = 240;
        [Min(30)] public float adaptiveTurnAcceleration = 1440;
        [Min(1)] public float headingResponse = 10;
        [Min(.05f)] public float nearClipPlane = .25f;
        [Min(100)] public float farClipPlane = 350;
        public bool effectsEnabled = true, speedEffects = true, landingFeedback = true, impactFeedback = true, collisionAvoidance = true;
        Vector3 visualTravel, heading = Vector3.forward, cameraUp = Vector3.up, lastGravityUp = Vector3.up;
        float pitch, landing, impact, manualGrace, turnVelocity;
        float lastTurnSign = 1, armLength;
        bool snap = true;
        int nearby;
        BallController observed;
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly Collider[] overlaps = new Collider[8];
        public Vector3 Forward => heading;
        public void Snap() { snap = true; visualTravel = Vector3.zero; landing = impact = 0; armLength = 0; ResetTravelFollow(); }
        void ResetTravelFollow() { turnVelocity = 0; }
        public void ResetOrbit()
        {
            heading = Vector3.forward; cameraUp = lastGravityUp = Vector3.up; pitch = 0; manualGrace = 0; lastTurnSign = 1; Snap();
        }
        public void Orbit(Vector2 normalizedDelta)
        {
            if (normalizedDelta.sqrMagnitude < .0000001f) return;
            Vector3 up = target ? target.MovementUp : Vector3.up;
            heading = Quaternion.AngleAxis(normalizedDelta.x * orbitSensitivity,up) * heading;
            heading = Vector3.ProjectOnPlane(heading,up).normalized;
            pitch = Mathf.Clamp(pitch - normalizedDelta.y * orbitSensitivity,-12,35);
            manualGrace = 2; ResetTravelFollow();
        }
        public void Landing(float force) { if (effectsEnabled && landingFeedback) landing = Mathf.Min(.06f,force * .004f); }
        public void Impact(float speed) { if (effectsEnabled && impactFeedback) impact = Mathf.Min(.025f,speed * .0015f); }
        public void SetRaceContext(int nearbyCompetitors) => nearby = Mathf.Clamp(nearbyCompetitors,0,7);

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,CinemachineCore.Stage stage,ref CameraState state,float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || !target) return;
            bool instant = snap || deltaTime < 0;
            float dt = Mathf.Max(0,deltaTime);
            Vector3 gravityUp = target.MovementUp;
            // Parallel-transport the orbit through a gravity change; never pick a world-axis flip at a wall.
            if (Vector3.Dot(lastGravityUp,gravityUp) < .999999f)
            {
                Quaternion transport=Quaternion.FromToRotation(lastGravityUp,gravityUp);
                heading = transport * heading;
                visualTravel = transport * visualTravel;
                lastGravityUp = gravityUp;
            }
            heading = Vector3.ProjectOnPlane(heading,gravityUp).normalized;
            if (heading.sqrMagnitude < .001f) heading = target.ControlForward;
            cameraUp = instant ? gravityUp : Vector3.Slerp(cameraUp,gravityUp,1 - Mathf.Exp(-dt * 8)).normalized;
            Vector3 travel = target.Body ? Vector3.ProjectOnPlane(target.Body.linearVelocity - target.SupportVelocity,gravityUp) : Vector3.zero;
            visualTravel = instant ? travel : Vector3.Lerp(visualTravel,travel,1-Mathf.Exp(-dt*9));
            float speed = visualTravel.magnitude;
            manualGrace = Mathf.Max(0,manualGrace - dt);
            FollowTravel(visualTravel,travel.magnitude,gravityUp,dt);
            target.SetControlFrame(heading);
            Vector3 viewHeading = Vector3.ProjectOnPlane(heading,cameraUp);
            if (viewHeading.sqrMagnitude < .001f) viewHeading = Vector3.ProjectOnPlane(target.ControlForward,cameraUp);
            if (viewHeading.sqrMagnitude < .001f) viewHeading = Vector3.Cross(cameraUp,Vector3.right);
            viewHeading.Normalize();
            Vector3 side = Vector3.Cross(cameraUp,viewHeading).normalized;
            Vector3 ball = target.transform.position;
            Vector3 cameraOffset = side * offset.x + cameraUp * offset.y + viewHeading * offset.z;
            cameraOffset = Quaternion.AngleAxis(pitch,side) * cameraOffset;
            // Translation follows the interpolated ball immediately. World-space position
            // damping and sideways velocity prediction used to leave it off-centre in turns.
            // Smooth heading, gravity-up and obstruction distance instead of the ball anchor.
            Vector3 desired = ball + cameraOffset;
            if (collisionAvoidance)
            {
                Vector3 pivot = ball + cameraUp * .6f, ray = desired - pivot;
                int count = Physics.SphereCastNonAlloc(pivot,.25f,ray.normalized,hits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
                float distance = ray.magnitude;
                for (int i=0;i<count;i++)
                {
                    var hit=hits[i];
                    if (!hit.collider || hit.collider.attachedRigidbody && hit.collider.attachedRigidbody.GetComponent<BallController>()) continue;
                    distance = Mathf.Min(distance,Mathf.Max(.65f,hit.distance - .2f));
                }
                // Pull in immediately for an obstruction, then recover the camera arm smoothly.
                armLength = instant || armLength<=0 ? distance : distance<armLength ? distance : Mathf.Lerp(armLength,distance,1-Mathf.Exp(-dt*5));
                desired = pivot + ray.normalized * armLength;
                int overlapCount = Physics.OverlapSphereNonAlloc(desired,.35f,overlaps,~0,QueryTriggerInteraction.Ignore);
                for (int i=0;i<overlapCount;i++)
                {
                    var other = overlaps[i].attachedRigidbody;
                    if (other && other != target.Body && other.GetComponent<BallController>()) { desired += cameraUp * .65f; break; }
                }
            }
            Vector3 position = desired; snap = false;
            if (collisionAvoidance)
            {
                Vector3 pivot=ball+cameraUp*.6f, arm=position-pivot;
                int count=Physics.SphereCastNonAlloc(pivot,.22f,arm.normalized,hits,arm.magnitude,~0,QueryTriggerInteraction.Ignore);
                float clear=arm.magnitude;
                for(int i=0;i<count;i++)
                {
                    var hit=hits[i];
                    if(!hit.collider || hit.collider.attachedRigidbody&&hit.collider.attachedRigidbody.GetComponent<BallController>())continue;
                    clear=Mathf.Min(clear,Mathf.Max(.65f,hit.distance-.15f));
                }
                if(clear<arm.magnitude)position=pivot+arm.normalized*clear;
            }
            landing = Mathf.MoveTowards(landing,0,dt * .8f); impact = Mathf.MoveTowards(impact,0,dt * .35f);
            float shake = effectsEnabled && impactFeedback ? Mathf.Sin(Time.time * 49) * impact : 0;
            state.RawPosition = position - cameraUp * (effectsEnabled && landingFeedback ? landing : 0) + side * shake;
            var lens = state.Lens;
            // The source camera's clipping planes are replaced by the virtual lens. Keep a useful
            // mobile depth range here too; a tiny near plane magnifies road/shadow depth shimmer.
            lens.NearClipPlane = Mathf.Max(.05f,nearClipPlane);
            lens.FarClipPlane = Mathf.Max(lens.NearClipPlane + 100,farClipPlane);
            float fov = baseFieldOfView + (effectsEnabled && speedEffects ? Mathf.Clamp01(speed / 25) * speedFieldOfView : 0) + Mathf.Clamp01(nearby / 4f) * crowdedFieldOfView;
            lens.FieldOfView = instant ? fov : Mathf.Lerp(lens.FieldOfView,fov,1 - Mathf.Exp(-dt * 4)); state.Lens = lens;
            // Aim from the actual, collision-cleared lens position. This fixes horizontal
            // composition every frame, including sideways travel, jumps and camera shake.
            // A small pitch above the ball reserves room for upcoming road while keeping
            // the sphere at one consistent position in portrait view as FOV changes.
            float framingPitch = Mathf.Atan((1-2*ballViewportHeight)*Mathf.Tan(lens.FieldOfView*Mathf.Deg2Rad*.5f))*Mathf.Rad2Deg;
            state.RawOrientation = Quaternion.LookRotation(ball-state.RawPosition,cameraUp)*Quaternion.Euler(-framingPitch,0,0);
        }
        void FollowTravel(Vector3 travel,float speed,Vector3 up,float dt)
        {
            if (dt <= 0) return;
            if (mode != BallCameraMode.AdaptiveChase || !target.active || manualGrace > 0 || speed < adaptiveSpeedThreshold)
            {
                ResetTravelFollow(); return;
            }
            float angle = Vector3.SignedAngle(heading,travel,up);
            // At exactly 180 degrees either arc is valid. Retain one direction rather than
            // allowing minute contact velocity changes to alternate left/right every frame.
            if (Mathf.Abs(angle) > 175) angle = Mathf.Abs(angle) * lastTurnSign;
            else if (Mathf.Abs(angle) > 1) lastTurnSign = Mathf.Sign(angle);
            // Every deliberate heading change starts in this update. A delayed 40-degree
            // engage gate left the camera looking across the road during ordinary turns.
            // Low-pass travel and the at-rest speed threshold reject physical contact noise.
            if (travel.sqrMagnitude < .0001f) { ResetTravelFollow(); return; }
            if (Mathf.Abs(angle) < .05f)
            {
                heading = travel.normalized; ResetTravelFollow(); return;
            }
            float acceleration = Mathf.Max(30,adaptiveTurnAcceleration);
            float desiredSpeed = Mathf.Sign(angle) * Mathf.Min(adaptiveTurnSpeed,Mathf.Min(Mathf.Abs(angle)*headingResponse,Mathf.Sqrt(2 * acceleration * Mathf.Abs(angle))));
            turnVelocity = Mathf.MoveTowards(turnVelocity,desiredSpeed,acceleration * dt);
            float step = turnVelocity * dt;
            if (Mathf.Sign(step) == Mathf.Sign(angle) && Mathf.Abs(step) >= Mathf.Abs(angle))
            {
                heading = travel.normalized; ResetTravelFollow();
            }
            else heading = (Quaternion.AngleAxis(step,up) * heading).normalized;
        }
        protected override void OnEnable() { base.OnEnable(); observed = null; }
        void LateUpdate()
        {
            if (observed != target)
            {
                if (observed) observed.Impacted -= Impact;
                observed = target;
                if (observed) { observed.Impacted += Impact; ResetOrbit(); }
            }
        }
        void OnDisable() { if (observed) observed.Impacted -= Impact; observed = null; }
    }
}
