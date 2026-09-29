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
        const string UiRoot = Root + "/UI";
        const string FontAssetPath = UiRoot + "/NotoSansJP-Bold.asset";
        const string TextSettingsPath = UiRoot + "/BattleFightText.asset";
        const string PanelSettingsPath = UiRoot + "/BattleFightPanel.asset";
        const string ThemePath = UiRoot + "/BattleFightTheme.tss";

        static PanelSettings BuildPanelSettings()
        {
            var fontAsset = EnsureFontAsset();

            var textSettings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(TextSettingsPath);
            if (textSettings == null)
            {
                textSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(textSettings, TextSettingsPath);
            }
            textSettings.defaultFontAsset = fontAsset;
            EditorUtility.SetDirty(textSettings);

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.textSettings = textSettings;
            // 1920x1080 を基準に、画面の高さに合わせて拡大縮小する(以前の HUD と同じ考え方)
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 1f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }

        /// <summary>
        /// .otf から UI Toolkit のフォントアセットを作る(動的に文字を足すモード)。
        /// USS(Common.uss)はこのアセットを参照するので、作ったあとで読み込み直す。
        /// </summary>
        static FontAsset EnsureFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(HudFontPath);
                fontAsset = FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fontAsset.name = "NotoSansJP-Bold";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                // アトラスとマテリアルはアセットの中に入れておかないと、シーンを保存したときに消える
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture == null) continue;
                    texture.name = "NotoSansJP-Bold Atlas";
                    AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "NotoSansJP-Bold Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                AssetDatabase.SaveAssets();
                foreach (var uss in new[] { "Common.uss", "Hud.uss", "LoadoutEditor.uss", "StageSelect.uss" })
                {
                    AssetDatabase.ImportAsset($"{UiRoot}/{uss}", ImportAssetOptions.ForceUpdate);
                }
                AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceUpdate);
            }
            return fontAsset;
        }

        static T UiDocument<T>(string objectName, string uxml, PanelSettings panel, int sortingOrder) where T : MonoBehaviour
        {
            var go = new GameObject(objectName);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UiRoot}/{uxml}.uxml");
            document.sortingOrder = sortingOrder;
            return go.AddComponent<T>();
        }
    }
}
