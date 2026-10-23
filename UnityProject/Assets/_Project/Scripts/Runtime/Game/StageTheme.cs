using System;
using UnityEngine;

namespace BattleFight
{
    /// <summary>ステージの見た目(色・光・霧)。ステージを始めるときに StageThemeApplier が当てる</summary>
    [Serializable]
    public class StageTheme
    {
        public Color FloorColor = new Color(0.32f, 0.34f, 0.38f);
        public Color WallColor = new Color(0.22f, 0.23f, 0.27f);
        public Color PillarColor = new Color(0.4f, 0.38f, 0.36f);
        public Color PlatformColor = new Color(0.35f, 0.4f, 0.45f);
        [Tooltip("壁の上の帯など、ステージを特徴づける色")]
        public Color AccentColor = new Color(0.4f, 0.75f, 1f);
        [Tooltip("アリーナの外の遠景")]
        public Color BackdropColor = new Color(0.25f, 0.28f, 0.34f);
        [Tooltip("空(カメラの背景)")]
        public Color SkyColor = new Color(0.55f, 0.65f, 0.78f);
        public Color FogColor = new Color(0.55f, 0.62f, 0.72f);
        public float FogStart = 40f;
        public float FogEnd = 140f;
        public Color LightColor = Color.white;
        public float LightIntensity = 1.3f;
        [Tooltip("太陽の高さと向き(度)")]
        public Vector2 LightAngle = new Vector2(50f, -30f);
        public Color AmbientSky = new Color(0.55f, 0.6f, 0.7f);
        public Color AmbientEquator = new Color(0.4f, 0.4f, 0.45f);
        public Color AmbientGround = new Color(0.2f, 0.2f, 0.22f);
        [Tooltip("床や壁の色を変えるか(探索のマップは自分の色を持つので、光と霧だけ変える)")]
        public bool TintArena = true;
    }
}
