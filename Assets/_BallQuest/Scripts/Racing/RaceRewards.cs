using System;

namespace BallQuest
{
    public static class RaceRewards
    {
        public static void Award(SaveStore save,RaceResult result,int raceCount,bool playerFinished)
        {
            if(save==null||result==null)return;
            if(result.RewardsProcessed){result.Coins=result.Gems=0;return;}result.RewardsProcessed=true;
            if(!playerFinished){result.Coins=result.Gems=0;result.Victory=false;save.Save();return;}
            var data=save.Data;int desiredCoins=0,desiredGems=0;
            if(playerFinished)
            {
                desiredCoins=result.Format==RaceFormat.TimeTrial?(result.TimeTrialSuccess?120:30):Math.Max(20,150-(result.PlayerPosition-1)*25);
                if(result.Clean)desiredCoins+=25;
                if(result.Victory)desiredGems=1;
                desiredCoins+=result.CollectedCoins;desiredGems+=result.CollectedGems;
                data.racesCompleted++;
                if(result.Victory)data.raceVictories++;
                if(result.Format!=RaceFormat.TimeTrial||result.TimeTrialSuccess)
                    data.unlockedRace=Math.Max(data.unlockedRace,Math.Min(raceCount,result.TrackId+1));
            }
            PayRecord(data,result.TrackId,(int)result.Format,result.PlayerPosition,result.Seconds,desiredCoins,desiredGems,result.Victory,out int coins,out int gems);
            result.Coins=coins;result.Gems=gems;
            var record=data.races.Find(value=>value.id==result.TrackId&&value.format==(int)result.Format);
            data.tokens+=Math.Max(0,result.CollectedTokens-record.rewardedTokens);record.rewardedTokens=Math.Max(record.rewardedTokens,result.CollectedTokens);
            save.Save();
        }

        public static void AwardTournament(SaveStore save,RaceResult result,int position,float seconds,int finishes)
        {
            if(save==null||finishes<=0)return;
            if(result.TournamentRewardsProcessed)return;result.TournamentRewardsProcessed=true;
            int desired=position==1?500:position==2?300:position==3?200:75;
            int desiredGems=position==1?3:position<=3?1:0;
            PayRecord(save.Data,1001,(int)RaceFormat.Tournament,position,seconds,desired,desiredGems,position==1,out int coins,out int gems);
            result.Coins+=coins;result.Gems+=gems;
            if(position==1)save.Data.tournamentsWon++;
            save.Save();
        }

        static void PayRecord(SaveData data,int id,int format,int place,float seconds,int desiredCoins,int desiredGems,bool won,out int coins,out int gems)
        {
            var record=data.races.Find(value=>value.id==id&&value.format==format);
            if(record==null){record=new RaceRecord{id=id,format=format,bestPlace=place,bestSeconds=seconds};data.races.Add(record);}
            record.bestPlace=record.bestPlace<=0?place:Math.Min(record.bestPlace,place);
            if(seconds>0&&(record.bestSeconds<=0||seconds<record.bestSeconds))record.bestSeconds=seconds;
            if(won)record.wins++;
            coins=Math.Max(0,desiredCoins-record.rewardedCoins);gems=Math.Max(0,desiredGems-record.rewardedGems);
            record.rewardedCoins=Math.Max(record.rewardedCoins,desiredCoins);record.rewardedGems=Math.Max(record.rewardedGems,desiredGems);
            data.coins+=coins;data.gems+=gems;
        }
    }
}
