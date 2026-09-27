using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 試作アリーナの WebGL ビルド。出力先は UnityProject/Builds/WebGL(Firebase Hosting の公開ディレクトリ)。
    /// メニュー: BattleFight/Build WebGL
    /// バッチ: -buildTarget WebGL -executeMethod BattleFight.EditorTools.WebGLBuilder.BuildFromCommandLine
    /// </summary>
    public static class WebGLBuilder
    {
        const string OutputPath = "Builds/WebGL";

        [MenuItem("BattleFight/Build WebGL")]
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
            // Firebase Hosting が配信時に圧縮するので、Unity 側では圧縮しない
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.productName = "BattleFight";

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrototypeSceneBuilder.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            Debug.Log($"[BattleFight] WebGL ビルド: {summary.result} / {summary.totalSize / (1024f * 1024f):0.0} MB / {summary.totalTime}");
            return summary.result == BuildResult.Succeeded;
        }
    }
}
