using UnityEngine;

namespace BattleFight
{
    /// <summary>残像。少しずつ細く・低くなって消える</summary>
    public class AfterimageFx : MonoBehaviour
    {
        float mLife;
        float mElapsed;
        Vector3 mBaseScale;
        Mesh mBakedMesh;

        /// <summary>骨で動くモデルの形を焼き付ける入れ物(使い回すので、1 つの残像に 1 つだけ作る)</summary>
        public Mesh BakedMesh
        {
            get
            {
                if (mBakedMesh == null) mBakedMesh = new Mesh { name = "Afterimage" };
                return mBakedMesh;
            }
        }

        void OnDestroy()
        {
            if (mBakedMesh != null) Destroy(mBakedMesh);
        }

        public void Play(float iLife)
        {
            mElapsed = 0f;
            mLife = Mathf.Max(0.05f, iLife);
            mBaseScale = transform.localScale;
        }

        void Update()
        {
            mElapsed += Time.unscaledDeltaTime;
            float aProgress = mElapsed / mLife;
            float aShrink = 1f - 0.5f * aProgress;
            transform.localScale = new Vector3(mBaseScale.x * aShrink, mBaseScale.y * (1f - 0.15f * aProgress), mBaseScale.z * aShrink);
            if (aProgress >= 1f) EffectPool.Release(gameObject);
        }
    }
}
