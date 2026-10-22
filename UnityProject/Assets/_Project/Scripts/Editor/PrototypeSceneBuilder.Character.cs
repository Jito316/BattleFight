using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>プレイヤーの人型モデル(Quaternius Universal Base Characters)と、そのアニメーター</summary>
    public static partial class PrototypeSceneBuilder
    {
        const string mPlayerModelPath = QuaterniusImporter.CharactersRoot + "/Superhero_Male_FullBody.fbx";
        const string mPlayerHairPath = QuaterniusImporter.HairRoot + "/Hair_SimpleParted.fbx";
        const string mPlayerControllerPath = mRoot + "/Animation/AC_Player.controller";
        static readonly string[] mAnimationPaths =
        {
            QuaterniusImporter.AnimationsRoot + "/UAL1_Standard.fbx",
            QuaterniusImporter.AnimationsRoot + "/UAL2_Standard.fbx",
        };

        /// <summary>モデルの背の高さ(CharacterController の高さ 2 に収める)</summary>
        const float mPlayerModelHeight = 1.85f;

        /// <summary>プレイヤーの見た目を作る。モデルが見つからないときは null(呼び出し側で仮の形にする)</summary>
        static CharacterAnimator BuildPlayerModel(GameObject iPlayer)
        {
            var aPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mPlayerModelPath);
            if (aPrefab == null)
            {
                Debug.LogWarning($"キャラクターモデルが見つかりません: {mPlayerModelPath}");
                return null;
            }

            var aModel = (GameObject)PrefabUtility.InstantiatePrefab(aPrefab);
            aModel.name = "Model";
            aModel.transform.SetParent(iPlayer.transform, false);
            aModel.transform.localPosition = Vector3.zero;
            aModel.transform.localRotation = Quaternion.identity;

            // 書き出し元によっては後ろを向いているので、肩の並びから正面を求めて前(+Z)に向ける
            var aLeft = FindDeep(aModel.transform, "upperarm_l");
            var aRight = FindDeep(aModel.transform, "upperarm_r");
            if (aLeft != null && aRight != null && Vector3.Cross(aRight.position - aLeft.position, Vector3.up).z < 0f)
            {
                aModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // 背の高さをそろえる
            var aBounds = RendererBounds(aModel);
            if (aBounds.size.y > 0.01f) aModel.transform.localScale = Vector3.one * (mPlayerModelHeight / aBounds.size.y);

            var aSuit = Mat("Hero_Suit", new Color(0.88f, 0.9f, 0.95f));
            var aAccent = Mat("Hero_Eyes", new Color(0.1f, 0.6f, 1f));
            var aHair = Mat("Hero_Hair", new Color(0.12f, 0.14f, 0.2f));
            foreach (var aRenderer in aModel.GetComponentsInChildren<Renderer>())
            {
                string aName = aRenderer.name.ToLowerInvariant();
                var aMaterial = aName.Contains("eyebrow") ? aHair : aName.Contains("eye") ? aAccent : aSuit;
                var aSlots = new Material[aRenderer.sharedMaterials.Length];
                for (int aI = 0; aI < aSlots.Length; aI++) aSlots[aI] = aMaterial;
                aRenderer.sharedMaterials = aSlots;
                if (aRenderer is SkinnedMeshRenderer aSkinned) aSkinned.updateWhenOffscreen = true;
            }

            var aAnimator = aModel.GetComponent<Animator>() ?? aModel.AddComponent<Animator>();
            aAnimator.runtimeAnimatorController = BuildPlayerController();
            aAnimator.applyRootMotion = false;
            aAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            AttachHair(aModel, aAnimator, aHair);

            return iPlayer.AddComponent<CharacterAnimator>();
        }

        /// <summary>髪(原点 0 で書き出されたもの)を、取り込んだままの姿勢で頭の骨に付ける</summary>
        static void AttachHair(GameObject iModel, Animator iAnimator, Material iMaterial)
        {
            var aHairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mPlayerHairPath);
            var aHead = iAnimator.avatar != null && iAnimator.avatar.isHuman ? iAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (aHairPrefab == null || aHead == null) return;

            var aHair = (GameObject)PrefabUtility.InstantiatePrefab(aHairPrefab);
            aHair.name = "Hair";
            aHair.transform.SetParent(iModel.transform, false);
            aHair.transform.localPosition = Vector3.zero;
            aHair.transform.localRotation = Quaternion.identity;
            aHair.transform.localScale = Vector3.one;
            aHair.transform.SetParent(aHead, true);
            foreach (var aRenderer in aHair.GetComponentsInChildren<Renderer>()) aRenderer.sharedMaterial = iMaterial;
        }

        static Transform FindDeep(Transform iRoot, string iName)
        {
            foreach (var aChild in iRoot.GetComponentsInChildren<Transform>(true))
            {
                if (aChild.name == iName) return aChild;
            }
            return null;
        }

        static Bounds RendererBounds(GameObject iRoot)
        {
            var aRenderers = iRoot.GetComponentsInChildren<Renderer>();
            if (aRenderers.Length == 0) return new Bounds(iRoot.transform.position, Vector3.zero);
            var aBounds = aRenderers[0].bounds;
            foreach (var aRenderer in aRenderers) aBounds.Encapsulate(aRenderer.bounds);
            return aBounds;
        }

        /// <summary>
        /// プレイヤーのアニメーターを作り直す。移動はブレンドツリー(Speed)、技・被弾・倒れるは
        /// CharacterAnimator が状態名を指定して切り替える(遷移の線は引かない)。
        /// </summary>
        static AnimatorController BuildPlayerController()
        {
            var aClips = new Dictionary<string, AnimationClip>();
            foreach (var aPath in mAnimationPaths)
            {
                foreach (var aAsset in AssetDatabase.LoadAllAssetsAtPath(aPath))
                {
                    if (aAsset is AnimationClip aClip && !aClip.name.StartsWith("__preview__")) aClips[aClip.name] = aClip;
                }
            }

            EnsureFolder(mRoot, "Animation");
            AssetDatabase.DeleteAsset(mPlayerControllerPath);
            var aController = AnimatorController.CreateAnimatorControllerAtPath(mPlayerControllerPath);
            aController.AddParameter(CharacterAnimator.SpeedParameter, AnimatorControllerParameterType.Float);
            aController.AddParameter(new AnimatorControllerParameter
            {
                name = CharacterAnimator.MotionSpeedParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f,
            });
            var aMachine = aController.layers[0].stateMachine;

            var aTree = new BlendTree
            {
                name = CharacterAnimator.LocomotionState,
                blendType = BlendTreeType.Simple1D,
                blendParameter = CharacterAnimator.SpeedParameter,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(aTree, aController);
            AddMotion(aTree, aClips, "Idle_Loop", 0f);
            AddMotion(aTree, aClips, "Walk_Loop", 1.8f);
            AddMotion(aTree, aClips, "Jog_Fwd_Loop", 4.5f);
            AddMotion(aTree, aClips, "Sprint_Loop", 7.5f);
            var aLocomotion = aMachine.AddState(CharacterAnimator.LocomotionState);
            aLocomotion.motion = aTree;
            aMachine.defaultState = aLocomotion;

            AddState(aMachine, aClips, CharacterAnimator.AirState, "Jump_Loop", false);
            AddState(aMachine, aClips, CharacterAnimator.HitState, CharacterAnimator.HitState, false);
            AddState(aMachine, aClips, CharacterAnimator.DeathState, CharacterAnimator.DeathState, false);
            // 技のモーションは、段の長さに合わせて速さを変える(MotionSpeed)
            var aAdded = new HashSet<string> { CharacterAnimator.AirState, CharacterAnimator.HitState, CharacterAnimator.DeathState };
            foreach (var aName in CharacterAnimator.ActionStates.Concat(SkillMotionNames()))
            {
                if (aAdded.Add(aName)) AddState(aMachine, aClips, aName, aName, true);
            }

            EditorUtility.SetDirty(aController);
            AssetDatabase.SaveAssets();
            return aController;
        }

        static void AddMotion(BlendTree iTree, Dictionary<string, AnimationClip> iClips, string iClip, float iThreshold)
        {
            if (iClips.TryGetValue(iClip, out var aClip)) iTree.AddChild(aClip, iThreshold);
            else Debug.LogWarning($"アニメーションが見つかりません: {iClip}");
        }

        static void AddState(AnimatorStateMachine iMachine, Dictionary<string, AnimationClip> iClips, string iState, string iClip, bool iFitToSkill)
        {
            if (!iClips.TryGetValue(iClip, out var aClip))
            {
                Debug.LogWarning($"アニメーションが見つかりません: {iClip}");
                return;
            }
            var aState = iMachine.AddState(iState);
            aState.motion = aClip;
            if (iFitToSkill)
            {
                aState.speedParameter = CharacterAnimator.MotionSpeedParameter;
                aState.speedParameterActive = true;
            }
        }
    }
}
