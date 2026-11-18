using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace BallQuest.Tests
{
    public sealed class UpgradeAcceptanceV10Tests
    {
        Scene original,loaded;string directory;GameBootstrap app;
        [UnitySetUp]public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestV10-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();const string path="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);loaded=SceneManager.GetSceneByPath(path);SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+20;while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);app.Save.Data.unlockedLevel=1000;
        }
        void Click(string name){var b=app.UI.GetComponentsInChildren<Button>().Single(x=>x.name==name);Assert.IsTrue(b.interactable);b.onClick.Invoke();}
        [UnityTest]public IEnumerator NativeChapterButtonsAndEveryPaginationPageDisplayOneThroughOneThousandInOrder()
        {
            var ids=new List<int>();
            for(int chapter=1;chapter<=20;chapter++)
            {
                app.UI.Worlds();while(app.UI.GetComponentsInChildren<Button>().Single(x=>x.name=="Previous chapter page").interactable)Click("Previous chapter page");for(int section=0;section<(chapter-1)/4;section++)Click("Next chapter page");Click("Chapter "+chapter);
                for(int page=0;page<5;page++)
                {
                    yield return null;
                    foreach(var button in app.UI.GetComponentsInChildren<Button>())if(int.TryParse(button.name.Split(' ')[0],out int id))ids.Add(id);
                    if(chapter==1&&page==0){yield return new WaitForSecondsRealtime(.3f);BootstrapFlowTests.Capture("V10SequentialChapter");}
                    if(chapter==20&&page==4)
                    {
                        var last=app.UI.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.text=="1000");last.ForceMeshUpdate();Assert.AreEqual(1,last.textInfo.lineCount);Assert.IsFalse(last.isTextOverflowing,"Four-digit course IDs must remain readable.");
                        BootstrapFlowTests.Capture("V11SequentialEnd");
                    }
                    var next=app.UI.GetComponentsInChildren<Button>().Single(x=>x.name=="Next adventure page");
                    if(page<4){Assert.IsTrue(next.interactable);next.onClick.Invoke();}else Assert.IsFalse(next.interactable);
                }
            }
            CollectionAssert.AreEqual(Enumerable.Range(1,1000).ToArray(),ids);
            app.UI.Worlds();Click("All levels");yield return null;
            Assert.IsTrue(app.UI.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("01 ")),"Changing lists must reset the previous page.");
            File.WriteAllText("TestResults/challenge-navigation-v11.json",JsonUtility.ToJson(new Navigation{ids=ids.ToArray(),chapterCount=20,allCoursesResetToFirstPage=true},true));
            app.Save.Data.levels.Clear();app.Save.Data.levels.AddRange(Enumerable.Range(1,1000).Select(id=>new LevelRecord{id=id,stars=3}));app.UI.Worlds();Click("All levels");yield return null;
            var total=app.UI.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.text=="3000/3000");total.ForceMeshUpdate();Assert.AreEqual(1,total.textInfo.lineCount);Assert.IsFalse(total.isTextOverflowing,"Full campaign star totals must fit.");
        }
        [Serializable]sealed class Navigation{public int[] ids;public int chapterCount;public bool allCoursesResetToFirstPage;}
        [UnityTest]public IEnumerator NewBallModelsUseTheirOwnRecordedContactTimbresOnTheSameFloor()
        {
            var impacts=new HashSet<AudioClip>();var images=new List<string>();
            foreach(string id in new[]{"brushed-steel","carbon-rubber","copper-core","walnut"})
            {
                app.Save.Data.selectedSkin=id;if(!app.Save.Data.skins.Contains(id))app.Save.Data.skins.Add(id);
                app.Play(1);yield return BootstrapFlowTests.AwaitPlaying(app);yield return new WaitForSeconds(.3f);
                var ball=app.Session.Ball;var audio=ball.GetComponent<BallContactAudio>();var skin=app.catalog.Skin(id);
                Assert.AreEqual(BallAudioProfile.ResolveMaterial(ball.profile,skin),audio.CurrentMaterial);
                Assert.IsTrue(audio.profile.rolling.name.Contains(audio.CurrentMaterial.ToString()));
                yield return new WaitForSeconds(.12f);
                Assert.IsTrue(audio.ReportImpactAt(7,ball.GroundDetector.SupportCollider,ball.GroundDetector.Point,ball.GroundNormal));
                if(id!="copper-core")Assert.IsTrue(impacts.Add(audio.LastImpactClip),"Metal, rubber and wood must have different impacts on the same road.");
                foreach(var renderer in ball.GetComponentsInChildren<MeshRenderer>())Assert.IsTrue(renderer.sharedMaterial.shader.isSupported);
                Assert.LessOrEqual(ball.GetComponentsInChildren<MeshRenderer>().Length,4);
                BootstrapFlowTests.Capture("V10Material-"+id);images.Add("V10Material-"+id+".png");
            }
            foreach(var demo in new[]{(id:3,kind:ObstacleKind.Piston),(id:4,kind:ObstacleKind.SweepArm),(id:2,kind:ObstacleKind.RollerDrum),(id:7,kind:ObstacleKind.Hammer),(id:7,kind:ObstacleKind.TimedDoor)})
            {
                app.Play(app.catalog.levels.First(l=>l.obstacles.Any(o=>o.kind==demo.kind)).id);yield return BootstrapFlowTests.AwaitPlaying(app);
                var hazard=app.CurrentCourse.GetComponentsInChildren<QuestObstacle>().First(h=>h.kind==demo.kind);
                if(demo.kind==ObstacleKind.TimedDoor){hazard.phase=4.2f;hazard.speed=0;hazard.ResetCourseState();Assert.IsFalse(hazard.IsPassable);}
                var visible=hazard.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();Assert.That(visible.Length,Is.InRange(2,6));
                foreach(var renderer in visible)Assert.IsTrue(renderer.sharedMaterial.shader.isSupported);
                var spec=app.Session.Level.obstacles.First(o=>o.kind==demo.kind);float distance=Mathf.Max(3.8f,spec.size.x*3);
                var camera=Camera.main;var brain=camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();brain.enabled=false;
                var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var active=canvases.Select(c=>c.enabled).ToArray();for(int i=0;i<canvases.Length;i++)canvases[i].enabled=false;
                camera.transform.position=hazard.transform.position+hazard.transform.rotation*new Vector3(-distance*.55f,distance*.55f,-distance);camera.transform.LookAt(hazard.transform.position+Vector3.up*.3f);
                BootstrapFlowTests.Capture("V10Mechanism-"+demo.kind);images.Add("V10Mechanism-"+demo.kind+".png");
                if(demo.kind==ObstacleKind.TimedDoor)
                {
                    Vector3 physical=hazard.GetComponent<Rigidbody>().position;hazard.phase=0;hazard.ResetCourseState();Assert.IsTrue(hazard.IsPassable);
                    Assert.AreEqual(physical,hazard.GetComponent<Rigidbody>().position);
                    Assert.IsTrue(visible.Any(r=>r.transform.localPosition.y>3),"The open shutter must visibly clear the road.");
                    BootstrapFlowTests.Capture("V10Mechanism-TimedDoorOpen");images.Add("V10Mechanism-TimedDoorOpen.png");
                }
                for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=active[i];brain.enabled=true;
            }
            File.WriteAllText("TestResults/material-models-v10.json",JsonUtility.ToJson(new Models{images=images.ToArray(),differentBodyImpactsOnSameFloor=impacts.Count==3},true));
        }
        [Serializable]sealed class Models{public string[] images;public bool differentBodyImpactsOnSameFloor;}
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
