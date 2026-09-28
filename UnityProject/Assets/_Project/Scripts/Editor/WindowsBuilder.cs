using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 試作アリーナの Windows ビルド。出力先は UnityProject/Builds/Windows、配布用の zip も作る。
    /// メニュー: BattleFight/Build Windows
    /// バッチ: -buildTarget Win64 -executeMethod BattleFight.EditorTools.WindowsBuilder.BuildFromCommandLine
    /// </summary>
    public static class WindowsBuilder
    {
        const string OutputDirectory = "Builds/Windows";
        const string ExecutableName = "BattleFight.exe";
        const string ZipPath = "Builds/BattleFight-Windows.zip";

        [MenuItem("BattleFight/Build Windows")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try
            {
                EditorApplication.Exit(Build() ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static bool Build()
        {
            PlayerSettings.productName = "BattleFight";
            // Mono: IL2CPP より速くビルドでき、Visual Studio の C++ ツールもいらない
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            // 前回の出力を消してから作る(古いファイルを配布しない)
            if (Directory.Exists(OutputDirectory)) Directory.Delete(OutputDirectory, true);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrototypeSceneBuilder.ScenePath },
                locationPathName = Path.Combine(OutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            bool succeeded = summary.result == BuildResult.Succeeded;
            if (succeeded) CreateZip();

            Debug.Log($"[BattleFight] Windows ビルド: {summary.result} / {summary.totalSize / (1024f * 1024f):0.0} MB / {summary.totalTime}");
            return succeeded;
        }

        /// <summary>配布しやすいように zip にまとめる(デバッグ用の BurstDebugInformation などは除く)</summary>
        static void CreateZip()
        {
            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            using var zip = ZipFile.Open(ZipPath, ZipArchiveMode.Create);
            foreach (var file in Directory.GetFiles(OutputDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(OutputDirectory, file);
                if (relative.Contains("DoNotShip")) continue;
                zip.CreateEntryFromFile(file, Path.Combine("BattleFight", relative), System.IO.Compression.CompressionLevel.Optimal);
            }
        }
    }
}
