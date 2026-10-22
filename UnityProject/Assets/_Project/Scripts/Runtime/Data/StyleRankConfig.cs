using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    [CreateAssetMenu(menuName = "BattleFight/Style Rank Config", fileName = "StyleRankConfig")]
    public class StyleRankConfig : ScriptableObject
    {
        [Header("ランク")]
        [FormerlySerializedAs("rankNames")] public string[] RankNames = { "D", "C", "B", "A", "S", "SS", "SSS" };
        [Tooltip("各ランクに到達する累計ポイント")]
        [FormerlySerializedAs("rankThresholds")] public float[] RankThresholds = { 0f, 100f, 250f, 450f, 700f, 1000f, 1400f };
        [FormerlySerializedAs("maxPoints")] public float MaxPoints = 1800f;

        [Header("減衰")]
        [Tooltip("最後に加点してから減衰が始まるまでの秒数")]
        [FormerlySerializedAs("idleDelay")] public float IdleDelay = 1.5f;
        [Tooltip("ランクごとの毎秒の減衰量")]
        [FormerlySerializedAs("decayPerSecond")] public float[] DecayPerSecond = { 10f, 15f, 20f, 30f, 40f, 55f, 70f };
        [Tooltip("被弾で下がるランク数")]
        [FormerlySerializedAs("ranksLostOnDamage")] public int RanksLostOnDamage = 2;

        [Header("同じ技の繰り返し")]
        [FormerlySerializedAs("repeatHistory")] public int RepeatHistory = 6;
        [FormerlySerializedAs("repeatPenaltyPerUse")] public float RepeatPenaltyPerUse = 0.2f;
        [FormerlySerializedAs("minRepeatMultiplier")] public float MinRepeatMultiplier = 0.2f;

        [Header("ボーナス")]
        [Tooltip("直前と違う武器種で当てたときの倍率")]
        [FormerlySerializedAs("weaponVarietyMultiplier")] public float WeaponVarietyMultiplier = 1.25f;
        [Tooltip("アーセナル(3スロットすべて違う武器種)時の倍率")]
        [FormerlySerializedAs("arsenalMultiplier")] public float ArsenalMultiplier = 1.2f;
        [FormerlySerializedAs("swapStrikeBonus")] public float SwapStrikeBonus = 40f;
        [FormerlySerializedAs("swapCancelBonus")] public float SwapCancelBonus = 25f;
        [FormerlySerializedAs("justDodgeBonus")] public float JustDodgeBonus = 60f;
        [FormerlySerializedAs("counterBonus")] public float CounterBonus = 80f;

        [Header("報酬")]
        [Tooltip("ランクごとの刻片ドロップ倍率")]
        [FormerlySerializedAs("rewardMultiplier")] public float[] RewardMultiplier = { 1f, 1.2f, 1.5f, 2f, 2.5f, 3f, 4f };

        [Header("ランクによる強化")]
        [Tooltip("ランクごとの攻撃力の倍率(格好よく戦うほど強くなる)")]
        [FormerlySerializedAs("damageMultiplier")] public float[] DamageMultipliers = { 1f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f };
        [Tooltip("コンボが途切れたとき、1ヒットあたりに入るスタイルのボーナス(10ヒット以上)")]
        [FormerlySerializedAs("comboBonusPerHit")] public float ComboBonusPerHit = 1.5f;

        public float DamageMultiplier(int iRank) =>
            DamageMultipliers == null || DamageMultipliers.Length == 0 ? 1f : DamageMultipliers[Mathf.Clamp(iRank, 0, DamageMultipliers.Length - 1)];
    }
}
