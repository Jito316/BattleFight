using UnityEngine;

namespace BattleFight
{
    [CreateAssetMenu(menuName = "BattleFight/Style Rank Config", fileName = "StyleRankConfig")]
    public class StyleRankConfig : ScriptableObject
    {
        [Header("ランク")]
        public string[] rankNames = { "D", "C", "B", "A", "S", "SS", "SSS" };
        [Tooltip("各ランクに到達する累計ポイント")]
        public float[] rankThresholds = { 0f, 100f, 250f, 450f, 700f, 1000f, 1400f };
        public float maxPoints = 1800f;

        [Header("減衰")]
        [Tooltip("最後に加点してから減衰が始まるまでの秒数")]
        public float idleDelay = 1.5f;
        [Tooltip("ランクごとの毎秒の減衰量")]
        public float[] decayPerSecond = { 10f, 15f, 20f, 30f, 40f, 55f, 70f };
        [Tooltip("被弾で下がるランク数")]
        public int ranksLostOnDamage = 2;

        [Header("同じ技の繰り返し")]
        public int repeatHistory = 6;
        public float repeatPenaltyPerUse = 0.2f;
        public float minRepeatMultiplier = 0.2f;

        [Header("ボーナス")]
        [Tooltip("直前と違う武器種で当てたときの倍率")]
        public float weaponVarietyMultiplier = 1.25f;
        [Tooltip("アーセナル(3スロットすべて違う武器種)時の倍率")]
        public float arsenalMultiplier = 1.2f;
        public float swapStrikeBonus = 40f;
        public float swapCancelBonus = 25f;
        public float justDodgeBonus = 60f;
        public float counterBonus = 80f;

        [Header("報酬")]
        [Tooltip("ランクごとの刻片ドロップ倍率")]
        public float[] rewardMultiplier = { 1f, 1.2f, 1.5f, 2f, 2.5f, 3f, 4f };
    }
}
