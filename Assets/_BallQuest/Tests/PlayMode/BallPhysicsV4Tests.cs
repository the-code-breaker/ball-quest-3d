using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class BallPhysicsV4Tests
    {
        readonly List<Object> objects=new List<Object>();
        BallController ball;
        BallProfile profile;
        GameObject floor;
        T Keep<T>(T value)where T:Object{objects.Add(value);return value;}
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale=1;
            floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(150,1,300);
            profile=Keep(ScriptableObject.CreateInstance<BallProfile>());profile.bounce=.025f;
            ball=MakeBall(new Vector3(0,.6f,0));
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
        }
        BallController MakeBall(Vector3 point)
        {
            var go=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));go.layer=8;go.transform.position=point;go.AddComponent<Rigidbody>();go.AddComponent<BallInput>();
            var controller=go.AddComponent<BallController>();controller.ApplyProfile(profile);controller.SetExternalControl(true);controller.active=true;return controller;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;foreach(var item in objects)if(item)Object.Destroy(item);objects.Clear();yield return null;
        }
        [UnityTest]
        public IEnumerator SharedCommandsSteerBothWaysWithoutInstantReversalAndSettleOnRelease()
        {
            ball.SubmitControl(new BallControlCommand(.7f,1));
            for(int i=0;i<60;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.position.x,1);float rightSpeed=ball.Body.linearVelocity.x;Assert.Greater(rightSpeed,1);
            ball.SubmitControl(new BallControlCommand(.7f,-1));yield return new WaitForFixedUpdate();
            Assert.Greater(ball.Body.linearVelocity.x,0,"Steering must change momentum through force, not instantly reverse velocity.");
            for(int i=0;i<90;i++)yield return new WaitForFixedUpdate();
            Assert.Less(ball.Body.linearVelocity.x,-1);
            ball.SubmitControl(default);
            for(int i=0;i<540;i++)yield return new WaitForFixedUpdate();
            Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x),.5f);Assert.Less(Mathf.Abs(ball.Steering),.01f);
        }
        [UnityTest]
        public IEnumerator LongThrottleAndExtremeSteeringRespectConfiguredLimits()
        {
            ball.SubmitControl(new BallControlCommand(1,100));
            for(int i=0;i<240;i++)yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(ball.Body.linearVelocity.z,profile.maxSpeed+.6f);
            Assert.LessOrEqual(Vector3.ProjectOnPlane(ball.Body.linearVelocity,Vector3.up).magnitude,profile.maxSpeed+.7f,"All directions share one manual speed cap.");
            Assert.LessOrEqual(ball.Body.angularVelocity.magnitude,profile.maximumAngularSpeed+.1f);
        }
        [UnityTest]
        public IEnumerator SupportOnAnotherSolidLayerIsGroundedAndWallsAreNot()
        {
            floor.layer=12;
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Grounded);Assert.Greater(Vector3.Dot(ball.GroundNormal,Vector3.up),.99f);
            var wall=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));wall.transform.position=new Vector3(0,4,1);wall.transform.localScale=new Vector3(8,8,1);
            ball.Respawn(new Vector3(0,4,0));ball.Body.linearVelocity=Vector3.forward*2;
            for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
            Assert.IsFalse(ball.Grounded,"A vertical wall contact is not floor support.");
        }
        [UnityTest]
        public IEnumerator AirborneBallPreservesForwardMomentumAndFallsUnderGravity()
        {
            ball.Respawn(new Vector3(0,8,0));ball.Body.linearVelocity=Vector3.forward*8;
            for(int i=0;i<30;i++)yield return new WaitForFixedUpdate();
            Assert.IsFalse(ball.Grounded);Assert.Greater(ball.Body.linearVelocity.z,7.6f);Assert.Less(ball.Body.position.y,7.1f);
        }
        [UnityTest]
        public IEnumerator OrdinaryLandingDoesNotBounceExcessivelyAndRestDoesNotJitter()
        {
            ball.Respawn(new Vector3(0,5,0));
            for(int i=0;i<180;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Grounded);float low=ball.Body.position.y,high=low;
            for(int i=0;i<40;i++){yield return new WaitForFixedUpdate();low=Mathf.Min(low,ball.Body.position.y);high=Mathf.Max(high,ball.Body.position.y);}
            Assert.Less(high,.65f);Assert.Less(high-low,.008f);Assert.Less(ball.Body.linearVelocity.magnitude,.08f);
        }
        [UnityTest]
        public IEnumerator MovingPlatformCarriesThroughContactAndRelativeSteering()
        {
            var platform=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));platform.transform.position=new Vector3(0,2,40);platform.transform.localScale=new Vector3(12,1,16);
            var support=platform.AddComponent<Rigidbody>();support.isKinematic=true;support.interpolation=RigidbodyInterpolation.Interpolate;
            ball.Respawn(new Vector3(0,3.1f,40));
            for(int i=0;i<20;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(support,ball.GroundDetector.SupportBody);
            for(int i=0;i<90;i++){support.MovePosition(support.position+Vector3.right*.02f);yield return new WaitForFixedUpdate();}
            Assert.Greater(ball.Body.position.x,1);Assert.Greater(ball.SupportVelocity.x,.8f);Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x-ball.SupportVelocity.x),.65f);
            Assert.Greater(ball.Body.position.y,2.9f);Assert.IsNull(ball.transform.parent,"Carry must be physical, not transform parenting.");
        }
        [UnityTest]
        public IEnumerator RearCollisionTransfersMomentumWithoutTunnellingOrExploding()
        {
            var front=MakeBall(new Vector3(0,.55f,5));
            ball.Body.linearVelocity=Vector3.forward*14;
            for(int i=0;i<60;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(front.Body.linearVelocity.z,2);Assert.Less(ball.Body.position.z,front.Body.position.z+.1f);
            Assert.Greater(Vector3.Distance(ball.Body.position,front.Body.position),.85f);Assert.Less(ball.Body.linearVelocity.magnitude,20);Assert.Less(front.Body.linearVelocity.magnitude,20);
        }
        [Test]
        public void NonFiniteCommandsAreSafelyNeutralized()
        {
            var command=new BallControlCommand(float.NaN,float.PositiveInfinity,float.NegativeInfinity);
            Assert.AreEqual(0,command.throttle);Assert.AreEqual(0,command.steering);Assert.AreEqual(0,command.brake);
        }
        CameraState Step(QuestCamera camera,CinemachineCamera vcam,CameraState state,float dt)
        {
            var method=typeof(QuestCamera).GetMethod("PostPipelineStageCallback",BindingFlags.Instance|BindingFlags.NonPublic);
            object[] arguments={vcam,CinemachineCore.Stage.Finalize,state,dt};method.Invoke(camera,arguments);return (CameraState)arguments[2];
        }
        [Test]
        public void ActualChaseCameraIsStableAtThirtyAndOneHundredTwentySamples()
        {
            var go=Keep(new GameObject("Chase frame-rate validation"));var vcam=go.AddComponent<CinemachineCamera>();var chase=go.AddComponent<QuestCamera>();chase.target=ball;chase.collisionAvoidance=false;
            CameraState Evaluate(int rate)
            {
                ball.Body.position=new Vector3(0,.5f,0);ball.transform.position=ball.Body.position;ball.Body.linearVelocity=Vector3.zero;ball.SetPathForward(Vector3.forward);chase.Snap();
                var state=Step(chase,vcam,CameraState.Default,0);
                ball.Body.linearVelocity=Vector3.forward*10;
                // Synthetic visual poses isolate camera sampling from the separate rigidbody tests.
                for(int i=1;i<=rate*2;i++){ball.Body.position=new Vector3(0,.5f,i*10f/rate);ball.transform.position=ball.Body.position;state=Step(chase,vcam,state,1f/rate);}
                return state;
            }
            var thirty=Evaluate(30);var fast=Evaluate(120);
            Assert.That(fast.RawPosition.z,Is.InRange(11f,15f));
            Assert.Less(Vector3.Distance(thirty.RawPosition,fast.RawPosition),.25f);
            Assert.Less(Quaternion.Angle(thirty.RawOrientation,fast.RawOrientation),1);
            Assert.Less(Mathf.Abs(thirty.Lens.FieldOfView-fast.Lens.FieldOfView),.02f);
            ball.Body.rotation=Quaternion.Euler(130,70,20);ball.transform.rotation=ball.Body.rotation;var rolled=Step(chase,vcam,fast,0);
            Assert.Less(Quaternion.Angle(fast.RawOrientation,rolled.RawOrientation),.001f,"Camera orientation must ignore the sphere's rolling orientation.");
        }
        [Test]
        public void ChaseCameraAvoidsSolidGeometryAndOffersOptionalEffects()
        {
            var wall=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));wall.transform.position=new Vector3(0,3,-4);wall.transform.localScale=new Vector3(8,6,1);Physics.SyncTransforms();
            var go=Keep(new GameObject("Camera obstruction validation"));var vcam=go.AddComponent<CinemachineCamera>();var chase=go.AddComponent<QuestCamera>();chase.target=ball;chase.effectsEnabled=false;chase.Landing(20);chase.Impact(20);
            var state=Step(chase,vcam,CameraState.Default,0);
            Assert.Greater(state.RawPosition.z,-4);Assert.IsFalse(wall.GetComponent<Collider>().bounds.Contains(state.RawPosition));
            Assert.AreEqual(chase.baseFieldOfView,state.Lens.FieldOfView,.01f);
            chase.SetRaceContext(4);chase.Snap();state=Step(chase,vcam,state,0);
            Assert.Greater(state.Lens.FieldOfView,chase.baseFieldOfView+2.5f);
        }
    }
}
