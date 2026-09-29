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
        const string SceneName = "Prototype_Arena";

        Keyboard keyboard;
        SkillSlotController slots;
        SkillExecutor executor;
        StyleRankSystem style;
        Transform player;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        /// <summary>第1幕(ステージ番号 1)</summary>
        const int DefaultStage = 1;

        ArenaDirector director;

        IEnumerator LoadArena(SwapMode mode = SwapMode.Rack, int stage = DefaultStage)
        {
            // 前回保存した編成・探索の進み具合に左右されないようにする
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            slots = Object.FindFirstObjectByType<SkillSlotController>();
            executor = Object.FindFirstObjectByType<SkillExecutor>();
            style = Object.FindFirstObjectByType<StyleRankSystem>();
            director = Object.FindFirstObjectByType<ArenaDirector>();
            player = executor.transform;
            slots.SetMode(mode);
            if (stage >= 0) director.StartStage(stage);
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            base.TearDown();
        }

        void MakePlayerSturdy() => player.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);

        /// <summary>ウェーブを止めて敵を消す(被弾で技が中断されると困るテスト用)</summary>
        static void ClearEnemies()
        {
            Object.FindFirstObjectByType<ArenaDirector>().StopAllCoroutines();
            foreach (var enemy in EnemyController.Active.ToArray()) Object.Destroy(enemy.gameObject);
        }

        IEnumerator UseSlot(SlotType slot, float timeout = 6f)
        {
            var keys = new[] { keyboard.jKey, keyboard.kKey, keyboard.lKey };
            yield return WaitUntil(() => !executor.IsBusy && !executor.IsStunned && executor.GetComponent<PlayerMotor>().IsGrounded, timeout);
            yield return Tap(keys[(int)slot]);
            yield return WaitUntil(() => !executor.IsBusy, timeout);
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        static IEnumerator WaitUntil(Func<bool> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout) Assert.Fail("タイムアウトしました");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SwapKeys_CycleEachSlotInRealTime()
        {
            yield return LoadArena();

            Assert.AreEqual("連斬", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual("居合", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual("ステップ斬り", slots.GetCurrent(SlotType.Movement).displayName);
            Assert.AreEqual(WeaponBonusKind.Mastery, slots.Bonus.Kind);

            yield return Tap(keyboard.digit1Key);
            Assert.AreEqual("重撃", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual(WeaponBonusKind.Synergy, slots.Bonus.Kind);
            Assert.IsTrue(executor.SwapStrikeReady);

            yield return Tap(keyboard.digit2Key);
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual("引き寄せ", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual(WeaponBonusKind.Arsenal, slots.Bonus.Kind);

            yield return Tap(keyboard.digit3Key);
            Assert.AreEqual("ハンマージャンプ", slots.GetCurrent(SlotType.Movement).displayName);
        }

        [UnityTest]
        public IEnumerator Attack_StartsCombo_AndSwapDuringRecoveryCancelsIt()
        {
            yield return LoadArena();
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            yield return Tap(keyboard.jKey);
            Assert.IsTrue(executor.IsBusy);
            Assert.AreEqual("連斬", executor.Current.displayName);

            yield return WaitUntil(() => executor.DebugLabel.Contains("Recovery"), 3f);
            yield return Tap(keyboard.digit1Key);

            Assert.IsFalse(executor.IsBusy, "硬直中の切り替えで技がキャンセルされていない");
            Assert.AreEqual("重撃", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.IsTrue(style.Announcements.Exists(a => a.text == "SWAP CANCEL"));
        }

        [UnityTest]
        public IEnumerator Attack_DamagesEnemy_AndRaisesStyle()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);

            // 敵をプレイヤーの目の前に置く
            var enemy = EnemyController.Active[0];
            enemy.transform.position = player.position + player.forward * 1.6f;
            Physics.SyncTransforms();
            float before = enemy.Damageable.Health;

            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => enemy == null || enemy.Damageable.Health < before, 2f);

            Assert.Greater(style.Meter.Points, 0f);
        }

        [UnityTest]
        public IEnumerator Wire_ZipsToNearestVisiblePoint_AndHangs()
        {
            yield return LoadArena();
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            // 目の前の近いポイントと、同じ方向の遠いポイントを置く
            var near = new GameObject("TestPoint_Near").AddComponent<GrapplePoint>();
            near.transform.position = player.position + player.forward * 7f + Vector3.up * 4f;
            var far = new GameObject("TestPoint_Far").AddComponent<GrapplePoint>();
            far.transform.position = player.position + player.forward * 14f + Vector3.up * 5f;

            // 移動スロットをワイヤーにする(ステップ斬り → ハンマージャンプ → ワイヤー)
            yield return Tap(keyboard.digit3Key);
            yield return Tap(keyboard.digit3Key);
            Assert.AreEqual("ワイヤー", slots.GetCurrent(SlotType.Movement).displayName);
            yield return null;
            Assert.AreSame(near, executor.GrapplePreview, "視界内で一番近いポイントが選ばれていない");

            yield return Tap(keyboard.lKey);
            yield return WaitUntil(() => executor.DebugLabel.Contains("Hang"), 3f);

            float distance = Vector3.Distance(player.position + Vector3.up * 1.8f, near.transform.position);
            Assert.Less(distance, 1.5f, "ポイントの下にぶら下がっていない");

            // ぶら下がり中は次の近いポイント(遠い方)が候補になる
            yield return null;
            Assert.AreSame(far, executor.GrapplePreview);
        }

        [UnityTest]
        public IEnumerator PresetKeys_SwitchAllSlotsAtOnce()
        {
            yield return LoadArena(SwapMode.Preset);

            Assert.AreEqual(0, slots.PresetIndex);
            Assert.AreEqual("連斬", slots.GetCurrent(SlotType.AttackA).displayName);

            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(1, slots.PresetIndex);
            Assert.AreEqual("魔弾", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual("落雷", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual("ブリンク", slots.GetCurrent(SlotType.Movement).displayName);
            Assert.AreEqual(WeaponBonusKind.Mastery, slots.Bonus.Kind);
            Assert.AreEqual(WeaponType.Staff, slots.Bonus.Weapon);
            Assert.IsTrue(executor.SwapStrikeReady);

            yield return Tap(keyboard.digit4Key);
            Assert.AreEqual(3, slots.PresetIndex);
            Assert.AreEqual(WeaponBonusKind.Arsenal, slots.Bonus.Kind);
        }

        [UnityTest]
        public IEnumerator PresetSwitch_DuringRecovery_CancelsTheSkill()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            yield return Tap(keyboard.jKey);
            Assert.AreEqual("連斬", executor.Current.displayName);
            yield return WaitUntil(() => executor.DebugLabel.Contains("Recovery"), 3f);
            Assert.IsTrue(executor.CanPresetCancel(2));

            yield return Tap(keyboard.digit3Key);
            Assert.IsFalse(executor.IsBusy, "プリセットの切り替えで技がキャンセルされていない");
            Assert.IsTrue(style.Announcements.Exists(a => a.text == "SWAP CANCEL"));
        }

        [UnityTest]
        public IEnumerator EveryDatabaseSkill_CanBeUsedWithoutErrors()
        {
            yield return LoadArena(SwapMode.Preset);
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            yield return Tap(keyboard.tabKey);

            foreach (var skill in slots.Database.skills)
            {
                Assert.IsTrue(slots.AssignPresetSkill(0, skill.slot, skill), skill.name);
                yield return UseSlot(skill.slot);
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

            foreach (var weapon in slots.Weapons)
            {
                if (weapon.masteryFinisher == null) continue;
                for (int s = 0; s < 3; s++)
                {
                    SkillData pick = null;
                    foreach (var skill in slots.Database.ForSlot((SlotType)s))
                    {
                        if (skill.weapon == weapon.weapon)
                        {
                            pick = skill;
                            break;
                        }
                    }
                    Assert.NotNull(pick, $"{weapon.displayName} の {(SlotType)s} スキルがない");
                    slots.AssignPresetSkill(0, (SlotType)s, pick);
                }
                Assert.AreSame(weapon.masteryFinisher, slots.MasteryFinisher);

                yield return WaitUntil(() => !executor.IsBusy && !executor.IsStunned, 6f);
                yield return Tap(keyboard.fKey);
                Assert.AreSame(weapon.masteryFinisher, executor.Current, $"{weapon.displayName} のフィニッシャーが出ない");
                yield return WaitUntil(() => !executor.IsBusy, 8f);
            }
        }

        [UnityTest]
        public IEnumerator LoadoutEditor_PausesAndSaves()
        {
            yield return LoadArena(SwapMode.Preset);

            yield return Tap(keyboard.pKey);
            Assert.IsTrue(LoadoutEditorUI.IsOpen);
            Assert.IsTrue(GamePause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            var fireball = slots.Database.Find("Skill_Staff_Fireball");
            Assert.IsTrue(slots.AssignPresetSkill(0, SlotType.AttackB, fireball));
            Assert.AreSame(fireball, slots.GetCurrent(SlotType.AttackB));

            yield return Tap(keyboard.pKey);
            Assert.IsFalse(LoadoutEditorUI.IsOpen);
            Assert.IsFalse(GamePause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);

            Assert.IsTrue(LoadoutStorage.TryLoad(slots.Database, out var saved));
            Assert.AreEqual(SwapMode.Preset, saved.mode);
            Assert.AreSame(fireball, saved.presets[0].attackB);
        }

        EnemyProfile TrainingDummy() => director.Stages[0].trainingDummies[0];

        /// <summary>ウェーブを止めて、目の前に訓練用の人形を1体置く</summary>
        IEnumerator PlaceDummy(EnemyProfile profile, float distance)
        {
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            ClearEnemies();
            yield return null;
            var enemy = director.SpawnExtra(profile, player.position + player.forward * distance);
            yield return null;
            enemy.transform.position = player.position + player.forward * distance;
            Physics.SyncTransforms();
            yield return null;
            dummy = enemy;
        }

        EnemyController dummy;

        [UnityTest]
        public IEnumerator Hud_ShowsPresetSkills_AfterSwitchAndEdit()
        {
            yield return LoadArena(SwapMode.Preset);
            var hud = Object.FindFirstObjectByType<BattleHud>();

            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual("魔弾", hud.ShownSkill(SlotType.AttackA).displayName, "プリセットを切り替えても表示が変わらない");

            var fireball = slots.Database.Find("Skill_Staff_Fireball");
            slots.AssignPresetSkill(1, SlotType.AttackB, fireball);
            Assert.AreSame(fireball, hud.ShownSkill(SlotType.AttackB), "編成を変えても表示が変わらない");
        }

        [UnityTest]
        public IEnumerator Weakness_MultipliesDamage()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            // 同じ人形を、弱点あり(剣)となしで用意して、連斬の1段目のダメージを比べる
            var weakProfile = Object.Instantiate(TrainingDummy());
            weakProfile.weaknesses = new[] { WeaponType.Sword };
            var plainProfile = Object.Instantiate(TrainingDummy());
            plainProfile.weaknesses = new WeaponType[0];

            yield return PlaceDummy(plainProfile, 1.6f);
            float before = dummy.Damageable.Health;
            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => dummy.Damageable.Health < before, 2f);
            float plainDamage = dummy.Damageable.LastDamage;
            yield return WaitUntil(() => !executor.IsBusy, 3f);

            yield return PlaceDummy(weakProfile, 1.6f);
            before = dummy.Damageable.Health;
            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => dummy.Damageable.Health < before, 2f);
            float weakDamage = dummy.Damageable.LastDamage;

            Assert.AreEqual(plainDamage * 1.5f, weakDamage, 0.01f, "弱点のダメージが 1.5 倍になっていない");
        }

        [UnityTest]
        public IEnumerator SwapThenHit_BuildsChain_AndChangeAttackHitsNearbyEnemy()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);
            yield return PlaceDummy(TrainingDummy(), 1.2f);
            Assert.AreEqual(0, executor.SwapChain.Chain);
            Assert.IsTrue(executor.ChangeAttackReady);

            // 切り替えた瞬間のチェンジアタックが、そばの人形に当たる
            float before = dummy.Damageable.Health;
            yield return Tap(keyboard.digit4Key);
            Assert.Less(dummy.Damageable.Health, before, "チェンジアタックが当たっていない");
            Assert.IsFalse(executor.ChangeAttackReady, "チェンジアタックにクールダウンがない");
            Assert.AreEqual(0, executor.SwapChain.Chain, "チェンジアタックだけでチェインが上がった");

            // 切り替えてから技を当てるとチェインが上がる(混成: 連打)
            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => executor.SwapChain.Chain == 1, 2f);
            Assert.AreEqual(1.1f, executor.SwapChain.DamageMultiplier, 1e-4f);
            Assert.IsTrue(style.Announcements.Exists(a => a.text.StartsWith("CHAIN")));
        }

        EnemyProfile ShadowBoss()
        {
            foreach (var stage in director.Stages)
            {
                if (stage.waves == null) continue;
                foreach (var wave in stage.waves)
                foreach (var enemy in wave.enemies)
                {
                    if (enemy != null && enemy.displayName == "影刃") return enemy;
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
            var boss = dummy;

            // 背後取り(3番目の攻撃)を出させる
            Assert.IsTrue(boss.ForceAttack(2));
            yield return new WaitForSeconds(boss.Profile.attacks[2].windup * 0.5f + 0.15f);

            Vector3 toBoss = boss.transform.position - player.position;
            toBoss.y = 0f;
            Assert.Less(Vector3.Dot(toBoss.normalized, player.forward), -0.5f, "プレイヤーの背後に回っていない");
            Assert.Less(toBoss.magnitude, 5f);
        }

        [UnityTest]
        public IEnumerator ShadowBoss_EntersPhase2AtHalfHealth()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return PlaceDummy(ShadowBoss(), 8f);
            var boss = dummy;
            var damageable = boss.Damageable;
            Assert.IsFalse(boss.IsPhase2);

            // アーマーを割ったうえで、体力を半分より少し多いところまで減らす
            damageable.ApplyHit(new HitInfo { damage = 0f, armorBreak = 999f });
            damageable.ApplyHit(new HitInfo { damage = damageable.Health - damageable.MaxHealth * 0.55f });
            Assert.IsFalse(boss.IsPhase2, "半分を切る前に第二形態になった");

            bool announced = false;
            System.Action<EnemyController> onPhase = e => announced |= e == boss;
            EnemyController.PhaseChanged += onPhase;
            damageable.ApplyHit(new HitInfo { damage = damageable.MaxHealth * 0.1f });
            EnemyController.PhaseChanged -= onPhase;

            Assert.IsTrue(boss.IsPhase2, "半分を切っても第二形態にならない");
            Assert.IsTrue(announced);
            Assert.AreEqual(damageable.MaxArmor, damageable.Armor, 1e-3f, "第二形態でアーマーが戻っていない");
            // 第二形態の攻撃も出せる(通常3 + 追加2)
            Assert.IsTrue(boss.ForceAttack(4));
            yield return new WaitForSeconds(1.5f);
        }

        [UnityTest]
        public IEnumerator Arena_StartsOnStageSelect()
        {
            yield return LoadArena(stage: -1);
            Assert.AreEqual(ArenaDirector.GameState.StageSelect, director.State);
            Assert.GreaterOrEqual(director.Stages.Count, 5);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(0, EnemyController.Active.Count, "ステージ選択中に敵が出ている");
        }

        [UnityTest]
        public IEnumerator EveryStage_StartsAndSpawnsEnemies()
        {
            yield return LoadArena(stage: -1);
            int count = director.Stages.Count;
            for (int i = 0; i < count; i++)
            {
                // 探索は部屋に入るまで敵が出ない(ExplorationPlayTests で確かめる)
                if (director.Stages[i].kind == StageKind.Exploration) continue;
                yield return LoadArena(SwapMode.Preset, i);
                MakePlayerSturdy();
                yield return WaitUntil(() => EnemyController.Active.Count > 0, 6f);
                Assert.AreEqual(director.Stages[i], director.CurrentStage);
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

            var profiles = new System.Collections.Generic.HashSet<EnemyProfile>();
            foreach (var stage in director.Stages)
            {
                if (stage.waves != null) foreach (var wave in stage.waves) foreach (var e in wave.enemies) profiles.Add(e);
                if (stage.trainingDummies != null) foreach (var e in stage.trainingDummies) profiles.Add(e);
                if (stage.endlessBosses != null) foreach (var e in stage.endlessBosses) profiles.Add(e);
                foreach (var entry in stage.endlessPool) profiles.Add(entry.profile);
            }
            Assert.GreaterOrEqual(profiles.Count, 11);

            // 1種類ずつ目の前に出して、しばらく戦わせる(遠距離・自爆・召喚・飛行などの処理を通す)
            foreach (var profile in profiles)
            {
                var enemy = director.SpawnExtra(profile, player.position + player.forward * 6f);
                Assert.NotNull(enemy, profile.displayName);
                yield return new WaitForSeconds(2.5f);
                ClearEnemies();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ClearingStage_RecordsAndMovesToNextStage()
        {
            yield return LoadArena(SwapMode.Preset);
            var stageName = director.CurrentStage.name;
            StageRecords.Clear(stageName);
            MakePlayerSturdy();

            // 出てきた敵を倒し続けてクリアまで進める
            float start = Time.realtimeSinceStartup;
            while (director.State != ArenaDirector.GameState.Cleared)
            {
                foreach (var enemy in EnemyController.Active.ToArray()) enemy.Damageable.Kill();
                if (Time.realtimeSinceStartup - start > 40f) Assert.Fail("クリアまで進まない");
                yield return null;
            }

            Assert.IsTrue(StageRecords.IsCleared(stageName));
            Assert.IsTrue(director.NewRecord);
            Assert.IsTrue(director.HasNextStage);

            yield return Tap(keyboard.nKey);
            yield return null;
            yield return null;
            var next = Object.FindFirstObjectByType<ArenaDirector>();
            Assert.AreEqual(DefaultStage + 1, next.StageIndex, "次のステージが始まっていない");
            StageRecords.Clear(stageName);
        }

        [UnityTest]
        public IEnumerator EveryRackSkill_CanBeUsedWithoutErrors()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            yield return Tap(keyboard.tabKey);

            var keys = new[] { keyboard.jKey, keyboard.kKey, keyboard.lKey };
            var swaps = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key };
            for (int candidate = 0; candidate < SkillRack.Capacity; candidate++)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    yield return WaitUntil(() => !executor.IsBusy && executor.GetComponent<PlayerMotor>().IsGrounded, 5f);
                    yield return Tap(keys[slot]);
                    yield return WaitUntil(() => !executor.IsBusy, 5f);
                }
                foreach (var swap in swaps) yield return Tap(swap);
            }
        }

        [UnityTest]
        public IEnumerator LosingFocus_ReleasesHeldMoveKey()
        {
            yield return LoadArena();
            var reader = Object.FindFirstObjectByType<PlayerInputReader>();

            Press(keyboard.wKey);
            yield return null;
            Assert.Greater(reader.Move.y, 0.5f, "W を押しても前進の入力にならない");

            // ロックオン(Tab)やウィンドウ切り替えでフォーカスが外れ、W を離したことが届かなかった状況
            reader.SendMessage("OnApplicationFocus", false);
            yield return null;
            Assert.AreEqual(Vector2.zero, reader.Move, "フォーカスが外れても移動の入力が残っている");
        }

        [UnityTest]
        public IEnumerator HelpKey0_TogglesGuide_AndGuideFollowsLastDevice()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            yield return LoadArena();
            var hud = Object.FindFirstObjectByType<BattleHud>();

            Assert.IsTrue(hud.IsHelpShown);
            yield return Tap(keyboard.digit0Key);
            Assert.IsFalse(hud.IsHelpShown, "0 キーで操作説明が閉じない");
            yield return Tap(keyboard.digit0Key);
            Assert.IsTrue(hud.IsHelpShown, "0 キーで操作説明が開かない");

            // 0 は切り替えに使っていないので、スキルは変わらない
            Assert.AreEqual("連斬", slots.GetCurrent(SlotType.AttackA).displayName);

            yield return Tap(gamepad.buttonSouth);
            Assert.IsTrue(hud.UsingGamepad, "ゲームパッドを押しても表示がゲームパッド用にならない");
            yield return Tap(gamepad.rightStickButton);
            Assert.IsFalse(hud.IsHelpShown, "R3 で操作説明が閉じない");

            yield return Tap(keyboard.wKey);
            Assert.IsFalse(hud.UsingGamepad, "キーボードを押しても表示がキーボード用に戻らない");
        }
    }
}
