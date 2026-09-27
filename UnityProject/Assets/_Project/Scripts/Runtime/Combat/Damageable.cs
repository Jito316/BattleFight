using System;
using UnityEngine;

namespace BattleFight
{
    public struct HitInfo
    {
        public float damage;
        public float stagger;
        public float armorBreak;
        public float knockback;
        public float launch;
        /// <summary>水平方向の、攻撃者から被弾者への向き</summary>
        public Vector3 direction;
        public GameObject source;
    }

    public enum HitOutcome
    {
        Ignored,
        /// <summary>無敵中だった</summary>
        Evaded,
        /// <summary>カウンターで受け止められた</summary>
        Countered,
        /// <summary>アーマーで受けた(ひるまない)</summary>
        Armored,
        ArmorBroken,
        Hit,
        Killed,
    }

    public static class HitOutcomeExtensions
    {
        /// <summary>ダメージが通ったか</summary>
        public static bool Landed(this HitOutcome outcome) => outcome >= HitOutcome.Armored;
    }

    /// <summary>体力とアーマー。プレイヤーと敵の両方が持つ。</summary>
    public class Damageable : MonoBehaviour
    {
        [SerializeField] Team team = Team.Enemy;
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float maxArmor;
        [SerializeField] float armorRegenDelay = 6f;
        [SerializeField, Range(0f, 1f)] float armoredDamageMultiplier = 0.4f;

        float armorBrokenTime;
        bool initialized;

        public Team Team => team;
        public float MaxHealth => maxHealth;
        public float Health { get; private set; }
        public float MaxArmor => maxArmor;
        public float Armor { get; private set; }
        public bool IsDead { get; private set; }
        public bool Invulnerable { get; set; }
        /// <summary>最後に受けた実ダメージ(アーマー軽減後)</summary>
        public float LastDamage { get; private set; }

        /// <summary>true を返すと、その攻撃をカウンターとして無効化する</summary>
        public Func<HitInfo, bool> CounterHandler { get; set; }

        public event Action<HitInfo, HitOutcome> Damaged;
        public event Action<HitInfo> Evaded;
        public event Action Died;

        void Awake()
        {
            if (!initialized) ResetState();
        }

        public void Configure(Team newTeam, float newMaxHealth, float newMaxArmor, float newArmorRegenDelay)
        {
            team = newTeam;
            maxHealth = newMaxHealth;
            maxArmor = newMaxArmor;
            armorRegenDelay = newArmorRegenDelay;
            ResetState();
        }

        public void ResetState()
        {
            Health = maxHealth;
            Armor = maxArmor;
            IsDead = false;
            initialized = true;
        }

        void Update()
        {
            if (maxArmor > 0f && Armor <= 0f && !IsDead && Time.time - armorBrokenTime >= armorRegenDelay)
            {
                Armor = maxArmor;
            }
        }

        /// <summary>攻撃によらず倒れる(自爆など)</summary>
        public void Kill()
        {
            if (IsDead) return;
            Health = 0f;
            IsDead = true;
            Died?.Invoke();
        }

        public HitOutcome ApplyHit(HitInfo hit)
        {
            if (IsDead) return HitOutcome.Ignored;
            if (Invulnerable)
            {
                Evaded?.Invoke(hit);
                return HitOutcome.Evaded;
            }
            if (CounterHandler != null && CounterHandler(hit)) return HitOutcome.Countered;

            float damage = hit.damage;
            var outcome = HitOutcome.Hit;
            if (Armor > 0f)
            {
                Armor -= hit.armorBreak;
                damage *= armoredDamageMultiplier;
                if (Armor <= 0f)
                {
                    Armor = 0f;
                    armorBrokenTime = Time.time;
                    outcome = HitOutcome.ArmorBroken;
                }
                else
                {
                    outcome = HitOutcome.Armored;
                }
            }

            LastDamage = damage;
            Health -= damage;
            if (Health <= 0f)
            {
                Health = 0f;
                IsDead = true;
                outcome = HitOutcome.Killed;
            }

            Damaged?.Invoke(hit, outcome);
            if (outcome == HitOutcome.Killed) Died?.Invoke();
            return outcome;
        }
    }
}
