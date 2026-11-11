using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 双剣のたたき台。いちばん速く手数が多い武器。1 発は軽く、ひるみ・アーマー削りは弱いが、
    /// キャンセルが早く、切り替えやチェインにつなぎやすい。移動は敵の背後へ回り込む。
    /// </summary>
    static partial class PrototypeDefaults
    {
        public static void DualCombo(SkillData iSkill)
        {
            // 左右の刃で交互に 6 回。どの段も早くキャンセルできる。最後だけ少し重く
            var aSteps = new SkillStep[6];
            for (int aIndex = 0; aIndex < 5; aIndex++)
            {
                aSteps[aIndex] = Step(4, 3, 9, 5, 6, 2, 12, new Vector3(0, 1, 1.2f), 1.25f, 0.45f, 1f, iHitstop: 0.02f, iCancel: 3);
            }
            aSteps[5] = Step(6, 4, 18, 10, 18, 4, 22, new Vector3(0, 1, 1.3f), 1.4f, 0.8f, 3f, iHitstop: 0.05f, iCancel: 8);
            Setup(iSkill, "乱舞", "左右の刃でとても速く 6 回斬る。1 発は軽いが、どの段からもすぐ切り替えられる。", SlotType.AttackA,
                WeaponType.DualBlades, SkillBehavior.Standard, aSteps);
        }

        public static void DualSpin(SkillData iSkill)
        {
            // 一度押すと、周り 360° を 4 回斬る。空中ではほとんど落ちない
            var aSteps = new SkillStep[4];
            for (int aIndex = 0; aIndex < 3; aIndex++)
            {
                aSteps[aIndex] = Step(aIndex == 0 ? 7 : 2, 4, 2, 6, 10, 3, 14, new Vector3(0, 1, 0f), 2.3f, 0f, 2f, iHitstop: 0.03f);
            }
            aSteps[3] = Step(2, 5, 22, 9, 22, 5, 22, new Vector3(0, 1, 0f), 2.5f, 0f, 5f, iHitstop: 0.06f, iCancel: 10);
            Setup(iSkill, "回転斬り", "回りながら周りを 4 回斬る。囲まれたときに。空中で使うとその場にとどまる。", SlotType.AttackB,
                WeaponType.DualBlades, SkillBehavior.Standard, aSteps);
            iSkill.AutoChain = true;
            iSkill.AirGravityScale = 0.05f;
        }

        public static void DualCross(SkillData iSkill)
        {
            Setup(iSkill, "交差ステップ", "ロックオン中は、敵をすり抜けて背後へ回り込む(無敵あり)。ロックオンしていないときは入力の方向へ短く移動する。",
                SlotType.Movement, WeaponType.DualBlades, SkillBehavior.Blink,
                Step(0, 2, 8, 0, 0, 0, 0, mNone, 0f, 0f, iCancel: 3));
            iSkill.DashDistance = 4.5f;
            iSkill.BlinkBehindTarget = true;
            iSkill.InvulnerableFrames = 14;
            iSkill.AirGravityScale = 0f;
        }

        public static void DualFinisher(SkillData iSkill)
        {
            var aSteps = new SkillStep[11];
            for (int aIndex = 0; aIndex < 10; aIndex++)
            {
                aSteps[aIndex] = Step(aIndex == 0 ? 6 : 1, 2, 1, 5, 10, 4, 9, new Vector3(0, 1, 1.2f), 1.8f, 0.05f, 0.5f, iHitstop: 0.015f);
            }
            aSteps[10] = Step(8, 5, 30, 28, 90, 30, 50, new Vector3(0, 1, 1.4f), 2.2f, 1.2f, 10f, iHitstop: 0.13f);
            Setup(iSkill, "無尽", "双剣マスタリーのフィニッシャー。10 連撃のあと、交差斬りでとどめを刺す。", SlotType.AttackA,
                WeaponType.DualBlades, SkillBehavior.Standard, aSteps);
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 60;
        }

        public static void DualBlades(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.DualBlades;
            iWeapon.DisplayName = "双剣";
            iWeapon.Color = new Color(0.75f, 1f, 0.3f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            iWeapon.HeadScale = new Vector3(0.06f, 0.06f, 0.75f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 0.45f);
            iWeapon.HandleLength = 0.1f;
            // 左手にも同じ刃を持つ(右手の位置から左へ)
            iWeapon.SecondHeadOffset = new Vector3(-0.9f, 0f, 0f);
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.15f;
        }
    }
}
