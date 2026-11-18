using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace BallQuest.Tests
{
    public sealed class MaterialAndChaptersV10Tests
    {
        GameCatalog Catalog=>AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_BallQuest/ScriptableObjects/GameCatalog.asset");
        [Test]public void EveryCampaignIdAppearsExactlyOnceInConsecutiveChaptersAndHasTheCorrectSuccessor()
        {
            var all=new List<int>();
            for(int chapter=0;chapter<20;chapter++)
            {
                var levels=CampaignChapters.Courses(Catalog,chapter);Assert.Greater(levels.Length,0);
                for(int i=1;i<levels.Length;i++)Assert.AreEqual(levels[i-1].id+1,levels[i].id);
                all.AddRange(levels.Select(l=>l.id));
            }
            CollectionAssert.AreEqual(Enumerable.Range(1,1000).ToArray(),all);
            for(int id=1;id<1000;id++)Assert.AreEqual(id+1,CampaignChapters.Next(Catalog,id).id);
            Assert.IsNull(CampaignChapters.Next(Catalog,1000));
            Assert.GreaterOrEqual(Catalog.levels.Count(l=>l.difficulty>=3),50);
            Assert.GreaterOrEqual(Catalog.levels.Take(100).Count(l=>l.obstacles.Any(o=>o.kind>=ObstacleKind.Piston)),5);
            Assert.Greater(Catalog.levels.Sum(l=>l.obstacles.Count(o=>o.kind==ObstacleKind.Piston)),5);
            Assert.Greater(Catalog.levels.Sum(l=>l.obstacles.Count(o=>o.kind==ObstacleKind.SweepArm)),5);
            Assert.Greater(Catalog.levels.Sum(l=>l.obstacles.Count(o=>o.kind==ObstacleKind.RollerDrum)),5);
        }
        [Test]public void AllRecordedBodiesAndFortyEightPairedImpactsAreReferencedAndFinite()
        {
            var clips=new HashSet<AudioClip>();
            foreach(var profile in Resources.LoadAll<BallAudioProfile>("BallAudio"))
            {
                Assert.AreEqual(10,profile.audioRevision);
                foreach(var clip in new[]{profile.rolling,profile.sliding,profile.impact}.Concat(profile.surfaces.Select(s=>s.impactOverride)))
                {
                    Assert.IsTrue(clips.Add(clip),"Material identity cannot collapse to a shared surface sound.");
                    Assert.IsTrue(clip.name.Contains(profile.material.ToString()));
                    var samples=new float[clip.samples*clip.channels];Assert.IsTrue(clip.GetData(samples,0));
                    double square=0;foreach(float sample in samples){Assert.IsFalse(float.IsNaN(sample)||float.IsInfinity(sample));Assert.Less(Mathf.Abs(sample),.9f);square+=sample*sample;}
                    Assert.Greater(Math.Sqrt(square/samples.Length),.005);
                }
                Assert.Less(profile.slidingVolume,.08f,"Excess slip hiss must not dominate the contact sound.");
            }
            Assert.AreEqual(66,clips.Count);
        }
        [Test]public void MaterialBallsAndSaveMigrationKeepProgressAndThePlayersControlPreferences()
        {
            var old=new SaveData{version=9,coins=700,unlockedLevel=23,selectedSkin="neon"};old.skins.Add("neon");old.settings.sensitivity=.71f;old.Validate();
            Assert.AreEqual(700,old.coins);Assert.AreEqual(23,old.unlockedLevel);Assert.AreEqual("neon",old.selectedSkin);Assert.AreEqual(.71f,old.settings.sensitivity);
            foreach(string id in new[]{"brushed-steel","carbon-rubber","copper-core","walnut"})
            {var skin=Catalog.Skin(id);Assert.AreEqual(id,skin.id);Assert.IsTrue(skin.explicitAudioMaterial);}
            Assert.Contains("brushed-steel",old.skins);Assert.Contains("carbon-rubber",old.skins);
            Assert.AreEqual(BallAudioMaterial.Metal,BallAudioProfile.ResolveMaterial(null,Catalog.Skin("brushed-steel")));
            Assert.AreEqual(BallAudioMaterial.Rubber,BallAudioProfile.ResolveMaterial(null,Catalog.Skin("carbon-rubber")));
        }
    }
}
