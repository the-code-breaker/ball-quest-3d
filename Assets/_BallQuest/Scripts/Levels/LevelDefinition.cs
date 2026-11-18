using System;
using UnityEngine;

namespace BallQuest
{
    public enum TrackStyle { Deck, Ramp, Bridge, Ice, HighGrip, Slippery, Bouncy, Moving, Banked }
    public enum TrackSurface { Standard, Slippery, HighGrip, Moving, Bouncy, Ice }
    public enum ObstacleKind { Hammer, SlidingWall, Pendulum, MovingBridge, FallingTile, JumpPad, Booster, Conveyor, Laser, Elevator, StaticBlock, VerticalRotor, RollingHazard, PushBlock, CrushingWall, TimedDoor, CollapsingBridge, CircularPlatform, ForwardPlatform, RotatingBlade, Piston, SweepArm, RollerDrum }
    public enum PickupKind { Coin, Gem, Token }

    [Serializable]
    public sealed class TrackPiece
    {
        public Vector3 position;
        public Vector3 size = new Vector3(7, 0.8f, 16);
        public Vector3 rotation;
        public TrackStyle style;
        public bool rails = true;
        public TrackSurface surface;
        public bool alternate;
    }

    [Serializable]
    public sealed class ObstacleSpec
    {
        public ObstacleKind kind;
        public Vector3 position;
        public Vector3 size = Vector3.one;
        public Vector3 axis = Vector3.right;
        public float amplitude = 2;
        public float speed = 1;
        public float phase;
        public Vector3 rotation;
        public float warningSeconds = 1.1f;
    }

    [Serializable]
    public sealed class PickupSpec { public PickupKind kind; public Vector3 position; }

    [CreateAssetMenu(menuName = "Ball Quest/Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public int id = 1;
        public string title;
        [TextArea] public string hint;
        public int world;
        public Vector3 spawn = new Vector3(0, 1.4f, 1);
        public Vector3 finish = new Vector3(0, 1, 120);
        public Vector3[] checkpoints = Array.Empty<Vector3>();
        public TrackPiece[] tracks = Array.Empty<TrackPiece>();
        public ObstacleSpec[] obstacles = Array.Empty<ObstacleSpec>();
        public PickupSpec[] pickups = Array.Empty<PickupSpec>();
        public float parSeconds = 100;
        public float killHeight = -12;
        public bool allowJump;
        [TextArea] public string mission;
        public float[] dropStarts = Array.Empty<float>();
        public Vector3[] dropPoints = Array.Empty<Vector3>();
        public Vector3[] dropDirections = Array.Empty<Vector3>();
        public Vector3[] routePoints = Array.Empty<Vector3>();
        public float[] routeWidths = Array.Empty<float>();
        public int difficulty;
        public bool expedition;
        public string signatureMechanic;
        public Vector3[] alternateFinishes = Array.Empty<Vector3>();
        public string[] mechanics = Array.Empty<string>();
        [Header("Grand campaign compact geometry")]
        public bool streamedCampaign;
        public int environment;
        public int archetype;
        public int layoutSeed;
        public int[] roadFlags=Array.Empty<int>();
        public TrackPiece[] bonusTracks=Array.Empty<TrackPiece>();
        public ChallengeKind challenge;
        public int challengeTarget;
        public float challengeSeconds;
        public int challengeReward=50;
    }
}
