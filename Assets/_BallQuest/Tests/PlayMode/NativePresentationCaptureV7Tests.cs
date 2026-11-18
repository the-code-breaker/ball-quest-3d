using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    /// <summary>Native gameplay captures for human art review, not replacement artwork.</summary>
    public sealed class NativePresentationCaptureV7Tests
    {
        Scene original,loaded;
        string directory;
        [Serializable]sealed class CaptureRecord
        {
            public int level;
            public string image;
            public Vector3 ball,camera;
            public int rewardCount,rewardRenderers,boostPads;
            public bool grounded;
            public double buildMilliseconds;
        }
        [Serializable]sealed class CaptureReport
        {
            public string environment="Native Unity Windows Editor gameplay camera; no Android visual claim";
            public CaptureRecord[] captures;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator GameplayCameraShowsPolishedRewardsRoadCurvesAndActiveAcceleratorArtwork()
        {
            Time.timeScale=1;
            directory=Path.Combine(Path.GetTempPath(),"BallQuestNativeArtV7-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();
            const string path="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath(path);SceneManager.SetActiveScene(loaded);
            float ready=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<ready)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);
            app.Save.Data.unlockedLevel=1000;
            var report=new List<CaptureRecord>();
            int boostId=System.Array.Find(app.catalog.levels,l=>System.Array.Exists(l.obstacles,o=>o.kind==ObstacleKind.Booster)).id;
            int[] ids={1,56,boostId};
            string[] names={"V10HarborCurveAndCoins","V10SkyMazePresentation","V10AcceleratorAndCoins"};
            for(int index=0;index<ids.Length;index++)
            {
                if(index==1)app.PlayExpedition(ids[index]);else app.Play(ids[index]);yield return BootstrapFlowTests.AwaitPlaying(app);
                var course=app.CurrentCourse;var ball=app.Session.Ball;
                // Explicit recovery chooses a representative review location. The actual
                // shipped geometry, models, materials, HUD and chase camera render it.
                Vector3 position,direction;
                if(ids[index]==56){position=new Vector3(0,24.65f,9);direction=Vector3.forward;}
                else if(index==2)
                {
                    var pad=System.Array.Find(app.Session.Level.obstacles,o=>o.kind==ObstacleKind.Booster);Assert.IsNotNull(pad);
                    direction=pad.axis.normalized;position=pad.position-direction*(pad.size.z*.5f+2.4f)+Vector3.up*.53f;
                }
                else
                {
                    var points=app.Session.Level.routePoints;int point=Mathf.Min(12,points.Length-2);
                    position=points[point]+Vector3.up*.65f;direction=(points[point+1]-points[point]).normalized;
                }
                ball.Respawn(position);ball.SetExternalControl(true);ball.SubmitControl(default);
                course.FollowCamera.ResetOrbit();course.FollowCamera.Orbit(new Vector2(Vector3.SignedAngle(Vector3.forward,direction,Vector3.up)/course.FollowCamera.orbitSensitivity,0));course.FollowCamera.Snap();
                yield return new WaitForSeconds(.65f);
                Assert.AreEqual(SessionState.Playing,app.Session.State);Assert.AreEqual(0,app.Session.Falls);
                Assert.IsTrue(ball.Grounded,"Native art review requires supported actual ball geometry.");
                var rewards=course.GetComponentsInChildren<Collectible>();Assert.Greater(rewards.Length,0);
                int rewardRenderers=0;
                foreach(var reward in rewards)
                {
                    var renderers=reward.GetComponentsInChildren<MeshRenderer>();
                    Assert.AreEqual(2,renderers.Length,"Every reward must use its upgraded two-part mesh.");
                    foreach(var renderer in renderers)
                    {
                        Assert.IsTrue(renderer.sharedMaterial&&renderer.sharedMaterial.shader&&renderer.sharedMaterial.shader.isSupported);
                        Assert.AreNotEqual("Hidden/InternalErrorShader",renderer.sharedMaterial.shader.name);
                    }
                    rewardRenderers+=renderers.Length;
                }
                var pads=course.GetComponentsInChildren<PremiumBoosterVisuals>();
                if(index==2)
                {
                    Assert.Greater(pads.Length,0);
                    foreach(var pad in pads)
                    {
                        var meshes=pad.GetComponentsInChildren<MeshRenderer>();Assert.AreEqual(3,meshes.Length);
                        bool flow=false;foreach(var mesh in meshes)flow|=mesh.sharedMaterial.shader.name=="BallQuest/Boost Flow"&&mesh.sharedMaterial.shader.isSupported;
                        Assert.IsTrue(flow,"The actual course must include the animated accelerator material.");
                    }
                }
                BootstrapFlowTests.Capture(names[index]);
                if(index==0)
                {
                    var audio=ball.GetComponent<BallContactAudio>();Assert.IsNotNull(audio);
                    Assert.IsTrue(audio.Feedback.Emit(ball.GroundDetector.Point,ball.GroundNormal,BallSurfaceTag.Identify(ball.GroundDetector.SupportCollider),9));
                    yield return new WaitForSeconds(.07f);BootstrapFlowTests.Capture("V10ContactFeedback");
                }
                report.Add(new CaptureRecord{level=ids[index],image="TestResults/Previews/"+names[index]+".png",ball=ball.Body.position,camera=Camera.main.transform.position,rewardCount=rewards.Length,rewardRenderers=rewardRenderers,boostPads=pads.Length,grounded=ball.Grounded,buildMilliseconds=app.LastCourseBuildMilliseconds});
            }
            var output=Path.GetFullPath("TestResults/native-presentation-v10.json");Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonUtility.ToJson(new CaptureReport{captures=report.ToArray()},true));
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
