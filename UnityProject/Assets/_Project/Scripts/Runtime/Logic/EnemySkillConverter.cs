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

        public static List<EnemyAttack> ToAttacks(IEnumerable<SkillData> skills, float damageScale, float windupScale)
        {
            var attacks = new List<EnemyAttack>();
            if (skills == null) return attacks;
            foreach (var skill in skills)
            {
                if (skill != null) attacks.Add(ToAttack(skill, damageScale, windupScale));
            }
            return attacks;
        }

        public static EnemyAttack ToAttack(SkillData skill, float damageScale, float windupScale)
        {
            var step = skill.GetStep(0);
            float startup = FrameTime.ToSeconds(step.startupFrames);
            var attack = new EnemyAttack
            {
                name = skill.displayName,
                sourceSkill = skill,
                windup = Mathf.Max(MinWindup, startup * windupScale + WindupPadding),
                active = Mathf.Max(0.1f, FrameTime.ToSeconds(step.activeFrames)),
                recovery = Mathf.Max(MinRecovery, FrameTime.ToSeconds(step.recoveryFrames) * 1.4f),
                damage = Mathf.Max(MinDamage, step.damage * damageScale),
                radius = step.hitboxRadius > 0f ? step.hitboxRadius : 1f,
                offset = step.hitboxOffset,
                knockback = Mathf.Max(2f, step.knockback),
                lunge = step.forwardMove,
                // 攻撃A を多め、移動は少なめに使う(プレイヤーの戦い方に近づける)
                weight = skill.slot == SlotType.AttackA ? 1.3f : skill.slot == SlotType.AttackB ? 1f : 0.6f,
            };
            attack.range = attack.offset.z + attack.radius + 0.4f;

            // 多段の技は、同じ攻撃を続けて出す(2段目以降は予備動作を短く)
            if (skill.StepCount > 1 && !skill.autoChain)
            {
                attack.repeat = Mathf.Min(MaxRepeat, skill.StepCount - 1);
                attack.repeatWindup = Mathf.Max(0.25f, FrameTime.ToSeconds(skill.GetStep(1).startupFrames) * windupScale * 0.6f);
            }

            switch (skill.behavior)
            {
                case SkillBehavior.Projectile:
                    attack.ranged = true;
                    attack.repeat = 0;
                    attack.projectileCount = Mathf.Max(1, skill.projectileCount);
                    attack.projectileSpread = skill.projectileSpread > 0f ? Mathf.Min(skill.projectileSpread, 60f) : (skill.projectileCount > 1 ? 20f : 0f);
                    // 弾は遅めにして避けられるようにする
                    attack.projectileSpeed = Mathf.Clamp(skill.projectileSpeed * 0.55f, 9f, 20f);
                    attack.projectileRadius = Mathf.Max(0.25f, skill.projectileRadius);
                    attack.range = Mathf.Clamp(skill.projectileSpeed * skill.projectileLifetime * 0.6f, 8f, 15f);
                    break;
                case SkillBehavior.Beam:
                    attack.ranged = true;
                    attack.repeat = 0;
                    attack.projectileCount = 1;
                    attack.projectileSpeed = 24f;
                    attack.projectileRadius = Mathf.Max(0.3f, skill.beamRadius);
                    attack.range = Mathf.Min(skill.beamLength, 14f);
                    break;
                case SkillBehavior.TargetedStrike:
                    attack.targetedStrike = true;
                    attack.repeat = 0;
                    attack.radius = Mathf.Max(1.5f, step.hitboxRadius);
                    attack.strikeDelay = skill.strikeDelay + 0.5f;
                    attack.range = 14f;
                    break;
                case SkillBehavior.DashStrike:
                case SkillBehavior.ChargeCounter:
                    // 突進して斬る(居合は溜めるぶん予備動作が長い)
                    attack.lunge = Mathf.Max(4f, Mathf.Max(step.forwardMove, skill.dashDistance));
                    if (skill.behavior == SkillBehavior.ChargeCounter) attack.windup += FrameTime.ToSeconds(skill.chargeFrames) * 0.5f;
                    attack.range = attack.lunge + attack.radius;
                    attack.repeat = 0;
                    break;
                case SkillBehavior.HammerJump:
                    attack.jumpSlam = true;
                    attack.repeat = 0;
                    attack.radius = Mathf.Max(2.2f, MaxRadius(skill));
                    attack.offset = new Vector3(0f, 0.5f, 0.3f);
                    attack.range = 9f;
                    break;
                case SkillBehavior.Blink:
                    // 瞬間移動 = 背後に現れて斬る
                    attack.teleportBehind = true;
                    attack.repeat = 0;
                    attack.windup = Mathf.Max(attack.windup, 0.9f);
                    attack.radius = Mathf.Max(1f, attack.radius);
                    attack.offset = new Vector3(0f, 1f, 1f);
                    attack.range = 12f;
                    break;
                case SkillBehavior.Grapple:
                case SkillBehavior.Pull:
                case SkillBehavior.Boost:
                    // 距離を詰める技は、遠くから飛びかかる
                    attack.lunge = Mathf.Clamp(skill.range > 0f ? skill.range * 0.5f : Mathf.Max(skill.dashDistance, 5f), 5f, 9f);
                    attack.radius = Mathf.Max(1.1f, attack.radius);
                    attack.range = attack.lunge + attack.radius;
                    attack.repeat = 0;
                    break;
            }
            return attack;
        }

        static float MaxRadius(SkillData skill)
        {
            float radius = 0f;
            foreach (var step in skill.steps) radius = Mathf.Max(radius, step.hitboxRadius);
            return radius;
        }
    }
}
