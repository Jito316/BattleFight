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
            // クラッシュ時のスタックトレースに関数名が出るようにする(原因調査用)
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Embedded;
            PlayerSettings.productName = "BattleFight";
            // ウィンドウいっぱいに表示する専用テンプレート(Assets/WebGLTemplates/BattleFight)
            PlayerSettings.WebGL.template = "PROJECT:BattleFight";
            // メモリの拡張回数を減らすため、最初から多めに確保する
            PlayerSettings.WebGL.initialMemorySize = 256;
            UseDesktopQualityForWebGL();

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

        /// <summary>
        /// WebGL の既定の画質を「PC」にする。既定の「Mobile」は描画解像度が 80% なので、ブラウザでぼやけて見える。
        /// </summary>
        static void UseDesktopQualityForWebGL()
        {
            var qualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualityAsset == null || qualityAsset.Length == 0) return;

            var so = new SerializedObject(qualityAsset[0]);
            var levels = so.FindProperty("m_QualitySettings");
            int pcIndex = -1;
            for (int i = 0; i < levels.arraySize; i++)
            {
                if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "PC") pcIndex = i;
            }
            if (pcIndex < 0) return;

            var defaults = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < defaults.arraySize; i++)
            {
                var pair = defaults.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue == "WebGL")
                {
                    pair.FindPropertyRelative("second").intValue = pcIndex;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
