using UnityEngine;

namespace BattleFight
{
    /// <summary>EnemyProfile から仮モデルの敵を組み立てる。</summary>
    public static class EnemyFactory
    {
        /// <param name="surfaceMaterial">色を付ける元のマテリアル。ビルドでシェーダーが削られないよう、シーンから参照させる</param>
        public static EnemyController Create(EnemyProfile profile, Vector3 position, Quaternion rotation, Transform player,
            Material surfaceMaterial)
        {
            var root = new GameObject(profile.displayName);
            root.transform.SetPositionAndRotation(position, rotation);
            root.transform.localScale = Vector3.one * profile.scale;

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.stepOffset = 0.3f;

            var body = CreatePart(profile.bodyShape, root.transform, surfaceMaterial, profile.color);
            body.localPosition = new Vector3(0f, 1f, 0f);
            if (profile.bodyShape != PrimitiveType.Capsule) body.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            if (profile.flying)
            {
                // 飛ぶ敵には羽を付ける
                var wings = CreatePart(PrimitiveType.Cube, root.transform, surfaceMaterial, Color.Lerp(profile.color, Color.white, 0.4f));
                wings.localPosition = new Vector3(0f, 1.2f, -0.1f);
                wings.localScale = new Vector3(2.4f, 0.08f, 0.6f);
            }

            // 向きがわかるように顔を付ける
            var visor = CreatePart(PrimitiveType.Cube, root.transform, surfaceMaterial, new Color(0.1f, 0.1f, 0.1f));
            visor.localPosition = new Vector3(0f, 1.5f, 0.4f);
            visor.localScale = new Vector3(0.6f, 0.18f, 0.25f);

            if (profile.maxArmor > 0f)
            {
                var plate = CreatePart(PrimitiveType.Cube, root.transform, surfaceMaterial, new Color(0.35f, 0.35f, 0.4f));
                plate.localPosition = new Vector3(0f, 1.05f, 0.35f);
                plate.localScale = new Vector3(0.9f, 0.7f, 0.25f);
            }

            // プレイヤーのスキルを使う敵は武器を持つ(色は型ごとに EnemyController が塗る)
            if (profile.skillSets != null && profile.skillSets.Count > 0)
            {
                var weapon = CreatePart(PrimitiveType.Cube, root.transform, surfaceMaterial, profile.skillSets[0].color);
                weapon.name = "Weapon";
                weapon.localPosition = new Vector3(0.62f, 1.05f, 0.45f);
                weapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
                weapon.localScale = new Vector3(0.14f, 0.14f, 1.3f);
            }

            var damageable = root.AddComponent<Damageable>();
            damageable.Configure(Team.Enemy, profile.maxHealth, profile.maxArmor, profile.armorRegenDelay);

            var enemy = root.AddComponent<EnemyController>();
            enemy.Initialize(profile, player);
            return enemy;
        }

        static Transform CreatePart(PrimitiveType shape, Transform parent, Material material, Color color)
        {
            var part = GameObject.CreatePrimitive(shape);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            var renderer = part.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.material.color = color;
            return part.transform;
        }
    }
}
