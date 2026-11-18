using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BallQuest.Tests
{
    public sealed class ResponsiveMovementV6Tests
    {
        readonly List<Object> objects = new List<Object>();
        BallController ball;
        Touchscreen screen;
        Keyboard keyboard;
        Mouse mouse;
        [System.Serializable]
        sealed class Measurements
        {
            public float firstTouchStrokeSpeed, repeatedTouchStrokeSpeed, releaseSpeedRetentionAfterPointTwoSeconds;
            public float fastRepeatedSpeed, gentleSingleSpeed, fastWindowSeconds, gentleWindowSeconds;
            public float reverseCrossingSeconds, speedBeforeReverse;
        }
        static Measurements measurements;
        static StringBuilder trace;
        [OneTimeSetUp] public void StartReport()
        {
            measurements = new Measurements();
            trace = new StringBuilder("label,frame,time,unscaledDt,screenX,screenY,inputX,inputY,appliedX,appliedY,speedX,speedZ,pointer,serial\n");
        }
        [OneTimeTearDown] public void WriteReport()
        {
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/responsive-movement-v10.json",JsonUtility.ToJson(measurements,true));
            File.WriteAllText("TestResults/responsive-touch-v10.csv",trace.ToString());
        }
        void Trace(string label)
        {
            Vector2 point=screen.primaryTouch.position.ReadValue();
            trace.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3:F8},{4:F3},{5:F3},{6:F5},{7:F5},{8:F5},{9:F5},{10:F5},{11:F5},{12},{13}\n",label,Time.frameCount,Time.time,Time.unscaledDeltaTime,point.x,point.y,ball.Input.Move.x,ball.Input.Move.y,ball.AppliedControl.x,ball.AppliedControl.y,ball.Body.linearVelocity.x,ball.Body.linearVelocity.z,ball.Input.PointerHeld,ball.Input.MovementGestureSerial);
        }
        InputSettings.BackgroundBehavior background;
        bool runInBackground;
        int targetFrameRate, vSyncCount;
        float captureDeltaTime;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        T Keep<T>(T value) where T:Object { objects.Add(value); return value; }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            targetFrameRate=Application.targetFrameRate;vSyncCount=QualitySettings.vSyncCount;captureDeltaTime=Time.captureDeltaTime;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.captureDeltaTime=1f/60;
            runInBackground = Application.runInBackground; Application.runInBackground = true;
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            screen = InputSystem.AddDevice<Touchscreen>(); screen.MakeCurrent();
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0,-.5f,0); floor.transform.localScale = new Vector3(240,1,240);
            var sphere = Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere)); sphere.layer = 8;
            sphere.transform.position = new Vector3(0,.6f,0); sphere.AddComponent<Rigidbody>(); sphere.AddComponent<BallInput>();
            ball = sphere.AddComponent<BallController>(); var profile = Keep(ScriptableObject.CreateInstance<BallProfile>()); profile.bounce = .01f;
            ball.ApplyProfile(profile); ball.active = ball.Input.active = true;
            for(int i=0;i<15;i++) yield return new WaitForFixedUpdate();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach(var device in new InputDevice[]{screen,keyboard,mouse}) if(device!=null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.backgroundBehavior = background; Application.runInBackground = runInBackground;
            Application.targetFrameRate=targetFrameRate;QualitySettings.vSyncCount=vSyncCount;Time.captureDeltaTime=captureDeltaTime;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
            foreach(var item in objects) if(item) Object.Destroy(item); objects.Clear(); yield return null;
        }
        void Touch(int id,Vector2 position,InputTouchPhase phase)
        {
            screen.MakeCurrent();
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,position=position,phase=phase,pressure=phase==InputTouchPhase.Ended?0:1});
            InputSystem.Update();
        }
        [UnityTest]
        public IEnumerator ShortActualTouchStrokesAcceleratePromptlyAndLiftRetainsMomentum()
        {
            Vector2 start = new Vector2(Screen.width*.35f,Screen.height*.3f);
            float extent = Mathf.Min(Screen.width,Screen.height), firstSpeed = 0;
            for(int stroke=0;stroke<4;stroke++)
            {
                Touch(stroke+1,start,InputTouchPhase.Began); yield return null;
                for(int i=1;i<=7;i++)
                {
                    Touch(stroke+1,start+Vector2.up*(extent*.08f*i/7),InputTouchPhase.Moved);
                    Trace("short-event");yield return null;Trace("short-update"); yield return new WaitForFixedUpdate();Trace("short-fixed");
                }
                Touch(stroke+1,start+Vector2.up*(extent*.08f),InputTouchPhase.Ended);
                yield return null; yield return new WaitForFixedUpdate();
                if(stroke==0) { firstSpeed = measurements.firstTouchStrokeSpeed = ball.Body.linearVelocity.z; Assert.Greater(firstSpeed,.9f,"A short real touchscreen stroke should move the ball clearly."); }
                for(int i=0;i<3;i++) yield return new WaitForFixedUpdate();
            }
            float release = ball.Body.linearVelocity.z;
            measurements.repeatedTouchStrokeSpeed = release;
            Assert.Greater(release,firstSpeed+1); Assert.Greater(release,3);
            Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x),.1f);
            Vector3 position = ball.Body.position;
            for(int i=0;i<12;i++) yield return new WaitForFixedUpdate();
            measurements.releaseSpeedRetentionAfterPointTwoSeconds = ball.Body.linearVelocity.z/release;
            Assert.Greater(ball.Body.linearVelocity.z,release*.75f,"Lifting a finger should coast instead of feeling glued down.");
            Assert.Greater(ball.Body.position.z,position.z+.3f);
            for(int i=0;i<720;i++) yield return new WaitForFixedUpdate();
            Assert.Less(ball.Body.linearVelocity.magnitude,.12f,"Lower drag must still naturally settle to rest.");
        }
        [Test]
        public void FastSmallStrokesProduceMorePowerAndReversalDoesNotCrossOldAnchor()
        {
            var input = ball.Input; input.enabled = false; input.inputSmoothing = 0;
            input.touchMode = BallTouchMode.GestureVelocity;
            input.BeginPointer(Vector2.zero);
            input.SamplePointer(Vector2.up*40,.4f,1000,1800); float slow = input.ConsumeMove().y;
            input.BeginPointer(Vector2.zero);
            input.SamplePointer(Vector2.up*40,.05f,1000,1800); float fast = input.ConsumeMove().y;
            Assert.Greater(slow,.35f); Assert.Greater(fast,slow*1.1f,"Swipe speed has a modest gain; repetition supplies the main speed buildup.");
            input.SamplePointer(Vector2.up*70,.04f,1000,1800); Assert.Greater(input.Move.y,.35f);
            input.SamplePointer(Vector2.up*60,.016f,1000,1800);
            Assert.Less(input.Move.y,-.1f,"A reversing finger must steer backward immediately, even above its original start.");
            for(int i=0;i<30;i++) input.SamplePointer(Vector2.up*60,1f/60,1000,1800);
            Assert.Less(input.Move.magnitude,.002f,"A parked finger must still return to neutral.");
            input.EndPointer(); Assert.AreEqual(Vector2.zero,input.Move);
        }
        [UnityTest]
        public IEnumerator TouchGestureKeepsWorldDirectionWhileCameraTurnsAndNextGestureUsesNewView()
        {
            ball.Input.enabled = false; ball.SetControlFrame(Vector3.forward);
            var input = ball.Input; input.BeginPointer(Vector2.one*400);
            input.SamplePointer(Vector2.one*400-Vector2.up*5,Time.fixedDeltaTime,1000,1800);
            yield return new WaitForFixedUpdate();
            for(int i=1;i<=90;i++)
            {
                input.SamplePointer(Vector2.one*400-Vector2.up*(i*5),Time.fixedDeltaTime,1000,1800);
                ball.SetControlFrame(Quaternion.Euler(0,180f*i/90,0)*Vector3.forward);
                yield return new WaitForFixedUpdate();
                Assert.Greater(Vector3.Dot(ball.GestureForward,Vector3.forward),.99f,"The automatic camera must not curl a held reverse stroke.");
            }
            Assert.Less(ball.Body.linearVelocity.z,-.5f,"One capped stroke still coasts in its captured world direction."); Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x),.2f);
            input.EndPointer(); for(int i=0;i<3;i++) yield return new WaitForFixedUpdate();
            input.BeginPointer(Vector2.one*400);
            for(int i=1;i<=12;i++)
            {
                input.SamplePointer(Vector2.one*400+Vector2.up*(i*5),Time.fixedDeltaTime,1000,1800);
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(Vector3.Dot(ball.GestureForward,Vector3.back),.99f,"A new upward stroke should travel into the reversed camera view.");
            Assert.Less(ball.Body.linearVelocity.z,-.5f);
        }
        [UnityTest]
        public IEnumerator HeldKeyboardReverseCannotBeBentByAutomaticCameraRotation()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S)); InputSystem.Update(); yield return null;
            for(int i=0;i<90;i++)
            {
                ball.SetControlFrame(Quaternion.Euler(0,180f*i/89,0)*Vector3.forward);
                yield return null; yield return new WaitForFixedUpdate();
            }
            Assert.Less(ball.Body.linearVelocity.z,-6); Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x),.3f);
            Assert.Greater(Vector3.Dot(ball.GestureForward,Vector3.forward),.99f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); yield return null; yield return new WaitForFixedUpdate();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); InputSystem.Update(); yield return null;
            for(int i=0;i<10;i++) yield return new WaitForFixedUpdate();
            Assert.Greater(Vector3.Dot(ball.GestureForward,Vector3.back),.99f);
            Assert.Less(ball.Body.linearVelocity.z,-6);
        }
        [UnityTest]
        public IEnumerator CounterSwipeBrakesThroughZeroWithinAControlledTurnaroundWindow()
        {
            ball.SetExternalControl(true); ball.SubmitControl(new BallControlCommand(1,0));
            for(int i=0;i<65;i++) yield return new WaitForFixedUpdate();
            float before = ball.Body.linearVelocity.z; Assert.Greater(before,8);
            measurements.speedBeforeReverse = before; float reversedAt = Time.time;
            ball.SubmitControl(new BallControlCommand(-1,0)); yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.z,0,"Counter input must brake real momentum rather than flip velocity.");
            float deadline = Time.time+.7f;
            while(ball.Body.linearVelocity.z>=0 && Time.time<deadline) yield return new WaitForFixedUpdate();
            measurements.reverseCrossingSeconds = Time.time-reversedAt;
            Assert.Less(ball.Body.linearVelocity.z,0,"A responsive counter swipe should not require a long push against a heavy ball.");
            for(int i=0;i<40;i++) yield return new WaitForFixedUpdate();
            float speed = -ball.Body.linearVelocity.z;
            Assert.Greater(speed,6);
            float rolling = Vector3.ProjectOnPlane(ball.Body.angularVelocity,Vector3.up).magnitude*ball.GroundDetector.Radius;
            Assert.That(rolling/speed,Is.InRange(.8f,1.2f));
        }
        [UnityTest]
        public IEnumerator FasterRepeatedTouchSwipesBuildMoreRealSpeedThanOneGentleSwipeInTheSameWindow()
        {
            float[] speeds = new float[2];
            Vector2 start = new Vector2(Screen.width*.35f,Screen.height*.3f);
            float extent = Mathf.Min(Screen.width,Screen.height);
            for(int variant=0;variant<2;variant++)
            {
                ball.Respawn(new Vector3(0,.55f,0));
                for(int i=0;i<10;i++) yield return new WaitForFixedUpdate();
                float started = Time.time;
                int id = 20+variant*10;
                for(int frame=0;frame<48;frame++)
                {
                    if(variant==0)
                    {
                        int phase=frame%8;
                        if(phase==0)Touch(++id,start,InputTouchPhase.Began);
                        else if(phase<7)Touch(id,start+Vector2.up*(extent*.08f*phase/6),InputTouchPhase.Moved);
                        else Touch(id,start+Vector2.up*(extent*.08f),InputTouchPhase.Ended);
                    }
                    else if(frame==0)Touch(id,start,InputTouchPhase.Began);
                    else Touch(id,start+Vector2.up*(extent*.08f*frame/47),InputTouchPhase.Moved);
                    Trace("variant"+variant+"-event");yield return null;Trace("variant"+variant+"-update"); yield return new WaitForFixedUpdate();Trace("variant"+variant+"-fixed");
                }
                speeds[variant] = ball.Body.linearVelocity.z;
                if(variant==0) measurements.fastWindowSeconds=Time.time-started;
                else measurements.gentleWindowSeconds=Time.time-started;
                Touch(id,start+Vector2.up*(extent*.08f),InputTouchPhase.Ended); yield return null;
            }
            measurements.fastRepeatedSpeed=speeds[0]; measurements.gentleSingleSpeed=speeds[1];
            Assert.Greater(speeds[1],.2f,"Gentle strokes must remain usable for precise balancing.");
            Assert.Greater(speeds[0],speeds[1]*1.8f,"Repeated faster swipes must build clearly greater actual rigidbody speed.");
            Assert.Greater(speeds[0],4);
        }
        [UnityTest]
        public IEnumerator ReplacementTouchReanchorsWithoutAFrameGapAndResumedStrokeUsesCurrentView()
        {
            Vector2 start = new Vector2(Screen.width*.35f,Screen.height*.3f);
            float extent = Mathf.Min(Screen.width,Screen.height);
            Touch(71,start,InputTouchPhase.Began); yield return null;
            Touch(71,start+Vector2.up*(extent*.04f),InputTouchPhase.Moved); yield return null; yield return new WaitForFixedUpdate();
            Touch(71,start+Vector2.up*(extent*.04f),InputTouchPhase.Ended);
            Touch(72,start,InputTouchPhase.Began); yield return null;
            Assert.IsTrue(ball.Input.PointerHeld,"A replacement finger must not require lifting every finger for a whole frame.");
            Assert.Less(ball.Input.Move.magnitude,.001f,"A new touch cannot inherit the previous screen displacement.");
            for(int i=1;i<=10;i++)
            {
                Touch(72,start+Vector2.up*(extent*.04f*i/10),InputTouchPhase.Moved);
                yield return null; yield return new WaitForFixedUpdate();
            }
            uint serial = ball.Input.MovementGestureSerial;
            for(int i=0;i<20;i++) { yield return null; yield return new WaitForFixedUpdate(); }
            Assert.Less(ball.Input.Move.magnitude,.002f); ball.SetControlFrame(Vector3.back);
            Touch(72,start+Vector2.up*(extent*.05f),InputTouchPhase.Moved); yield return null; yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Input.MovementGestureSerial,serial);
            Assert.Greater(Vector3.Dot(ball.GestureForward,Vector3.back),.99f,"Resuming after a parked finger should use the view now on screen.");
            Touch(72,start+Vector2.up*(extent*.05f),InputTouchPhase.Ended); yield return null;
        }
        [Test]
        public void SparseTouchSamplesRetainPowerAcrossDifferentRenderRates()
        {
            var input=ball.Input;input.enabled=false;input.touchMode=BallTouchMode.GestureVelocity;
            float baseline=0;
            foreach(int renderRate in new[]{60,30,120,600})
            {
                input.BeginPointer(Vector2.zero);float integrated=0;
                for(int frame=1;frame<=renderRate;frame++)
                {
                    float time=frame/(float)renderRate;
                    // The hardware reports60Hz; slower renders receive the newest combined sample.
                    float sampleTime=Mathf.Floor(time*60+.0001f)/60;
                    input.SamplePointer(Vector2.up*(sampleTime*180),1f/renderRate,1000,1800);
                    if(time>.2f)integrated+=input.ConsumeMove().y*Time.fixedDeltaTime;
                    else input.ConsumeMove();
                }
                if(renderRate==60)baseline=integrated;
                else Assert.Less(Mathf.Abs(integrated-baseline),.025f,"Sparse touch displacement was lost at render rate"+renderRate);
                Assert.That(integrated,Is.InRange(.045f,.091f),"One continued stroke retains its finite reduced drive across render rates.");
                input.EndPointer();
            }
        }
    }
}
