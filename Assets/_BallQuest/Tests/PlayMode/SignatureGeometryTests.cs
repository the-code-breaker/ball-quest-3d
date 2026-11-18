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
    public sealed class SignatureGeometryTests
    {
        Scene original,loaded;
        string directory;

        [UnityTest,Timeout(90000)]
        public IEnumerator BothMazeExitsAcceptPhysicalCrossingsNearEitherRoadEdge()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestMazeEdges-"+Guid.NewGuid());
            GameBootstrap.EditorSaveDirectory=directory;original=SceneManager.GetActiveScene();
            const string scene="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath(scene);SceneManager.SetActiveScene(loaded);
            float deadline=Time.realtimeSinceStartup+25;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<deadline)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);
            foreach(float exitX in new[]{-14f,14f})foreach(float offset in new[]{-4.25f,4.25f})
            {
                app.PlayExpedition(56);yield return BootstrapFlowTests.AwaitPlaying(app);
                var finishes=app.CurrentCourse.GetComponentsInChildren<CourseTrigger>().Where(gate=>gate.finish).ToArray();
                Assert.AreEqual(2,finishes.Length);
                foreach(var finish in finishes)
                {
                    Assert.Greater(Vector3.Dot(finish.transform.forward,Vector3.forward),.999f,"Each finish must align with its actual straight exit road.");
                    Assert.GreaterOrEqual(finish.GetComponent<BoxCollider>().bounds.size.x,9.49f,"Trigger must cover the usable width of the 10m exit road.");
                }
                // Isolate finish coverage. Route tests separately reach this checkpoint through real input.
                // Recovery is used only as fixture setup; the tested crossing itself is force-driven.
                app.Session.Checkpoint(0,app.Session.Level.checkpoints[0]);
                var ball=app.Session.Ball;ball.Respawn(new Vector3(exitX+offset,24.65f,111));
                ball.SetExternalControl(true);ball.SetPathForward(Vector3.forward);ball.SubmitControl(new BallControlCommand(1,0));
                float end=Time.realtimeSinceStartup+4;
                while(app.Session.State==SessionState.Playing&&Time.realtimeSinceStartup<end)yield return null;
                Assert.AreEqual(SessionState.Complete,app.Session.State,"Edge crossing failed at exit "+exitX+", offset "+offset+", ball "+ball.Body.position);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);
            GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
