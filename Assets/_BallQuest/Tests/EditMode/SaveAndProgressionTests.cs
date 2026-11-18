using System;
using System.IO;
using NUnit.Framework;

namespace BallQuest.Tests
{
    public sealed class SaveAndProgressionTests
    {
        string directory;
        [SetUp] public void Setup() => directory = Path.Combine(Path.GetTempPath(), "BallQuestTests-" + Guid.NewGuid());
        [TearDown] public void Teardown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test]
        public void CompletionUnlocksAndDoesNotDuplicateRewards()
        {
            var data = new SaveData();
            Assert.AreEqual(25, ProgressionRules.Complete(data, 1, 3, 25, 1, 2, 40, 1));
            Assert.AreEqual(2, data.unlockedLevel);
            Assert.AreEqual(0, ProgressionRules.Complete(data, 1, 3, 25, 1, 1, 50, 1));
            Assert.AreEqual(25, data.coins); Assert.AreEqual(1, data.gems); Assert.AreEqual(1, data.tokens);
            Assert.AreEqual(2, data.levels[0].stars);
            Assert.AreEqual(5, ProgressionRules.Complete(data, 1, 3, 30, 2, 3, 35));
            Assert.AreEqual(30, data.coins); Assert.AreEqual(35, data.levels[0].bestSeconds);
        }

        [Test] public void LockedLevelCannotGrantRewards()
            => Assert.Throws<ArgumentOutOfRangeException>(() => ProgressionRules.Complete(new SaveData(), 2, 3, 99, 0, 3, 1));

        [Test]
        public void CorruptPrimaryRecoversPreviousCompleteBackup()
        {
            var store = new SaveStore(directory, new UnitySaveCodec());
            store.Data.coins = 12; Assert.IsTrue(store.Save());
            store.Data.coins = 24; Assert.IsTrue(store.Save());
            File.WriteAllText(Path.Combine(directory, "ball-quest-save.json"), "broken");
            var recovered = new SaveStore(directory, new UnitySaveCodec()); recovered.Load();
            Assert.AreEqual(12, recovered.Data.coins);
        }

        [Test]
        public void SaveRoundTripPreservesSelectedCosmeticsAndSettings()
        {
            var store = new SaveStore(directory, new UnitySaveCodec());
            store.Data.selectedSkin = "ocean-blue"; store.Data.settings.sensitivity = 1.8f;
            ProgressionRules.Complete(store.Data, 1, 3, 42, 2, 3, 65);
            Assert.IsTrue(store.Save());
            var loaded = new SaveStore(directory, new UnitySaveCodec()); loaded.Load();
            Assert.AreEqual("ocean-blue", loaded.Data.selectedSkin); Assert.AreEqual(42, loaded.Data.coins);
            Assert.AreEqual(1.8f, loaded.Data.settings.sensitivity); Assert.AreEqual(3, loaded.Data.levels[0].stars);
        }

        [Test]
        public void ValidationMigratesAndRepairsInvalidValues()
        {
            var data = new SaveData { version = 1, coins = -5, unlockedLevel = 99999, skins = null };
            data.settings.sensitivity = float.NaN; data.Validate();
            Assert.AreEqual(SaveData.CurrentVersion, data.version); Assert.AreEqual(0, data.coins);
            Assert.AreEqual(1000, data.unlockedLevel); Assert.AreEqual(1, data.settings.sensitivity);
            Assert.Contains("classic-red", data.skins);
        }

        [TestCase(8, 10, 0, 70, 90, 3)]
        [TestCase(7, 10, 0, 70, 90, 2)]
        [TestCase(8, 10, 1, 70, 90, 2)]
        [TestCase(0, 0, 1, 100, 90, 1)]
        public void StarsFollowPublishedRules(int collected, int available, int falls, float seconds, float par, int expected)
            => Assert.AreEqual(expected, ProgressionRules.Stars(collected, available, falls, seconds, par));
    }
}
