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
    public static class PrototypeSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string DataRoot = Root + "/Data";
        public const string ScenePath = Root + "/Scenes/Prototype_Arena.unity";
        const string HudFontPath = Root + "/Fonts/NotoSansJP-Bold.otf";

        [MenuItem("BattleFight/Build Prototype Arena")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(resetData: false);
        }

        [MenuItem("BattleFight/Reset Data To Defaults")]
        public static void ResetDataFromMenu()
        {
            if (!EditorUtility.DisplayDialog("BattleFight", "スキル・武器・敵のアセットをたたき台の数値で上書きします。よろしいですか?", "上書き", "キャンセル"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(resetData: true);
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                Build(resetData: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void Build(bool resetData)
        {
            EnsureFolder(Root, "Data");
            EnsureFolder(DataRoot, "Skills");
            EnsureFolder(DataRoot, "Weapons");
            EnsureFolder(DataRoot, "Enemies");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Scenes");

            // NewScene は未使用アセットを解放するので、アセットを読み込む前に作る
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- データ ----------
            var swordCombo = Asset<SkillData>("Skills/Skill_Sword_Combo", resetData, PrototypeDefaults.SwordCombo);
            var swordIai = Asset<SkillData>("Skills/Skill_Sword_Iai", resetData, PrototypeDefaults.SwordIai);
            var swordStep = Asset<SkillData>("Skills/Skill_Sword_Step", resetData, PrototypeDefaults.SwordStep);
            var swordFinisher = Asset<SkillData>("Skills/Skill_Sword_Finisher", resetData, PrototypeDefaults.SwordFinisher);

            var hammerCombo = Asset<SkillData>("Skills/Skill_Hammer_Combo", resetData, PrototypeDefaults.HammerCombo);
            var hammerQuake = Asset<SkillData>("Skills/Skill_Hammer_Quake", resetData, PrototypeDefaults.HammerQuake);
            var hammerJump = Asset<SkillData>("Skills/Skill_Hammer_Jump", resetData, PrototypeDefaults.HammerJump);
            var hammerFinisher = Asset<SkillData>("Skills/Skill_Hammer_Finisher", resetData, PrototypeDefaults.HammerFinisher);

            var chainCombo = Asset<SkillData>("Skills/Skill_Chain_Combo", resetData, PrototypeDefaults.ChainCombo);
            var chainPull = Asset<SkillData>("Skills/Skill_Chain_Pull", resetData, PrototypeDefaults.ChainPull);
            var chainWire = Asset<SkillData>("Skills/Skill_Chain_Wire", resetData, PrototypeDefaults.ChainWire);
            var chainFinisher = Asset<SkillData>("Skills/Skill_Chain_Finisher", resetData, PrototypeDefaults.ChainFinisher);

            var sword = Asset<WeaponTypeData>("Weapons/Weapon_Sword", resetData, w => PrototypeDefaults.Sword(w, swordFinisher));
            var hammer = Asset<WeaponTypeData>("Weapons/Weapon_Hammer", resetData, w => PrototypeDefaults.Hammer(w, hammerFinisher));
            var chain = Asset<WeaponTypeData>("Weapons/Weapon_Chain", resetData, w => PrototypeDefaults.Chain(w, chainFinisher));
            var weapons = new Object[] { sword, hammer, chain };

            var styleConfig = Asset<StyleRankConfig>("StyleRankConfig", resetData, _ => { });

            var grunt = Asset<EnemyProfile>("Enemies/Enemy_Grunt", resetData, PrototypeDefaults.Grunt);
            var armored = Asset<EnemyProfile>("Enemies/Enemy_Armored", resetData, PrototypeDefaults.Armored);
            var boss = Asset<EnemyProfile>("Enemies/Enemy_Boss", resetData, PrototypeDefaults.Boss);

            AssetDatabase.SaveAssets();

            // ---------- シーン ----------
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.22f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            BuildArena();

            // プレイヤー
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 0.1f, -10f);
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.05f;

            var body = Part(PrimitiveType.Capsule, "Body", player.transform, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f),
                Mat("Player", new Color(0.9f, 0.9f, 0.95f)));
            Part(PrimitiveType.Cube, "Visor", body, new Vector3(0f, 0.5f, 0.4f), new Vector3(0.7f, 0.15f, 0.3f),
                Mat("Visor", new Color(0.1f, 0.6f, 1f)));
            var hand = new GameObject("HandAnchor").transform;
            hand.SetParent(player.transform, false);
            hand.localPosition = new Vector3(0.45f, 1.15f, 0.25f);

            var damageable = player.AddComponent<Damageable>();
            var input = player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var slots = player.AddComponent<SkillSlotController>();
            var weaponHolder = player.AddComponent<WeaponHolder>();
            var lockOn = player.AddComponent<LockOnSystem>();
            var style = player.AddComponent<StyleRankSystem>();
            var executor = player.AddComponent<SkillExecutor>();
            var playerController = player.AddComponent<PlayerController>();

            // カメラ
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            cameraObject.AddComponent<AudioListener>();
            var cameraRig = cameraObject.AddComponent<ThirdPersonCamera>();
            cameraObject.transform.position = new Vector3(0f, 4f, -17f);

            // ゲーム進行
            var directorObject = new GameObject("GameDirector");
            var director = directorObject.AddComponent<ArenaDirector>();
            var feedback = directorObject.AddComponent<CombatFeedback>();
            var hud = directorObject.AddComponent<BattleHud>();

            // ---------- 参照の設定 ----------
            Configure(damageable, so =>
            {
                so.FindProperty("team").enumValueIndex = (int)Team.Player;
                so.FindProperty("maxHealth").floatValue = 200f;
            });
            Configure(slots, so =>
            {
                SetArray(so, "attackARack", swordCombo, hammerCombo, chainCombo);
                SetArray(so, "attackBRack", swordIai, hammerQuake, chainPull);
                SetArray(so, "movementRack", swordStep, hammerJump, chainWire);
                SetArray(so, "weaponDatabase", weapons);
            });
            Configure(weaponHolder, so =>
            {
                so.FindProperty("handAnchor").objectReferenceValue = hand;
                so.FindProperty("slots").objectReferenceValue = slots;
                so.FindProperty("surfaceMaterial").objectReferenceValue = Mat("Surface", Color.white);
                SetArray(so, "weaponDatabase", weapons);
            });
            Configure(lockOn, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("viewTransform").objectReferenceValue = cameraObject.transform;
            });
            Configure(style, so => so.FindProperty("config").objectReferenceValue = styleConfig);
            Configure(executor, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("motor").objectReferenceValue = motor;
                so.FindProperty("slots").objectReferenceValue = slots;
                so.FindProperty("weapons").objectReferenceValue = weaponHolder;
                so.FindProperty("lockOn").objectReferenceValue = lockOn;
                so.FindProperty("style").objectReferenceValue = style;
                so.FindProperty("self").objectReferenceValue = damageable;
            });
            Configure(playerController, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("motor").objectReferenceValue = motor;
                so.FindProperty("executor").objectReferenceValue = executor;
                so.FindProperty("damageable").objectReferenceValue = damageable;
                so.FindProperty("style").objectReferenceValue = style;
                so.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            });
            Configure(cameraRig, so =>
            {
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("lockOn").objectReferenceValue = lockOn;
            });
            // 実行時に生成する仮モデル用。シーンから参照してシェーダーがビルドに残るようにする
            var surface = Mat("Surface", Color.white);
            Configure(feedback, so =>
            {
                so.FindProperty("cameraRig").objectReferenceValue = cameraRig;
                so.FindProperty("effectMaterial").objectReferenceValue = surface;
            });
            Configure(hud, so =>
            {
                so.FindProperty("slots").objectReferenceValue = slots;
                so.FindProperty("executor").objectReferenceValue = executor;
                so.FindProperty("style").objectReferenceValue = style;
                so.FindProperty("player").objectReferenceValue = damageable;
                so.FindProperty("lockOn").objectReferenceValue = lockOn;
                so.FindProperty("director").objectReferenceValue = director;
                so.FindProperty("view").objectReferenceValue = camera;
                so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(HudFontPath);
            });
            Configure(director, so =>
            {
                so.FindProperty("player").objectReferenceValue = player.transform;
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("style").objectReferenceValue = style;
                so.FindProperty("enemyMaterial").objectReferenceValue = surface;
                var waves = so.FindProperty("waves");
                SetWave(waves, 0, "WAVE 1", grunt, grunt, grunt);
                SetWave(waves, 1, "WAVE 2", grunt, grunt, armored, grunt);
                SetWave(waves, 2, "WAVE 3", armored, grunt, armored, grunt);
                SetWave(waves, 3, "BOSS", boss);
            });

            EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[BattleFight] 試作アリーナを生成しました: {ScenePath}");
        }

        static void BuildArena()
        {
            var arena = new GameObject("Arena").transform;
            var floorMat = Mat("Floor", new Color(0.32f, 0.34f, 0.38f));
            var wallMat = Mat("Wall", new Color(0.22f, 0.23f, 0.27f));
            var pillarMat = Mat("Pillar", new Color(0.4f, 0.38f, 0.36f));
            var platformMat = Mat("Platform", new Color(0.35f, 0.4f, 0.45f));
            var grappleMat = Mat("GrapplePoint", new Color(0.75f, 0.5f, 1f));

            Part(PrimitiveType.Cube, "Floor", arena, new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f), floorMat, true);
            Part(PrimitiveType.Cube, "Wall_N", arena, new Vector3(0f, 2f, 30f), new Vector3(60f, 4f, 1f), wallMat, true);
            Part(PrimitiveType.Cube, "Wall_S", arena, new Vector3(0f, 2f, -30f), new Vector3(60f, 4f, 1f), wallMat, true);
            Part(PrimitiveType.Cube, "Wall_E", arena, new Vector3(30f, 2f, 0f), new Vector3(1f, 4f, 60f), wallMat, true);
            Part(PrimitiveType.Cube, "Wall_W", arena, new Vector3(-30f, 2f, 0f), new Vector3(1f, 4f, 60f), wallMat, true);

            Part(PrimitiveType.Cube, "Pillar_1", arena, new Vector3(-12f, 3f, 8f), new Vector3(2f, 6f, 2f), pillarMat, true);
            Part(PrimitiveType.Cube, "Pillar_2", arena, new Vector3(12f, 3f, 8f), new Vector3(2f, 6f, 2f), pillarMat, true);
            Part(PrimitiveType.Cube, "Pillar_3", arena, new Vector3(-12f, 3f, -8f), new Vector3(2f, 6f, 2f), pillarMat, true);
            Part(PrimitiveType.Cube, "Pillar_4", arena, new Vector3(12f, 3f, -8f), new Vector3(2f, 6f, 2f), pillarMat, true);

            // ハンマージャンプやワイヤーで乗れる高台
            Part(PrimitiveType.Cube, "Platform_W", arena, new Vector3(-22f, 1.5f, 0f), new Vector3(8f, 3f, 10f), platformMat, true);
            Part(PrimitiveType.Cube, "Platform_E", arena, new Vector3(22f, 1.5f, 0f), new Vector3(8f, 3f, 10f), platformMat, true);

            Vector3[] grapplePoints =
            {
                new Vector3(-22f, 8f, 0f), new Vector3(22f, 8f, 0f),
                new Vector3(0f, 9f, 18f), new Vector3(0f, 9f, -18f),
            };
            for (int i = 0; i < grapplePoints.Length; i++)
            {
                var point = Part(PrimitiveType.Sphere, $"GrapplePoint_{i + 1}", arena, grapplePoints[i], Vector3.one * 0.8f, grappleMat);
                point.gameObject.AddComponent<GrapplePoint>();
            }
        }

        // ---------- 補助 ----------

        static T Asset<T>(string relativePath, bool reset, Action<T> initialize) where T : ScriptableObject
        {
            string path = $"{DataRoot}/{relativePath}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                initialize(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else if (reset)
            {
                initialize(asset);
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{Root}/Materials/M_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Part(PrimitiveType shape, string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        static void Configure(Object target, Action<SerializedObject> configure)
        {
            var so = new SerializedObject(target);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(SerializedObject so, string field, params Object[] values)
        {
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetWave(SerializedProperty waves, int index, string label, params EnemyProfile[] enemies)
        {
            if (waves.arraySize <= index) waves.arraySize = index + 1;
            var wave = waves.GetArrayElementAtIndex(index);
            wave.FindPropertyRelative("label").stringValue = label;
            var list = wave.FindPropertyRelative("enemies");
            list.arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
