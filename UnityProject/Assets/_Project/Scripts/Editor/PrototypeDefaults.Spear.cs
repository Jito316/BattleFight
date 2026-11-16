using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 槍のたたき台。遠くまで届く突きの武器。双剣とは反対に、手数は少ないが 1 発が重く、敵を押し返して間合いを取る。
    /// 移動は槍を突いて前へ高く跳ぶ(敵や段差を越えられる)。
    /// </summary>
    static partial class PrototypeDefaults
    {
        public static void SpearCombo(SkillData iSkill)
        {
            // 剣より遠く(体の前 2.3m まで)届く、細い突き。3 回目は重い
            Setup(iSkill, "三段突き", "遠くまで届く 3 回の突き。最後の突きは重く、敵を押し返す。", SlotType.AttackA, WeaponType.Spear,
                SkillBehavior.Standard,
                Step(8, 4, 14, 11, 16, 6, 20, new Vector3(0, 1, 2.3f), 0.95f, 0.6f, 2f, iHitstop: 0.04f),
                Step(8, 4, 14, 11, 16, 6, 20, new Vector3(0, 1, 2.3f), 0.95f, 0.6f, 2f, iHitstop: 0.04f),
                Step(12, 5, 24, 22, 38, 14, 32, new Vector3(0, 1, 2.6f), 1.1f, 1.0f, 7f, iHitstop: 0.08f, iCancel: 14));
        }

        public static void SpearSweep(SkillData iSkill)
        {
            Setup(iSkill, "薙ぎ払い", "槍を大きく振って前を広く払い、敵を押し返す。間合いを取り直すときに。", SlotType.AttackB, WeaponType.Spear,
                SkillBehavior.Standard,
                Step(12, 6, 24, 18, 40, 12, 28, new Vector3(0, 1, 1.4f), 2.5f, 0.2f, 10f, iHitstop: 0.07f, iCancel: 12));
        }

        public static void SpearJavelin(SkillData iSkill)
        {
            // 飛燕斬より遅いが重く、遠くまで飛ぶ。構えが長い代わりに、当たった敵を大きく押し返す
            Setup(iSkill, "投げ槍", "槍を投げる。遠くまでまっすぐ飛び、敵を貫いて押し返す。構えは長い。", SlotType.AttackB, WeaponType.Spear,
                SkillBehavior.Projectile,
                Step(16, 2, 26, 26, 42, 16, 34, mNone, 0f, 0.2f, 8f, iHitstop: 0.08f));
            ProjectileSettings(iSkill, 36f, 1.0f, 0.4f, iPierce: true);
        }

        public static void SpearVault(SkillData iSkill)
        {
            // 大槌のジャンプと同じ仕組み(1 段目で跳ぶ)。着地の段は攻撃しない
            Setup(iSkill, "棒高跳び", "槍を地面に突いて、前へ高く跳ぶ。敵や段差を飛び越えられる(跳び始めに無敵あり)。", SlotType.Movement,
                WeaponType.Spear, SkillBehavior.HammerJump,
                Step(4, 1, 0, 0, 0, 0, 0, Vector3.zero, 0f, 0f),
                Step(0, 2, 8, 0, 0, 0, 0, Vector3.zero, 0f, 0f, iCancel: 3));
            iSkill.UsableInAir = false;
            iSkill.JumpVelocity = 13f;
            iSkill.DashDistance = 7f;
            iSkill.InvulnerableFrames = 12;
        }

        public static void SpearFinisher(SkillData iSkill)
        {
            var aSteps = new SkillStep[13];
            for (int aIndex = 0; aIndex < 12; aIndex++)
            {
                aSteps[aIndex] = Step(aIndex == 0 ? 6 : 1, 2, 1, 5, 10, 5, 9, new Vector3(0, 1, 2.2f), 1.1f, 0.05f, 0.5f, iHitstop: 0.015f);
            }
            aSteps[12] = Step(10, 5, 30, 34, 100, 40, 50, new Vector3(0, 1, 3.2f), 1.5f, 1.5f, 14f, iHitstop: 0.14f);
            Setup(iSkill, "千本突き", "槍マスタリーのフィニッシャー。12 回の速い突きのあと、遠くまで届く大きな突きでとどめを刺す。", SlotType.AttackA,
                WeaponType.Spear, SkillBehavior.Standard, aSteps);
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 60;
        }

        public static void Spear(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Spear;
            iWeapon.DisplayName = "槍";
            iWeapon.Color = new Color(0.85f, 0.88f, 0.95f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            iWeapon.HeadScale = new Vector3(0.12f, 0.03f, 0.4f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 2.0f);
            iWeapon.HeadRotation = new Vector3(0f, 0f, 45f);
            iWeapon.HandleLength = 1.8f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.15f;
        }
    }
}
