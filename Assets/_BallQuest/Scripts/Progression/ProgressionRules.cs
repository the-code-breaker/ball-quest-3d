using System;

namespace BallQuest
{
    public static class ProgressionRules
    {
        public static int Stars(int collected, int available, int falls, float seconds, float parSeconds)
            => 1 + (available > 0 && collected >= available - available / 5 ? 1 : 0)
                 + (falls == 0 && seconds <= parSeconds ? 1 : 0);

        // Rewards are a per-level high-water mark, so replaying cannot duplicate the same earned reward.
        public static int Complete(SaveData save, int id, int catalogCount, int coins, int gems, int stars, float seconds, int tokens = 0)
        {
            if (id < 1 || id > catalogCount || id > save.unlockedLevel) throw new ArgumentOutOfRangeException(nameof(id));
            int reward=Reward(save,save.levels,id,coins,gems,stars,seconds,tokens);
            save.unlockedLevel = Math.Max(save.unlockedLevel, Math.Min(catalogCount, id + 1));
            return reward;
        }
        public static int CompleteExpedition(SaveData save,int id,int coins,int gems,int stars,float seconds,int tokens=0)
        {
            if(id<56||id>60)throw new ArgumentOutOfRangeException(nameof(id));
            if(save.expeditions==null)save.expeditions=new System.Collections.Generic.List<LevelRecord>();
            return Reward(save,save.expeditions,id,coins,gems,stars,seconds,tokens);
        }
        static int Reward(SaveData save,System.Collections.Generic.List<LevelRecord> records,int id,int coins,int gems,int stars,float seconds,int tokens)
        {
            var record = records.Find(r => r.id == id);
            if (record == null) { record = new LevelRecord { id = id }; records.Add(record); }
            coins = Math.Max(0, coins); gems = Math.Max(0, gems); tokens = Math.Max(0, tokens);
            var reward = Math.Max(0, coins - record.rewardedCoins);
            save.coins += reward;
            save.gems += Math.Max(0, gems - record.rewardedGems);
            save.tokens += Math.Max(0, tokens - record.rewardedTokens);
            record.rewardedCoins = Math.Max(record.rewardedCoins, coins);
            record.rewardedGems = Math.Max(record.rewardedGems, gems);
            record.rewardedTokens = Math.Max(record.rewardedTokens, tokens);
            record.stars = Math.Max(record.stars, Math.Max(1, Math.Min(3, stars)));
            if (seconds > 0 && (record.bestSeconds <= 0 || seconds < record.bestSeconds)) record.bestSeconds = seconds;
            return reward;
        }
    }
}
