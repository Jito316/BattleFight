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
    /// <summary>試作アリーナを実際に動かし、キー入力で切り替えと戦闘ができるかを確かめる。</summary>
    public class ArenaPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        SkillSlotController mSlots;
        SkillExecutor mExecutor;
        StyleRankSystem mStyle;
        Transform mPlayer;

        public override void Setup()
        {
            base.Setup();
            // エリートはランダムに出るので、ここでは出さない(FunPlayTests で確かめる)
            ArenaDirector.EliteChanceOverride = 0f;
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        /// <summary>第1幕(ステージ番号 1)</summary>
        const int mDefaultStage = 1;

        ArenaDirector mDirector;

        IEnumerator LoadArena(SwapMode iMode = SwapMode.Rack, int iStage = mDefaultStage)
        {
            // 前回保存した編成・探索の進み具合に左右されないようにする
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mSlots = Object.FindFirstObjectByType<SkillSlotController>();
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mStyle = Object.FindFirstObjectByType<StyleRankSystem>();
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mPlayer = mExecutor.transform;
            mSlots.SetMode(iMode);
            if (iStage >= 0) mDirector.StartStage(iStage);
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            base.TearDown();
        }

        void MakePlayerSturdy() => mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);

        /// <summary>ウェーブを止めて敵を消す(被弾で技が中断されると困るテスト用)</summary>
        static void ClearEnemies()
        {
            Object.FindFirstObjectByType<ArenaDirector>().StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
        }

        IEnumerator UseSlot(SlotType iSlot, float iTimeout = 6f)
        {
            var aKeys = new[] { mKeyboard.jKey, mKeyboard.kKey, mKeyboard.lKey };
            yield return WaitUntil(() => !mExecutor.IsBusy && !mExecutor.IsStunned && mExecutor.GetComponent<PlayerMotor>().IsGrounded, iTimeout);
            yield return Tap(aKeys[(int)iSlot]);
            yield return WaitUntil(() => !mExecutor.IsBusy, iTimeout);
        }

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
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

        [UnityTest]
        public IEnumerator SwapKeys_CycleEachSlotInRealTime()
        {
            yield return LoadArena();

            Assert.AreEqual("連斬", mSlots.GetCurrent(SlotType.AttackA).DisplayName);
            Assert.AreEqual("居合", mSlots.GetCurrent(SlotType.AttackB).DisplayName);
            Assert.AreEqual("ステップ斬り", mSlots.GetCurrent(SlotType.Movement).DisplayName);
            Assert.AreEqual(WeaponBonusKind.Mastery, mSlots.Bonus.Kind);

            yield return Tap(mKeyboard.digit1Key);
            Assert.AreEqual("重撃", mSlots.GetCurrent(SlotType.AttackA).DisplayName);
            Assert.AreEqual(WeaponBonusKind.Synergy, mSlots.Bonus.Kind);
            Assert.IsTrue(mExecutor.SwapStrikeReady);

            yield return Tap(mKeyboard.digit2Key);
            yield return Tap(mKeyboard.digit2Key);
            Assert.AreEqual("引き寄せ", mSlots.GetCurrent(SlotType.AttackB).DisplayName);
            Assert.AreEqual(WeaponBonusKind.Arsenal, mSlots.Bonus.Kind);

            yield return Tap(mKeyboard.digit3Key);
            Assert.AreEqual("ハンマージャンプ", mSlots.GetCurrent(SlotType.Movement).DisplayName);
        }

        [UnityTest]
        public IEnumerator Attack_StartsCombo_AndSwapDuringRecoveryCancelsIt()
        {
            yield return LoadArena();
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            yield return Tap(mKeyboard.jKey);
            Assert.IsTrue(mExecutor.IsBusy);
            Assert.AreEqual("連斬", mExecutor.Current.DisplayName);

            yield return WaitUntil(() => mExecutor.DebugLabel.Contains("Recovery"), 3f);
            yield return Tap(mKeyboard.digit1Key);

            Assert.IsFalse(mExecutor.IsBusy, "硬直中の切り替えで技がキャンセルされていない");
            Assert.AreEqual("重撃", mSlots.GetCurrent(SlotType.AttackA).DisplayName);
            Assert.IsTrue(mStyle.Announcements.Exists(iAnnouncement => iAnnouncement.Text == "SWAP CANCEL"));
        }

        [UnityTest]
        public IEnumerator Attack_DamagesEnemy_AndRaisesStyle()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);

            // 敵をプレイヤーの目の前に置く
            var aEnemy = EnemyController.Active[0];
            aEnemy.transform.position = mPlayer.position + mPlayer.forward * 1.6f;
            Physics.SyncTransforms();
            float aBefore = aEnemy.Damageable.Health;

            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => aEnemy == null || aEnemy.Damageable.Health < aBefore, 2f);

            Assert.Greater(mStyle.Meter.Points, 0f);
        }

        [UnityTest]
        public IEnumerator Wire_ZipsToNearestVisiblePoint_AndHangs()
        {
            yield return LoadArena();
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            // 目の前の近いポイントと、同じ方向の遠いポイントを置く
            var aNear = new GameObject("TestPoint_Near").AddComponent<GrapplePoint>();
            aNear.transform.position = mPlayer.position + mPlayer.forward * 7f + Vector3.up * 4f;
            var aFar = new GameObject("TestPoint_Far").AddComponent<GrapplePoint>();
            aFar.transform.position = mPlayer.position + mPlayer.forward * 14f + Vector3.up * 5f;

            // 移動スロットをワイヤーにする(ステップ斬り → ハンマージャンプ → ワイヤー)
            yield return Tap(mKeyboard.digit3Key);
            yield return Tap(mKeyboard.digit3Key);
            Assert.AreEqual("ワイヤー", mSlots.GetCurrent(SlotType.Movement).DisplayName);
            yield return null;
            Assert.AreSame(aNear, mExecutor.GrapplePreview, "視界内で一番近いポイントが選ばれていない");

            yield return Tap(mKeyboard.lKey);
            yield return WaitUntil(() => mExecutor.DebugLabel.Contains("Hang"), 3f);

            float aDistance = Vector3.Distance(mPlayer.position + Vector3.up * 1.8f, aNear.transform.position);
            Assert.Less(aDistance, 1.5f, "ポイントの下にぶら下がっていない");

            // ぶら下がり中は次の近いポイント(遠い方)が候補になる
            yield return null;
            Assert.AreSame(aFar, mExecutor.GrapplePreview);
        }

        [UnityTest]
        public IEnumerator PresetKeys_SwitchAllSlotsAtOnce()
        {
            yield return LoadArena(SwapMode.Preset);

            Assert.AreEqual(0, mSlots.PresetIndex);
            Assert.AreEqual("連斬", mSlots.GetCurrent(SlotType.AttackA).DisplayName);

            yield return Tap(mKeyboard.digit2Key);
            Assert.AreEqual(1, mSlots.PresetIndex);
            Assert.AreEqual("魔弾", mSlots.GetCurrent(SlotType.AttackA).DisplayName);
            Assert.AreEqual("落雷", mSlots.GetCurrent(SlotType.AttackB).DisplayName);
            Assert.AreEqual("ブリンク", mSlots.GetCurrent(SlotType.Movement).DisplayName);
            Assert.AreEqual(WeaponBonusKind.Mastery, mSlots.Bonus.Kind);
            Assert.AreEqual(WeaponType.Staff, mSlots.Bonus.Weapon);
            Assert.IsTrue(mExecutor.SwapStrikeReady);

            yield return Tap(mKeyboard.digit4Key);
            Assert.AreEqual(3, mSlots.PresetIndex);
            Assert.AreEqual(WeaponBonusKind.Arsenal, mSlots.Bonus.Kind);
        }

        [UnityTest]
        public IEnumerator PresetSwitch_DuringRecovery_CancelsTheSkill()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            yield return Tap(mKeyboard.jKey);
            Assert.AreEqual("連斬", mExecutor.Current.DisplayName);
            yield return WaitUntil(() => mExecutor.DebugLabel.Contains("Recovery"), 3f);
            Assert.IsTrue(mExecutor.CanPresetCancel(2));

            yield return Tap(mKeyboard.digit3Key);
            Assert.IsFalse(mExecutor.IsBusy, "プリセットの切り替えで技がキャンセルされていない");
            Assert.IsTrue(mStyle.Announcements.Exists(iAnnouncement => iAnnouncement.Text == "SWAP CANCEL"));
        }

        [UnityTest]
        public IEnumerator EveryDatabaseSkill_CanBeUsedWithoutErrors()
        {
            yield return LoadArena(SwapMode.Preset);
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            yield return Tap(mKeyboard.tabKey);

            foreach (var aSkill in mSlots.Database.Skills)
            {
                Assert.IsTrue(mSlots.AssignPresetSkill(0, aSkill.Slot, aSkill), aSkill.name);
                yield return UseSlot(aSkill.Slot);
            }
            // 弾や遅れて落ちる攻撃が消えるのを待つ
            yield return new WaitForSeconds(1.5f);
        }

        [UnityTest]
        public IEnumerator EveryMasteryFinisher_CanBeUsed()
        {
            yield return LoadArena(SwapMode.Preset);
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            ClearEnemies();
            yield return null;

            foreach (var aWeapon in mSlots.Weapons)
            {
                if (aWeapon.MasteryFinisher == null) continue;
                for (int aSlot = 0; aSlot < 3; aSlot++)
                {
                    SkillData aPick = null;
                    foreach (var aSkill in mSlots.Database.ForSlot((SlotType)aSlot))
                    {
                        if (aSkill.Weapon == aWeapon.Weapon)
                        {
                            aPick = aSkill;
                            break;
                        }
                    }
                    Assert.NotNull(aPick, $"{aWeapon.DisplayName} の {(SlotType)aSlot} スキルがない");
                    mSlots.AssignPresetSkill(0, (SlotType)aSlot, aPick);
                }
                Assert.AreSame(aWeapon.MasteryFinisher, mSlots.MasteryFinisher);

                yield return WaitUntil(() => !mExecutor.IsBusy && !mExecutor.IsStunned, 6f);
                yield return Tap(mKeyboard.fKey);
                Assert.AreSame(aWeapon.MasteryFinisher, mExecutor.Current, $"{aWeapon.DisplayName} のフィニッシャーが出ない");
                yield return WaitUntil(() => !mExecutor.IsBusy, 8f);
            }
        }

        [UnityTest]
        public IEnumerator LoadoutEditor_PausesAndSaves()
        {
            yield return LoadArena(SwapMode.Preset);

            yield return Tap(mKeyboard.pKey);
            Assert.IsTrue(LoadoutEditorUI.IsOpen);
            Assert.IsTrue(GamePause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            var aFireball = mSlots.Database.Find("Skill_Staff_Fireball");
            Assert.IsTrue(mSlots.AssignPresetSkill(0, SlotType.AttackB, aFireball));
            Assert.AreSame(aFireball, mSlots.GetCurrent(SlotType.AttackB));

            yield return Tap(mKeyboard.pKey);
            Assert.IsFalse(LoadoutEditorUI.IsOpen);
            Assert.IsFalse(GamePause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);

            Assert.IsTrue(LoadoutStorage.TryLoad(mSlots.Database, out var aSaved));
            Assert.AreEqual(SwapMode.Preset, aSaved.Mode);
            Assert.AreSame(aFireball, aSaved.Presets[0].AttackB);
        }

        EnemyProfile TrainingDummy() => mDirector.Stages[0].TrainingDummies[0];

        /// <summary>ウェーブを止めて、目の前に訓練用の人形を1体置く</summary>
        IEnumerator PlaceDummy(EnemyProfile iProfile, float iDistance)
        {
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            ClearEnemies();
            yield return null;
            var aEnemy = mDirector.SpawnExtra(iProfile, mPlayer.position + mPlayer.forward * iDistance);
            yield return null;
            aEnemy.transform.position = mPlayer.position + mPlayer.forward * iDistance;
            Physics.SyncTransforms();
            yield return null;
            mDummy = aEnemy;
        }

        EnemyController mDummy;

        [UnityTest]
        public IEnumerator Hud_ShowsPresetSkills_AfterSwitchAndEdit()
        {
            yield return LoadArena(SwapMode.Preset);
            var aHud = Object.FindFirstObjectByType<BattleHud>();

            yield return Tap(mKeyboard.digit2Key);
            Assert.AreEqual("魔弾", aHud.ShownSkill(SlotType.AttackA).DisplayName, "プリセットを切り替えても表示が変わらない");

            var aFireball = mSlots.Database.Find("Skill_Staff_Fireball");
            mSlots.AssignPresetSkill(1, SlotType.AttackB, aFireball);
            Assert.AreSame(aFireball, aHud.ShownSkill(SlotType.AttackB), "編成を変えても表示が変わらない");
        }

        [UnityTest]
        public IEnumerator Weakness_MultipliesDamage()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            // 同じ人形を、弱点あり(剣)となしで用意して、連斬の1段目のダメージを比べる
            var aWeakProfile = Object.Instantiate(TrainingDummy());
            aWeakProfile.Weaknesses = new[] { WeaponType.Sword };
            var aPlainProfile = Object.Instantiate(TrainingDummy());
            aPlainProfile.Weaknesses = new WeaponType[0];

            yield return PlaceDummy(aPlainProfile, 1.6f);
            float aBefore = mDummy.Damageable.Health;
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => mDummy.Damageable.Health < aBefore, 2f);
            float aPlainDamage = mDummy.Damageable.LastDamage;
            yield return WaitUntil(() => !mExecutor.IsBusy, 3f);

            yield return PlaceDummy(aWeakProfile, 1.6f);
            aBefore = mDummy.Damageable.Health;
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => mDummy.Damageable.Health < aBefore, 2f);
            float aWeakDamage = mDummy.Damageable.LastDamage;

            Assert.AreEqual(aPlainDamage * 1.5f, aWeakDamage, 0.01f, "弱点のダメージが 1.5 倍になっていない");
        }

        [UnityTest]
        public IEnumerator SwapThenHit_BuildsChain_AndChangeAttackHitsNearbyEnemy()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);
            yield return PlaceDummy(TrainingDummy(), 1.2f);
            Assert.AreEqual(0, mExecutor.SwapChain.Chain);
            Assert.IsTrue(mExecutor.ChangeAttackReady);

            // 切り替えた瞬間のチェンジアタックが、そばの人形に当たる
            float aBefore = mDummy.Damageable.Health;
            yield return Tap(mKeyboard.digit4Key);
            Assert.Less(mDummy.Damageable.Health, aBefore, "チェンジアタックが当たっていない");
            Assert.IsFalse(mExecutor.ChangeAttackReady, "チェンジアタックにクールダウンがない");
            Assert.AreEqual(0, mExecutor.SwapChain.Chain, "チェンジアタックだけでチェインが上がった");

            // 切り替えてから技を当てるとチェインが上がる(混成: 連打)
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => mExecutor.SwapChain.Chain == 1, 2f);
            Assert.AreEqual(1.1f, mExecutor.SwapChain.DamageMultiplier, 1e-4f);
            Assert.IsTrue(mStyle.Announcements.Exists(iAnnouncement => iAnnouncement.Text.StartsWith("CHAIN")));
        }

        EnemyProfile ShadowBoss()
        {
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                foreach (var aEnemy in aWave.Enemies)
                {
                    if (aEnemy != null && aEnemy.DisplayName == "影刃") return aEnemy;
                }
            }
            Assert.Fail("影刃のいるステージがない");
            return null;
        }

        [UnityTest]
        public IEnumerator ShadowBoss_TeleportsBehindPlayer()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return PlaceDummy(ShadowBoss(), 8f);
            var aBoss = mDummy;

            // 背後取り(3番目の攻撃)を出させる
            Assert.IsTrue(aBoss.ForceAttack(2));
            yield return new WaitForSeconds(aBoss.Profile.Attacks[2].Windup * 0.5f + 0.15f);

            Vector3 aToBoss = aBoss.transform.position - mPlayer.position;
            aToBoss.y = 0f;
            Assert.Less(Vector3.Dot(aToBoss.normalized, mPlayer.forward), -0.5f, "プレイヤーの背後に回っていない");
            Assert.Less(aToBoss.magnitude, 5f);
        }

        [UnityTest]
        public IEnumerator ShadowBoss_EntersPhase2AtHalfHealth()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return PlaceDummy(ShadowBoss(), 8f);
            var aBoss = mDummy;
            var aDamageable = aBoss.Damageable;
            Assert.IsFalse(aBoss.IsPhase2);

            // アーマーを割ったうえで、体力を半分より少し多いところまで減らす
            aDamageable.ApplyHit(new HitInfo { Damage = 0f, ArmorBreak = 999f });
            aDamageable.ApplyHit(new HitInfo { Damage = aDamageable.Health - aDamageable.MaxHealth * 0.55f });
            Assert.IsFalse(aBoss.IsPhase2, "半分を切る前に第二形態になった");

            bool aAnnounced = false;
            System.Action<EnemyController> aOnPhase = iEnemy => aAnnounced |= iEnemy == aBoss;
            EnemyController.PhaseChanged += aOnPhase;
            aDamageable.ApplyHit(new HitInfo { Damage = aDamageable.MaxHealth * 0.1f });
            EnemyController.PhaseChanged -= aOnPhase;

            Assert.IsTrue(aBoss.IsPhase2, "半分を切っても第二形態にならない");
            Assert.IsTrue(aAnnounced);
            Assert.AreEqual(aDamageable.MaxArmor, aDamageable.Armor, 1e-3f, "第二形態でアーマーが戻っていない");
            // 第二形態の攻撃も出せる(通常3 + 追加2)
            Assert.IsTrue(aBoss.ForceAttack(4));
            yield return new WaitForSeconds(1.5f);
        }

        [UnityTest]
        public IEnumerator Arena_StartsOnStageSelect()
        {
            yield return LoadArena(iStage: -1);
            Assert.AreEqual(ArenaDirector.GameState.StageSelect, mDirector.State);
            Assert.GreaterOrEqual(mDirector.Stages.Count, 5);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(0, EnemyController.Active.Count, "ステージ選択中に敵が出ている");
        }

        [UnityTest]
        public IEnumerator EveryStage_StartsAndSpawnsEnemies()
        {
            yield return LoadArena(iStage: -1);
            int aCount = mDirector.Stages.Count;
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                // 探索は部屋に入るまで敵が出ない(ExplorationPlayTests で確かめる)
                if (mDirector.Stages[aIndex].Kind == StageKind.Exploration) continue;
                yield return LoadArena(SwapMode.Preset, aIndex);
                MakePlayerSturdy();
                yield return WaitUntil(() => EnemyController.Active.Count > 0, 6f);
                Assert.AreEqual(mDirector.Stages[aIndex], mDirector.CurrentStage);
                yield return new WaitForSeconds(1f);
            }
        }

        [UnityTest]
        public IEnumerator EveryEnemyType_ActsWithoutErrors()
        {
            yield return LoadArena(SwapMode.Preset);
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            ClearEnemies();
            yield return null;

            var aProfiles = new System.Collections.Generic.HashSet<EnemyProfile>();
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage.Waves != null) foreach (var aWave in aStage.Waves) foreach (var aProfile in aWave.Enemies) aProfiles.Add(aProfile);
                if (aStage.TrainingDummies != null) foreach (var aProfile in aStage.TrainingDummies) aProfiles.Add(aProfile);
                if (aStage.EndlessBosses != null) foreach (var aProfile in aStage.EndlessBosses) aProfiles.Add(aProfile);
                foreach (var aEntry in aStage.EndlessPool) aProfiles.Add(aEntry.Profile);
            }
            Assert.GreaterOrEqual(aProfiles.Count, 11);

            // 1種類ずつ目の前に出して、しばらく戦わせる(遠距離・自爆・召喚・飛行などの処理を通す)
            foreach (var aProfile in aProfiles)
            {
                var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + mPlayer.forward * 6f);
                Assert.NotNull(aEnemy, aProfile.DisplayName);
                yield return new WaitForSeconds(2.5f);
                ClearEnemies();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ClearingStage_RecordsAndMovesToNextStage()
        {
            yield return LoadArena(SwapMode.Preset);
            var aStageName = mDirector.CurrentStage.name;
            StageRecords.Clear(aStageName);
            MakePlayerSturdy();

            // 出てきた敵を倒し続けてクリアまで進める
            float aStart = Time.realtimeSinceStartup;
            while (mDirector.State != ArenaDirector.GameState.Cleared)
            {
                foreach (var aEnemy in EnemyController.Active.ToArray()) aEnemy.Damageable.Kill();
                if (Time.realtimeSinceStartup - aStart > 40f) Assert.Fail("クリアまで進まない");
                yield return null;
            }

            Assert.IsTrue(StageRecords.IsCleared(aStageName));
            Assert.IsTrue(mDirector.NewRecord);
            Assert.IsTrue(mDirector.HasNextStage);

            yield return Tap(mKeyboard.nKey);
            yield return null;
            yield return null;
            var aNext = Object.FindFirstObjectByType<ArenaDirector>();
            Assert.AreEqual(mDefaultStage + 1, aNext.StageIndex, "次のステージが始まっていない");
            StageRecords.Clear(aStageName);
        }

        [UnityTest]
        public IEnumerator EveryRackSkill_CanBeUsedWithoutErrors()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            yield return Tap(mKeyboard.tabKey);

            var aKeys = new[] { mKeyboard.jKey, mKeyboard.kKey, mKeyboard.lKey };
            var aSwaps = new[] { mKeyboard.digit1Key, mKeyboard.digit2Key, mKeyboard.digit3Key };
            for (int aCandidate = 0; aCandidate < SkillRack.Capacity; aCandidate++)
            {
                for (int aSlot = 0; aSlot < 3; aSlot++)
                {
                    yield return WaitUntil(() => !mExecutor.IsBusy && mExecutor.GetComponent<PlayerMotor>().IsGrounded, 5f);
                    yield return Tap(aKeys[aSlot]);
                    yield return WaitUntil(() => !mExecutor.IsBusy, 5f);
                }
                foreach (var aSwap in aSwaps) yield return Tap(aSwap);
            }
        }

        [UnityTest]
        public IEnumerator LosingFocus_ReleasesHeldMoveKey()
        {
            yield return LoadArena();
            var aReader = Object.FindFirstObjectByType<PlayerInputReader>();

            Press(mKeyboard.wKey);
            yield return null;
            Assert.Greater(aReader.Move.y, 0.5f, "W を押しても前進の入力にならない");

            // ロックオン(Tab)やウィンドウ切り替えでフォーカスが外れ、W を離したことが届かなかった状況
            aReader.SendMessage("OnApplicationFocus", false);
            yield return null;
            Assert.AreEqual(Vector2.zero, aReader.Move, "フォーカスが外れても移動の入力が残っている");
        }

        [UnityTest]
        public IEnumerator HelpKey0_TogglesGuide_AndGuideFollowsLastDevice()
        {
            var aGamepad = InputSystem.AddDevice<Gamepad>();
            yield return LoadArena();
            var aHud = Object.FindFirstObjectByType<BattleHud>();

            Assert.IsTrue(aHud.IsHelpShown);
            yield return Tap(mKeyboard.digit0Key);
            Assert.IsFalse(aHud.IsHelpShown, "0 キーで操作説明が閉じない");
            yield return Tap(mKeyboard.digit0Key);
            Assert.IsTrue(aHud.IsHelpShown, "0 キーで操作説明が開かない");

            // 0 は切り替えに使っていないので、スキルは変わらない
            Assert.AreEqual("連斬", mSlots.GetCurrent(SlotType.AttackA).DisplayName);

            yield return Tap(aGamepad.buttonSouth);
            Assert.IsTrue(aHud.UsingGamepad, "ゲームパッドを押しても表示がゲームパッド用にならない");
            yield return Tap(aGamepad.rightStickButton);
            Assert.IsFalse(aHud.IsHelpShown, "R3 で操作説明が閉じない");

            yield return Tap(mKeyboard.wKey);
            Assert.IsFalse(aHud.UsingGamepad, "キーボードを押しても表示がキーボード用に戻らない");
        }
    }
}
