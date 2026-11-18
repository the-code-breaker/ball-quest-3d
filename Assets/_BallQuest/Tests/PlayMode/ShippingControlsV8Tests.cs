using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace BallQuest.Tests
{
    public sealed class ShippingControlsV8Tests
    {
        Scene original,loaded;
        string directory;
        Touchscreen touchscreen;
        Keyboard keyboard;
        Mouse mouse;
        GameBootstrap app;
        Camera output;
        CinemachineBrain brain;
        int previousTarget,previousVsync;
        float previousCapture;
        bool previousBackground;
        InputSettings.BackgroundBehavior previousInputBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
        float maxHorizontalError,maxVerticalError;
        [Serializable]sealed class Evidence
        {
            public string environment="Actual shipping bootstrap, Input System touches, Rigidbody, and Cinemachine output in Unity Editor";
            public float firstStrokeSpeed,fourStrokeSpeed,maxHorizontalError,maxVerticalError,headingChangeAfterRightSwipe;
            public int falls;
            public bool physicalAndroidDevice;
        }
        void Touch(int id,Vector2 point,TouchPhase phase)
        {
            touchscreen.MakeCurrent();InputSystem.QueueStateEvent(touchscreen,new TouchState{touchId=id,position=point,phase=phase,pressure=phase==TouchPhase.Ended?0:1});InputSystem.Update();
        }
        void CheckFrame()
        {
            brain.ManualUpdate();
            Vector3 point=output.WorldToViewportPoint(app.Session.Ball.transform.position);
            maxHorizontalError=Mathf.Max(maxHorizontalError,Mathf.Abs(point.x-.5f));
            maxVerticalError=Mathf.Max(maxVerticalError,Mathf.Abs(point.y-.4f));
            Assert.Greater(point.z,1);Assert.Less(Mathf.Abs(point.x-.5f),.025f,"The actual shipping camera must keep the rolling ball horizontally centered.");
            Assert.Less(Mathf.Abs(point.y-.4f),.035f,"The ball must retain its central gameplay height while steering.");
        }
        IEnumerator Stroke(int id,Vector2 direction,float distance)
        {
            Vector2 start=new Vector2(Screen.width*.35f,Screen.height*.36f);
            float extent=Mathf.Min(Screen.width,Screen.height);
            Touch(id,start,TouchPhase.Began);yield return null;CheckFrame();
            for(int i=1;i<=6;i++)
            {
                Touch(id,start+direction*(extent*distance*i/6),TouchPhase.Moved);yield return null;CheckFrame();yield return new WaitForFixedUpdate();
            }
            Touch(id,start+direction*(extent*distance),TouchPhase.Ended);yield return null;CheckFrame();yield return new WaitForFixedUpdate();
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator InstalledDefaultsBuildSpeedAcrossStrokesAndKeepSidewaysMotionCentered()
        {
            previousTarget=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;previousCapture=Time.captureDeltaTime;
            previousBackground=Application.runInBackground;previousInputBackground=InputSystem.settings.backgroundBehavior;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.captureDeltaTime=1f/60;Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();touchscreen=InputSystem.AddDevice<Touchscreen>();touchscreen.MakeCurrent();
            directory=Path.Combine(Path.GetTempPath(),"BallQuestShippingV8-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();const string path="Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);loaded=SceneManager.GetSceneByPath(path);SceneManager.SetActiveScene(loaded);
            float end=Time.realtimeSinceStartup+20;
            while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;
            app=GameBootstrap.Instance;Assert.IsTrue(app&&app.Ready);app.Play(1);yield return BootstrapFlowTests.AwaitPlaying(app);
            output=Camera.main;brain=output.GetComponent<CinemachineBrain>();brain.UpdateMethod=CinemachineBrain.UpdateMethods.ManualUpdate;
            Assert.AreEqual(BallCameraMode.AdaptiveChase,app.CurrentCourse.FollowCamera.mode);
            var record=new Evidence();
            for(int stroke=0;stroke<4;stroke++)
            {
                yield return Stroke(stroke+1,Vector2.up,.1f);
                float speed=Vector3.ProjectOnPlane(app.Session.Ball.Body.linearVelocity,Vector3.up).magnitude;
                if(stroke==0)record.firstStrokeSpeed=speed;
                if(stroke==3)record.fourStrokeSpeed=speed;
            }
            Assert.That(record.firstStrokeSpeed,Is.InRange(.9f,2.6f));
            Assert.Greater(record.fourStrokeSpeed,record.firstStrokeSpeed+2);
            Assert.Less(record.fourStrokeSpeed,9,"Four short swipes should build speed progressively, not jump to the powered ceiling.");
            Vector3 before=app.CurrentCourse.FollowCamera.Forward;
            yield return Stroke(8,Vector2.right,.075f);
            for(int i=0;i<24;i++){yield return null;CheckFrame();}
            record.headingChangeAfterRightSwipe=Vector3.Angle(before,app.CurrentCourse.FollowCamera.Forward);
            Assert.Greater(record.headingChangeAfterRightSwipe,5,"A modest rightward steering stroke must change the view without the old 40-degree gate.");
            Assert.AreEqual(0,app.Session.Falls);Assert.IsTrue(app.Session.Ball.Grounded);
            record.falls=app.Session.Falls;record.maxHorizontalError=maxHorizontalError;record.maxVerticalError=maxVerticalError;
            BootstrapFlowTests.Capture("V10ShippingSteering");Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/shipping-controls-v10.json",JsonUtility.ToJson(record,true));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);
            GameBootstrap.EditorSaveDirectory=null;
            foreach(var device in new InputDevice[]{touchscreen,keyboard,mouse})if(device!=null&&device.added)InputSystem.RemoveDevice(device);
            Application.targetFrameRate=previousTarget;QualitySettings.vSyncCount=previousVsync;Time.captureDeltaTime=previousCapture;
            Application.runInBackground=previousBackground;InputSystem.settings.backgroundBehavior=previousInputBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;
#endif
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
