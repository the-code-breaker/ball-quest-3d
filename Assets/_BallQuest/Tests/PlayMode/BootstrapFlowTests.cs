using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BallQuest.Tests
{
    public sealed class BootstrapFlowTests
    {
        const string ScenePath = "Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
        Scene original;
        Scene loaded;
        string directory;

        [UnityTest]
        public IEnumerator NewCampaignMenuDisplaysActualDistinctRouteSilhouettes()
        {
#if UNITY_EDITOR
            directory=Path.Combine(Path.GetTempPath(),"BallQuestRouteMenu-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
#endif
            original=SceneManager.GetActiveScene();yield return SceneManager.LoadSceneAsync(ScenePath,LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath(ScenePath);SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);app.UI.Worlds();Click(app,"All levels");
            yield return new WaitForSecondsRealtime(.25f);Capture("V10LevelSilhouettes");
            var cards=app.UI.GetComponentsInChildren<CourseRouteGraphic>();Assert.AreEqual(12,cards.Length);
            foreach(var card in cards)
            {Assert.IsNotNull(card.level);var mesh=card.GetComponent<CanvasRenderer>().GetMesh();Assert.IsNotNull(mesh);Assert.Greater(mesh.vertexCount,20,"Actual route silhouette must render in the level card.");}
        }

        [UnityTest]
        public IEnumerator MainMenuStartsCourseAndPauseMenuNavigates()
        {
#if UNITY_EDITOR
            directory = Path.Combine(Path.GetTempPath(), "BallQuestFlow-" + Guid.NewGuid());
            GameBootstrap.EditorSaveDirectory = directory;
#endif
            original = SceneManager.GetActiveScene();
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
            loaded = SceneManager.GetSceneByPath(ScenePath); SceneManager.SetActiveScene(loaded);
            float deadline=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            var app = GameBootstrap.Instance;
            Assert.IsNotNull(app);
            Assert.IsTrue(app.Ready,"Bootstrap did not finish preparing its UI.");
            Assert.IsNotNull(app.UI, "Run Tools > Ball Quest > Generate Project Content before testing the bootstrap scene.");
            Capture("MainMenu");
            Click(app, "PLAY");
            yield return AwaitPlaying(app);
            Assert.AreEqual(SessionState.Playing, app.Session.State); Assert.IsNotNull(app.Session.Ball);
            Assert.AreEqual(1,app.Session.Level.id,"PLAY must continue the varied campaign, rather than reopening the same five featured adventures.");Assert.IsFalse(app.Session.Ball.ExternalControl);
            var resting=app.Session.Ball.Body.position;yield return new WaitForSeconds(.5f);
            Assert.Less(Vector3.ProjectOnPlane(app.Session.Ball.Body.position-resting,Vector3.up).magnitude,.04f,"The real player cannot auto-accelerate at adventure start.");
            app.Session.Pause(); Assert.AreEqual(0, Time.timeScale);
            Click(app,"Settings");yield return new WaitForSecondsRealtime(.25f);Capture("Settings");
            Click(app,"Controls & camera");Click(app,"Touch control");
            Assert.AreEqual(0,app.Save.Data.settings.touchMode);Assert.AreEqual(BallCameraMode.AdaptiveChase,app.CurrentCourse.FollowCamera.mode);Capture("ControlSettings");
            Click(app,"Save controls");Click(app,"Save & back");Click(app,"Resume");
            Assert.AreEqual(SessionState.Playing, app.Session.State); Assert.AreEqual(1, Time.timeScale);
            app.Session.Pause(); Click(app, "Main menu"); Click(app, "Ball collection");
            Assert.IsNotNull(Array.Find(app.UI.GetComponentsInChildren<Button>(), b => b.name == "Change ball type"));
            Click(app, "Back"); Click(app, "Worlds & levels");
            yield return new WaitForSecondsRealtime(.25f);Capture("Worlds");
            var world = Array.Find(app.UI.GetComponentsInChildren<Button>(), b => b.name=="Chapter 1");
            Assert.IsNotNull(world); world.onClick.Invoke();
            Assert.IsNotNull(Array.Find(app.UI.GetComponentsInChildren<Button>(), b => b.name.StartsWith("01", StringComparison.Ordinal)));
            app.MainMenu();app.UI.Missions();yield return new WaitForSecondsRealtime(.25f);Capture("Missions");
            app.UI.DailyRewards();yield return new WaitForSecondsRealtime(.25f);Capture("DailyRewards");
            Click(app,"Claim daily reward");Assert.IsFalse(ArcadeProgress.CanClaimDaily(app.Save.Data));
            app.Save.Data.lifetimeCoins=50;app.UI.Missions();Click(app,"Claim mission coin-scout");Assert.Contains("coin-scout",app.Save.Data.claimedMissions);
            app.UI.Shop();yield return new WaitForSecondsRealtime(.25f);Capture("Shop");
            app.Save.Data.coins=1200;app.UI.Shop();Click(app,"Buy neon");
            Assert.Contains("neon",app.Save.Data.skins);Assert.AreEqual(600,app.Save.Data.coins);
            Click(app,"Buy neon");Assert.AreEqual(600,app.Save.Data.coins);Assert.AreEqual("neon",app.Save.Data.selectedSkin);
            app.Save.Data.selectedSkin="classic-red";
            app.UI.Spin();yield return new WaitForSecondsRealtime(.25f);Capture("Spin");
            Click(app,"Free daily spin");yield return new WaitForSecondsRealtime(3.4f);
            Assert.AreEqual(1,app.Save.Data.lifetimeSpins);Assert.IsFalse(ArcadeProgress.CanSpin(app.Save.Data));Capture("SpinResult");
            app.Save.Data.unlockedLevel = app.catalog.levels.Length;
            foreach (var course in Array.FindAll(app.catalog.levels,l=>Array.IndexOf(new[]{1,50,51,56,120,121,250,500,750,999,1000},l.id)>=0))
            {
                app.Play(course.id);
                yield return AwaitPlaying(app);
                for (int frame = 0; frame < 30; frame++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(SessionState.Playing, app.Session.State, "Course failed at spawn: " + course.title);
                Assert.Greater(app.Session.Ball.Body.position.y, 0.1f, "Spawn support missing: " + course.title);
                Assert.IsNotNull(Camera.main, "Render camera missing: " + course.title);
                Capture("Course" + course.id);
                if(course.id==1)
                {
                    app.Session.Ball.Boost();
                    for(int frame=0;frame<45;frame++)yield return new WaitForFixedUpdate();
                    Capture("BoostFraming");
                }
            }
            app.MainMenu();app.UI.Collection();yield return new WaitForSecondsRealtime(.25f);Capture("Collection");
            app.UI.Worlds();Click(app,"All levels");yield return new WaitForSecondsRealtime(.25f);Capture("Courses");
            var routeCards=app.UI.GetComponentsInChildren<CourseRouteGraphic>();Assert.Greater(routeCards.Length,0);
            foreach(var routeCard in routeCards)
            {var mesh=routeCard.GetComponent<CanvasRenderer>().GetMesh();Assert.IsNotNull(mesh);Assert.Greater(mesh.vertexCount,20,"Level selection must display the actual route shape.");}
            app.Play(1);yield return AwaitPlaying(app);app.Session.Ball.Body.position=Vector3.down*100;
            yield return null;yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(SessionState.Fallen,app.Session.State);Capture("Failed");
        }

        public static IEnumerator AwaitPlaying(GameBootstrap app)
        {
            float end=Time.realtimeSinceStartup+15;
            while((app.Loading||app.Session.State!=SessionState.Playing)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.AreEqual(SessionState.Playing,app.Session.State,"Course did not enter Playing.");
        }

        public static void Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var camera = Camera.main;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            var target = new RenderTexture(720, 1280, 24);
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            target.antiAliasing=pipeline?pipeline.msaaSampleCount:1;
            var previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; distances[i] = canvases[i].planeDistance;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 0.5f;
                }
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                texture = new Texture2D(720, 1280, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); texture.Apply();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/Previews"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (!canvases[i]) continue;
                    canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i];
                }
                target.Release(); UnityEngine.Object.Destroy(target);
                if (texture) UnityEngine.Object.Destroy(texture);
            }
        }

        static void Click(GameBootstrap app, string name)
        {
            var button = Array.Find(app.UI.GetComponentsInChildren<Button>(), b => b.name == name);
            Assert.IsNotNull(button, "Missing UI action: " + name); Assert.IsTrue(button.interactable);
            button.onClick.Invoke();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
#if UNITY_EDITOR
            GameBootstrap.EditorSaveDirectory = null;
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
#endif
        }
    }
}
