using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: 弾・遠くへの一撃・ビーム</summary>
    public partial class SkillExecutor
    {
        /// <summary>弾を撃つ方向。ロックオン中は対象の胸へ、それ以外は向いている方向へ水平に。</summary>
        Vector3 AimDirection(Vector3 iOrigin)
        {
            var aTarget = mLockOn.Target;
            if (aTarget != null && !aTarget.IsDead)
            {
                Vector3 aOffset = aTarget.CenterPoint - iOrigin;
                if (aOffset.sqrMagnitude > 0.01f) return aOffset.normalized;
            }
            return transform.forward;
        }

        Vector3 mMuzzlePosition => transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;

        void FireProjectiles(Color iColor)
        {
            var aContext = CurrentContext();
            Vector3 aOrigin = mMuzzlePosition;
            Vector3 aAim = AimDirection(aOrigin);
            int aCount = Mathf.Max(1, mCurrent.ProjectileCount);
            bool aFullCircle = mCurrent.ProjectileSpread >= 359f;

            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                float aAngle;
                if (aCount == 1) aAngle = 0f;
                else if (aFullCircle) aAngle = 360f * aIndex / aCount;
                else aAngle = Mathf.Lerp(-mCurrent.ProjectileSpread * 0.5f, mCurrent.ProjectileSpread * 0.5f, aIndex / (aCount - 1f));

                Vector3 aDirection = Quaternion.AngleAxis(aAngle, Vector3.up) * aAim;
                Vector3 aSpawn = aFullCircle ? transform.position + Vector3.up * 1.2f + aDirection * 0.6f : aOrigin;
                SkillProjectile.Spawn(this, aContext, aSpawn, aDirection * mCurrent.ProjectileSpeed, mLockOn.Target, iColor);
            }
        }

        void FireStrike(Color iColor)
        {
            var aTarget = mLockOn.Target;
            Vector3 aPosition;
            if (aTarget != null && !aTarget.IsDead && Vector3.Distance(aTarget.transform.position, transform.position) <= mCurrent.StrikeDistance * 2f)
            {
                aPosition = aTarget.transform.position;
            }
            else
            {
                aPosition = transform.position + transform.forward * mCurrent.StrikeDistance;
                // 地面の高さに合わせる
                if (Physics.Raycast(aPosition + Vector3.up * 5f, Vector3.down, out var aGround, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    aPosition = aGround.point;
                }
            }
            SkillProjectile.Spawn(this, CurrentContext(), aPosition + Vector3.up * 0.1f, Vector3.zero, null, iColor, mCurrent.StrikeDelay);
        }

        void GetBeam(out Vector3 oFrom, out Vector3 oTo)
        {
            oFrom = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
            Vector3 aDirection = AimDirection(oFrom);
            float aLength = mCurrent.BeamLength;
            if (Physics.Raycast(oFrom, aDirection, out var aWall, aLength, ~0, QueryTriggerInteraction.Ignore)
                && aWall.collider.GetComponentInParent<Damageable>() == null)
            {
                aLength = aWall.distance;
            }
            oTo = oFrom + aDirection * aLength;
        }
    }
}
