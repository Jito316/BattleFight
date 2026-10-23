using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    public struct HitInfo
    {
        public float Damage;
        public float Stagger;
        public float ArmorBreak;
        public float Knockback;
        public float Launch;
        /// <summary>水平方向の、攻撃者から被弾者への向き</summary>
        public Vector3 Direction;
        public GameObject Source;
        /// <summary>被弾ののけぞり・つぶれの大きさ(1 = ふつう。0 のときは 1 として扱う)</summary>
        public float Reaction;
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
        public static bool Landed(this HitOutcome iOutcome) => iOutcome >= HitOutcome.Armored;
    }

    /// <summary>体力とアーマー。プレイヤーと敵の両方が持つ。</summary>
    public class Damageable : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("team")] Team mTeam = Team.Enemy;
        [SerializeField, FormerlySerializedAs("maxHealth")] float mMaxHealth = 100f;
        [SerializeField, FormerlySerializedAs("maxArmor")] float mMaxArmor;
        [SerializeField, FormerlySerializedAs("armorRegenDelay")] float mArmorRegenDelay = 6f;
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("armoredDamageMultiplier")] float mArmoredDamageMultiplier = 0.4f;

        float mArmorBrokenTime;
        bool mInitialized;

        public Team Team => mTeam;
        public float MaxHealth => mMaxHealth;
        public float Health { get; private set; }
        public float MaxArmor => mMaxArmor;
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
            if (!mInitialized) ResetState();
        }

        public void Configure(Team iNewTeam, float iNewMaxHealth, float iNewMaxArmor, float iNewArmorRegenDelay)
        {
            mTeam = iNewTeam;
            mMaxHealth = iNewMaxHealth;
            mMaxArmor = iNewMaxArmor;
            mArmorRegenDelay = iNewArmorRegenDelay;
            ResetState();
        }

        public void ResetState()
        {
            Health = mMaxHealth;
            Armor = mMaxArmor;
            IsDead = false;
            mInitialized = true;
        }

        void Update()
        {
            if (mMaxArmor > 0f && Armor <= 0f && !IsDead && Time.time - mArmorBrokenTime >= mArmorRegenDelay)
            {
                Armor = mMaxArmor;
            }
        }

        /// <summary>体力を回復する(探索の祭壇など)。倒れていたら何もしない</summary>
        public void Heal(float iAmount)
        {
            if (IsDead) return;
            Health = Mathf.Min(mMaxHealth, Health + iAmount);
        }

        /// <summary>アーマーを最大まで戻す(ボスの第二形態など)</summary>
        public void RestoreArmor() => Armor = mMaxArmor;

        /// <summary>攻撃によらず倒れる(自爆など)</summary>
        public void Kill()
        {
            if (IsDead) return;
            Health = 0f;
            IsDead = true;
            Died?.Invoke();
        }

        public HitOutcome ApplyHit(HitInfo iHit)
        {
            if (IsDead) return HitOutcome.Ignored;
            if (Invulnerable)
            {
                Evaded?.Invoke(iHit);
                return HitOutcome.Evaded;
            }
            if (CounterHandler != null && CounterHandler(iHit)) return HitOutcome.Countered;

            float aDamage = iHit.Damage;
            var aOutcome = HitOutcome.Hit;
            if (Armor > 0f)
            {
                Armor -= iHit.ArmorBreak;
                aDamage *= mArmoredDamageMultiplier;
                if (Armor <= 0f)
                {
                    Armor = 0f;
                    mArmorBrokenTime = Time.time;
                    aOutcome = HitOutcome.ArmorBroken;
                }
                else
                {
                    aOutcome = HitOutcome.Armored;
                }
            }

            LastDamage = aDamage;
            Health -= aDamage;
            if (Health <= 0f)
            {
                Health = 0f;
                IsDead = true;
                aOutcome = HitOutcome.Killed;
            }

            Damaged?.Invoke(iHit, aOutcome);
            if (aOutcome == HitOutcome.Killed) Died?.Invoke();
            return aOutcome;
        }
    }
}
