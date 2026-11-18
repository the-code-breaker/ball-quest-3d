using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class MaterialAudioTests
    {
        readonly List<Object> owned = new List<Object>();
        BallController ball;
        BallContactAudio contact;
        BallSurfaceTag surface;
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            if (!Object.FindFirstObjectByType<AudioListener>()) Keep(new GameObject("Contact audio test listener")).AddComponent<AudioListener>();
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(160, 1, 160);
            surface = floor.AddComponent<BallSurfaceTag>(); surface.surface = SurfaceAudioKind.Wood;
            var go = Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere)); go.transform.position = new Vector3(0, .6f, 0);
            go.AddComponent<Rigidbody>(); go.AddComponent<BallInput>(); ball = go.AddComponent<BallController>();
            var profile = Keep(ScriptableObject.CreateInstance<BallProfile>()); profile.bounce = 0;
            ball.ApplyProfile(profile); ball.active = true; ball.SetExternalControl(true);
            contact = go.AddComponent<BallContactAudio>(); contact.masterVolume = 1;
            contact.SetMaterial(BallAudioMaterial.Metal);
            for (int i = 0; i < 18; i++) yield return new WaitForFixedUpdate();
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; foreach (var value in owned) if (value) Object.Destroy(value); owned.Clear(); yield return null;
        }

        [UnityTest]
        public IEnumerator GroundContactSpeedDrivesLoopsAndAirborneAndStoppedBallsAreSilent()
        {
            Assert.IsTrue(ball.Grounded); Assert.Less(contact.RollingLevel, .001f);
            ball.SubmitControl(new BallControlCommand(.7f, 0));
            for (int i = 0; i < 75; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.Greater(contact.ContactSpeed, 2); Assert.Greater(contact.RollingLevel, .015f);
            Assert.Greater(contact.LoopStartCount, 0);
            ball.Respawn(new Vector3(0, 8, 0)); ball.SubmitControl(default);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsFalse(ball.Grounded); Assert.AreEqual(0, contact.RollingLevel); Assert.AreEqual(0, contact.SlidingLevel);
            ball.Respawn(new Vector3(0, .6f, 0));
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.Less(contact.RollingLevel, .001f);
        }

        [UnityTest]
        public IEnumerator SixMaterialProfilesAndEightSurfaceResponsesRemainDistinctWithoutRestartingEveryFrame()
        {
            var clips = new HashSet<AudioClip>();
            for (int i = 0; i < 6; i++)
            {
                var profile = TemporaryBallAudio.Profile((BallAudioMaterial)i);
                Assert.AreEqual((BallAudioMaterial)i, profile.material);
                Assert.IsTrue(clips.Add(profile.rolling)); Assert.IsNotNull(profile.sliding); Assert.IsNotNull(profile.impact);
                Assert.AreEqual(8, profile.surfaces.Length); Assert.IsTrue(profile.provenance.Contains("Natural contact v10"));Assert.AreEqual(10,profile.audioRevision);
            }
            ball.SubmitControl(new BallControlCommand(.65f, 0));
            for (int i = 0; i < 50; i++) yield return new WaitForFixedUpdate();
            yield return null;
            int starts = contact.LoopStartCount;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.LessOrEqual(contact.LoopStartCount - starts, 1, "A rolling source must not restart every render frame.");
            surface.surface = SurfaceAudioKind.Ice;
            yield return null; Assert.AreEqual(SurfaceAudioKind.Ice, contact.CurrentSurface);
            contact.SetMaterial(BallAudioMaterial.Glass);
            yield return null; Assert.AreEqual(BallAudioMaterial.Glass, contact.CurrentMaterial);
            surface.surface = SurfaceAudioKind.Sand;
            yield return null; Assert.AreEqual(SurfaceAudioKind.Sand, contact.CurrentSurface);
            Assert.AreNotEqual(contact.profile.Response(SurfaceAudioKind.Ice).pitch, contact.profile.Response(SurfaceAudioKind.Sand).pitch);
        }

        [UnityTest]
        public IEnumerator PhysicalImpactUsesBoundedPoolAndContactSlipHasSeparateFrictionLoop()
        {
            var wall = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.transform.position = new Vector3(4, 2, 0); wall.transform.localScale = new Vector3(.6f, 4, 15);
            int before = contact.PlayedImpactCount;
            ball.Body.linearVelocity = Vector3.right * 11; ball.Body.angularVelocity = Vector3.zero;
            yield return null;
            Assert.Greater(contact.ContactSlipSpeed, 1); Assert.Greater(contact.SlidingLevel, 0);
            for (int i = 0; i < 45; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(contact.PlayedImpactCount, before, "A real collision must dispatch the impulse audio event.");
            Assert.LessOrEqual(contact.ActiveImpactVoices, 4);
            // Isolate the burst limiter from fresh wall/floor contacts following the real impact above.
            ball.Body.isKinematic = true;
            yield return new WaitForSeconds(contact.profile.impactCooldown + .05f);
            int accepted = 0;
            for (int i = 0; i < 30; i++) if (contact.ReportImpact(8)) accepted++;
            Assert.AreEqual(1, accepted, "Cooldown limits a collision burst to one voice, rather than overlapping 30 sounds.");
            Assert.LessOrEqual(contact.ActiveImpactVoices, 4);
            contact.masterVolume = 0; yield return null;
            Assert.AreEqual(0, contact.RollingLevel); Assert.AreEqual(0, contact.SlidingLevel);
            Assert.AreEqual(0, contact.ActiveImpactVoices); Assert.IsFalse(contact.ReportImpact(8));
            contact.masterVolume = 1;
            yield return new WaitForSeconds(contact.profile.impactCooldown + .05f);
            Assert.IsTrue(contact.ReportImpact(8)); Assert.Greater(contact.ActiveImpactVoices, 0);
            Time.timeScale = 0; yield return null;
            Assert.AreEqual(0, contact.ActiveImpactVoices, "Pause must silence an existing transient voice, not only future collisions.");
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator AudioServiceCanRunInStandalonePhysicsLabWithoutBootstrap()
        {
            var serviceObject = Keep(new GameObject("Standalone audio service"));
            var audio = serviceObject.AddComponent<QuestAudio>();
            audio.Apply(new GameSettings { sfx = .35f, music = 0 }); audio.BindBall(ball);
            yield return null;
            Assert.AreSame(contact, audio.ContactAudio); Assert.AreEqual(.35f, contact.masterVolume, .001f);
            audio.BindBall(null); yield return null; Assert.IsFalse(contact.enabled);
        }
    }
}
