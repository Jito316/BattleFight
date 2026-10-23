using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// シーンを手で直したあとに、壊れていないか確かめる。
    /// スクリプトが外れた物・参照が切れた所・なくてはならない物・つないでおかないと動かない参照を探す。
    /// メニュー: BattleFight/Validate Scene(テスト: SceneValidatorTests)
    /// </summary>
    public static class SceneValidator
    {
        /// <summary>つないでおかないと動かない参照(コンポーネントの種類, フィールド名)</summary>
        static readonly (Type Component, string Field)[] mRequiredReferences =
        {
            (typeof(ArenaDirector), "mPlayer"),
            (typeof(ArenaDirector), "mInput"),
            (typeof(ArenaDirector), "mStyle"),
            (typeof(ArenaDirector), "mArenaRoot"),
            (typeof(ArenaDirector), "mExploration"),
            (typeof(ArenaDirector), "mSlots"),
            (typeof(ArenaDirector), "mEnemyMaterial"),
            (typeof(CombatFeedback), "mCameraRig"),
            (typeof(CombatFeedback), "mEffectMaterial"),
            (typeof(CombatFeedback), "mEffects"),
            (typeof(ThirdPersonCamera), "mTarget"),
            (typeof(ThirdPersonCamera), "mInput"),
            (typeof(ThirdPersonCamera), "mLockOn"),
            (typeof(LockOnSystem), "mViewTransform"),
            (typeof(SkillExecutor), "mView"),
            (typeof(PlayerController), "mCameraTransform"),
            (typeof(BattleHud), "mSlots"),
            (typeof(BattleHud), "mExecutor"),
            (typeof(BattleHud), "mStyle"),
            (typeof(BattleHud), "mPlayer"),
            (typeof(BattleHud), "mLockOn"),
            (typeof(BattleHud), "mDirector"),
            (typeof(BattleHud), "mView"),
            (typeof(StageSelectUI), "mDirector"),
            (typeof(LoadoutEditorUI), "mSlots"),
            (typeof(LoadoutEditorUI), "mInput"),
        };

        /// <summary>シーンに 1 つだけなくてはならない物</summary>
        static readonly Type[] mRequiredSingles =
        {
            typeof(ArenaDirector), typeof(CombatFeedback), typeof(ThirdPersonCamera), typeof(SkillExecutor),
            typeof(BattleHud), typeof(StageSelectUI), typeof(LoadoutEditorUI), typeof(SettingsUI), typeof(ExplorationRegion),
        };

        [MenuItem("BattleFight/Validate Scene")]
        static void ValidateFromMenu()
        {
            var aProblems = Validate(SceneManager.GetActiveScene());
            foreach (var aProblem in aProblems) Debug.LogError($"[BattleFight] シーンの問題: {aProblem}");
            EditorUtility.DisplayDialog("BattleFight", aProblems.Count == 0 ? "問題は見つかりませんでした" : $"{aProblems.Count} 件の問題があります(Console を見てください)", "OK");
        }

        /// <summary>見つけた問題(なければ空)</summary>
        public static List<string> Validate(Scene iScene)
        {
            var aProblems = new List<string>();
            var aComponents = new List<Component>();
            foreach (var aRoot in iScene.GetRootGameObjects())
            {
                foreach (var aTransform in aRoot.GetComponentsInChildren<Transform>(true))
                {
                    int aMissing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(aTransform.gameObject);
                    if (aMissing > 0) aProblems.Add($"{PathOf(aTransform)} にスクリプトが外れたコンポーネントが {aMissing} 個ある");
                }
                aComponents.AddRange(aRoot.GetComponentsInChildren<Component>(true));
            }

            foreach (var aComponent in aComponents)
            {
                if (aComponent == null) continue;
                FindBrokenReferences(aComponent, aProblems);
            }

            foreach (var aType in mRequiredSingles)
            {
                int aCount = aComponents.FindAll(iComponent => iComponent != null && aType.IsInstanceOfType(iComponent)).Count;
                if (aCount != 1) aProblems.Add($"{aType.Name} がシーンに {aCount} 個ある(1 個にする)");
            }

            foreach (var (aType, aField) in mRequiredReferences)
            {
                foreach (var aComponent in aComponents)
                {
                    if (aComponent == null || !aType.IsInstanceOfType(aComponent)) continue;
                    var aProperty = new SerializedObject(aComponent).FindProperty(aField);
                    if (aProperty == null) aProblems.Add($"{aType.Name}.{aField} が見つからない(名前が変わった?)");
                    else if (aProperty.objectReferenceValue == null) aProblems.Add($"{PathOf(aComponent.transform)} の {aType.Name}.{aField} がつながっていない");
                }
            }
            return aProblems;
        }

        /// <summary>参照先が消えて切れた所(Inspector で「Missing」と出る所)</summary>
        static void FindBrokenReferences(Component iComponent, List<string> ioProblems)
        {
            var aSerialized = new SerializedObject(iComponent);
            var aProperty = aSerialized.GetIterator();
            while (aProperty.NextVisible(true))
            {
                if (aProperty.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (aProperty.objectReferenceValue == null && aProperty.objectReferenceInstanceIDValue != 0)
                {
                    ioProblems.Add($"{PathOf(iComponent.transform)} の {iComponent.GetType().Name}.{aProperty.propertyPath} の参照先が消えている");
                }
            }
        }

        static string PathOf(Transform iTransform)
        {
            string aPath = iTransform.name;
            for (var aParent = iTransform.parent; aParent != null; aParent = aParent.parent) aPath = aParent.name + "/" + aPath;
            return aPath;
        }

        /// <summary>シーンのファイルを足して開いて確かめ、閉じる(テスト用。今開いているシーンはそのまま)</summary>
        public static List<string> ValidateScenePath(string iPath)
        {
            var aScene = EditorSceneManager.OpenScene(iPath, OpenSceneMode.Additive);
            try
            {
                return Validate(aScene);
            }
            finally
            {
                EditorSceneManager.CloseScene(aScene, true);
            }
        }
    }
}
