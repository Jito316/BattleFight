using UnityEngine;

namespace BattleFight
{
    /// <summary>EnemyProfile から仮モデルの敵を組み立てる。</summary>
    public static class EnemyFactory
    {
        public static EnemyController Create(EnemyProfile profile, Vector3 position, Quaternion rotation, Transform player)
        {
            var root = new GameObject(profile.displayName);
            root.transform.SetPositionAndRotation(position, rotation);
            root.transform.localScale = Vector3.one * profile.scale;

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.stepOffset = 0.3f;

            var body = CreatePart(PrimitiveType.Capsule, root.transform, profile.color);
            body.localPosition = new Vector3(0f, 1f, 0f);

            // 向きがわかるように顔を付ける
            var visor = CreatePart(PrimitiveType.Cube, root.transform, new Color(0.1f, 0.1f, 0.1f));
            visor.localPosition = new Vector3(0f, 1.5f, 0.4f);
            visor.localScale = new Vector3(0.6f, 0.18f, 0.25f);

            if (profile.maxArmor > 0f)
            {
                var plate = CreatePart(PrimitiveType.Cube, root.transform, new Color(0.35f, 0.35f, 0.4f));
                plate.localPosition = new Vector3(0f, 1.05f, 0.35f);
                plate.localScale = new Vector3(0.9f, 0.7f, 0.25f);
            }

            var damageable = root.AddComponent<Damageable>();
            damageable.Configure(Team.Enemy, profile.maxHealth, profile.maxArmor, profile.armorRegenDelay);

            var enemy = root.AddComponent<EnemyController>();
            enemy.Initialize(profile, player);
            return enemy;
        }

        static Transform CreatePart(PrimitiveType shape, Transform parent, Color color)
        {
            var part = GameObject.CreatePrimitive(shape);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.GetComponent<Renderer>().material.color = color;
            return part.transform;
        }
    }
}
