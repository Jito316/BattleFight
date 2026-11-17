using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 大剣のたたき台。振りは遅いが、1 発でひるませてアーマーを大きく削る、いちばん重い近接武器。
    /// 攻撃B は長く溜めて振り下ろす技と、地面を裂いて前へ衝撃を走らせる技。移動は敵を崩す肩の体当たり。
    /// </summary>
    static partial class PrototypeDefaults
    {
        public static void GreatswordCombo(SkillData iSkill)
        {
            // 剣の連斬より遅いが、1 回ごとのひるみ・アーマー削りが大きい。3 回目はとても重い
            Setup(iSkill, "豪剣", "重い 3 回の振り。1 回ごとに敵を大きくひるませ、アーマーを削る。振りは遅い。", SlotType.AttackA,
                WeaponType.Greatsword, SkillBehavior.Standard,
                Step(14, 6, 22, 20, 35, 15, 28, new Vector3(0, 1, 1.6f), 1.8f, 0.8f, 4f, iHitstop: 0.08f),
                Step(14, 6, 22, 20, 35, 15, 28, new Vector3(0, 1, 1.6f), 1.8f, 0.8f, 4f, iHitstop: 0.08f),
                Step(20, 7, 32, 38, 70, 35, 45, new Vector3(0, 1, 1.7f), 2.1f, 1.0f, 9f, iHitstop: 0.12f, iCancel: 18));
        }

        public static void GreatswordCharge(SkillData iSkill)
        {
            // 居合と同じ仕組み(長押しで溜め、溜め中に受けるとカウンター)。居合より溜めが長く、突進せずにその場で振り下ろす
            Setup(iSkill, "剛断", "長押しで大きく溜め、離すとその場で振り下ろす。アーマーをまとめて削る。溜め中に攻撃を受けるとカウンター。",
                SlotType.AttackB, WeaponType.Greatsword, SkillBehavior.ChargeCounter,
                Step(6, 6, 30, 40, 90, 60, 60, new Vector3(0, 1, 1.8f), 2.0f, 1.0f, 10f, iHitstop: 0.14f, iCancel: 20));
            iSkill.ChargeFrames = 75;
        }

        public static void GreatswordRift(SkillData iSkill)
        {
            // レーザーと同じ仕組み(前へまっすぐの当たり)。1 回だけの太く短い衝撃
            Setup(iSkill, "地裂き", "大剣を地面に叩きつけ、前へまっすぐ衝撃を走らせる。並んだ敵をまとめて斬る。", SlotType.AttackB,
                WeaponType.Greatsword, SkillBehavior.Beam,
                Step(18, 6, 28, 24, 40, 20, 34, mNone, 0f, 0.3f, 6f, iHitstop: 0.08f, iCancel: 16));
            iSkill.BeamLength = 9f;
            iSkill.BeamRadius = 0.9f;
        }

        public static void GreatswordTackle(SkillData iSkill)
        {
            // ステップ斬りより短く無敵も短いが、当てた敵を大きくひるませ、アーマーを削る
            Setup(iSkill, "肩当て", "前へ踏み込んで肩から体当たりする。当てた敵を大きくひるませる(無敵は短い)。", SlotType.Movement,
                WeaponType.Greatsword, SkillBehavior.DashStrike,
                Step(4, 10, 14, 10, 40, 25, 18, new Vector3(0, 1, 0.6f), 1.2f, 5.0f, 8f, iHitstop: 0.06f, iCancel: 6));
            iSkill.InvulnerableFrames = 8;
            iSkill.AirGravityScale = 0f;
        }

        public static void GreatswordFinisher(SkillData iSkill)
        {
            var aSteps = new SkillStep[5];
            for (int aIndex = 0; aIndex < 4; aIndex++)
            {
                aSteps[aIndex] = Step(aIndex == 0 ? 10 : 6, 5, 4, 24, 40, 20, 24, new Vector3(0, 1, 1.6f), 2.0f, 0.4f, 3f, iHitstop: 0.08f);
            }
            aSteps[4] = Step(16, 7, 36, 70, 120, 60, 60, new Vector3(0, 1, 1.8f), 2.8f, 0.6f, 16f, iLaunch: 5f, iHitstop: 0.18f);
            Setup(iSkill, "天地両断", "大剣マスタリーのフィニッシャー。重い 4 回の振りのあと、天から地へ一刀で叩き斬る。",
                SlotType.AttackA, WeaponType.Greatsword, SkillBehavior.Standard, aSteps);
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 60;
        }

        public static void Greatsword(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Greatsword;
            iWeapon.DisplayName = "大剣";
            iWeapon.Color = new Color(0.95f, 0.55f, 0.3f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            // 短い柄に、幅広く長い刃
            iWeapon.HeadScale = new Vector3(0.1f, 0.38f, 1.7f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 1.15f);
            iWeapon.HeadRotation = Vector3.zero;
            iWeapon.HandleLength = 0.4f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.2f;
        }
    }
}
