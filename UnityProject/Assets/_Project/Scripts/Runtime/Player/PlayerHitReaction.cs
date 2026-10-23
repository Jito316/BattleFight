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

        /// <summary>のけぞりの途中か(テスト用)</summary>
        public bool IsReacting => mStrength > 0f;

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
            if (mBody == null || mStrength <= 0f) return;
            bool aFrozen = Time.unscaledTime < mFrozenUntil;
            if (!aFrozen) mTime += Time.deltaTime;
            if (mTime >= mDuration)
            {
                mStrength = 0f;
                mBody.localPosition = mBasePosition;
                mBody.localScale = mBaseScale;
                mBody.localRotation = mBaseRotation;
                return;
            }

            float aCurve = Mathf.Exp(-mTime * 11f) * Mathf.Cos(mTime * 26f);
            float aAmount = mStrength * aCurve;
            float aSquash = 1f - 0.18f * aAmount;
            float aStretch = 1f + 0.14f * aAmount;
            mBody.localScale = new Vector3(mBaseScale.x * aStretch, mBaseScale.y * aSquash, mBaseScale.z * aStretch);

            // 足元を支点に傾ける(体の中心は、足元から体の高さの半分のところ)
            Vector3 aAxis = Vector3.Cross(Vector3.up, mDirection);
            var aLean = Quaternion.AngleAxis(22f * aAmount, aAxis);
            Vector3 aFromFeet = new Vector3(0f, mBasePosition.y * aSquash, 0f);
            Vector3 aShake = aFrozen ? mDirection * (Random.Range(-1f, 1f) * 0.07f * mStrength) : Vector3.zero;
            mBody.localPosition = new Vector3(mBasePosition.x, 0f, mBasePosition.z) + aLean * aFromFeet + aShake;
            mBody.localRotation = aLean * mBaseRotation;
        }
    }
}
