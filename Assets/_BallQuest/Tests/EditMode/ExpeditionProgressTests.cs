using System;
using System.IO;
using NUnit.Framework;

namespace BallQuest.Tests
{
    public sealed class ExpeditionProgressTests
    {
        [Test]
        public void ExpeditionRewardsHaveIndependentHighWaterMarksWithoutSkippingCampaign()
        {
            var data=new SaveData{unlockedLevel=2,coins=7};
            Assert.AreEqual(50,ProgressionRules.CompleteExpedition(data,56,50,2,2,95,1));
            Assert.AreEqual(57,data.coins);Assert.AreEqual(2,data.unlockedLevel);Assert.IsEmpty(data.levels);
            Assert.AreEqual(0,ProgressionRules.CompleteExpedition(data,56,50,2,2,100,1));
            Assert.AreEqual(57,data.coins);Assert.AreEqual(2,data.gems);Assert.AreEqual(1,data.tokens);
            Assert.AreEqual(10,ProgressionRules.CompleteExpedition(data,56,60,3,3,85,2));
            Assert.AreEqual(3,data.expeditions[0].stars);Assert.AreEqual(85,data.expeditions[0].bestSeconds);
            Assert.AreEqual(67,data.coins);Assert.AreEqual(3,data.gems);Assert.AreEqual(2,data.tokens);
            Assert.AreEqual(2,data.unlockedLevel);
        }
        [Test]
        public void OlderSavesMigrateAndExpeditionControlsSurviveRealSaveReload()
        {
            string directory=Path.Combine(Path.GetTempPath(),"BallQuestManualSave-"+Guid.NewGuid());
            try
            {
                var store=new SaveStore(directory,new UnitySaveCodec());store.Data.version=4;store.Data.coins=120;store.Data.unlockedLevel=8;
                store.Data.levels.Add(new LevelRecord{id=1,stars=3});store.Data.Validate();
                Assert.AreEqual(SaveData.CurrentVersion,store.Data.version);Assert.AreEqual(120,store.Data.coins);Assert.AreEqual(3,store.Data.levels[0].stars);
                ProgressionRules.CompleteExpedition(store.Data,58,35,1,3,90);store.Data.settings.touchMode=1;store.Data.settings.cameraMode=1;store.Save();
                var fresh=new SaveStore(directory,new UnitySaveCodec());fresh.Load();
                Assert.AreEqual(8,fresh.Data.unlockedLevel);Assert.AreEqual(155,fresh.Data.coins);Assert.AreEqual(58,fresh.Data.expeditions[0].id);
                Assert.AreEqual(1,fresh.Data.settings.touchMode);Assert.AreEqual(1,fresh.Data.settings.cameraMode);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
