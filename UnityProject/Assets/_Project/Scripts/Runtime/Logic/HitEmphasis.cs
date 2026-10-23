using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 当たりの強調の計算。コンボが続くほど大きく、技の最後の一撃(締め)はさらに大きく見せる。
    /// </summary>
    public static class HitEmphasis
    {
        /// <summary>この数のコンボで、強調がいちばん大きくなる</summary>
        public const int FullComboCount = 40;
        /// <summary>コンボがいちばん大きいときの倍率</summary>
        public const float MaxComboScale = 1.6f;

        /// <summary>コンボの数に応じた倍率(ヒットストップ・揺れ・火花)。1 〜 MaxComboScale</summary>
        public static float ComboScale(int iCombo) =>
            1f + (MaxComboScale - 1f) * Mathf.Clamp01(iCombo / (float)FullComboCount);

        /// <summary>技の締めの一撃か(2段以上ある技の最後の段、またはフィニッシャー)</summary>
        public static bool IsFinale(SkillData iSkill, int iStep, bool iFinisher)
        {
            if (iSkill == null) return false;
            if (iFinisher) return iStep >= iSkill.StepCount - 1;
            return iSkill.StepCount > 1 && iStep >= iSkill.StepCount - 1;
        }

        /// <summary>ダメージの数字の大きさ(1 = ふつう)。目立たせたい当たりほど大きい</summary>
        public static float DamageNumberScale(bool iWeak, bool iSwapStrike, bool iArmorBroken, bool iHeavy, bool iFinale)
        {
            float aScale = 1f;
            if (iHeavy) aScale = Mathf.Max(aScale, 1.25f);
            if (iWeak) aScale = Mathf.Max(aScale, 1.35f);
            if (iArmorBroken) aScale = Mathf.Max(aScale, 1.5f);
            if (iFinale) aScale = Mathf.Max(aScale, 1.5f);
            if (iSwapStrike) aScale = Mathf.Max(aScale, 1.6f);
            return aScale;
        }
    }
}
