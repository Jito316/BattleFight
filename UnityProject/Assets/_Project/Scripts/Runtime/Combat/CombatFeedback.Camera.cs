using UnityEngine;

namespace BattleFight
{
    /// <summary>CombatFeedback: カメラと画面(揺れ・押し・ズーム・沈み込み・倒したときの寄り・インパクトフレーム・画面の縁の光)</summary>
    public partial class CombatFeedback
    {
        public void Shake(float iAmount)
        {
            if (mCameraRig != null) mCameraRig.AddShake(iAmount * GameSettings.Shake);
        }

        /// <summary>当たった向きへカメラを一瞬押す</summary>
        public void Kick(Vector3 iDirection, float iAmount)
        {
            if (mCameraRig != null) mCameraRig.AddKick(iDirection, iAmount * GameSettings.Shake);
        }

        /// <summary>倒した瞬間の決め: その敵の方へカメラを寄せてズームする</summary>
        public void KillCam(Vector3 iPoint, float iDuration)
        {
            if (mCameraRig != null && GameSettings.Zoom > 0f) mCameraRig.FocusOn(iPoint, iDuration, 9f * GameSettings.Zoom);
        }

        /// <summary>カメラを一瞬沈ませる(着地)</summary>
        public void Dip(float iAmount)
        {
            if (mCameraRig != null) mCameraRig.AddDip(iAmount * GameSettings.Shake);
        }

        /// <summary>一瞬ズームする(重い一撃・技の締め)</summary>
        public void Punch(float iDegrees)
        {
            if (mCameraRig != null) mCameraRig.AddZoom(iDegrees * GameSettings.Zoom);
        }

        /// <summary>インパクトフレーム: 1〜2 コマ、画面を白く光らせる(いちばん強い当たりだけに使う)</summary>
        public void Flash(float iStrength)
        {
            mFlashStrength = Mathf.Max(FlashAmount, Mathf.Clamp01(iStrength * GameSettings.Flash));
            mFlashTime = Time.unscaledTime;
        }

        /// <summary>画面の縁を、武器の色で一瞬光らせる</summary>
        public void Vignette(Color iColor, float iStrength)
        {
            mVignetteStrength = Mathf.Max(VignetteAmount, Mathf.Clamp01(iStrength * GameSettings.Flash));
            mVignetteTime = Time.unscaledTime;
            VignetteColor = iColor;
        }
    }
}
