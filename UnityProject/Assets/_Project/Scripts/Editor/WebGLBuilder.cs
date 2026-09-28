using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

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
            // ビルドごとにファイル名を変える。同じ名前だと、ブラウザに残った古い .js と新しい .wasm が
            // 組み合わさって起動できなくなる(LinkError)
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            // 関数名は普段は入れない(wasm に埋め込むと約15MB増える)。クラッシュを調べるときだけ
            // BATTLEFIGHT_WEBGL_SYMBOLS=1 を付けてビルドする。
            // ※ External(別ファイル)は Unity 6000.3 のローダーで SYMBOLS_FILENAME エラーになり使えない
            bool withSymbols = Environment.GetEnvironmentVariable("BATTLEFIGHT_WEBGL_SYMBOLS") == "1";
            PlayerSettings.WebGL.debugSymbolMode = withSymbols ? WebGLDebugSymbolMode.Embedded : WebGLDebugSymbolMode.Off;
            PlayerSettings.productName = "BattleFight";
            // ウィンドウいっぱいに表示する専用テンプレート(Assets/WebGLTemplates/BattleFight)
            PlayerSettings.WebGL.template = "PROJECT:BattleFight";
            // メモリの拡張回数を減らすため、最初から多めに確保する
            PlayerSettings.WebGL.initialMemorySize = 256;
            // Unity のロゴ画面(約2.7MB)を出さない。Unity 6 では Personal でも切れる
            PlayerSettings.SplashScreen.show = false;
            // ロゴの表示もオフにしないと、ロゴのテクスチャがビルドに残る
            PlayerSettings.SplashScreen.showUnityLogo = false;
            UseLightweightQualityForWebGL();

            // ファイル名が毎回変わるので、前回の出力を消してから作る(古いファイルをデプロイしない)
            if (System.IO.Directory.Exists(OutputPath)) System.IO.Directory.Delete(OutputPath, true);

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

        const string SourceRenderPipeline = "Assets/Settings/PC_RPAsset.asset";
        const string SourceRenderer = "Assets/Settings/PC_Renderer.asset";
        const string WebGLRenderPipeline = "Assets/Settings/WebGL_RPAsset.asset";
        const string WebGLRenderer = "Assets/Settings/WebGL_Renderer.asset";
        const string WebGLQualityName = "WebGL";

        /// <summary>
        /// WebGL 専用の軽い画質を用意して、WebGL の既定にする。
        /// ・描画解像度は 100%(既定の「Mobile」は 80% でぼやける)
        /// ・SSAO なし、HDR なし、影はハード・1カスケード・1024・距離 35m、MSAA 2x
        /// PC 用の設定から複製して作るので、エディタや PC ビルドの見た目は変わらない。
        /// </summary>
        static void UseLightweightQualityForWebGL()
        {
            var renderer = CreateLightweightRenderer();
            var pipeline = CreateLightweightPipeline(renderer);
            if (pipeline == null) return;

            var qualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualityAsset == null || qualityAsset.Length == 0) return;
            var so = new SerializedObject(qualityAsset[0]);
            var levels = so.FindProperty("m_QualitySettings");

            int pcIndex = -1;
            int webglIndex = -1;
            for (int i = 0; i < levels.arraySize; i++)
            {
                string name = levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                if (name == "PC") pcIndex = i;
                if (name == WebGLQualityName) webglIndex = i;
            }
            if (pcIndex < 0) return;

            if (webglIndex < 0)
            {
                // PC の設定を末尾に複製して WebGL 用にする(既存の番号がずれないように末尾へ)
                levels.InsertArrayElementAtIndex(pcIndex);
                levels.MoveArrayElement(pcIndex + 1, levels.arraySize - 1);
                webglIndex = levels.arraySize - 1;
                levels.GetArrayElementAtIndex(webglIndex).FindPropertyRelative("name").stringValue = WebGLQualityName;
            }

            var level = levels.GetArrayElementAtIndex(webglIndex);
            level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = pipeline;

            // WebGL ビルドには WebGL 用の画質だけを入れる(PC / Mobile の URP 設定や SSAO のシェーダーを含めない)
            for (int i = 0; i < levels.arraySize; i++)
            {
                var excluded = levels.GetArrayElementAtIndex(i).FindPropertyRelative("excludedTargetPlatforms");
                if (excluded == null) continue;
                SetPlatformExcluded(excluded, "WebGL", i != webglIndex);
            }

            var defaults = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < defaults.arraySize; i++)
            {
                var pair = defaults.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue == "WebGL") pair.FindPropertyRelative("second").intValue = webglIndex;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static Object CreateLightweightRenderer()
        {
            if (AssetDatabase.LoadMainAssetAtPath(WebGLRenderer) == null) AssetDatabase.CopyAsset(SourceRenderer, WebGLRenderer);
            var renderer = AssetDatabase.LoadMainAssetAtPath(WebGLRenderer);
            if (renderer == null) return null;

            // レンダラー機能(PC 用の SSAO)を外す。WebGL では重いわりに、仮モデルでは効果が小さい
            var so = new SerializedObject(renderer);
            so.FindProperty("m_RendererFeatures").ClearArray();
            so.FindProperty("m_RendererFeatureMap").ClearArray();
            // ポストエフェクトは使わないので、フィルムグレインや SMAA のテクスチャ(約3MB)を含めない
            var postProcess = so.FindProperty("postProcessData");
            if (postProcess != null) postProcess.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(WebGLRenderer))
            {
                if (sub == null || sub == renderer) continue;
                AssetDatabase.RemoveObjectFromAsset(sub);
                Object.DestroyImmediate(sub, true);
            }
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        static Object CreateLightweightPipeline(Object renderer)
        {
            if (renderer == null) return null;
            if (AssetDatabase.LoadMainAssetAtPath(WebGLRenderPipeline) == null) AssetDatabase.CopyAsset(SourceRenderPipeline, WebGLRenderPipeline);
            var pipeline = AssetDatabase.LoadMainAssetAtPath(WebGLRenderPipeline);
            if (pipeline == null) return null;

            var so = new SerializedObject(pipeline);
            var renderers = so.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            SetInt(so, "m_DefaultRendererIndex", 0);
            SetFloat(so, "m_RenderScale", 1f);
            SetInt(so, "m_MSAA", 2);
            SetBool(so, "m_SupportsHDR", false);
            SetInt(so, "m_MainLightShadowmapResolution", 1024);
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            SetFloat(so, "m_ShadowDistance", 35f);
            SetInt(so, "m_ShadowCascadeCount", 1);
            SetBool(so, "m_SoftShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        static void SetPlatformExcluded(SerializedProperty excluded, string platform, bool exclude)
        {
            int found = -1;
            for (int i = 0; i < excluded.arraySize; i++)
            {
                if (excluded.GetArrayElementAtIndex(i).stringValue == platform) found = i;
            }
            if (exclude && found < 0)
            {
                excluded.arraySize++;
                excluded.GetArrayElementAtIndex(excluded.arraySize - 1).stringValue = platform;
            }
            else if (!exclude && found >= 0)
            {
                excluded.DeleteArrayElementAtIndex(found);
            }
        }

        static void SetInt(SerializedObject so, string name, int value)
        {
            var property = so.FindProperty(name);
            if (property != null) property.intValue = value;
            else Debug.LogWarning($"[BattleFight] URP の設定 {name} が見つかりません");
        }

        static void SetFloat(SerializedObject so, string name, float value)
        {
            var property = so.FindProperty(name);
            if (property != null) property.floatValue = value;
            else Debug.LogWarning($"[BattleFight] URP の設定 {name} が見つかりません");
        }

        static void SetBool(SerializedObject so, string name, bool value)
        {
            var property = so.FindProperty(name);
            if (property != null) property.boolValue = value;
            else Debug.LogWarning($"[BattleFight] URP の設定 {name} が見つかりません");
        }
    }
}
