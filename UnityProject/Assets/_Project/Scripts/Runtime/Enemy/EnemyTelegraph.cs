using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 敵の予備動作を見やすくする。
    /// 近接・飛びかかり: 攻撃の範囲を地面に出し、予備動作が進むほど内側が埋まっていく。
    /// 弾: 狙っている向きに光る線を出す。
    /// 攻撃が出る直前(GlintTime 秒前)に、範囲と線が白く光り、敵にもきらめきが出る(パリィ・見切りの合図)。
    /// </summary>
    public class EnemyTelegraph : MonoBehaviour
    {
        /// <summary>
        /// 攻撃が出る何秒前に白く光らせるか。光ったのを見てから押して(人の反応 約0.2秒)、
        /// パリィ(押して 0.18 秒以内)・見切り(回避の無敵)にちょうど間に合うように、少し早めに光らせる
        /// </summary>
        public const float GlintTime = 0.35f;
        /// <summary>ガードできない(逃げるしかない)攻撃の色。ふつうの攻撃の色(琥珀色など)と見分けられるよう、赤はここだけで使う</summary>
        public static readonly Color DangerColor = new Color(1f, 0.12f, 0.15f);
        const float mAimLength = 14f;

        EnemyController mEnemy;
        Material mMaterial;
        Transform mArea;
        Transform mFill;
        Transform mAim;
        Renderer mAreaRenderer;
        Renderer mFillRenderer;
        Renderer mAimRenderer;
        bool mGlinted;

        /// <summary>今、範囲を出しているか / 狙いの線を出しているか(テスト用)</summary>
        public bool ShowingArea => mArea != null && mArea.gameObject.activeSelf;
        public bool ShowingAim => mAim != null && mAim.gameObject.activeSelf;
        /// <summary>範囲の埋まり具合(0 〜 1)。テスト用</summary>
        public float FillAmount { get; private set; }
        /// <summary>攻撃直前の白い光を出しているか(テスト用)</summary>
        public bool IsGlinting { get; private set; }
        /// <summary>今出している予告が、逃げるしかない攻撃か(テスト用)</summary>
        public bool IsDanger { get; private set; }

        public void Setup(EnemyController iEnemy, Material iMaterial)
        {
            mEnemy = iEnemy;
            mMaterial = iMaterial;
        }

        void Start()
        {
            mArea = CreatePart("TelegraphArea", PrimitiveType.Cylinder, out mAreaRenderer);
            mFill = CreatePart("TelegraphFill", PrimitiveType.Cylinder, out mFillRenderer);
            mAim = CreatePart("TelegraphAim", PrimitiveType.Cube, out mAimRenderer);
            Hide();
        }

        Transform CreatePart(string iName, PrimitiveType iShape, out Renderer oRenderer)
        {
            var aObject = GameObject.CreatePrimitive(iShape);
            aObject.name = iName;
            Destroy(aObject.GetComponent<Collider>());
            // 敵の子にすると、敵の大きさ・のけぞりで形が崩れるので、親を持たせない(敵が消えるとき OnDestroy で消す)
            oRenderer = aObject.GetComponent<Renderer>();
            if (mMaterial != null) oRenderer.sharedMaterial = mMaterial;
            oRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            oRenderer.receiveShadows = false;
            return aObject.transform;
        }

        void OnDestroy()
        {
            if (mArea != null) Destroy(mArea.gameObject);
            if (mFill != null) Destroy(mFill.gameObject);
            if (mAim != null) Destroy(mAim.gameObject);
        }

        void Hide()
        {
            if (mArea != null) mArea.gameObject.SetActive(false);
            if (mFill != null) mFill.gameObject.SetActive(false);
            if (mAim != null) mAim.gameObject.SetActive(false);
            FillAmount = 0f;
            IsGlinting = false;
            IsDanger = false;
        }

        void LateUpdate()
        {
            if (mEnemy == null || mArea == null) return;
            var aAttack = mEnemy.CurrentAttack;
            if (!mEnemy.IsTelegraphing || aAttack == null || mEnemy.IsDead)
            {
                if (ShowingArea || ShowingAim) Hide();
                mGlinted = false;
                return;
            }

            float aProgress = mEnemy.WindupProgress;
            IsGlinting = mEnemy.WindupRealTimeLeft <= GlintTime;
            if (IsGlinting && !mGlinted)
            {
                mGlinted = true;
                if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnSparks(mEnemy.CenterPoint + Vector3.up * 0.6f, Color.white, 6, 4f);
            }

            IsDanger = aAttack.Unblockable;
            var aColor = IsDanger ? DangerColor : mEnemy.Profile.TelegraphColor;
            // 白く光る間は点滅させる。逃げるしかない攻撃は、はじめから赤く速く脈打つ
            float aBlink = IsGlinting ? 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 60f)
                : IsDanger ? 0.25f + 0.25f * Mathf.Sin(Time.unscaledTime * 18f) : 0f;
            var aBright = Color.Lerp(aColor, Color.white, aBlink);

            if (aAttack.Ranged)
            {
                mArea.gameObject.SetActive(false);
                mFill.gameObject.SetActive(false);
                ShowAim(aBright, aProgress);
                return;
            }
            if (aAttack.TargetedStrike || aAttack.Summon != null)
            {
                // 狙いの場所は、攻撃が出たときに別の予告が出る
                Hide();
                return;
            }

            mAim.gameObject.SetActive(false);
            Vector3 aCenter = aAttack.JumpSlam ? mEnemy.SlamDestination : mEnemy.transform.TransformPoint(aAttack.Offset);
            float aGround = aAttack.JumpSlam ? mEnemy.SlamDestination.y : mEnemy.transform.position.y;
            float aRadius = mEnemy.AttackRadius(aAttack);
            FillAmount = aProgress;

            mArea.gameObject.SetActive(true);
            mArea.position = new Vector3(aCenter.x, aGround + 0.03f, aCenter.z);
            mArea.localScale = new Vector3(aRadius * 2f, 0.01f, aRadius * 2f);
            mAreaRenderer.material.color = Color.Lerp(aColor * 0.45f, Color.white, aBlink * 0.6f);

            mFill.gameObject.SetActive(true);
            float aFill = aRadius * 2f * Mathf.Clamp01(aProgress);
            mFill.position = new Vector3(aCenter.x, aGround + 0.045f, aCenter.z);
            mFill.localScale = new Vector3(aFill, 0.01f, aFill);
            mFillRenderer.material.color = aBright;
        }

        void ShowAim(Color iColor, float iProgress)
        {
            mAim.gameObject.SetActive(true);
            Vector3 aStart = mEnemy.CenterPoint;
            Vector3 aDirection = mEnemy.transform.forward;
            if (mEnemy.Target != null)
            {
                Vector3 aOffset = mEnemy.Target.position + Vector3.up - aStart;
                if (aOffset.sqrMagnitude > 0.01f) aDirection = aOffset.normalized;
            }
            // 予備動作が進むほど太く・長く
            float aLength = mAimLength * (0.4f + 0.6f * Mathf.Clamp01(iProgress));
            float aWidth = 0.04f + 0.08f * Mathf.Clamp01(iProgress);
            mAim.position = aStart + aDirection * (aLength * 0.5f);
            mAim.rotation = Quaternion.LookRotation(aDirection);
            mAim.localScale = new Vector3(aWidth, aWidth, aLength);
            mAimRenderer.material.color = iColor;
        }
    }
}
