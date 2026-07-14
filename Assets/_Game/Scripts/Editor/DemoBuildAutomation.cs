using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GenericGachaRPG.Editor
{
    public static class DemoBuildAutomation
    {
        public const string BuildPassMarker = "[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]";
        public const string OutputPath = "Builds/Windows/GenericGachaRPGDemo.exe";

        [MenuItem("Tools/Generic Gacha RPG/Build Windows Demo _F6", priority = 20)]
        public static void BuildWindowsDemo()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode and wait for Unity to finish importing before building.");
            }

            DemoSceneGenerator.GenerateOrRepairDemo();

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DemoSceneGenerator.DatabasePath);
            DemoProjectVerifier.Verify(database);

            string absoluteOutput = Path.GetFullPath(OutputPath);
            string outputDirectory = Path.GetDirectoryName(absoluteOutput);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new InvalidOperationException("Could not resolve the Windows build output directory.");
            }

            Directory.CreateDirectory(outputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { DemoSceneGenerator.ScenePath },
                locationPathName = absoluteOutput,
                target = BuildTarget.StandaloneWindows64,
                // The workspace was renamed from Game-jjk to BubbleMind. Let Unity
                // rebuild Bee's path-sensitive cache through the supported API.
                options = BuildOptions.CleanBuildCache
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows build failed: {summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}.");
            }

            Debug.Log(
                $"{BuildPassMarker} {OutputPath} ({summary.totalSize:N0} bytes, {summary.totalTime.TotalSeconds:0.0}s)." );
        }
    }
}
