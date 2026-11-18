using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallQuest
{
    /// <summary>Read-only instrumentation for the standalone twelve-station physics laboratory.</summary>
    public sealed class PhysicsLabTelemetry : MonoBehaviour
    {
        public BallController ball;
        public Vector3[] stationSpawns = Array.Empty<Vector3>();
        public string[] stationNames = Array.Empty<string>();
        public bool recordCsv;
        public TextMeshProUGUI readout;
        [Min(.02f)] public float sampleInterval = .1f;
        public int Station { get; private set; }
        public float StoppingDistance { get; private set; }
        public float ContactSpeed { get; private set; }
        public float AngularSpeed { get; private set; }
        public float RollingRatio { get; private set; }
        public Vector2 Command { get; private set; }
        readonly List<string> samples = new List<string>(4096);
        bool wasDriven, trackingStop;
        Vector3 lastPosition;
        float elapsed, nextSample, nextReadout;
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        void Start()
        {
            samples.Add("seconds,station,x,y,z,vx,vy,vz,angular_speed,grounded,input_x,input_y,contact_speed,rolling_ratio,stopping_distance");
            if (ball) lastPosition = ball.Body.position;
        }
        void FixedUpdate()
        {
            if (!ball || !ball.Body) return;
            float dt = Time.fixedDeltaTime;
            elapsed += dt;
            Command = ball.AppliedControl;
            Vector3 relative = ball.Body.linearVelocity - ball.SupportVelocity;
            Vector3 tangent = Vector3.ProjectOnPlane(relative, ball.GroundNormal);
            ContactSpeed = tangent.magnitude;
            AngularSpeed = ball.Body.angularVelocity.magnitude;
            float radius = ball.GroundDetector ? ball.GroundDetector.Radius : .5f;
            RollingRatio = ContactSpeed > .1f ? AngularSpeed * radius / ContactSpeed : 0;
            bool driven = Command.sqrMagnitude > .001f;
            if (wasDriven && !driven) { trackingStop = true; StoppingDistance = 0; }
            if (driven) trackingStop = false;
            if (trackingStop)
            {
                StoppingDistance += Vector3.Distance(lastPosition, ball.Body.position);
                if (ContactSpeed < .035f) trackingStop = false;
            }
            wasDriven = driven;
            lastPosition = ball.Body.position;
            if (readout && elapsed >= nextReadout)
            {
                nextReadout = elapsed + sampleInterval;
                readout.text = (stationNames.Length > Station ? stationNames[Station] : "Physics laboratory") + "\n" +
                    "Speed " + ContactSpeed.ToString("0.00") + " m/s   Spin " + AngularSpeed.ToString("0.00") + " rad/s\n" +
                    (ball.Grounded ? "GROUNDED" : "AIRBORNE") + "   Input " + Command.ToString("0.00") + "\n" +
                    "Rolling ratio " + RollingRatio.ToString("0.00") + "   Coast distance " + StoppingDistance.ToString("0.00") + " m";
            }
            if (!recordCsv || elapsed < nextSample) return;
            nextSample = elapsed + sampleInterval;
            var p = ball.Body.position; var v = ball.Body.linearVelocity;
            samples.Add(string.Join(",", F(elapsed), Station, F(p.x), F(p.y), F(p.z), F(v.x), F(v.y), F(v.z), F(AngularSpeed), ball.Grounded ? 1 : 0, F(Command.x), F(Command.y), F(ContactSpeed), F(RollingRatio), F(StoppingDistance)));
        }
        static string F(float value) => value.ToString("0.0000", Invariant);
        public void SelectStation(int index)
        {
            if (!ball || stationSpawns.Length == 0) return;
            Station = Mathf.Clamp(index, 0, stationSpawns.Length - 1);
            ball.SetExternalControl(false);
            ball.ResetGravity();
            ball.Respawn(stationSpawns[Station]);
            ball.active = true; ball.Input.active = true;
            var camera = FindFirstObjectByType<QuestCamera>(); if (camera) camera.Snap();
            trackingStop = false; wasDriven = false; StoppingDistance = 0; lastPosition = ball.Body.position;
        }
        public void ExportCsv()
        {
            if (samples.Count < 2) return;
            string directory = Application.isEditor ? Path.Combine(Directory.GetCurrentDirectory(), "TestResults") : Application.persistentDataPath;
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, "physics-lab-manual.csv"), samples);
        }
        public static void ReturnToGame() { Time.timeScale = 1; SceneManager.LoadScene("BallQuest"); }
        public void GoBack() => ReturnToGame();
        void OnDestroy() { if (recordCsv) ExportCsv(); }
    }
}
