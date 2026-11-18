using System;
using UnityEngine;

namespace BallQuest
{
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(BallInput))]
    public sealed class BallController : MonoBehaviour
    {
        public BallProfile profile;
        public bool allowJump;
        public bool active;
        public bool Grounded { get; private set; }
        public Rigidbody Body { get; private set; }
        public BallInput Input { get; private set; }
        public BallGroundDetector GroundDetector { get; private set; }
        public bool ExternalControl { get; private set; }
        // Route direction is planning metadata only. It is never a player movement constraint.
        public Vector3 PathForward { get; private set; } = Vector3.forward;
        public Vector3 ControlForward { get; private set; } = Vector3.forward;
        public Vector3 GestureForward => humanFrameLocked ? humanControlForward : ControlForward;
        public Vector3 MovementUp { get; private set; } = Vector3.up;
        public Vector3 GravityVector { get; private set; } = new Vector3(0,-9.81f,0);
        public Vector3 GroundNormal => Grounded ? GroundDetector.Normal : MovementUp;
        public Vector3 SupportVelocity => Grounded ? GroundDetector.SupportVelocity : Vector3.zero;
        public Vector2 AppliedControl { get; private set; }
        public float SurfaceResistanceMultiplier { get; set; } = 1;
        public event Action<float> Landed;
        public event Action<float> Impacted;
        public event Action Jumped;
        float lastGrounded = -100, lastJump = -100, previousDownSpeed;
        PhysicsMaterial material;
        float boostUntil, queuedImpulse;
        Vector3 boostDirection = Vector3.forward;
        Vector2 filteredControl;
        Vector3 humanControlForward = Vector3.forward;
        bool humanFrameLocked;
        uint humanGestureSerial;
        BallControlCommand externalCommand;
        public bool Boosted => Time.time < boostUntil;
        public float CruiseSpeed => Body ? Vector3.Dot(Body.linearVelocity - SupportVelocity, PathForward) : 0;
        public float SpeedLimit => profile ? Boosted ? profile.boostSpeed : Mathf.Max(profile.maxSpeed, profile.maximumSpeed) : 22;
        public float Steering => filteredControl.x;

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); Input = GetComponent<BallInput>();
            GroundDetector = GetComponent<BallGroundDetector>();
            if (!GroundDetector) GroundDetector = gameObject.AddComponent<BallGroundDetector>();
            if (!GetComponent<SurfaceContactResponse>()) gameObject.AddComponent<SurfaceContactResponse>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxDepenetrationVelocity = 4; Body.solverIterations = 10; Body.solverVelocityIterations = 4;
            Body.sleepThreshold = .015f;
            material = new PhysicsMaterial("Ball grip") { frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Maximum };
            GetComponent<SphereCollider>().material = material;
            ResetGravity();
            if (profile) ApplyProfile(profile);
        }
        public void ApplyProfile(BallProfile value)
        {
            if (!value) return;
            profile = value;
            if (!Body) return;
            Body.mass = Mathf.Max(.1f, profile.mass);
            Body.maxAngularVelocity = Mathf.Max(10, profile.maximumAngularSpeed);
            Body.linearDamping = Mathf.Max(0, profile.linearDamping);
            Body.angularDamping = Mathf.Max(0, profile.angularDamping);
            material.dynamicFriction = material.staticFriction = Mathf.Clamp01(profile.friction);
            material.bounciness = Mathf.Clamp01(profile.bounce);
        }
        public void SetExternalControl(bool enabled)
        {
            ExternalControl = enabled; externalCommand = default; queuedImpulse = 0; filteredControl = Vector2.zero;
            humanFrameLocked = false;
            Input.ResetInput();
        }
        public void SetExternalControl(Vector2 command, bool enabled = true)
        {
            if (ExternalControl != enabled) SetExternalControl(enabled);
            SubmitControl(new BallControlCommand(command.y, command.x));
        }
        public void SubmitControl(BallControlCommand command) => externalCommand = new BallControlCommand(command.throttle, command.steering, command.brake, command.jump);
        public void SetPathForward(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, MovementUp);
            if (Finite(flat) && flat.sqrMagnitude > .0001f) PathForward = flat.normalized;
        }
        public void SetControlFrame(Vector3 forward)
        {
            Vector3 flat = Vector3.ProjectOnPlane(forward, MovementUp);
            if (Finite(flat) && flat.sqrMagnitude > .0001f) ControlForward = flat.normalized;
        }
        public void SetGravityDirection(Vector3 acceleration)
        {
            if (!Finite(acceleration) || acceleration.sqrMagnitude < .0001f) return;
            GravityVector = Vector3.ClampMagnitude(acceleration, 50);
            Vector3 nextUp = -GravityVector.normalized;
            Quaternion transport = Quaternion.FromToRotation(MovementUp,nextUp);
            ControlForward = transport * ControlForward;
            humanControlForward = transport * humanControlForward;
            PathForward = transport * PathForward;
            MovementUp = nextUp;
            if (Body) Body.useGravity = false;
            if (GroundDetector) GroundDetector.Up = MovementUp;
        }
        public void ResetGravity() => SetGravityDirection(Physics.gravity.sqrMagnitude > .0001f ? Physics.gravity : new Vector3(0,-9.81f,0));
        static bool Finite(Vector3 value) => !float.IsNaN(value.sqrMagnitude) && !float.IsInfinity(value.sqrMagnitude);

        // Explicit finite-energy command for authored/test tools and AI ramp planning; touch never calls this.
        public void QueueForwardImpulse(float impulse)
        {
            if (!profile || !active || float.IsNaN(impulse) || float.IsInfinity(impulse)) return;
            queuedImpulse = Mathf.Clamp(queuedImpulse + impulse, -profile.flickImpulse * 1.5f, profile.flickImpulse * 1.5f);
        }
        void FixedUpdate()
        {
            if (Body && !Body.isKinematic) Body.AddForce(GravityVector, ForceMode.Acceleration);
            if (!active || !profile || Body.isKinematic) return;
            bool wasGrounded = Grounded;
            Grounded = GroundDetector.Grounded && Time.time - lastJump > .16f;
            if (Grounded)
            {
                lastGrounded = Time.time;
                if (!wasGrounded && previousDownSpeed > 1.5f) Landed?.Invoke(previousDownSpeed);
            }
            previousDownSpeed = Mathf.Max(0, -Vector3.Dot(Body.linearVelocity, MovementUp));
            Vector2 humanMove = ExternalControl ? Vector2.zero : Input.ConsumeMove();
            if (!ExternalControl)
            {
                // Keep the current stroke in its initial world direction while the chase camera
                // turns behind reverse/sideways travel. A fresh stroke adopts the new view.
                if (!Input.MovementGestureActive && humanMove.sqrMagnitude < .0001f) humanFrameLocked = false;
                if (!humanFrameLocked || humanGestureSerial != Input.MovementGestureSerial)
                {
                    humanControlForward = ControlForward; humanGestureSerial = Input.MovementGestureSerial;
                    humanFrameLocked = Input.MovementGestureActive || humanMove.sqrMagnitude > .0001f;
                }
            }
            BallControlCommand command = ExternalControl ? externalCommand : new BallControlCommand(humanMove.y,humanMove.x,0,Input.ConsumeJump());
            externalCommand.jump = false;
            Vector2 desiredInput = ExternalControl ? Vector2.ClampMagnitude(new Vector2(command.steering,command.throttle),1) : humanMove;
            float response = desiredInput.sqrMagnitude < .0001f ? profile.steeringReleaseTime : profile.steeringResponseTime;
            filteredControl = ExternalControl ? Vector2.Lerp(filteredControl,desiredInput,1 - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(.001f,response))) : desiredInput;
            AppliedControl = Vector2.ClampMagnitude(filteredControl,1);
            Vector3 normal = GroundNormal;
            Vector3 forward = Vector3.ProjectOnPlane(ExternalControl ? PathForward : GestureForward, normal);
            if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(Vector3.forward,normal);
            if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(Vector3.right,normal);
            forward.Normalize();
            Vector3 side = Vector3.Cross(normal,forward).normalized;
            Vector3 relativeVelocity = Body.linearVelocity - SupportVelocity;
            Vector3 tangent = Vector3.ProjectOnPlane(relativeVelocity,normal);
            Vector3 direction = forward * filteredControl.y + side * filteredControl.x;
            float authority = Grounded ? 1 : Mathf.Clamp01(profile.airControl);
            if (command.brake > .05f || Vector3.Dot(direction,tangent) < -.1f) boostUntil = Mathf.Min(boostUntil,Time.time);
            Vector3 force = direction * (profile.acceleration * authority);
            // Only remove power that would increase speed beyond the same cap in every direction.
            if (tangent.magnitude >= profile.maxSpeed && Vector3.Dot(force,tangent) > 0)
                force -= Vector3.Project(force,tangent);
            if (!ExternalControl)
            {
                // A fast swipe may arrive as one sampled event. It may add its finite impulse
                // in this step, but cannot overshoot the same powered speed ceiling even when
                // starting at rest or steering perpendicular to existing motion. Limit force,
                // never assign velocity. Preserve pre-existing gravity/boost overspeed.
                Vector3 predicted=tangent+force*(Time.fixedDeltaTime/Body.mass);
                float ceiling=Mathf.Max(profile.maxSpeed,tangent.magnitude);
                if(predicted.sqrMagnitude>ceiling*ceiling)
                    force=(Vector3.ClampMagnitude(predicted,ceiling)-tangent)*(Body.mass/Time.fixedDeltaTime);
            }
            Drive(force,normal,Grounded);
            if (Grounded && direction.sqrMagnitude > .001f && Vector3.Dot(direction,tangent) < -.1f)
            {
                float extraBrake = Mathf.Max(0,profile.braking - profile.acceleration / Body.mass);
                Drive(-tangent.normalized * (Mathf.Min(extraBrake,tangent.magnitude / Time.fixedDeltaTime) * Body.mass * Mathf.Min(1,direction.magnitude)),normal,true);
            }
            // No neutral steering servo: resistance follows actual motion, never a route centre/heading.
            if (Grounded && tangent.sqrMagnitude > .0001f)
            {
                float surface = 1;
                var support = GroundDetector.SupportCollider;
                if (support && support.sharedMaterial) surface = Mathf.Lerp(.2f,1.3f,Mathf.Clamp01(support.sharedMaterial.dynamicFriction));
                float resistance = Mathf.Max(0,profile.rollingResistance) + Mathf.Max(0,profile.groundedViscousResistance) * tangent.magnitude;
                resistance = Mathf.Min(resistance * surface * Mathf.Clamp(SurfaceResistanceMultiplier,.02f,10),tangent.magnitude / Time.fixedDeltaTime);
                Drive(-tangent.normalized * (resistance * Body.mass),normal,true);
                // Traction correction supplies bounded torque, rather than assigning angular velocity.
                Vector3 slip = tangent - Vector3.Cross(Body.angularVelocity,normal) * GroundDetector.Radius;
                Vector3 tractionTorque = Vector3.Cross(normal,slip) * (profile.tractionTorque * Body.mass * GroundDetector.Radius);
                Body.AddTorque(Vector3.ClampMagnitude(tractionTorque,profile.steeringAcceleration * Body.mass * GroundDetector.Radius),ForceMode.Force);
            }
            if (command.brake > 0 && tangent.sqrMagnitude > .0001f && Grounded)
            {
                float amount = Mathf.Min(Mathf.Max(1,profile.braking) * command.brake,tangent.magnitude / Time.fixedDeltaTime);
                Drive(-tangent.normalized * (amount * Body.mass),normal,true);
            }
            if (Mathf.Abs(queuedImpulse) > .0001f)
            {
                float step = Mathf.MoveTowards(0,queuedImpulse,profile.maximumFlickForce * Time.fixedDeltaTime);
                if (Mathf.Sign(step) * Vector3.Dot(relativeVelocity,forward) >= SpeedLimit) { queuedImpulse = 0; step = 0; }
                queuedImpulse -= step;
                Drive(forward * (step / Time.fixedDeltaTime * authority),normal,Grounded);
            }
            if (Boosted)
            {
                Vector3 boost = Vector3.ProjectOnPlane(boostDirection,normal).normalized;
                float speed = Vector3.Dot(relativeVelocity,boost);
                if (speed < profile.boostSpeed)
                    Drive(boost * (Mathf.Min(profile.boostAcceleration,(profile.boostSpeed - speed) / Time.fixedDeltaTime) * Body.mass * authority),normal,Grounded);
            }
            if (tangent.magnitude > SpeedLimit)
                Drive(-tangent.normalized * ((tangent.magnitude - SpeedLimit) * profile.speedResistance * Body.mass),normal,Grounded);
            if (command.jump && allowJump && Time.time - lastGrounded < .1f && Time.time - lastJump > .25f)
            {
                Body.AddForce(MovementUp * Mathf.Max(0,profile.jumpSpeed - Vector3.Dot(Body.linearVelocity,MovementUp)),ForceMode.VelocityChange);
                lastJump = Time.time; Grounded = false; GroundDetector.Ignore(.18f); Jumped?.Invoke();
            }
            if (profile.ability == BallAbility.Feather && Vector3.Dot(Body.linearVelocity,MovementUp) < -3)
                Body.AddForce(MovementUp * ((-3 - Vector3.Dot(Body.linearVelocity,MovementUp)) / Time.fixedDeltaTime),ForceMode.Acceleration);
        }
        void Drive(Vector3 force, Vector3 normal, bool withTorque)
        {
            Body.AddForce(force,ForceMode.Force);
            if (withTorque) Body.AddTorque(Vector3.Cross(normal,force) * (GroundDetector.Radius * profile.rollingTorqueRatio),ForceMode.Force);
        }
        public void Respawn(Vector3 point)
        {
            boostUntil = 0; queuedImpulse = 0; filteredControl = AppliedControl = Vector2.zero; externalCommand = default;
            humanFrameLocked = false;
            bool frozen = Body.isKinematic; if (frozen) Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.position = point; Body.rotation = Quaternion.identity;
            if (frozen) Body.isKinematic = true;
            lastGrounded = -100; previousDownSpeed = 0; Grounded = false;
            ResetGravity(); GroundDetector.ResetSupport(); Input.ResetInput(); Physics.SyncTransforms();
        }
        public void Boost(float seconds = 2.4f) => Boost(PathForward,seconds);
        public void Boost(Vector3 direction, float seconds = 2.4f)
        {
            Vector3 tangent = Vector3.ProjectOnPlane(direction,MovementUp);
            boostDirection = tangent.sqrMagnitude > .001f ? tangent.normalized : ControlForward;
            boostUntil = Mathf.Max(boostUntil,Time.time + Mathf.Max(.05f,seconds));
        }
        void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount == 0 || Vector3.Dot(collision.GetContact(0).normal,MovementUp) > .6f) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed > 2) Impacted?.Invoke(Mathf.Min(20,speed));
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
