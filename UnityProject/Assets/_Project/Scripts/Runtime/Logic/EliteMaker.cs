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

        public static string Label(EliteAffix iAffix) => iAffix switch
        {
            EliteAffix.Giant => "巨大",
            EliteAffix.Swift => "俊足",
            EliteAffix.Splitter => "分裂",
            EliteAffix.Barrier => "障壁",
            _ => "",
        };

        /// <summary>ボス以外の敵からランダムに選ぶ(エリートにしないなら None)</summary>
        public static EliteAffix Roll(EnemyProfile iProfile, float iChance, System.Random iRandom)
        {
            if (iProfile == null || iProfile.IsBoss || iProfile.Elite != EliteAffix.None || iChance <= 0f) return EliteAffix.None;
            if (iRandom.NextDouble() >= iChance) return EliteAffix.None;
            return (EliteAffix)iRandom.Next(1, 5);
        }

        /// <summary>エリートの複製を作る(使い終わったら破棄してよい)</summary>
        public static EnemyProfile Create(EnemyProfile iSource, EliteAffix iAffix)
        {
            var aElite = Object.Instantiate(iSource);
            aElite.name = iSource.name + "_Elite_" + iAffix;
            aElite.Elite = iAffix;
            aElite.EliteSource = iSource;
            aElite.ShardValue = iSource.ShardValue * ShardMultiplier;
            aElite.Color = Color.Lerp(iSource.Color, new Color(1f, 0.82f, 0.3f), 0.35f);
            switch (iAffix)
            {
                case EliteAffix.Giant:
                    aElite.Scale = iSource.Scale * 1.45f;
                    aElite.MaxHealth = iSource.MaxHealth * 2.5f;
                    aElite.StaggerThreshold = iSource.StaggerThreshold * 2f;
                    aElite.Heavy = true;
                    aElite.SkillDamageScale = iSource.SkillDamageScale * 1.4f;
                    foreach (var aAttack in aElite.Attacks) aAttack.Damage *= 1.4f;
                    break;
                case EliteAffix.Swift:
                    aElite.MoveSpeed = iSource.MoveSpeed * 1.6f;
                    aElite.TurnSpeed = iSource.TurnSpeed * 1.5f;
                    aElite.AttackCooldown = iSource.AttackCooldown * 0.55f;
                    aElite.SkillWindupScale = iSource.SkillWindupScale * 0.85f;
                    foreach (var aAttack in aElite.Attacks) aAttack.Windup *= 0.8f;
                    break;
                case EliteAffix.Splitter:
                    aElite.MaxHealth = iSource.MaxHealth * 1.4f;
                    break;
                case EliteAffix.Barrier:
                    aElite.MaxArmor = iSource.MaxArmor + 80f;
                    aElite.ArmorRegenDelay = 4f;
                    aElite.MaxHealth = iSource.MaxHealth * 1.3f;
                    aElite.Color = Color.Lerp(iSource.Color, new Color(0.5f, 0.8f, 1f), 0.4f);
                    break;
            }
            return aElite;
        }

        /// <summary>分裂したときの小さな分身</summary>
        public static EnemyProfile CreateSplit(EnemyProfile iSource)
        {
            var aSplit = Object.Instantiate(iSource);
            aSplit.name = iSource.name + "_Split";
            aSplit.DisplayName = iSource.DisplayName + "の分身";
            aSplit.Elite = EliteAffix.None;
            aSplit.EliteSource = null;
            aSplit.Scale = iSource.Scale * 0.65f;
            aSplit.MaxHealth = Mathf.Max(10f, iSource.MaxHealth * 0.3f);
            aSplit.MaxArmor = 0f;
            aSplit.MoveSpeed = iSource.MoveSpeed * 1.2f;
            aSplit.ShardValue = iSource.ShardValue * 0.3f;
            return aSplit;
        }
    }
}
