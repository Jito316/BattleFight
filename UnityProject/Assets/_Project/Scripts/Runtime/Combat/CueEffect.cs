using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 差し替えた演出(EffectLibrary のプレハブ)を、出すたびに最初から流し、時間が来たらしまう(EffectPool で使い回す)
    /// </summary>
    public class CueEffect : MonoBehaviour
    {
        /// <summary>パーティクルがないプレハブを出しておく秒数</summary>
        const float mDefaultLifetime = 2f;

        ParticleSystem[] mParticles;
        float mRemaining;

        public void Play(Color iColor, bool iUseColor, float iLifetime)
        {
            if (mParticles == null) mParticles = GetComponentsInChildren<ParticleSystem>(true);
            float aLongest = 0f;
            foreach (var aParticle in mParticles)
            {
                var aMain = aParticle.main;
                if (iUseColor) aMain.startColor = iColor;
                aLongest = Mathf.Max(aLongest, aMain.duration + aMain.startLifetime.constantMax);
                aParticle.Clear(true);
                aParticle.Play(true);
            }
            mRemaining = iLifetime > 0f ? iLifetime : aLongest > 0f ? aLongest : mDefaultLifetime;
        }

        void Update()
        {
            mRemaining -= Time.deltaTime;
            if (mRemaining <= 0f) EffectPool.Release(gameObject);
        }
    }
}
