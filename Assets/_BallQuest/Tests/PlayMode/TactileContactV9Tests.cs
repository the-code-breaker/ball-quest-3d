using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class TactileContactV9Tests
    {
        readonly List<Object> owned=new List<Object>();
        BallController ball;BallContactAudio audio;Collider floor;
        [System.Serializable]sealed class Evidence
        {
            public string environment="Unity Editor physical collisions and bounded contact presentation; no human audio audition claim";
            public string actualHitSurface;public int effectCapacity,maximumActiveRings;public bool mutedCollisionAnimated,visualOwnerDisposed;
            public float slowRollingPitch,fastRollingPitch;
        }
        static Evidence evidence;
        [OneTimeSetUp]public void Begin()=>evidence=new Evidence();
        [OneTimeTearDown]public void Report(){Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/tactile-contact-v10.json",JsonUtility.ToJson(evidence,true));}
        T Keep<T>(T value)where T:Object{owned.Add(value);return value;}
        [UnitySetUp]public IEnumerator Setup()
        {
            Time.timeScale=1;
            var deck=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));deck.transform.position=Vector3.down*.5f;deck.transform.localScale=new Vector3(120,1,120);floor=deck.GetComponent<Collider>();deck.AddComponent<BallSurfaceTag>().surface=SurfaceAudioKind.Wood;
            var go=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));go.transform.position=new Vector3(0,.6f,0);go.AddComponent<Rigidbody>();go.AddComponent<BallInput>();ball=go.AddComponent<BallController>();
            var profile=Keep(ScriptableObject.CreateInstance<BallProfile>());profile.bounce=0;ball.ApplyProfile(profile);ball.active=true;ball.SetExternalControl(true);
            audio=go.AddComponent<BallContactAudio>();audio.SetMaterial(BallAudioMaterial.Metal);audio.masterVolume=1;
            for(int i=0;i<25;i++)yield return new WaitForFixedUpdate();yield return null;
        }
        GameObject Wall()
        {
            var wall=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));wall.transform.position=new Vector3(3,2,0);wall.transform.localScale=new Vector3(.5f,4,10);wall.AddComponent<BallSurfaceTag>().surface=SurfaceAudioKind.Metal;return wall;
        }
        [UnityTest]public IEnumerator CollisionUsesTheStruckMetalWallRatherThanTheWoodUnderTheBall()
        {
            Wall();Assert.AreEqual(SurfaceAudioKind.Wood,audio.CurrentSurface);int before=audio.PlayedImpactCount;
            ball.Body.linearVelocity=Vector3.right*10;float end=Time.time+2;
            while((audio.PlayedImpactCount==before||audio.LastImpactSurface!=SurfaceAudioKind.Metal)&&Time.time<end)yield return new WaitForFixedUpdate();
            Assert.Greater(audio.PlayedImpactCount,before);Assert.AreEqual(SurfaceAudioKind.Metal,audio.LastImpactSurface);
            Assert.AreSame(audio.profile.Response(SurfaceAudioKind.Metal).impactOverride,audio.LastImpactClip);
            Assert.AreEqual(SurfaceAudioKind.Metal,audio.Feedback.LastSurface);Assert.Greater(Vector3.Dot(audio.Feedback.LastContactNormal,Vector3.left),.8f);
            Assert.LessOrEqual(audio.ActiveImpactVoices,4);Assert.Greater(audio.Feedback.ActiveRings,0);evidence.actualHitSurface=audio.LastImpactSurface.ToString();
        }
        [UnityTest]public IEnumerator MutedPhysicalCollisionStillShowsContactFeedbackWithoutScalingPhysics()
        {
            Wall();audio.masterVolume=0;yield return null;int before=audio.Feedback.EmittedContacts;
            Vector3 scale=ball.transform.localScale;ball.Body.linearVelocity=Vector3.right*10;float end=Time.time+2;
            while((audio.Feedback.EmittedContacts==before||audio.Feedback.LastSurface!=SurfaceAudioKind.Metal)&&Time.time<end)yield return new WaitForFixedUpdate();
            Assert.Greater(audio.Feedback.EmittedContacts,before);Assert.AreEqual(SurfaceAudioKind.Metal,audio.Feedback.LastSurface);
            Assert.AreEqual(0,audio.ActiveImpactVoices);Assert.AreEqual(scale,ball.transform.localScale);Assert.IsFalse(ball.Body.isKinematic);
            Assert.IsFalse(audio.Feedback.PresentationRoot.transform.IsChildOf(ball.transform),"Contact effects must not add renderer hierarchy or scale to the physics ball.");evidence.mutedCollisionAnimated=true;
        }
        [UnityTest]public IEnumerator ContactEffectsHaveFixedCapacityExpireAndDisposeWithTheBall()
        {
            var effects=audio.Feedback;evidence.effectCapacity=effects.Capacity;Assert.AreEqual(6,effects.Capacity);Assert.AreEqual(7,effects.PresentationRoot.transform.childCount);
            for(int i=0;i<16;i++)
            {
                effects.Emit(new Vector3(0,.02f,0),Vector3.up,SurfaceAudioKind.Metal,9);yield return new WaitForSeconds(.06f);
                evidence.maximumActiveRings=Mathf.Max(evidence.maximumActiveRings,effects.ActiveRings);Assert.LessOrEqual(effects.ActiveRings,6);
            }
            Assert.AreEqual(7,effects.PresentationRoot.transform.childCount);Assert.AreEqual(1,effects.PresentationRoot.GetComponentsInChildren<ParticleSystem>().Length);
            yield return new WaitForSeconds(.4f);Assert.AreEqual(0,effects.ActiveRings);
            var owner=effects.PresentationRoot;var material=owner.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
            Object.Destroy(ball.gameObject);yield return null;yield return null;Assert.IsTrue(owner==null);Assert.IsTrue(material==null);evidence.visualOwnerDisposed=true;
        }
        [UnityTest]public IEnumerator FasterRollingChangesPresenceWithoutARevvingPitchSweep()
        {
            ball.Body.linearVelocity=Vector3.forward*2;ball.Body.angularVelocity=Vector3.right*4;
            for(int i=0;i<5;i++)yield return null;evidence.slowRollingPitch=audio.RollingPlaybackPitch;float slowLevel=audio.RollingLevel;
            ball.Body.linearVelocity=Vector3.forward*12;ball.Body.angularVelocity=Vector3.right*24;
            for(int i=0;i<5;i++)yield return null;evidence.fastRollingPitch=audio.RollingPlaybackPitch;
            Assert.Greater(audio.RollingLevel,slowLevel);Assert.Less(Mathf.Abs(evidence.fastRollingPitch-evidence.slowRollingPitch),.1f);
            Assert.That(evidence.fastRollingPitch,Is.InRange(.88f,1.14f));
        }
        [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;foreach(var item in owned)if(item)Object.Destroy(item);owned.Clear();yield return null;}
    }
}
