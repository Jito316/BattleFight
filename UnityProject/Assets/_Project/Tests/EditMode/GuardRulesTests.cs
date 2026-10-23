using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>ガードとパリィのきまり</summary>
    public class GuardRulesTests
    {
        [Test]
        public void Guard_CoversTheFront_ButNotTheBack()
        {
            // 攻撃の向きは、攻撃者から自分へ。正面の敵からなら -forward
            Assert.IsTrue(GuardRules.Covers(Vector3.forward, Vector3.back), "正面からの攻撃を受けられない");
            Assert.IsTrue(GuardRules.Covers(Vector3.forward, new Vector3(0.6f, 0f, -0.8f)), "斜め前からの攻撃を受けられない");
            Assert.IsFalse(GuardRules.Covers(Vector3.forward, Vector3.forward), "後ろからの攻撃まで受けている");
            Assert.IsFalse(GuardRules.Covers(Vector3.forward, Vector3.right), "真横からの攻撃まで受けている");
        }

        [Test]
        public void Parry_OnlyRightAfterPressingGuard()
        {
            Assert.IsTrue(GuardRules.IsParry(10f, 10f));
            Assert.IsTrue(GuardRules.IsParry(10f, 10f + GuardRules.ParryWindow - 0.001f));
            Assert.IsFalse(GuardRules.IsParry(10f, 10f + GuardRules.ParryWindow + 0.01f), "押してしばらくたってもパリィになる");
            Assert.IsFalse(GuardRules.IsParry(10f, 9.9f), "押す前の攻撃がパリィになる");
        }

        [Test]
        public void GuardedDamage_IsSmall_AndNeverKills()
        {
            Assert.AreEqual(20f * GuardRules.ChipRatio, GuardRules.ChipDamage(20f, 100f), 1e-5f);
            Assert.AreEqual(4f, GuardRules.ChipDamage(100f, 5f), 1e-5f, "ガードで受けて倒れる");
            Assert.AreEqual(0f, GuardRules.ChipDamage(50f, 1f), 1e-5f);
        }
    }
}
