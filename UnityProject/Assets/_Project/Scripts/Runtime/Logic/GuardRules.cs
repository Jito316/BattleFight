using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// ガードとパリィのきまり。
    /// ガード: 押している間、前から来た攻撃を受け止める(ダメージは ChipRatio に減り、ひるまない)。
    /// パリィ: ガードを押した直後(ParryWindow 秒以内)に受けると、ダメージなしで弾き返し、殴った敵がよろける。
    /// </summary>
    public static class GuardRules
    {
        /// <summary>ガードで受けたときに通るダメージの割合</summary>
        public const float ChipRatio = 0.2f;
        /// <summary>ガードを押してから、パリィになる時間(秒)</summary>
        public const float ParryWindow = 0.18f;
        /// <summary>ガード中の移動の速さの倍率</summary>
        public const float MoveSpeedScale = 0.45f;
        /// <summary>パリィされた敵がよろける時間(ボスは半分)</summary>
        public const float ParryStagger = 1.2f;
        /// <summary>
        /// 受け止められる向きの広さ。正面と、攻撃が来た向きの角度の cos がこれより大きければ受け止める(約 150 度)
        /// </summary>
        public const float FrontCos = 0.25f;

        /// <summary>前から来た攻撃か。iHitDirection は攻撃者から自分への向き</summary>
        public static bool Covers(Vector3 iForward, Vector3 iHitDirection)
        {
            iForward.y = 0f;
            iHitDirection.y = 0f;
            if (iForward.sqrMagnitude < 1e-4f || iHitDirection.sqrMagnitude < 1e-4f) return true;
            return Vector3.Dot(iForward.normalized, -iHitDirection.normalized) > FrontCos;
        }

        /// <summary>ガードを押した時刻と、攻撃を受けた時刻から、パリィになるか</summary>
        public static bool IsParry(float iGuardStartTime, float iHitTime) =>
            iHitTime >= iGuardStartTime && iHitTime - iGuardStartTime <= ParryWindow;

        /// <summary>ガードで受けたときのダメージ(倒れはしない: 体力は 1 残る)</summary>
        public static float ChipDamage(float iDamage, float iHealth) => Mathf.Min(iDamage * ChipRatio, Mathf.Max(0f, iHealth - 1f));
    }
}
