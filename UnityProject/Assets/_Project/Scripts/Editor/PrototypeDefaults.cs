using System.Collections.Generic;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 試作のたたき台の数値。アセットがまだ無いときだけ使われる。
    /// 調整は生成後のアセット(Assets/_Project/Data)で行う。
    /// </summary>
    static partial class PrototypeDefaults
    {
        static SkillStep Step(int startup, int active, int recovery, float damage, float stagger, float armorBreak, float style,
            Vector3 offset, float radius, float forward = 0.5f, float knockback = 2f, float launch = 0f, float hitstop = 0.04f,
            int chain = 0, int cancel = -1)
        {
            return new SkillStep
            {
                startupFrames = startup,
                activeFrames = active,
                recoveryFrames = recovery,
                chainFrame = chain,
                cancelFrame = cancel >= 0 ? cancel : Mathf.RoundToInt(recovery * 0.6f),
                damage = damage,
                stagger = stagger,
                armorBreak = armorBreak,
                stylePoints = style,
                hitboxOffset = offset,
                hitboxRadius = radius,
                forwardMove = forward,
                knockback = knockback,
                launch = launch,
                hitstop = hitstop,
            };
        }

        static void Setup(SkillData s, string name, string description, SlotType slot, WeaponType weapon, SkillBehavior behavior,
            params SkillStep[] steps)
        {
            s.displayName = name;
            s.description = description;
            s.slot = slot;
            s.weapon = weapon;
            s.behavior = behavior;
            s.steps = new List<SkillStep>(steps);
        }

        // ---------- 剣 ----------

        public static void SwordCombo(SkillData s)
        {
            Setup(s, "連斬", "速い4段コンボ。キャンセルしやすく、つなぎに使う。", SlotType.AttackA, WeaponType.Sword, SkillBehavior.Standard,
                Step(6, 4, 14, 8, 12, 4, 18, new Vector3(0, 1, 1.3f), 1.3f, 0.6f, 1.5f),
                Step(6, 4, 14, 8, 12, 4, 18, new Vector3(0, 1, 1.3f), 1.3f, 0.6f, 1.5f),
                Step(7, 5, 16, 10, 15, 5, 22, new Vector3(0, 1, 1.4f), 1.4f, 0.7f, 2f),
                Step(10, 6, 26, 16, 30, 10, 32, new Vector3(0, 1, 1.5f), 1.6f, 1.0f, 5f, hitstop: 0.07f, cancel: 16));
        }

        public static void SwordIai(SkillData s)
        {
            Setup(s, "居合", "長押しで溜め、離すと突進斬り。溜め中に攻撃を受けるとカウンター。", SlotType.AttackB, WeaponType.Sword,
                SkillBehavior.ChargeCounter,
                Step(4, 6, 24, 22, 45, 10, 45, new Vector3(0, 1, 1.8f), 1.8f, 4.0f, 6f, hitstop: 0.09f, cancel: 16));
            s.chargeFrames = 45;
        }

        public static void SwordStep(SkillData s)
        {
            Setup(s, "ステップ斬り", "入力方向への短距離ダッシュ。無敵があり、通過した敵を斬る。", SlotType.Movement, WeaponType.Sword,
                SkillBehavior.DashStrike,
                Step(2, 12, 10, 5, 8, 2, 12, new Vector3(0, 1, 0.5f), 1.1f, 5.5f, 1f, cancel: 4));
            s.invulnerableFrames = 12;
            s.airGravityScale = 0f;
        }

        public static void SwordFinisher(SkillData s)
        {
            Setup(s, "千刃", "剣マスタリーのフィニッシャー。周囲を斬り刻む。", SlotType.AttackA, WeaponType.Sword, SkillBehavior.Standard,
                Step(8, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(0, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(0, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(0, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(6, 6, 30, 35, 80, 40, 60, new Vector3(0, 1, 0.8f), 3.2f, 0f, 8f, hitstop: 0.12f));
            s.autoChain = true;
            s.invulnerableFrames = 60;
        }

        // ---------- 大槌 ----------

        public static void HammerCombo(SkillData s)
        {
            Setup(s, "重撃", "遅い3段コンボ。アーマーを大きく削る。", SlotType.AttackA, WeaponType.Hammer, SkillBehavior.Standard,
                Step(16, 6, 22, 18, 35, 30, 28, new Vector3(0, 1, 1.6f), 1.6f, 0.8f, 4f, hitstop: 0.07f),
                Step(18, 6, 24, 22, 40, 35, 30, new Vector3(0, 1, 1.6f), 1.6f, 0.8f, 4f, hitstop: 0.07f),
                Step(26, 8, 34, 34, 70, 60, 45, new Vector3(0, 1, 1.7f), 2.0f, 1.0f, 8f, hitstop: 0.11f, cancel: 22));
        }

        public static void HammerQuake(SkillData s)
        {
            Setup(s, "地砕き", "周囲に衝撃波を出して敵を打ち上げる。", SlotType.AttackB, WeaponType.Hammer, SkillBehavior.Standard,
                Step(22, 6, 30, 20, 40, 25, 40, new Vector3(0, 0.5f, 0.8f), 3.8f, 0f, 3f, launch: 11f, hitstop: 0.1f, cancel: 20));
        }

        public static void HammerJump(SkillData s)
        {
            Setup(s, "ハンマージャンプ", "地面を叩いて高く跳び、着地で衝撃波を出す。", SlotType.Movement, WeaponType.Hammer,
                SkillBehavior.HammerJump,
                Step(5, 1, 0, 0, 0, 0, 0, Vector3.zero, 0f, 0f),
                Step(0, 4, 16, 12, 30, 15, 25, new Vector3(0, 0.3f, 0), 3.0f, 0f, 3f, launch: 7f, hitstop: 0.06f, cancel: 6));
            s.usableInAir = false;
            s.jumpVelocity = 15f;
            s.dashDistance = 3.5f;
            s.invulnerableFrames = 8;
        }

        public static void HammerFinisher(SkillData s)
        {
            Setup(s, "天崩", "大槌マスタリーのフィニッシャー。広範囲を叩き潰す。", SlotType.AttackA, WeaponType.Hammer, SkillBehavior.Standard,
                Step(30, 8, 40, 70, 120, 120, 90, new Vector3(0, 0.5f, 1.0f), 5.5f, 0.5f, 6f, launch: 12f, hitstop: 0.15f));
            s.invulnerableFrames = 40;
        }

        // ---------- 鎖 ----------

        public static void ChainCombo(SkillData s)
        {
            Setup(s, "鎖鞭", "中距離まで届く3段の連撃。威力は低め。", SlotType.AttackA, WeaponType.Chain, SkillBehavior.Standard,
                Step(7, 5, 14, 6, 10, 3, 16, new Vector3(0, 1, 2.6f), 1.9f, 0.2f, 1f),
                Step(7, 5, 14, 6, 10, 3, 16, new Vector3(0, 1, 3.0f), 1.9f, 0.2f, 1f),
                Step(10, 8, 20, 10, 20, 5, 26, new Vector3(0, 1, 1.0f), 3.2f, 0.2f, 3f, hitstop: 0.05f));
        }

        public static void ChainPull(SkillData s)
        {
            Setup(s, "引き寄せ", "軽い敵は手元に引き寄せ、重い敵には自分が飛びつく。", SlotType.AttackB, WeaponType.Chain, SkillBehavior.Pull,
                Step(4, 4, 18, 6, 30, 5, 30, new Vector3(0, 1, 1.3f), 1.4f, 0f, 1f, cancel: 10));
            s.range = 14f;
            s.moveSpeed = 30f;
        }

        public static void ChainWire(SkillData s)
        {
            Setup(s, "ワイヤー",
                "視界内で一番近いグラップルポイントへ飛び、少しの間ぶら下がる。ぶら下がり中は次のワイヤー・ジャンプ・空中攻撃につなげられる。" +
                "ポイントがなければロックオン中の敵へ、それもなければ空中ダッシュ。", SlotType.Movement,
                WeaponType.Chain, SkillBehavior.Grapple,
                Step(0, 4, 12, 5, 15, 3, 20, new Vector3(0, 1, 1.0f), 1.4f, 0f, 1f, cancel: 4));
            s.range = 18f;
            s.moveSpeed = 28f;
            s.dashDistance = 6f;
        }

        public static void ChainFinisher(SkillData s)
        {
            Setup(s, "縛鎖陣", "鎖マスタリーのフィニッシャー。周囲を鎖で薙ぎ払い続ける。", SlotType.AttackA, WeaponType.Chain,
                SkillBehavior.Standard,
                Step(10, 4, 3, 12, 25, 10, 22, new Vector3(0, 1, 0), 4.5f, 0f, 1f),
                Step(0, 4, 3, 12, 25, 10, 22, new Vector3(0, 1, 0), 4.5f, 0f, 1f),
                Step(0, 4, 3, 12, 25, 10, 22, new Vector3(0, 1, 0), 4.5f, 0f, 1f),
                Step(4, 6, 30, 25, 60, 30, 50, new Vector3(0, 1, 0), 5.0f, 0f, 6f, launch: 9f, hitstop: 0.12f));
            s.autoChain = true;
            s.invulnerableFrames = 50;
        }

        // ---------- 武器種 ----------

        public static void Sword(WeaponTypeData w, SkillData finisher)
        {
            w.weapon = WeaponType.Sword;
            w.displayName = "剣";
            w.color = new Color(0.45f, 0.8f, 1f);
            w.headShape = PrimitiveType.Cube;
            w.headScale = new Vector3(0.08f, 0.14f, 1.3f);
            w.headOffset = new Vector3(0f, 0f, 0.8f);
            w.handleLength = 0.2f;
            w.masteryFinisher = finisher;
            w.synergyStaggerBonus = 0.1f;
        }

        public static void Hammer(WeaponTypeData w, SkillData finisher)
        {
            w.weapon = WeaponType.Hammer;
            w.displayName = "大槌";
            w.color = new Color(1f, 0.55f, 0.2f);
            w.headShape = PrimitiveType.Cube;
            w.headScale = new Vector3(0.45f, 0.45f, 0.7f);
            w.headOffset = new Vector3(0f, 0f, 1.25f);
            w.handleLength = 1.0f;
            w.masteryFinisher = finisher;
            w.synergyStaggerBonus = 0.15f;
        }

        public static void Chain(WeaponTypeData w, SkillData finisher)
        {
            w.weapon = WeaponType.Chain;
            w.displayName = "鎖";
            w.color = new Color(0.75f, 0.5f, 1f);
            w.headShape = PrimitiveType.Cylinder;
            w.headScale = new Vector3(0.06f, 1.2f, 0.06f);
            w.headOffset = new Vector3(0f, 0f, 1.2f);
            w.headRotation = new Vector3(90f, 0f, 0f);
            w.handleLength = 0f;
            w.masteryFinisher = finisher;
            w.synergyStaggerBonus = 0.1f;
        }

        // ---------- 敵 ----------

        public static void Grunt(EnemyProfile p)
        {
            p.displayName = "雑兵";
            p.maxHealth = 90f;
            p.staggerThreshold = 5f;
            p.moveSpeed = 3.5f;
            p.attackCooldown = 1.4f;
            p.color = new Color(0.45f, 0.5f, 0.6f);
            p.shardValue = 1f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "斬りかかり", range = 2.2f, windup = 0.7f, active = 0.15f, recovery = 0.9f, damage = 10f,
                    radius = 1.2f, offset = new Vector3(0, 1, 1.2f), knockback = 5f, lunge = 1.5f },
            };
        }

        public static void Armored(EnemyProfile p)
        {
            p.displayName = "重装兵";
            p.heavy = true;
            p.maxHealth = 160f;
            p.maxArmor = 60f;
            p.armorRegenDelay = 7f;
            p.staggerThreshold = 25f;
            p.moveSpeed = 2.6f;
            p.attackCooldown = 1.6f;
            p.scale = 1.3f;
            p.color = new Color(0.6f, 0.4f, 0.25f);
            p.shardValue = 2.5f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "叩きつけ", range = 2.4f, windup = 1.0f, active = 0.15f, recovery = 1.1f, damage = 18f,
                    radius = 1.2f, offset = new Vector3(0, 1, 1.1f), knockback = 8f },
                new EnemyAttack { name = "薙ぎ払い", range = 2.8f, windup = 0.8f, active = 0.2f, recovery = 1.0f, damage = 14f,
                    radius = 1.8f, offset = new Vector3(0, 1, 0.4f), knockback = 7f },
            };
        }

        public static void Boss(EnemyProfile p)
        {
            p.displayName = "鉄獣";
            p.isBoss = true;
            p.heavy = true;
            p.maxHealth = 900f;
            p.maxArmor = 150f;
            p.armorRegenDelay = 10f;
            p.staggerThreshold = 60f;
            p.staggerDuration = 0.6f;
            p.armorBreakStagger = 2.5f;
            p.moveSpeed = 4f;
            p.turnSpeed = 240f;
            p.attackCooldown = 1.0f;
            p.scale = 2.2f;
            p.color = new Color(0.55f, 0.15f, 0.15f);
            p.shardValue = 20f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "爪撃", range = 4f, windup = 0.6f, active = 0.15f, recovery = 0.8f, damage = 15f,
                    radius = 1.1f, offset = new Vector3(0, 0.8f, 0.9f), knockback = 8f, weight = 2f },
                new EnemyAttack { name = "突進", range = 10f, windup = 0.8f, active = 0.35f, recovery = 1.0f, damage = 20f,
                    radius = 0.9f, offset = new Vector3(0, 0.6f, 0.6f), knockback = 12f, lunge = 9f },
                new EnemyAttack { name = "跳躍叩きつけ", range = 13f, windup = 1.1f, active = 0.12f, recovery = 1.3f, damage = 25f,
                    radius = 2.0f, offset = Vector3.zero, knockback = 14f, jumpSlam = true, weight = 0.8f },
            };
        }
    }
}
