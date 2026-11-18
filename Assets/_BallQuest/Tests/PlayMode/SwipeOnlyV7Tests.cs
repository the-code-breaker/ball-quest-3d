using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BallQuest.Tests
{
    public sealed class SwipeOnlyV7Tests
    {
        readonly List<Object> objects=new List<Object>();
        BallController ball;
        Touchscreen touchscreen;
        Keyboard keyboard;
        Mouse mouse;
        int previousTarget,previousVsync;
        float previousCapture;
        bool previousBackground;
        InputSettings.BackgroundBehavior previousInputBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
        [System.Serializable]sealed class Evidence
        {
            public float stationaryMaximumSpeed,jitterMaximumSpeed,parkedMaximumDrive;
            public float postSwipeInitialSpeed,postSwipeFinalSpeed,slowSameDistanceSpeed,fastSameDistanceSpeed,singleSampleSameDistanceSpeed;
            public bool noCommandReplay;
        }
        static Evidence evidence;
        [OneTimeSetUp]public void ReportStart()=>evidence=new Evidence();
        [OneTimeTearDown]public void ReportEnd(){Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/swipe-only-v10.json",JsonUtility.ToJson(evidence,true));}
        T Keep<T>(T item)where T:Object{objects.Add(item);return item;}
        [UnitySetUp]public IEnumerator Setup()
        {
            Time.timeScale=1;previousTarget=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;previousCapture=Time.captureDeltaTime;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.captureDeltaTime=1f/60;
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            previousInputBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();touchscreen=InputSystem.AddDevice<Touchscreen>();touchscreen.MakeCurrent();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(220,1,220);
            var sphere=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));sphere.transform.position=new Vector3(0,.55f,0);sphere.layer=8;sphere.AddComponent<Rigidbody>();sphere.AddComponent<BallInput>();
            ball=sphere.AddComponent<BallController>();var profile=Keep(ScriptableObject.CreateInstance<BallProfile>());profile.bounce=.01f;ball.ApplyProfile(profile);ball.active=ball.Input.active=true;
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
        }
        void Touch(int id,Vector2 point,TouchPhase phase)
        {
            touchscreen.MakeCurrent();InputSystem.QueueStateEvent(touchscreen,new TouchState{touchId=id,position=point,phase=phase,pressure=phase==TouchPhase.Ended?0:1});InputSystem.Update();
        }
        Vector2 Start=>new Vector2(Screen.width*.35f,Screen.height*.3f);
        float Extent=>Mathf.Min(Screen.width,Screen.height);
        [UnityTest]public IEnumerator StationaryFingerCannotAccelerateInEitherSavedTouchMode()
        {
            foreach(var mode in new[]{BallTouchMode.RelativeDrag,BallTouchMode.GestureVelocity})
            {
                ball.Input.touchMode=mode;ball.Respawn(new Vector3(0,.55f,0));
                Touch(1,Start,TouchPhase.Began);yield return null;
                for(int i=0;i<300;i++)
                {
                    Touch(1,Start,TouchPhase.Stationary);yield return new WaitForFixedUpdate();yield return null;
                    evidence.stationaryMaximumSpeed=Mathf.Max(evidence.stationaryMaximumSpeed,Vector3.ProjectOnPlane(ball.Body.linearVelocity,Vector3.up).magnitude);
                    Assert.AreEqual(Vector2.zero,ball.AppliedControl,"A held finger with no displacement must issue no physical drive.");
                }
                Assert.Less(evidence.stationaryMaximumSpeed,.03f);Assert.Less(Vector3.ProjectOnPlane(ball.Body.position,Vector3.up).magnitude,.02f);
                Touch(1,Start,TouchPhase.Ended);yield return null;
            }
        }
        [UnityTest]public IEnumerator SubPixelStationaryJitterCannotAccumulateMomentum()
        {
            Touch(2,Start,TouchPhase.Began);yield return null;
            for(int i=0;i<300;i++)
            {
                Vector2 noise=new Vector2(Mathf.Sin(i*1.7f),Mathf.Cos(i*2.3f))*(Extent*.00045f);
                Touch(2,Start+noise,TouchPhase.Moved);yield return new WaitForFixedUpdate();yield return null;
                evidence.jitterMaximumSpeed=Mathf.Max(evidence.jitterMaximumSpeed,Vector3.ProjectOnPlane(ball.Body.linearVelocity,Vector3.up).magnitude);
                Assert.AreEqual(Vector2.zero,ball.AppliedControl);
            }
            Assert.Less(evidence.jitterMaximumSpeed,.03f);Touch(2,Start,TouchPhase.Ended);yield return null;
        }
        [UnityTest]public IEnumerator ParkingAfterSwipeHasNoForceTailAndOnlyLosesMomentum()
        {
            foreach(var mode in new[]{BallTouchMode.RelativeDrag,BallTouchMode.GestureVelocity})
            {
                ball.Respawn(new Vector3(0,.55f,0));ball.Input.touchMode=mode;Touch(3,Start,TouchPhase.Began);yield return null;
                for(int i=1;i<=9;i++){Touch(3,Start+Vector2.up*(Extent*.12f*i/9),TouchPhase.Moved);yield return null;yield return new WaitForFixedUpdate();}
                Vector2 parked=Start+Vector2.up*(Extent*.12f);Touch(3,parked,TouchPhase.Stationary);yield return null;
                yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                float previous=ball.Body.linearVelocity.z;evidence.postSwipeInitialSpeed=previous;Assert.Greater(previous,1);
                for(int i=0;i<300;i++)
                {
                    Touch(3,parked,TouchPhase.Stationary);yield return null;yield return new WaitForFixedUpdate();
                    evidence.parkedMaximumDrive=Mathf.Max(evidence.parkedMaximumDrive,ball.AppliedControl.magnitude);
                    Assert.AreEqual(Vector2.zero,ball.AppliedControl,"Touch velocity and controller smoothing must not replay prior drive.");
                    float current=ball.Body.linearVelocity.z;Assert.LessOrEqual(current,previous+.025f);previous=current;
                }
                evidence.postSwipeFinalSpeed=previous;Assert.Less(previous,evidence.postSwipeInitialSpeed*.3f);
                Touch(3,parked,TouchPhase.Ended);yield return null;
            }
        }
        [Test]public void EachSwipeBudgetCanBeConsumedOnlyOnceAcrossPhysicsCatchUpSteps()
        {
            var input=ball.Input;input.enabled=false;input.BeginPointer(Vector2.zero);
            input.SamplePointer(Vector2.up*80,.12f,1000,1800);Assert.Greater(input.ConsumeMove().y,.1f);
            for(int i=0;i<12;i++)Assert.AreEqual(Vector2.zero,input.ConsumeMove(),"Extra physics ticks cannot reuse the last render command.");
            input.SamplePointer(Vector2.up*80,1f/30,1000,1800);Assert.AreEqual(Vector2.zero,input.Move);Assert.AreEqual(Vector2.zero,input.ConsumeMove());
            evidence.noCommandReplay=true;
        }
        [UnityTest]public IEnumerator PauseAndResumeWithFingerStillHeldCannotReplayTheLastSwipe()
        {
            Touch(4,Start,TouchPhase.Began);yield return null;
            Touch(4,Start+Vector2.up*(Extent*.08f),TouchPhase.Moved);yield return null;
            Time.timeScale=0;
            for(int i=0;i<4;i++)yield return null;
            Assert.AreEqual(Vector2.zero,ball.Input.PendingDrive);
            Time.timeScale=1;
            for(int i=0;i<8;i++)
            {
                Touch(4,Start+Vector2.up*(Extent*.08f),TouchPhase.Stationary);yield return null;yield return new WaitForFixedUpdate();
                Assert.AreEqual(Vector2.zero,ball.AppliedControl,"Resuming with a parked finger cannot recreate an old swipe.");
            }
            Touch(4,Start+Vector2.up*(Extent*.08f),TouchPhase.Ended);yield return null;
        }
        [UnityTest]public IEnumerator SameTravelDistanceSwipedFasterProducesMorePhysicalSpeed()
        {
            ball.Input.enabled=false;var input=ball.Input;
            float[] speeds=new float[3];int[] steps={30,6,1};
            for(int variant=0;variant<3;variant++)
            {
                ball.Respawn(new Vector3(0,.55f,0));for(int n=0;n<10;n++)yield return new WaitForFixedUpdate();input.BeginPointer(Vector2.zero);
                for(int n=1;n<=steps[variant];n++)
                {
                    input.SamplePointer(Vector2.up*(100f*n/steps[variant]),Time.fixedDeltaTime,1000,1800);yield return new WaitForFixedUpdate();
                }
                input.EndPointer();yield return new WaitForFixedUpdate();speeds[variant]=ball.Body.linearVelocity.z;
            }
            evidence.slowSameDistanceSpeed=speeds[0];evidence.fastSameDistanceSpeed=speeds[1];evidence.singleSampleSameDistanceSpeed=speeds[2];
            Assert.Greater(speeds[0],.5f);Assert.Greater(speeds[1],speeds[0]*1.25f,"Fast and slow strokes cover identical screen distance, while speed changes their finite drive budget.");
            Assert.GreaterOrEqual(speeds[2],speeds[1],"The shortest complete sampled swipe must not lose energy to a per-frame request clamp.");
            Assert.Less(speeds[2],ball.profile.maxSpeed+.3f);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;foreach(var device in new InputDevice[]{touchscreen,keyboard,mouse})if(device!=null&&device.added)InputSystem.RemoveDevice(device);
            Application.targetFrameRate=previousTarget;QualitySettings.vSyncCount=previousVsync;Time.captureDeltaTime=previousCapture;Application.runInBackground=previousBackground;InputSystem.settings.backgroundBehavior=previousInputBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;
#endif
            foreach(var item in objects)if(item)Object.Destroy(item);objects.Clear();yield return null;
        }
    }
}
