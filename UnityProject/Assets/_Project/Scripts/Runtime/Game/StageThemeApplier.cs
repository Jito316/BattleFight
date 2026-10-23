using UnityEngine;
using UnityEngine.Rendering;

namespace BattleFight
{
    /// <summary>ステージの見た目を当てる。床・壁などの色は MaterialPropertyBlock で変える(マテリアルのアセットは変えない)</summary>
    public static class StageThemeApplier
    {
        static readonly int mBaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int mColorId = Shader.PropertyToID("_Color");
        static MaterialPropertyBlock mBlock;

        /// <summary>今当たっている見た目(テスト用)</summary>
        public static StageTheme Current { get; private set; }

        public static void Apply(StageTheme iTheme, Transform iArena, Light iSun, Camera iCamera)
        {
            if (iTheme == null) return;
            Current = iTheme;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = iTheme.AmbientSky;
            RenderSettings.ambientEquatorColor = iTheme.AmbientEquator;
            RenderSettings.ambientGroundColor = iTheme.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = iTheme.FogColor;
            RenderSettings.fogStartDistance = iTheme.FogStart;
            RenderSettings.fogEndDistance = iTheme.FogEnd;

            if (iSun != null)
            {
                iSun.color = iTheme.LightColor;
                iSun.intensity = iTheme.LightIntensity;
                iSun.transform.rotation = Quaternion.Euler(iTheme.LightAngle.x, iTheme.LightAngle.y, 0f);
            }
            if (iCamera != null)
            {
                iCamera.clearFlags = CameraClearFlags.SolidColor;
                iCamera.backgroundColor = iTheme.SkyColor;
            }

            if (iArena == null || !iTheme.TintArena) return;
            foreach (var aRenderer in iArena.GetComponentsInChildren<Renderer>(true))
            {
                var aColor = ColorFor(iTheme, aRenderer.name);
                if (aColor.HasValue) Tint(aRenderer, aColor.Value);
            }
        }

        /// <summary>部品の名前から色を決める(GrapplePoint などは変えない)</summary>
        public static Color? ColorFor(StageTheme iTheme, string iName)
        {
            if (iName == "Floor") return iTheme.FloorColor;
            if (iName.StartsWith("Wall_")) return iTheme.WallColor;
            if (iName.StartsWith("Pillar_")) return iTheme.PillarColor;
            if (iName.StartsWith("Platform_")) return iTheme.PlatformColor;
            if (iName.StartsWith("Accent_")) return iTheme.AccentColor;
            if (iName.StartsWith("Backdrop_")) return iTheme.BackdropColor;
            return null;
        }

        public static void Tint(Renderer iRenderer, Color iColor)
        {
            mBlock ??= new MaterialPropertyBlock();
            iRenderer.GetPropertyBlock(mBlock);
            mBlock.SetColor(mBaseColorId, iColor);
            mBlock.SetColor(mColorId, iColor);
            iRenderer.SetPropertyBlock(mBlock);
        }

        /// <summary>部品に当たっている色(テスト用。当たっていなければ null)</summary>
        public static Color? TintOf(Renderer iRenderer)
        {
            mBlock ??= new MaterialPropertyBlock();
            iRenderer.GetPropertyBlock(mBlock);
            if (mBlock.isEmpty) return null;
            return mBlock.GetColor(mBaseColorId);
        }
    }
}
