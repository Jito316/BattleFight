using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class DamageableTests
    {
        GameObject mGo;
        Damageable mDamageable;

        [SetUp]
        public void SetUp()
        {
            mGo = new GameObject("target");
            mDamageable = mGo.AddComponent<Damageable>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(mGo);

        static HitInfo Hit(float iDamage, float iArmorBreak = 0f) => new HitInfo { Damage = iDamage, ArmorBreak = iArmorBreak };

        [Test]
        public void Hit_ReducesHealth()
        {
            mDamageable.Configure(Team.Enemy, 100f, 0f, 5f);

            var aOutcome = mDamageable.ApplyHit(Hit(30f));

            Assert.AreEqual(HitOutcome.Hit, aOutcome);
            Assert.AreEqual(70f, mDamageable.Health, 1e-4f);
        }

        [Test]
        public void Armor_ReducesDamage_UntilBroken()
        {
            mDamageable.Configure(Team.Enemy, 100f, 20f, 5f);

            Assert.AreEqual(HitOutcome.Armored, mDamageable.ApplyHit(Hit(10f, 10f)));
            Assert.Less(mDamageable.LastDamage, 10f);
            Assert.AreEqual(HitOutcome.ArmorBroken, mDamageable.ApplyHit(Hit(10f, 10f)));
            Assert.AreEqual(0f, mDamageable.Armor, 1e-4f);
            Assert.AreEqual(HitOutcome.Hit, mDamageable.ApplyHit(Hit(10f, 10f)));
            Assert.AreEqual(10f, mDamageable.LastDamage, 1e-4f);
        }

        [Test]
        public void Invulnerable_Evades()
        {
            mDamageable.Configure(Team.Player, 100f, 0f, 5f);
            mDamageable.Invulnerable = true;
            bool aEvaded = false;
            mDamageable.Evaded += _ => aEvaded = true;

            Assert.AreEqual(HitOutcome.Evaded, mDamageable.ApplyHit(Hit(50f)));
            Assert.IsTrue(aEvaded);
            Assert.AreEqual(100f, mDamageable.Health, 1e-4f);
        }

        [Test]
        public void CounterHandler_NegatesHit()
        {
            mDamageable.Configure(Team.Player, 100f, 0f, 5f);
            mDamageable.CounterHandler = _ => true;

            Assert.AreEqual(HitOutcome.Countered, mDamageable.ApplyHit(Hit(50f)));
            Assert.AreEqual(100f, mDamageable.Health, 1e-4f);
        }

        [Test]
        public void LethalHit_Kills_AndRaisesDied()
        {
            mDamageable.Configure(Team.Enemy, 20f, 0f, 5f);
            bool aDied = false;
            mDamageable.Died += () => aDied = true;

            Assert.AreEqual(HitOutcome.Killed, mDamageable.ApplyHit(Hit(50f)));
            Assert.IsTrue(aDied);
            Assert.IsTrue(mDamageable.IsDead);
            Assert.AreEqual(HitOutcome.Ignored, mDamageable.ApplyHit(Hit(10f)));
        }
    }
}
