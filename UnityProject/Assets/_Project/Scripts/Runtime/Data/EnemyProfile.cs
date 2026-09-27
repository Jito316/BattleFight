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
    }

    [CreateAssetMenu(menuName = "BattleFight/Enemy Profile", fileName = "Enemy_")]
    public class EnemyProfile : ScriptableObject
    {
        public string displayName = "敵";
        public bool isBoss;
        [Tooltip("引き寄せで動かせない(自分が飛びつく)")]
        public bool heavy;

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

        [Header("攻撃")]
        public float attackCooldown = 1.2f;
        public List<EnemyAttack> attacks = new List<EnemyAttack> { new EnemyAttack() };

        [Header("見た目・報酬")]
        public float scale = 1f;
        public Color color = new Color(0.45f, 0.5f, 0.6f);
        public Color telegraphColor = new Color(1f, 0.3f, 0.1f);
        [Tooltip("倒したときの刻片(基本値に対する倍率)")]
        public float shardValue = 1f;
    }
}
