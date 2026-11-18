using UnityEngine;

namespace BallQuest
{
    public enum BallAbility { Standard, Heavy, Rubber, Magnetic, Feather, Energy }

    [CreateAssetMenu(menuName = "Ball Quest/Ball Profile")]
    public sealed class BallProfile : ScriptableObject
    {
        public string id = "standard";
        public string displayName = "Standard";
        public BallAbility ability;
        public float mass = 1f;
        public float acceleration = 27f;
        public float maxSpeed = 14f;
        public float braking = 32f;
        public float airControl = 0.22f;
        public float jumpSpeed = 6f;
        [Range(0, 1)] public float bounce = 0.08f;
        public float friction = 0.6f;
        [Header("Motion limits")]
        public float maximumSpeed = 22f;
        [HideInInspector] public float reverseSpeed = 6f;
        public float speedResistance = 8f;
        public float rollingResistance = .55f;
        [Min(0)] public float groundedViscousResistance = .32f;
        [Range(0,1)] public float rollingTorqueRatio = .4f;
        [Min(0)] public float tractionTorque = 5f;
        public float maximumAngularSpeed = 55f;
        public float linearDamping = .015f;
        public float angularDamping = .025f;
        [Header("Steering")]
        [HideInInspector] public float maximumLateralSpeed = 5f;
        public float steeringAcceleration = 28f;
        [HideInInspector] public float lateralDamping = 7f;
        [HideInInspector] public float airSteeringAcceleration = 4f;
        public float steeringResponseTime = .018f;
        public float steeringReleaseTime = .018f;
        [Header("Flicks and powered pads")]
        public float flickImpulse = 22f;
        public float maximumFlickForce = 36f;
        public float boostSpeed = 25f;
        public float boostAcceleration = 35f;
        [HideInInspector] public float brakeAngularDamping = 10f;
        [HideInInspector] public float brakeGestureDuration = .45f;
        public int unlockLevel = 1;
        [TextArea] public string description = "Balanced grip and momentum.";
    }
}
