using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace BallQuest.Tests
{
    public sealed class BallOnlyGesturesV9Tests
    {
        readonly List<Object> owned=new List<Object>();
        Touchscreen screen;Keyboard keyboard;Mouse mouse;
        int oldTarget,oldVsync;float oldCapture;
        bool oldBackground;InputSettings.BackgroundBehavior oldInputBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        T Keep<T>(T value)where T:Object{owned.Add(value);return value;}
        void Touch(int id,Vector2 position,TouchPhase phase)
        {screen.MakeCurrent();InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,position=position,phase=phase,pressure=phase==TouchPhase.Ended?0:1});InputSystem.Update();}
        [System.Serializable]sealed class Evidence
        {public int frames;public float maximumSideSpeed,maximumForwardSpeed,maximumCameraCommand,viewportError;public bool secondaryFingerDidNotOrbit;}
        [UnityTest]public IEnumerator CircularTouchAndSecondaryFingerDriveOnlyBallWhileCameraFollowsPhysicalTravel()
        {
            oldTarget=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;oldCapture=Time.captureDeltaTime;oldBackground=Application.runInBackground;oldInputBackground=InputSystem.settings.backgroundBehavior;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.captureDeltaTime=1f/60;Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();screen=InputSystem.AddDevice<Touchscreen>();screen.MakeCurrent();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(150,1,150);
            var sphere=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));sphere.transform.position=Vector3.up*.55f;sphere.layer=8;sphere.AddComponent<Rigidbody>();sphere.AddComponent<BallInput>();var ball=sphere.AddComponent<BallController>();ball.ApplyProfile(Keep(ScriptableObject.CreateInstance<BallProfile>()));ball.active=ball.Input.active=true;
            var output=Keep(new GameObject("Ball only output")).AddComponent<Camera>();output.aspect=9f/16;var brain=output.gameObject.AddComponent<CinemachineBrain>();brain.UpdateMethod=CinemachineBrain.UpdateMethods.ManualUpdate;
            var rig=Keep(new GameObject("Ball only rig")).AddComponent<CinemachineCamera>();rig.Follow=rig.LookAt=ball.transform;var camera=rig.gameObject.AddComponent<QuestCamera>();camera.target=ball;camera.effectsEnabled=false;camera.collisionAvoidance=false;
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
            Vector2 center=new Vector2(Screen.width*.4f,Screen.height*.4f);float extent=Mathf.Min(Screen.width,Screen.height),radius=extent*.1f;
            Touch(1,center+Vector2.right*radius,TouchPhase.Began);yield return null;
            var report=new Evidence();
            for(int i=1;i<=180;i++)
            {
                float a=i*Mathf.PI*2/90;Vector2 point=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                Touch(1,point,TouchPhase.Moved);
                if(i==30)Touch(2,center+Vector2.right*extent*.25f,TouchPhase.Began);
                if(i>30&&i<130)Touch(2,center+Vector2.right*extent*.25f+new Vector2(Mathf.Sin(i*.2f),Mathf.Cos(i*.2f))*extent*.15f,TouchPhase.Moved);
                if(i==130)Touch(2,center+Vector2.right*extent*.25f,TouchPhase.Ended);
                yield return null;brain.ManualUpdate();
                Assert.IsFalse(ball.Input.CameraGesture);float command=ball.Input.ConsumeCameraDelta().magnitude;Assert.AreEqual(0,command);
                report.maximumCameraCommand=Mathf.Max(report.maximumCameraCommand,command);
                report.maximumSideSpeed=Mathf.Max(report.maximumSideSpeed,Mathf.Abs(ball.Body.linearVelocity.x));report.maximumForwardSpeed=Mathf.Max(report.maximumForwardSpeed,Mathf.Abs(ball.Body.linearVelocity.z));
                Vector3 viewport=output.WorldToViewportPoint(ball.transform.position);report.viewportError=Mathf.Max(report.viewportError,Mathf.Abs(viewport.x-.5f));Assert.Less(Mathf.Abs(viewport.x-.5f),.02f);report.frames++;
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(report.maximumSideSpeed,.25f);Assert.Greater(report.maximumForwardSpeed,.25f);report.secondaryFingerDidNotOrbit=true;
            Touch(1,center+Vector2.right*radius,TouchPhase.Ended);yield return null;
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/ball-only-gestures-v10.json",JsonUtility.ToJson(report,true));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;foreach(var device in new InputDevice[]{screen,keyboard,mouse})if(device!=null&&device.added)InputSystem.RemoveDevice(device);
            Application.targetFrameRate=oldTarget;QualitySettings.vSyncCount=oldVsync;Time.captureDeltaTime=oldCapture;Application.runInBackground=oldBackground;InputSystem.settings.backgroundBehavior=oldInputBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
#endif
            foreach(var value in owned)if(value)Object.Destroy(value);owned.Clear();yield return null;
        }
    }
}
