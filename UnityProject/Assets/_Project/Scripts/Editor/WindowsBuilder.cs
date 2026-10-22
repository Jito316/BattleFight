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
        const string mOutputDirectory = "Builds/Windows";
        const string mExecutableName = "BattleFight.exe";
        const string mZipPath = "Builds/BattleFight-Windows.zip";

        [MenuItem("BattleFight/Build Windows")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try
            {
                EditorApplication.Exit(Build() ? 0 : 1);
            }
            catch (Exception aE)
            {
                Debug.LogException(aE);
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
            if (Directory.Exists(mOutputDirectory)) Directory.Delete(mOutputDirectory, true);

            var aReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrototypeSceneBuilder.ScenePath },
                locationPathName = Path.Combine(mOutputDirectory, mExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            var aSummary = aReport.summary;
            bool aSucceeded = aSummary.result == BuildResult.Succeeded;
            if (aSucceeded) CreateZip();

            Debug.Log($"[BattleFight] Windows ビルド: {aSummary.result} / {aSummary.totalSize / (1024f * 1024f):0.0} MB / {aSummary.totalTime}");
            return aSucceeded;
        }

        /// <summary>配布しやすいように zip にまとめる(デバッグ用の BurstDebugInformation などは除く)</summary>
        static void CreateZip()
        {
            if (File.Exists(mZipPath)) File.Delete(mZipPath);
            using var aZip = ZipFile.Open(mZipPath, ZipArchiveMode.Create);
            foreach (var aFile in Directory.GetFiles(mOutputDirectory, "*", SearchOption.AllDirectories))
            {
                string aRelative = Path.GetRelativePath(mOutputDirectory, aFile);
                if (aRelative.Contains("DoNotShip")) continue;
                aZip.CreateEntryFromFile(aFile, Path.Combine("BattleFight", aRelative), System.IO.Compression.CompressionLevel.Optimal);
            }
        }
    }
}
