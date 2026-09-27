using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class DamageableTests
    {
        GameObject go;
        Damageable damageable;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("target");
            damageable = go.AddComponent<Damageable>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        static HitInfo Hit(float damage, float armorBreak = 0f) => new HitInfo { damage = damage, armorBreak = armorBreak };

        [Test]
        public void Hit_ReducesHealth()
        {
            damageable.Configure(Team.Enemy, 100f, 0f, 5f);

            var outcome = damageable.ApplyHit(Hit(30f));

            Assert.AreEqual(HitOutcome.Hit, outcome);
            Assert.AreEqual(70f, damageable.Health, 1e-4f);
        }

        [Test]
        public void Armor_ReducesDamage_UntilBroken()
        {
            damageable.Configure(Team.Enemy, 100f, 20f, 5f);

            Assert.AreEqual(HitOutcome.Armored, damageable.ApplyHit(Hit(10f, 10f)));
            Assert.Less(damageable.LastDamage, 10f);
            Assert.AreEqual(HitOutcome.ArmorBroken, damageable.ApplyHit(Hit(10f, 10f)));
            Assert.AreEqual(0f, damageable.Armor, 1e-4f);
            Assert.AreEqual(HitOutcome.Hit, damageable.ApplyHit(Hit(10f, 10f)));
            Assert.AreEqual(10f, damageable.LastDamage, 1e-4f);
        }

        [Test]
        public void Invulnerable_Evades()
        {
            damageable.Configure(Team.Player, 100f, 0f, 5f);
            damageable.Invulnerable = true;
            bool evaded = false;
            damageable.Evaded += _ => evaded = true;

            Assert.AreEqual(HitOutcome.Evaded, damageable.ApplyHit(Hit(50f)));
            Assert.IsTrue(evaded);
            Assert.AreEqual(100f, damageable.Health, 1e-4f);
        }

        [Test]
        public void CounterHandler_NegatesHit()
        {
            damageable.Configure(Team.Player, 100f, 0f, 5f);
            damageable.CounterHandler = _ => true;

            Assert.AreEqual(HitOutcome.Countered, damageable.ApplyHit(Hit(50f)));
            Assert.AreEqual(100f, damageable.Health, 1e-4f);
        }

        [Test]
        public void LethalHit_Kills_AndRaisesDied()
        {
            damageable.Configure(Team.Enemy, 20f, 0f, 5f);
            bool died = false;
            damageable.Died += () => died = true;

            Assert.AreEqual(HitOutcome.Killed, damageable.ApplyHit(Hit(50f)));
            Assert.IsTrue(died);
            Assert.IsTrue(damageable.IsDead);
            Assert.AreEqual(HitOutcome.Ignored, damageable.ApplyHit(Hit(10f)));
        }
    }
}
