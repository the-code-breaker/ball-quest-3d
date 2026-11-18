using System;
using UnityEngine;

namespace BallQuest
{
    public enum RaceFormat { Sprint, Obstacle, Elimination, TimeTrial, Tournament }
    public enum RaceDifficulty { Easy, Medium, Hard }
    public enum RaceState { Idle, Countdown, Racing, Paused, Complete }

    [Serializable]
    public sealed class RaceRouteOption
    {
        public string id;
        public int entryRouteIndex;
        public int exitRouteIndex;
        public Vector3[] points=Array.Empty<Vector3>();
        public float minimumSpeed=8;
        public bool risky;
    }

    [CreateAssetMenu(menuName="Ball Quest/Race Track")]
    public sealed class RaceTrackDefinition : ScriptableObject
    {
        public int id=1;
        public string title;
        public LevelDefinition course;
        public Vector3[] route=Array.Empty<Vector3>();
        [Range(3,7)] public int aiCount=4;
        public float timeTarget=80;
        public int unlockLevel=1;
        public RaceFormat format=RaceFormat.Obstacle;
        public RaceRouteOption[] alternatives=Array.Empty<RaceRouteOption>();
    }

    [Serializable]
    public sealed class RaceResultEntry
    {
        public int Id;
        public string Name;
        public int Position;
        public float Seconds;
        public float Progress;
        public int Falls;
        public bool Finished;
        public bool Eliminated;
        public bool IsPlayer;
    }

    [Serializable]
    public sealed class RaceResult
    {
        [NonSerialized] public bool RewardsProcessed;
        [NonSerialized] public bool TournamentRewardsProcessed;
        public int TrackId;
        public string Title;
        public RaceFormat Format;
        public RaceDifficulty Difficulty;
        public int PlayerPosition;
        public int Competitors;
        public float Seconds;
        public int Coins;
        public int Gems;
        public int CollectedCoins;
        public int CollectedGems;
        public int CollectedTokens;
        public bool Clean;
        public bool Victory;
        public bool TimeTrialSuccess;
        public bool TournamentComplete;
        public bool TournamentVictory;
        public int TournamentPosition;
        public RaceResultEntry[] Standings=Array.Empty<RaceResultEntry>();
    }

    [Serializable]
    public sealed class TournamentStanding
    {
        public int Id;
        public string Name;
        public int Points;
        public int Finishes;
        public float Seconds;
        public int Position;
        public bool IsPlayer=>Id==0;
    }
}
