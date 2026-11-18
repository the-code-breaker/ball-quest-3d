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
    public sealed class RacingIntegrationTests
    {
        Scene original,loaded;string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestRace-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();yield return SceneManager.LoadSceneAsync("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity",LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity");SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+25;while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.IsTrue(GameBootstrap.Instance.Ready);GameBootstrap.Instance.Save.Data.unlockedRace=5;
        }
        static IEnumerator WaitForRace(GameBootstrap app)
        {float end=Time.realtimeSinceStartup+20;while((app.Loading||app.Race.State==RaceState.Idle)&&Time.realtimeSinceStartup<end)yield return null;Assert.AreEqual(RaceState.Countdown,app.Race.State);}
        static RaceDriver DrivePlayer(RaceManager race,RaceDifficulty difficulty=RaceDifficulty.Hard)
        {var driver=race.Player.Ball.gameObject.AddComponent<RaceDriver>();driver.Initialize(race,race.Player,difficulty);return driver;}
        [UnityTest] public IEnumerator FourOpponentVerticalSliceStartsTogetherAndValidatesActualFinishes()
        {
            var app=GameBootstrap.Instance;app.PlayRace(1,RaceFormat.Sprint,RaceDifficulty.Medium,4);yield return WaitForRace(app);
            var race=app.Race;Assert.AreEqual(5,race.Competitors.Count);
            var starts=race.Competitors.Select(r=>r.Ball.Body.position).ToArray();
            foreach(var racer in race.Competitors){Assert.IsTrue(racer.Ball.Body.isKinematic);Assert.IsFalse(racer.Ball.active);}
            yield return new WaitForSeconds(.4f);
            for(int i=0;i<starts.Length;i++)Assert.Less(Vector3.Distance(starts[i],race.Competitors[i].Ball.Body.position),.001f);
            BootstrapFlowTests.Capture("RaceCountdown");app.PauseCurrent();float elapsed=race.Elapsed;yield return new WaitForSecondsRealtime(.15f);Assert.AreEqual(elapsed,race.Elapsed);app.ResumeCurrent();
            DrivePlayer(race);Time.timeScale=5;
            while(race.State==RaceState.Countdown)yield return null;
            foreach(var racer in race.Competitors){Assert.IsTrue(racer.Ball.active);Assert.IsFalse(racer.Ball.Body.isKinematic);}
            bool captured=false;float realEnd=Time.realtimeSinceStartup+90;
            while(race.State==RaceState.Racing&&Time.realtimeSinceStartup<realEnd)
            {if(!captured&&race.Elapsed>5){captured=true;BootstrapFlowTests.Capture("RaceGameplay");}yield return null;}
            Assert.AreEqual(RaceState.Complete,race.State);Assert.IsNotNull(race.Result);
            foreach(var racer in race.Standings)Debug.Log("Race slice: "+racer.DisplayName+" finished="+racer.Finished+" progress="+racer.Progress+" falls="+racer.Falls+" point="+racer.Ball.Body.position);
            Assert.IsTrue(race.Player.Finished,"Player input-driven test car did not finish.");Assert.AreEqual(5,race.Result.Standings.Count(r=>r.Finished),"All four real AI opponents must finish the first vertical slice.");
            var finishers=race.Result.Standings.Where(r=>r.Finished).ToArray();for(int i=1;i<finishers.Length;i++)Assert.GreaterOrEqual(finishers[i].Seconds,finishers[i-1].Seconds);
            Assert.IsTrue(app.Save.Data.races.Any(r=>r.id==1));yield return new WaitForSecondsRealtime(.35f);BootstrapFlowTests.Capture("RaceResults");
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);}

        [UnityTest] public IEnumerator AllFiveTracksAndThreeFormatsUsePhysicalCompetitors()
        {
            var app=GameBootstrap.Instance;var failures=new System.Collections.Generic.List<string>();
            for(int track=1;track<=5;track++)
            {
                var format=track==2?RaceFormat.Elimination:track==3?RaceFormat.TimeTrial:RaceFormat.Obstacle;
                app.PlayRace(track,format,track==4?RaceDifficulty.Hard:RaceDifficulty.Medium,track==5?7:3);yield return WaitForRace(app);
                var race=app.Race;DrivePlayer(race);Time.timeScale=6;float end=Time.realtimeSinceStartup+100;
                while((race.State==RaceState.Countdown||race.State==RaceState.Racing)&&Time.realtimeSinceStartup<end)yield return null;
                if(race.State!=RaceState.Complete){failures.Add("Race "+track+" did not resolve");continue;}
                foreach(var racer in race.Standings)
                    Debug.Log("Race validation "+track+": "+racer.DisplayName+" finished="+racer.Finished+" eliminated="+racer.Eliminated+" progress="+racer.Progress+" falls="+racer.Falls+" point="+racer.Ball.Body.position);
                if(!race.Player.Finished)failures.Add("Player did not finish race "+track);
                if(format!=RaceFormat.Elimination&&format!=RaceFormat.TimeTrial&&race.Result.Standings.Count(r=>r.Finished)<2)failures.Add("AI did not finish race "+track);
                if(format==RaceFormat.Elimination&&!race.Result.Standings.Any(r=>r.Eliminated))failures.Add("No elimination occurred");
                if(format==RaceFormat.TimeTrial)Assert.AreEqual(1,race.Competitors.Count);
                yield return new WaitForSecondsRealtime(.2f);BootstrapFlowTests.Capture("RaceTrack"+track+"Result");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        [UnityTest] public IEnumerator ThreeRoundTournamentPersistsPointsAndRewards()
        {
            var app=GameBootstrap.Instance;app.StartTournament(RaceDifficulty.Medium,4);
            for(int round=0;round<3;round++)
            {
                yield return WaitForRace(app);DrivePlayer(app.Race);Time.timeScale=6;float end=Time.realtimeSinceStartup+100;
                while((app.Race.State==RaceState.Countdown||app.Race.State==RaceState.Racing)&&Time.realtimeSinceStartup<end)yield return null;
                Assert.AreEqual(RaceState.Complete,app.Race.State);Assert.IsTrue(app.Race.Player.Finished,"Tournament player did not finish round "+(round+1));
                if(round<2){Assert.IsFalse(app.Race.TournamentComplete);app.NextRace();}
            }
            Assert.IsTrue(app.Race.TournamentComplete);Assert.AreEqual(5,app.Race.TournamentStandings.Count);
            Assert.AreEqual(3,app.Race.TournamentStandings.Single(r=>r.IsPlayer).Finishes);
            Assert.IsTrue(app.Save.Data.races.Any(r=>r.id==1001));
            var reload=new SaveStore(directory,new UnitySaveCodec());reload.Load();Assert.AreEqual(app.Save.Data.coins,reload.Data.coins);Assert.AreEqual(app.Save.Data.races.Count,reload.Data.races.Count);
            BootstrapFlowTests.Capture("TournamentResult");
        }

        [UnityTest] public IEnumerator RaceFallRecoveryCountdownPauseAndRestartAreSafe()
        {
            var app=GameBootstrap.Instance;app.PlayRace(1,RaceFormat.Obstacle,RaceDifficulty.Easy,7);yield return WaitForRace(app);
            Time.timeScale=5;while(app.Race.State==RaceState.Countdown)yield return null;
            while(app.Race.Elapsed<3)yield return null;
            Assert.Greater(app.Race.PlayerPosition,1,"Physics-driven rivals must be able to overtake a player who does not accelerate.");
            var player=app.Race.Player;player.Ball.Body.position=new Vector3(0,-50,0);yield return new WaitForFixedUpdate();yield return null;
            Assert.IsTrue(player.Recovering);Time.timeScale=1;app.PauseCurrent();float timer=app.Race.Elapsed;
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(timer,app.Race.Elapsed);app.ResumeCurrent();
            float end=Time.realtimeSinceStartup+5;while(player.Recovering&&Time.realtimeSinceStartup<end)yield return null;
            Assert.IsFalse(player.Recovering);Assert.AreEqual(1,player.Falls);Assert.Greater(player.Ball.Body.position.y,30);
            app.RetryCurrent();yield return WaitForRace(app);Assert.AreEqual(8,app.Race.Competitors.Count);Assert.AreEqual(0,app.Race.Player.Falls);Assert.AreEqual(0,app.Race.Elapsed);
        }

        [Serializable]public sealed class EditorRaceMeasurement
        {
            public string environment,graphicsDevice,operatingSystem;
            public int competitors,frames,gen0Collections,renderers,colliders;
            public double courseBuildMilliseconds,meanWallFrameMilliseconds,medianFrameMilliseconds,p95FrameMilliseconds,unityAllocatedMiB;
            public bool physicalDevice;
        }
        [UnityTest]public IEnumerator RaceMenuAlternativeAndEightBallEditorMeasurement()
        {
            var app=GameBootstrap.Instance;
            app.UI.Races(false);yield return new WaitForSecondsRealtime(.25f);BootstrapFlowTests.Capture("RaceSelection");
            app.UI.Races(true);yield return new WaitForSecondsRealtime(.25f);BootstrapFlowTests.Capture("TournamentSelection");
            app.PlayRace(4,RaceFormat.Obstacle,RaceDifficulty.Hard,7);yield return WaitForRace(app);DrivePlayer(app.Race);
            Time.timeScale=1;while(app.Race.State==RaceState.Countdown)yield return null;
            for(int i=0;i<30;i++)yield return null;
            int collections=GC.CollectionCount(0);float start=Time.realtimeSinceStartup,previous=start;var frames=new float[180];
            for(int i=0;i<frames.Length;i++){yield return null;float now=Time.realtimeSinceStartup;frames[i]=(now-previous)*1000;previous=now;}
            Array.Sort(frames);
            var measurement=new EditorRaceMeasurement{environment="Unity Editor batchmode, active native render camera, target 60FPS; not an Android benchmark",graphicsDevice=SystemInfo.graphicsDeviceName,operatingSystem=SystemInfo.operatingSystem,competitors=app.Race.Competitors.Count,frames=frames.Length,gen0Collections=GC.CollectionCount(0)-collections,renderers=app.CurrentCourse.GetComponentsInChildren<Renderer>().Length,colliders=app.CurrentCourse.GetComponentsInChildren<Collider>().Length,courseBuildMilliseconds=app.LastCourseBuildMilliseconds,meanWallFrameMilliseconds=(Time.realtimeSinceStartup-start)*1000/frames.Length,medianFrameMilliseconds=frames[90],p95FrameMilliseconds=frames[171],unityAllocatedMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,physicalDevice=false};
            File.WriteAllText(Path.GetFullPath("TestResults/editor-race-measurement.json"),JsonUtility.ToJson(measurement,true));
            Time.timeScale=6;float end=Time.realtimeSinceStartup+85;
            while(app.Race.State==RaceState.Racing&&Time.realtimeSinceStartup<end)yield return null;
            Assert.AreEqual(RaceState.Complete,app.Race.State);
            if(!app.Race.Player.Finished)
                foreach(var racer in app.Race.Competitors)
                    Debug.Log("Canyon force diagnostic: "+racer.DisplayName+" position="+racer.Ball.Body.position+" velocity="+racer.Ball.Body.linearVelocity+" distance="+racer.Distance+" gate="+racer.Cursor.NextGate+" branch="+(racer.Cursor.ActiveAlternative!=null)+" completed="+racer.Cursor.CompletedAlternatives+" falls="+racer.Falls+" plannedSpeed="+(racer.Driver?racer.Driver.PlannedSpeed:0)+" plannedLane="+(racer.Driver?racer.Driver.PlannedLane:0)+" command="+(racer.Driver?racer.Driver.LastCommand.throttle:0)+","+(racer.Driver?racer.Driver.LastCommand.steering:0));
            Assert.IsTrue(app.Race.Player.Finished);
            Assert.IsTrue(app.Race.Competitors.Any(r=>r.Cursor.CompletedAlternatives>0),"The authored alternate route must be driven physically, not merely listed as metadata.");
            yield return new WaitForSecondsRealtime(.3f);BootstrapFlowTests.Capture("EightBallResults");
        }
    }
}
