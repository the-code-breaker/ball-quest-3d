using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class CrazyCampaignNativeViewsV9Tests
    {
        Scene original,loaded;string directory;
        [Serializable]sealed class View
        {public int id,activeRenderers;public string title,family,overview,precisionView;public float minimumWidth;public double editorCourseBuildMs,editorAllocatedMiB;}
        [Serializable]sealed class Report
        {public string scope="Twenty environment themes plus two late courses, native Unity overviews and supported inspection poses. Editor memory includes the Editor. Physical completion is recorded separately in grand-traversal-v11.csv.";public View[] views;}
        [UnityTest,Timeout(120000)]
        public IEnumerator ElevenNativeFamilyOverviewsAndPrecisionViewsShowShippingGeometry()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestCrazyViews-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();const string path="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);loaded=SceneManager.GetSceneByPath(path);SceneManager.SetActiveScene(loaded);
            float ready=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<ready)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);app.Save.Data.unlockedLevel=1000;
            var views=new List<View>();
            foreach(int id in new[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,999,1000})
            {
                app.Play(id);yield return BootstrapFlowTests.AwaitPlaying(app);
                var level=app.Session.Level;var ball=app.Session.Ball;ball.SetExternalControl(true);ball.SubmitControl(default);
                var bounds=new Bounds(level.spawn,Vector3.one);TrackPiece narrow=level.tracks[0];
                foreach(var piece in level.tracks){bounds.Encapsulate(piece.position);if(piece.size.x<narrow.size.x)narrow=piece;}
                var camera=Camera.main;var brain=camera.GetComponent<CinemachineBrain>();brain.enabled=false;
                var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var enabled=new bool[canvases.Length];
                for(int i=0;i<canvases.Length;i++){enabled[i]=canvases[i].enabled;canvases[i].enabled=false;}
                bool fog=RenderSettings.fog;RenderSettings.fog=false;float far=camera.farClipPlane;camera.farClipPlane=2000;camera.fieldOfView=50;
                float radius=bounds.extents.magnitude+7,horizontalHalf=Mathf.Atan(Mathf.Tan(25*Mathf.Deg2Rad)*9f/16);
                float distance=radius/Mathf.Sin(horizontalHalf)*1.06f;
                camera.transform.position=bounds.center+new Vector3(.7f,1.2f,-.85f).normalized*distance;camera.transform.LookAt(bounds.center);
                string overview="V11World"+id.ToString("00")+"Overview";BootstrapFlowTests.Capture(overview);
                camera.farClipPlane=far;RenderSettings.fog=fog;
                for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=enabled[i];brain.enabled=true;
                Quaternion rotation=Quaternion.Euler(narrow.rotation);Vector3 direction=rotation*Vector3.forward;
                ball.Respawn(narrow.position+rotation*Vector3.up*(narrow.size.y*.5f+.62f));ball.SetExternalControl(true);ball.SubmitControl(default);
                var follow=app.CurrentCourse.FollowCamera;follow.ResetOrbit();follow.Orbit(new Vector2(Vector3.SignedAngle(Vector3.forward,direction,Vector3.up)/follow.orbitSensitivity,0));follow.Snap();
                yield return new WaitForSeconds(.4f);
                Assert.AreEqual(SessionState.Playing,app.Session.State);Assert.AreEqual(0,app.Session.Falls);Assert.IsTrue(ball.Grounded,"Inspection pose must be physically supported.");
                string precision="V11World"+id.ToString("00")+"Precision";BootstrapFlowTests.Capture(precision);
                int active=0;foreach(var r in app.CurrentCourse.GetComponentsInChildren<Renderer>())if(r.enabled)active++;
                Assert.Less(active,750,"Shared-material batches must bound renderer submission costs.");
                views.Add(new View{id=id,title=level.title,family=level.signatureMechanic,minimumWidth=narrow.size.x,activeRenderers=active,editorCourseBuildMs=app.LastCourseBuildMilliseconds,editorAllocatedMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,overview="TestResults/Previews/"+overview+".png",precisionView="TestResults/Previews/"+precision+".png"});
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/grand-native-views-v11.json",JsonUtility.ToJson(new Report{views=views.ToArray()},true));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
