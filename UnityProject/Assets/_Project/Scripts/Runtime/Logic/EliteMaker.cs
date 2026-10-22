using UnityEngine;

namespace BattleFight
{
    public enum EliteAffix
    {
        None,
        /// <summary>巨大: 大きく・硬く・重い</summary>
        Giant,
        /// <summary>俊足: 速く、攻撃の間隔が短い</summary>
        Swift,
        /// <summary>分裂: 倒すと小さな分身が2体出る</summary>
        Splitter,
        /// <summary>障壁: アーマーを持つ(大槌や重い技で割る)</summary>
        Barrier,
    }

    /// <summary>
    /// ときどき出る強化された敵(エリート)を作る。元のデータは変えず、複製に手を加える。
    /// 倒すと刻片が多く、スタイルのボーナスもある。
    /// </summary>
    public static class EliteMaker
    {
        public const float ShardMultiplier = 3f;

        public static string Label(EliteAffix affix) => affix switch
        {
            EliteAffix.Giant => "巨大",
            EliteAffix.Swift => "俊足",
            EliteAffix.Splitter => "分裂",
            EliteAffix.Barrier => "障壁",
            _ => "",
        };

        /// <summary>ボス以外の敵からランダムに選ぶ(エリートにしないなら None)</summary>
        public static EliteAffix Roll(EnemyProfile profile, float chance, System.Random random)
        {
            if (profile == null || profile.isBoss || profile.elite != EliteAffix.None || chance <= 0f) return EliteAffix.None;
            if (random.NextDouble() >= chance) return EliteAffix.None;
            return (EliteAffix)random.Next(1, 5);
        }

        /// <summary>エリートの複製を作る(使い終わったら破棄してよい)</summary>
        public static EnemyProfile Create(EnemyProfile source, EliteAffix affix)
        {
            var elite = Object.Instantiate(source);
            elite.name = source.name + "_Elite_" + affix;
            elite.elite = affix;
            elite.eliteSource = source;
            elite.shardValue = source.shardValue * ShardMultiplier;
            elite.color = Color.Lerp(source.color, new Color(1f, 0.82f, 0.3f), 0.35f);
            switch (affix)
            {
                case EliteAffix.Giant:
                    elite.scale = source.scale * 1.45f;
                    elite.maxHealth = source.maxHealth * 2.5f;
                    elite.staggerThreshold = source.staggerThreshold * 2f;
                    elite.heavy = true;
                    elite.skillDamageScale = source.skillDamageScale * 1.4f;
                    foreach (var attack in elite.attacks) attack.damage *= 1.4f;
                    break;
                case EliteAffix.Swift:
                    elite.moveSpeed = source.moveSpeed * 1.6f;
                    elite.turnSpeed = source.turnSpeed * 1.5f;
                    elite.attackCooldown = source.attackCooldown * 0.55f;
                    elite.skillWindupScale = source.skillWindupScale * 0.85f;
                    foreach (var attack in elite.attacks) attack.windup *= 0.8f;
                    break;
                case EliteAffix.Splitter:
                    elite.maxHealth = source.maxHealth * 1.4f;
                    break;
                case EliteAffix.Barrier:
                    elite.maxArmor = source.maxArmor + 80f;
                    elite.armorRegenDelay = 4f;
                    elite.maxHealth = source.maxHealth * 1.3f;
                    elite.color = Color.Lerp(source.color, new Color(0.5f, 0.8f, 1f), 0.4f);
                    break;
            }
            return elite;
        }

        /// <summary>分裂したときの小さな分身</summary>
        public static EnemyProfile CreateSplit(EnemyProfile source)
        {
            var split = Object.Instantiate(source);
            split.name = source.name + "_Split";
            split.displayName = source.displayName + "の分身";
            split.elite = EliteAffix.None;
            split.eliteSource = null;
            split.scale = source.scale * 0.65f;
            split.maxHealth = Mathf.Max(10f, source.maxHealth * 0.3f);
            split.maxArmor = 0f;
            split.moveSpeed = source.moveSpeed * 1.2f;
            split.shardValue = source.shardValue * 0.3f;
            return split;
        }
    }
}
