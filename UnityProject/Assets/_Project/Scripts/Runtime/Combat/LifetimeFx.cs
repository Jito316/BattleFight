using UnityEngine;

namespace BattleFight
{
    /// <summary>決めた時間がたったら、演出をしまう(ビームなど、形の変わらない演出)</summary>
    public class LifetimeFx : MonoBehaviour
    {
        float mLeft;

        public void Play(float iDuration) => mLeft = iDuration;

        void Update()
        {
            mLeft -= Time.deltaTime;
            if (mLeft <= 0f) EffectPool.Release(gameObject);
        }
    }
}
