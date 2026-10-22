using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// プレイヤーのスキル(SkillData)を、敵の攻撃(EnemyAttack)に置き換える。
    /// 形(当たり判定・弾・落雷・突進・跳躍・背後取り)はスキルに合わせ、
    /// 予備動作は長く・威力は低くして、見てから避けられる強さにする。
    /// </summary>
    public static class EnemySkillConverter
    {
        /// <summary>予備動作の最短(秒)。プレイヤーの技は発生が速いので、そのままだと見えない</summary>
        public const float MinWindup = 0.45f;
        /// <summary>予告の色が見えるよう、予備動作に足す時間(秒)</summary>
        public const float WindupPadding = 0.25f;
        public const float MinRecovery = 0.6f;
        public const float MinDamage = 4f;
        /// <summary>多段の技を敵が続けて出す回数の上限(連斬の4段は3回まで)</summary>
        public const int MaxRepeat = 2;

        public static List<EnemyAttack> ToAttacks(IEnumerable<SkillData> iSkills, float iDamageScale, float iWindupScale)
        {
            var aAttacks = new List<EnemyAttack>();
            if (iSkills == null) return aAttacks;
            foreach (var aSkill in iSkills)
            {
                if (aSkill != null) aAttacks.Add(ToAttack(aSkill, iDamageScale, iWindupScale));
            }
            return aAttacks;
        }

        public static EnemyAttack ToAttack(SkillData iSkill, float iDamageScale, float iWindupScale)
        {
            var aStep = iSkill.GetStep(0);
            float aStartup = FrameTime.ToSeconds(aStep.StartupFrames);
            var aAttack = new EnemyAttack
            {
                Name = iSkill.DisplayName,
                SourceSkill = iSkill,
                Windup = Mathf.Max(MinWindup, aStartup * iWindupScale + WindupPadding),
                Active = Mathf.Max(0.1f, FrameTime.ToSeconds(aStep.ActiveFrames)),
                Recovery = Mathf.Max(MinRecovery, FrameTime.ToSeconds(aStep.RecoveryFrames) * 1.4f),
                Damage = Mathf.Max(MinDamage, aStep.Damage * iDamageScale),
                Radius = aStep.HitboxRadius > 0f ? aStep.HitboxRadius : 1f,
                Offset = aStep.HitboxOffset,
                Knockback = Mathf.Max(2f, aStep.Knockback),
                Lunge = aStep.ForwardMove,
                // 攻撃A を多め、移動は少なめに使う(プレイヤーの戦い方に近づける)
                Weight = iSkill.Slot == SlotType.AttackA ? 1.3f : iSkill.Slot == SlotType.AttackB ? 1f : 0.6f,
            };
            aAttack.Range = aAttack.Offset.z + aAttack.Radius + 0.4f;

            // 多段の技は、同じ攻撃を続けて出す(2段目以降は予備動作を短く)
            if (iSkill.StepCount > 1 && !iSkill.AutoChain)
            {
                aAttack.Repeat = Mathf.Min(MaxRepeat, iSkill.StepCount - 1);
                aAttack.RepeatWindup = Mathf.Max(0.25f, FrameTime.ToSeconds(iSkill.GetStep(1).StartupFrames) * iWindupScale * 0.6f);
            }

            switch (iSkill.Behavior)
            {
                case SkillBehavior.Projectile:
                    aAttack.Ranged = true;
                    aAttack.Repeat = 0;
                    aAttack.ProjectileCount = Mathf.Max(1, iSkill.ProjectileCount);
                    aAttack.ProjectileSpread = iSkill.ProjectileSpread > 0f ? Mathf.Min(iSkill.ProjectileSpread, 60f) : (iSkill.ProjectileCount > 1 ? 20f : 0f);
                    // 弾は遅めにして避けられるようにする
                    aAttack.ProjectileSpeed = Mathf.Clamp(iSkill.ProjectileSpeed * 0.55f, 9f, 20f);
                    aAttack.ProjectileRadius = Mathf.Max(0.25f, iSkill.ProjectileRadius);
                    aAttack.Range = Mathf.Clamp(iSkill.ProjectileSpeed * iSkill.ProjectileLifetime * 0.6f, 8f, 15f);
                    break;
                case SkillBehavior.Beam:
                    aAttack.Ranged = true;
                    aAttack.Repeat = 0;
                    aAttack.ProjectileCount = 1;
                    aAttack.ProjectileSpeed = 24f;
                    aAttack.ProjectileRadius = Mathf.Max(0.3f, iSkill.BeamRadius);
                    aAttack.Range = Mathf.Min(iSkill.BeamLength, 14f);
                    break;
                case SkillBehavior.TargetedStrike:
                    aAttack.TargetedStrike = true;
                    aAttack.Repeat = 0;
                    aAttack.Radius = Mathf.Max(1.5f, aStep.HitboxRadius);
                    aAttack.StrikeDelay = iSkill.StrikeDelay + 0.5f;
                    aAttack.Range = 14f;
                    break;
                case SkillBehavior.DashStrike:
                case SkillBehavior.ChargeCounter:
                    // 突進して斬る(居合は溜めるぶん予備動作が長い)
                    aAttack.Lunge = Mathf.Max(4f, Mathf.Max(aStep.ForwardMove, iSkill.DashDistance));
                    if (iSkill.Behavior == SkillBehavior.ChargeCounter) aAttack.Windup += FrameTime.ToSeconds(iSkill.ChargeFrames) * 0.5f;
                    aAttack.Range = aAttack.Lunge + aAttack.Radius;
                    aAttack.Repeat = 0;
                    break;
                case SkillBehavior.HammerJump:
                    aAttack.JumpSlam = true;
                    aAttack.Repeat = 0;
                    aAttack.Radius = Mathf.Max(2.2f, MaxRadius(iSkill));
                    aAttack.Offset = new Vector3(0f, 0.5f, 0.3f);
                    aAttack.Range = 9f;
                    break;
                case SkillBehavior.Blink:
                    // 瞬間移動 = 背後に現れて斬る
                    aAttack.TeleportBehind = true;
                    aAttack.Repeat = 0;
                    aAttack.Windup = Mathf.Max(aAttack.Windup, 0.9f);
                    aAttack.Radius = Mathf.Max(1f, aAttack.Radius);
                    aAttack.Offset = new Vector3(0f, 1f, 1f);
                    aAttack.Range = 12f;
                    break;
                case SkillBehavior.Grapple:
                case SkillBehavior.Pull:
                case SkillBehavior.Boost:
                    // 距離を詰める技は、遠くから飛びかかる
                    aAttack.Lunge = Mathf.Clamp(iSkill.Range > 0f ? iSkill.Range * 0.5f : Mathf.Max(iSkill.DashDistance, 5f), 5f, 9f);
                    aAttack.Radius = Mathf.Max(1.1f, aAttack.Radius);
                    aAttack.Range = aAttack.Lunge + aAttack.Radius;
                    aAttack.Repeat = 0;
                    break;
            }
            return aAttack;
        }

        static float MaxRadius(SkillData iSkill)
        {
            float aRadius = 0f;
            foreach (var aStep in iSkill.Steps) aRadius = Mathf.Max(aRadius, aStep.HitboxRadius);
            return aRadius;
        }
    }
}
