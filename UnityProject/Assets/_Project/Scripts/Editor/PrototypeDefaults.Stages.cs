using System.Collections.Generic;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>ボリューム追加分: 敵4種 + 訓練用の人形 + 2体目のボス、ステージ構成。</summary>
    static partial class PrototypeDefaults
    {
        // ---------- 敵 ----------

        public static void Shooter(EnemyProfile iP)
        {
            iP.DisplayName = "射手";
            iP.MaxHealth = 70f;
            iP.StaggerThreshold = 5f;
            iP.MoveSpeed = 3f;
            iP.PreferredDistance = 9f;
            iP.AttackCooldown = 1.4f;
            iP.Color = new Color(0.4f, 0.6f, 0.35f);
            iP.TelegraphColor = new Color(1f, 1f, 0.3f);
            iP.ShardValue = 1.5f;
            iP.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "射撃", Ranged = true, Range = 14f, Windup = 0.8f, Active = 0.1f, Recovery = 1.0f, Damage = 8f,
                    Knockback = 4f, ProjectileSpeed = 18f, ProjectileRadius = 0.3f, Weight = 2f },
                new EnemyAttack { Name = "三連射", Ranged = true, Range = 12f, Windup = 1.0f, Active = 0.1f, Recovery = 1.3f, Damage = 7f,
                    Knockback = 3f, ProjectileSpeed = 16f, ProjectileRadius = 0.3f, ProjectileCount = 3, ProjectileSpread = 24f },
            };
        }

        public static void Dasher(EnemyProfile iP)
        {
            iP.DisplayName = "疾風兵";
            iP.MaxHealth = 60f;
            iP.StaggerThreshold = 4f;
            iP.MoveSpeed = 6.5f;
            iP.AttackCooldown = 1.0f;
            iP.Scale = 0.9f;
            iP.Color = new Color(0.5f, 0.8f, 0.95f);
            iP.ShardValue = 1.2f;
            iP.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "疾風斬り", Range = 6f, Windup = 0.45f, Active = 0.22f, Recovery = 0.8f, Damage = 9f,
                    Radius = 1.1f, Offset = new Vector3(0, 1, 1f), Knockback = 5f, Lunge = 6f },
            };
        }

        public static void Flyer(EnemyProfile iP)
        {
            iP.DisplayName = "飛蟲";
            iP.Flying = true;
            iP.HoverHeight = 3.5f;
            iP.MaxHealth = 55f;
            iP.StaggerThreshold = 4f;
            iP.MoveSpeed = 5f;
            iP.AttackCooldown = 1.3f;
            iP.BodyShape = PrimitiveType.Sphere;
            iP.Scale = 0.85f;
            iP.Color = new Color(0.6f, 0.35f, 0.8f);
            iP.ShardValue = 1.5f;
            iP.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "急降下", Range = 9f, Windup = 0.7f, Active = 0.4f, Recovery = 1.0f, Damage = 10f,
                    Radius = 1.0f, Offset = new Vector3(0, 1, 0.6f), Knockback = 6f, Lunge = 10f, Weight = 2f },
                new EnemyAttack { Name = "毒液", Ranged = true, Range = 12f, Windup = 0.7f, Active = 0.1f, Recovery = 1.1f, Damage = 7f,
                    Knockback = 3f, ProjectileSpeed = 14f, ProjectileRadius = 0.35f },
            };
        }

        public static void Bomber(EnemyProfile iP)
        {
            iP.DisplayName = "爆弾兵";
            iP.MaxHealth = 40f;
            iP.StaggerThreshold = 3f;
            iP.MoveSpeed = 5.2f;
            iP.AttackCooldown = 0.3f;
            iP.BodyShape = PrimitiveType.Sphere;
            iP.Scale = 0.9f;
            iP.Color = new Color(0.95f, 0.5f, 0.15f);
            iP.TelegraphColor = new Color(1f, 0.95f, 0.2f);
            iP.ShardValue = 0.8f;
            iP.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "自爆", SelfDestruct = true, Range = 2.2f, Windup = 0.9f, Active = 0.1f, Recovery = 0.1f,
                    Damage = 24f, Radius = 2.6f, Offset = new Vector3(0, 1, 0), Knockback = 12f },
            };
        }

        public static void Dummy(EnemyProfile iP)
        {
            iP.DisplayName = "訓練用人形";
            iP.MaxHealth = 300f;
            iP.StaggerThreshold = 1f;
            iP.MoveSpeed = 0f;
            iP.Color = new Color(0.75f, 0.7f, 0.55f);
            iP.ShardValue = 0f;
            iP.Attacks = new List<EnemyAttack>();
        }

        public static void ArmoredDummy(EnemyProfile iP)
        {
            iP.DisplayName = "重装人形";
            iP.Heavy = true;
            iP.MaxHealth = 400f;
            iP.MaxArmor = 80f;
            iP.ArmorRegenDelay = 4f;
            iP.StaggerThreshold = 20f;
            iP.MoveSpeed = 0f;
            iP.Scale = 1.3f;
            iP.Color = new Color(0.6f, 0.55f, 0.45f);
            iP.ShardValue = 0f;
            iP.Attacks = new List<EnemyAttack>();
        }

        public static void FlyingDummy(EnemyProfile iP)
        {
            iP.DisplayName = "浮遊人形";
            iP.Flying = true;
            iP.HoverHeight = 3f;
            iP.MaxHealth = 250f;
            iP.StaggerThreshold = 1f;
            iP.MoveSpeed = 0f;
            iP.BodyShape = PrimitiveType.Sphere;
            iP.Color = new Color(0.7f, 0.65f, 0.8f);
            iP.ShardValue = 0f;
            iP.Attacks = new List<EnemyAttack>();
        }

        public static void Magus(EnemyProfile iP, EnemyProfile iSummon)
        {
            iP.DisplayName = "魔導機";
            iP.IsBoss = true;
            iP.Heavy = true;
            iP.Flying = true;
            iP.HoverHeight = 2.5f;
            iP.MaxHealth = 1000f;
            iP.MaxArmor = 120f;
            iP.ArmorRegenDelay = 10f;
            iP.StaggerThreshold = 60f;
            iP.StaggerDuration = 0.6f;
            iP.ArmorBreakStagger = 2.5f;
            iP.MoveSpeed = 3f;
            iP.TurnSpeed = 200f;
            iP.PreferredDistance = 8f;
            iP.AttackCooldown = 1.1f;
            iP.BodyShape = PrimitiveType.Sphere;
            iP.Scale = 2f;
            iP.Color = new Color(0.25f, 0.3f, 0.8f);
            iP.TelegraphColor = new Color(0.5f, 0.9f, 1f);
            iP.ShardValue = 25f;
            iP.Attacks = new List<EnemyAttack>
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
        public static void Shadow(EnemyProfile iP)
        {
            iP.DisplayName = "影刃";
            iP.IsBoss = true;
            iP.Heavy = true;
            iP.Weaknesses = new[] { WeaponType.Chain, WeaponType.Staff };
            iP.MaxHealth = 1100f;
            iP.MaxArmor = 100f;
            iP.ArmorRegenDelay = 8f;
            iP.StaggerThreshold = 55f;
            iP.StaggerDuration = 0.5f;
            iP.ArmorBreakStagger = 2.2f;
            iP.MoveSpeed = 5.5f;
            iP.TurnSpeed = 540f;
            iP.AttackCooldown = 0.9f;
            iP.Scale = 1.5f;
            iP.Color = new Color(0.15f, 0.15f, 0.22f);
            iP.TelegraphColor = new Color(0.8f, 0.3f, 1f);
            iP.ShardValue = 30f;
            iP.Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "三連斬", Range = 3.2f, Windup = 0.55f, Active = 0.15f, Recovery = 0.9f, Damage = 10f,
                    Radius = 1.0f, Offset = new Vector3(0, 0.8f, 0.9f), Knockback = 4f, Lunge = 1.5f, Repeat = 2, RepeatWindup = 0.3f,
                    Weight = 2f },
                new EnemyAttack { Name = "影走り", Range = 12f, Windup = 0.7f, Active = 0.3f, Recovery = 1.0f, Damage = 14f,
                    Radius = 0.8f, Offset = new Vector3(0, 0.6f, 0.5f), Knockback = 9f, Lunge = 11f },
                new EnemyAttack { Name = "背後取り", TeleportBehind = true, Range = 20f, Windup = 1.1f, Active = 0.15f, Recovery = 1.0f,
                    Damage = 18f, Radius = 1.0f, Offset = new Vector3(0, 0.7f, 0.8f), Knockback = 10f, Weight = 0.8f },
            };
            iP.Phase2HealthRatio = 0.5f;
            iP.Phase2SpeedMultiplier = 1.3f;
            iP.Phase2CooldownMultiplier = 0.6f;
            iP.Phase2Message = "影がさらに速くなる……!";
            iP.Phase2Attacks = new List<EnemyAttack>
            {
                new EnemyAttack { Name = "旋風刃", Range = 4f, Windup = 0.7f, Active = 0.2f, Recovery = 1.0f, Damage = 16f,
                    Radius = 1.5f, Offset = new Vector3(0, 0.7f, 0), Knockback = 12f },
                new EnemyAttack { Name = "飛刃", Ranged = true, Range = 16f, Windup = 0.8f, Active = 0.1f, Recovery = 1.1f, Damage = 8f,
                    Knockback = 4f, ProjectileSpeed = 20f, ProjectileRadius = 0.35f, ProjectileCount = 5, ProjectileSpread = 60f },
            };
        }

        // ---------- ステージ ----------

        static StageWave Wave(string iLabel, params EnemyProfile[] iEnemies) => new StageWave { Label = iLabel, Enemies = iEnemies };

        public static void ExplorationStage(StageData iS)
        {
            iS.DisplayName = "探索  古城";
            iS.Description = "剣だけを手に城を巡り、スキルを集めよう。手に入れたスキルで、ひび割れた壁・高い段差・谷を越えられる。";
            iS.Kind = StageKind.Exploration;
        }

        public static void TrainingStage(StageData iS, EnemyProfile iDummy, EnemyProfile iArmoredDummy, EnemyProfile iFlyingDummy)
        {
            iS.DisplayName = "訓練場";
            iS.Description = "攻撃してこない人形が、倒しても復活します。スキルやプリセットを試す場所です。";
            iS.Kind = StageKind.Training;
            iS.TrainingDummies = new[] { iDummy, iDummy, iArmoredDummy, iFlyingDummy };
        }

        public static void Act1(StageData iS, EnemyProfile iGrunt, EnemyProfile iArmored, EnemyProfile iBoss)
        {
            iS.DisplayName = "第1幕  鉄獣";
            iS.Description = "雑兵と重装兵を退け、鉄の獣を倒せ。アーマーは大槌や重い技で削ろう。";
            iS.Kind = StageKind.Waves;
            iS.Waves = new[]
            {
                Wave("WAVE", iGrunt, iGrunt, iGrunt),
                Wave("WAVE", iGrunt, iGrunt, iArmored, iGrunt),
                Wave("WAVE", iArmored, iGrunt, iArmored, iGrunt),
                Wave("BOSS", iBoss),
            };
        }

        public static void Act2(StageData iS, EnemyProfile iGrunt, EnemyProfile iShooter, EnemyProfile iDasher, EnemyProfile iFlyer,
            EnemyProfile iArmored, EnemyProfile iMagus)
        {
            iS.DisplayName = "第2幕  空と銃火";
            iS.Description = "遠くから撃つ射手と、空を飛ぶ飛蟲が現れる。飛び道具かワイヤーで距離を詰めよう。";
            iS.Kind = StageKind.Waves;
            iS.Waves = new[]
            {
                Wave("WAVE", iGrunt, iGrunt, iShooter, iShooter),
                Wave("WAVE", iDasher, iDasher, iDasher, iShooter),
                Wave("WAVE", iFlyer, iFlyer, iFlyer, iGrunt, iGrunt),
                Wave("WAVE", iArmored, iShooter, iShooter, iFlyer, iFlyer),
                Wave("BOSS", iMagus),
            };
        }

        public static void Act3(StageData iS, EnemyProfile iBomber, EnemyProfile iDasher, EnemyProfile iShooter, EnemyProfile iArmored,
            EnemyProfile iFlyer, EnemyProfile iBoss, EnemyProfile iMagus)
        {
            iS.DisplayName = "第3幕  爆炎の回廊";
            iS.Description = "近づいて自爆する爆弾兵が群れで来る。最後は2体のボスが同時に襲いかかる。";
            iS.Kind = StageKind.Waves;
            iS.Waves = new[]
            {
                Wave("WAVE", iBomber, iBomber, iBomber, iBomber),
                Wave("WAVE", iBomber, iBomber, iDasher, iDasher, iShooter),
                Wave("WAVE", iArmored, iArmored, iFlyer, iFlyer, iBomber, iBomber),
                Wave("WAVE", iShooter, iShooter, iShooter, iDasher, iDasher, iDasher),
                Wave("BOSS", iBoss, iMagus),
            };
        }

        public static void Act4(StageData iS, EnemyProfile iDasher, EnemyProfile iShooter, EnemyProfile iFlyer, EnemyProfile iArmored,
            EnemyProfile iBomber, EnemyProfile iShadow)
        {
            iS.DisplayName = "第4幕  影の剣聖";
            iS.Description = "素早い敵が続き、最後は背後に回り込む剣士「影刃」。体力が半分を切ると第二形態になる。弱点は鎖と杖。";
            iS.Kind = StageKind.Waves;
            iS.Waves = new[]
            {
                Wave("WAVE", iDasher, iDasher, iDasher, iShooter),
                Wave("WAVE", iFlyer, iFlyer, iDasher, iDasher, iArmored),
                Wave("WAVE", iBomber, iBomber, iBomber, iShooter, iShooter),
                Wave("BOSS", iShadow),
            };
        }

        public static void Endless(StageData iS, EnemyProfile iGrunt, EnemyProfile iDasher, EnemyProfile iShooter, EnemyProfile iBomber,
            EnemyProfile iFlyer, EnemyProfile iArmored, EnemyProfile iBoss, EnemyProfile iMagus)
        {
            iS.DisplayName = "エンドレス";
            iS.Description = "敵が際限なく押し寄せる。5ウェーブごとにボス(鉄獣 → 魔導機 → 影刃)が出る。どこまで行けるか。";
            iS.Kind = StageKind.Endless;
            iS.BossEvery = 5;
            iS.EndlessBosses = new[] { iBoss, iMagus };
            iS.EndlessPool = new List<EndlessEntry>
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
