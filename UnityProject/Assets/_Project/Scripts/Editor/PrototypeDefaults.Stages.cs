using System.Collections.Generic;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>ボリューム追加分: 敵4種 + 訓練用の人形 + 2体目のボス、ステージ構成。</summary>
    static partial class PrototypeDefaults
    {
        // ---------- 敵 ----------

        public static void Shooter(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "射手";
            iProfile.MaxHealth = 70f;
            iProfile.StaggerThreshold = 5f;
            iProfile.MoveSpeed = 3f;
            iProfile.PreferredDistance = 9f;
            iProfile.AttackCooldown = 1.4f;
            iProfile.Color = new Color(0.4f, 0.6f, 0.35f);
            iProfile.TelegraphColor = new Color(1f, 1f, 0.3f);
            iProfile.ShardValue = 1.5f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "射撃", Ranged = true, Range = 14f, Windup = 0.8f, Active = 0.1f, Recovery = 1.0f, Damage = 8f,
                    Knockback = 4f, ProjectileSpeed = 18f, ProjectileRadius = 0.3f, Weight = 2f },
                new EnemyAttack { Name = "三連射", Ranged = true, Range = 12f, Windup = 1.0f, Active = 0.1f, Recovery = 1.3f, Damage = 7f,
                    Knockback = 3f, ProjectileSpeed = 16f, ProjectileRadius = 0.3f, ProjectileCount = 3, ProjectileSpread = 24f },
            };
        }

        public static void Dasher(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "疾風兵";
            iProfile.MaxHealth = 60f;
            iProfile.StaggerThreshold = 4f;
            iProfile.MoveSpeed = 6.5f;
            iProfile.AttackCooldown = 1.0f;
            iProfile.Scale = 0.9f;
            iProfile.Color = new Color(0.5f, 0.8f, 0.95f);
            iProfile.ShardValue = 1.2f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "疾風斬り", Range = 6f, Windup = 0.45f, Active = 0.22f, Recovery = 0.8f, Damage = 9f,
                    Radius = 1.1f, Offset = new Vector3(0, 1, 1f), Knockback = 5f, Lunge = 6f },
            };
        }

        public static void Flyer(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "飛蟲";
            iProfile.Flying = true;
            iProfile.HoverHeight = 3.5f;
            iProfile.MaxHealth = 55f;
            iProfile.StaggerThreshold = 4f;
            iProfile.MoveSpeed = 5f;
            iProfile.AttackCooldown = 1.3f;
            iProfile.BodyShape = PrimitiveType.Sphere;
            iProfile.Scale = 0.85f;
            iProfile.Color = new Color(0.6f, 0.35f, 0.8f);
            iProfile.ShardValue = 1.5f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "急降下", Range = 9f, Windup = 0.7f, Active = 0.4f, Recovery = 1.0f, Damage = 10f,
                    Radius = 1.0f, Offset = new Vector3(0, 1, 0.6f), Knockback = 6f, Lunge = 10f, Weight = 2f },
                new EnemyAttack { Name = "毒液", Ranged = true, Range = 12f, Windup = 0.7f, Active = 0.1f, Recovery = 1.1f, Damage = 7f,
                    Knockback = 3f, ProjectileSpeed = 14f, ProjectileRadius = 0.35f },
            };
        }

        public static void Bomber(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "爆弾兵";
            iProfile.MaxHealth = 40f;
            iProfile.StaggerThreshold = 3f;
            iProfile.MoveSpeed = 5.2f;
            iProfile.AttackCooldown = 0.3f;
            iProfile.BodyShape = PrimitiveType.Sphere;
            iProfile.Scale = 0.9f;
            iProfile.Color = new Color(0.95f, 0.5f, 0.15f);
            iProfile.TelegraphColor = new Color(1f, 0.95f, 0.2f);
            iProfile.ShardValue = 0.8f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "自爆", SelfDestruct = true, Unblockable = true, Range = 2.2f, Windup = 0.9f, Active = 0.1f, Recovery = 0.1f,
                    Damage = 24f, Radius = 2.6f, Offset = new Vector3(0, 1, 0), Knockback = 12f },
            };
        }

        public static void Dummy(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "訓練用人形";
            iProfile.MaxHealth = 300f;
            iProfile.StaggerThreshold = 1f;
            iProfile.MoveSpeed = 0f;
            iProfile.Color = new Color(0.75f, 0.7f, 0.55f);
            iProfile.ShardValue = 0f;
            iProfile.Attacks = new List<EnemyAttack>();
        }

        public static void ArmoredDummy(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "重装人形";
            iProfile.Heavy = true;
            iProfile.MaxHealth = 400f;
            iProfile.MaxArmor = 80f;
            iProfile.ArmorRegenDelay = 4f;
            iProfile.StaggerThreshold = 20f;
            iProfile.MoveSpeed = 0f;
            iProfile.Scale = 1.3f;
            iProfile.Color = new Color(0.6f, 0.55f, 0.45f);
            iProfile.ShardValue = 0f;
            iProfile.Attacks = new List<EnemyAttack>();
        }

        public static void FlyingDummy(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "浮遊人形";
            iProfile.Flying = true;
            iProfile.HoverHeight = 3f;
            iProfile.MaxHealth = 250f;
            iProfile.StaggerThreshold = 1f;
            iProfile.MoveSpeed = 0f;
            iProfile.BodyShape = PrimitiveType.Sphere;
            iProfile.Color = new Color(0.7f, 0.65f, 0.8f);
            iProfile.ShardValue = 0f;
            iProfile.Attacks = new List<EnemyAttack>();
        }

        public static void Magus(EnemyProfile iProfile, EnemyProfile iSummon)
        {
            iProfile.DisplayName = "魔導機";
            iProfile.IsBoss = true;
            iProfile.Heavy = true;
            iProfile.Flying = true;
            iProfile.HoverHeight = 2.5f;
            iProfile.MaxHealth = 1000f;
            iProfile.MaxArmor = 120f;
            iProfile.ArmorRegenDelay = 10f;
            iProfile.StaggerThreshold = 60f;
            iProfile.StaggerDuration = 0.6f;
            iProfile.ArmorBreakStagger = 2.5f;
            iProfile.MoveSpeed = 3f;
            iProfile.TurnSpeed = 200f;
            iProfile.PreferredDistance = 8f;
            iProfile.AttackCooldown = 1.1f;
            iProfile.BodyShape = PrimitiveType.Sphere;
            iProfile.Scale = 2f;
            iProfile.Color = new Color(0.25f, 0.3f, 0.8f);
            iProfile.TelegraphColor = new Color(0.5f, 0.9f, 1f);
            iProfile.ShardValue = 25f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "魔弾幕", Ranged = true, Range = 22f, Windup = 0.9f, Active = 0.1f, Recovery = 1.0f, Damage = 10f,
                    Knockback = 5f, ProjectileSpeed = 15f, ProjectileRadius = 0.4f, ProjectileCount = 7, ProjectileSpread = 70f, Weight = 2f },
                new EnemyAttack { Name = "雷撃", TargetedStrike = true, Range = 25f, Windup = 0.6f, Active = 0.1f, Recovery = 0.8f,
                    Damage = 18f, Radius = 2.8f, Knockback = 8f, StrikeDelay = 0.9f, Weight = 1.5f },
                new EnemyAttack { Name = "召喚", Summon = iSummon, SummonCount = 2, Range = 30f, Windup = 1.2f, Active = 0.1f,
                    Recovery = 1.5f, Weight = 0.6f },
                new EnemyAttack { Name = "衝撃", Range = 4f, Windup = 0.8f, Active = 0.15f, Recovery = 1.0f, Damage = 16f,
                    Radius = 1.8f, Offset = new Vector3(0, 0.5f, 0), Knockback = 14f },
            };
        }

        /// <summary>
        /// 3体目のボス「影刃」。素早い人型の剣士。
        /// 三連斬・影走り(長い突進)・背後取り(消えて背後に現れる)。体力が半分を切ると第二形態になり、
        /// 速くなって攻撃の間隔が縮み、旋風刃(周囲)と飛刃(扇状の飛び道具)が増える。
        /// </summary>
        public static void Shadow(EnemyProfile iProfile)
        {
            iProfile.DisplayName = "影刃";
            iProfile.IsBoss = true;
            iProfile.Heavy = true;
            iProfile.Weaknesses = new[] { WeaponType.Chain, WeaponType.Staff };
            iProfile.MaxHealth = 1100f;
            iProfile.MaxArmor = 100f;
            iProfile.ArmorRegenDelay = 8f;
            iProfile.StaggerThreshold = 55f;
            iProfile.StaggerDuration = 0.5f;
            iProfile.ArmorBreakStagger = 2.2f;
            iProfile.MoveSpeed = 5.5f;
            iProfile.TurnSpeed = 540f;
            iProfile.AttackCooldown = 0.9f;
            iProfile.Scale = 1.5f;
            iProfile.Color = new Color(0.15f, 0.15f, 0.22f);
            iProfile.TelegraphColor = new Color(0.8f, 0.3f, 1f);
            iProfile.ShardValue = 30f;
            iProfile.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "三連斬", Range = 3.2f, Windup = 0.55f, Active = 0.15f, Recovery = 0.9f, Damage = 10f,
                    Radius = 1.0f, Offset = new Vector3(0, 0.8f, 0.9f), Knockback = 4f, Lunge = 1.5f, Repeat = 2, RepeatWindup = 0.3f,
                    Weight = 2f },
                new EnemyAttack { Name = "影走り", Range = 12f, Windup = 0.7f, Active = 0.3f, Recovery = 1.0f, Damage = 14f,
                    Radius = 0.8f, Offset = new Vector3(0, 0.6f, 0.5f), Knockback = 9f, Lunge = 11f },
                new EnemyAttack { Name = "背後取り", TeleportBehind = true, Range = 20f, Windup = 1.1f, Active = 0.15f, Recovery = 1.0f,
                    Damage = 18f, Radius = 1.0f, Offset = new Vector3(0, 0.7f, 0.8f), Knockback = 10f, Weight = 0.8f },
            };
            iProfile.Phase2HealthRatio = 0.5f;
            iProfile.Phase2SpeedMultiplier = 1.3f;
            iProfile.Phase2CooldownMultiplier = 0.6f;
            iProfile.Phase2Message = "影がさらに速くなる……!";
            iProfile.Phase2Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "旋風刃", Range = 4f, Windup = 0.7f, Active = 0.2f, Recovery = 1.0f, Damage = 16f,
                    Radius = 1.5f, Offset = new Vector3(0, 0.7f, 0), Knockback = 12f },
                new EnemyAttack { Name = "飛刃", Ranged = true, Range = 16f, Windup = 0.8f, Active = 0.1f, Recovery = 1.1f, Damage = 8f,
                    Knockback = 4f, ProjectileSpeed = 20f, ProjectileRadius = 0.35f, ProjectileCount = 5, ProjectileSpread = 60f },
            };
        }

        // ---------- ステージ ----------

        static StageWave Wave(string iLabel, params EnemyProfile[] iEnemies) => new StageWave { Label = iLabel, Enemies = iEnemies };

        public static void ExplorationStage(StageData iStage)
        {
            iStage.DisplayName = "探索  古城";
            iStage.Description = "剣だけを手に城を巡り、スキルを集めよう。手に入れたスキルで、ひび割れた壁・高い段差・谷を越えられる。";
            iStage.Kind = StageKind.Exploration;
        }

        public static void TrainingStage(StageData iStage, EnemyProfile iDummy, EnemyProfile iArmoredDummy, EnemyProfile iFlyingDummy)
        {
            iStage.DisplayName = "訓練場";
            iStage.Description = "攻撃してこない人形が、倒しても復活します。スキルやプリセットを試す場所です。";
            iStage.Kind = StageKind.Training;
            iStage.TrainingDummies = new[] { iDummy, iDummy, iArmoredDummy, iFlyingDummy };
        }

        public static void Act1(StageData iStage, EnemyProfile iGrunt, EnemyProfile iArmored, EnemyProfile iBoss)
        {
            iStage.DisplayName = "第1幕  鉄獣";
            iStage.Description = "雑兵と重装兵を退け、鉄の獣を倒せ。アーマーは大槌や重い技で削ろう。";
            iStage.Kind = StageKind.Waves;
            iStage.Waves = new[]
            {
                Wave("WAVE", iGrunt, iGrunt, iGrunt),
                Wave("WAVE", iGrunt, iGrunt, iArmored, iGrunt),
                Wave("WAVE", iArmored, iGrunt, iArmored, iGrunt),
                Wave("BOSS", iBoss),
            };
        }

        public static void Act2(StageData iStage, EnemyProfile iGrunt, EnemyProfile iShooter, EnemyProfile iDasher, EnemyProfile iFlyer,
            EnemyProfile iArmored, EnemyProfile iMagus)
        {
            iStage.DisplayName = "第2幕  空と銃火";
            iStage.Description = "遠くから撃つ射手と、空を飛ぶ飛蟲が現れる。飛び道具かワイヤーで距離を詰めよう。";
            iStage.Kind = StageKind.Waves;
            iStage.Waves = new[]
            {
                Wave("WAVE", iGrunt, iGrunt, iShooter, iShooter),
                Wave("WAVE", iDasher, iDasher, iDasher, iShooter),
                Wave("WAVE", iFlyer, iFlyer, iFlyer, iGrunt, iGrunt),
                Wave("WAVE", iArmored, iShooter, iShooter, iFlyer, iFlyer),
                Wave("BOSS", iMagus),
            };
        }

        public static void Act3(StageData iStage, EnemyProfile iBomber, EnemyProfile iDasher, EnemyProfile iShooter, EnemyProfile iArmored,
            EnemyProfile iFlyer, EnemyProfile iBoss, EnemyProfile iMagus)
        {
            iStage.DisplayName = "第3幕  爆炎の回廊";
            iStage.Description = "近づいて自爆する爆弾兵が群れで来る。最後は2体のボスが同時に襲いかかる。";
            iStage.Kind = StageKind.Waves;
            iStage.Waves = new[]
            {
                Wave("WAVE", iBomber, iBomber, iBomber, iBomber),
                Wave("WAVE", iBomber, iBomber, iDasher, iDasher, iShooter),
                Wave("WAVE", iArmored, iArmored, iFlyer, iFlyer, iBomber, iBomber),
                Wave("WAVE", iShooter, iShooter, iShooter, iDasher, iDasher, iDasher),
                Wave("BOSS", iBoss, iMagus),
            };
        }

        public static void Act4(StageData iStage, EnemyProfile iDasher, EnemyProfile iShooter, EnemyProfile iFlyer, EnemyProfile iArmored,
            EnemyProfile iBomber, EnemyProfile iShadow)
        {
            iStage.DisplayName = "第4幕  影の剣聖";
            iStage.Description = "素早い敵が続き、最後は背後に回り込む剣士「影刃」。体力が半分を切ると第二形態になる。弱点は鎖と杖。";
            iStage.Kind = StageKind.Waves;
            iStage.Waves = new[]
            {
                Wave("WAVE", iDasher, iDasher, iDasher, iShooter),
                Wave("WAVE", iFlyer, iFlyer, iDasher, iDasher, iArmored),
                Wave("WAVE", iBomber, iBomber, iBomber, iShooter, iShooter),
                Wave("BOSS", iShadow),
            };
        }

        public static void Endless(StageData iStage, EnemyProfile iGrunt, EnemyProfile iDasher, EnemyProfile iShooter, EnemyProfile iBomber,
            EnemyProfile iFlyer, EnemyProfile iArmored, EnemyProfile iBoss, EnemyProfile iMagus)
        {
            iStage.DisplayName = "エンドレス";
            iStage.Description = "敵が際限なく押し寄せる。5ウェーブごとにボス(鉄獣 → 魔導機 → 影刃)が出る。どこまで行けるか。";
            iStage.Kind = StageKind.Endless;
            iStage.BossEvery = 5;
            iStage.EndlessBosses = new[] { iBoss, iMagus };
            iStage.EndlessPool = new List<EndlessEntry>
            {
                new EndlessEntry { Profile = iGrunt, Cost = 1f, MinWave = 1 },
                new EndlessEntry { Profile = iDasher, Cost = 1.5f, MinWave = 2 },
                new EndlessEntry { Profile = iShooter, Cost = 2f, MinWave = 2 },
                new EndlessEntry { Profile = iBomber, Cost = 1.5f, MinWave = 3 },
                new EndlessEntry { Profile = iFlyer, Cost = 2f, MinWave = 3 },
                new EndlessEntry { Profile = iArmored, Cost = 3f, MinWave = 4 },
            };
        }
    }
}
