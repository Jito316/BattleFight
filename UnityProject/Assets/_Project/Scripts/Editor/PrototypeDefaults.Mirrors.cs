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
        static readonly Color mMirrorColor = new Color(0.82f, 0.84f, 0.92f);
        static readonly Color mMirrorTelegraph = new Color(0.85f, 0.45f, 1f);

        static void Mirror(EnemyProfile iProfile, string iName, float iHealth, float iSpeed, params EnemySkillSet[] iSets)
        {
            iProfile.DisplayName = iName;
            iProfile.MaxHealth = iHealth;
            iProfile.StaggerThreshold = 6f;
            iProfile.StaggerDuration = 0.45f;
            iProfile.MoveSpeed = iSpeed;
            iProfile.AttackCooldown = 1.1f;
            iProfile.Color = mMirrorColor;
            iProfile.TelegraphColor = mMirrorTelegraph;
            iProfile.ShardValue = 2.5f;
            iProfile.Attacks = new List<EnemyAttack>();
            iProfile.Weaknesses = new WeaponType[0];
            iProfile.SkillSets = new List<EnemySkillSet>(iSets);
            iProfile.SkillSwapInterval = 7f;
            iProfile.SkillDamageScale = 0.5f;
            iProfile.SkillWindupScale = 2.2f;
        }

        public static void MirrorBlade(EnemyProfile iProfile, EnemySkillSet iSword, EnemySkillSet iHammer) =>
            Mirror(iProfile, "写し身・剣槌", 170f, 4.5f, iSword, iHammer);

        public static void MirrorCaster(EnemyProfile iProfile, EnemySkillSet iStaff, EnemySkillSet iGun)
        {
            Mirror(iProfile, "写し身・杖銃", 130f, 3.8f, iStaff, iGun);
            // 遠くから撃つ型なので、近づかれると下がる
            iProfile.PreferredDistance = 7f;
        }

        public static void MirrorBrawler(EnemyProfile iProfile, EnemySkillSet iGauntlet, EnemySkillSet iChain)
        {
            Mirror(iProfile, "写し身・拳鎖", 150f, 5f, iGauntlet, iChain);
            iProfile.SkillSwapInterval = 6f;
        }

        public static void Act5(StageData iStage, EnemyProfile iGrunt, EnemyProfile iShooter, EnemyProfile iBlade, EnemyProfile iCaster,
            EnemyProfile iBrawler, EnemyProfile iShadow)
        {
            iStage.DisplayName = "第5幕  写し身";
            iStage.Description = "こちらと同じスキルを使う「写し身」が現れる。型を切り替えると弱点も変わるので、頭上の弱点を見て持ち替えよう。";
            iStage.Kind = StageKind.Waves;
            iStage.Waves = new[]
            {
                Wave("WAVE", iBlade, iGrunt, iGrunt),
                Wave("WAVE", iCaster, iBlade, iShooter),
                Wave("WAVE", iBrawler, iBrawler, iCaster),
                Wave("BOSS", iShadow, iBlade),
            };
        }
    }
}
