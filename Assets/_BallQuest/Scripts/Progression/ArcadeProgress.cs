using System;
using System.Globalization;

namespace BallQuest
{
    public sealed class MissionView
    {
        public string id,title,description;
        public int current,target,rewardCoins;
        public bool claimed;
    }

    public static class ArcadeProgress
    {
        static string Today=>DateTime.UtcNow.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        public static void Refresh(SaveData data)
        {
            if(data.claimedMissions==null)data.claimedMissions=new System.Collections.Generic.List<string>();
            data.dailyIndex=Math.Max(0,Math.Min(6,data.dailyIndex));
        }
        public static MissionView[] Missions(SaveData data)
        {
            Refresh(data);
            int complete=data.levels.FindAll(r=>r.stars>0).Count;
            int perfect=data.levels.FindAll(r=>r.stars==3).Count;
            MissionView M(string id,string title,string description,int value,int target,int reward)=>new MissionView{
                id=id,title=title,description=description,current=Math.Min(value,target),target=target,rewardCoins=reward,claimed=data.claimedMissions.Contains(id)};
            return new[]{
                M("first-finish","First finish","Finish any course",complete,1,75),
                M("coin-scout","Coin scout","Collect 50 coins on the tracks",data.lifetimeCoins,50,100),
                M("turbo-six","Feel the rush","Ride 6 turbo runways",data.lifetimeBoosts,6,120),
                M("drop-six","Air time","Land 6 big drops",data.lifetimeDrops,6,150),
                M("three-finishes","World explorer","Complete 3 different courses",complete,3,200),
                M("clean-three","Clean streak","Finish 3 runs without a fall",data.flawlessFinishes,3,180),
                M("coin-hunter","Coin hunter","Collect 150 track coins",data.lifetimeCoins,150,200),
                M("long-roll","Keep on rolling","Roll for 1,500 metres",(int)data.lifetimeDistance,1500,250),
                M("perfect-three","Star collector","Earn 3 stars on 3 courses",perfect,3,300),
                M("lucky-spin","A little luck","Use the free daily spin",data.lifetimeSpins,1,50),
                M("coin-legend","Golden trail","Collect 500 track coins",data.lifetimeCoins,500,400),
                M("all-twelve","Around the worlds","Complete 12 courses",complete,12,1000),
                M("campaign-fifty-five","Adventure master","Complete 55 adventure courses",complete,55,2000),
                M("campaign-century","Century explorer","Complete 100 courses",complete,100,2500),
                M("campaign-quarter","Trail veteran","Complete 250 courses",complete,250,4000),
                M("campaign-five-hundred","World wanderer","Complete 500 courses",complete,500,6000),
                M("campaign-thousand","Thousand trails","Complete all 1,000 courses",complete,1000,10000),
                M("challenge-twenty","Challenge hunter","Clear 20 bonus objectives",data.levels.FindAll(r=>r.challengeCompleted).Count,20,800),
                M("first-race","On the grid","Finish a competitive race",data.racesCompleted,1,100),
                M("first-race-win","Top step","Win a validated race",data.raceVictories,1,150)
            };
        }
        public static bool ClaimMission(SaveData data,string id)
        {
            foreach(var mission in Missions(data))
            {
                if(mission.id!=id||mission.claimed||mission.current<mission.target)continue;
                data.claimedMissions.Add(id);data.coins+=mission.rewardCoins;return true;
            }
            return false;
        }
        public static bool CanClaimDaily(SaveData data)=>data.dailyClaimDate!=Today;
        static int NextDay(SaveData data)
        {
            string yesterday=DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            return data.dailyClaimDate==yesterday?Math.Max(0,Math.Min(6,data.dailyIndex)):0;
        }
        public static int DailyDay(SaveData data)=>CanClaimDaily(data)?NextDay(data):(data.dailyIndex+6)%7;
        public static string DailyStatus(SaveData data)=>CanClaimDaily(data)?"Your daily gift is ready!":"Claimed! Your next gift arrives tomorrow.";
        public static bool ClaimDaily(SaveData data)
        {
            Refresh(data);if(!CanClaimDaily(data))return false;
            int day=NextDay(data);
            switch(day)
            {
                case 0:data.coins+=50;break;
                case 1:data.gems++;break;
                case 2:data.coins+=100;break;
                case 3:data.gems+=2;break;
                case 4:data.coins+=150;break;
                case 5:data.coins+=250;break;
                case 6:if(!data.skins.Contains("galaxy"))data.skins.Add("galaxy");else data.coins+=350;break;
            }
            data.dailyIndex=(day+1)%7;data.dailyClaimDate=Today;return true;
        }
        public static bool CanSpin(SaveData data)=>data.spinClaimDate!=Today;
        public static string FreeSpin(SaveData data)
        {
            if(!CanSpin(data))return "Your next free spin is tomorrow.";
            data.spinClaimDate=Today;data.lifetimeSpins++;
            int result=new Random(Guid.NewGuid().GetHashCode()).Next(8);
            if(result==7){data.gems+=3;return "+3 GEMS";}
            int[] rewards={30,50,50,75,100,100,150};int reward=rewards[result];data.coins+=reward;
            return "+"+reward+" COINS";
        }
        public static bool ExchangeGems(SaveData data,int gems,int coins)
        {
            if(gems<1||gems>1000||coins!=gems*100||data.gems<gems)return false;
            data.gems-=gems;data.coins+=coins;return true;
        }
    }
}
