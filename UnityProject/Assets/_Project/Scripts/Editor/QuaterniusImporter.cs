using UnityEditor;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// Quaternius のモデル(ThirdParty/Quaternius)の取り込み設定。
    /// キャラクターとアニメーションは Humanoid にして、別のモデルのモーションを流せるようにする。
    /// マテリアルは取り込まず、シーンを作るときに URP のマテリアルを割り当てる。
    /// </summary>
    public class QuaterniusImporter : AssetPostprocessor
    {
        public const string Root = "Assets/_Project/ThirdParty/Quaternius";
        public const string CharactersRoot = Root + "/Characters";
        public const string AnimationsRoot = Root + "/Animations";
        public const string HairRoot = Root + "/Hair";

        // 設定を変えたら上げる(取り込み直される)
        public override uint GetVersion() => 2;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var aImporter = (ModelImporter)assetImporter;
            aImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            aImporter.importCameras = false;
            aImporter.importLights = false;

            if (assetPath.StartsWith(HairRoot))
            {
                aImporter.animationType = ModelImporterAnimationType.None;
                aImporter.importAnimation = false;
                return;
            }

            aImporter.animationType = ModelImporterAnimationType.Human;
            aImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            aImporter.importAnimation = assetPath.StartsWith(AnimationsRoot);
            // アニメーション用のファイルに入っているマネキンの見た目は使わない
            if (assetPath.StartsWith(AnimationsRoot)) aImporter.importBlendShapes = false;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(AnimationsRoot)) return;
            var aImporter = (ModelImporter)assetImporter;
            var aClips = aImporter.defaultClipAnimations;
            foreach (var aClip in aClips)
            {
                // Blender から書き出したテイクは "Armature|Idle_Loop" のような名前なので、"|" より後ろだけにする
                int aBar = aClip.name.LastIndexOf('|');
                if (aBar >= 0) aClip.name = aClip.name.Substring(aBar + 1);
                bool aLoop = aClip.name.EndsWith("_Loop") || aClip.name == "Sword_Idle";
                aClip.loopTime = aLoop;
                // 移動はコードで行うので、モーションで体の位置・向きが動かないようにする
                aClip.lockRootRotation = true;
                aClip.lockRootHeightY = true;
                aClip.lockRootPositionXZ = true;
                aClip.keepOriginalOrientation = true;
                aClip.keepOriginalPositionY = true;
                aClip.keepOriginalPositionXZ = true;
            }
            aImporter.clipAnimations = aClips;
        }
    }
}
