using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 敵の弾。まっすぐ飛んでプレイヤーに当たると消える。
    /// 速度0 + 遅延 で使うと、プレイヤーの足元に予告を出してから爆発する範囲攻撃になる。
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        static readonly Collider[] OverlapBuffer = new Collider[16];

        GameObject source;
        Vector3 velocity;
        float radius;
        float damage;
        float knockback;
        float lifetime;
        float delay;
        float age;
        Color color;
        Transform telegraph;

        public static EnemyProjectile Spawn(GameObject source, Vector3 position, Vector3 velocity, float radius, float damage,
            float knockback, Color color, float lifetime = 3f, float delay = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyProjectile";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (radius * 2f);

            var material = CombatFeedback.Instance != null ? CombatFeedback.Instance.EffectMaterial : null;
            var renderer = go.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.material.color = color;

            var projectile = go.AddComponent<EnemyProjectile>();
            projectile.source = source;
            projectile.velocity = velocity;
            projectile.radius = radius;
            projectile.damage = damage;
            projectile.knockback = knockback;
            projectile.lifetime = Mathf.Max(lifetime, delay);
            projectile.delay = delay;
            projectile.color = color;

            if (delay > 0f)
            {
                renderer.enabled = false;
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "EnemyTelegraph";
                DestroyImmediate(disc.GetComponent<Collider>());
                var discRenderer = disc.GetComponent<Renderer>();
                if (material != null) discRenderer.sharedMaterial = material;
                discRenderer.material.color = color;
                disc.transform.position = new Vector3(position.x, position.y + 0.03f, position.z);
                disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
                projectile.telegraph = disc.transform;
            }
            return projectile;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            if (delay > 0f)
            {
                if (telegraph != null)
                {
                    // 爆発が近づくほど速く点滅する
                    float t = age / delay;
                    float blink = Mathf.PingPong(age * (6f + 14f * t), 1f);
                    float diameter = radius * 2f * (0.6f + 0.4f * t) * (0.9f + 0.1f * blink);
                    telegraph.localScale = new Vector3(diameter, 0.01f, diameter);
                }
                if (age >= delay) Explode();
                return;
            }

            Vector3 step = velocity * dt;
            if (Physics.SphereCast(transform.position, radius * 0.5f, step.normalized, out var hit, step.magnitude, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                var victim = hit.collider.GetComponentInParent<Damageable>();
                if (victim != null && victim.Team == Team.Player) HitPlayer(victim);
                else if (victim == null)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            transform.position += step;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var victim = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (victim != null && victim.Team == Team.Player)
                {
                    HitPlayer(victim);
                    return;
                }
            }

            if (age >= lifetime) Destroy(gameObject);
        }

        void HitPlayer(Damageable victim)
        {
            Vector3 direction = velocity;
            direction.y = 0f;
            victim.ApplyHit(new HitInfo
            {
                damage = damage,
                stagger = 100f,
                knockback = knockback,
                direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward,
                source = source,
            });
            Destroy(gameObject);
        }

        void Explode()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var victim = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (victim == null || victim.Team != Team.Player) continue;
                Vector3 direction = victim.transform.position - transform.position;
                direction.y = 0f;
                victim.ApplyHit(new HitInfo
                {
                    damage = damage,
                    stagger = 100f,
                    knockback = knockback,
                    direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward,
                    source = source,
                });
                break;
            }
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position, radius, color);
                CombatFeedback.Instance.Shake(0.15f);
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (telegraph != null) Destroy(telegraph.gameObject);
        }
    }
}
