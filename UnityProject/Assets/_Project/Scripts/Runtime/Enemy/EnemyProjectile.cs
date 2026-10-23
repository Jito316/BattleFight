using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 敵の弾。まっすぐ飛んでプレイヤーに当たると消える。
    /// 速度0 + 遅延 で使うと、プレイヤーの足元に予告を出してから爆発する範囲攻撃になる。
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        static readonly Collider[] mOverlapBuffer = new Collider[16];

        GameObject mSource;
        Vector3 mVelocity;
        float mRadius;
        float mDamage;
        float mKnockback;
        float mLifetime;
        float mDelay;
        bool mUnblockable;
        float mAge;
        Color mColor;
        Transform mTelegraph;

        public static EnemyProjectile Spawn(GameObject iSource, Vector3 iPosition, Vector3 iVelocity, float iRadius, float iDamage,
            float iKnockback, Color iColor, float iLifetime = 3f, float iDelay = 0f, bool iUnblockable = false)
        {
            var aObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aObject.name = "EnemyProjectile";
            DestroyImmediate(aObject.GetComponent<Collider>());
            aObject.transform.position = iPosition;
            aObject.transform.localScale = Vector3.one * (iRadius * 2f);

            var aMaterial = CombatFeedback.Instance != null ? CombatFeedback.Instance.EffectMaterial : null;
            var aRenderer = aObject.GetComponent<Renderer>();
            if (aMaterial != null) aRenderer.sharedMaterial = aMaterial;
            aRenderer.material.color = iColor;

            var aProjectile = aObject.AddComponent<EnemyProjectile>();
            aProjectile.mSource = iSource;
            aProjectile.mVelocity = iVelocity;
            aProjectile.mRadius = iRadius;
            aProjectile.mDamage = iDamage;
            aProjectile.mKnockback = iKnockback;
            aProjectile.mLifetime = Mathf.Max(iLifetime, iDelay);
            aProjectile.mDelay = iDelay;
            aProjectile.mUnblockable = iUnblockable;
            aProjectile.mColor = iColor;

            if (iDelay > 0f)
            {
                aRenderer.enabled = false;
                var aDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                aDisc.name = "EnemyTelegraph";
                DestroyImmediate(aDisc.GetComponent<Collider>());
                var aDiscRenderer = aDisc.GetComponent<Renderer>();
                if (aMaterial != null) aDiscRenderer.sharedMaterial = aMaterial;
                aDiscRenderer.material.color = iColor;
                aDisc.transform.position = new Vector3(iPosition.x, iPosition.y + 0.03f, iPosition.z);
                aDisc.transform.localScale = new Vector3(iRadius * 2f, 0.01f, iRadius * 2f);
                aProjectile.mTelegraph = aDisc.transform;
            }
            return aProjectile;
        }

        void Update()
        {
            // 敵の弾も「見切り」の間は遅くなる
            float aDeltaTime = EnemyTime.DeltaTime;
            mAge += aDeltaTime;

            if (mDelay > 0f)
            {
                if (mTelegraph != null)
                {
                    // 爆発が近づくほど速く点滅する
                    float aProgress = mAge / mDelay;
                    float aBlink = Mathf.PingPong(mAge * (6f + 14f * aProgress), 1f);
                    float aDiameter = mRadius * 2f * (0.6f + 0.4f * aProgress) * (0.9f + 0.1f * aBlink);
                    mTelegraph.localScale = new Vector3(aDiameter, 0.01f, aDiameter);
                }
                if (mAge >= mDelay) Explode();
                return;
            }

            Vector3 aStep = mVelocity * aDeltaTime;
            if (Physics.SphereCast(transform.position, mRadius * 0.5f, aStep.normalized, out var aHit, aStep.magnitude, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                var aVictim = aHit.collider.GetComponentInParent<Damageable>();
                if (aVictim != null && aVictim.Team == Team.Player) HitPlayer(aVictim);
                else if (aVictim == null)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            transform.position += aStep;

            int aCount = Physics.OverlapSphereNonAlloc(transform.position, mRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aVictim = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                if (aVictim != null && aVictim.Team == Team.Player)
                {
                    HitPlayer(aVictim);
                    return;
                }
            }

            if (mAge >= mLifetime) Destroy(gameObject);
        }

        void HitPlayer(Damageable iVictim)
        {
            Vector3 aDirection = mVelocity;
            aDirection.y = 0f;
            iVictim.ApplyHit(new HitInfo
            {
                Damage = mDamage,
                Stagger = 100f,
                Knockback = mKnockback,
                Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : Vector3.forward,
                Source = mSource,
                Ranged = true,
                Unblockable = mUnblockable,
            });
            Destroy(gameObject);
        }

        void Explode()
        {
            int aCount = Physics.OverlapSphereNonAlloc(transform.position, mRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aVictim = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                if (aVictim == null || aVictim.Team != Team.Player) continue;
                Vector3 aDirection = aVictim.transform.position - transform.position;
                aDirection.y = 0f;
                aVictim.ApplyHit(new HitInfo
                {
                    Damage = mDamage,
                    Stagger = 100f,
                    Knockback = mKnockback,
                    Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : Vector3.forward,
                    Source = mSource,
                    Ranged = true,
                    Unblockable = mUnblockable,
                });
                break;
            }
            if (CombatFeedback.Instance != null)
            {
                if (!CombatFeedback.Instance.PlayCue(EffectCue.EnemyProjectileBurst, transform.position, mRadius, mColor))
                    CombatFeedback.Instance.SpawnShockwave(transform.position, mRadius, mColor);
                CombatFeedback.Instance.Shake(0.15f);
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mTelegraph != null) Destroy(mTelegraph.gameObject);
        }
    }
}
