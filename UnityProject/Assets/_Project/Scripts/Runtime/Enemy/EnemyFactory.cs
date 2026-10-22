using UnityEngine;

namespace BattleFight
{
    /// <summary>EnemyProfile から仮モデルの敵を組み立てる。</summary>
    public static class EnemyFactory
    {
        /// <param name="iSurfaceMaterial">色を付ける元のマテリアル。ビルドでシェーダーが削られないよう、シーンから参照させる</param>
        public static EnemyController Create(EnemyProfile iProfile, Vector3 iPosition, Quaternion iRotation, Transform iPlayer,
            Material iSurfaceMaterial)
        {
            var aRoot = new GameObject(iProfile.DisplayName);
            aRoot.transform.SetPositionAndRotation(iPosition, iRotation);
            aRoot.transform.localScale = Vector3.one * iProfile.Scale;

            var aController = aRoot.AddComponent<CharacterController>();
            aController.center = new Vector3(0f, 1f, 0f);
            aController.height = 2f;
            aController.radius = 0.5f;
            aController.stepOffset = 0.3f;

            var aBody = CreatePart(iProfile.BodyShape, aRoot.transform, iSurfaceMaterial, iProfile.Color);
            aBody.localPosition = new Vector3(0f, 1f, 0f);
            if (iProfile.BodyShape != PrimitiveType.Capsule) aBody.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            if (iProfile.Flying)
            {
                // 飛ぶ敵には羽を付ける
                var aWings = CreatePart(PrimitiveType.Cube, aRoot.transform, iSurfaceMaterial, Color.Lerp(iProfile.Color, Color.white, 0.4f));
                aWings.localPosition = new Vector3(0f, 1.2f, -0.1f);
                aWings.localScale = new Vector3(2.4f, 0.08f, 0.6f);
            }

            // 向きがわかるように顔を付ける
            var aVisor = CreatePart(PrimitiveType.Cube, aRoot.transform, iSurfaceMaterial, new Color(0.1f, 0.1f, 0.1f));
            aVisor.localPosition = new Vector3(0f, 1.5f, 0.4f);
            aVisor.localScale = new Vector3(0.6f, 0.18f, 0.25f);

            if (iProfile.MaxArmor > 0f)
            {
                var aPlate = CreatePart(PrimitiveType.Cube, aRoot.transform, iSurfaceMaterial, new Color(0.35f, 0.35f, 0.4f));
                aPlate.localPosition = new Vector3(0f, 1.05f, 0.35f);
                aPlate.localScale = new Vector3(0.9f, 0.7f, 0.25f);
            }

            // プレイヤーのスキルを使う敵は武器を持つ(色は型ごとに EnemyController が塗る)
            if (iProfile.SkillSets != null && iProfile.SkillSets.Count > 0)
            {
                var aWeapon = CreatePart(PrimitiveType.Cube, aRoot.transform, iSurfaceMaterial, iProfile.SkillSets[0].Color);
                aWeapon.name = "Weapon";
                aWeapon.localPosition = new Vector3(0.62f, 1.05f, 0.45f);
                aWeapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
                aWeapon.localScale = new Vector3(0.14f, 0.14f, 1.3f);
            }

            // エリートは金の冠をかぶる
            if (iProfile.Elite != EliteAffix.None)
            {
                var aCrown = CreatePart(PrimitiveType.Cube, aRoot.transform, iSurfaceMaterial, new Color(1f, 0.82f, 0.25f));
                aCrown.name = "Crown";
                aCrown.localPosition = new Vector3(0f, 2.25f, 0f);
                aCrown.localRotation = Quaternion.Euler(0f, 45f, 0f);
                aCrown.localScale = new Vector3(0.45f, 0.22f, 0.45f);
            }

            var aDamageable = aRoot.AddComponent<Damageable>();
            aDamageable.Configure(Team.Enemy, iProfile.MaxHealth, iProfile.MaxArmor, iProfile.ArmorRegenDelay);

            var aEnemy = aRoot.AddComponent<EnemyController>();
            aEnemy.Initialize(iProfile, iPlayer);
            return aEnemy;
        }

        static Transform CreatePart(PrimitiveType iShape, Transform iParent, Material iMaterial, Color iColor)
        {
            var aPart = GameObject.CreatePrimitive(iShape);
            Object.DestroyImmediate(aPart.GetComponent<Collider>());
            aPart.transform.SetParent(iParent, false);
            var aRenderer = aPart.GetComponent<Renderer>();
            if (iMaterial != null) aRenderer.sharedMaterial = iMaterial;
            aRenderer.material.color = iColor;
            return aPart.transform;
        }
    }
}
