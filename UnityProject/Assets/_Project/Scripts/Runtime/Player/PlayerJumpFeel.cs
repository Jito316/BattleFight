using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// ジャンプと着地の手ごたえ。踏み切りで体が縦に伸び、足元に土煙が立つ。
    /// 着地では体がつぶれ、落ちてきた速さに応じて土煙・音・カメラの沈み込み・振動が大きくなる。
    /// </summary>
    public class PlayerJumpFeel : MonoBehaviour
    {
        /// <summary>この速さ(下向き)より遅い着地は、軽い着地として扱う</summary>
        public const float SoftLandingSpeed = 4f;
        /// <summary>この速さで落ちてきた着地が、いちばん重い</summary>
        public const float HardLandingSpeed = 20f;
        static readonly Color mDustColor = new Color(0.72f, 0.68f, 0.6f);

        PlayerMotor mMotor;
        PlayerHitReaction mBody;

        /// <summary>着地した回数と、最後の着地の重さ(0 〜 1)。テスト用</summary>
        public int LandCount { get; private set; }
        public float LastLandStrength { get; private set; }

        /// <summary>着地の重さ(0 = ふわっと、1 = 高いところから)</summary>
        public static float LandStrength(float iFallSpeed) => Mathf.InverseLerp(SoftLandingSpeed, HardLandingSpeed, iFallSpeed);

        void Awake()
        {
            mMotor = GetComponent<PlayerMotor>();
            mBody = GetComponent<PlayerHitReaction>();
            if (mBody == null) mBody = gameObject.AddComponent<PlayerHitReaction>();
        }

        void OnEnable()
        {
            if (mMotor == null) return;
            mMotor.Jumped += OnJumped;
            mMotor.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (mMotor == null) return;
            mMotor.Jumped -= OnJumped;
            mMotor.Landed -= OnLanded;
        }

        void OnJumped()
        {
            mBody.Squash(-0.3f);
            SoundPlayer.Play(Sfx.Jump, 0.5f);
            Rumble.Play(0.05f, 0.2f, 0.05f);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, 0.9f, mDustColor);
            }
        }

        void OnLanded(float iFallSpeed)
        {
            float aStrength = LandStrength(iFallSpeed);
            LandCount++;
            LastLandStrength = aStrength;

            mBody.Squash(0.18f + 0.37f * aStrength);
            SoundPlayer.Play(Sfx.Land, 0.35f + 0.55f * aStrength, 1.1f - 0.3f * aStrength);
            Rumble.Play(0.1f + 0.6f * aStrength, 0.15f + 0.3f * aStrength, 0.05f + 0.12f * aStrength);
            var aFeedback = CombatFeedback.Instance;
            if (aFeedback == null) return;
            aFeedback.SpawnShockwave(transform.position + Vector3.up * 0.05f, 0.8f + 1.8f * aStrength, mDustColor);
            aFeedback.Dip(0.05f + 0.25f * aStrength);
            if (aStrength > 0.5f)
            {
                aFeedback.Shake(0.1f + 0.2f * aStrength);
                aFeedback.SpawnSparks(transform.position + Vector3.up * 0.2f, mDustColor, Mathf.RoundToInt(4 + 8 * aStrength), 5f);
            }
        }
    }
}
