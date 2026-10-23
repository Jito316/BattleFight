using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// プレイヤーが攻撃を受けたときの、体ののけぞり・つぶれ。足元を支点に、殴られた向きへ傾いてつぶれ、
    /// ヒットストップの間は小刻みに震え、止まり終わったらはねるように元へ戻る。
    /// 見た目の体(「Body」)だけを動かすので、当たり判定や移動には影響しない。
    /// </summary>
    public class PlayerHitReaction : MonoBehaviour
    {
        const string mBodyName = "Body";
        const float mDuration = 0.45f;

        Transform mBody;
        Vector3 mBasePosition;
        Vector3 mBaseScale;
        Quaternion mBaseRotation;
        Vector3 mDirection;
        float mStrength;
        float mTime;
        float mFrozenUntil = -1f;
        /// <summary>縦のつぶれ(+)・伸び(-)。ばねで 0 へ戻る</summary>
        float mSquash;
        float mSquashVelocity;
        /// <summary>体のひねり(度。+ で右回り)。ばねで 0 へ戻る</summary>
        float mTwist;
        float mTwistVelocity;

        /// <summary>のけぞりの途中か(テスト用)</summary>
        public bool IsReacting => mStrength > 0f;
        /// <summary>つぶれ・伸びの途中か(テスト用)</summary>
        public bool IsSquashing => Mathf.Abs(mSquash) > 0.01f || Mathf.Abs(mSquashVelocity) > 0.05f;

        /// <summary>ひねりの途中か(テスト用)</summary>
        public bool IsTwisting => Mathf.Abs(mTwist) > 0.5f || Mathf.Abs(mTwistVelocity) > 5f;

        /// <summary>体をひねる(振りかぶり・振り抜き)。iDegrees は + で右回り。ばねで戻る</summary>
        public void Twist(float iDegrees)
        {
            mTwist = Mathf.Clamp(iDegrees, -50f, 50f);
            mTwistVelocity = 0f;
        }

        /// <summary>縦につぶす(+: 着地)・伸ばす(-: 踏み切り)。iAmount は 0.3 で大きめ</summary>
        public void Squash(float iAmount)
        {
            mSquash = Mathf.Clamp(iAmount, -0.5f, 0.6f);
            mSquashVelocity = 0f;
        }

        void Awake()
        {
            mBody = transform.Find(mBodyName);
            if (mBody == null) return;
            mBasePosition = mBody.localPosition;
            mBaseScale = mBody.localScale;
            mBaseRotation = mBody.localRotation;
        }

        /// <summary>iDirection(攻撃者からプレイヤーへの向き)へのけぞる。iFreeze 秒は当たった瞬間の形で震える</summary>
        public void React(Vector3 iDirection, float iStrength, float iFreeze)
        {
            if (mBody == null) return;
            Vector3 aLocal = transform.InverseTransformDirection(iDirection);
            aLocal.y = 0f;
            mDirection = aLocal.sqrMagnitude > 1e-4f ? aLocal.normalized : Vector3.back;
            mStrength = Mathf.Clamp(iStrength, 0f, 2f);
            mTime = 0f;
            mFrozenUntil = Time.unscaledTime + iFreeze;
        }

        void LateUpdate()
        {
            if (mBody == null) return;
            // つぶれ・伸び: ばね(少し行き過ぎてから止まる)
            float aDeltaTime = Time.deltaTime;
            if (aDeltaTime > 0f)
            {
                mSquashVelocity += (-mSquash * 260f - mSquashVelocity * 16f) * aDeltaTime;
                mSquash += mSquashVelocity * aDeltaTime;
                if (Mathf.Abs(mSquash) < 0.002f && Mathf.Abs(mSquashVelocity) < 0.02f)
                {
                    mSquash = 0f;
                    mSquashVelocity = 0f;
                }
                // ひねりは少し硬めのばね(すぐ戻って、軽く行き過ぎる)
                mTwistVelocity += (-mTwist * 320f - mTwistVelocity * 20f) * aDeltaTime;
                mTwist += mTwistVelocity * aDeltaTime;
                if (Mathf.Abs(mTwist) < 0.05f && Mathf.Abs(mTwistVelocity) < 0.5f)
                {
                    mTwist = 0f;
                    mTwistVelocity = 0f;
                }
            }

            if (mStrength <= 0f)
            {
                ApplyPose(0f, false);
                return;
            }
            bool aFrozen = Time.unscaledTime < mFrozenUntil;
            if (!aFrozen) mTime += Time.deltaTime;
            if (mTime >= mDuration)
            {
                mStrength = 0f;
                ApplyPose(0f, false);
                return;
            }

            float aCurve = Mathf.Exp(-mTime * 11f) * Mathf.Cos(mTime * 26f);
            ApplyPose(mStrength * aCurve, aFrozen);
        }

        /// <summary>のけぞり(iAmount)と、縦のつぶれ・伸び(mSquash)を合わせて、体の形を決める</summary>
        void ApplyPose(float iAmount, bool iFrozen)
        {
            if (iAmount == 0f && mSquash == 0f && mTwist == 0f)
            {
                mBody.localPosition = mBasePosition;
                mBody.localScale = mBaseScale;
                mBody.localRotation = mBaseRotation;
                return;
            }
            float aSquash = (1f - 0.18f * iAmount) * (1f - mSquash);
            float aStretch = (1f + 0.14f * iAmount) * (1f + mSquash * 0.6f);
            mBody.localScale = new Vector3(mBaseScale.x * aStretch, mBaseScale.y * aSquash, mBaseScale.z * aStretch);

            // 足元を支点に傾ける(体の中心は、足元から体の高さの半分のところ)
            Vector3 aAxis = Vector3.Cross(Vector3.up, mDirection);
            var aLean = Quaternion.AngleAxis(22f * iAmount, aAxis);
            Vector3 aFromFeet = new Vector3(0f, mBasePosition.y * aSquash, 0f);
            Vector3 aShake = iFrozen ? mDirection * (Random.Range(-1f, 1f) * 0.07f * mStrength) : Vector3.zero;
            mBody.localPosition = new Vector3(mBasePosition.x, 0f, mBasePosition.z) + aLean * aFromFeet + aShake;
            mBody.localRotation = aLean * Quaternion.Euler(0f, mTwist, 0f) * mBaseRotation;
        }
    }
}
