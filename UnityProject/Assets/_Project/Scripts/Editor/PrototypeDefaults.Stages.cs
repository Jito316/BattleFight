using System.Collections.Generic;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>ボリューム追加分: 敵4種 + 訓練用の人形 + 2体目のボス、ステージ構成。</summary>
    static partial class PrototypeDefaults
    {
        // ---------- 敵 ----------

        public static void Shooter(EnemyProfile p)
        {
            p.displayName = "射手";
            p.maxHealth = 70f;
            p.staggerThreshold = 5f;
            p.moveSpeed = 3f;
            p.preferredDistance = 9f;
            p.attackCooldown = 1.4f;
            p.color = new Color(0.4f, 0.6f, 0.35f);
            p.telegraphColor = new Color(1f, 1f, 0.3f);
            p.shardValue = 1.5f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "射撃", ranged = true, range = 14f, windup = 0.8f, active = 0.1f, recovery = 1.0f, damage = 8f,
                    knockback = 4f, projectileSpeed = 18f, projectileRadius = 0.3f, weight = 2f },
                new EnemyAttack { name = "三連射", ranged = true, range = 12f, windup = 1.0f, active = 0.1f, recovery = 1.3f, damage = 7f,
                    knockback = 3f, projectileSpeed = 16f, projectileRadius = 0.3f, projectileCount = 3, projectileSpread = 24f },
            };
        }

        public static void Dasher(EnemyProfile p)
        {
            p.displayName = "疾風兵";
            p.maxHealth = 60f;
            p.staggerThreshold = 4f;
            p.moveSpeed = 6.5f;
            p.attackCooldown = 1.0f;
            p.scale = 0.9f;
            p.color = new Color(0.5f, 0.8f, 0.95f);
            p.shardValue = 1.2f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "疾風斬り", range = 6f, windup = 0.45f, active = 0.22f, recovery = 0.8f, damage = 9f,
                    radius = 1.1f, offset = new Vector3(0, 1, 1f), knockback = 5f, lunge = 6f },
            };
        }

        public static void Flyer(EnemyProfile p)
        {
            p.displayName = "飛蟲";
            p.flying = true;
            p.hoverHeight = 3.5f;
            p.maxHealth = 55f;
            p.staggerThreshold = 4f;
            p.moveSpeed = 5f;
            p.attackCooldown = 1.3f;
            p.bodyShape = PrimitiveType.Sphere;
            p.scale = 0.85f;
            p.color = new Color(0.6f, 0.35f, 0.8f);
            p.shardValue = 1.5f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "急降下", range = 9f, windup = 0.7f, active = 0.4f, recovery = 1.0f, damage = 10f,
                    radius = 1.0f, offset = new Vector3(0, 1, 0.6f), knockback = 6f, lunge = 10f, weight = 2f },
                new EnemyAttack { name = "毒液", ranged = true, range = 12f, windup = 0.7f, active = 0.1f, recovery = 1.1f, damage = 7f,
                    knockback = 3f, projectileSpeed = 14f, projectileRadius = 0.35f },
            };
        }

        public static void Bomber(EnemyProfile p)
        {
            p.displayName = "爆弾兵";
            p.maxHealth = 40f;
            p.staggerThreshold = 3f;
            p.moveSpeed = 5.2f;
            p.attackCooldown = 0.3f;
            p.bodyShape = PrimitiveType.Sphere;
            p.scale = 0.9f;
            p.color = new Color(0.95f, 0.5f, 0.15f);
            p.telegraphColor = new Color(1f, 0.95f, 0.2f);
            p.shardValue = 0.8f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "自爆", selfDestruct = true, range = 2.2f, windup = 0.9f, active = 0.1f, recovery = 0.1f,
                    damage = 24f, radius = 2.6f, offset = new Vector3(0, 1, 0), knockback = 12f },
            };
        }

        public static void Dummy(EnemyProfile p)
        {
            p.displayName = "訓練用人形";
            p.maxHealth = 300f;
            p.staggerThreshold = 1f;
            p.moveSpeed = 0f;
            p.color = new Color(0.75f, 0.7f, 0.55f);
            p.shardValue = 0f;
            p.attacks = new List<EnemyAttack>();
        }

        public static void ArmoredDummy(EnemyProfile p)
        {
            p.displayName = "重装人形";
            p.heavy = true;
            p.maxHealth = 400f;
            p.maxArmor = 80f;
            p.armorRegenDelay = 4f;
            p.staggerThreshold = 20f;
            p.moveSpeed = 0f;
            p.scale = 1.3f;
            p.color = new Color(0.6f, 0.55f, 0.45f);
            p.shardValue = 0f;
            p.attacks = new List<EnemyAttack>();
        }

        public static void FlyingDummy(EnemyProfile p)
        {
            p.displayName = "浮遊人形";
            p.flying = true;
            p.hoverHeight = 3f;
            p.maxHealth = 250f;
            p.staggerThreshold = 1f;
            p.moveSpeed = 0f;
            p.bodyShape = PrimitiveType.Sphere;
            p.color = new Color(0.7f, 0.65f, 0.8f);
            p.shardValue = 0f;
            p.attacks = new List<EnemyAttack>();
        }

        public static void Magus(EnemyProfile p, EnemyProfile summon)
        {
            p.displayName = "魔導機";
            p.isBoss = true;
            p.heavy = true;
            p.flying = true;
            p.hoverHeight = 2.5f;
            p.maxHealth = 1000f;
            p.maxArmor = 120f;
            p.armorRegenDelay = 10f;
            p.staggerThreshold = 60f;
            p.staggerDuration = 0.6f;
            p.armorBreakStagger = 2.5f;
            p.moveSpeed = 3f;
            p.turnSpeed = 200f;
            p.preferredDistance = 8f;
            p.attackCooldown = 1.1f;
            p.bodyShape = PrimitiveType.Sphere;
            p.scale = 2f;
            p.color = new Color(0.25f, 0.3f, 0.8f);
            p.telegraphColor = new Color(0.5f, 0.9f, 1f);
            p.shardValue = 25f;
            p.attacks = new List<EnemyAttack>
            {
                new EnemyAttack { name = "魔弾幕", ranged = true, range = 22f, windup = 0.9f, active = 0.1f, recovery = 1.0f, damage = 10f,
                    knockback = 5f, projectileSpeed = 15f, projectileRadius = 0.4f, projectileCount = 7, projectileSpread = 70f, weight = 2f },
                new EnemyAttack { name = "雷撃", targetedStrike = true, range = 25f, windup = 0.6f, active = 0.1f, recovery = 0.8f,
                    damage = 18f, radius = 2.8f, knockback = 8f, strikeDelay = 0.9f, weight = 1.5f },
                new EnemyAttack { name = "召喚", summon = summon, summonCount = 2, range = 30f, windup = 1.2f, active = 0.1f,
                    recovery = 1.5f, weight = 0.6f },
                new EnemyAttack { name = "衝撃", range = 4f, windup = 0.8f, active = 0.15f, recovery = 1.0f, damage = 16f,
                    radius = 1.8f, offset = new Vector3(0, 0.5f, 0), knockback = 14f },
            };
        }

        // ---------- ステージ ----------

        static StageWave Wave(string label, params EnemyProfile[] enemies) => new StageWave { label = label, enemies = enemies };

        public static void TrainingStage(StageData s, EnemyProfile dummy, EnemyProfile armoredDummy, EnemyProfile flyingDummy)
        {
            s.displayName = "訓練場";
            s.description = "攻撃してこない人形が、倒しても復活します。スキルやプリセットを試す場所です。";
            s.kind = StageKind.Training;
            s.trainingDummies = new[] { dummy, dummy, armoredDummy, flyingDummy };
        }

        public static void Act1(StageData s, EnemyProfile grunt, EnemyProfile armored, EnemyProfile boss)
        {
            s.displayName = "第1幕  鉄獣";
            s.description = "雑兵と重装兵を退け、鉄の獣を倒せ。アーマーは大槌や重い技で削ろう。";
            s.kind = StageKind.Waves;
            s.waves = new[]
            {
                Wave("WAVE", grunt, grunt, grunt),
                Wave("WAVE", grunt, grunt, armored, grunt),
                Wave("WAVE", armored, grunt, armored, grunt),
                Wave("BOSS", boss),
            };
        }

        public static void Act2(StageData s, EnemyProfile grunt, EnemyProfile shooter, EnemyProfile dasher, EnemyProfile flyer,
            EnemyProfile armored, EnemyProfile magus)
        {
            s.displayName = "第2幕  空と銃火";
            s.description = "遠くから撃つ射手と、空を飛ぶ飛蟲が現れる。飛び道具かワイヤーで距離を詰めよう。";
            s.kind = StageKind.Waves;
            s.waves = new[]
            {
                Wave("WAVE", grunt, grunt, shooter, shooter),
                Wave("WAVE", dasher, dasher, dasher, shooter),
                Wave("WAVE", flyer, flyer, flyer, grunt, grunt),
                Wave("WAVE", armored, shooter, shooter, flyer, flyer),
                Wave("BOSS", magus),
            };
        }

        public static void Act3(StageData s, EnemyProfile bomber, EnemyProfile dasher, EnemyProfile shooter, EnemyProfile armored,
            EnemyProfile flyer, EnemyProfile boss, EnemyProfile magus)
        {
            s.displayName = "第3幕  爆炎の回廊";
            s.description = "近づいて自爆する爆弾兵が群れで来る。最後は2体のボスが同時に襲いかかる。";
            s.kind = StageKind.Waves;
            s.waves = new[]
            {
                Wave("WAVE", bomber, bomber, bomber, bomber),
                Wave("WAVE", bomber, bomber, dasher, dasher, shooter),
                Wave("WAVE", armored, armored, flyer, flyer, bomber, bomber),
                Wave("WAVE", shooter, shooter, shooter, dasher, dasher, dasher),
                Wave("BOSS", boss, magus),
            };
        }

        public static void Endless(StageData s, EnemyProfile grunt, EnemyProfile dasher, EnemyProfile shooter, EnemyProfile bomber,
            EnemyProfile flyer, EnemyProfile armored, EnemyProfile boss, EnemyProfile magus)
        {
            s.displayName = "エンドレス";
            s.description = "敵が際限なく押し寄せる。5ウェーブごとにボスが出る。どこまで行けるか。";
            s.kind = StageKind.Endless;
            s.bossEvery = 5;
            s.endlessBosses = new[] { boss, magus };
            s.endlessPool = new List<EndlessEntry>
            {
                new EndlessEntry { profile = grunt, cost = 1f, minWave = 1 },
                new EndlessEntry { profile = dasher, cost = 1.5f, minWave = 2 },
                new EndlessEntry { profile = shooter, cost = 2f, minWave = 2 },
                new EndlessEntry { profile = bomber, cost = 1.5f, minWave = 3 },
                new EndlessEntry { profile = flyer, cost = 2f, minWave = 3 },
                new EndlessEntry { profile = armored, cost = 3f, minWave = 4 },
            };
        }
    }
}
