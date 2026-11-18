using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class CameraTurnaroundV6Tests
    {
        readonly List<Object> objects = new List<Object>();
        BallController ball;
        QuestCamera camera;
        CinemachineCamera virtualCamera;
        CinemachineBrain brain;
        Keyboard keyboard;
        Mouse mouse;
        Touchscreen touchscreen;
        bool oldBackground;
        InputSettings.BackgroundBehavior oldInputBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        T Keep<T>(T item) where T : Object { objects.Add(item); return item; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            oldBackground = Application.runInBackground; Application.runInBackground = true;
            oldInputBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            touchscreen = InputSystem.AddDevice<Touchscreen>(); touchscreen.MakeCurrent();
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0,-.5f,0); floor.transform.localScale = new Vector3(250,1,250);
            var sphere = Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere)); sphere.layer = 8;
            sphere.transform.position = new Vector3(0,.6f,0); sphere.AddComponent<Rigidbody>(); sphere.AddComponent<BallInput>();
            ball = sphere.AddComponent<BallController>();
            var profile = Keep(ScriptableObject.CreateInstance<BallProfile>()); profile.bounce = .01f;
            ball.ApplyProfile(profile); ball.active = true; ball.SetExternalControl(true);
            var output = Keep(new GameObject("Turnaround actual output",typeof(Camera)));
            brain = output.AddComponent<CinemachineBrain>(); brain.enabled = false;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            var rig = Keep(new GameObject("Turnaround actual virtual camera"));
            virtualCamera = rig.AddComponent<CinemachineCamera>(); virtualCamera.Follow = ball.transform; virtualCamera.LookAt = ball.transform;
            camera = rig.AddComponent<QuestCamera>(); camera.target = ball;
            camera.effectsEnabled = false; camera.collisionAvoidance = false;
            for (int i=0;i<15;i++) yield return new WaitForFixedUpdate();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            foreach (var device in new InputDevice[]{keyboard,mouse,touchscreen}) if (device != null && device.added) InputSystem.RemoveDevice(device);
            Application.runInBackground = oldBackground; InputSystem.settings.backgroundBehavior = oldInputBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
#endif
            foreach (var item in objects) if (item) Object.Destroy(item);
            objects.Clear(); yield return null;
        }
        CameraState Step(CameraState state,float dt)
        {
            var callback = typeof(QuestCamera).GetMethod("PostPipelineStageCallback",BindingFlags.Instance|BindingFlags.NonPublic);
            object[] arguments = {virtualCamera,CinemachineCore.Stage.Finalize,state,dt};
            callback.Invoke(camera,arguments); return (CameraState)arguments[2];
        }
        [Test]
        public void AdaptiveReverseCompletesFullTurnWithinTwoSecondsAndStopsAtRest()
        {
            Assert.AreEqual(BallCameraMode.AdaptiveChase,camera.mode);
            ball.Body.linearVelocity = Vector3.back * 8;
            var state = Step(CameraState.Default,0);
            float maximumHeadingStep = 0;
            for (int i=0;i<120;i++)
            {
                Vector3 before = camera.Forward;
                state = Step(state,1f/60);
                maximumHeadingStep = Mathf.Max(maximumHeadingStep,Vector3.Angle(before,camera.Forward));
                if (i == 0) Assert.Greater(Vector3.Angle(camera.Forward,Vector3.forward),.05f,"The v0.8 camera begins following deliberate reverse travel immediately.");
                Assert.IsFalse(float.IsNaN(state.RawOrientation.w));
            }
            Assert.Less(Vector3.Angle(camera.Forward,Vector3.back),.6f,"Reverse travel must complete the turnaround instead of stopping at the engage hysteresis.");
            Assert.That(maximumHeadingStep,Is.InRange(.5f,camera.adaptiveTurnSpeed/60 + .03f),"Heading must rotate with a bounded rate rather than snap.");
            Assert.AreEqual(.25f,state.Lens.NearClipPlane,.0001f); Assert.AreEqual(350,state.Lens.FarClipPlane,.001f);
            Vector3 restingHeading = camera.Forward; ball.Body.linearVelocity = Vector3.zero;
            for (int i=0;i<180;i++) state = Step(state,1f/60);
            Assert.Less(Vector3.Angle(restingHeading,camera.Forward),.001f);
            ball.Body.linearVelocity = Vector3.right * 8;
            for (int i=0;i<120;i++) state = Step(state,0);
            Assert.Less(Vector3.Angle(restingHeading,camera.Forward),.001f,"Paused camera updates must not advance a turnaround.");
        }
        [Test]
        public void ManualOrbitCanCompleteFullCirclesBothWaysAndHasTravelFollowGrace()
        {
            ball.Body.linearVelocity = Vector3.back * 8;
            var state = Step(CameraState.Default,0);
            camera.Orbit(new Vector2(.5f,0));
            for (int i=0;i<60;i++) state = Step(state,1f/60);
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.right),.999f,"Manual orbit owns heading during its grace period.");
            for (int i=0;i<3;i++) { camera.Orbit(new Vector2(.5f,0)); state = Step(state,1f/60); }
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.forward),.999f);
            for (int i=0;i<4;i++) { camera.Orbit(new Vector2(-.5f,0)); state = Step(state,1f/60); }
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.forward),.999f);
            Assert.AreEqual(Vector3.back * 8,ball.Body.linearVelocity,"Orbit cannot push or rotate the ball body.");
            for (int i=0;i<250;i++) state = Step(state,1f/60);
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.back),.999f,"Follow should resume after manual look grace.");
        }
        [Test]
        public void FollowFrameRemainsTangentAndFiniteThroughWallAndCeilingGravity()
        {
            var state = Step(CameraState.Default,0);
            foreach (var gravity in new[]{Vector3.forward * 9.81f,Vector3.up * 9.81f,Vector3.down * 9.81f})
            {
                ball.SetGravityDirection(gravity);
                ball.Body.linearVelocity = ball.ControlForward * -6;
                for (int i=0;i<120;i++) state = Step(state,1f/60);
                Assert.Less(Mathf.Abs(Vector3.Dot(camera.Forward,ball.MovementUp)),.0001f);
                Assert.Greater(Vector3.Dot(camera.Forward,ball.Body.linearVelocity.normalized),.999f);
                Assert.IsFalse(float.IsNaN(state.RawOrientation.w));
            }
        }
        void Hold(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); keyboard.MakeCurrent(); InputSystem.Update();
        }
        [UnityTest]
        public IEnumerator ActualBrainAndHeldReverseCompleteTurnWithoutCameraSteeringFeedback()
        {
            ball.SetExternalControl(false); ball.Input.active = true; brain.enabled = true;
            yield return null; yield return null;
            Hold(Key.S);
            yield return new WaitForSeconds(3.1f);
            Assert.Less(ball.Body.linearVelocity.z,-5,"Holding reverse must keep rolling in the original requested world direction throughout camera rotation.");
            Assert.Less(Mathf.Abs(ball.Body.position.x),.6f,"The orbit must not feed a spiral into a held movement gesture.");
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.back),.98f);
            Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(brain.transform.forward,Vector3.up).normalized,Vector3.back),.97f,"The real output camera must turn to face reverse travel.");
            Hold(); yield return null; yield return new WaitForFixedUpdate(); yield return null;
            Hold(Key.S);
            yield return new WaitForSeconds(3.6f);
            Assert.Greater(ball.Body.linearVelocity.z,5,"A fresh gesture adopts the new view and can roll back in the other direction.");
            Assert.Less(Mathf.Abs(ball.Body.position.x),.8f);
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.forward),.98f);
            Hold();
        }
    }
}
