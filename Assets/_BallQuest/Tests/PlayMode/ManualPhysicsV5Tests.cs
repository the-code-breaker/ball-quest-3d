using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class ManualPhysicsV5Tests
    {
        readonly List<Object> objects = new List<Object>();
        BallController ball;
        BallProfile profile;
        T Keep<T>(T value) where T:Object { objects.Add(value); return value; }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.position = new Vector3(0,-.5f,0); floor.transform.localScale = new Vector3(220,1,220);
            profile = Keep(ScriptableObject.CreateInstance<BallProfile>()); profile.bounce = .015f;
            var go = Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere)); go.layer = 8; go.transform.position = new Vector3(0,.6f,0);
            go.AddComponent<Rigidbody>(); go.AddComponent<BallInput>(); ball = go.AddComponent<BallController>();
            ball.ApplyProfile(profile); ball.SetExternalControl(true); ball.active = true;
            for (int i=0;i<15;i++) yield return new WaitForFixedUpdate();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; foreach (var item in objects) if(item) Object.Destroy(item); objects.Clear(); yield return null;
        }
        [UnityTest]
        public IEnumerator NoInputRestsAndPathMetadataCannotPushOrCentreTheBall()
        {
            Vector3 start = ball.Body.position;
            for (int i=0;i<120;i++)
            {
                ball.SetPathForward(Quaternion.Euler(0,i*3,0) * Vector3.forward);
                yield return new WaitForFixedUpdate();
            }
            Assert.Less(Vector3.Distance(start,ball.Body.position),.015f); Assert.Less(ball.Body.linearVelocity.magnitude,.04f);
            Assert.Less(ball.Body.angularVelocity.magnitude,.04f);
        }
        [UnityTest]
        public IEnumerator AllFourDirectionsHaveSymmetricPowerAndDiagonalIsBounded()
        {
            Vector2[] commands = { Vector2.up,Vector2.down,Vector2.left,Vector2.right,new Vector2(1,1).normalized };
            var speeds = new List<float>();
            foreach (var command in commands)
            {
                ball.Respawn(new Vector3(0,.55f,0)); ball.SetPathForward(Vector3.forward);
                for (int i=0;i<10;i++) yield return new WaitForFixedUpdate();
                ball.SubmitControl(new BallControlCommand(command.y,command.x));
                for (int i=0;i<65;i++) yield return new WaitForFixedUpdate();
                Vector3 direction = new Vector3(command.x,0,command.y);
                Vector3 velocity = Vector3.ProjectOnPlane(ball.Body.linearVelocity,Vector3.up);
                Assert.Greater(Vector3.Dot(velocity,direction),4); Assert.Less(Vector3.Angle(velocity,direction),4);
                speeds.Add(velocity.magnitude);
            }
            foreach (float speed in speeds) Assert.That(speed,Is.InRange(speeds[0]*.94f,speeds[0]*1.06f));
        }
        [UnityTest]
        public IEnumerator OppositeInputBrakesThroughZeroThenRollsBackward()
        {
            ball.SubmitControl(new BallControlCommand(1,0));
            for(int i=0;i<70;i++) yield return new WaitForFixedUpdate();
            float before = ball.Body.linearVelocity.z; Assert.Greater(before,4);
            ball.SubmitControl(new BallControlCommand(-1,0)); yield return new WaitForFixedUpdate();
            Assert.That(ball.Body.linearVelocity.z,Is.InRange(0,before));
            bool crossed = false;
            for(int i=0;i<170;i++) { yield return new WaitForFixedUpdate(); if(ball.Body.linearVelocity.z<0)crossed=true; }
            Assert.IsTrue(crossed); Assert.Less(ball.Body.linearVelocity.z,-4);
        }
        [UnityTest]
        public IEnumerator ReleaseCoastsThenStopsWithoutFreezingOrNewAcceleration()
        {
            ball.SubmitControl(new BallControlCommand(1,0));
            for(int i=0;i<75;i++) yield return new WaitForFixedUpdate();
            float before = ball.Body.linearVelocity.z, position = ball.Body.position.z;
            ball.SubmitControl(default);
            for(int i=0;i<12;i++) yield return new WaitForFixedUpdate();
            Assert.That(ball.Body.linearVelocity.z,Is.InRange(.5f,before+.01f));
            Assert.Greater(ball.Body.position.z,position+.3f);
            for(int i=0;i<570;i++) yield return new WaitForFixedUpdate();
            Assert.Less(ball.Body.linearVelocity.magnitude,.12f); Assert.Less(ball.Body.angularVelocity.magnitude,.3f);
        }
        [UnityTest]
        public IEnumerator GroundedRollingSpinMatchesTranslationAndCircularCommandsStayFinite()
        {
            ball.SubmitControl(new BallControlCommand(1,0));
            for(int i=0;i<90;i++) yield return new WaitForFixedUpdate();
            float velocity = Vector3.ProjectOnPlane(ball.Body.linearVelocity,Vector3.up).magnitude;
            float spin = Vector3.ProjectOnPlane(ball.Body.angularVelocity,Vector3.up).magnitude * ball.GroundDetector.Radius;
            Assert.That(spin / velocity,Is.InRange(.8f,1.2f),"Rolling reference is omega*R = V; correction must use bounded torque.");
            for(int i=0;i<240;i++)
            {
                float angle = i * Mathf.PI / 120;
                ball.SubmitControl(new BallControlCommand(Mathf.Cos(angle),Mathf.Sin(angle)));
                yield return new WaitForFixedUpdate();
                Assert.IsFalse(float.IsNaN(ball.Body.position.sqrMagnitude));
                Assert.Less(ball.Body.linearVelocity.magnitude,profile.maximumSpeed+.5f);
            }
        }
        [Test]
        public void TouchDirectionMagnitudeSamplingAndStationaryNeutralAreSymmetric()
        {
            BallInput input = ball.Input; input.enabled = false; input.inputSmoothing = .02f;
            foreach (BallTouchMode mode in new[]{BallTouchMode.RelativeDrag,BallTouchMode.GestureVelocity})
            {
                input.touchMode = mode;
                Vector2 baseline = Vector2.zero;
                foreach (int rate in new[]{30,60,90,120})
                {
                    Vector2[] directions = {Vector2.up,Vector2.down,Vector2.left,Vector2.right,new Vector2(1,1).normalized};
                    foreach (Vector2 direction in directions)
                    {
                        input.BeginPointer(Vector2.one*400);
                        Vector2 integrated=Vector2.zero;
                        for(int step=1;step<=rate/2;step++)
                        {
                            input.SamplePointer(Vector2.one*400+direction*(step*180f/rate),1f/rate,1000,1800);
                            integrated+=input.ConsumeMove()*Time.fixedDeltaTime;
                        }
                        Assert.Greater(Vector2.Dot(integrated,direction),.045f); Assert.Less(Vector2.Angle(integrated,direction),.1f);
                        if (direction==Vector2.up)
                        {
                            if(rate==30)baseline=integrated;
                            else Assert.Less(Mathf.Abs(integrated.magnitude-baseline.magnitude),.035f);
                        }
                        Vector2 point = Vector2.one*400+direction*90;
                        for(int step=0;step<rate/2;step++)input.SamplePointer(point,1f/rate,1000,1800);
                        Assert.Less(input.Move.magnitude,.002f,"Stationary fingers cannot leave an acceleration latched.");
                        input.EndPointer(); Assert.AreEqual(Vector2.zero,input.Move);
                    }
                }
            }
            input.touchMode=BallTouchMode.RelativeDrag; input.inputSmoothing=0;
            // Compare unsaturated small strokes after the v0.8 sensitivity reduction.
            input.BeginPointer(Vector2.zero); input.SamplePointer(Vector2.up*3,.02f,1000,1800); float small=input.Move.y;
            input.BeginPointer(Vector2.zero); input.SamplePointer(Vector2.up*9,.02f,1000,1800); Assert.Greater(input.Move.y,small*2.8f);
        }
        [UnityTest]
        public IEnumerator GravityRelativeGroundingWorksOnAWallAndResetRestoresNormalGravity()
        {
            var wall=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.transform.position=new Vector3(0,8,6); wall.transform.localScale=new Vector3(30,20,1);
            ball.Respawn(new Vector3(0,8,4.8f)); ball.SetGravityDirection(Vector3.forward*9.81f);
            for(int i=0;i<45;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Grounded); Assert.Greater(Vector3.Dot(ball.GroundNormal,Vector3.back),.98f);
            Assert.Greater(Vector3.Dot(ball.MovementUp,Vector3.back),.99f); Assert.IsNotNull(ball.GroundDetector.SupportCollider);
            Assert.Greater(Vector3.Dot(ball.ControlForward,Vector3.up),.99f,"Control basis parallel-transports through wall gravity.");
            ball.Respawn(new Vector3(0,.6f,0)); Assert.Greater(Vector3.Dot(ball.MovementUp,Vector3.up),.99f);
        }
        CameraState Step(QuestCamera camera,CinemachineCamera vcam,CameraState state,float dt)
        {
            var method=typeof(QuestCamera).GetMethod("PostPipelineStageCallback",BindingFlags.Instance|BindingFlags.NonPublic);
            object[] arguments={vcam,CinemachineCore.Stage.Finalize,state,dt};method.Invoke(camera,arguments);return (CameraState)arguments[2];
        }
        [Test]
        public void StableCameraIgnoresRouteAndBallSpinAndManualOrbitDoesNotDriveTheBody()
        {
            var go=Keep(new GameObject("Stable orbit verification"));var vcam=go.AddComponent<CinemachineCamera>();var camera=go.AddComponent<QuestCamera>();camera.target=ball;camera.mode=BallCameraMode.StableOrbit;camera.collisionAvoidance=false;camera.effectsEnabled=false;
            var first=Step(camera,vcam,CameraState.Default,0);Vector3 velocity=ball.Body.linearVelocity;
            ball.SetPathForward(Vector3.back);ball.Body.rotation=Quaternion.Euler(130,80,42);
            var after=Step(camera,vcam,first,.016f);
            Assert.Less(Quaternion.Angle(first.RawOrientation,after.RawOrientation),.01f);
            ball.Body.linearVelocity=Vector3.back*8;
            for(int i=0;i<100;i++)after=Step(camera,vcam,after,.016f);
            Assert.Less(Vector3.Angle(camera.Forward,Vector3.forward),.01f,"Reverse motion must not flip stable orbit.");
            ball.Body.linearVelocity=velocity;camera.Orbit(new Vector2(.5f,0));
            for(int i=0;i<100;i++)after=Step(camera,vcam,after,.016f);
            Assert.Greater(Vector3.Dot(camera.Forward,Vector3.right),.99f);Assert.AreEqual(velocity,ball.Body.linearVelocity);
            Assert.Greater(Vector3.Dot(ball.ControlForward,Vector3.right),.99f);
        }
    }
}
