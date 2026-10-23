using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>ステージごとの見た目(色・光・霧)と、アリーナの遠景・色の帯。シーンを作るたびに上書きする</summary>
    public static partial class PrototypeSceneBuilder
    {
        static StageTheme Theme(Color iFloor, Color iWall, Color iPillar, Color iPlatform, Color iAccent, Color iBackdrop,
            Color iSky, Color iFog, float iFogStart, float iFogEnd, Color iLight, float iIntensity, Vector2 iAngle,
            Color iAmbientSky, Color iAmbientEquator, Color iAmbientGround, bool iTintArena = true, float iReflection = 0.6f) => new StageTheme
        {
            Reflection = iReflection,
            FloorColor = iFloor, WallColor = iWall, PillarColor = iPillar, PlatformColor = iPlatform, AccentColor = iAccent,
            BackdropColor = iBackdrop, SkyColor = iSky, FogColor = iFog, FogStart = iFogStart, FogEnd = iFogEnd,
            LightColor = iLight, LightIntensity = iIntensity, LightAngle = iAngle,
            AmbientSky = iAmbientSky, AmbientEquator = iAmbientEquator, AmbientGround = iAmbientGround, TintArena = iTintArena,
        };

        static Color Hex(string iHex) => ColorUtility.TryParseHtmlString(iHex, out var aColor) ? aColor : Color.magenta;

        static readonly Dictionary<string, StageTheme> mStageThemes = new Dictionary<string, StageTheme>
        {
            // 訓練場: 明るく、すっきりした青灰色
            ["Stage_0_Training"] = Theme(Hex("#5A6575"), Hex("#3C4656"), Hex("#6E7887"), Hex("#56708A"), Hex("#5FD4FF"), Hex("#44526A"),
                Hex("#9CC4E8"), Hex("#A8C6E2"), 45f, 150f, Hex("#F4F8FF"), 1.35f, new Vector2(52f, -30f),
                Hex("#A6B8D0"), Hex("#6D7888"), Hex("#33383F")),
            // 第1幕 鉄獣: 錆びた鉄と、オレンジの夕日
            ["Stage_1_Beast"] = Theme(Hex("#5B4A3E"), Hex("#3F2F27"), Hex("#7A5A43"), Hex("#6B5240"), Hex("#FF8A3D"), Hex("#4A3226"),
                Hex("#D9925A"), Hex("#C98A5C"), 35f, 130f, Hex("#FFC48A"), 1.4f, new Vector2(28f, -60f),
                Hex("#D9A27A"), Hex("#7A5A45"), Hex("#3A2A22"), iReflection: 0.4f),
            // 第2幕 空と銃火: 砂色の床と、高い青空
            ["Stage_2_Skyfire"] = Theme(Hex("#B9A27E"), Hex("#8C7756"), Hex("#C9B28C"), Hex("#A58E6A"), Hex("#4FB6FF"), Hex("#7F93AE"),
                Hex("#6FB2F0"), Hex("#A9CDEE"), 50f, 170f, Hex("#FFF6E2"), 1.55f, new Vector2(65f, -20f),
                Hex("#9FC8F2"), Hex("#B8A586"), Hex("#5C4E3B")),
            // 第3幕 爆炎の回廊: 炭色の床、赤い光と赤い霧
            ["Stage_3_Inferno"] = Theme(Hex("#2E2624"), Hex("#231B1A"), Hex("#3D2C28"), Hex("#4A2C24"), Hex("#FF4A1F"), Hex("#3A1612"),
                Hex("#4A1610"), Hex("#6E2216"), 22f, 100f, Hex("#FF8A5C"), 1.2f, new Vector2(35f, 40f),
                Hex("#B0463A"), Hex("#5A2420"), Hex("#1E0E0C"), iReflection: 0.2f),
            // 第4幕 影の剣聖: 夜。青い月光と紺色の霧
            ["Stage_4_Shadow"] = Theme(Hex("#262C3D"), Hex("#191D2B"), Hex("#30374D"), Hex("#2A3350"), Hex("#9C7BFF"), Hex("#151A2C"),
                Hex("#0D1222"), Hex("#1A2140"), 20f, 95f, Hex("#AFC6FF"), 0.9f, new Vector2(60f, 150f),
                Hex("#4A5A8C"), Hex("#2A3352"), Hex("#0D1018"), iReflection: 0.1f),
            // 第5幕 写し身: 白い床と、シアン・マゼンタの光
            ["Stage_5_Mirror"] = Theme(Hex("#D8DCE6"), Hex("#A9AFC2"), Hex("#E6E9F2"), Hex("#C3C9DA"), Hex("#FF4FD8"), Hex("#B9A7D8"),
                Hex("#E3D9F5"), Hex("#E8DFF7"), 40f, 150f, Hex("#E9F6FF"), 1.3f, new Vector2(70f, 10f),
                Hex("#8EE6F2"), Hex("#D9B8E8"), Hex("#6E6A85")),
            // エンドレス: 紫と赤の黄昏
            ["Stage_9_Endless"] = Theme(Hex("#3A2A3E"), Hex("#2A1C30"), Hex("#4C3352"), Hex("#4A2E4A"), Hex("#FF3D6E"), Hex("#2C1530"),
                Hex("#5C2448"), Hex("#6A2C52"), 25f, 115f, Hex("#FFA0C8"), 1.15f, new Vector2(18f, -100f),
                Hex("#B0507A"), Hex("#5A2C50"), Hex("#1E0E1E"), iReflection: 0.25f),
            // 探索 古城: 床や壁は探索のマップの色のまま。光と霧だけ、緑がかった灰色に
            ["Stage_10_Explore"] = Theme(Hex("#5A6575"), Hex("#3C4656"), Hex("#6E7887"), Hex("#56708A"), Hex("#9CE0A8"), Hex("#3A4A40"),
                Hex("#7E9488"), Hex("#8AA092"), 25f, 120f, Hex("#E8F2E0"), 1.2f, new Vector2(45f, -45f),
                Hex("#8FA698"), Hex("#5C6A60"), Hex("#262E28"), false, 0.35f),
        };

        /// <summary>表の見た目をステージに書き込む</summary>
        static void EnsureStageThemes(IEnumerable<StageData> iStages)
        {
            foreach (var aStage in iStages)
            {
                if (aStage == null || !mStageThemes.TryGetValue(aStage.name, out var aTheme)) continue;
                aStage.Theme = aTheme;
                EditorUtility.SetDirty(aStage);
            }
        }

        /// <summary>
        /// アリーナの遠景(外に並ぶ大きなシルエット)と、壁の上の色の帯。色はステージの見た目で変わる
        /// </summary>
        /// <summary>床・壁などのつやを抑える(シーンを作るたびに当てる)</summary>
        static void EnsureMatteArena()
        {
            foreach (var aName in new[] { "Floor", "Wall", "Pillar", "Platform", "Backdrop" })
            {
                var aMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{mRoot}/Materials/M_{aName}.mat");
                if (aMaterial == null || !aMaterial.HasProperty("_Smoothness")) continue;
                aMaterial.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(aMaterial);
            }
        }

        static void BuildArenaDecor(Transform iArena)
        {
            var aBackdropMat = Mat("Backdrop", new Color(0.25f, 0.28f, 0.34f));
            var aAccentMat = Mat("Accent", new Color(0.4f, 0.75f, 1f));

            // 壁の上の帯(当たり判定なし)
            EnsureMatteArena();
            Part(PrimitiveType.Cube, "Accent_N", iArena, new Vector3(0f, 4.1f, 29.6f), new Vector3(60f, 0.2f, 0.3f), aAccentMat);
            Part(PrimitiveType.Cube, "Accent_S", iArena, new Vector3(0f, 4.1f, -29.6f), new Vector3(60f, 0.2f, 0.3f), aAccentMat);
            Part(PrimitiveType.Cube, "Accent_E", iArena, new Vector3(29.6f, 4.1f, 0f), new Vector3(0.3f, 0.2f, 60f), aAccentMat);
            Part(PrimitiveType.Cube, "Accent_W", iArena, new Vector3(-29.6f, 4.1f, 0f), new Vector3(0.3f, 0.2f, 60f), aAccentMat);
            // 柱の上の帯
            foreach (var aPillar in new[] { new Vector3(-12f, 6.05f, 8f), new Vector3(12f, 6.05f, 8f), new Vector3(-12f, 6.05f, -8f), new Vector3(12f, 6.05f, -8f) })
            {
                Part(PrimitiveType.Cube, $"Accent_Pillar_{aPillar.x}_{aPillar.z}", iArena, aPillar, new Vector3(2.1f, 0.15f, 2.1f), aAccentMat);
            }

            // 遠景: アリーナを囲んで、高さの違う塔・岩のシルエットを並べる(決まった並びにするため乱数の種を固定)
            var aRandom = new System.Random(7);
            const int aCount = 28;
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                float aAngle = 360f * aIndex / aCount + (float)aRandom.NextDouble() * 8f;
                float aDistance = 52f + (float)aRandom.NextDouble() * 30f;
                float aHeight = 8f + (float)aRandom.NextDouble() * 26f;
                float aWidth = 6f + (float)aRandom.NextDouble() * 10f;
                var aPosition = Quaternion.Euler(0f, aAngle, 0f) * Vector3.forward * aDistance;
                var aPart = Part(PrimitiveType.Cube, $"Backdrop_{aIndex + 1}", iArena, new Vector3(aPosition.x, aHeight * 0.5f - 1f, aPosition.z),
                    new Vector3(aWidth, aHeight, aWidth * 0.8f), aBackdropMat);
                aPart.rotation = Quaternion.Euler(0f, aAngle + (float)aRandom.NextDouble() * 30f, 0f);
                aPart.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
