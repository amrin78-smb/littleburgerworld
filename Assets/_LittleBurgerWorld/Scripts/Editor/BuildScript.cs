using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>Player builds. Android APK goes to Builds/Android/; a Linux desktop build is used for automated play-tests.</summary>
    public static class BuildScript
    {
        public const string AndroidPath = "Builds/Android/LittleBurgerWorld.apk";
        public const string LinuxPath = "Builds/Linux/LittleBurgerWorld.x86_64";

        private static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem(LittleBurgerWorldSetup.MenuRoot + "Build Android APK", priority = 30)]
        public static void BuildAndroidMenu() => BuildAndroid();

        public static BuildReport BuildAndroid()
        {
            LittleBurgerWorldSetup.ConfigurePlayerSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            Directory.CreateDirectory(Path.GetDirectoryName(AndroidPath));
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = AndroidPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Log(report);
            return report;
        }

        public static void BuildAndroidBatch()
        {
            BuildReport r = BuildAndroid();
            if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        [MenuItem(LittleBurgerWorldSetup.MenuRoot + "Build Linux Test Player", priority = 31)]
        public static BuildReport BuildLinux()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.GetDirectoryName(LinuxPath));
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = LinuxPath,
                target = BuildTarget.StandaloneLinux64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Log(report);
            return report;
        }

        public static void BuildLinuxBatch()
        {
            BuildReport r = BuildLinux();
            if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        private static void Log(BuildReport report)
        {
            BuildSummary s = report.summary;
            Debug.Log($"[LBW Build] {s.platform} result={s.result} size={s.totalSize / (1024f * 1024f):0.0} MB " +
                      $"time={s.totalTime.TotalSeconds:0}s errors={s.totalErrors} warnings={s.totalWarnings} output={s.outputPath}");
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage m in step.messages)
                {
                    if (m.type == LogType.Error || m.type == LogType.Exception) Debug.LogError("[LBW Build] " + m.content);
                }
            }
        }
    }
}
