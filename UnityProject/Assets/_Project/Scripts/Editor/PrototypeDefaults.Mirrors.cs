using System.Collections.Generic;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// プレイヤーと同じスキルで戦う敵「写し身」。武器種ごとの型を2つ持ち、戦闘中に切り替える(切り替えると弱点も変わる)。
    /// 攻撃はスキルから作るので、attacks は空にしておく。
    /// </summary>
    static partial class PrototypeDefaults
    {
        static readonly Color MirrorColor = new Color(0.82f, 0.84f, 0.92f);
        static readonly Color MirrorTelegraph = new Color(0.85f, 0.45f, 1f);

        static void Mirror(EnemyProfile p, string name, float health, float speed, params EnemySkillSet[] sets)
        {
            p.displayName = name;
            p.maxHealth = health;
            p.staggerThreshold = 6f;
            p.staggerDuration = 0.45f;
            p.moveSpeed = speed;
            p.attackCooldown = 1.1f;
            p.color = MirrorColor;
            p.telegraphColor = MirrorTelegraph;
            p.shardValue = 2.5f;
            p.attacks = new List<EnemyAttack>();
            p.weaknesses = new WeaponType[0];
            p.skillSets = new List<EnemySkillSet>(sets);
            p.skillSwapInterval = 7f;
            p.skillDamageScale = 0.5f;
            p.skillWindupScale = 2.2f;
        }

        public static void MirrorBlade(EnemyProfile p, EnemySkillSet sword, EnemySkillSet hammer) =>
            Mirror(p, "写し身・剣槌", 170f, 4.5f, sword, hammer);

        public static void MirrorCaster(EnemyProfile p, EnemySkillSet staff, EnemySkillSet gun)
        {
            Mirror(p, "写し身・杖銃", 130f, 3.8f, staff, gun);
            // 遠くから撃つ型なので、近づかれると下がる
            p.preferredDistance = 7f;
        }

        public static void MirrorBrawler(EnemyProfile p, EnemySkillSet gauntlet, EnemySkillSet chain)
        {
            Mirror(p, "写し身・拳鎖", 150f, 5f, gauntlet, chain);
            p.skillSwapInterval = 6f;
        }

        public static void Act5(StageData s, EnemyProfile grunt, EnemyProfile shooter, EnemyProfile blade, EnemyProfile caster,
            EnemyProfile brawler, EnemyProfile shadow)
        {
            s.displayName = "第5幕  写し身";
            s.description = "こちらと同じスキルを使う「写し身」が現れる。型を切り替えると弱点も変わるので、頭上の弱点を見て持ち替えよう。";
            s.kind = StageKind.Waves;
            s.waves = new[]
            {
                Wave("WAVE", blade, grunt, grunt),
                Wave("WAVE", caster, blade, shooter),
                Wave("WAVE", brawler, brawler, caster),
                Wave("BOSS", shadow, blade),
            };
        }
    }
}
