using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// PrototypeSceneBuilder: プレハブ(プレイヤー・敵・武器)。
    /// プレハブがなければ仮モデルから作ってデータにつなぐ。あれば中身には手を付けない(アーティストが差し替えた見た目を消さない)。
    /// 作り直したいときは、そのプレハブを消してからシーンを作り直す。
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const string mPrefabRoot = mRoot + "/Prefabs";
        public const string PlayerPrefabPath = mPrefabRoot + "/Player/Player.prefab";
        public const string EnemyBasePrefabPath = mPrefabRoot + "/Enemies/Enemy_Base.prefab";
        // シーンに置く物(シーンを分けて作業できるよう、それぞれプレハブにする)
        public const string ArenaPrefabPath = mPrefabRoot + "/Scene/Arena.prefab";
        public const string ExplorationPrefabPath = mPrefabRoot + "/Scene/Exploration.prefab";
        public const string CameraPrefabPath = mPrefabRoot + "/Scene/MainCamera.prefab";
        public const string DirectorPrefabPath = mPrefabRoot + "/Scene/GameDirector.prefab";
        public const string UiPrefabPath = mPrefabRoot + "/Scene/UI.prefab";

        static void EnsurePrefabFolders()
        {
            EnsureFolder(mRoot, "Prefabs");
            EnsureFolder(mPrefabRoot, "Player");
            EnsureFolder(mPrefabRoot, "Enemies");
            EnsureFolder(mPrefabRoot, "Weapons");
            EnsureFolder(mPrefabRoot, "Scene");
        }

        /// <summary>プレハブの部品のマテリアル(プレハブに保存できるよう、アセットとして作る)</summary>
        static Func<string, Color, Material> PrefabMaterials(string iOwner) => (iPart, iColor) => Mat($"{iOwner}_{iPart}", iColor);

        /// <summary>武器のプレハブを用意し、武器のデータにつなぐ</summary>
        static void EnsureWeaponPrefab(WeaponTypeData iData)
        {
            string aPath = $"{mPrefabRoot}/Weapons/{iData.name}.prefab";
            var aPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(aPath);
            if (aPrefab == null)
            {
                var aWeapon = PrimitiveVisuals.BuildWeapon(iData, PrefabMaterials(iData.name));
                aPrefab = PrefabUtility.SaveAsPrefabAsset(aWeapon.gameObject, aPath);
                Object.DestroyImmediate(aWeapon.gameObject);
            }
            if (iData.Prefab != null) return;
            iData.Prefab = aPrefab;
            EditorUtility.SetDirty(iData);
        }

        /// <summary>敵の土台のプレハブ: 当たりの大きさと、動かすための部品だけ(見た目は種類ごとのバリアントで持つ)</summary>
        static GameObject EnsureEnemyBasePrefab()
        {
            var aPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePrefabPath);
            if (aPrefab != null) return aPrefab;
            var aRoot = new GameObject("Enemy_Base");
            EnemyFactory.ConfigureBody(aRoot.AddComponent<CharacterController>());
            aRoot.AddComponent<Damageable>();
            aRoot.AddComponent<EnemyController>();
            aRoot.AddComponent<CharacterAnimation>();
            aPrefab = PrefabUtility.SaveAsPrefabAsset(aRoot, EnemyBasePrefabPath);
            Object.DestroyImmediate(aRoot);
            return aPrefab;
        }

        /// <summary>敵の種類ごとのプレハブ(土台のバリアント + 仮モデルの見た目)を用意し、敵のデータにつなぐ</summary>
        static void EnsureEnemyPrefab(EnemyProfile iProfile, GameObject iBase)
        {
            string aPath = $"{mPrefabRoot}/Enemies/{iProfile.name}.prefab";
            var aPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(aPath);
            if (aPrefab == null)
            {
                var aInstance = (GameObject)PrefabUtility.InstantiatePrefab(iBase);
                aInstance.name = iProfile.name;
                var aVisual = PrimitiveVisuals.BuildEnemy(iProfile, PrefabMaterials(iProfile.name));
                aVisual.transform.SetParent(aInstance.transform, false);
                // 土台のプレハブのインスタンスから保存すると、土台のバリアントになる
                aPrefab = PrefabUtility.SaveAsPrefabAsset(aInstance, aPath);
                Object.DestroyImmediate(aInstance);
            }
            if (iProfile.Prefab != null) return;
            iProfile.Prefab = aPrefab;
            EditorUtility.SetDirty(iProfile);
        }

        static void EnsureEnemyPrefabs(IEnumerable<EnemyProfile> iProfiles)
        {
            var aBase = EnsureEnemyBasePrefab();
            foreach (var aProfile in iProfiles)
            {
                if (aProfile != null) EnsureEnemyPrefab(aProfile, aBase);
            }
        }

        /// <summary>
        /// 演出の差し替え表。すべての場面(EffectCue)の行を用意する(プレハブは空のまま。入っている行には手を付けない)
        /// </summary>
        static EffectLibrary EnsureEffectLibrary()
        {
            var aLibrary = Asset<EffectLibrary>("EffectLibrary", false, _ => { });
            bool aAdded = false;
            foreach (EffectCue aCue in Enum.GetValues(typeof(EffectCue))) aAdded |= aLibrary.EnsureEntry(aCue);
            if (aAdded) EditorUtility.SetDirty(aLibrary);
            return aLibrary;
        }

        /// <summary>音の差し替え表。すべての音の種類(Sfx)の行を用意する(ファイルは空のまま。入っている行には手を付けない)</summary>
        static SoundLibrary EnsureSoundLibrary()
        {
            var aLibrary = Asset<SoundLibrary>("SoundLibrary", false, _ => { });
            bool aAdded = false;
            foreach (Sfx aSound in Enum.GetValues(typeof(Sfx))) aAdded |= aLibrary.EnsureEntry(aSound);
            if (aAdded) EditorUtility.SetDirty(aLibrary);
            return aLibrary;
        }

        /// <summary>
        /// 今あるプレハブの、空の参照だけを埋める(あとから足した参照のため。入っている参照には手を付けない)。
        /// iSetup は (プレハブの中身, 埋めたか) を受け取り、埋めたら true を返す
        /// </summary>
        static void FillEmptyReferences(string iPath, Func<GameObject, bool> iSetup)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(iPath) == null) return;
            var aContents = PrefabUtility.LoadPrefabContents(iPath);
            try
            {
                if (iSetup(aContents)) PrefabUtility.SaveAsPrefabAsset(aContents, iPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(aContents);
            }
        }

        /// <summary>iComponent の iField が空なら iValue を入れる。入れたら true</summary>
        static bool FillIfEmpty(Component iComponent, string iField, Object iValue)
        {
            if (iComponent == null || iValue == null) return false;
            var aSerialized = new SerializedObject(iComponent);
            var aProperty = aSerialized.FindProperty(iField);
            if (aProperty == null || aProperty.objectReferenceValue != null) return false;
            aProperty.objectReferenceValue = iValue;
            aSerialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        /// <summary>
        /// シーンにプレハブを置く。プレハブがあればそのインスタンス、なければ iBuild で組み立ててプレハブにする。
        /// シーンの物(カメラなど)への参照は、置いたあとにインスタンスへ入れる(プレハブには入れられない)
        /// </summary>
        static GameObject PlacePrefab(string iPath, Func<GameObject> iBuild)
        {
            var aPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(iPath);
            if (aPrefab != null) return (GameObject)PrefabUtility.InstantiatePrefab(aPrefab);
            var aObject = iBuild();
            PrefabUtility.SaveAsPrefabAssetAndConnect(aObject, iPath, InteractionMode.AutomatedAction);
            return aObject;
        }
    }
}
