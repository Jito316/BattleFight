using UnityEngine;

namespace BattleFight
{
    /// <summary>衝撃波・土煙の輪。地面に沿って一気に広がり、薄くなって消える</summary>
    public class ShockwaveFx : MonoBehaviour
    {
        const float mDuration = 0.25f;

        float mRadius;
        float mElapsed;

        public void Play(float iNewRadius, Color iColor)
        {
            mRadius = iNewRadius;
            GetComponent<Renderer>().material.color = iColor;
            transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        void Update()
        {
            mElapsed += Time.deltaTime;
            float aProgress = mElapsed / mDuration;
            float aDiameter = mRadius * 2f * Mathf.Sqrt(Mathf.Clamp01(aProgress));
            transform.localScale = new Vector3(aDiameter, 0.02f * (1f - aProgress) + 0.005f, aDiameter);
            if (aProgress >= 1f) Destroy(gameObject);
        }
    }
}
