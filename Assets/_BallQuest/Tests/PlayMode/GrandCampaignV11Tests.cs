using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace BallQuest.Tests
{
    public sealed class GrandCampaignV11Tests
    {
        Scene original,loaded;string directory;GameBootstrap app;
        [UnitySetUp]public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestGrand-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();const string path="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);loaded=SceneManager.GetSceneByPath(path);SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+20;while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);app.Save.Data.unlockedLevel=1000;
        }
        [UnityTest]public IEnumerator OnlyTheActiveCourseExpandsItsCollisionGeometryAndSpecialIdsRemainSeparate()
        {
            app.Play(1000);yield return BootstrapFlowTests.AwaitPlaying(app);
            Assert.AreEqual(1000,app.Session.Level.id);Assert.AreNotSame(app.catalog.Level(1000),app.Session.Level);
            Assert.Greater(app.Session.Level.tracks.Length,30);Assert.IsTrue(app.catalog.levels.All(l=>l.tracks.Length==0));
            Assert.IsNotNull(app.UI.GetComponentsInChildren<TMPro.TMP_Text>().FirstOrDefault(t=>t.text.Contains("Falls:")||t.text.Contains("seconds")||t.text.Contains("coins")||t.text.Contains("gem")||t.text.Contains("boost pad")));
            var old=app.Session.Level;
            app.Play(56);yield return BootstrapFlowTests.AwaitPlaying(app);Assert.IsFalse(app.Session.Level.expedition);
            yield return null;Assert.IsFalse(old,"The unloaded expanded definition must be released.");
            app.PlayExpedition(56);yield return BootstrapFlowTests.AwaitPlaying(app);Assert.IsTrue(app.Session.Level.expedition);
            app.RetryCurrent();yield return BootstrapFlowTests.AwaitPlaying(app);Assert.IsTrue(app.Session.Level.expedition,"Retry must not switch from a special adventure to campaign level 56.");
            Assert.IsTrue(app.catalog.levels.All(l=>l.tracks.Length==0));
            app.Save.Save();var fresh=new SaveStore(directory,new UnitySaveCodec());fresh.Load();Assert.AreEqual(1000,fresh.Data.unlockedLevel);
        }
        [UnityTest]public IEnumerator ARealSessionCompletionBanksTheBonusOnceAndKeepsNormalProgressOptional()
        {
            app.Play(1);yield return BootstrapFlowTests.AwaitPlaying(app);var session=app.Session;
            for(int i=0;i<session.Level.checkpoints.Length;i++)session.Checkpoint(i,session.Level.checkpoints[i]);
            int before=app.Save.Data.coins;session.Finish();Assert.AreEqual(SessionState.Complete,session.State);
            Assert.IsTrue(session.ChallengeCompleted);Assert.AreEqual(session.Level.challengeReward,session.ChallengeReward);
            Assert.AreEqual(session.BankedReward,app.Save.Data.coins-before);Assert.IsTrue(app.Save.Data.levels.Single(r=>r.id==1).challengeCompleted);
            app.Play(1);yield return BootstrapFlowTests.AwaitPlaying(app);for(int i=0;i<session.Level.checkpoints.Length;i++)session.Checkpoint(i,session.Level.checkpoints[i]);session.Finish();
            Assert.AreEqual(0,session.ChallengeReward,"A replay must not duplicate its bonus.");
            int coinId=app.catalog.levels.First(l=>l.challenge==ChallengeKind.CoinHunt).id;app.Play(coinId);yield return BootstrapFlowTests.AwaitPlaying(app);
            for(int i=0;i<session.Level.checkpoints.Length;i++)session.Checkpoint(i,session.Level.checkpoints[i]);session.Finish();
            Assert.AreEqual(SessionState.Complete,session.State);Assert.IsFalse(session.ChallengeCompleted);Assert.AreEqual(0,session.ChallengeReward);
            Assert.GreaterOrEqual(app.Save.Data.unlockedLevel,coinId+1);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
