using UnityEngine;

namespace BattleFight
{
    /// <summary>火花・砕けた欠片・飛んでくる刻片。homing があると、少しあとからそこへ吸い寄せられる</summary>
    public class DebrisFx : MonoBehaviour
    {
        Vector3 mVelocity;
        float mLifetime;
        float mElapsed;
        float mSize;
        Transform mHoming;

        public void Play(Vector3 iStartVelocity, float iLife, Transform iTarget)
        {
            mVelocity = iStartVelocity;
            mLifetime = iLife;
            mHoming = iTarget;
            mSize = transform.localScale.x;
        }

        void Update()
        {
            float aDeltaTime = Time.deltaTime;
            mElapsed += aDeltaTime;
            float aProgress = mElapsed / mLifetime;
            if (mHoming != null && aProgress > 0.35f)
            {
                // 刻片: 途中からプレイヤーへ向かう
                Vector3 aOffset = mHoming.position + Vector3.up - transform.position;
                mVelocity = Vector3.Lerp(mVelocity, aOffset.normalized * 22f, 1f - Mathf.Exp(-10f * aDeltaTime));
                if (aOffset.sqrMagnitude < 0.4f) aProgress = 1f;
            }
            else
            {
                mVelocity += Physics.gravity * (0.6f * aDeltaTime);
            }
            transform.position += mVelocity * aDeltaTime;
            transform.Rotate(mVelocity * (40f * aDeltaTime), Space.World);
            transform.localScale = Vector3.one * (mSize * Mathf.Clamp01(1.2f - aProgress));
            if (aProgress >= 1f) Destroy(gameObject);
        }
    }
}
