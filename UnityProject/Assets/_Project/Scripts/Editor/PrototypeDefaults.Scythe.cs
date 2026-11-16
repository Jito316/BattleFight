using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 大鎌のたたき台。前を広く刈る、振りの遅い武器。槍とは反対に、当てた敵を手前へ引き寄せる(押し返しが負の値)。
    /// 攻撃B は遠くの敵の足元に大きな鎌を落とす技と、周りの敵をまとめて引き寄せる技。移動は無敵の長い突進。
    /// </summary>
    static partial class PrototypeDefaults
    {
        public static void ScytheReap(SkillData iSkill)
        {
            // 前を広く(半径 2m)刈る 3 回。当てた敵を手前へ引き寄せる。3 回目は重い
            Setup(iSkill, "刈り取り", "前を広く刈る 3 回の振り。当てた敵を手前へ引き寄せる。振りは遅い。", SlotType.AttackA, WeaponType.Scythe,
                SkillBehavior.Standard,
                Step(11, 5, 18, 12, 18, 6, 22, new Vector3(0, 1, 1.3f), 2.0f, 0.3f, -4f, iHitstop: 0.05f),
                Step(11, 5, 18, 12, 18, 6, 22, new Vector3(0, 1, 1.3f), 2.0f, 0.3f, -4f, iHitstop: 0.05f),
                Step(15, 6, 28, 24, 40, 14, 34, new Vector3(0, 1, 1.4f), 2.3f, 0.4f, -6f, iHitstop: 0.09f, iCancel: 16));
        }

        public static void ScytheDoom(SkillData iSkill)
        {
            // 落雷と同じ仕組み。遅れて大きな鎌が落ち、アーマーを大きく削る
            Setup(iSkill, "死神の鎌", "ロックオン対象(いなければ前方)の足元に、少し遅れて大きな鎌を振り下ろす。アーマーを大きく削る。",
                SlotType.AttackB, WeaponType.Scythe, SkillBehavior.TargetedStrike,
                Step(18, 2, 26, 34, 60, 45, 48, mNone, 0f, 0f, 2f, iLaunch: 4f, iHitstop: 0.12f));
            iSkill.ExplosionRadius = 2.2f;
            iSkill.StrikeDelay = 0.45f;
            iSkill.StrikeDistance = 8f;
        }

        public static void ScytheHarvest(SkillData iSkill)
        {
            // 周り 360° を刈り、離れた敵ほど強く手前へ寄せる(まとめてから刈り取りにつなぐ)
            Setup(iSkill, "円月刈り", "鎌を大きく一回転させて周りを刈り、周りの敵をまとめて手元へ引き寄せる。", SlotType.AttackB, WeaponType.Scythe,
                SkillBehavior.Standard,
                Step(14, 6, 26, 16, 30, 10, 30, new Vector3(0, 1, 0f), 3.4f, 0f, -12f, iHitstop: 0.07f, iCancel: 14));
        }

        public static void ScytheShade(SkillData iSkill)
        {
            // ステップ斬りより長く、無敵も長い。通り抜けた敵を浅く斬る
            Setup(iSkill, "影走り", "影のように低く長く駆け抜ける。無敵が長く、通り抜けた敵を斬る。", SlotType.Movement, WeaponType.Scythe,
                SkillBehavior.DashStrike,
                Step(3, 14, 12, 6, 12, 3, 14, new Vector3(0, 1, 0.5f), 1.3f, 7f, 1f, iCancel: 5));
            iSkill.InvulnerableFrames = 18;
            iSkill.AirGravityScale = 0f;
        }

        public static void ScytheFinisher(SkillData iSkill)
        {
            var aSteps = new SkillStep[7];
            for (int aIndex = 0; aIndex < 6; aIndex++)
            {
                aSteps[aIndex] = Step(aIndex == 0 ? 8 : 2, 3, 2, 10, 20, 8, 16, new Vector3(0, 1, 0f), 3.0f, 0f, -3f, iHitstop: 0.03f);
            }
            aSteps[6] = Step(12, 6, 34, 40, 100, 45, 55, new Vector3(0, 1, 0f), 4.2f, 0f, 14f, iLaunch: 6f, iHitstop: 0.15f);
            Setup(iSkill, "魂狩り", "大鎌マスタリーのフィニッシャー。周りを 6 回刈って敵を引き寄せ、最後の大きな一振りでまとめて吹き飛ばす。",
                SlotType.AttackA, WeaponType.Scythe, SkillBehavior.Standard, aSteps);
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 60;
        }

        public static void Scythe(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Scythe;
            iWeapon.DisplayName = "大鎌";
            iWeapon.Color = new Color(0.55f, 0.35f, 0.75f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            // 柄の先に、横へ張り出す反った刃
            iWeapon.HeadScale = new Vector3(0.8f, 0.03f, 0.14f);
            iWeapon.HeadOffset = new Vector3(0.35f, 0f, 1.7f);
            iWeapon.HeadRotation = new Vector3(0f, -20f, 0f);
            iWeapon.HandleLength = 1.7f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.15f;
        }
    }
}
