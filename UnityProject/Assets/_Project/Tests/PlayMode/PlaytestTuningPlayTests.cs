using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>通しで遊んで見つけたところの調整: ロックオン中の向き・カメラ・暗いステージの照り返し</summary>
    public class PlaytestTuningPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        Transform mPlayer;

        public override void Setup()
        {
            base.Setup();
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            ArenaDirector.EliteChanceOverride = 0f;
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            base.TearDown();
        }

        IEnumerator LoadEmptyArena(int iStage = 1)
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mPlayer = Object.FindFirstObjectByType<SkillExecutor>().transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            mDirector.StartStage(iStage);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
            yield return WaitUntil(() => mPlayer.GetComponent<PlayerMotor>().IsGrounded, 3f);
        }

        static IEnumerator WaitUntil(Func<bool> iCondition, float iTimeout)
        {
            float aStart = Time.realtimeSinceStartup;
            while (!iCondition())
            {
                if (Time.realtimeSinceStartup - aStart > iTimeout) Assert.Fail("タイムアウトしました");
                yield return null;
            }
        }

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        EnemyController SpawnAhead()
        {
            var aProfile = mDirector.Stages[1].Waves[0].Enemies[0];
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + mPlayer.forward * 9f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        [UnityTest]
        public IEnumerator BackingAwayWhileLockedOn_KeepsFacingTheEnemy()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnAhead();
            yield return null;
            yield return Tap(mKeyboard.tabKey);
            Assert.IsNotNull(mPlayer.GetComponent<LockOnSystem>().Target);

            // 後ろへ下がる
            Press(mKeyboard.sKey);
            yield return new WaitForSeconds(0.6f);
            Release(mKeyboard.sKey);
            Vector3 aToEnemy = aEnemy.transform.position - mPlayer.position;
            aToEnemy.y = 0f;
            Assert.Greater(Vector3.Dot(mPlayer.forward, aToEnemy.normalized), 0.9f, "ロックオン中に下がると、敵に背を向けた");
        }

        [UnityTest]
        public IEnumerator WithoutLockOn_BackingAwayTurnsAround()
        {
            yield return LoadEmptyArena();
            Vector3 aForward = mPlayer.forward;
            Press(mKeyboard.sKey);
            yield return new WaitForSeconds(0.6f);
            Release(mKeyboard.sKey);
            Assert.Less(Vector3.Dot(mPlayer.forward, aForward), 0f, "ロックオンしていないのに、歩く向きを向かない");
        }

        [UnityTest]
        public IEnumerator LockedOnCamera_SitsOverTheRightShoulder()
        {
            yield return LoadEmptyArena();
            SpawnAhead();
            yield return null;
            var aCamera = Camera.main.transform;
            float aBefore = Vector3.Dot(aCamera.position - mPlayer.position, aCamera.right);
            yield return Tap(mKeyboard.tabKey);
            yield return new WaitForSeconds(0.8f);
            float aAfter = Vector3.Dot(aCamera.position - mPlayer.position, aCamera.right);
            Assert.Greater(aAfter, aBefore + 0.6f, "ロックオンしても、カメラが右肩へずれない");
        }

        [UnityTest]
        public IEnumerator DarkStages_ReflectLess()
        {
            yield return LoadEmptyArena(4);
            Assert.AreEqual(mDirector.Stages[4].Theme.Reflection, RenderSettings.reflectionIntensity, 1e-4f);
            Assert.Less(RenderSettings.reflectionIntensity, 0.3f, "夜のステージの照り返しが強いまま");
            mDirector.StartStage(2);
            yield return null;
            Assert.Greater(RenderSettings.reflectionIntensity, 0.4f, "明るいステージまで照り返しが弱い");
        }

        EnemyProfile FindArmored()
        {
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                {
                    foreach (var aProfile in aWave.Enemies)
                    {
                        if (aProfile != null && aProfile.DisplayName == "重装兵") return aProfile;
                    }
                }
            }
            Assert.Fail("重装兵がいない");
            return null;
        }

        [UnityTest]
        public IEnumerator FirstWave_HasNoElites()
        {
            ArenaDirector.EliteChanceOverride = -1f;
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            Assert.AreEqual(1, mDirector.WaveNumber);
            foreach (var aEnemy in EnemyController.Active)
            {
                Assert.AreEqual(EliteAffix.None, aEnemy.Profile.Elite, "最初のウェーブにエリートが出た");
            }
        }

        [UnityTest]
        public IEnumerator GiantElite_DangerArea_StaysEscapable()
        {
            yield return LoadEmptyArena();
            var aGiant = mDirector.SpawnElite(FindArmored(), EliteAffix.Giant, mPlayer.position + mPlayer.forward * 8f);
            yield return null;
            var aQuake = aGiant.Profile.Attacks.Find(iAttack => iAttack.Unblockable);
            Assert.NotNull(aQuake);
            Assert.Greater(aQuake.Radius * aGiant.Profile.Scale, EnemyController.MaxDangerRadius, "(前提)巨大になると範囲が上限を超える");
            Assert.AreEqual(EnemyController.MaxDangerRadius, aGiant.AttackRadius(aQuake), 1e-4f, "逃げるしかない攻撃の範囲が上限で止まらない");
        }

        [UnityTest]
        public IEnumerator LockingOnToABigEnemy_PullsTheCameraBack()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            float aBase = aRig.CurrentDistance;
            mDirector.SpawnElite(FindArmored(), EliteAffix.Giant, mPlayer.position + mPlayer.forward * 9f);
            yield return null;
            yield return Tap(mKeyboard.tabKey);
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(aRig.CurrentDistance, aBase + 1f, "大きな敵にロックオンしても、カメラが引かない");
        }

        [UnityTest]
        public IEnumerator Announcements_DoNotPileUp()
        {
            yield return LoadEmptyArena();
            var aStyle = mPlayer.GetComponent<StyleRankSystem>();
            aStyle.Announce("WEAK!", Color.white);
            aStyle.Announce("CHAIN ×1", Color.white);
            aStyle.Announce("WEAK!", Color.white);
            aStyle.Announce("SWAP STRIKE!", Color.white);
            aStyle.Announce("CHANGE ATTACK", Color.white);
            aStyle.Announce("WEAK!", Color.white);
            Assert.LessOrEqual(aStyle.Announcements.Count, StyleRankSystem.MaxAnnouncements, "右側の文字が多すぎる");
            Assert.AreEqual(1, aStyle.Announcements.FindAll(iAnnouncement => iAnnouncement.Text == "WEAK!").Count, "同じ文字が並んでいる");
            Assert.AreEqual("WEAK!", aStyle.Announcements[aStyle.Announcements.Count - 1].Text, "最後に出た文字がいちばん新しくない");
        }

        [UnityTest]
        public IEnumerator DangerAttacks_DoNotOverlapOrRepeat()
        {
            yield return LoadEmptyArena();
            var aProfile = FindArmored();
            int aQuake = aProfile.Attacks.FindIndex(iAttack => iAttack.Unblockable);
            var aEnemies = new System.Collections.Generic.List<EnemyController>();
            for (int aIndex = 0; aIndex < 3; aIndex++)
            {
                var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + Quaternion.Euler(0f, aIndex * 120f, 0f) * Vector3.forward * 3f);
                aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
                aEnemies.Add(aEnemy);
            }
            yield return null;
            Assert.IsTrue(aEnemies[0].ForceAttack(aQuake));
            yield return null;

            // 地割れが終わって間をあけるまで、ほかの敵も同じ敵も、逃げるしかない攻撃を始めない
            float aUntil = Time.time + aProfile.Attacks[aQuake].Windup + aProfile.Attacks[aQuake].Active
                + aProfile.Attacks[aQuake].Recovery + EnemyController.DangerSpacing - 0.2f;
            bool aFirstQuakeOver = false;
            while (Time.time < aUntil)
            {
                // 最初の地割れ(強制で出したもの)が終わるまでは、その敵だけ見逃す
                if (aEnemies[0].CurrentAttack == null) aFirstQuakeOver = true;
                for (int aIndex = 0; aIndex < aEnemies.Count; aIndex++)
                {
                    if (aIndex == 0 && !aFirstQuakeOver) continue;
                    var aAttack = aEnemies[aIndex].CurrentAttack;
                    Assert.IsFalse(aAttack != null && aAttack.Unblockable, "逃げるしかない攻撃が重なった・続いた");
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator StandingNextToABigEnemy_PullsTheCameraBack_WithoutLockOn()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            float aBase = aRig.CurrentDistance;
            var aGiant = mDirector.SpawnElite(FindArmored(), EliteAffix.Giant, mPlayer.position + mPlayer.forward * 3.5f);
            aGiant.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            yield return new WaitForSeconds(1.2f);
            Assert.IsNull(mPlayer.GetComponent<LockOnSystem>().Target);
            Assert.Greater(aRig.CurrentDistance, aBase + 0.8f, "すぐ近くに大きな敵がいても、カメラが引かない");
        }

        [UnityTest]
        public IEnumerator CameraPushedByAWall_RisesAndLooksDown()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            yield return new WaitForSeconds(0.5f);
            Assert.Less(aRig.WallLift, 0.2f, "(前提)壁がないのに上がっている");

            // カメラとプレイヤーの間、プレイヤーのすぐ後ろに壁を立てる
            Vector3 aBack = aRig.transform.position - mPlayer.position;
            aBack.y = 0f;
            var aWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aWall.transform.position = mPlayer.position + aBack.normalized * 1.8f + Vector3.up * 2f;
            aWall.transform.rotation = Quaternion.LookRotation(aBack.normalized);
            aWall.transform.localScale = new Vector3(12f, 8f, 0.4f);
            yield return new WaitForSeconds(1f);
            float aLift = aRig.WallLift;
            Object.Destroy(aWall);
            Assert.Greater(aLift, 1f, "壁に押されても、カメラが上へ逃げない");
        }

        [UnityTest]
        public IEnumerator CameraRisingOffAWall_DoesNotSinkIntoACeiling()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            yield return new WaitForSeconds(0.5f);
            Vector3 aBack = aRig.transform.position - mPlayer.position;
            aBack.y = 0f;
            aBack.Normalize();
            // すぐ後ろに壁、頭の少し上に天井(壁から上へ逃げると、天井にめり込んでいた)
            var aWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aWall.transform.position = mPlayer.position + aBack * 2.2f + Vector3.up * 2f;
            aWall.transform.rotation = Quaternion.LookRotation(aBack);
            aWall.transform.localScale = new Vector3(12f, 8f, 0.4f);
            var aCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aCeiling.transform.position = mPlayer.position + aBack * 1f + Vector3.up * 3.6f;
            aCeiling.transform.rotation = Quaternion.LookRotation(aBack);
            aCeiling.transform.localScale = new Vector3(12f, 0.4f, 3f);
            yield return new WaitForSeconds(1f);
            bool aInside = aCeiling.GetComponent<Collider>().bounds.Contains(aRig.transform.position)
                           || aWall.GetComponent<Collider>().bounds.Contains(aRig.transform.position);
            Object.Destroy(aWall);
            Object.Destroy(aCeiling);
            Assert.IsFalse(aInside, "壁から上へ逃げたカメラが、天井や壁にめり込んでいる");
        }

        [UnityTest]
        public IEnumerator BigEnemyBehindThePlayer_DoesNotHideThePlayer()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            yield return new WaitForSeconds(0.5f);
            Vector3 aBack = aRig.transform.position - mPlayer.position;
            aBack.y = 0f;
            var aBoss = FindBoss();
            var aEnemy = mDirector.SpawnExtra(aBoss, mPlayer.position + aBack.normalized * 3f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            aEnemy.enabled = false;
            yield return new WaitForSeconds(1.5f);
            // カメラからプレイヤーの頭が見える(カメラが敵の手前に回るか、敵の体の中に入る)
            Vector3 aHead = mPlayer.position + Vector3.up * 1.6f;
            Vector3 aFrom = aRig.transform.position;
            bool aHidden = false;
            foreach (var aHit in Physics.RaycastAll(aFrom, (aHead - aFrom).normalized, Vector3.Distance(aFrom, aHead), ~0, QueryTriggerInteraction.Ignore))
            {
                if (aHit.collider.GetComponentInParent<EnemyController>() == aEnemy) aHidden = true;
            }
            Assert.IsFalse(aHidden, "ボスの体でプレイヤーが隠れている");
        }

        [UnityTest]
        public IEnumerator BigEnemyRightBehind_DoesNotPushTheCameraIntoThePlayer()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            yield return new WaitForSeconds(0.5f);
            Vector3 aBack = aRig.transform.position - mPlayer.position;
            aBack.y = 0f;
            // ボスがすぐ後ろにいると、カメラがプレイヤーの頭に張り付いて、頭で画面がふさがっていた
            var aEnemy = mDirector.SpawnExtra(FindBoss(), mPlayer.position + aBack.normalized * 1.8f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            aEnemy.enabled = false;
            yield return new WaitForSeconds(1.5f);
            Vector3 aOffset = aRig.transform.position - mPlayer.position;
            aOffset.y = 0f;
            Assert.Greater(aOffset.magnitude, 2.2f, "大きな敵がすぐ後ろにいると、カメラがプレイヤーに近づきすぎる");
        }

        EnemyProfile FindBoss()
        {
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                {
                    foreach (var aProfile in aWave.Enemies)
                    {
                        if (aProfile != null && aProfile.IsBoss) return aProfile;
                    }
                }
            }
            Assert.Fail("ボスがいない");
            return null;
        }

        [UnityTest]
        public IEnumerator BrightestFloor_LeavesRoomForHudText()
        {
            yield return LoadEmptyArena();
            // 白に近い床の上では、HUD の白い文字が読めない。どのステージの床も明るさを抑える
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Theme == null) continue;
                Color aFloor = aStage.Theme.FloorColor;
                float aLuminance = 0.2126f * aFloor.r + 0.7152f * aFloor.g + 0.0722f * aFloor.b;
                Assert.Less(aLuminance, 0.75f, $"{aStage.name} の床が明るすぎる");
            }
        }

        [UnityTest]
        public IEnumerator Hud_FitsScreensNarrowerThan16By9()
        {
            yield return LoadEmptyArena();
            // 高さだけに合わせると、4:3 の画面で右端の表示が切れる。基準の 1920x1080 がまるごと入るようにする
            var aDocument = Object.FindFirstObjectByType<BattleHud>().GetComponent<UnityEngine.UIElements.UIDocument>();
            Assert.AreEqual(UnityEngine.UIElements.PanelScreenMatchMode.Expand, aDocument.panelSettings.screenMatchMode);
        }

        [UnityTest]
        public IEnumerator GuardShield_ShowsAroundTheBody_FromBehind()
        {
            yield return LoadEmptyArena();
            Press(mKeyboard.qKey);
            yield return new WaitForSeconds(0.2f);
            var aShield = mPlayer.Find("GuardShield");
            Assert.NotNull(aShield, "ガードしても盾が出ない");
            Assert.IsTrue(aShield.gameObject.activeSelf);
            // 体より盾が大きくないと、後ろのカメラからは体に隠れて見えない
            float aBodyWidth = mPlayer.GetComponent<CharacterController>().radius * 2f * mPlayer.lossyScale.x;
            var aBounds = aShield.GetComponent<Renderer>().bounds;
            float aShieldWidth = Mathf.Max(aBounds.size.x, aBounds.size.z);
            Release(mKeyboard.qKey);
            Assert.Greater(aShieldWidth, aBodyWidth + 0.5f, "盾が体に隠れて、後ろから見えない");
        }

        [UnityTest]
        public IEnumerator PlaytestCommands_WarpToARoom_AndMakeAnEnemyAttack()
        {
            yield return LoadEmptyArena();
            // いちばん近い敵が、言われた攻撃をすぐ出す
            var aEnemy = SpawnAhead();
            yield return null;
            mDirector.PlaytestCommand("attack 0");
            Assert.IsTrue(aEnemy.IsTelegraphing, "attack で敵が攻撃を始めない");

            // 探索で、言われた部屋へ移る
            int aExplore = -1;
            for (int aIndex = 0; aIndex < mDirector.Stages.Count; aIndex++)
            {
                if (mDirector.Stages[aIndex] != null && mDirector.Stages[aIndex].Kind == StageKind.Exploration) aExplore = aIndex;
            }
            Assert.GreaterOrEqual(aExplore, 0, "探索のステージがない");
            mDirector.StartStage(aExplore);
            yield return WaitUntil(() => ExplorationRegion.Active != null && ExplorationRegion.Active.Player != null, 5f);
            var aRegion = ExplorationRegion.Active;
            Assert.Greater(aRegion.RoomCount, 2);
            string aName = aRegion.WarpToRoom(2);
            yield return null;
            yield return null;
            Assert.AreEqual(aName, aRegion.CurrentRoomName(), "room で、その部屋へ移らない");
        }

        [Test]
        public void Glint_ComesEarlyEnoughToReactInto_TheParryWindow()
        {
            // 光ったのを見てから押す(人の反応 約0.2秒)と、ちょうどパリィの受付(押して 0.18 秒以内に受ける)に入る
            const float aReactionTime = 0.2f;
            float aPressBeforeHit = EnemyTelegraph.GlintTime - aReactionTime;
            Assert.Greater(aPressBeforeHit, 0f, "光ってから反応したのでは、もう当たっている");
            Assert.LessOrEqual(aPressBeforeHit, GuardRules.ParryWindow, "光ってすぐ押すと、早すぎてパリィにならない");
        }

        [UnityTest]
        public IEnumerator NormalAttackAreas_LookDifferentFromEscapeOnlyAreas()
        {
            yield return LoadEmptyArena();
            Color.RGBToHSV(EnemyTelegraph.DangerColor, out float aDangerHue, out _, out _);
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                {
                    foreach (var aProfile in aWave.Enemies)
                    {
                        if (aProfile == null) continue;
                        Color.RGBToHSV(aProfile.TelegraphColor, out float aHue, out _, out _);
                        float aGap = Mathf.Abs(aHue - aDangerHue);
                        aGap = Mathf.Min(aGap, 1f - aGap);
                        // ガードできる攻撃の範囲が、逃げるしかない攻撃の赤と同じに見えると、どちらかわからない
                        Assert.Greater(aGap, 0.08f, $"{aProfile.DisplayName} の予告の色が、逃げるしかない攻撃の赤に近すぎる");
                    }
                }
            }
        }

        static float Luminance(Color iColor)
        {
            var aLinear = iColor.linear;
            return 0.2126f * aLinear.r + 0.7152f * aLinear.g + 0.0722f * aLinear.b;
        }

        static float Contrast(Color iFirst, Color iSecond)
        {
            float aFirst = Luminance(iFirst);
            float aSecond = Luminance(iSecond);
            return (Mathf.Max(aFirst, aSecond) + 0.05f) / (Mathf.Min(aFirst, aSecond) + 0.05f);
        }

        [UnityTest]
        public IEnumerator AttackAreas_StandOutFromEachStageFloor()
        {
            yield return LoadEmptyArena();
            // 範囲の外側(届く広さ)が床に溶けると、どこまで届くかわからない(第1幕の茶色い床で、ほぼ見えなかった)
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                Color aFloor = aStage.Theme.FloorColor;
                bool aDark = EnemyTelegraph.IsDarkFloor(aFloor);
                foreach (var aWave in aStage.Waves)
                {
                    foreach (var aProfile in aWave.Enemies)
                    {
                        if (aProfile == null) continue;
                        float aContrast = Contrast(EnemyTelegraph.AreaColor(aProfile.TelegraphColor, aDark), aFloor);
                        Assert.Greater(aContrast, 2f, $"{aStage.name} の床の上で、{aProfile.DisplayName} の攻撃の範囲が見えにくい({aContrast:0.00})");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator ClosedGuide_StaysClosedNextTime()
        {
            GameSettings.ShowGuide = true;
            yield return LoadEmptyArena();
            var aHud = Object.FindFirstObjectByType<BattleHud>();
            Assert.IsTrue(aHud.IsHelpShown);
            yield return Tap(mKeyboard.digit0Key);
            Assert.IsFalse(aHud.IsHelpShown);

            // もう一度遊ぶ(シーンを読み直す)
            yield return LoadEmptyArena();
            aHud = Object.FindFirstObjectByType<BattleHud>();
            bool aStillClosed = !aHud.IsHelpShown;
            GameSettings.ShowGuide = true;
            Assert.IsTrue(aStillClosed, "閉じた操作説明が、次に遊ぶとまた開いている");
        }
    }
}
