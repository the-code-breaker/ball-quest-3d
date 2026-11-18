using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class PhysicsLabTests
    {
        Scene original, loaded;
        PhysicsLabTelemetry lab;
        [UnitySetUp]
        public IEnumerator OpenLab()
        {
            original = SceneManager.GetActiveScene();
            yield return SceneManager.LoadSceneAsync("Assets/_BallQuest/Scenes/Gameplay/PhysicsLab.unity", LoadSceneMode.Additive);
            loaded = SceneManager.GetSceneByPath("Assets/_BallQuest/Scenes/Gameplay/PhysicsLab.unity");
            SceneManager.SetActiveScene(loaded);
            foreach (var root in loaded.GetRootGameObjects())
            {
                lab = root.GetComponentInChildren<PhysicsLabTelemetry>(); if (lab) break;
            }
            Assert.IsNotNull(lab); lab.recordCsv = false;
            yield return new WaitForFixedUpdate();
        }
        [UnityTest]
        public IEnumerator TwelveStationsHaveActualGeometryAndManualBallRestsOnFlatGround()
        {
            Assert.AreEqual(12, lab.stationSpawns.Length);
            Assert.AreEqual(12, lab.stationNames.Length);
            var ball = lab.ball; Assert.IsFalse(ball.ExternalControl); Assert.IsTrue(ball.active);
            yield return new WaitForSeconds(.6f);
            var start = ball.Body.position;
            yield return new WaitForSeconds(1);
            Assert.Less(Vector3.ProjectOnPlane(ball.Body.position - start, Vector3.up).magnitude, .03f, "Flat lab ball drifted without input.");
            Assert.IsTrue(ball.Grounded);
            foreach (Vector3 spawn in lab.stationSpawns)
                Assert.IsTrue(Physics.Raycast(spawn, Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "Station spawn has no physical support at " + spawn);
            Assert.IsNotNull(GameObject.Find("Inclined physical ramp"));
            Assert.IsNotNull(GameObject.Find("Physical oscillating support"));
        }
        [UnityTest]
        public IEnumerator StationResetClearsExternalControlAndRecordsActualBodyMotion()
        {
            var ball = lab.ball;
            lab.recordCsv = true;
            ball.SetExternalControl(true); ball.SubmitControl(new BallControlCommand(1, .2f));
            yield return new WaitForSeconds(.65f);
            Assert.Greater(lab.ContactSpeed, .2f);
            Assert.Greater(lab.AngularSpeed, .1f);
            Assert.Greater(lab.Command.y, .5f);
            float releaseSpeed = lab.ContactSpeed;
            ball.SubmitControl(default);
            yield return new WaitForSeconds(.1f);
            Assert.Greater(lab.ContactSpeed, releaseSpeed * .2f, "Release stopped the ball abruptly.");
            float coastDeadline = Time.time + 10;
            while (lab.ContactSpeed > .12f && Time.time < coastDeadline) yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Grounded, "Coast left the large flat testing arena.");
            Assert.Less(lab.ContactSpeed, .12f, "Rolling resistance failed to stop the ball.");
            Assert.Greater(lab.StoppingDistance, .15f, "Coasting distance was not measured.");
            lab.ExportCsv();
            var report = Path.GetFullPath("TestResults/physics-lab-manual.csv");
            Assert.IsTrue(File.Exists(report));
            File.Copy(report, Path.GetFullPath("TestResults/physics-lab-v10.csv"), true);
            lab.recordCsv = false;
            lab.SelectStation(11);
            Assert.AreEqual(11, lab.Station);
            Assert.IsFalse(ball.ExternalControl);
            Assert.Less(ball.Body.linearVelocity.magnitude, .001f);
            Assert.Less(ball.Body.angularVelocity.magnitude, .001f);
            Assert.Less(Vector3.Distance(lab.stationSpawns[11], ball.Body.position), .01f);
            yield return new WaitForSeconds(.5f);
            Assert.IsTrue(ball.Grounded);
        }
        [UnityTearDown]
        public IEnumerator CloseLab()
        {
            Time.timeScale = 1;
            if (lab) lab.recordCsv = false;
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }
}
