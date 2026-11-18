using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class BoosterArtV9Tests
    {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        Scene original,loaded;
        string directory;
        T Keep<T>(T value)where T:UnityEngine.Object{owned.Add(value);return value;}

        [Test]
        public void AcceleratorUsesRaisedDrivePlatesOpenIntervalsAndBoundedGeometry()
        {
            var body=Keep(PremiumPickupGeometry.PadHousing());var alloy=Keep(PremiumPickupGeometry.PadTrim());var light=Keep(PremiumPickupGeometry.PadArrows());
            int triangles=0;
            foreach(var mesh in new[]{body,alloy,light})
            {
                triangles+=mesh.triangles.Length/3;
                foreach(var vertex in mesh.vertices)
                {
                    Assert.IsFalse(float.IsNaN(vertex.x)||float.IsNaN(vertex.y)||float.IsNaN(vertex.z));
                    Assert.LessOrEqual(Mathf.Abs(vertex.x),.51f);Assert.LessOrEqual(Mathf.Abs(vertex.z),.51f);
                }
                foreach(var normal in mesh.normals)Assert.That(normal.sqrMagnitude,Is.InRange(.99f,1.01f));
            }
            Assert.Greater(body.bounds.max.y,.20f,"Fins need real sculpted depth rather than painted outlines.");
            Assert.Greater(alloy.bounds.max.y,.15f);Assert.Less(triangles,1800,"Three shared meshes must stay inexpensive on mobile.");
            int covered=0,open=0,recessed=0;
            for(float z=-.44f;z<.47f;z+=.025f)
            {
                bool plate=TopCovers(alloy,new Vector2(0,z));if(plate)covered++;else{open++;if(TopCovers(body,new Vector2(0,z),.005f))recessed++;}
            }
            Assert.Greater(covered,8,"Raised drive plates must remain readable down the runway.");
            Assert.Greater(open,8,"Distinct drive plates need clear intervals between their bright faces.");
            Assert.AreEqual(open,recessed,"The gaps must reveal a continuous recessed dark cassette, not bare road.");
        }
        static bool TopCovers(Mesh mesh,Vector2 point,float minimumHeight=.025f)
        {
            var vertices=mesh.vertices;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 va=vertices[triangles[i]],vb=vertices[triangles[i+1]],vc=vertices[triangles[i+2]];
                if(Mathf.Min(va.y,Mathf.Min(vb.y,vc.y))<minimumHeight)continue;
                Vector2 a=new Vector2(va.x,va.z),b=new Vector2(vb.x,vb.z),c=new Vector2(vc.x,vc.z);
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<.000001f)continue;
                float u=Cross(point-a,c-a)/area,v=Cross(b-a,point-a)/area;
                if(u>=-.00001f&&v>=-.00001f&&u+v<=1.00001f)return true;
            }
            return false;
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;

        [Test]
        public void DriveLaunchAndConveyorKeepThreeSharedPartsAndIndependentActivation()
        {
            var course=Keep(new GameObject("V10 accelerator resource owner"));
            var kinds=new[]{ObstacleKind.Booster,ObstacleKind.JumpPad,ObstacleKind.Conveyor};
            Mesh first=null;
            var pads=new List<PremiumBoosterVisuals>();
            for(int i=0;i<kinds.Length;i++)
            {
                var item=new GameObject(kinds[i]+" trigger");item.transform.SetParent(course.transform);item.transform.position=Vector3.right*(i*7);
                var trigger=item.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(5,.85f,5);
                var pad=PremiumBoosterVisuals.Attach(item.transform,new ObstacleSpec{kind=kinds[i],size=new Vector3(5,.2f,5)},null);pads.Add(pad);
                Assert.AreEqual(kinds[i],pad.Kind);Assert.AreEqual(3,item.GetComponentsInChildren<MeshRenderer>().Length);
                Assert.AreEqual(1,item.GetComponentsInChildren<Collider>().Length);
                var mesh=item.GetComponentInChildren<MeshFilter>().sharedMesh;
                if(i==0)first=mesh;else Assert.AreSame(first,mesh);
                var block=new MaterialPropertyBlock();
                foreach(var renderer in item.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial.shader.name=="BallQuest/Boost Flow")
                    {renderer.GetPropertyBlock(block);Assert.AreEqual(i,block.GetFloat("_FlowType"));}
            }
            var library=course.GetComponent<PremiumCollectibleVisuals>();Assert.AreEqual(7,library.SharedMeshCount);Assert.AreEqual(10,library.SharedMaterialCount);
            Vector3 root=pads[0].transform.position;var assembly=pads[0].transform.GetChild(0);Vector3 rest=assembly.localPosition;
            pads[0].Activate(null);Assert.AreEqual(1,pads[0].Activation);Assert.AreEqual(0,pads[1].Activation);
            for(int step=0;step<12;step++)library.Animate(step*.01f,.01f,new Vector3(0,5,-8));
            Assert.Greater(assembly.localPosition.y,rest.y);Assert.AreEqual(root,pads[0].transform.position,"Only the cosmetic assembly may respond to activation.");
            pads[0].ResetVisual();Assert.AreEqual(0,pads[0].Activation);Assert.AreEqual(rest,assembly.localPosition);
        }

        [Serializable]sealed class CaptureEvidence
        {
            public string scope="Native Unity gameplay and model-inspection renders of the same shared accelerator geometry; not Android GPU evidence";
            public int meshes,materials,renderersPerPad=3;
            public bool actualBoostTriggered;
            public float activation,speed;
            public string[] images;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator NativeRunwayAndThreeVariantCloseupsRenderAndRealCrossingActivates()
        {
            Time.timeScale=1;
            directory=Path.Combine(Path.GetTempPath(),"BallQuestBoosterV9-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();const string path="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);loaded=SceneManager.GetSceneByPath(path);SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);
            var fixture=Keep(ScriptableObject.CreateInstance<LevelDefinition>());
            fixture.id=990;fixture.title="ACCELERATOR RUNWAY";fixture.hint="Native booster geometry review";fixture.world=0;
            fixture.spawn=new Vector3(0,24.62f,2);fixture.finish=new Vector3(0,24.7f,44);fixture.killHeight=10;
            fixture.tracks=new[]{
                new TrackPiece{position=new Vector3(0,23.6f,3),size=new Vector3(6,.8f,18.08f)},
                new TrackPiece{position=new Vector3(0,23.6f,21),size=new Vector3(6,.8f,18.08f)},
                new TrackPiece{position=new Vector3(0,23.6f,39),size=new Vector3(6,.8f,18.08f)}};
            fixture.routePoints=new[]{new Vector3(0,24,-6),new Vector3(0,24,12),new Vector3(0,24,30),new Vector3(0,24,48)};
            fixture.routeWidths=new[]{6f,6,6,6};
            fixture.obstacles=new[]{
                new ObstacleSpec{kind=ObstacleKind.Booster,position=new Vector3(0,24.13f,8),size=new Vector3(5,.25f,5),axis=Vector3.forward},
                new ObstacleSpec{kind=ObstacleKind.JumpPad,position=new Vector3(0,24.13f,25),size=new Vector3(5,.25f,5),axis=Vector3.forward,amplitude=3},
                new ObstacleSpec{kind=ObstacleKind.Conveyor,position=new Vector3(0,24.1f,39),size=new Vector3(5,.25f,5),axis=Vector3.forward,amplitude=1.1f}};
            app.Session.Loading();var root=Keep(new GameObject("Native accelerator runway fixture"));var builder=root.AddComponent<LevelBuilder>();
            builder.Build(app.catalog,fixture,app.Session,app.Save.Data);app.Session.Begin(fixture,builder.Ball);
            var ball=builder.Ball;ball.SetExternalControl(true);ball.SubmitControl(default);
            builder.FollowCamera.ResetOrbit();builder.FollowCamera.Snap();yield return new WaitForSeconds(.4f);
            Assert.IsTrue(ball.Grounded);BootstrapFlowTests.Capture("V10BoosterInRunway");
            var pads=root.GetComponentsInChildren<PremiumBoosterVisuals>();Assert.AreEqual(3,pads.Length);
            var camera=Camera.main;var brain=camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();brain.enabled=false;
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var enabled=new bool[canvases.Length];for(int i=0;i<canvases.Length;i++){enabled[i]=canvases[i].enabled;canvases[i].enabled=false;}
            string[] heroNames={"V10DriveBoosterClose","V10LaunchBoosterClose","V10ConveyorBoosterClose"};
            try
            {
                for(int i=0;i<pads.Length;i++)
                {
                    camera.transform.position=pads[i].transform.position+new Vector3(-3.3f,3.5f,-6.7f);
                    camera.transform.LookAt(pads[i].transform.position+Vector3.forward*.1f);camera.fieldOfView=52;
                    CaptureInspection(camera,heroNames[i]);
                }
            }
            finally{for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=enabled[i];brain.enabled=true;}
            builder.FollowCamera.Snap();yield return null;ball.SubmitControl(new BallControlCommand(1,0));
            float deadline=Time.time+4;while(!ball.Boosted&&Time.time<deadline)yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Boosted);Assert.Greater(pads[0].Activation,.5f);Assert.AreEqual(0,app.Session.Falls);
            BootstrapFlowTests.Capture("V10BoosterRealActivation");
            var library=root.GetComponent<PremiumCollectibleVisuals>();
            var record=new CaptureEvidence{meshes=library.SharedMeshCount,materials=library.SharedMaterialCount,actualBoostTriggered=ball.Boosted,activation=pads[0].Activation,speed=ball.Body.linearVelocity.magnitude,
                images=new[]{"TestResults/Previews/V9BoosterInRunway.png","TestResults/Previews/V9DriveBoosterClose.png","TestResults/Previews/V9LaunchBoosterClose.png","TestResults/Previews/V9ConveyorBoosterClose.png","TestResults/Previews/V9BoosterRealActivation.png"}};
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/booster-presentation-v10.json",JsonUtility.ToJson(record,true));
        }
        static void CaptureInspection(Camera camera,string name)
        {
            const int width=1280,height=960;
            var target=new RenderTexture(width,height,24);
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;target.antiAliasing=pipeline?Mathf.Max(1,pipeline.msaaSampleCount):1;
            var previous=RenderTexture.active;float aspect=camera.aspect;Texture2D texture=null;
            try
            {
                camera.aspect=width/(float)height;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;texture=new Texture2D(width,height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                Directory.CreateDirectory("TestResults/Previews");File.WriteAllBytes("TestResults/Previews/"+name+".png",texture.EncodeToPNG());
            }
            finally
            {
                camera.aspect=aspect;RenderTexture.active=previous;target.Release();UnityEngine.Object.Destroy(target);if(texture)UnityEngine.Object.Destroy(texture);
            }
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;foreach(var item in owned)if(item)UnityEngine.Object.Destroy(item);owned.Clear();yield return null;
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
