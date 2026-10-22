using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
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
        const string mOutputPath = "Builds/WebGL";

        [MenuItem("BattleFight/Build WebGL")]
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
            // Firebase Hosting が配信時に圧縮するので、Unity 側では圧縮しない
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            // ビルドごとにファイル名を変える。同じ名前だと、ブラウザに残った古い .js と新しい .wasm が
            // 組み合わさって起動できなくなる(LinkError)
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            // 関数名は普段は入れない(wasm に埋め込むと約15MB増える)。クラッシュを調べるときだけ
            // BATTLEFIGHT_WEBGL_SYMBOLS=1 を付けてビルドする。
            // ※ External(別ファイル)は Unity 6000.3 のローダーで SYMBOLS_FILENAME エラーになり使えない
            bool aWithSymbols = Environment.GetEnvironmentVariable("BATTLEFIGHT_WEBGL_SYMBOLS") == "1";
            PlayerSettings.WebGL.debugSymbolMode = aWithSymbols ? WebGLDebugSymbolMode.Embedded : WebGLDebugSymbolMode.Off;
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
            OptimizeCodeForSize();

            // ファイル名が毎回変わるので、前回の出力を消してから作る(古いファイルをデプロイしない)
            if (System.IO.Directory.Exists(mOutputPath)) System.IO.Directory.Delete(mOutputPath, true);

            var aReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrototypeSceneBuilder.ScenePath },
                locationPathName = mOutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            var aSummary = aReport.summary;
            Debug.Log($"[BattleFight] WebGL ビルド: {aSummary.result} / {aSummary.totalSize / (1024f * 1024f):0.0} MB / {aSummary.totalTime}");
            return aSummary.result == BuildResult.Succeeded;
        }

        /// <summary>
        /// wasm(エンジンのコード)を小さくする。
        /// ・Managed Stripping: High … 使っていない .NET / Unity のコードを強めに削る
        ///   (自分たちのコードは link.xml で丸ごと残す。リフレクションで使う型が消えないように)
        /// ・IL2CPP: OptimizeSize … 生成する C++ を容量優先にする
        /// ・Wasm: DiskSizeLTO … 容量優先の最適化 + リンク時最適化(ビルドは少し遅くなる)
        /// </summary>
        static void OptimizeCodeForSize()
        {
            var aTarget = NamedBuildTarget.WebGL;
            PlayerSettings.SetManagedStrippingLevel(aTarget, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(aTarget, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;

            // WebGL モジュールの設定は、モジュールが入っていないとコンパイルできないのでリフレクションで触る
            var aSettings = Type.GetType("UnityEditor.WebGL.UserBuildSettings, UnityEditor.WebGL.Extensions");
            var aProperty = aSettings?.GetProperty("codeOptimization", BindingFlags.Public | BindingFlags.Static);
            if (aProperty == null)
            {
                Debug.LogWarning("[BattleFight] WebGL のコード最適化の設定が見つかりません(既定のまま)");
                return;
            }
            aProperty.SetValue(null, Enum.Parse(aProperty.PropertyType, "DiskSizeLTO"));
        }

        const string mSourceRenderPipeline = "Assets/Settings/PC_RPAsset.asset";
        const string mSourceRenderer = "Assets/Settings/PC_Renderer.asset";
        const string mWebGLRenderPipeline = "Assets/Settings/WebGL_RPAsset.asset";
        const string mWebGLRenderer = "Assets/Settings/WebGL_Renderer.asset";
        const string mWebGLQualityName = "WebGL";

        /// <summary>
        /// WebGL 専用の軽い画質を用意して、WebGL の既定にする。
        /// ・描画解像度は 100%(既定の「Mobile」は 80% でぼやける)
        /// ・SSAO なし、HDR なし、影はハード・1カスケード・1024・距離 35m、MSAA 2x
        /// PC 用の設定から複製して作るので、エディタや PC ビルドの見た目は変わらない。
        /// </summary>
        static void UseLightweightQualityForWebGL()
        {
            var aRenderer = CreateLightweightRenderer();
            var aPipeline = CreateLightweightPipeline(aRenderer);
            if (aPipeline == null) return;

            var aQualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (aQualityAsset == null || aQualityAsset.Length == 0) return;
            var aSo = new SerializedObject(aQualityAsset[0]);
            var aLevels = aSo.FindProperty("m_QualitySettings");

            int aPcIndex = -1;
            int aWebglIndex = -1;
            for (int aI = 0; aI < aLevels.arraySize; aI++)
            {
                string aName = aLevels.GetArrayElementAtIndex(aI).FindPropertyRelative("name").stringValue;
                if (aName == "PC") aPcIndex = aI;
                if (aName == mWebGLQualityName) aWebglIndex = aI;
            }
            if (aPcIndex < 0) return;

            if (aWebglIndex < 0)
            {
                // PC の設定を末尾に複製して WebGL 用にする(既存の番号がずれないように末尾へ)
                aLevels.InsertArrayElementAtIndex(aPcIndex);
                aLevels.MoveArrayElement(aPcIndex + 1, aLevels.arraySize - 1);
                aWebglIndex = aLevels.arraySize - 1;
                aLevels.GetArrayElementAtIndex(aWebglIndex).FindPropertyRelative("name").stringValue = mWebGLQualityName;
            }

            var aLevel = aLevels.GetArrayElementAtIndex(aWebglIndex);
            aLevel.FindPropertyRelative("customRenderPipeline").objectReferenceValue = aPipeline;

            // WebGL ビルドには WebGL 用の画質だけを入れる(PC / Mobile の URP 設定や SSAO のシェーダーを含めない)
            for (int aI = 0; aI < aLevels.arraySize; aI++)
            {
                var aExcluded = aLevels.GetArrayElementAtIndex(aI).FindPropertyRelative("excludedTargetPlatforms");
                if (aExcluded == null) continue;
                SetPlatformExcluded(aExcluded, "WebGL", aI != aWebglIndex);
            }

            var aDefaults = aSo.FindProperty("m_PerPlatformDefaultQuality");
            for (int aI = 0; aI < aDefaults.arraySize; aI++)
            {
                var aPair = aDefaults.GetArrayElementAtIndex(aI);
                if (aPair.FindPropertyRelative("first").stringValue == "WebGL") aPair.FindPropertyRelative("second").intValue = aWebglIndex;
            }
            aSo.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static Object CreateLightweightRenderer()
        {
            if (AssetDatabase.LoadMainAssetAtPath(mWebGLRenderer) == null) AssetDatabase.CopyAsset(mSourceRenderer, mWebGLRenderer);
            var aRenderer = AssetDatabase.LoadMainAssetAtPath(mWebGLRenderer);
            if (aRenderer == null) return null;

            // レンダラー機能(PC 用の SSAO)を外す。WebGL では重いわりに、仮モデルでは効果が小さい
            var aSo = new SerializedObject(aRenderer);
            aSo.FindProperty("m_RendererFeatures").ClearArray();
            aSo.FindProperty("m_RendererFeatureMap").ClearArray();
            // ポストエフェクトは使わないので、フィルムグレインや SMAA のテクスチャ(約3MB)を含めない
            var aPostProcess = aSo.FindProperty("postProcessData");
            if (aPostProcess != null) aPostProcess.objectReferenceValue = null;
            aSo.ApplyModifiedPropertiesWithoutUndo();
            foreach (var aSub in AssetDatabase.LoadAllAssetsAtPath(mWebGLRenderer))
            {
                if (aSub == null || aSub == aRenderer) continue;
                AssetDatabase.RemoveObjectFromAsset(aSub);
                Object.DestroyImmediate(aSub, true);
            }
            EditorUtility.SetDirty(aRenderer);
            return aRenderer;
        }

        static Object CreateLightweightPipeline(Object iRenderer)
        {
            if (iRenderer == null) return null;
            if (AssetDatabase.LoadMainAssetAtPath(mWebGLRenderPipeline) == null) AssetDatabase.CopyAsset(mSourceRenderPipeline, mWebGLRenderPipeline);
            var aPipeline = AssetDatabase.LoadMainAssetAtPath(mWebGLRenderPipeline);
            if (aPipeline == null) return null;

            var aSo = new SerializedObject(aPipeline);
            var aRenderers = aSo.FindProperty("m_RendererDataList");
            aRenderers.arraySize = 1;
            aRenderers.GetArrayElementAtIndex(0).objectReferenceValue = iRenderer;
            SetInt(aSo, "m_DefaultRendererIndex", 0);
            SetFloat(aSo, "m_RenderScale", 1f);
            SetInt(aSo, "m_MSAA", 2);
            SetBool(aSo, "m_SupportsHDR", false);
            SetInt(aSo, "m_MainLightShadowmapResolution", 1024);
            SetBool(aSo, "m_AdditionalLightShadowsSupported", false);
            SetFloat(aSo, "m_ShadowDistance", 35f);
            SetInt(aSo, "m_ShadowCascadeCount", 1);
            SetBool(aSo, "m_SoftShadowsSupported", false);
            aSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(aPipeline);
            return aPipeline;
        }

        static void SetPlatformExcluded(SerializedProperty iExcluded, string iPlatform, bool iExclude)
        {
            int aFound = -1;
            for (int aI = 0; aI < iExcluded.arraySize; aI++)
            {
                if (iExcluded.GetArrayElementAtIndex(aI).stringValue == iPlatform) aFound = aI;
            }
            if (iExclude && aFound < 0)
            {
                iExcluded.arraySize++;
                iExcluded.GetArrayElementAtIndex(iExcluded.arraySize - 1).stringValue = iPlatform;
            }
            else if (!iExclude && aFound >= 0)
            {
                iExcluded.DeleteArrayElementAtIndex(aFound);
            }
        }

        static void SetInt(SerializedObject iSo, string iName, int iValue)
        {
            var aProperty = iSo.FindProperty(iName);
            if (aProperty != null) aProperty.intValue = iValue;
            else Debug.LogWarning($"[BattleFight] URP の設定 {iName} が見つかりません");
        }

        static void SetFloat(SerializedObject iSo, string iName, float iValue)
        {
            var aProperty = iSo.FindProperty(iName);
            if (aProperty != null) aProperty.floatValue = iValue;
            else Debug.LogWarning($"[BattleFight] URP の設定 {iName} が見つかりません");
        }

        static void SetBool(SerializedObject iSo, string iName, bool iValue)
        {
            var aProperty = iSo.FindProperty(iName);
            if (aProperty != null) aProperty.boolValue = iValue;
            else Debug.LogWarning($"[BattleFight] URP の設定 {iName} が見つかりません");
        }
    }
}
