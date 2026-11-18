using System;
using System.IO;
using NUnit.Framework;

namespace BallQuest.Tests
{
    public sealed class DirectionalSettingsV6Tests
    {
        [Test]
        public void ExistingProgressReceivesFollowCameraOnceAndLaterOrbitChoicePersists()
        {
            string directory=Path.Combine(Path.GetTempPath(),"BallQuestFollowUpgrade-"+Guid.NewGuid());
            try
            {
                var store=new SaveStore(directory,new UnitySaveCodec());
                store.Data.version=7;store.Data.settings.cameraMode=0;store.Data.settings.touchMode=0;
                store.Data.settings.sensitivity=1.3f;store.Data.coins=321;store.Data.unlockedLevel=9;
                store.Data.levels.Add(new LevelRecord{id=7,stars=3});
                store.Data.Validate();
                Assert.AreEqual(1,store.Data.settings.cameraMode);
                Assert.AreEqual(1,store.Data.settings.touchMode);
                Assert.AreEqual(321,store.Data.coins);Assert.AreEqual(9,store.Data.unlockedLevel);
                Assert.AreEqual(3,store.Data.levels[0].stars);Assert.AreEqual(1f,store.Data.settings.sensitivity,"Upgrade reduces an excessive saved sensitivity once.");
                store.Data.settings.cameraMode=0;store.Data.settings.touchMode=0;store.Data.settings.sensitivity=1.3f;Assert.IsTrue(store.Save());
                var reload=new SaveStore(directory,new UnitySaveCodec());reload.Load();
                Assert.AreEqual(0,reload.Data.settings.cameraMode,"A later explicit stable-orbit choice must survive validation and reload.");
                Assert.AreEqual(0,reload.Data.settings.touchMode);
                Assert.AreEqual(1.3f,reload.Data.settings.sensitivity,"A sensitivity selected after upgrading must persist.");
                Assert.AreEqual(SaveData.CurrentVersion,reload.Data.version);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        [Test]
        public void FreshInstallUsesDirectionalFollowCamera()
        {
            var save=new SaveData();save.Validate();
            Assert.AreEqual(1,save.settings.cameraMode);
            Assert.AreEqual(1,save.settings.touchMode);
        }
    }
}
