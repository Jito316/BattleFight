using UnityEngine;

namespace BattleFight
{
    /// <summary>残像。少しずつ細く・低くなって消える</summary>
    public class AfterimageFx : MonoBehaviour
    {
        float mLife;
        float mElapsed;
        Vector3 mBaseScale;

        public void Play(float iLife)
        {
            mLife = Mathf.Max(0.05f, iLife);
            mBaseScale = transform.localScale;
        }

        void Update()
        {
            mElapsed += Time.unscaledDeltaTime;
            float aProgress = mElapsed / mLife;
            float aShrink = 1f - 0.5f * aProgress;
            transform.localScale = new Vector3(mBaseScale.x * aShrink, mBaseScale.y * (1f - 0.15f * aProgress), mBaseScale.z * aShrink);
            if (aProgress >= 1f) Destroy(gameObject);
        }
    }
}
