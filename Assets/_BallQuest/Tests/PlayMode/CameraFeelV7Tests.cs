using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class CameraFeelV7Tests
    {
        readonly List<Object> objects=new List<Object>();
        BallController ball;QuestCamera camera;CinemachineCamera rig;Camera output;
        T Keep<T>(T item)where T:Object{objects.Add(item);return item;}
        [UnitySetUp]public IEnumerator Setup()
        {
            Time.timeScale=1;
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(180,1,180);
            var sphere=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));sphere.transform.position=new Vector3(0,.55f,0);sphere.AddComponent<Rigidbody>();sphere.AddComponent<BallInput>();ball=sphere.AddComponent<BallController>();
            ball.ApplyProfile(Keep(ScriptableObject.CreateInstance<BallProfile>()));ball.active=true;ball.SetExternalControl(true);
            rig=Keep(new GameObject("Camera feel rig")).AddComponent<CinemachineCamera>();camera=rig.gameObject.AddComponent<QuestCamera>();camera.target=ball;camera.collisionAvoidance=false;camera.effectsEnabled=false;
            output=Keep(new GameObject("Portrait camera projection")).AddComponent<Camera>();output.enabled=false;output.aspect=9f/16;
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
        }
        CameraState Step(CameraState state,float dt)
        {
            object[] args={rig,CinemachineCore.Stage.Finalize,state,dt};typeof(QuestCamera).GetMethod("PostPipelineStageCallback",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(camera,args);return(CameraState)args[2];
        }
        [Test]public void SmallLateralCorrectionsAreFollowedPromptlyAndRemainSmooth()
        {
            ball.Body.linearVelocity=Vector3.forward*9;var state=Step(CameraState.Default,0);
            float maximumStep=0;
            for(int frame=0;frame<240;frame++)
            {
                float side=frame/12%2==0?1:-1;
                ball.Body.linearVelocity=Quaternion.AngleAxis(side*32,Vector3.up)*Vector3.forward*9;
                Vector3 before=camera.Forward;
                state=Step(state,1f/60);
                float step=Vector3.Angle(before,camera.Forward);maximumStep=Mathf.Max(maximumStep,step);
                if(frame==0)Assert.Greater(step,.05f,"The v0.8 follow camera starts ordinary steering in the first rendered update.");
                Assert.Less(Vector3.Angle(camera.Forward,Vector3.forward),40,"Rapid alternating corrections must remain bounded instead of whipping through large arcs.");
            }
            Assert.Less(maximumStep,camera.adaptiveTurnSpeed/60+.04f);
            Vector3 steady=Quaternion.AngleAxis(18,Vector3.up)*Vector3.forward;ball.Body.linearVelocity=steady*9;
            for(int frame=0;frame<120;frame++)state=Step(state,1f/60);
            Assert.Less(Vector3.Angle(camera.Forward,steady),.2f,"Small sustained turns must finish instead of retaining a permanent off-axis view.");
        }
        [Test]public void PortraitFramingKeepsBallVisibleAcrossSpeedAndContactVelocityChanges()
        {
            camera.effectsEnabled=true;camera.mode=BallCameraMode.StableOrbit;var state=Step(CameraState.Default,0);
            float minimumY=1,maximumY=0;
            for(int frame=0;frame<240;frame++)
            {
                float speed=frame<60?0:frame<120?14:frame<180?7:0;
                ball.Body.linearVelocity=new Vector3(frame%2==0?.5f:-.5f,0,speed);
                state=Step(state,1f/60);output.transform.SetPositionAndRotation(state.RawPosition,state.RawOrientation);output.fieldOfView=state.Lens.FieldOfView;
                var point=output.WorldToViewportPoint(ball.transform.position);
                minimumY=Mathf.Min(minimumY,point.y);maximumY=Mathf.Max(maximumY,point.y);
                Assert.That(point.x,Is.InRange(.45f,.55f));Assert.That(point.y,Is.InRange(.18f,.48f));Assert.Greater(point.z,3);
                Assert.That(state.Lens.FieldOfView,Is.InRange(camera.baseFieldOfView,camera.baseFieldOfView+camera.speedFieldOfView+.01f));
            }
            Assert.Less(maximumY-minimumY,.06f,"Speed changes cannot pump the ball up and down the portrait frame.");
        }
        [Test]public void CameraRecoversFromObstructionWithoutSmoothingThroughSolidWall()
        {
            camera.collisionAvoidance=true;var state=Step(CameraState.Default,0);
            var wall=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));wall.transform.position=new Vector3(0,3,-5);wall.transform.localScale=new Vector3(8,6,.5f);Physics.SyncTransforms();
            for(int frame=0;frame<6;frame++)
            {
                state=Step(state,1f/60);
                Assert.Greater(state.RawPosition.z,-4.7f,"An obstructed camera must pull in immediately, even while its position spring still references the old point.");
            }
            wall.SetActive(false);Vector3 previous=state.RawPosition;
            for(int frame=0;frame<100;frame++)
            {
                state=Step(state,1f/60);Assert.Less(Vector3.Distance(previous,state.RawPosition),.5f);previous=state.RawPosition;
            }
            Assert.Less(state.RawPosition.z,-7.8f);
        }
        [UnityTearDown]public IEnumerator Cleanup(){foreach(var item in objects)if(item)Object.Destroy(item);objects.Clear();yield return null;}
    }
}
