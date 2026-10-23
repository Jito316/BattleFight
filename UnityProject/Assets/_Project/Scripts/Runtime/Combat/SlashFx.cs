using UnityEngine;

namespace BattleFight
{
    /// <summary>斬撃の線。はじめに一気に伸び、細くなりながら消える</summary>
    public class SlashFx : MonoBehaviour
    {
        const float mDuration = 0.16f;

        float mLength;
        float mElapsed;

        public void Play(float iLength, Color iColor)
        {
            mLength = iLength;
            GetComponent<Renderer>().material.color = iColor;
            transform.localScale = new Vector3(0.01f, 0.08f, 0.01f);
        }

        void Update()
        {
            // ヒットストップ中も見えるよう、止まらない時間で進める
            mElapsed += Time.unscaledDeltaTime;
            float aProgress = Mathf.Clamp01(mElapsed / mDuration);
            float aGrow = Mathf.Min(1f, aProgress * 4f);
            transform.localScale = new Vector3(mLength * aGrow, 0.09f * (1f - aProgress) + 0.01f, 0.02f);
            if (aProgress >= 1f) Destroy(gameObject);
        }
    }
}
