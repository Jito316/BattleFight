using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    [Serializable]
    public class EnemyAttack
    {
        public string name = "攻撃";
        [Tooltip("この距離以内で攻撃を始める")]
        public float range = 2f;
        public float windup = 0.7f;
        public float active = 0.12f;
        public float recovery = 0.9f;
        public float damage = 10f;
        [Tooltip("敵のスケールが掛かる")]
        public float radius = 1.2f;
        [Tooltip("敵のスケールが掛かる")]
        public Vector3 offset = new Vector3(0f, 1f, 1.2f);
        public float knockback = 5f;
        [Tooltip("攻撃判定中に前進する距離")]
        public float lunge;
        [Tooltip("予備動作で跳び、着地時に攻撃する")]
        public bool jumpSlam;
        public float weight = 1f;

        [Header("遠距離(弾を撃つ)")]
        public bool ranged;
        [Min(1)] public int projectileCount = 1;
        public float projectileSpread;
        public float projectileSpeed = 16f;
        public float projectileRadius = 0.35f;

        [Header("特殊")]
        [Tooltip("プレイヤーの足元に予告を出し、少し遅れて範囲攻撃を落とす")]
        public bool targetedStrike;
        public float strikeDelay = 0.8f;
        [Tooltip("攻撃すると自分も倒れる(爆弾兵)")]
        public bool selfDestruct;
        [Tooltip("手下を呼ぶ")]
        public EnemyProfile summon;
        [Min(0)] public int summonCount;
        [Tooltip("予備動作の途中で消え、プレイヤーの背後に現れてから攻撃する")]
        public bool teleportBehind;
        [Tooltip("同じ攻撃を続けて出す回数(三連斬なら 2)")]
        [Min(0)] public int repeat;
        [Tooltip("続けて出すときの予備動作(秒)")]
        public float repeatWindup = 0.3f;
    }

    [CreateAssetMenu(menuName = "BattleFight/Enemy Profile", fileName = "Enemy_")]
    public class EnemyProfile : ScriptableObject
    {
        public string displayName = "敵";
        public bool isBoss;
        [Tooltip("引き寄せで動かせない(自分が飛びつく)")]
        public bool heavy;

        [Header("弱点")]
        [Tooltip("この武器種の技で攻撃されると、ダメージとひるみが増える(頭上に表示される)")]
        public WeaponType[] weaknesses = new WeaponType[0];

        public bool IsWeakTo(WeaponType weapon)
        {
            if (weaknesses == null) return false;
            foreach (var w in weaknesses)
            {
                if (w == weapon) return true;
            }
            return false;
        }

        [Header("体力・アーマー")]
        public float maxHealth = 100f;
        public float maxArmor;
        public float armorRegenDelay = 6f;
        [Tooltip("この値以上のひるみ値でひるむ")]
        public float staggerThreshold = 5f;
        public float staggerDuration = 0.45f;
        public float armorBreakStagger = 1.4f;

        [Header("移動")]
        public float moveSpeed = 3.5f;
        public float turnSpeed = 360f;
        [Tooltip("この距離より近づかれると下がる(射手など)。0 なら下がらない")]
        public float preferredDistance;
        [Tooltip("空を飛ぶ(重力を受けず、地面からこの高さに浮く)")]
        public bool flying;
        public float hoverHeight = 3.5f;

        [Header("攻撃")]
        public float attackCooldown = 1.2f;
        public List<EnemyAttack> attacks = new List<EnemyAttack> { new EnemyAttack() };

        [Header("第二形態(ボス)")]
        [Tooltip("体力がこの割合を下回ると第二形態になる。0 なら第二形態なし")]
        [Range(0f, 1f)] public float phase2HealthRatio;
        public float phase2SpeedMultiplier = 1.3f;
        [Tooltip("攻撃の間隔に掛かる(小さいほど攻撃が多い)")]
        public float phase2CooldownMultiplier = 0.6f;
        [Tooltip("第二形態で増える攻撃")]
        public List<EnemyAttack> phase2Attacks = new List<EnemyAttack>();
        public string phase2Message = "本気になった!";

        [Header("見た目・報酬")]
        public PrimitiveType bodyShape = PrimitiveType.Capsule;
        public float scale = 1f;
        public Color color = new Color(0.45f, 0.5f, 0.6f);
        public Color telegraphColor = new Color(1f, 0.3f, 0.1f);
        [Tooltip("倒したときの刻片(基本値に対する倍率)")]
        public float shardValue = 1f;
    }
}
