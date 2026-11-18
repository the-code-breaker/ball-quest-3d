using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BallQuest.Tests
{
    public sealed class ManualPointerSafetyTests
    {
        readonly List<Object> objects = new List<Object>();
        BallController ball;
        Touchscreen touchscreen;
        Mouse mouse;
        Keyboard keyboard;
        InputSettings.BackgroundBehavior oldBackground;
        bool oldRunInBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        T Keep<T>(T value) where T:Object { objects.Add(value); return value; }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            oldRunInBackground = Application.runInBackground; Application.runInBackground = true;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            touchscreen = InputSystem.AddDevice<Touchscreen>(); touchscreen.MakeCurrent();
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.position = new Vector3(0,-.5f,0); floor.transform.localScale = new Vector3(100,1,100);
            var go = Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere)); go.transform.position = new Vector3(0,.6f,0); go.AddComponent<Rigidbody>(); go.AddComponent<BallInput>();
            ball = go.AddComponent<BallController>(); var profile = Keep(ScriptableObject.CreateInstance<BallProfile>()); profile.bounce = .01f;
            ball.ApplyProfile(profile); ball.active = true; ball.Input.active = true;
            // This fixture covers drag ownership/reanchoring. Default swipe response has a separate physics fixture.
            ball.Input.touchMode = BallTouchMode.RelativeDrag;
            if (!EventSystem.current) Keep(new GameObject("Pointer safety events")).AddComponent<EventSystem>();
            for(int i=0;i<15;i++) yield return new WaitForFixedUpdate();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            foreach (var device in new InputDevice[]{touchscreen,mouse,keyboard}) if(device!=null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.backgroundBehavior = oldBackground; Application.runInBackground = oldRunInBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
#endif
            foreach(var item in objects)if(item)Object.Destroy(item); objects.Clear(); yield return null;
        }
        void Touch(int id,Vector2 position,InputTouchPhase phase)
        {
            InputSystem.QueueStateEvent(touchscreen,new TouchState{touchId=id,position=position,phase=phase,pressure=phase==InputTouchPhase.Ended?0:1});
        }
        void Flush() { touchscreen.MakeCurrent(); InputSystem.Update(); }
        Vector2 Point(float x,float y) => new Vector2(Screen.width*x,Screen.height*y);
        RectTransform MakeBlockingUI(Vector2 centre)
        {
            var canvas = Keep(new GameObject("Actual raycast fixture",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)));
            var component=canvas.GetComponent<Canvas>(); component.renderMode=RenderMode.ScreenSpaceOverlay;component.sortingOrder=30000;
            var panel = new GameObject("UI-owned touch",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one*.5f;
            rect.anchoredPosition=centre-Point(.5f,.5f);rect.sizeDelta=Vector2.one*Mathf.Min(Screen.width,Screen.height)*.18f;
            panel.GetComponent<Image>().raycastTarget=true; Canvas.ForceUpdateCanvases();
            return rect;
        }
        void AssertUIHit(Vector2 centre)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=centre},hits);
            Assert.Greater(hits.Count,0,"The fixture must hit real UI before testing ownership.");
        }
        [UnityTest]
        public IEnumerator UIStartedTouchCannotSteerWhenDraggedAwayAndRepositionIsNeutral()
        {
            var rectangle=MakeBlockingUI(Point(.3f,.48f));
            // Overlay graphics acquire depth after the first canvas/render update, not during AddComponent.
            yield return null; Canvas.ForceUpdateCanvases();
            Vector2 ui=RectTransformUtility.WorldToScreenPoint(null,rectangle.position);
            AssertUIHit(ui); Vector3 origin=ball.Body.position;
            Touch(1,ui,InputTouchPhase.Began);Flush();yield return null;
            for(int i=1;i<=24;i++)
            {
                Touch(1,ui+Point(.25f,.18f)*(i/24f),InputTouchPhase.Moved);Flush();yield return null;yield return new WaitForFixedUpdate();
                Assert.Less(ball.Input.Move.sqrMagnitude,.000001f,"Touch ownership must survive leaving the UI rectangle.");
            }
            Assert.Less(Vector3.Distance(origin,ball.Body.position),.035f);Assert.Less(ball.Body.linearVelocity.magnitude,.06f);
            Touch(1,ui+Point(.25f,.18f),InputTouchPhase.Ended);Flush();yield return null;
            Vector2 start=Point(.25f,.2f);Touch(2,start,InputTouchPhase.Began);Flush();yield return null;
            Assert.Less(ball.Input.Move.magnitude,.001f,"A new pointer establishes an anchor without displacement.");
            for(int i=1;i<=30;i++)
            {
                Touch(2,start+Point(.15f,0)*(i/30f),InputTouchPhase.Moved);Flush();yield return null;yield return new WaitForFixedUpdate();
            }
            Assert.Greater(ball.Body.linearVelocity.x,.25f,"Physical motion must prove the accepted swipe drove right.");
            Touch(2,start+Point(.15f,0),InputTouchPhase.Ended);Flush();yield return null;
            Vector3 before=ball.Body.position;
            Vector2 reposition=Point(.65f,.2f);Touch(3,reposition,InputTouchPhase.Began);Flush();yield return null;
            Assert.Less(ball.Input.Move.magnitude,.001f,"Repositioning cannot interpret screen distance from the previous finger as movement.");
            Assert.Less(Vector3.Distance(before,ball.Body.position),.8f);
            float beforeReverse=ball.Body.linearVelocity.x;
            for(int i=1;i<=30;i++)
            {
                Touch(3,reposition-Point(.16f,0)*(i/30f),InputTouchPhase.Moved);Flush();yield return null;yield return new WaitForFixedUpdate();
            }
            Assert.Less(ball.Body.linearVelocity.x,beforeReverse-.25f,"An accepted left swipe must oppose the previous physical momentum.");
            Touch(3,reposition-Point(.16f,0),InputTouchPhase.Ended);Flush();yield return null;
        }
        [UnityTest]
        public IEnumerator ExtraFingerNeverOrbitsAndPrimaryFingerStillMovesTheBall()
        {
            Vector3 origin=ball.Body.position;Vector2 a=Point(.25f,.3f),b=Point(.5f,.3f);
            Touch(10,a,InputTouchPhase.Began);Touch(11,b,InputTouchPhase.Began);Flush();yield return null;
            Assert.IsFalse(ball.Input.CameraGesture);Assert.IsTrue(ball.Input.PointerHeld);
            for(int i=1;i<=24;i++)
            {
                Vector2 delta=Point(.14f,.08f)*(i/24f);
                Touch(10,a+delta,InputTouchPhase.Moved);Touch(11,b+delta,InputTouchPhase.Moved);Flush();yield return null;yield return new WaitForFixedUpdate();
                Assert.IsFalse(ball.Input.CameraGesture);Assert.IsTrue(ball.Input.PointerHeld);Assert.AreEqual(Vector2.zero,ball.Input.ConsumeCameraDelta());
            }
            Assert.AreEqual(Vector2.zero,ball.Input.ConsumeCameraDelta());Assert.Greater(ball.Body.linearVelocity.x,.2f);Assert.Greater(ball.Body.linearVelocity.z,.1f);
            Touch(10,a+Point(.14f,.08f),InputTouchPhase.Ended);Touch(11,b+Point(.14f,.08f),InputTouchPhase.Ended);Flush();yield return null;
            Vector2 edge=Point(.93f,.3f);mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=edge,buttons=2});InputSystem.Update();yield return null;
            for(int i=1;i<=24;i++)
            {
                InputSystem.QueueStateEvent(mouse,new MouseState{position=edge+Point(-.3f,.1f)*(i/24f),buttons=2});InputSystem.Update();yield return null;yield return new WaitForFixedUpdate();
                Assert.IsFalse(ball.Input.CameraGesture);Assert.AreEqual(Vector2.zero,ball.Input.ConsumeCameraDelta());
            }
            Assert.AreEqual(Vector2.zero,ball.Input.ConsumeCameraDelta());
            Assert.Greater(Vector3.Distance(origin,ball.Body.position),.05f,"Pointer movement must act on the physical ball, including desktop right-drag.");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=edge+Point(-.3f,.1f)});InputSystem.Update();yield return null;
        }
        [UnityTest]
        public IEnumerator SingleFingerSwipeStartingAtRightEdgeDrivesTheBallInsteadOfOrbiting()
        {
            Vector2 edge=Point(.94f,.3f);Touch(12,edge,InputTouchPhase.Began);Flush();yield return null;
            Assert.IsTrue(ball.Input.PointerHeld);Assert.IsFalse(ball.Input.CameraGesture);
            for(int i=1;i<=24;i++)
            {
                Touch(12,edge+Point(-.3f,.1f)*(i/24f),InputTouchPhase.Moved);Flush();yield return null;yield return new WaitForFixedUpdate();
                Assert.IsFalse(ball.Input.CameraGesture,"A single finger must steer everywhere outside actual UI.");
            }
            Assert.AreEqual(Vector2.zero,ball.Input.ConsumeCameraDelta());
            Assert.Less(ball.Body.linearVelocity.x,-.4f,"The formerly reserved right edge must supply real physical leftward drive.");
            Assert.Greater(ball.Body.linearVelocity.z,.1f);
            Touch(12,edge+Point(-.3f,.1f),InputTouchPhase.Ended);Flush();yield return null;
        }
    }
}
