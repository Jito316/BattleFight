using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>技を撃った瞬間の情報。弾は技が終わったあとに当たることがあるので、発射時に固定しておく。</summary>
    public struct SkillHitContext
    {
        public SkillData skill;
        public int stepIndex;
        public float multiplier;
        /// <summary>発射時にスワップストライクが有効だったなら、その切り替えの番号。無効なら -1</summary>
        public int swapStrikeId;

        public SkillStep Step => skill.GetStep(stepIndex);
    }

    /// <summary>
    /// 技の弾。まっすぐ(またはロックオン対象へ曲がりながら)飛び、当たると SkillExecutor にヒットを報告する。
    /// 速度0 + 遅延 で使うと、その場で予告してから爆発する範囲攻撃(落雷など)になる。
    /// </summary>
    public class SkillProjectile : MonoBehaviour
    {
        static readonly Collider[] OverlapBuffer = new Collider[32];

        readonly HashSet<Damageable> alreadyHit = new HashSet<Damageable>();
        readonly List<Damageable> hits = new List<Damageable>();

        SkillExecutor owner;
        SkillHitContext context;
        Transform ownerRoot;
        Vector3 velocity;
        EnemyController homingTarget;
        float homing;
        float radius;
        float explosionRadius;
        float lifetime;
        float delay;
        bool pierce;
        float age;
        Color color;
        Transform telegraph;

        public static SkillProjectile Spawn(SkillExecutor owner, SkillHitContext context, Vector3 position, Vector3 velocity,
            EnemyController homingTarget, Color color, float delay = 0f)
        {
            var skill = context.skill;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Projectile_{skill.name}";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = position;

            var material = CombatFeedback.Instance != null ? CombatFeedback.Instance.EffectMaterial : null;
            var renderer = go.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.material.color = color;

            var projectile = go.AddComponent<SkillProjectile>();
            projectile.owner = owner;
            projectile.ownerRoot = owner.transform;
            projectile.context = context;
            projectile.velocity = velocity;
            projectile.homingTarget = homingTarget;
            projectile.homing = skill.homing;
            projectile.radius = Mathf.Max(0.05f, skill.projectileRadius);
            projectile.explosionRadius = skill.explosionRadius;
            projectile.lifetime = Mathf.Max(delay, skill.projectileLifetime);
            projectile.delay = delay;
            projectile.pierce = skill.projectilePierce;
            projectile.color = color;

            go.transform.localScale = Vector3.one * (projectile.radius * 2f);
            if (delay > 0f) projectile.CreateTelegraph(material);
            return projectile;
        }

        void CreateTelegraph(Material material)
        {
            // 予告の円。落ちる場所がわかるように地面に置く
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(disc.GetComponent<Collider>());
            disc.name = "Telegraph";
            var renderer = disc.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.material.color = color;
            telegraph = disc.transform;
            telegraph.position = new Vector3(transform.position.x, transform.position.y + 0.03f, transform.position.z);
            float diameter = Mathf.Max(radius, explosionRadius) * 2f;
            telegraph.localScale = new Vector3(diameter, 0.01f, diameter);
            GetComponent<Renderer>().enabled = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            if (age < delay)
            {
                if (telegraph != null)
                {
                    float blink = Mathf.PingPong(age * 10f, 1f);
                    float diameter = Mathf.Max(radius, explosionRadius) * 2f * (0.85f + 0.15f * blink);
                    telegraph.localScale = new Vector3(diameter, 0.01f, diameter);
                }
                return;
            }

            if (delay > 0f)
            {
                // 遅延型: その場で爆発して終わり
                Explode(transform.position);
                return;
            }

            if (homing > 0f && homingTarget != null && !homingTarget.IsDead && velocity.sqrMagnitude > 0.01f)
            {
                Vector3 desired = (homingTarget.CenterPoint - transform.position).normalized * velocity.magnitude;
                velocity = Vector3.RotateTowards(velocity, desired, homing * Mathf.Deg2Rad * dt, 0f);
            }

            Vector3 start = transform.position;
            Vector3 step = velocity * dt;

            // 壁や床に当たったら止まる(爆発する弾は爆発)
            if (step.sqrMagnitude > 0f
                && Physics.SphereCast(start, radius * 0.5f, step.normalized, out var wall, step.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && wall.collider.GetComponentInParent<Damageable>() == null)
            {
                transform.position = start + step.normalized * wall.distance;
                if (explosionRadius > 0f) Explode(transform.position);
                else Destroy(gameObject);
                return;
            }

            transform.position = start + step;
            if (TryHitEnemies()) return;

            if (age >= lifetime)
            {
                if (explosionRadius > 0f) Explode(transform.position);
                else Destroy(gameObject);
            }
        }

        bool TryHitEnemies()
        {
            hits.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var target = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (target == null || target.IsDead || target.Team == Team.Player || alreadyHit.Contains(target)) continue;
                if (ownerRoot != null && target.transform.IsChildOf(ownerRoot)) continue;
                hits.Add(target);
            }
            if (hits.Count == 0) return false;

            if (explosionRadius > 0f)
            {
                Explode(transform.position);
                return true;
            }

            foreach (var target in hits) alreadyHit.Add(target);
            if (owner != null) owner.ResolveHits(context, hits, transform.position - velocity.normalized);
            if (!pierce)
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        void Explode(Vector3 position)
        {
            hits.Clear();
            int count = Physics.OverlapSphereNonAlloc(position, explosionRadius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var target = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (target == null || target.IsDead || target.Team == Team.Player || hits.Contains(target)) continue;
                hits.Add(target);
            }

            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(position, explosionRadius, color);
                CombatFeedback.Instance.Shake(0.1f);
            }
            if (owner != null && hits.Count > 0) owner.ResolveHits(context, hits, position);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (telegraph != null) Destroy(telegraph.gameObject);
        }
    }
}
