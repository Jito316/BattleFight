using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleFight
{
    /// <summary>
    /// 仮モデル(箱・円柱・カプセル)の見た目を組み立てる。
    /// エディタではプレハブの最初の中身を作るのに使い、ゲーム中はプレハブがないときの代わりに使う。
    /// iMaterial は (部品の名前, 色) からマテリアルを返す(エディタではマテリアルのアセット、ゲーム中はその場で作る)。
    /// </summary>
    public static class PrimitiveVisuals
    {
        public const string VisualName = "Visual";
        public const string BodyName = "Body";
        public const string HandAnchorName = "HandAnchor";
        public const string HeadAnchorName = "HeadAnchor";
        public const string WeaponName = "Weapon";
        public const string TipName = "Tip";
        public const string CrownName = "Crown";
        /// <summary>頭の上の目印の高さ(体の高さ 2m のとき)</summary>
        const float mHeadHeight = 2.25f;
        /// <summary>武器の軌跡が消えるまでの時間</summary>
        const float mTrailTime = 0.12f;

        /// <summary>ゲーム中に使う、色ごとのマテリアルの作り方(元のマテリアルを写して色を付ける)</summary>
        public static Func<string, Color, Material> RuntimeMaterials(Material iSurface) => (iPart, iColor) =>
        {
            var aMaterial = iSurface != null ? new Material(iSurface) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            aMaterial.color = iColor;
            return aMaterial;
        };

        /// <summary>プレイヤーの仮モデル: 白いカプセルに青いバイザー。手と頭の上の目印を持つ</summary>
        public static CharacterVisual BuildPlayer(Func<string, Color, Material> iMaterial)
        {
            var aVisual = new GameObject(VisualName).transform;
            var aBody = Part(PrimitiveType.Capsule, BodyName, aVisual, iMaterial(BodyName, new Color(0.9f, 0.9f, 0.95f)));
            aBody.localPosition = new Vector3(0f, 1f, 0f);
            aBody.localScale = new Vector3(0.8f, 1f, 0.8f);
            var aVisor = Part(PrimitiveType.Cube, "Visor", aBody, iMaterial("Visor", new Color(0.1f, 0.6f, 1f)));
            aVisor.localPosition = new Vector3(0f, 0.5f, 0.4f);
            aVisor.localScale = new Vector3(0.7f, 0.15f, 0.3f);
            var aHand = Anchor(HandAnchorName, aVisual, new Vector3(0.45f, 1.15f, 0.25f));
            var aHead = Anchor(HeadAnchorName, aVisual, new Vector3(0f, mHeadHeight, 0f));

            var aCharacter = aVisual.gameObject.AddComponent<CharacterVisual>();
            aCharacter.Setup(aBody, aHand, aHead, null);
            return aCharacter;
        }

        /// <summary>敵の仮モデル: 体の形と色は EnemyProfile から。飛ぶ敵は羽、アーマー持ちは胸当て、スキルを使う敵は武器を持つ</summary>
        public static CharacterVisual BuildEnemy(EnemyProfile iProfile, Func<string, Color, Material> iMaterial)
        {
            var aVisual = new GameObject(VisualName).transform;
            var aBody = Part(iProfile.BodyShape, BodyName, aVisual, iMaterial(BodyName, iProfile.Color));
            aBody.localPosition = new Vector3(0f, 1f, 0f);
            if (iProfile.BodyShape != PrimitiveType.Capsule) aBody.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            if (iProfile.Flying)
            {
                var aWings = Part(PrimitiveType.Cube, "Wings", aVisual, iMaterial("Wings", Color.Lerp(iProfile.Color, Color.white, 0.4f)));
                aWings.localPosition = new Vector3(0f, 1.2f, -0.1f);
                aWings.localScale = new Vector3(2.4f, 0.08f, 0.6f);
            }

            // 向きがわかるように顔を付ける
            var aVisor = Part(PrimitiveType.Cube, "Visor", aVisual, iMaterial("Visor", new Color(0.1f, 0.1f, 0.1f)));
            aVisor.localPosition = new Vector3(0f, 1.5f, 0.4f);
            aVisor.localScale = new Vector3(0.6f, 0.18f, 0.25f);

            if (iProfile.MaxArmor > 0f)
            {
                var aPlate = Part(PrimitiveType.Cube, "Plate", aVisual, iMaterial("Plate", new Color(0.35f, 0.35f, 0.4f)));
                aPlate.localPosition = new Vector3(0f, 1.05f, 0.35f);
                aPlate.localScale = new Vector3(0.9f, 0.7f, 0.25f);
            }

            // プレイヤーのスキルを使う敵は武器を持つ(色は型ごとに EnemyController が塗る)
            Renderer aWeaponRenderer = null;
            if (iProfile.SkillSets != null && iProfile.SkillSets.Count > 0)
            {
                var aWeapon = Part(PrimitiveType.Cube, WeaponName, aVisual, iMaterial(WeaponName, iProfile.SkillSets[0].Color));
                aWeapon.localPosition = new Vector3(0.62f, 1.05f, 0.45f);
                aWeapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
                aWeapon.localScale = new Vector3(0.14f, 0.14f, 1.3f);
                aWeaponRenderer = aWeapon.GetComponent<Renderer>();
            }
            var aHead = Anchor(HeadAnchorName, aVisual, new Vector3(0f, mHeadHeight, 0f));

            var aCharacter = aVisual.gameObject.AddComponent<CharacterVisual>();
            aCharacter.Setup(aBody, null, aHead, aWeaponRenderer);
            return aCharacter;
        }

        /// <summary>武器の仮モデル: 柄と頭(形・大きさは WeaponTypeData から)。先に軌跡を付ける</summary>
        public static WeaponVisual BuildWeapon(WeaponTypeData iData, Func<string, Color, Material> iMaterial)
        {
            var aRoot = new GameObject(iData.DisplayName).transform;
            if (iData.HandleLength > 0f)
            {
                var aHandle = Part(PrimitiveType.Cube, "Handle", aRoot, iMaterial("Handle", new Color(0.25f, 0.2f, 0.15f)));
                aHandle.localScale = new Vector3(0.05f, 0.05f, iData.HandleLength);
                aHandle.localPosition = new Vector3(0f, 0f, iData.HandleLength * 0.5f);
            }

            var aHead = Part(iData.HeadShape, "Head", aRoot, iMaterial("Head", iData.Color));
            aHead.localPosition = iData.HeadOffset;
            aHead.localRotation = Quaternion.Euler(iData.HeadRotation);
            aHead.localScale = iData.HeadScale;

            var aTip = Anchor(TipName, aRoot, iData.HeadOffset);
            float aWidth = Mathf.Clamp(Mathf.Max(iData.HeadScale.x, iData.HeadScale.y, iData.HeadScale.z) * 0.5f, 0.12f, 0.45f);
            var aTrail = AddTrail(aTip, aWidth, iMaterial("Trail", Color.Lerp(iData.Color, Color.white, 0.5f)));

            var aWeapon = aRoot.gameObject.AddComponent<WeaponVisual>();
            aWeapon.Setup(aTip, aTrail);
            return aWeapon;
        }

        /// <summary>振ったときの軌跡を iTip に付ける(描くのは攻撃判定が出ている間だけ)</summary>
        public static TrailRenderer AddTrail(Transform iTip, float iWidth, Material iMaterial)
        {
            var aTrail = iTip.gameObject.AddComponent<TrailRenderer>();
            aTrail.time = mTrailTime;
            aTrail.minVertexDistance = 0.05f;
            aTrail.startWidth = iWidth;
            aTrail.endWidth = 0f;
            aTrail.emitting = false;
            aTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aTrail.receiveShadows = false;
            if (iMaterial != null) aTrail.sharedMaterial = iMaterial;
            return aTrail;
        }

        /// <summary>エリートの金の冠を、頭の上の目印に付ける</summary>
        public static Transform AddCrown(CharacterVisual iVisual, Material iMaterial)
        {
            var aParent = iVisual.HeadAnchor != null ? iVisual.HeadAnchor : iVisual.transform;
            var aCrown = Part(PrimitiveType.Cube, CrownName, aParent, iMaterial);
            aCrown.localPosition = iVisual.HeadAnchor != null ? Vector3.zero : new Vector3(0f, mHeadHeight, 0f);
            aCrown.localRotation = Quaternion.Euler(0f, 45f, 0f);
            aCrown.localScale = new Vector3(0.45f, 0.22f, 0.45f);
            return aCrown;
        }

        static Transform Part(PrimitiveType iShape, string iName, Transform iParent, Material iMaterial)
        {
            var aPart = GameObject.CreatePrimitive(iShape);
            aPart.name = iName;
            // 当たり判定はすぐに消す(見た目だけの部品が、弾や移動の邪魔にならないように)
            Object.DestroyImmediate(aPart.GetComponent<Collider>());
            aPart.transform.SetParent(iParent, false);
            if (iMaterial != null) aPart.GetComponent<Renderer>().sharedMaterial = iMaterial;
            return aPart.transform;
        }

        static Transform Anchor(string iName, Transform iParent, Vector3 iLocalPosition)
        {
            var aAnchor = new GameObject(iName).transform;
            aAnchor.SetParent(iParent, false);
            aAnchor.localPosition = iLocalPosition;
            return aAnchor;
        }
    }
}
