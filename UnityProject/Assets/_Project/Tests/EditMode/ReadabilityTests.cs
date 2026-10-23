using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>ロックオンの切り替え先と、ステージの見た目の割り当て</summary>
    public class ReadabilityTests
    {
        [Test]
        public void LockOnSwitch_PicksTheNearestEnemyOnThatSide()
        {
            var aOrigin = Vector3.zero;
            var aCurrent = new Vector3(0f, 0f, 10f);
            var aCandidates = new List<Vector3>
            {
                new Vector3(-6f, 0f, 10f),  // 0: 左
                new Vector3(3f, 0f, 10f),   // 1: 右(近い)
                new Vector3(9f, 0f, 4f),    // 2: 右(遠い)
            };
            Assert.AreEqual(1, LockOnRules.PickSide(aOrigin, aCurrent, aCandidates, 1), "右のいちばん近い敵を選ばない");
            Assert.AreEqual(0, LockOnRules.PickSide(aOrigin, aCurrent, aCandidates, -1), "左の敵を選ばない");
            Assert.AreEqual(-1, LockOnRules.PickSide(aOrigin, aCurrent, new List<Vector3> { new Vector3(-6f, 0f, 10f) }, 1),
                "右に敵がいないのに選んだ");
        }

        [Test]
        public void StageTheme_ColorsOnlyTheArenaParts()
        {
            var aTheme = new StageTheme { FloorColor = Color.red, WallColor = Color.green, AccentColor = Color.blue };
            Assert.AreEqual(Color.red, StageThemeApplier.ColorFor(aTheme, "Floor"));
            Assert.AreEqual(Color.green, StageThemeApplier.ColorFor(aTheme, "Wall_N"));
            Assert.AreEqual(Color.blue, StageThemeApplier.ColorFor(aTheme, "Accent_E"));
            Assert.IsNull(StageThemeApplier.ColorFor(aTheme, "GrapplePoint_1"), "ワイヤーの的の色まで変えている");
        }
    }
}
