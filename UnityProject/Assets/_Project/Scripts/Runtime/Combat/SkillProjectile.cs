using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>技を撃った瞬間の情報。弾は技が終わったあとに当たることがあるので、発射時に固定しておく。</summary>
    public struct SkillHitContext
    {
        public SkillData Skill;
        public int StepIndex;
        public float Multiplier;
        /// <summary>発射時にスワップストライクが有効だったなら、その切り替えの番号。無効なら -1</summary>
        public int SwapStrikeId;

        public SkillStep Step => Skill.GetStep(StepIndex);
    }

    /// <summary>
    /// 技の弾。まっすぐ(またはロックオン対象へ曲がりながら)飛び、当たると SkillExecutor にヒットを報告する。
    /// 速度0 + 遅延 で使うと、その場で予告してから爆発する範囲攻撃(落雷など)になる。
    /// </summary>
    public class SkillProjectile : MonoBehaviour
    {
        static readonly Collider[] mOverlapBuffer = new Collider[32];
        static readonly RaycastHit[] mCastBuffer = new RaycastHit[32];

        readonly HashSet<Damageable> mAlreadyHit = new HashSet<Damageable>();
        readonly List<Damageable> mHits = new List<Damageable>();
        readonly List<float> mHitDistances = new List<float>();

        SkillExecutor mOwner;
        SkillHitContext mContext;
        Transform mOwnerRoot;
        Vector3 mVelocity;
        EnemyController mHomingTarget;
        float mHoming;
        float mRadius;
        float mExplosionRadius;
        float mLifetime;
        float mDelay;
        bool mPierce;
        float mAge;
        Color mColor;
        Transform mTelegraph;

        public static SkillProjectile Spawn(SkillExecutor iOwner, SkillHitContext iContext, Vector3 iPosition, Vector3 iVelocity,
            EnemyController iHomingTarget, Color iColor, float iDelay = 0f)
        {
            var aSkill = iContext.Skill;
            var aObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aObject.name = $"Projectile_{aSkill.name}";
            DestroyImmediate(aObject.GetComponent<Collider>());
            aObject.transform.position = iPosition;

            var aMaterial = CombatFeedback.Instance != null ? CombatFeedback.Instance.EffectMaterial : null;
            var aRenderer = aObject.GetComponent<Renderer>();
            if (aMaterial != null) aRenderer.sharedMaterial = aMaterial;
            aRenderer.material.color = iColor;

            var aProjectile = aObject.AddComponent<SkillProjectile>();
            aProjectile.mOwner = iOwner;
            aProjectile.mOwnerRoot = iOwner.transform;
            aProjectile.mContext = iContext;
            aProjectile.mVelocity = iVelocity;
            aProjectile.mHomingTarget = iHomingTarget;
            aProjectile.mHoming = aSkill.Homing;
            aProjectile.mRadius = Mathf.Max(0.05f, aSkill.ProjectileRadius);
            aProjectile.mExplosionRadius = aSkill.ExplosionRadius;
            aProjectile.mLifetime = Mathf.Max(iDelay, aSkill.ProjectileLifetime);
            aProjectile.mDelay = iDelay;
            aProjectile.mPierce = aSkill.ProjectilePierce;
            aProjectile.mColor = iColor;

            aObject.transform.localScale = Vector3.one * (aProjectile.mRadius * 2f);
            if (iDelay > 0f) aProjectile.CreateTelegraph(aMaterial);
            return aProjectile;
        }

        void CreateTelegraph(Material iMaterial)
        {
            // 予告の円。落ちる場所がわかるように地面に置く
            var aDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(aDisc.GetComponent<Collider>());
            aDisc.name = "Telegraph";
            var aRenderer = aDisc.GetComponent<Renderer>();
            if (iMaterial != null) aRenderer.sharedMaterial = iMaterial;
            aRenderer.material.color = mColor;
            mTelegraph = aDisc.transform;
            mTelegraph.position = new Vector3(transform.position.x, transform.position.y + 0.03f, transform.position.z);
            float aDiameter = Mathf.Max(mRadius, mExplosionRadius) * 2f;
            mTelegraph.localScale = new Vector3(aDiameter, 0.01f, aDiameter);
            GetComponent<Renderer>().enabled = false;
        }

        void Update()
        {
            float aDeltaTime = Time.deltaTime;
            mAge += aDeltaTime;

            if (mAge < mDelay)
            {
                if (mTelegraph != null)
                {
                    float aBlink = Mathf.PingPong(mAge * 10f, 1f);
                    float aDiameter = Mathf.Max(mRadius, mExplosionRadius) * 2f * (0.85f + 0.15f * aBlink);
                    mTelegraph.localScale = new Vector3(aDiameter, 0.01f, aDiameter);
                }
                return;
            }

            if (mDelay > 0f)
            {
                // 遅延型: その場で爆発して終わり
                Explode(transform.position);
                return;
            }

            if (mHoming > 0f && mHomingTarget != null && !mHomingTarget.IsDead && mVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 aDesired = (mHomingTarget.CenterPoint - transform.position).normalized * mVelocity.magnitude;
                mVelocity = Vector3.RotateTowards(mVelocity, aDesired, mHoming * Mathf.Deg2Rad * aDeltaTime, 0f);
            }

            Vector3 aStart = transform.position;
            Vector3 aStep = mVelocity * aDeltaTime;
            float aDistance = aStep.magnitude;
            Vector3 aDirection = aDistance > 1e-5f ? aStep / aDistance : transform.forward;

            // 速い弾は 1 フレームで敵の幅より長く進むので、進んだ道の上をまとめて調べる(弾抜けしないように)
            float aWallDistance = WallDistance(aStart, aDirection, aDistance);
            if (TryHitEnemies(aStart, aDirection, Mathf.Min(aDistance, aWallDistance))) return;

            // 壁や床に当たったら止まる(爆発する弾は爆発)
            if (aWallDistance < aDistance)
            {
                transform.position = aStart + aDirection * aWallDistance;
                if (mExplosionRadius > 0f) Explode(transform.position);
                else Destroy(gameObject);
                return;
            }

            transform.position = aStart + aStep;

            if (mAge >= mLifetime)
            {
                if (mExplosionRadius > 0f) Explode(transform.position);
                else Destroy(gameObject);
            }
        }

        /// <summary>この先 iDistance までにある壁・床までの距離。なければ float.MaxValue(敵は壁として扱わない)</summary>
        float WallDistance(Vector3 iStart, Vector3 iDirection, float iDistance)
        {
            if (iDistance <= 0f) return float.MaxValue;
            int aCount = Physics.SphereCastNonAlloc(iStart, mRadius * 0.5f, iDirection, mCastBuffer, iDistance, ~0, QueryTriggerInteraction.Ignore);
            float aNearest = float.MaxValue;
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aHit = mCastBuffer[aIndex];
                // 撃った瞬間に重なっているもの(距離 0)は壁としない
                if (aHit.distance <= 0f || aHit.collider.GetComponentInParent<Damageable>() != null) continue;
                aNearest = Mathf.Min(aNearest, aHit.distance);
            }
            return aNearest;
        }

        /// <summary>
        /// iStart から iDistance 進む間に触れた敵に当てる。貫通しない弾は、いちばん手前の敵に当たってそこで消える。
        /// 当たって消えた(または爆発した)ら true
        /// </summary>
        bool TryHitEnemies(Vector3 iStart, Vector3 iDirection, float iDistance)
        {
            mHits.Clear();
            mHitDistances.Clear();
            int aCount = Physics.SphereCastNonAlloc(iStart, mRadius, iDirection, mCastBuffer, Mathf.Max(iDistance, 1e-4f), ~0, QueryTriggerInteraction.Ignore);
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aHit = mCastBuffer[aIndex];
                var aTarget = aHit.collider.GetComponentInParent<Damageable>();
                if (aTarget == null || aTarget.IsDead || aTarget.Team == Team.Player || mAlreadyHit.Contains(aTarget)) continue;
                if (mOwnerRoot != null && aTarget.transform.IsChildOf(mOwnerRoot)) continue;
                int aExisting = mHits.IndexOf(aTarget);
                if (aExisting >= 0)
                {
                    mHitDistances[aExisting] = Mathf.Min(mHitDistances[aExisting], aHit.distance);
                    continue;
                }
                mHits.Add(aTarget);
                mHitDistances.Add(aHit.distance);
            }
            if (mHits.Count == 0) return false;

            // いちばん手前の敵
            int aFirst = 0;
            for (int aIndex = 1; aIndex < mHits.Count; aIndex++)
            {
                if (mHitDistances[aIndex] < mHitDistances[aFirst]) aFirst = aIndex;
            }
            Vector3 aImpact = iStart + iDirection * mHitDistances[aFirst];

            if (mExplosionRadius > 0f)
            {
                transform.position = aImpact;
                Explode(aImpact);
                return true;
            }

            if (!mPierce)
            {
                // 貫通しない弾は、手前の 1 体だけに当たる
                var aTarget = mHits[aFirst];
                mHits.Clear();
                mHits.Add(aTarget);
            }
            foreach (var aTarget in mHits) mAlreadyHit.Add(aTarget);
            if (mOwner != null) mOwner.ResolveHits(mContext, mHits, aImpact - iDirection);
            if (!mPierce)
            {
                transform.position = aImpact;
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        void Explode(Vector3 iPosition)
        {
            mHits.Clear();
            int aCount = Physics.OverlapSphereNonAlloc(iPosition, mExplosionRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aTarget = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                if (aTarget == null || aTarget.IsDead || aTarget.Team == Team.Player || mHits.Contains(aTarget)) continue;
                mHits.Add(aTarget);
            }

            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(iPosition, mExplosionRadius, mColor);
                CombatFeedback.Instance.Shake(0.1f);
            }
            if (mOwner != null && mHits.Count > 0) mOwner.ResolveHits(mContext, mHits, iPosition);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mTelegraph != null) Destroy(mTelegraph.gameObject);
        }
    }
}
