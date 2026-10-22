using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>技の1段分。フレーム数は 60fps 基準。</summary>
    [Serializable]
    public class SkillStep
    {
        [Header("フレーム(60fps基準)")]
        [Min(0)] [FormerlySerializedAs("startupFrames")] public int StartupFrames = 8;
        [Min(0)] [FormerlySerializedAs("activeFrames")] public int ActiveFrames = 4;
        [Min(0)] [FormerlySerializedAs("recoveryFrames")] public int RecoveryFrames = 14;
        [Tooltip("硬直開始から何フレーム目以降に次の段へつなぐか")]
        [Min(0)] [FormerlySerializedAs("chainFrame")] public int ChainFrame;
        [Tooltip("硬直開始から何フレーム目以降に、他の技やジャンプでキャンセルできるか")]
        [Min(0)] [FormerlySerializedAs("cancelFrame")] public int CancelFrame = 10;

        [Header("ヒット")]
        [FormerlySerializedAs("damage")] public float Damage = 10f;
        [FormerlySerializedAs("stagger")] public float Stagger = 10f;
        [FormerlySerializedAs("armorBreak")] public float ArmorBreak = 5f;
        [FormerlySerializedAs("stylePoints")] public float StylePoints = 20f;
        [FormerlySerializedAs("knockback")] public float Knockback = 2f;
        [Tooltip("0より大きいと敵を打ち上げる(上向きの速度)")]
        [FormerlySerializedAs("launch")] public float Launch;
        [FormerlySerializedAs("hitstop")] public float Hitstop = 0.04f;

        [Header("判定・移動")]
        [Tooltip("プレイヤー基準のローカル座標")]
        [FormerlySerializedAs("hitboxOffset")] public Vector3 HitboxOffset = new Vector3(0f, 1f, 1.2f);
        [Tooltip("0以下なら攻撃判定なし")]
        [FormerlySerializedAs("hitboxRadius")] public float HitboxRadius = 1.2f;
        [Tooltip("開始〜攻撃判定の間に前進する距離")]
        [FormerlySerializedAs("forwardMove")] public float ForwardMove = 0.5f;
    }

    [CreateAssetMenu(menuName = "BattleFight/Skill Data", fileName = "Skill_")]
    public class SkillData : ScriptableObject
    {
        [FormerlySerializedAs("displayName")] public string DisplayName = "新しいスキル";
        [TextArea] [FormerlySerializedAs("description")] public string Description;
        [FormerlySerializedAs("slot")] public SlotType Slot;
        [FormerlySerializedAs("weapon")] public WeaponType Weapon;
        [FormerlySerializedAs("behavior")] public SkillBehavior Behavior = SkillBehavior.Standard;

        [Header("共通")]
        [FormerlySerializedAs("usableInAir")] public bool UsableInAir = true;
        [Tooltip("段が自動で進む(フィニッシャー用)")]
        [FormerlySerializedAs("autoChain")] public bool AutoChain;
        [Tooltip("硬直開始から何フレーム目以降にスワップキャンセルできるか")]
        [Min(0)] [FormerlySerializedAs("swapCancelFrame")] public int SwapCancelFrame;
        [Tooltip("発動から何フレームの間、無敵になるか")]
        [Min(0)] [FormerlySerializedAs("invulnerableFrames")] public int InvulnerableFrames;
        [Tooltip("空中で使ったときの重力倍率(DMC風の空中停滞)")]
        [FormerlySerializedAs("airGravityScale")] public float AirGravityScale = 0.15f;

        [Header("挙動ごとのパラメータ")]
        [Tooltip("Grapple / Pull: 対象を探す距離")]
        [FormerlySerializedAs("range")] public float Range;
        [Tooltip("HammerJump: 跳んでいる間の前進距離 / Grapple: 対象がないときの空中ダッシュ距離")]
        [FormerlySerializedAs("dashDistance")] public float DashDistance;
        [Tooltip("HammerJump: 跳ぶ速度")]
        [FormerlySerializedAs("jumpVelocity")] public float JumpVelocity;
        [Tooltip("ChargeCounter: 最大溜めフレーム")]
        [Min(0)] [FormerlySerializedAs("chargeFrames")] public int ChargeFrames;
        [Tooltip("Grapple / Pull: 移動速度")]
        [FormerlySerializedAs("moveSpeed")] public float MoveSpeed;

        [Header("飛び道具(Projectile / TargetedStrike)")]
        [Min(1)] [FormerlySerializedAs("projectileCount")] public int ProjectileCount = 1;
        [Tooltip("複数発のときの広がり(度)。360 で全方位")]
        [FormerlySerializedAs("projectileSpread")] public float ProjectileSpread;
        [FormerlySerializedAs("projectileSpeed")] public float ProjectileSpeed = 25f;
        [FormerlySerializedAs("projectileLifetime")] public float ProjectileLifetime = 1.2f;
        [FormerlySerializedAs("projectileRadius")] public float ProjectileRadius = 0.35f;
        [Tooltip("敵を貫通する")]
        [FormerlySerializedAs("projectilePierce")] public bool ProjectilePierce;
        [Tooltip("0より大きいと、当たったとき(寿命が尽きたとき)にこの半径で爆発する")]
        [FormerlySerializedAs("explosionRadius")] public float ExplosionRadius;
        [Tooltip("ロックオン対象へ曲がる速さ(度/秒)")]
        [FormerlySerializedAs("homing")] public float Homing;
        [Tooltip("TargetedStrike: 予告から攻撃までの秒数")]
        [FormerlySerializedAs("strikeDelay")] public float StrikeDelay = 0.35f;
        [Tooltip("TargetedStrike: ロックオンしていないとき、前方のこの距離に落とす")]
        [FormerlySerializedAs("strikeDistance")] public float StrikeDistance = 8f;

        [Header("ビーム(Beam)")]
        [FormerlySerializedAs("beamLength")] public float BeamLength = 14f;
        [FormerlySerializedAs("beamRadius")] public float BeamRadius = 0.5f;

        [FormerlySerializedAs("steps")] public List<SkillStep> Steps = new List<SkillStep> { new SkillStep() };

        public int StepCount => Steps.Count;

        public SkillStep GetStep(int iIndex) => Steps[Mathf.Clamp(iIndex, 0, Steps.Count - 1)];
    }
}
