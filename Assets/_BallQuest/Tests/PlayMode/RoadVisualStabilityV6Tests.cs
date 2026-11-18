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
    public sealed class RoadVisualStabilityV6Tests
    {
        Scene original,loaded;
        string directory;
        [Serializable]sealed class FrameRecord
        {
            public string image;
            public Vector3 ball,camera;
            public float speed;
            public bool grounded;
        }
        [Serializable]sealed class RenderRecord
        {
            public string platform;
            public bool physicalAndroidDevice;
            public double actualCourseBuildMilliseconds,isolatedVisibleRoadBuildMilliseconds;
            public int roadColliderCount,unchangedSolidVertices,originalRoadTriangles,visibleRoadVertices,visibleRoadTriangles;
            public FrameRecord[] frames;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator MovingBallAndChaseCameraRenderAcrossTheActualMazeJunction()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestRoadVisuals-"+Guid.NewGuid());
            GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();
            const string scene="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath(scene);SceneManager.SetActiveScene(loaded);
            float ready=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<ready)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);
            app.PlayExpedition(56);yield return BootstrapFlowTests.AwaitPlaying(app);
            var course=app.CurrentCourse;var ball=app.Session.Ball;
            var record=new RenderRecord{platform=Application.platform.ToString(),physicalAndroidDevice=false,actualCourseBuildMilliseconds=app.LastCourseBuildMilliseconds};

            foreach(var collider in course.GetComponentsInChildren<MeshCollider>())
            {
                if(collider.name!="Wooden racing surface")continue;
                record.roadColliderCount++;record.unchangedSolidVertices+=collider.sharedMesh.vertexCount;
                Assert.AreEqual(8,collider.sharedMesh.vertexCount,"Render clipping must leave the original complete solid collider intact.");
                Assert.AreNotSame(collider.sharedMesh,collider.GetComponent<MeshFilter>().sharedMesh,"Visible road faces must be separate from the padded collision solid.");
            }
            Assert.AreEqual(app.Session.Level.tracks.Length,record.roadColliderCount);
            // These samples straddle the real 50m-wide area, both of its seams, and
            // the continuing narrow lane. Rendering subtraction must never open physics gaps.
            for(float z=14.13f;z<34;z+=.31f)
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(0,25.6f,z),Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore),"Missing support at "+z);
                Assert.AreEqual("Wooden racing surface",hit.collider.name,"Unexpected support across the junction at "+z);
                Assert.AreEqual(24,hit.point.y,.002f);
            }
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var roadMeshes=RoadRenderGeometry.Build(app.Session.Level.tracks);watch.Stop();
            record.isolatedVisibleRoadBuildMilliseconds=watch.Elapsed.TotalMilliseconds;
            record.originalRoadTriangles=app.Session.Level.tracks.Length*12;
            foreach(var mesh in roadMeshes){record.visibleRoadVertices+=mesh.vertexCount;record.visibleRoadTriangles+=mesh.triangles.Length/3;UnityEngine.Object.Destroy(mesh);}

            // Recovery is fixture setup. Every subsequent captured metre is produced by
            // the shipping force controller, with the active native Cinemachine camera.
            ball.Respawn(new Vector3(0,24.65f,13.8f));ball.SetExternalControl(true);ball.SetPathForward(Vector3.forward);
            ball.SubmitControl(default);course.FollowCamera.ResetOrbit();course.FollowCamera.mode=BallCameraMode.StableOrbit;
            yield return new WaitForSeconds(.2f);
            var camera=Camera.main;Assert.IsNotNull(camera);
            var initialCamera=camera.transform.position;
            var frames=new List<FrameRecord>();
            ball.SubmitControl(new BallControlCommand(1,0));
            for(int frame=0;frame<7;frame++)
            {
                if(frame>0)yield return new WaitForSeconds(.25f);
                Assert.AreEqual(SessionState.Playing,app.Session.State);Assert.AreEqual(0,app.Session.Falls);
                var toBall=ball.transform.position+ball.MovementUp*.2f-camera.transform.position;
                var sightline=new Ray(camera.transform.position,toBall.normalized);
                foreach(var renderer in course.GetComponentsInChildren<MeshRenderer>())
                {
                    if(renderer.name!="Arch header")continue;
                    bool blocking=renderer.bounds.IntersectRay(sightline,out float distance)&&distance<toBall.magnitude;
                    Assert.IsFalse(blocking,"Gate header blocks the moving camera's view of the ball in frame "+frame);
                }
                foreach(var sign in course.GetComponentsInChildren<CourseSignVisibility>())
                    if(Vector3.Distance(sign.transform.position,camera.transform.position)<sign.hiddenDistance-.1f)
                        Assert.Less(sign.VisibilityAlpha,.01f,"A nearby world label must fade before its glyphs fill the camera.");
                string image="V10SkyMazeMoving"+frame.ToString("00");
                BootstrapFlowTests.Capture(image);
                frames.Add(new FrameRecord{image="Previews/"+image+".png",ball=ball.Body.position,camera=camera.transform.position,speed=ball.Body.linearVelocity.magnitude,grounded=ball.Grounded});
            }
            ball.SubmitControl(default);
            Assert.Greater(ball.Body.position.z,28,"The physical ball must actually cross the overlapping area into its next lane.");
            Assert.Greater(Vector3.Distance(initialCamera,camera.transform.position),6,"The native camera must move during the captured sequence.");
            Assert.IsTrue(ball.Grounded);
            record.frames=frames.ToArray();
            string report=Path.GetFullPath("TestResults/road-rendering-v10.json");Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllText(report,JsonUtility.ToJson(record,true));
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
