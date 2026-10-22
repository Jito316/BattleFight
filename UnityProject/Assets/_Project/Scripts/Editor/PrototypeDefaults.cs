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
        static SkillStep Step(int iStartup, int iActive, int iRecovery, float iDamage, float iStagger, float iArmorBreak, float iStyle,
            Vector3 iOffset, float iRadius, float iForward = 0.5f, float iKnockback = 2f, float iLaunch = 0f, float iHitstop = 0.04f,
            int iChain = 0, int iCancel = -1)
        {
            return new SkillStep
            {
                StartupFrames = iStartup,
                ActiveFrames = iActive,
                RecoveryFrames = iRecovery,
                ChainFrame = iChain,
                CancelFrame = iCancel >= 0 ? iCancel : Mathf.RoundToInt(iRecovery * 0.6f),
                Damage = iDamage,
                Stagger = iStagger,
                ArmorBreak = iArmorBreak,
                StylePoints = iStyle,
                HitboxOffset = iOffset,
                HitboxRadius = iRadius,
                ForwardMove = iForward,
                Knockback = iKnockback,
                Launch = iLaunch,
                Hitstop = iHitstop,
            };
        }

        static void Setup(SkillData iSkill, string iName, string iDescription, SlotType iSlot, WeaponType iWeapon, SkillBehavior iBehavior,
            params SkillStep[] iSteps)
        {
            iSkill.DisplayName = iName;
            iSkill.Description = iDescription;
            iSkill.Slot = iSlot;
            iSkill.Weapon = iWeapon;
            iSkill.Behavior = iBehavior;
            iSkill.Steps = new List<SkillStep>(iSteps);
        }

        // ---------- 剣 ----------

        public static void SwordCombo(SkillData iSkill)
        {
            Setup(iSkill, "連斬", "速い4段コンボ。キャンセルしやすく、つなぎに使う。", SlotType.AttackA, WeaponType.Sword, SkillBehavior.Standard,
                Step(6, 4, 14, 8, 12, 4, 18, new Vector3(0, 1, 1.3f), 1.3f, 0.6f, 1.5f),
                Step(6, 4, 14, 8, 12, 4, 18, new Vector3(0, 1, 1.3f), 1.3f, 0.6f, 1.5f),
                Step(7, 5, 16, 10, 15, 5, 22, new Vector3(0, 1, 1.4f), 1.4f, 0.7f, 2f),
                Step(10, 6, 26, 16, 30, 10, 32, new Vector3(0, 1, 1.5f), 1.6f, 1.0f, 5f, iHitstop: 0.07f, iCancel: 16));
        }

        public static void SwordIai(SkillData iSkill)
        {
            Setup(iSkill, "居合", "長押しで溜め、離すと突進斬り。溜め中に攻撃を受けるとカウンター。", SlotType.AttackB, WeaponType.Sword,
                SkillBehavior.ChargeCounter,
                Step(4, 6, 24, 22, 45, 10, 45, new Vector3(0, 1, 1.8f), 1.8f, 4.0f, 6f, iHitstop: 0.09f, iCancel: 16));
            iSkill.ChargeFrames = 45;
        }

        public static void SwordStep(SkillData iSkill)
        {
            Setup(iSkill, "ステップ斬り", "入力方向への短距離ダッシュ。無敵があり、通過した敵を斬る。", SlotType.Movement, WeaponType.Sword,
                SkillBehavior.DashStrike,
                Step(2, 12, 10, 5, 8, 2, 12, new Vector3(0, 1, 0.5f), 1.1f, 5.5f, 1f, iCancel: 4));
            iSkill.InvulnerableFrames = 12;
            iSkill.AirGravityScale = 0f;
        }

        public static void SwordFinisher(SkillData iSkill)
        {
            Setup(iSkill, "千刃", "剣マスタリーのフィニッシャー。周囲を斬り刻む。", SlotType.AttackA, WeaponType.Sword, SkillBehavior.Standard,
                Step(8, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(0, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(0, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(0, 3, 2, 10, 20, 8, 20, new Vector3(0, 1, 0.5f), 2.8f, 0f, 1f),
                Step(6, 6, 30, 35, 80, 40, 60, new Vector3(0, 1, 0.8f), 3.2f, 0f, 8f, iHitstop: 0.12f));
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 60;
        }

        // ---------- 大槌 ----------

        public static void HammerCombo(SkillData iSkill)
        {
            Setup(iSkill, "重撃", "遅い3段コンボ。アーマーを大きく削る。", SlotType.AttackA, WeaponType.Hammer, SkillBehavior.Standard,
                Step(16, 6, 22, 18, 35, 30, 28, new Vector3(0, 1, 1.6f), 1.6f, 0.8f, 4f, iHitstop: 0.07f),
                Step(18, 6, 24, 22, 40, 35, 30, new Vector3(0, 1, 1.6f), 1.6f, 0.8f, 4f, iHitstop: 0.07f),
                Step(26, 8, 34, 34, 70, 60, 45, new Vector3(0, 1, 1.7f), 2.0f, 1.0f, 8f, iHitstop: 0.11f, iCancel: 22));
        }

        public static void HammerQuake(SkillData iSkill)
        {
            Setup(iSkill, "地砕き", "周囲に衝撃波を出して敵を打ち上げる。", SlotType.AttackB, WeaponType.Hammer, SkillBehavior.Standard,
                Step(22, 6, 30, 20, 40, 25, 40, new Vector3(0, 0.5f, 0.8f), 3.8f, 0f, 3f, iLaunch: 11f, iHitstop: 0.1f, iCancel: 20));
        }

        public static void HammerJump(SkillData iSkill)
        {
            Setup(iSkill, "ハンマージャンプ", "地面を叩いて高く跳び、着地で衝撃波を出す。", SlotType.Movement, WeaponType.Hammer,
                SkillBehavior.HammerJump,
                Step(5, 1, 0, 0, 0, 0, 0, Vector3.zero, 0f, 0f),
                Step(0, 4, 16, 12, 30, 15, 25, new Vector3(0, 0.3f, 0), 3.0f, 0f, 3f, iLaunch: 7f, iHitstop: 0.06f, iCancel: 6));
            iSkill.UsableInAir = false;
            iSkill.JumpVelocity = 15f;
            iSkill.DashDistance = 3.5f;
            iSkill.InvulnerableFrames = 8;
        }

        public static void HammerFinisher(SkillData iSkill)
        {
            Setup(iSkill, "天崩", "大槌マスタリーのフィニッシャー。広範囲を叩き潰す。", SlotType.AttackA, WeaponType.Hammer, SkillBehavior.Standard,
                Step(30, 8, 40, 70, 120, 120, 90, new Vector3(0, 0.5f, 1.0f), 5.5f, 0.5f, 6f, iLaunch: 12f, iHitstop: 0.15f));
            iSkill.InvulnerableFrames = 40;
        }

        // ---------- 鎖 ----------

        public static void ChainCombo(SkillData iSkill)
        {
            Setup(iSkill, "鎖鞭", "中距離まで届く3段の連撃。威力は低め。", SlotType.AttackA, WeaponType.Chain, SkillBehavior.Standard,
                Step(7, 5, 14, 6, 10, 3, 16, new Vector3(0, 1, 2.6f), 1.9f, 0.2f, 1f),
                Step(7, 5, 14, 6, 10, 3, 16, new Vector3(0, 1, 3.0f), 1.9f, 0.2f, 1f),
                Step(10, 8, 20, 10, 20, 5, 26, new Vector3(0, 1, 1.0f), 3.2f, 0.2f, 3f, iHitstop: 0.05f));
        }

        public static void ChainPull(SkillData iSkill)
        {
            Setup(iSkill, "引き寄せ", "軽い敵は手元に引き寄せ、重い敵には自分が飛びつく。", SlotType.AttackB, WeaponType.Chain, SkillBehavior.Pull,
                Step(4, 4, 18, 6, 30, 5, 30, new Vector3(0, 1, 1.3f), 1.4f, 0f, 1f, iCancel: 10));
            iSkill.Range = 14f;
            iSkill.MoveSpeed = 30f;
        }

        public static void ChainWire(SkillData iSkill)
        {
            Setup(iSkill, "ワイヤー",
                "視界内で一番近いグラップルポイントへ飛び、少しの間ぶら下がる。ぶら下がり中は次のワイヤー・ジャンプ・空中攻撃につなげられる。" +
                "ポイントがなければロックオン中の敵へ、それもなければ空中ダッシュ。", SlotType.Movement,
                WeaponType.Chain, SkillBehavior.Grapple,
                Step(0, 4, 12, 5, 15, 3, 20, new Vector3(0, 1, 1.0f), 1.4f, 0f, 1f, iCancel: 4));
            iSkill.Range = 18f;
            iSkill.MoveSpeed = 28f;
            iSkill.DashDistance = 6f;
        }

        public static void ChainFinisher(SkillData iSkill)
        {
            Setup(iSkill, "縛鎖陣", "鎖マスタリーのフィニッシャー。周囲を鎖で薙ぎ払い続ける。", SlotType.AttackA, WeaponType.Chain,
                SkillBehavior.Standard,
                Step(10, 4, 3, 12, 25, 10, 22, new Vector3(0, 1, 0), 4.5f, 0f, 1f),
                Step(0, 4, 3, 12, 25, 10, 22, new Vector3(0, 1, 0), 4.5f, 0f, 1f),
                Step(0, 4, 3, 12, 25, 10, 22, new Vector3(0, 1, 0), 4.5f, 0f, 1f),
                Step(4, 6, 30, 25, 60, 30, 50, new Vector3(0, 1, 0), 5.0f, 0f, 6f, iLaunch: 9f, iHitstop: 0.12f));
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 50;
        }

        // ---------- 武器種 ----------

        public static void Sword(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Sword;
            iWeapon.DisplayName = "剣";
            iWeapon.Color = new Color(0.45f, 0.8f, 1f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            iWeapon.HeadScale = new Vector3(0.08f, 0.14f, 1.3f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 0.8f);
            iWeapon.HandleLength = 0.2f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.1f;
        }

        public static void Hammer(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Hammer;
            iWeapon.DisplayName = "大槌";
            iWeapon.Color = new Color(1f, 0.55f, 0.2f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            iWeapon.HeadScale = new Vector3(0.45f, 0.45f, 0.7f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 1.25f);
            iWeapon.HandleLength = 1.0f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.15f;
        }

        public static void Chain(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Chain;
            iWeapon.DisplayName = "鎖";
            iWeapon.Color = new Color(0.75f, 0.5f, 1f);
            iWeapon.HeadShape = PrimitiveType.Cylinder;
            iWeapon.HeadScale = new Vector3(0.06f, 1.2f, 0.06f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 1.2f);
            iWeapon.HeadRotation = new Vector3(90f, 0f, 0f);
            iWeapon.HandleLength = 0f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.1f;
        }

        // ---------- 敵 ----------

        public static void Grunt(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "雑兵";
            iProfile.MaxHealth = 90f;
            iProfile.StaggerThreshold = 5f;
            iProfile.MoveSpeed = 3.5f;
            iProfile.AttackCooldown = 1.4f;
            iProfile.Color = new Color(0.45f, 0.5f, 0.6f);
            iProfile.ShardValue = 1f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "斬りかかり", Range = 2.2f, Windup = 0.7f, Active = 0.15f, Recovery = 0.9f, Damage = 10f,
                    Radius = 1.2f, Offset = new Vector3(0, 1, 1.2f), Knockback = 5f, Lunge = 1.5f },
            };
        }

        public static void Armored(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "重装兵";
            iProfile.Heavy = true;
            iProfile.MaxHealth = 160f;
            iProfile.MaxArmor = 60f;
            iProfile.ArmorRegenDelay = 7f;
            iProfile.StaggerThreshold = 25f;
            iProfile.MoveSpeed = 2.6f;
            iProfile.AttackCooldown = 1.6f;
            iProfile.Scale = 1.3f;
            iProfile.Color = new Color(0.6f, 0.4f, 0.25f);
            iProfile.ShardValue = 2.5f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "叩きつけ", Range = 2.4f, Windup = 1.0f, Active = 0.15f, Recovery = 1.1f, Damage = 18f,
                    Radius = 1.2f, Offset = new Vector3(0, 1, 1.1f), Knockback = 8f },
                new EnemyAttack { Name = "薙ぎ払い", Range = 2.8f, Windup = 0.8f, Active = 0.2f, Recovery = 1.0f, Damage = 14f,
                    Radius = 1.8f, Offset = new Vector3(0, 1, 0.4f), Knockback = 7f },
            };
        }

        public static void Boss(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "鉄獣";
            iProfile.IsBoss = true;
            iProfile.Heavy = true;
            iProfile.MaxHealth = 900f;
            iProfile.MaxArmor = 150f;
            iProfile.ArmorRegenDelay = 10f;
            iProfile.StaggerThreshold = 60f;
            iProfile.StaggerDuration = 0.6f;
            iProfile.ArmorBreakStagger = 2.5f;
            iProfile.MoveSpeed = 4f;
            iProfile.TurnSpeed = 240f;
            iProfile.AttackCooldown = 1.0f;
            iProfile.Scale = 2.2f;
            iProfile.Color = new Color(0.55f, 0.15f, 0.15f);
            iProfile.ShardValue = 20f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "爪撃", Range = 4f, Windup = 0.6f, Active = 0.15f, Recovery = 0.8f, Damage = 15f,
                    Radius = 1.1f, Offset = new Vector3(0, 0.8f, 0.9f), Knockback = 8f, Weight = 2f },
                new EnemyAttack { Name = "突進", Range = 10f, Windup = 0.8f, Active = 0.35f, Recovery = 1.0f, Damage = 20f,
                    Radius = 0.9f, Offset = new Vector3(0, 0.6f, 0.6f), Knockback = 12f, Lunge = 9f },
                new EnemyAttack { Name = "跳躍叩きつけ", Range = 13f, Windup = 1.1f, Active = 0.12f, Recovery = 1.3f, Damage = 25f,
                    Radius = 2.0f, Offset = Vector3.zero, Knockback = 14f, JumpSlam = true, Weight = 0.8f },
            };
        }
    }
}
