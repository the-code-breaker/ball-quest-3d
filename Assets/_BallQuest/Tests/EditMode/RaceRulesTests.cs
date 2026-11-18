using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BallQuest.Tests
{
    public sealed class RaceRulesTests
    {
        [Test] public void OrderedRouteBlocksJumpingAcrossOverlappingSections()
        {
            var definition=ScriptableObject.CreateInstance<RaceTrackDefinition>();
            try
            {
                definition.route=new[]{Vector3.zero,new Vector3(0,0,30),new Vector3(30,0,30),new Vector3(30,0,0),new Vector3(0,8,0),new Vector3(0,8,30)};
                var route=new RaceRoute(definition);var cursor=new RaceRouteCursor(route,new Vector3(0,.5f,0));
                cursor.Update(new Vector3(0,8.5f,25),.016f);Assert.Less(cursor.Progress,.3f,"Teleporting near the overlapping final lane must not count as lap progress.");
                cursor.Reset(0,new Vector3(0,.5f,0));
                for(float d=0;d<route.Length+.5f;d+=.5f){var p=route.Sample(d,out _,out _);cursor.Update(p+Vector3.up*.5f,.05f);}
                Assert.IsTrue(cursor.AllGatesPassed);Assert.Greater(cursor.Progress,.99f);
            }
            finally{UnityEngine.Object.DestroyImmediate(definition);}
        }
        [Test] public void RaceRewardsOnlyPayImprovementAndDnfCannotClaimVictory()
        {
            string folder=Path.Combine(Path.GetTempPath(),"BallQuestRaceRules-"+Guid.NewGuid());
            try
            {
                var save=new SaveStore(folder,new UnitySaveCodec());
                var result=new RaceResult{TrackId=1,PlayerPosition=1,Seconds=25,Victory=true,Clean=true,Format=RaceFormat.Sprint};
                RaceRewards.Award(save,result,5,true);int coins=save.Data.coins;Assert.Greater(coins,100);Assert.AreEqual(2,save.Data.unlockedRace);
                RaceRewards.Award(save,result,5,true);Assert.AreEqual(coins,save.Data.coins);Assert.AreEqual(0,result.Coins);
                var dnf=new RaceResult{TrackId=2,PlayerPosition=1,Victory=false,Format=RaceFormat.Obstacle};
                RaceRewards.Award(save,dnf,5,false);Assert.AreEqual(coins,save.Data.coins);Assert.IsFalse(save.Data.races.Exists(r=>r.id==2&&r.bestPlace==1));
                RaceRewards.AwardTournament(save,dnf,1,180,0);Assert.AreEqual(coins,save.Data.coins);Assert.AreEqual(0,save.Data.tournamentsWon);
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
    }
}
