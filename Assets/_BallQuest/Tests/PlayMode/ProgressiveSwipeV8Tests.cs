using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace BallQuest.Tests
{
    public sealed class ProgressiveSwipeV8Tests
    {
        readonly List<Object> owned=new List<Object>();
        BallController ball;
        Touchscreen touchscreen;
        Keyboard keyboard;
        Mouse mouse;
        int oldFrameRate,oldVsync;
        float oldCapture;
        bool oldBackground;
        InputSettings.BackgroundBehavior oldInputBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        [System.Serializable]sealed class Measurement
        {
            public string environment="Unity Editor physical Rigidbody and Input System touchscreen; not a phone latency measurement";
            public float firstStrokeSpeed,fourthStrokeSpeed,eighthStrokeSpeed,twelfthStrokeSpeed;
            public float maximumSingleLargeStrokeSpeed,maximumParkedDrive;
            public float minimumCadenceBudget,maximumCadenceBudget;
        }
        static Measurement evidence;
        [OneTimeSetUp]public void StartReport()=>evidence=new Measurement();
        [OneTimeTearDown]public void SaveReport()
        {Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/progressive-swipes-v10.json",JsonUtility.ToJson(evidence,true));}
        T Keep<T>(T value)where T:Object{owned.Add(value);return value;}
        [UnitySetUp]public IEnumerator Setup()
        {
            Time.timeScale=1;oldFrameRate=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;oldCapture=Time.captureDeltaTime;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.captureDeltaTime=1f/60;
            oldBackground=Application.runInBackground;Application.runInBackground=true;
            oldInputBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();touchscreen=InputSystem.AddDevice<Touchscreen>();touchscreen.MakeCurrent();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(300,1,300);
            var sphere=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));sphere.transform.position=new Vector3(0,.55f,0);sphere.layer=8;
            sphere.AddComponent<Rigidbody>();sphere.AddComponent<BallInput>();ball=sphere.AddComponent<BallController>();
            var profile=Keep(ScriptableObject.CreateInstance<BallProfile>());profile.bounce=.01f;ball.ApplyProfile(profile);ball.active=ball.Input.active=true;
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
        }
        void Touch(int id,Vector2 point,TouchPhase phase)
        {touchscreen.MakeCurrent();InputSystem.QueueStateEvent(touchscreen,new TouchState{touchId=id,position=point,phase=phase,pressure=phase==TouchPhase.Ended?0:1});InputSystem.Update();}

        [UnityTest]public IEnumerator RepeatedFastTouchStrokesBuildSpeedGraduallyInsteadOfLaunchingOnTheFirst()
        {
            Vector2 start=new Vector2(Screen.width*.35f,Screen.height*.3f);
            float extent=Mathf.Min(Screen.width,Screen.height),previous=0;
            for(int stroke=1;stroke<=12;stroke++)
            {
                Touch(stroke,start,TouchPhase.Began);yield return null;
                for(int point=1;point<=4;point++)
                {
                    Touch(stroke,start+Vector2.up*(extent*.1f*point/4),TouchPhase.Moved);
                    yield return null;yield return new WaitForFixedUpdate();
                }
                Touch(stroke,start+Vector2.up*(extent*.1f),TouchPhase.Ended);yield return null;yield return new WaitForFixedUpdate();
                float speed=ball.Body.linearVelocity.z;
                if(stroke<=8)Assert.Greater(speed,previous+.3f,"Repetition must add momentum gradually, without needing a held finger.");
                if(stroke==1){evidence.firstStrokeSpeed=speed;Assert.That(speed,Is.InRange(1.2f,2.3f));}
                if(stroke==4){evidence.fourthStrokeSpeed=speed;Assert.That(speed,Is.InRange(4.5f,7.5f));}
                if(stroke==8){evidence.eighthStrokeSpeed=speed;Assert.That(speed,Is.InRange(8.5f,12.5f));}
                if(stroke==12){evidence.twelfthStrokeSpeed=speed;Assert.That(speed,Is.InRange(10.5f,14.3f));}
                Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x),.05f);previous=speed;
            }
        }

        [UnityTest]public IEnumerator OneOversizedStrokeHasTheSameModestLimitAtEveryInputCadence()
        {
            ball.Input.enabled=false;
            foreach(int samples in new[]{1,6,30})
            {
                ball.Respawn(new Vector3(0,.55f,0));for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
                ball.Input.BeginPointer(Vector2.zero);
                float peak=0;
                for(int point=1;point<=samples;point++)
                {
                    ball.Input.SamplePointer(Vector2.up*(600f*point/samples),.12f/samples,1000,1800);
                    yield return new WaitForFixedUpdate();peak=Mathf.Max(peak,ball.Body.linearVelocity.z);
                }
                evidence.maximumSingleLargeStrokeSpeed=Mathf.Max(evidence.maximumSingleLargeStrokeSpeed,peak);
                Assert.That(peak,Is.InRange(1.5f,2.7f),"A large swipe must not approach the 14m/s powered ceiling by itself.");
                for(int step=0;step<30;step++)
                {
                    ball.Input.SamplePointer(Vector2.up*600,Time.fixedDeltaTime,1000,1800);yield return new WaitForFixedUpdate();
                    evidence.maximumParkedDrive=Mathf.Max(evidence.maximumParkedDrive,ball.AppliedControl.magnitude);Assert.AreEqual(Vector2.zero,ball.AppliedControl);
                }
            }
        }

        [Test]public void WholeStrokeBudgetIsStableAcrossCadencesAndCannotBeReplayed()
        {
            var input=ball.Input;input.enabled=false;float minimum=float.MaxValue,maximum=0;
            foreach(int rate in new[]{30,60,120,600})
            {
                input.BeginPointer(Vector2.zero);float supplied=0;
                for(int frame=1;frame<=rate/2;frame++)
                {
                    float sampledTime=Mathf.Floor(frame/(float)rate*60+.0001f)/60;
                    input.SamplePointer(Vector2.up*(sampledTime*1200),1f/rate,1000,1800);
                    supplied+=input.ConsumeMove().y*Time.fixedDeltaTime;
                    Assert.AreEqual(Vector2.zero,input.ConsumeMove());
                }
                Assert.That(supplied,Is.InRange(.089f,.091f));minimum=Mathf.Min(minimum,supplied);maximum=Mathf.Max(maximum,supplied);
            }
            evidence.minimumCadenceBudget=minimum;evidence.maximumCadenceBudget=maximum;Assert.Less(maximum-minimum,.001f);
        }

        [Test]public void ADeliberateTurnGetsFreshDriveWithoutChangingTheCapturedCameraFrame()
        {
            var input=ball.Input;input.enabled=false;input.BeginPointer(Vector2.zero);
            input.SamplePointer(Vector2.up*400,.1f,1000,1800);Assert.Greater(input.ConsumeMove().y,0);
            input.SamplePointer(Vector2.up*410,.02f,1000,1800);Assert.AreEqual(Vector2.zero,input.ConsumeMove(),"Continuing the exhausted stroke cannot add power.");
            uint serial=input.MovementGestureSerial;
            input.SamplePointer(new Vector2(30,410),.04f,1000,1800);Assert.Greater(input.ConsumeMove().x,.3f);
            Assert.AreEqual(serial,input.MovementGestureSerial,"Directional budget changes must not relatch an orbiting camera basis.");
            input.SamplePointer(new Vector2(20,410),.02f,1000,1800);Assert.Less(input.ConsumeMove().x,-.1f,"A deliberate counter swipe must brake without crossing the old pointer anchor.");
        }

        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;foreach(var device in new InputDevice[]{touchscreen,keyboard,mouse})if(device!=null&&device.added)InputSystem.RemoveDevice(device);
            Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVsync;Time.captureDeltaTime=oldCapture;Application.runInBackground=oldBackground;InputSystem.settings.backgroundBehavior=oldInputBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
#endif
            foreach(var value in owned)if(value)Object.Destroy(value);owned.Clear();yield return null;
        }
    }
}
