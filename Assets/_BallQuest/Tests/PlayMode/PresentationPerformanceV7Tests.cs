using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class PresentationPerformanceV7Tests
    {
        Scene original,loaded;string directory;
        [Serializable]sealed class Measurement
        {
            public string environment,graphics;
            public bool physicalAndroidDevice,renderCountersAvailable;
            public int activeRenderers,colliderComponents,balls,frames,gen0Collections,maxDrawCalls,maxSetPassCalls,maxTriangles;
            public long maxManagedBytesInFrame;
            public double courseBuildMilliseconds,unityAllocatedMiB,meanFrameMs,medianFrameMs,p95FrameMs;
        }
        [UnityTest]
        public IEnumerator EightBallPresentationUsesSharedMeshesAndFewerActiveRenderers()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestPresentation-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();const string scene="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Additive);loaded=SceneManager.GetSceneByPath(scene);SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);app.Save.Data.unlockedRace=5;
            app.PlayRace(4,RaceFormat.Obstacle,RaceDifficulty.Hard,7);
            while(app.Race.State!=RaceState.Countdown&&Time.realtimeSinceStartup<end)yield return null;
            Assert.AreEqual(8,app.Race.Competitors.Count);
            var driver=app.Race.Player.Ball.gameObject.AddComponent<RaceDriver>();driver.Initialize(app.Race,app.Race.Player,RaceDifficulty.Hard);
            while(app.Race.State==RaceState.Countdown)yield return null;
            for(int i=0;i<60;i++)yield return null;
            foreach(var ball in app.Race.Competitors)
                Assert.LessOrEqual(ball.Ball.GetComponentsInChildren<MeshRenderer>().Length,4,"Cosmetic fidelity must not require dozens of separate renderers per ball.");
            int active=0;foreach(var renderer in app.CurrentCourse.GetComponentsInChildren<Renderer>())if(renderer.enabled)active++;
            Assert.Less(active,750,"The v0.6 eight-ball course used 1,135 renderers; consolidated geometry should materially reduce submission overhead.");
            var record=new Measurement{environment="Unity Windows Editor, native rendered eight-ball course, 60FPS target; counters include Editor overhead",graphics=SystemInfo.graphicsDeviceName,activeRenderers=active,colliderComponents=app.CurrentCourse.GetComponentsInChildren<Collider>().Length,balls=8,frames=240,courseBuildMilliseconds=app.LastCourseBuildMilliseconds,maxDrawCalls=-1,maxSetPassCalls=-1,maxTriangles=-1,maxManagedBytesInFrame=-1};
            var frames=new double[record.frames];int gen0=GC.CollectionCount(0);double previous=Time.realtimeSinceStartupAsDouble;
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1))
            using(var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count",1))
            using(var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1))
            using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
            {
                for(int i=0;i<frames.Length;i++)
                {
                    yield return null;double now=Time.realtimeSinceStartupAsDouble;frames[i]=(now-previous)*1000;previous=now;record.meanFrameMs+=frames[i]/frames.Length;
                    if(draws.Valid&&draws.LastValue>0){record.maxDrawCalls=Math.Max(record.maxDrawCalls,(int)draws.LastValue);record.renderCountersAvailable=true;}
                    if(passes.Valid&&passes.LastValue>0)record.maxSetPassCalls=Math.Max(record.maxSetPassCalls,(int)passes.LastValue);
                    if(triangles.Valid&&triangles.LastValue>0)record.maxTriangles=Math.Max(record.maxTriangles,(int)triangles.LastValue);
                    if(gc.Valid)record.maxManagedBytesInFrame=Math.Max(record.maxManagedBytesInFrame,gc.LastValue);
                }
            }
            record.gen0Collections=GC.CollectionCount(0)-gen0;Array.Sort(frames);record.medianFrameMs=frames[frames.Length/2];record.p95FrameMs=frames[(int)(frames.Length*.95)];record.unityAllocatedMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0;
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/presentation-performance-v10.json",JsonUtility.ToJson(record,true));
            BootstrapFlowTests.Capture("V10EightBallPresentation");
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
