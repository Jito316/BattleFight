using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>技の1段分。フレーム数は 60fps 基準。</summary>
    [Serializable]
    public class SkillStep
    {
        [Header("フレーム(60fps基準)")]
        [Min(0)] public int startupFrames = 8;
        [Min(0)] public int activeFrames = 4;
        [Min(0)] public int recoveryFrames = 14;
        [Tooltip("硬直開始から何フレーム目以降に次の段へつなぐか")]
        [Min(0)] public int chainFrame;
        [Tooltip("硬直開始から何フレーム目以降に、他の技やジャンプでキャンセルできるか")]
        [Min(0)] public int cancelFrame = 10;

        [Header("ヒット")]
        public float damage = 10f;
        public float stagger = 10f;
        public float armorBreak = 5f;
        public float stylePoints = 20f;
        public float knockback = 2f;
        [Tooltip("0より大きいと敵を打ち上げる(上向きの速度)")]
        public float launch;
        public float hitstop = 0.04f;

        [Header("判定・移動")]
        [Tooltip("プレイヤー基準のローカル座標")]
        public Vector3 hitboxOffset = new Vector3(0f, 1f, 1.2f);
        [Tooltip("0以下なら攻撃判定なし")]
        public float hitboxRadius = 1.2f;
        [Tooltip("開始〜攻撃判定の間に前進する距離")]
        public float forwardMove = 0.5f;
    }

    [CreateAssetMenu(menuName = "BattleFight/Skill Data", fileName = "Skill_")]
    public class SkillData : ScriptableObject
    {
        public string displayName = "新しいスキル";
        [TextArea] public string description;
        public SlotType slot;
        public WeaponType weapon;
        public SkillBehavior behavior = SkillBehavior.Standard;

        [Header("共通")]
        public bool usableInAir = true;
        [Tooltip("段が自動で進む(フィニッシャー用)")]
        public bool autoChain;
        [Tooltip("硬直開始から何フレーム目以降にスワップキャンセルできるか")]
        [Min(0)] public int swapCancelFrame;
        [Tooltip("発動から何フレームの間、無敵になるか")]
        [Min(0)] public int invulnerableFrames;
        [Tooltip("空中で使ったときの重力倍率(DMC風の空中停滞)")]
        public float airGravityScale = 0.15f;

        [Header("挙動ごとのパラメータ")]
        [Tooltip("Grapple / Pull: 対象を探す距離")]
        public float range;
        [Tooltip("HammerJump: 跳んでいる間の前進距離 / Grapple: 対象がないときの空中ダッシュ距離")]
        public float dashDistance;
        [Tooltip("HammerJump: 跳ぶ速度")]
        public float jumpVelocity;
        [Tooltip("ChargeCounter: 最大溜めフレーム")]
        [Min(0)] public int chargeFrames;
        [Tooltip("Grapple / Pull: 移動速度")]
        public float moveSpeed;

        public List<SkillStep> steps = new List<SkillStep> { new SkillStep() };

        public int StepCount => steps.Count;

        public SkillStep GetStep(int index) => steps[Mathf.Clamp(index, 0, steps.Count - 1)];
    }
}
