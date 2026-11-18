using System;
using System.Collections.Generic;

namespace BallQuest
{
    [Serializable]
    public sealed class GameSettings
    {
        public float music = 0.45f;
        public float sfx = 0.8f;
        public float sensitivity = 1f;
        public int quality = 1;
        public bool joystickControls;
        public bool vibration = true;
        public bool cameraEffects = true;
        public int touchMode = 1;
        public int cameraMode = 1;
        public string language = "en";
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public int id;
        public int stars;
        public float bestSeconds;
        public int rewardedCoins;
        public int rewardedGems;
        public int rewardedTokens;
        public bool challengeCompleted;
    }

    [Serializable]
    public sealed class RaceRecord
    {
        public int id;
        public int format;
        public int bestPlace = 99;
        public int rewardedCoins;
        public int rewardedGems;
        public int wins;
        public int rewardedTokens;
        public float bestSeconds;
    }

    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 11;
        public int version = CurrentVersion;
        public int unlockedLevel = 1;
        public int coins;
        public int gems;
        public int tokens;
        public int falls;
        public string selectedSkin = "classic-red";
        public string selectedBall = "standard";
        public List<string> skins = new List<string> { "classic-red", "ocean-blue", "brushed-steel", "carbon-rubber" };
        public List<string> balls = new List<string> { "standard" };
        public List<LevelRecord> levels = new List<LevelRecord>();
        public List<LevelRecord> expeditions = new List<LevelRecord>();
        public GameSettings settings = new GameSettings();
        public int lifetimeCoins;
        public int lifetimeBoosts;
        public int lifetimeDrops;
        public int flawlessFinishes;
        public float lifetimeDistance;
        public int dailyIndex;
        public string dailyClaimDate = "";
        public string spinClaimDate = "";
        public int lifetimeSpins;
        public List<string> claimedMissions = new List<string>();
        public int unlockedRace = 1;
        public int racesCompleted;
        public int raceVictories;
        public int tournamentsWon;
        public List<RaceRecord> races = new List<RaceRecord>();

        public void Validate()
        {
            if (version > CurrentVersion) throw new InvalidOperationException("Save was created by a newer game version.");
            bool upgradeResponsiveControls = version < 9;
            bool addMaterialBalls=version<10;
            // Version 1 lacked per-level reward records. Preserve balances and safely initialize records.
            version = CurrentVersion;
            if(addMaterialBalls){if(skins==null)skins=new List<string>();foreach(string id in new[]{"brushed-steel","carbon-rubber"})if(!skins.Contains(id))skins.Add(id);}
            unlockedLevel = Math.Max(1, Math.Min(1000, unlockedLevel));
            coins = Math.Max(0, Math.Min(10000000, coins));
            gems = Math.Max(0, Math.Min(10000000, gems));
            tokens = Math.Max(0, Math.Min(10000000, tokens));
            falls = Math.Max(0, falls);
            lifetimeCoins=Math.Max(0,lifetimeCoins);lifetimeBoosts=Math.Max(0,lifetimeBoosts);
            lifetimeDrops=Math.Max(0,lifetimeDrops);flawlessFinishes=Math.Max(0,flawlessFinishes);
            lifetimeDistance=ClampFinite(lifetimeDistance,0,100000000,0);
            dailyIndex=Math.Max(0,Math.Min(6,dailyIndex));lifetimeSpins=Math.Max(0,lifetimeSpins);
            if(claimedMissions==null)claimedMissions=new List<string>();
            if(dailyClaimDate==null)dailyClaimDate="";if(spinClaimDate==null)spinClaimDate="";
            unlockedRace=Math.Max(1,Math.Min(120,unlockedRace));
            racesCompleted=Math.Max(0,racesCompleted);raceVictories=Math.Max(0,raceVictories);tournamentsWon=Math.Max(0,tournamentsWon);
            if(races==null)races=new List<RaceRecord>();
            var raceKeys=new HashSet<int>();
            races.RemoveAll(r=>r==null||r.id<1||r.id>10000||r.format<0||r.format>4||!raceKeys.Add(r.id*10+r.format));
            foreach(var record in races)
            {
                record.bestPlace=Math.Max(1,Math.Min(99,record.bestPlace));record.wins=Math.Max(0,record.wins);
                record.rewardedCoins=Math.Max(0,record.rewardedCoins);record.rewardedGems=Math.Max(0,record.rewardedGems);record.rewardedTokens=Math.Max(0,record.rewardedTokens);
                record.bestSeconds=ClampFinite(record.bestSeconds,0,86400,0);
            }
            if (skins == null) skins = new List<string>();
            if (balls == null) balls = new List<string>();
            if (!skins.Contains("classic-red")) skins.Add("classic-red");
            if (!balls.Contains("standard")) balls.Add("standard");
            if (!skins.Contains(selectedSkin ?? "")) selectedSkin = "classic-red";
            if (!balls.Contains(selectedBall ?? "")) selectedBall = "standard";
            if (settings == null) settings = new GameSettings();
            // Enable responsive swipes and travel-follow once. Later explicit choices persist.
            if (upgradeResponsiveControls) { settings.cameraMode = 1; settings.touchMode = 1; settings.sensitivity = Math.Min(settings.sensitivity,1f); }
            settings.music = ClampFinite(settings.music, 0, 1, 0.45f);
            settings.sfx = ClampFinite(settings.sfx, 0, 1, 0.8f);
            settings.sensitivity = ClampFinite(settings.sensitivity, 0.4f, 2.5f, 1);
            settings.quality = Math.Max(0, Math.Min(2, settings.quality));
            settings.touchMode=Math.Max(0,Math.Min(1,settings.touchMode));
            settings.cameraMode=Math.Max(0,Math.Min(1,settings.cameraMode));
            settings.language = "en"; // Only English ships in this milestone.
            if (levels == null) levels = new List<LevelRecord>();
            var ids = new HashSet<int>();
            levels.RemoveAll(r => r == null || r.id < 1 || r.id > 1000 || !ids.Add(r.id));
            foreach (var r in levels)
            {
                r.stars = Math.Max(0, Math.Min(3, r.stars));
                r.bestSeconds = ClampFinite(r.bestSeconds, 0, 86400, 0);
                r.rewardedCoins = Math.Max(0, r.rewardedCoins);
                r.rewardedGems = Math.Max(0, r.rewardedGems);
                r.rewardedTokens = Math.Max(0, r.rewardedTokens);
            }
            if(expeditions==null)expeditions=new List<LevelRecord>();
            var expeditionIds=new HashSet<int>();
            expeditions.RemoveAll(r=>r==null||r.id<56||r.id>60||!expeditionIds.Add(r.id));
            foreach(var r in expeditions)
            {
                r.stars=Math.Max(0,Math.Min(3,r.stars));r.bestSeconds=ClampFinite(r.bestSeconds,0,86400,0);
                r.rewardedCoins=Math.Max(0,r.rewardedCoins);r.rewardedGems=Math.Max(0,r.rewardedGems);r.rewardedTokens=Math.Max(0,r.rewardedTokens);
            }
        }

        static float ClampFinite(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }
}
