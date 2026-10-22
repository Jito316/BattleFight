using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// UI(UI Toolkit)の準備。レイアウト(.uxml)と見た目(.uss)は Assets/_Project/UI に置き、ここでは
    /// ・日本語フォント(使う文字だけに絞った .otf)から UI Toolkit 用のフォントアセットを作る
    /// ・画面の拡大縮小(1920x1080 基準、高さに合わせる)を決める PanelSettings を作る
    /// ・画面ごとに UIDocument を置く
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const string mUiRoot = mRoot + "/UI";
        const string mFontAssetPath = mUiRoot + "/NotoSansJP-Bold.asset";
        const string mTextSettingsPath = mUiRoot + "/BattleFightText.asset";
        const string mPanelSettingsPath = mUiRoot + "/BattleFightPanel.asset";
        const string mThemePath = mUiRoot + "/BattleFightTheme.tss";

        static PanelSettings BuildPanelSettings()
        {
            var aFontAsset = EnsureFontAsset();

            var aTextSettings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(mTextSettingsPath);
            if (aTextSettings == null)
            {
                aTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(aTextSettings, mTextSettingsPath);
            }
            aTextSettings.defaultFontAsset = aFontAsset;
            EditorUtility.SetDirty(aTextSettings);

            var aPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(mPanelSettingsPath);
            if (aPanel == null)
            {
                aPanel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(aPanel, mPanelSettingsPath);
            }
            aPanel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(mThemePath);
            aPanel.textSettings = aTextSettings;
            // 1920x1080 を基準に、画面の高さに合わせて拡大縮小する(以前の HUD と同じ考え方)
            aPanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            aPanel.referenceResolution = new Vector2Int(1920, 1080);
            aPanel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            aPanel.match = 1f;
            aPanel.clearColor = false;
            EditorUtility.SetDirty(aPanel);
            AssetDatabase.SaveAssets();
            return aPanel;
        }

        /// <summary>
        /// .otf から UI Toolkit のフォントアセットを作る(動的に文字を足すモード)。
        /// USS(Common.uss)はこのアセットを参照するので、作ったあとで読み込み直す。
        /// </summary>
        static FontAsset EnsureFontAsset()
        {
            var aFontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(mFontAssetPath);
            if (aFontAsset == null)
            {
                var aFont = AssetDatabase.LoadAssetAtPath<Font>(mHudFontPath);
                aFontAsset = FontAsset.CreateFontAsset(aFont, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                aFontAsset.name = "NotoSansJP-Bold";
                AssetDatabase.CreateAsset(aFontAsset, mFontAssetPath);
                // アトラスとマテリアルはアセットの中に入れておかないと、シーンを保存したときに消える
                foreach (var aTexture in aFontAsset.atlasTextures)
                {
                    if (aTexture == null) continue;
                    aTexture.name = "NotoSansJP-Bold Atlas";
                    AssetDatabase.AddObjectToAsset(aTexture, aFontAsset);
                }
                if (aFontAsset.material != null)
                {
                    aFontAsset.material.name = "NotoSansJP-Bold Material";
                    AssetDatabase.AddObjectToAsset(aFontAsset.material, aFontAsset);
                }
                AssetDatabase.SaveAssets();
                foreach (var aUss in new[] { "Common.uss", "Hud.uss", "LoadoutEditor.uss", "StageSelect.uss" })
                {
                    AssetDatabase.ImportAsset($"{mUiRoot}/{aUss}", ImportAssetOptions.ForceUpdate);
                }
                AssetDatabase.ImportAsset(mThemePath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                // フォントを作り直す(文字を足す)と、字形の番号が変わる。前のフォントで作った字形を残さないよう毎回消す
                // (字形は実行時に必要な分だけ作られる)
                aFontAsset.ClearFontAssetData(true);
                EditorUtility.SetDirty(aFontAsset);
                AssetDatabase.SaveAssets();
            }
            return aFontAsset;
        }

        static T UiDocument<T>(string iObjectName, string iUxml, PanelSettings iPanel, int iSortingOrder) where T : MonoBehaviour
        {
            var aObject = new GameObject(iObjectName);
            var aDocument = aObject.AddComponent<UIDocument>();
            aDocument.panelSettings = iPanel;
            aDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{mUiRoot}/{iUxml}.uxml");
            aDocument.sortingOrder = iSortingOrder;
            return aObject.AddComponent<T>();
        }
    }
}
