using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class GameplayTests
    {
        readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        BallController ball;
        BallProfile profile;
        Keyboard keyboard;
        GameSession session;
        LevelDefinition definition;
        SaveStore store;
        string directory;
        InputSettings.BackgroundBehavior backgroundBehavior;
        bool runInBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;
#endif

        T Keep<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            runInBackground = Application.runInBackground; Application.runInBackground = true;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>();
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(30, 1, 300);
            var go = Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere)); go.layer = 8; go.transform.position = new Vector3(0, .6f, 0);
            go.AddComponent<Rigidbody>(); go.AddComponent<BallInput>(); ball = go.AddComponent<BallController>();
            profile = Keep(ScriptableObject.CreateInstance<BallProfile>()); ball.ApplyProfile(profile); ball.allowJump = true;
            session = Keep(new GameObject("Session")).AddComponent<GameSession>();
            definition = Keep(ScriptableObject.CreateInstance<LevelDefinition>()); definition.spawn = new Vector3(0, .6f, 0); definition.finish = new Vector3(0, 1, 20);
            directory = Path.Combine(Path.GetTempPath(), "BallQuestPlay-" + Guid.NewGuid());
            store = new SaveStore(directory, new UnitySaveCodec()); session.Initialize(store, 3); session.Begin(definition, ball);
            session.fallingSeconds=0;session.respawnSeconds=0;
            yield return new WaitForFixedUpdate(); yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Time.timeScale = 1;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            Application.runInBackground = runInBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#endif
            foreach (var value in objects) if (value) UnityEngine.Object.Destroy(value);
            objects.Clear();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovementAcceleratesAndReleaseKeepsMomentum()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            keyboard.MakeCurrent(); InputSystem.Update();
            yield return null;
            for (int i = 0; i < 45; i++) yield return new WaitForFixedUpdate();
            float speed = ball.Body.linearVelocity.z;
            Assert.Greater(speed, 1); Assert.LessOrEqual(speed, profile.maxSpeed + .3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            for (int i = 0; i < 45; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.z, .1f,"Lifting must coast rather than instantly freeze.");
            Assert.Less(ball.Body.linearVelocity.z, speed,"Grounded resistance must gradually remove momentum.");
        }

        [UnityTest]
        public IEnumerator ManualPointerRollsCoastsAndOppositeDragBrakesThenReverses()
        {
            var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
            try
            {
                Vector2 start=new Vector2(Screen.width*.5f,Screen.height*.3f);
                void Send(Vector2 point,bool held)
                {
                    var state=new MouseState{position=point};
                    if(held)state=state.WithButton(MouseButton.Left);
                    InputSystem.QueueStateEvent(mouse,state);InputSystem.Update();
                }
                IEnumerator Stroke(Vector2 direction)
                {
                    float extent=Mathf.Min(Screen.width,Screen.height);
                    Send(start,true);yield return null;
                    for(int i=1;i<=8;i++)
                    {Send(start+direction*(extent*.1f*i/8),true);yield return new WaitForFixedUpdate();yield return null;}
                    Send(start+direction*(extent*.1f),false);yield return null;yield return new WaitForFixedUpdate();
                }
                // v0.8 deliberately caps one stroke. Exercise the same rolling/coasting/
                // reversing contract with repeated mouse swipes, as the player now does.
                for(int stroke=0;stroke<4;stroke++)yield return Stroke(Vector2.up);
                float speed=ball.Body.linearVelocity.z;Assert.Greater(speed,3);
                yield return new WaitForFixedUpdate();
                Assert.Greater(ball.Body.linearVelocity.z,.1f,"Release must not erase existing momentum.");
                for(int i=0;i<30;i++)yield return new WaitForFixedUpdate();
                Assert.Less(ball.Body.linearVelocity.z,speed,"Neutral contact resistance must slow the ball.");
                for(int stroke=0;stroke<6;stroke++)yield return Stroke(Vector2.down);
                Assert.Less(ball.Body.linearVelocity.z,-1,"Repeated opposing swipes must brake through zero and roll backward.");
                Send(start,false);yield return null;
            }
            finally {InputSystem.RemoveDevice(mouse);}
        }

        [UnityTest]
        public IEnumerator TurboTriggerIncreasesSpeedAndExpires()
        {
            var pad=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));pad.transform.position=ball.transform.position;
            pad.GetComponent<Collider>().isTrigger=true;pad.AddComponent<Rigidbody>().isKinematic=true;
            var boost=pad.AddComponent<QuestObstacle>();boost.kind=ObstacleKind.Booster;boost.axis=Vector3.forward;
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Boosted);Assert.Less(ball.Body.linearVelocity.z,3,"A powered pad should accelerate over time rather than inject an instant extreme velocity.");
            for(int i=0;i<40;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.z,profile.maxSpeed);
            for(int i=0;i<105;i++)yield return new WaitForFixedUpdate();
            Assert.IsFalse(ball.Boosted);Assert.LessOrEqual(ball.SpeedLimit,22);
            Assert.Greater(ball.Body.linearVelocity.z,1,"A powered impulse must coast naturally after its visual effect expires.");
        }

        [UnityTest]
        public IEnumerator GravityAcceleratesAnUntouchedBallDownASlope()
        {
            var slope=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
            slope.transform.SetPositionAndRotation(new Vector3(0,4,100),Quaternion.Euler(15,0,0));
            slope.transform.localScale=new Vector3(15,1,24);
            ball.Respawn(new Vector3(0,5.1f,100));
            for(int i=0;i<80;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.z,1,"Gravity must accelerate the ball without a stored cruise-speed motor resisting it.");
            Assert.Greater(ball.Body.angularVelocity.magnitude,1,"The sphere must rotate physically as it rolls.");
        }

        [UnityTest]
        public IEnumerator HeavyBallRespondsLessToTheSameImpulse()
        {
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            ball.Body.AddForce(Vector3.forward*8,ForceMode.Impulse);yield return new WaitForFixedUpdate();
            float lightSpeed=ball.Body.linearVelocity.z;
            ball.Respawn(new Vector3(0,.6f,0));profile.mass=3;ball.ApplyProfile(profile);
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            ball.Body.AddForce(Vector3.forward*8,ForceMode.Impulse);yield return new WaitForFixedUpdate();
            Assert.Less(ball.Body.linearVelocity.z,lightSpeed*.6f);
        }

        [UnityTest]
        public IEnumerator JumpRequiresGroundAndAddsUpwardVelocity()
        {
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Grounded);
            ball.Input.RequestJump(); yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.y, 2);
            float before = ball.Body.linearVelocity.y;
            ball.Input.RequestJump(); yield return new WaitForFixedUpdate();
            Assert.Less(ball.Body.linearVelocity.y, before + .1f);
        }

        [UnityTest]
        public IEnumerator FallingRecoversAtCheckpointWithZeroMomentum()
        {
            var checkpoint = new Vector3(1, .7f, 5); session.Checkpoint(0, checkpoint);
            ball.Body.position = new Vector3(0, -30, 0); yield return null; yield return null;
            Assert.AreEqual(SessionState.Fallen, session.State); Assert.AreEqual(1, session.Falls);
            session.Respawn();
            Assert.AreEqual(SessionState.Playing, session.State);
            Assert.Less(Vector3.Distance(checkpoint, ball.Body.position), .01f); Assert.Less(ball.Body.linearVelocity.magnitude, .01f);
        }

        [UnityTest]
        public IEnumerator CollectibleCollisionCountsOnce()
        {
            var pickup = Keep(new GameObject("Coin")); pickup.transform.position = ball.transform.position;
            var trigger = pickup.AddComponent<SphereCollider>(); trigger.radius = 1; trigger.isTrigger = true;
            var collectible = pickup.AddComponent<Collectible>(); collectible.session = session;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, session.Coins); Assert.IsFalse(pickup.activeSelf);
        }

        [Test]
        public void PauseResumeAndFinishHaveExplicitStates()
        {
            session.Pause(); Assert.AreEqual(0, Time.timeScale); Assert.IsFalse(ball.Input.active);
            session.Resume(); Assert.AreEqual(1, Time.timeScale); Assert.IsTrue(ball.Input.active);
            session.Collect(PickupKind.Coin); session.Finish();
            Assert.AreEqual(SessionState.Complete, session.State); Assert.AreEqual(2, store.Data.unlockedLevel);
            int coins = store.Data.coins; session.Finish(); Assert.AreEqual(coins, store.Data.coins);
        }

        [UnityTest]
        public IEnumerator FeatherCapsDescent()
        {
            profile.ability = BallAbility.Feather; ball.ApplyProfile(profile);
            ball.Body.position = new Vector3(0, 20, 0); ball.Body.linearVelocity = Vector3.down * 15;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.y, -3.5f);
        }

        [UnityTest]
        public IEnumerator LaserWarningRemainsPassableUntilDangerPhase()
        {
            var gate = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); gate.transform.position = new Vector3(10, 1, 10);
            gate.AddComponent<Rigidbody>().isKinematic = true;
            var obstacle = gate.AddComponent<QuestObstacle>(); obstacle.kind = ObstacleKind.Laser; obstacle.speed = 0; obstacle.phase = 2.8f;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(gate.GetComponent<Collider>().enabled);
            obstacle.phase = 4.2f; yield return new WaitForFixedUpdate();
            Assert.IsTrue(gate.GetComponent<Collider>().enabled);
        }
    }
}
