using System.Collections.Generic;
using UnityEditor;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// スキルごとのモーション(Quaternius Universal Animation Library)。シーンを作るたびに上書きする。
    /// 構え: 溜め・投げてから届くまで・最初の段の出始めに流す / 段: 段ごとに流す(足りない段は最後のもの)。
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        static readonly Dictionary<string, (string Charge, string[] Steps)> mSkillMotions = new Dictionary<string, (string, string[])>
        {
            // 剣
            ["Skill_Sword_Combo"] = (null, new[] { "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C", "Sword_Regular_A" }),
            ["Skill_Sword_Finisher"] = (null, new[] { "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C", "Sword_Regular_B", "Sword_Attack" }),
            ["Skill_Sword_Iai"] = ("Sword_Block", new[] { "Sword_Dash" }),
            ["Skill_Sword_Thrust"] = (null, new[] { "Sword_Dash" }),
            ["Skill_Sword_Wave"] = (null, new[] { "Sword_Attack" }),
            ["Skill_Sword_Step"] = (null, new[] { "Sword_Dash" }),
            ["Skill_Sword_Phantom"] = (null, new[] { "Roll" }),
            // 大槌
            ["Skill_Hammer_Combo"] = (null, new[] { "Sword_Attack", "TreeChopping_Loop", "Sword_Heavy_Combo" }),
            ["Skill_Hammer_Quake"] = (null, new[] { "TreeChopping_Loop" }),
            ["Skill_Hammer_Spin"] = (null, new[] { "Sword_Heavy_Combo", "Sword_Regular_C", "Sword_Heavy_Combo" }),
            ["Skill_Hammer_Finisher"] = (null, new[] { "Sword_Attack" }),
            ["Skill_Hammer_Meteor"] = (null, new[] { "OverhandThrow" }),
            ["Skill_Hammer_Charge"] = (null, new[] { "Shield_Dash" }),
            ["Skill_Hammer_Jump"] = (null, new[] { "NinjaJump_Start", "Sword_Attack" }),
            // 鎖
            ["Skill_Chain_Combo"] = (null, new[] { "Melee_Hook", "Sword_Regular_B", "Sword_Regular_C" }),
            ["Skill_Chain_Ring"] = (null, new[] { "Sword_Regular_Combo" }),
            ["Skill_Chain_Finisher"] = (null, new[] { "Melee_Hook", "Sword_Regular_B", "Sword_Regular_C", "OverhandThrow" }),
            ["Skill_Chain_Bind"] = (null, new[] { "OverhandThrow" }),
            ["Skill_Chain_Pull"] = ("OverhandThrow", new[] { "Melee_Hook" }),
            ["Skill_Chain_Wire"] = ("OverhandThrow", new[] { "Jump_Loop" }),
            // 杖
            ["Skill_Staff_Melee"] = (null, new[] { "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C" }),
            ["Skill_Staff_Bolt"] = (null, new[] { "Spell_Simple_Shoot" }),
            ["Skill_Staff_Fireball"] = (null, new[] { "Spell_Simple_Shoot" }),
            ["Skill_Staff_Frost"] = ("Spell_Simple_Enter", new[] { "Spell_Simple_Shoot" }),
            ["Skill_Staff_Thunder"] = ("Spell_Simple_Enter", new[] { "Spell_Simple_Shoot" }),
            ["Skill_Staff_Finisher"] = ("Spell_Simple_Enter", new[] { "Spell_Simple_Shoot" }),
            ["Skill_Staff_Blink"] = (null, new[] { "Roll" }),
            ["Skill_Staff_Levitate"] = (null, new[] { "NinjaJump_Start" }),
            // 銃
            ["Skill_Gun_Rapid"] = (null, new[] { "Pistol_Shoot" }),
            ["Skill_Gun_Shotgun"] = (null, new[] { "Pistol_Shoot" }),
            ["Skill_Gun_Finisher"] = (null, new[] { "Pistol_Shoot" }),
            ["Skill_Gun_Laser"] = (null, new[] { "Pistol_Aim_Neutral" }),
            ["Skill_Gun_Grenade"] = (null, new[] { "OverhandThrow" }),
            ["Skill_Gun_Slide"] = (null, new[] { "Slide_Start" }),
            ["Skill_Gun_Jet"] = (null, new[] { "Jump_Start" }),
            // 籠手
            ["Skill_Gauntlet_Rush"] = (null, new[] { "Punch_Jab", "Punch_Cross" }),
            ["Skill_Gauntlet_Finisher"] = (null, new[] { "Punch_Jab", "Punch_Cross" }),
            ["Skill_Gauntlet_Uppercut"] = ("Melee_Hook", new[] { "NinjaJump_Start" }),
            ["Skill_Gauntlet_Rocket"] = (null, new[] { "Punch_Cross" }),
            ["Skill_Gauntlet_Dash"] = (null, new[] { "Shield_Dash" }),
        };

        /// <summary>表のモーションをスキルに書き込む。段の数より短い表は、最後のモーションを繰り返す</summary>
        static void EnsureSkillMotions()
        {
            foreach (var aSkill in AllSkills())
            {
                if (aSkill == null || !mSkillMotions.TryGetValue(aSkill.name, out var aEntry)) continue;
                var aSteps = new string[aSkill.StepCount];
                for (int aStepIndex = 0; aStepIndex < aSteps.Length; aStepIndex++) aSteps[aStepIndex] = aEntry.Steps[System.Math.Min(aStepIndex, aEntry.Steps.Length - 1)];
                aSkill.ChargeMotion = aEntry.Charge;
                aSkill.Motions = aSteps;
                EditorUtility.SetDirty(aSkill);
            }
        }

        /// <summary>データにあるすべてのスキル(フィニッシャーも含む)</summary>
        static IEnumerable<SkillData> AllSkills()
        {
            foreach (var aGuid in AssetDatabase.FindAssets("t:SkillData", new[] { mDataRoot }))
            {
                var aSkill = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(aGuid));
                if (aSkill != null) yield return aSkill;
            }
        }

        /// <summary>スキルが使うモーションの名前(アニメーターに状態を用意する)</summary>
        static IEnumerable<string> SkillMotionNames()
        {
            foreach (var aSkill in AllSkills())
            {
                if (aSkill == null) continue;
                if (aSkill.HasChargeMotion) yield return aSkill.ChargeMotion;
                if (aSkill.Motions == null) continue;
                foreach (var aMotion in aSkill.Motions)
                {
                    if (!string.IsNullOrEmpty(aMotion)) yield return aMotion;
                }
            }
        }
    }
}
