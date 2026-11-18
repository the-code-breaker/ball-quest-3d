using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BallQuest.Editor;
namespace BallQuest.Tests
{
    public sealed class CrazyCampaignV9Tests
    {
        GameCatalog Catalog=>AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_BallQuest/ScriptableObjects/GameCatalog.asset");
        [Test]public void ThousandCompactCoursesHaveTwentyBiomesAndTwentyFiveTopologies()
        {
            var courses=Catalog.levels;Assert.AreEqual(1000,courses.Length);
            CollectionAssert.AreEqual(Enumerable.Range(1,1000),courses.Select(l=>l.id));
            Assert.AreEqual(1000,courses.Select(l=>l.title).Distinct().Count());
            Assert.AreEqual(25,courses.Select(l=>l.archetype).Distinct().Count());
            Assert.AreEqual(20,courses.Select(l=>l.environment).Distinct().Count());
            foreach(var biome in courses.GroupBy(l=>l.environment)){Assert.AreEqual(50,biome.Count());Assert.AreEqual(25,biome.Select(l=>l.archetype).Distinct().Count(),"Each theme needs every topology recipe.");}
            var shapes=new HashSet<string>();int points=0,tracks=0;
            foreach(var level in courses)
            {
                Assert.IsTrue(level.streamedCampaign);Assert.AreEqual(0,level.tracks.Length,"Catalog must not retain expanded track objects.");
                Assert.IsTrue(shapes.Add(GrandCampaignGenerator.Fingerprint(level.routePoints)),level.title+" is a translated/scaled duplicate.");
                points+=level.routePoints.Length;tracks+=CampaignGeometry.Expand(level).Length;
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/grand-structure-v11.json",JsonUtility.ToJson(new Report{courses=1000,biomes=20,topologies=25,uniqueNormalizedRoutes=shapes.Count,totalRoutePoints=points,totalExpandedTracks=tracks},true));
        }
        [Serializable]sealed class Report{public int courses,biomes,topologies,uniqueNormalizedRoutes,totalRoutePoints,totalExpandedTracks;public string scope="All 1000 serialized routes; structural checks do not establish physical completion or human difficulty.";}
        static bool Supported(TrackPiece[] tracks,Vector3 point,float minWidth=0)
        {
            foreach(var track in tracks)
            {
                var local=Quaternion.Inverse(Quaternion.Euler(track.rotation))*(point-track.position);
                if(track.size.x>=minWidth&&Mathf.Abs(local.x)<=track.size.x*.5f&&Mathf.Abs(local.z)<=track.size.z*.5f+.12f&&local.y>=track.size.y*.5f+.35f&&local.y<=track.size.y*.5f+1.7f)return true;
            }
            return false;
        }
        [Test]public void EveryCourseHasSupportedSpawnsFinishesCheckpointsAndFeasibleBonusQuotas()
        {
            int needles=0,expert=0,drops=0;
            foreach(var level in Catalog.levels)
            {
                var tracks=CampaignGeometry.Expand(level);Assert.Greater(tracks.Length,20);
                Assert.AreEqual(level.routePoints.Length-1,level.roadFlags.Length);Assert.AreEqual(level.routePoints.Length,level.routeWidths.Length);
                Assert.Greater(level.checkpoints.Length,0);Assert.GreaterOrEqual(tracks[0].size.x,6);
                Assert.IsTrue(Supported(tracks,level.spawn,6),level.title+" spawn unsupported");
                Assert.IsTrue(Supported(tracks,level.finish,6),level.title+" finish unsupported");
                foreach(var p in level.checkpoints)Assert.IsTrue(Supported(tracks,p,6),level.title+" checkpoint unsupported");
                foreach(float width in level.routeWidths)Assert.GreaterOrEqual(width,1.6f);
                Assert.Greater(level.spawn.y,level.killHeight+8);Assert.AreNotEqual(ChallengeKind.None,level.challenge);
                if(level.challenge==ChallengeKind.CoinHunt)Assert.GreaterOrEqual(level.pickups.Count(p=>p.kind==PickupKind.Coin),level.challengeTarget);
                if(level.challenge==ChallengeKind.Treasure)Assert.GreaterOrEqual(level.pickups.Count(p=>p.kind==PickupKind.Gem),level.challengeTarget);
                if(level.challenge==ChallengeKind.BoostRun)Assert.GreaterOrEqual(level.obstacles.Count(p=>p.kind==ObstacleKind.Booster),level.challengeTarget);
                Assert.Greater(level.challengeSeconds,0);Assert.That(level.challengeReward,Is.InRange(30,100));
                if(tracks.Any(t=>!t.rails&&t.size.x<=2.4f))needles++;if(level.difficulty>=3)expert++;
                Assert.AreEqual(level.dropPoints.Length,level.dropDirections.Length);
                foreach(var lip in level.dropPoints)
                {
                    drops++;Assert.IsTrue(tracks.Any(t=>{var q=Quaternion.Inverse(Quaternion.Euler(t.rotation))*(lip-t.position);return t.size.x>=8&&q.y>3&&q.y<10&&Mathf.Abs(q.x)<t.size.x*.5f&&Mathf.Abs(q.z)<t.size.z*.5f+.15f;}),level.title+" lacks a wide catch below drop");
                }
            }
            Assert.Greater(needles,100);Assert.Greater(expert,900);Assert.Greater(drops,100);
        }
        [Test]public void RemoteRoadCrossingsHavePhysicalOverpassClearance()
        {foreach(var course in Catalog.levels)Assert.IsTrue(GrandCampaignGenerator.Safe(course),course.title+" intersects another road at the same height");}
        [Test]public void ChallengeRewardsAreOptionalPersistedAndGrantedOnce()
        {
            var save=new SaveData{version=10,unlockedLevel=999,coins=40};save.levels.Add(new LevelRecord{id=999,stars=2,challengeCompleted=false});save.Validate();
            Assert.AreEqual(999,save.unlockedLevel);Assert.AreEqual(999,save.levels[0].id);
            Assert.AreEqual(0,ChallengeRules.Claim(save,999,60,false));Assert.AreEqual(40,save.coins);
            Assert.AreEqual(60,ChallengeRules.Claim(save,999,60,true));Assert.AreEqual(0,ChallengeRules.Claim(save,999,60,true));Assert.AreEqual(100,save.coins);
            var roundtrip=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));roundtrip.Validate();Assert.IsTrue(roundtrip.levels[0].challengeCompleted);
            Assert.IsFalse(ChallengeRules.Satisfied(ChallengeKind.Flawless,1,100,50,1,20,5,2));
            Assert.IsTrue(ChallengeRules.Satisfied(ChallengeKind.CoinHunt,20,100,20,5,200,0,0));
            Assert.IsFalse(ChallengeRules.Satisfied(ChallengeKind.TimeTrial,1,90,0,0,91,0,0));
            Assert.IsTrue(ChallengeRules.Satisfied(ChallengeKind.Treasure,1,90,0,0,200,1,0));
            Assert.IsTrue(ChallengeRules.Satisfied(ChallengeKind.BoostRun,1,90,0,0,200,0,1));
            Assert.AreEqual(56,Catalog.Level(56).id);Assert.IsFalse(Catalog.Level(56).expedition);Assert.IsTrue(Catalog.Expedition(56).expedition);
        }
    }
}
