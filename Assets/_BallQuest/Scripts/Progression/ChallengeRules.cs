using System;
namespace BallQuest
{
    public enum ChallengeKind { None, Flawless, CoinHunt, TimeTrial, Treasure, BoostRun }
    public static class ChallengeRules
    {
        public static bool Satisfied(ChallengeKind kind,int target,float limit,int coins,int falls,float seconds,int gems,int boosts)
        {
            switch(kind)
            {
                case ChallengeKind.Flawless:return falls==0;
                case ChallengeKind.CoinHunt:return coins>=Math.Max(1,target);
                case ChallengeKind.TimeTrial:return seconds<=limit;
                case ChallengeKind.Treasure:return gems>=Math.Max(1,target);
                case ChallengeKind.BoostRun:return boosts>=Math.Max(1,target);
                default:return false;
            }
        }
        public static int Claim(SaveData save,int id,int reward,bool satisfied)
        {
            var record=save.levels.Find(r=>r.id==id);
            if(!satisfied||record==null||record.challengeCompleted)return 0;
            record.challengeCompleted=true;reward=Math.Max(0,Math.Min(500,reward));save.coins+=reward;return reward;
        }
        public static string Description(ChallengeKind kind,int target,float seconds)
        {
            switch(kind)
            {
                case ChallengeKind.Flawless:return "Finish without falling";
                case ChallengeKind.CoinHunt:return "Collect "+target+" coins";
                case ChallengeKind.TimeTrial:return "Finish within "+Math.Ceiling(seconds)+" seconds";
                case ChallengeKind.Treasure:return "Find "+target+" treasure gem";
                case ChallengeKind.BoostRun:return "Ride "+target+" boost pad";
                default:return "Reach the finish";
            }
        }
    }
}
