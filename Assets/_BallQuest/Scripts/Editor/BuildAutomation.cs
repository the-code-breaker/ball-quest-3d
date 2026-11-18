using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BallQuest.Editor
{
    public static class BuildAutomation
    {
        [MenuItem("Tools/Ball Quest/Build Android")]
        public static void Android()
        {
            ProjectGenerator.GenerateProject();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) throw new InvalidOperationException("Install Android Build Support, SDK/NDK and OpenJDK through Unity Hub.");
            EditorUserBuildSettings.buildAppBundle = true;
            Build(BuildTarget.Android, "Builds/Android/BallQuest3D.aab", BuildOptions.None);
        }

        [MenuItem("Tools/Ball Quest/Build Android Development APK")]
        public static void AndroidDevelopment()
        {
            ProjectGenerator.GenerateProject(); EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/BallQuest3D-dev.apk", BuildOptions.Development);
        }

        [MenuItem("Tools/Ball Quest/Build Optimized Testing APK")]
        public static void AndroidTesting()
        {
            ProjectGenerator.GenerateProject();EditorUserBuildSettings.buildAppBundle=false;
            Build(BuildTarget.Android,"Builds/Android/BallQuest3D-test.apk",BuildOptions.None);
        }

        [MenuItem("Tools/Ball Quest/Export iOS")]
        public static void IOS()
        {
            ProjectGenerator.GenerateProject();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS)) throw new InvalidOperationException("Install iOS Build Support through Unity Hub. Xcode signing requires macOS.");
            Build(BuildTarget.iOS, "Builds/iOS", BuildOptions.None);
        }

        static void Build(BuildTarget target, string location, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(location));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectGenerator.ScenePath, PhysicsLabGenerator.ScenePath }, locationPathName = location, target = target, options = options
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + report.summary.result);
            Debug.Log("Build created: " + location + " (" + report.summary.totalSize + " bytes)");
        }
    }
}
