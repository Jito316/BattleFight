using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 試作アリーナのシーンとデータアセットを生成する。
    /// メニュー: BattleFight/Build Prototype Arena
    /// バッチ: -executeMethod BattleFight.EditorTools.PrototypeSceneBuilder.BuildFromCommandLine
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const string mRoot = "Assets/_Project";
        const string mDataRoot = mRoot + "/Data";
        public const string ScenePath = mRoot + "/Scenes/Prototype_Arena.unity";
        const string mHudFontPath = mRoot + "/Fonts/NotoSansJP-Bold-Subset.otf";

        [MenuItem("BattleFight/Build Prototype Arena")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(iResetData: false);
        }

        [MenuItem("BattleFight/Reset Data To Defaults")]
        public static void ResetDataFromMenu()
        {
            if (!EditorUtility.DisplayDialog("BattleFight", "スキル・武器・敵のアセットをたたき台の数値で上書きします。よろしいですか?", "上書き", "キャンセル"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(iResetData: true);
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                Build(iResetData: false);
            }
            catch (Exception aException)
            {
                Debug.LogException(aException);
                EditorApplication.Exit(1);
            }
        }

        static void Build(bool iResetData)
        {
            EnsureFolder(mRoot, "Data");
            EnsureFolder(mDataRoot, "Skills");
            EnsureFolder(mDataRoot, "Weapons");
            EnsureFolder(mDataRoot, "Enemies");
            EnsureFolder(mRoot, "Materials");
            EnsureFolder(mRoot, "Scenes");

            // NewScene は未使用アセットを解放するので、アセットを読み込む前に作る
            var aScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- データ ----------
            var aSwordCombo = Asset<SkillData>("Skills/Skill_Sword_Combo", iResetData, PrototypeDefaults.SwordCombo);
            var aSwordIai = Asset<SkillData>("Skills/Skill_Sword_Iai", iResetData, PrototypeDefaults.SwordIai);
            var aSwordStep = Asset<SkillData>("Skills/Skill_Sword_Step", iResetData, PrototypeDefaults.SwordStep);
            var aSwordFinisher = Asset<SkillData>("Skills/Skill_Sword_Finisher", iResetData, PrototypeDefaults.SwordFinisher);

            var aHammerCombo = Asset<SkillData>("Skills/Skill_Hammer_Combo", iResetData, PrototypeDefaults.HammerCombo);
            var aHammerQuake = Asset<SkillData>("Skills/Skill_Hammer_Quake", iResetData, PrototypeDefaults.HammerQuake);
            var aHammerJump = Asset<SkillData>("Skills/Skill_Hammer_Jump", iResetData, PrototypeDefaults.HammerJump);
            var aHammerFinisher = Asset<SkillData>("Skills/Skill_Hammer_Finisher", iResetData, PrototypeDefaults.HammerFinisher);

            var aChainCombo = Asset<SkillData>("Skills/Skill_Chain_Combo", iResetData, PrototypeDefaults.ChainCombo);
            var aChainPull = Asset<SkillData>("Skills/Skill_Chain_Pull", iResetData, PrototypeDefaults.ChainPull);
            var aChainWire = Asset<SkillData>("Skills/Skill_Chain_Wire", iResetData, PrototypeDefaults.ChainWire);
            var aChainFinisher = Asset<SkillData>("Skills/Skill_Chain_Finisher", iResetData, PrototypeDefaults.ChainFinisher);

            // 追加の候補(剣・大槌・鎖)
            var aSwordThrust = Asset<SkillData>("Skills/Skill_Sword_Thrust", iResetData, PrototypeDefaults.SwordThrust);
            var aSwordWave = Asset<SkillData>("Skills/Skill_Sword_Wave", iResetData, PrototypeDefaults.SwordWave);
            var aSwordPhantom = Asset<SkillData>("Skills/Skill_Sword_Phantom", iResetData, PrototypeDefaults.SwordPhantom);
            var aHammerSpin = Asset<SkillData>("Skills/Skill_Hammer_Spin", iResetData, PrototypeDefaults.HammerSpin);
            var aHammerMeteor = Asset<SkillData>("Skills/Skill_Hammer_Meteor", iResetData, PrototypeDefaults.HammerMeteor);
            var aHammerCharge = Asset<SkillData>("Skills/Skill_Hammer_Charge", iResetData, PrototypeDefaults.HammerCharge);
            var aChainRing = Asset<SkillData>("Skills/Skill_Chain_Ring", iResetData, PrototypeDefaults.ChainRing);
            var aChainBind = Asset<SkillData>("Skills/Skill_Chain_Bind", iResetData, PrototypeDefaults.ChainBind);

            // 杖(魔法)
            var aStaffBolt = Asset<SkillData>("Skills/Skill_Staff_Bolt", iResetData, PrototypeDefaults.StaffBolt);
            var aStaffMelee = Asset<SkillData>("Skills/Skill_Staff_Melee", iResetData, PrototypeDefaults.StaffMelee);
            var aStaffFireball = Asset<SkillData>("Skills/Skill_Staff_Fireball", iResetData, PrototypeDefaults.StaffFireball);
            var aStaffThunder = Asset<SkillData>("Skills/Skill_Staff_Thunder", iResetData, PrototypeDefaults.StaffThunder);
            var aStaffFrost = Asset<SkillData>("Skills/Skill_Staff_Frost", iResetData, PrototypeDefaults.StaffFrost);
            var aStaffBlink = Asset<SkillData>("Skills/Skill_Staff_Blink", iResetData, PrototypeDefaults.StaffBlink);
            var aStaffLevitate = Asset<SkillData>("Skills/Skill_Staff_Levitate", iResetData, PrototypeDefaults.StaffLevitate);
            var aStaffFinisher = Asset<SkillData>("Skills/Skill_Staff_Finisher", iResetData, PrototypeDefaults.StaffFinisher);

            // 銃(SF)
            var aGunRapid = Asset<SkillData>("Skills/Skill_Gun_Rapid", iResetData, PrototypeDefaults.GunRapid);
            var aGunShotgun = Asset<SkillData>("Skills/Skill_Gun_Shotgun", iResetData, PrototypeDefaults.GunShotgun);
            var aGunLaser = Asset<SkillData>("Skills/Skill_Gun_Laser", iResetData, PrototypeDefaults.GunLaser);
            var aGunGrenade = Asset<SkillData>("Skills/Skill_Gun_Grenade", iResetData, PrototypeDefaults.GunGrenade);
            var aGunJet = Asset<SkillData>("Skills/Skill_Gun_Jet", iResetData, PrototypeDefaults.GunJet);
            var aGunSlide = Asset<SkillData>("Skills/Skill_Gun_Slide", iResetData, PrototypeDefaults.GunSlide);
            var aGunFinisher = Asset<SkillData>("Skills/Skill_Gun_Finisher", iResetData, PrototypeDefaults.GunFinisher);

            // 拳
            var aGauntletRush = Asset<SkillData>("Skills/Skill_Gauntlet_Rush", iResetData, PrototypeDefaults.GauntletRush);
            var aGauntletUppercut = Asset<SkillData>("Skills/Skill_Gauntlet_Uppercut", iResetData, PrototypeDefaults.GauntletUppercut);
            var aGauntletRocket = Asset<SkillData>("Skills/Skill_Gauntlet_Rocket", iResetData, PrototypeDefaults.GauntletRocket);
            var aGauntletDash = Asset<SkillData>("Skills/Skill_Gauntlet_Dash", iResetData, PrototypeDefaults.GauntletDash);
            var aGauntletFinisher = Asset<SkillData>("Skills/Skill_Gauntlet_Finisher", iResetData, PrototypeDefaults.GauntletFinisher);

            var aSword = Asset<WeaponTypeData>("Weapons/Weapon_Sword", iResetData, iWeapon => PrototypeDefaults.Sword(iWeapon, aSwordFinisher));
            var aHammer = Asset<WeaponTypeData>("Weapons/Weapon_Hammer", iResetData, iWeapon => PrototypeDefaults.Hammer(iWeapon, aHammerFinisher));
            var aChain = Asset<WeaponTypeData>("Weapons/Weapon_Chain", iResetData, iWeapon => PrototypeDefaults.Chain(iWeapon, aChainFinisher));
            var aStaff = Asset<WeaponTypeData>("Weapons/Weapon_Staff", iResetData, iWeapon => PrototypeDefaults.Staff(iWeapon, aStaffFinisher));
            var aGun = Asset<WeaponTypeData>("Weapons/Weapon_Gun", iResetData, iWeapon => PrototypeDefaults.Gun(iWeapon, aGunFinisher));
            var aGauntlet = Asset<WeaponTypeData>("Weapons/Weapon_Gauntlet", iResetData, iWeapon => PrototypeDefaults.Gauntlet(iWeapon, aGauntletFinisher));
            var aWeapons = new Object[] { aSword, aHammer, aChain, aStaff, aGun, aGauntlet };

            // 編成画面で選べるスキル(フィニッシャーは除く)。常に最新の一覧で作り直す
            var aDatabase = Asset<SkillDatabase>("SkillDatabase", true, iDatabase => iDatabase.Skills = new System.Collections.Generic.List<SkillData>
            {
                aSwordCombo, aSwordThrust, aSwordIai, aSwordWave, aSwordStep, aSwordPhantom,
                aHammerCombo, aHammerSpin, aHammerQuake, aHammerMeteor, aHammerJump, aHammerCharge,
                aChainCombo, aChainRing, aChainPull, aChainBind, aChainWire,
                aStaffBolt, aStaffMelee, aStaffFireball, aStaffThunder, aStaffFrost, aStaffBlink, aStaffLevitate,
                aGunRapid, aGunShotgun, aGunLaser, aGunGrenade, aGunJet, aGunSlide,
                aGauntletRush, aGauntletUppercut, aGauntletRocket, aGauntletDash,
            });

            var aStyleConfig = Asset<StyleRankConfig>("StyleRankConfig", iResetData, _ => { });

            var aGrunt = Asset<EnemyProfile>("Enemies/Enemy_Grunt", iResetData, PrototypeDefaults.Grunt);
            var aArmored = Asset<EnemyProfile>("Enemies/Enemy_Armored", iResetData, PrototypeDefaults.Armored);
            var aBoss = Asset<EnemyProfile>("Enemies/Enemy_Boss", iResetData, PrototypeDefaults.Boss);
            var aShooter = Asset<EnemyProfile>("Enemies/Enemy_Shooter", iResetData, PrototypeDefaults.Shooter);
            var aDasher = Asset<EnemyProfile>("Enemies/Enemy_Dasher", iResetData, PrototypeDefaults.Dasher);
            var aFlyer = Asset<EnemyProfile>("Enemies/Enemy_Flyer", iResetData, PrototypeDefaults.Flyer);
            var aBomber = Asset<EnemyProfile>("Enemies/Enemy_Bomber", iResetData, PrototypeDefaults.Bomber);
            var aDummy = Asset<EnemyProfile>("Enemies/Enemy_Dummy", iResetData, PrototypeDefaults.Dummy);
            var aArmoredDummy = Asset<EnemyProfile>("Enemies/Enemy_ArmoredDummy", iResetData, PrototypeDefaults.ArmoredDummy);
            var aFlyingDummy = Asset<EnemyProfile>("Enemies/Enemy_FlyingDummy", iResetData, PrototypeDefaults.FlyingDummy);
            var aMagus = Asset<EnemyProfile>("Enemies/Enemy_Magus", iResetData, iProfile => PrototypeDefaults.Magus(iProfile, aFlyer));
            var aShadow = Asset<EnemyProfile>("Enemies/Enemy_Shadow", iResetData, PrototypeDefaults.Shadow);

            // 弱点(切り替えるメリット)。まだ設定されていない敵にだけ入れる
            EnsureWeaknesses(aGrunt, WeaponType.Sword, WeaponType.Gauntlet);
            EnsureWeaknesses(aArmored, WeaponType.Hammer, WeaponType.Gauntlet);
            EnsureWeaknesses(aShooter, WeaponType.Chain, WeaponType.Sword);
            EnsureWeaknesses(aDasher, WeaponType.Staff, WeaponType.Gun);
            EnsureWeaknesses(aFlyer, WeaponType.Gun, WeaponType.Staff);
            EnsureWeaknesses(aBomber, WeaponType.Gun, WeaponType.Chain);
            EnsureWeaknesses(aBoss, WeaponType.Hammer, WeaponType.Chain);
            EnsureWeaknesses(aMagus, WeaponType.Gun, WeaponType.Gauntlet);
            EnsureWeaknesses(aDummy, WeaponType.Sword);
            EnsureWeaknesses(aArmoredDummy, WeaponType.Hammer);
            EnsureWeaknesses(aFlyingDummy, WeaponType.Gun);

            // プレイヤーと同じスキルを使う敵「写し身」。型(武器種ごとのスキルの組)を切り替え、そのたびに弱点が変わる
            var aMirrorBlade = Asset<EnemyProfile>("Enemies/Enemy_MirrorBlade", iResetData, iProfile => PrototypeDefaults.MirrorBlade(iProfile,
                SkillSet("剣の型", aSword, new[] { WeaponType.Hammer, WeaponType.Gauntlet }, aSwordCombo, aSwordIai, aSwordStep),
                SkillSet("大槌の型", aHammer, new[] { WeaponType.Sword, WeaponType.Gun }, aHammerCombo, aHammerQuake, aHammerJump)));
            var aMirrorCaster = Asset<EnemyProfile>("Enemies/Enemy_MirrorCaster", iResetData, iProfile => PrototypeDefaults.MirrorCaster(iProfile,
                SkillSet("杖の型", aStaff, new[] { WeaponType.Sword, WeaponType.Gauntlet }, aStaffBolt, aStaffThunder, aStaffBlink),
                SkillSet("銃の型", aGun, new[] { WeaponType.Chain, WeaponType.Staff }, aGunRapid, aGunGrenade, aGunSlide)));
            var aMirrorBrawler = Asset<EnemyProfile>("Enemies/Enemy_MirrorBrawler", iResetData, iProfile => PrototypeDefaults.MirrorBrawler(iProfile,
                SkillSet("拳の型", aGauntlet, new[] { WeaponType.Hammer, WeaponType.Staff }, aGauntletRush, aGauntletUppercut, aGauntletDash),
                SkillSet("鎖の型", aChain, new[] { WeaponType.Gauntlet, WeaponType.Gun }, aChainCombo, aChainBind, aChainWire)));

            // ボスにもプレイヤーのスキルを混ぜる(弱点はボスのまま)。影刃は型を切り替える
            EnsureSkillSets(aBoss, 0f, SkillSet("大槌の技", aHammer, null, aHammerQuake, aHammerJump));
            EnsureSkillSets(aMagus, 0f, SkillSet("杖の技", aStaff, null, aStaffThunder, aStaffBolt));
            EnsureSkillSets(aShadow, 8f,
                SkillSet("剣の型", aSword, null, aSwordIai, aSwordWave, aSwordPhantom),
                SkillSet("鎖の型", aChain, null, aChainBind, aChainRing));

            EnsureFolder(mDataRoot, "Stages");
            var aStages = new Object[]
            {
                Asset<StageData>("Stages/Stage_0_Training", iResetData, iStage => PrototypeDefaults.TrainingStage(iStage, aDummy, aArmoredDummy, aFlyingDummy)),
                Asset<StageData>("Stages/Stage_1_Beast", iResetData, iStage => PrototypeDefaults.Act1(iStage, aGrunt, aArmored, aBoss)),
                Asset<StageData>("Stages/Stage_2_Skyfire", iResetData, iStage => PrototypeDefaults.Act2(iStage, aGrunt, aShooter, aDasher, aFlyer, aArmored, aMagus)),
                Asset<StageData>("Stages/Stage_3_Inferno", iResetData,
                    iStage => PrototypeDefaults.Act3(iStage, aBomber, aDasher, aShooter, aArmored, aFlyer, aBoss, aMagus)),
                Asset<StageData>("Stages/Stage_4_Shadow", iResetData,
                    iStage => PrototypeDefaults.Act4(iStage, aDasher, aShooter, aFlyer, aArmored, aBomber, aShadow)),
                Asset<StageData>("Stages/Stage_5_Mirror", iResetData,
                    iStage => PrototypeDefaults.Act5(iStage, aGrunt, aShooter, aMirrorBlade, aMirrorCaster, aMirrorBrawler, aShadow)),
                Asset<StageData>("Stages/Stage_9_Endless", iResetData,
                    iStage => PrototypeDefaults.Endless(iStage, aGrunt, aDasher, aShooter, aBomber, aFlyer, aArmored, aBoss, aMagus)),
                Asset<StageData>("Stages/Stage_10_Explore", iResetData, PrototypeDefaults.ExplorationStage),
            };
            EnsureStageThemes(System.Linq.Enumerable.OfType<StageData>(aStages));
            // 既にあるエンドレスにも、新しいボスを加える
            var aEndless = (StageData)aStages[aStages.Length - 2];
            EnsureEndlessBoss(aEndless, aShadow);
            // 写し身もエンドレスに混ぜる
            EnsureEndlessEntry(aEndless, aMirrorBlade, 3f, 4);
            EnsureEndlessEntry(aEndless, aMirrorCaster, 3f, 5);
            EnsureEndlessEntry(aEndless, aMirrorBrawler, 3f, 6);

            AssetDatabase.SaveAssets();

            // ---------- シーン ----------
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.22f);
            // 霧はステージごとに色と距離を変える。ビルドで霧のシェーダーが削られないよう、シーンでも使っておく
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.72f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 150f;

            var aLightObject = new GameObject("Directional Light");
            var aLight = aLightObject.AddComponent<Light>();
            aLight.type = LightType.Directional;
            aLight.intensity = 1.3f;
            aLight.shadows = LightShadows.Soft;
            aLightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var aArenaRoot = BuildArena();
            BuildArenaDecor(aArenaRoot);

            // 探索マップ(アリーナとは離れた場所に作る。探索のステージでだけ出す)
            var aExplorationSkills = new ExplorationSkills
            {
                SwordCombo = aSwordCombo, SwordIai = aSwordIai, SwordStep = aSwordStep,
                HammerCombo = aHammerCombo, HammerSpin = aHammerSpin, HammerQuake = aHammerQuake, HammerMeteor = aHammerMeteor,
                HammerJump = aHammerJump,
                ChainCombo = aChainCombo, ChainPull = aChainPull, ChainWire = aChainWire,
                StaffBolt = aStaffBolt, StaffBlink = aStaffBlink, StaffFireball = aStaffFireball, StaffThunder = aStaffThunder,
            };
            var aExplorationEnemies = new ExplorationEnemies
            {
                Grunt = aGrunt, Armored = aArmored, Shooter = aShooter, Dasher = aDasher, Flyer = aFlyer, Boss = aBoss,
            };
            var aExploration = BuildExploration(aExplorationSkills, aExplorationEnemies);

            // プレイヤー
            var aPlayer = new GameObject("Player") { tag = "Player" };
            aPlayer.transform.position = new Vector3(0f, 0.1f, -10f);
            var aController = aPlayer.AddComponent<CharacterController>();
            aController.center = new Vector3(0f, 1f, 0f);
            aController.height = 2f;
            aController.radius = 0.4f;
            aController.stepOffset = 0.3f;
            aController.skinWidth = 0.05f;

            var aBody = Part(PrimitiveType.Capsule, "Body", aPlayer.transform, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f),
                Mat("Player", new Color(0.9f, 0.9f, 0.95f)));
            Part(PrimitiveType.Cube, "Visor", aBody, new Vector3(0f, 0.5f, 0.4f), new Vector3(0.7f, 0.15f, 0.3f),
                Mat("Visor", new Color(0.1f, 0.6f, 1f)));
            var aHand = new GameObject("HandAnchor").transform;
            aHand.SetParent(aPlayer.transform, false);
            aHand.localPosition = new Vector3(0.45f, 1.15f, 0.25f);

            var aDamageable = aPlayer.AddComponent<Damageable>();
            var aInput = aPlayer.AddComponent<PlayerInputReader>();
            var aMotor = aPlayer.AddComponent<PlayerMotor>();
            var aSlots = aPlayer.AddComponent<SkillSlotController>();
            var aWeaponHolder = aPlayer.AddComponent<WeaponHolder>();
            var aLockOn = aPlayer.AddComponent<LockOnSystem>();
            var aStyle = aPlayer.AddComponent<StyleRankSystem>();
            var aExecutor = aPlayer.AddComponent<SkillExecutor>();
            var aPlayerController = aPlayer.AddComponent<PlayerController>();

            // カメラ
            var aCameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var aCamera = aCameraObject.AddComponent<Camera>();
            aCamera.fieldOfView = 55f;
            aCamera.clearFlags = CameraClearFlags.SolidColor;
            aCamera.backgroundColor = new Color(0.55f, 0.65f, 0.78f);
            aCamera.nearClipPlane = 0.1f;
            aCameraObject.AddComponent<AudioListener>();
            var aCameraRig = aCameraObject.AddComponent<ThirdPersonCamera>();
            aCameraObject.transform.position = new Vector3(0f, 4f, -17f);

            // ゲーム進行
            var aDirectorObject = new GameObject("GameDirector");
            var aDirector = aDirectorObject.AddComponent<ArenaDirector>();
            var aFeedback = aDirectorObject.AddComponent<CombatFeedback>();
            // UI(UI Toolkit)。画面ごとに UIDocument を分け、同じ PanelSettings で重ねる
            var aPanel = BuildPanelSettings();
            var aHud = UiDocument<BattleHud>("UI_Hud", "Hud", aPanel, 0);
            var aStageSelect = UiDocument<StageSelectUI>("UI_StageSelect", "StageSelect", aPanel, 10);
            var aLoadoutEditor = UiDocument<LoadoutEditorUI>("UI_LoadoutEditor", "LoadoutEditor", aPanel, 20);

            // ---------- 参照の設定 ----------
            Configure(aDamageable, iSerialized =>
            {
                iSerialized.FindProperty("mTeam").enumValueIndex = (int)Team.Player;
                iSerialized.FindProperty("mMaxHealth").floatValue = 200f;
            });
            Configure(aSlots, iSerialized =>
            {
                SetArray(iSerialized, "mAttackARack", aSwordCombo, aHammerCombo, aChainCombo);
                SetArray(iSerialized, "mAttackBRack", aSwordIai, aHammerQuake, aChainPull);
                SetArray(iSerialized, "mMovementRack", aSwordStep, aHammerJump, aChainWire);
                SetArray(iSerialized, "mWeaponDatabase", aWeapons);
                iSerialized.FindProperty("mDatabase").objectReferenceValue = aDatabase;
                // プリセット方式を試せるよう、最初からプリセット方式にしておく
                iSerialized.FindProperty("mMode").enumValueIndex = (int)SwapMode.Preset;
                var aPresets = iSerialized.FindProperty("mDefaultPresets");
                SetPreset(aPresets, 0, "剣士", aSwordCombo, aSwordIai, aSwordStep);
                SetPreset(aPresets, 1, "魔導士", aStaffBolt, aStaffThunder, aStaffBlink);
                SetPreset(aPresets, 2, "ガンナー", aGunRapid, aGunGrenade, aGunJet);
                SetPreset(aPresets, 3, "混成", aGauntletRush, aHammerQuake, aChainWire);
            });
            Configure(aWeaponHolder, iSerialized =>
            {
                iSerialized.FindProperty("mHandAnchor").objectReferenceValue = aHand;
                iSerialized.FindProperty("mSlots").objectReferenceValue = aSlots;
                iSerialized.FindProperty("mSurfaceMaterial").objectReferenceValue = Mat("Surface", Color.white);
                SetArray(iSerialized, "mWeaponDatabase", aWeapons);
            });
            Configure(aLockOn, iSerialized =>
            {
                iSerialized.FindProperty("mInput").objectReferenceValue = aInput;
                iSerialized.FindProperty("mViewTransform").objectReferenceValue = aCameraObject.transform;
            });
            Configure(aStyle, iSerialized => iSerialized.FindProperty("mConfig").objectReferenceValue = aStyleConfig);
            Configure(aExecutor, iSerialized =>
            {
                iSerialized.FindProperty("mInput").objectReferenceValue = aInput;
                iSerialized.FindProperty("mMotor").objectReferenceValue = aMotor;
                iSerialized.FindProperty("mSlots").objectReferenceValue = aSlots;
                iSerialized.FindProperty("mWeapons").objectReferenceValue = aWeaponHolder;
                iSerialized.FindProperty("mLockOn").objectReferenceValue = aLockOn;
                iSerialized.FindProperty("mStyle").objectReferenceValue = aStyle;
                iSerialized.FindProperty("mSelf").objectReferenceValue = aDamageable;
                iSerialized.FindProperty("mView").objectReferenceValue = aCamera;
            });
            Configure(aPlayerController, iSerialized =>
            {
                iSerialized.FindProperty("mInput").objectReferenceValue = aInput;
                iSerialized.FindProperty("mMotor").objectReferenceValue = aMotor;
                iSerialized.FindProperty("mExecutor").objectReferenceValue = aExecutor;
                iSerialized.FindProperty("mDamageable").objectReferenceValue = aDamageable;
                iSerialized.FindProperty("mStyle").objectReferenceValue = aStyle;
                iSerialized.FindProperty("mCameraTransform").objectReferenceValue = aCameraObject.transform;
            });
            Configure(aCameraRig, iSerialized =>
            {
                iSerialized.FindProperty("mTarget").objectReferenceValue = aPlayer.transform;
                iSerialized.FindProperty("mInput").objectReferenceValue = aInput;
                iSerialized.FindProperty("mLockOn").objectReferenceValue = aLockOn;
            });
            // 実行時に生成する仮モデル用。シーンから参照してシェーダーがビルドに残るようにする
            var aSurface = Mat("Surface", Color.white);
            Configure(aFeedback, iSerialized =>
            {
                iSerialized.FindProperty("mCameraRig").objectReferenceValue = aCameraRig;
                iSerialized.FindProperty("mEffectMaterial").objectReferenceValue = aSurface;
            });
            Configure(aHud, iSerialized =>
            {
                iSerialized.FindProperty("mSlots").objectReferenceValue = aSlots;
                iSerialized.FindProperty("mExecutor").objectReferenceValue = aExecutor;
                iSerialized.FindProperty("mStyle").objectReferenceValue = aStyle;
                iSerialized.FindProperty("mPlayer").objectReferenceValue = aDamageable;
                iSerialized.FindProperty("mLockOn").objectReferenceValue = aLockOn;
                iSerialized.FindProperty("mDirector").objectReferenceValue = aDirector;
                iSerialized.FindProperty("mView").objectReferenceValue = aCamera;
            });
            Configure(aLoadoutEditor, iSerialized =>
            {
                iSerialized.FindProperty("mSlots").objectReferenceValue = aSlots;
                iSerialized.FindProperty("mInput").objectReferenceValue = aInput;
            });
            Configure(aDirector, iSerialized =>
            {
                iSerialized.FindProperty("mPlayer").objectReferenceValue = aPlayer.transform;
                iSerialized.FindProperty("mInput").objectReferenceValue = aInput;
                iSerialized.FindProperty("mStyle").objectReferenceValue = aStyle;
                iSerialized.FindProperty("mEnemyMaterial").objectReferenceValue = aSurface;
                SetArray(iSerialized, "mStages", aStages);
                iSerialized.FindProperty("mArenaRoot").objectReferenceValue = aArenaRoot.gameObject;
                iSerialized.FindProperty("mExploration").objectReferenceValue = aExploration;
                iSerialized.FindProperty("mSlots").objectReferenceValue = aSlots;
            });
            Configure(aStageSelect, iSerialized =>
            {
                iSerialized.FindProperty("mDirector").objectReferenceValue = aDirector;
                iSerialized.FindProperty("mRankConfig").objectReferenceValue = aStyleConfig;
            });

            EditorSceneManager.SaveScene(aScene, ScenePath);

            var aBuildScenes = EditorBuildSettings.scenes.Where(iScene => iScene.path != ScenePath).ToList();
            aBuildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = aBuildScenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[BattleFight] 試作アリーナを生成しました: {ScenePath}");
        }

        static Transform BuildArena()
        {
            var aArena = new GameObject("Arena").transform;
            var aFloorMat = Mat("Floor", new Color(0.32f, 0.34f, 0.38f));
            var aWallMat = Mat("Wall", new Color(0.22f, 0.23f, 0.27f));
            var aPillarMat = Mat("Pillar", new Color(0.4f, 0.38f, 0.36f));
            var aPlatformMat = Mat("Platform", new Color(0.35f, 0.4f, 0.45f));
            var aGrappleMat = Mat("GrapplePoint", new Color(0.75f, 0.5f, 1f));

            Part(PrimitiveType.Cube, "Floor", aArena, new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f), aFloorMat, true);
            Part(PrimitiveType.Cube, "Wall_N", aArena, new Vector3(0f, 2f, 30f), new Vector3(60f, 4f, 1f), aWallMat, true);
            Part(PrimitiveType.Cube, "Wall_S", aArena, new Vector3(0f, 2f, -30f), new Vector3(60f, 4f, 1f), aWallMat, true);
            Part(PrimitiveType.Cube, "Wall_E", aArena, new Vector3(30f, 2f, 0f), new Vector3(1f, 4f, 60f), aWallMat, true);
            Part(PrimitiveType.Cube, "Wall_W", aArena, new Vector3(-30f, 2f, 0f), new Vector3(1f, 4f, 60f), aWallMat, true);

            Part(PrimitiveType.Cube, "Pillar_1", aArena, new Vector3(-12f, 3f, 8f), new Vector3(2f, 6f, 2f), aPillarMat, true);
            Part(PrimitiveType.Cube, "Pillar_2", aArena, new Vector3(12f, 3f, 8f), new Vector3(2f, 6f, 2f), aPillarMat, true);
            Part(PrimitiveType.Cube, "Pillar_3", aArena, new Vector3(-12f, 3f, -8f), new Vector3(2f, 6f, 2f), aPillarMat, true);
            Part(PrimitiveType.Cube, "Pillar_4", aArena, new Vector3(12f, 3f, -8f), new Vector3(2f, 6f, 2f), aPillarMat, true);

            // ハンマージャンプやワイヤーで乗れる高台
            Part(PrimitiveType.Cube, "Platform_W", aArena, new Vector3(-22f, 1.5f, 0f), new Vector3(8f, 3f, 10f), aPlatformMat, true);
            Part(PrimitiveType.Cube, "Platform_E", aArena, new Vector3(22f, 1.5f, 0f), new Vector3(8f, 3f, 10f), aPlatformMat, true);

            // ワイヤーで立体的に移動できるよう、高さと位置をばらして置く
            Vector3[] aGrapplePoints =
            {
                // 高台の上
                new Vector3(-22f, 8f, 0f), new Vector3(22f, 8f, 0f),
                // 南北の中央上空
                new Vector3(0f, 9f, 18f), new Vector3(0f, 9f, -18f),
                // 柱の上
                new Vector3(-12f, 8.5f, 8f), new Vector3(12f, 8.5f, 8f),
                new Vector3(-12f, 8.5f, -8f), new Vector3(12f, 8.5f, -8f),
                // 四隅の高所
                new Vector3(-24f, 11f, 22f), new Vector3(24f, 11f, 22f),
                new Vector3(-24f, 11f, -22f), new Vector3(24f, 11f, -22f),
                // アリーナ中央の上空
                new Vector3(0f, 12f, 0f),
            };
            for (int aGrapplePointIndex = 0; aGrapplePointIndex < aGrapplePoints.Length; aGrapplePointIndex++)
            {
                var aPoint = Part(PrimitiveType.Sphere, $"GrapplePoint_{aGrapplePointIndex + 1}", aArena, aGrapplePoints[aGrapplePointIndex], Vector3.one * 0.8f, aGrappleMat);
                aPoint.gameObject.AddComponent<GrapplePoint>();
            }
            return aArena;
        }

        // ---------- 補助 ----------

        static T Asset<T>(string iRelativePath, bool iReset, Action<T> iInitialize) where T : ScriptableObject
        {
            string aPath = $"{mDataRoot}/{iRelativePath}.asset";
            var aAsset = AssetDatabase.LoadAssetAtPath<T>(aPath);
            if (aAsset == null)
            {
                aAsset = ScriptableObject.CreateInstance<T>();
                iInitialize(aAsset);
                AssetDatabase.CreateAsset(aAsset, aPath);
            }
            else if (iReset)
            {
                iInitialize(aAsset);
                EditorUtility.SetDirty(aAsset);
            }
            return aAsset;
        }

        static Material Mat(string iName, Color iColor)
        {
            string aPath = $"{mRoot}/Materials/M_{iName}.mat";
            var aMaterial = AssetDatabase.LoadAssetAtPath<Material>(aPath);
            if (aMaterial != null) return aMaterial;

            var aShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            aMaterial = new Material(aShader);
            aMaterial.SetColor("_BaseColor", iColor);
            aMaterial.color = iColor;
            AssetDatabase.CreateAsset(aMaterial, aPath);
            return aMaterial;
        }

        static Transform Part(PrimitiveType iShape, string iName, Transform iParent, Vector3 iLocalPosition, Vector3 iLocalScale,
            Material iMaterial, bool iKeepCollider = false)
        {
            var aObject = GameObject.CreatePrimitive(iShape);
            aObject.name = iName;
            if (!iKeepCollider) Object.DestroyImmediate(aObject.GetComponent<Collider>());
            aObject.transform.SetParent(iParent, false);
            aObject.transform.localPosition = iLocalPosition;
            aObject.transform.localScale = iLocalScale;
            aObject.GetComponent<Renderer>().sharedMaterial = iMaterial;
            return aObject.transform;
        }

        static void Configure(Object iTarget, Action<SerializedObject> iConfigure)
        {
            var aSerialized = new SerializedObject(iTarget);
            iConfigure(aSerialized);
            aSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(SerializedObject iSerialized, string iField, params Object[] iValues)
        {
            var aProperty = iSerialized.FindProperty(iField);
            aProperty.arraySize = iValues.Length;
            for (int aValueIndex = 0; aValueIndex < iValues.Length; aValueIndex++) aProperty.GetArrayElementAtIndex(aValueIndex).objectReferenceValue = iValues[aValueIndex];
        }

        static void EnsureEndlessBoss(StageData iEndless, EnemyProfile iBoss)
        {
            if (iEndless == null || iBoss == null) return;
            var aBosses = new System.Collections.Generic.List<EnemyProfile>(iEndless.EndlessBosses ?? new EnemyProfile[0]);
            if (aBosses.Contains(iBoss)) return;
            aBosses.Add(iBoss);
            iEndless.EndlessBosses = aBosses.ToArray();
            EditorUtility.SetDirty(iEndless);
        }

        static EnemySkillSet SkillSet(string iName, WeaponTypeData iWeapon, WeaponType[] iWeaknesses, params SkillData[] iSkills) => new EnemySkillSet
        {
            Name = iName,
            Weapon = iWeapon.Weapon,
            Color = iWeapon.Color,
            Skills = iSkills,
            Weaknesses = iWeaknesses ?? new WeaponType[0],
        };

        /// <summary>まだスキルを持っていない敵にだけ、プレイヤーのスキルの型を入れる(既にある調整を上書きしない)</summary>
        static void EnsureSkillSets(EnemyProfile iProfile, float iSwapInterval, params EnemySkillSet[] iSets)
        {
            if (iProfile == null || (iProfile.SkillSets != null && iProfile.SkillSets.Count > 0)) return;
            iProfile.SkillSets = new System.Collections.Generic.List<EnemySkillSet>(iSets);
            iProfile.SkillSwapInterval = iSwapInterval;
            EditorUtility.SetDirty(iProfile);
        }

        static void EnsureEndlessEntry(StageData iEndless, EnemyProfile iProfile, float iCost, int iMinWave)
        {
            if (iEndless == null || iProfile == null || iEndless.EndlessPool.Exists(iEntry => iEntry.Profile == iProfile)) return;
            iEndless.EndlessPool.Add(new EndlessEntry { Profile = iProfile, Cost = iCost, MinWave = iMinWave });
            EditorUtility.SetDirty(iEndless);
        }

        static void EnsureWeaknesses(EnemyProfile iProfile, params WeaponType[] iWeaknesses)
        {
            if (iProfile == null || (iProfile.Weaknesses != null && iProfile.Weaknesses.Length > 0)) return;
            iProfile.Weaknesses = iWeaknesses;
            EditorUtility.SetDirty(iProfile);
        }

        static void SetPreset(SerializedProperty iPresets, int iIndex, string iName, SkillData iAttackA, SkillData iAttackB, SkillData iMovement)
        {
            if (iPresets.arraySize <= iIndex) iPresets.arraySize = iIndex + 1;
            var aPreset = iPresets.GetArrayElementAtIndex(iIndex);
            aPreset.FindPropertyRelative("Name").stringValue = iName;
            aPreset.FindPropertyRelative("AttackA").objectReferenceValue = iAttackA;
            aPreset.FindPropertyRelative("AttackB").objectReferenceValue = iAttackB;
            aPreset.FindPropertyRelative("Movement").objectReferenceValue = iMovement;
        }

        static void EnsureFolder(string iParent, string iName)
        {
            if (!AssetDatabase.IsValidFolder($"{iParent}/{iName}")) AssetDatabase.CreateFolder(iParent, iName);
        }
    }
}
