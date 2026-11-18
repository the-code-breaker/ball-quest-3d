using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class CenteredCameraV8Tests
    {
        readonly List<UnityEngine.Object> objects=new List<UnityEngine.Object>();
        BallController ball;QuestCamera follow;CinemachineCamera rig;CinemachineBrain brain;Camera output;
        int oldTarget,oldVsync;float oldCapture;
        [Serializable]sealed class Evidence
        {
            public string environment="Unity Editor actual CinemachineBrain output and physical force commands; no Android latency claim";
            public int frames;
            public float maximumHorizontalViewportError,maximumVerticalViewportError,maximumHeadingStepDegrees,firstSmallTurnStepDegrees,settledHeadingErrorDegrees;
            public bool physicalJumpObserved;
        }
        static Evidence evidence;
        [OneTimeSetUp]public void BeginEvidence()=>evidence=new Evidence();
        [OneTimeTearDown]public void SaveEvidence()
        {Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/centered-camera-v10.json",JsonUtility.ToJson(evidence,true));}
        T Keep<T>(T item)where T:UnityEngine.Object{objects.Add(item);return item;}
        [UnitySetUp]public IEnumerator Setup()
        {
            Time.timeScale=1;oldTarget=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;oldCapture=Time.captureDeltaTime;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.captureDeltaTime=1f/60;
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(300,1,300);
            var sphere=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));sphere.layer=8;sphere.transform.position=new Vector3(0,.55f,0);
            sphere.AddComponent<Rigidbody>();sphere.AddComponent<BallInput>();ball=sphere.AddComponent<BallController>();
            var profile=Keep(ScriptableObject.CreateInstance<BallProfile>());profile.bounce=.01f;ball.ApplyProfile(profile);ball.active=true;ball.SetExternalControl(true);
            output=Keep(new GameObject("Centered portrait actual output")).AddComponent<Camera>();output.aspect=9f/16;
            brain=output.gameObject.AddComponent<CinemachineBrain>();brain.UpdateMethod=CinemachineBrain.UpdateMethods.ManualUpdate;
            var cameraGo=Keep(new GameObject("Centered portrait actual virtual camera"));rig=cameraGo.AddComponent<CinemachineCamera>();rig.Follow=ball.transform;rig.LookAt=ball.transform;
            follow=cameraGo.AddComponent<QuestCamera>();follow.target=ball;follow.collisionAvoidance=false;follow.effectsEnabled=false;
            for(int frame=0;frame<12;frame++){yield return null;brain.ManualUpdate(Time.frameCount,1f/60);}
        }
        CameraState Step(CameraState state,float dt)
        {
            object[] args={rig,CinemachineCore.Stage.Finalize,state,dt};typeof(QuestCamera).GetMethod("PostPipelineStageCallback",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,args);return(CameraState)args[2];
        }
        void CheckActualFrame(Vector3 previousHeading)
        {
            brain.ManualUpdate(Time.frameCount,1f/60);
            Vector3 viewport=output.WorldToViewportPoint(ball.transform.position);
            float horizontal=Mathf.Abs(viewport.x-.5f),vertical=Mathf.Abs(viewport.y-follow.ballViewportHeight);
            evidence.frames++;evidence.maximumHorizontalViewportError=Mathf.Max(evidence.maximumHorizontalViewportError,horizontal);
            evidence.maximumVerticalViewportError=Mathf.Max(evidence.maximumVerticalViewportError,vertical);
            evidence.maximumHeadingStepDegrees=Mathf.Max(evidence.maximumHeadingStepDegrees,Vector3.Angle(previousHeading,follow.Forward));
            Assert.Less(horizontal,.008f,"Actual output must keep the ball horizontally centred during translation and turning.");
            Assert.Less(vertical,.012f,"Ball composition must not wander as the camera turns or the ball jumps.");
            Assert.Greater(viewport.z,3,"The follow camera must retain useful distance from the ball.");
            Assert.Less(Vector3.Angle(previousHeading,follow.Forward),follow.adaptiveTurnSpeed/60+.08f,"Heading changes must remain smooth and bounded.");
        }
        [Test]public void SmallMeaningfulTurnsBeginImmediatelyAndFinishStraightBehindTheBall()
        {
            ball.Body.linearVelocity=Vector3.forward*7;var state=Step(CameraState.Default,0);
            Vector3 target=Quaternion.AngleAxis(9,Vector3.up)*Vector3.forward;
            ball.Body.linearVelocity=target*7;Vector3 before=follow.Forward;state=Step(state,1f/60);
            evidence.firstSmallTurnStepDegrees=Vector3.Angle(before,follow.Forward);
            Assert.Greater(evidence.firstSmallTurnStepDegrees,.05f,"No angular dead zone or timer may defer an intentional turn.");
            for(int frame=0;frame<120;frame++)state=Step(state,1f/60);
            Assert.Less(Vector3.Angle(follow.Forward,target),.15f);
            Vector3 stopped=follow.Forward;ball.Body.linearVelocity=Vector3.zero;
            for(int frame=0;frame<180;frame++)state=Step(state,1f/60);
            Assert.Less(Vector3.Angle(stopped,follow.Forward),.001f,"Stopping holds the last useful viewing direction.");
        }
        [UnityTest]public IEnumerator ActualBrainKeepsBallCentredThroughCardinalTurnsCurvesSpeedChangesAndJump()
        {
            foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
            {
                ball.SetPathForward(direction);ball.SubmitControl(new BallControlCommand(1,0));
                for(int frame=0;frame<120;frame++)
                {
                    Vector3 previous=follow.Forward;yield return null;CheckActualFrame(previous);
                }
                float error=Vector3.Angle(follow.Forward,Vector3.ProjectOnPlane(ball.Body.linearVelocity,Vector3.up));
                evidence.settledHeadingErrorDegrees=Mathf.Max(evidence.settledHeadingErrorDegrees,error);
                Assert.Less(error,2,"The output must settle directly behind the actual travel direction on all four headings.");
            }
            for(int frame=0;frame<180;frame++)
            {
                Vector3 direction=Quaternion.AngleAxis(-90+frame*.65f,Vector3.up)*Vector3.forward;
                ball.SetPathForward(direction);ball.SubmitControl(new BallControlCommand(frame<90?.5f:1,0));
                Vector3 previous=follow.Forward;yield return null;CheckActualFrame(previous);
            }
            ball.allowJump=true;ball.SubmitControl(new BallControlCommand(0,0,0,true));
            for(int frame=0;frame<90;frame++)
            {
                Vector3 previous=follow.Forward;yield return null;CheckActualFrame(previous);
                evidence.physicalJumpObserved|=!ball.Grounded&&ball.Body.position.y>1;
            }
            Assert.IsTrue(evidence.physicalJumpObserved,"The framing sequence must include an actual force-driven airborne interval.");
        }
        [UnityTest]public IEnumerator StableManualModeAlsoKeepsTranslationCentredWithoutChangingItsChosenHeading()
        {
            follow.mode=BallCameraMode.StableOrbit;follow.Orbit(new Vector2(.25f,0));
            Vector3 chosen=follow.Forward;ball.SetPathForward(Vector3.right);ball.SubmitControl(new BallControlCommand(1,0));
            for(int frame=0;frame<120;frame++)
            {Vector3 previous=follow.Forward;yield return null;CheckActualFrame(previous);Assert.Less(Vector3.Angle(chosen,follow.Forward),.001f);}
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;Application.targetFrameRate=oldTarget;QualitySettings.vSyncCount=oldVsync;Time.captureDeltaTime=oldCapture;
            foreach(var item in objects)if(item)UnityEngine.Object.Destroy(item);objects.Clear();yield return null;
        }
    }
}
