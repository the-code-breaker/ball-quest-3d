using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class ObstacleLifecycleTests
    {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        T Keep<T>(T item)where T:UnityEngine.Object{owned.Add(item);return item;}
        BallController Ball(Vector3 position)
        {var go=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));go.layer=8;go.transform.position=position;go.AddComponent<Rigidbody>();go.AddComponent<BallInput>();var ball=go.AddComponent<BallController>();ball.ApplyProfile(Keep(ScriptableObject.CreateInstance<BallProfile>()));ball.active=true;return ball;}
        [UnityTest]public IEnumerator FallingDeckWarnsFallsUnderGravityAndResetsWithoutMomentum()
        {
            var go=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));go.transform.position=new Vector3(0,4,0);go.transform.localScale=new Vector3(6,.7f,8);var body=go.AddComponent<Rigidbody>();
            var obstacle=go.AddComponent<QuestObstacle>();obstacle.kind=ObstacleKind.FallingTile;obstacle.warningSeconds=.75f;
            var ball=Ball(new Vector3(0,5.1f,0));
            for(int i=0;i<20;i++)yield return new WaitForFixedUpdate();Assert.IsTrue(obstacle.Collapsing);Assert.IsTrue(body.isKinematic);
            for(int i=0;i<60;i++)yield return new WaitForFixedUpdate();Assert.IsFalse(body.isKinematic);Assert.IsTrue(body.useGravity);Assert.Less(body.position.y,3.8f);
            obstacle.ResetCourseState();Assert.IsTrue(body.isKinematic);Assert.IsFalse(obstacle.Collapsing);Assert.Less(Vector3.Distance(body.position,new Vector3(0,4,0)),.01f);
            Assert.Less(body.linearVelocity.magnitude,.01f);
        }
        [UnityTest]public IEnumerator TimedDoorHasSafeWarningAndRepeatableReset()
        {
            var go=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));go.transform.position=new Vector3(0,2,20);go.AddComponent<Rigidbody>();var door=go.AddComponent<QuestObstacle>();door.kind=ObstacleKind.TimedDoor;door.speed=0;door.phase=3.5f;
            yield return new WaitForFixedUpdate();Assert.IsTrue(door.IsPassable);Assert.IsFalse(go.GetComponent<Collider>().enabled);
            door.phase=4.2f;yield return new WaitForFixedUpdate();Assert.IsFalse(door.IsPassable);Assert.IsTrue(go.GetComponent<Collider>().enabled);
            door.phase=0;door.ResetCourseState();Assert.IsTrue(door.IsPassable);Assert.IsFalse(go.GetComponent<Collider>().enabled);
        }
        [UnityTest]public IEnumerator OtherBallsCannotFinishOrCollectForSoloPlayer()
        {
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,50);
            var player=Ball(new Vector3(-6,.6f,0));var rival=Ball(new Vector3(6,.6f,0));
            var game=Keep(new GameObject("Session")).AddComponent<GameSession>();var level=Keep(ScriptableObject.CreateInstance<LevelDefinition>());
            string folder=Path.Combine(Path.GetTempPath(),"BallQuestIdentity-"+Guid.NewGuid());game.Initialize(new SaveStore(folder,new UnitySaveCodec()),55);game.Begin(level,player);
            var coin=Keep(new GameObject("Coin"));coin.transform.position=rival.transform.position;coin.AddComponent<SphereCollider>().isTrigger=true;coin.AddComponent<Collectible>().session=game;
            var gate=Keep(new GameObject("Gate"));gate.transform.position=rival.transform.position;gate.AddComponent<BoxCollider>().isTrigger=true;var trigger=gate.AddComponent<CourseTrigger>();trigger.session=game;trigger.finish=true;
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();Assert.AreEqual(0,game.Coins);Assert.AreEqual(SessionState.Playing,game.State);Assert.IsTrue(coin.activeSelf);
            player.Respawn(new Vector3(6,.6f,0));yield return new WaitForSecondsRealtime(.12f);Assert.AreEqual(SessionState.Complete,game.State);
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;foreach(var item in owned)if(item)UnityEngine.Object.Destroy(item);owned.Clear();yield return null;}
    }
}

