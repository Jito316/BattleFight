using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: ブリンク・ワイヤー・引き寄せ</summary>
    public partial class SkillExecutor
    {
        void Blink()
        {
            Vector3 aDirection = mMoveDirection;
            float aDistance = mCurrent.DashDistance;
            Vector3 aBottom = transform.position + Vector3.up * 0.5f;
            Vector3 aTop = transform.position + Vector3.up * 1.5f;
            if (Physics.CapsuleCast(aBottom, aTop, 0.35f, aDirection, out var aHit, aDistance, ~0, QueryTriggerInteraction.Ignore)
                && !aHit.collider.transform.IsChildOf(transform))
            {
                aDistance = Mathf.Max(0f, aHit.distance - 0.2f);
            }

            var aData = mSlots.GetWeaponData(mCurrent.Weapon);
            var aColor = aData != null ? aData.Color : Color.white;
            var aFeedback = CombatFeedback.Instance;
            if (aFeedback != null && !aFeedback.PlayCue(EffectCue.BlinkOut, transform.position, transform.rotation, 1.2f, aColor))
                aFeedback.SpawnShockwave(transform.position, 1.2f, aColor);
            mMotor.Teleport(aDirection * aDistance);
            if (aFeedback != null && !aFeedback.PlayCue(EffectCue.BlinkIn, transform.position + aDirection * aDistance, transform.rotation, 1.6f, aColor))
                aFeedback.SpawnShockwave(transform.position + aDirection * aDistance, 1.6f, aColor);
        }

        void BeginGrapple()
        {
            mMotor.GravityScale = 0f;
            mMotor.SetVerticalVelocity(0f);

            // 優先順: 視界内で一番近いポイント → ロックオン中の敵 → 空中ダッシュ
            var aPoint = FindGrapplePoint(mCurrent.Range);
            var aTarget = mLockOn.Target;
            if (aPoint != null)
            {
                mTravelToPoint = true;
                mTravelTarget = aPoint.transform.position - Vector3.up * 1.8f;
            }
            else if (aTarget != null && Vector3.Distance(aTarget.transform.position, transform.position) <= mCurrent.Range)
            {
                mTravelEnemy = aTarget;
            }
            else
            {
                mTravelTarget = transform.position + mMoveDirection * mCurrent.DashDistance + Vector3.up * 0.8f;
            }

            Vector3 aDestination = mTravelEnemy != null ? mTravelEnemy.transform.position : mTravelTarget;
            BeginTravel(aDestination);
            mHitsEnabled = mTravelEnemy != null;
        }

        /// <summary>
        /// 視界内(カメラに映っていて、途中に遮るものがない)にあるグラップルポイントのうち、一番近いもの。
        /// 今ぶら下がっているポイントは除く。
        /// </summary>
        GrapplePoint FindGrapplePoint(float iRange)
        {
            Vector3 aChest = transform.position + Vector3.up;
            GrapplePoint aBest = null;
            float aBestDistance = float.MaxValue;
            foreach (var aPoint in GrapplePoint.All)
            {
                Vector3 aPosition = aPoint.transform.position;
                float aDistance = Vector3.Distance(aChest, aPosition);
                if (aDistance > iRange || aDistance < mGrappleMinDistance || aDistance >= aBestDistance) continue;
                if (!IsInView(aPosition)) continue;
                if (Physics.Linecast(aChest, aPosition, out var aHit, ~0, QueryTriggerInteraction.Ignore)
                    && !aHit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }
                aBest = aPoint;
                aBestDistance = aDistance;
            }
            return aBest;
        }

        bool IsInView(Vector3 iPosition)
        {
            if (mView == null) return Vector3.Dot(Flat(iPosition - transform.position).normalized, transform.forward) > 0.3f;
            Vector3 aViewport = mView.WorldToViewportPoint(iPosition);
            return aViewport.z > 0f
                   && aViewport.x >= mGrappleViewMargin && aViewport.x <= 1f - mGrappleViewMargin
                   && aViewport.y >= mGrappleViewMargin && aViewport.y <= 1f - mGrappleViewMargin;
        }

        void BeginTravel(Vector3 iDestination)
        {
            mPhase = Phase.Travel;
            mPhaseTime = 0f;
            float aSpeed = Mathf.Max(1f, mCurrent.MoveSpeed);
            mTravelTimeout = Vector3.Distance(transform.position, iDestination) / aSpeed + mTravelTimeMargin;
        }

        void BeginPull()
        {
            var aTarget = mLockOn.Target;
            if (aTarget == null || Vector3.Distance(aTarget.transform.position, transform.position) > mCurrent.Range)
            {
                aTarget = FindEnemyInFront(mCurrent.Range);
            }

            if (aTarget == null)
            {
                BeginStep(0);
                return;
            }

            mTravelEnemy = aTarget;
            mMoveDirection = DirectionTo(aTarget.transform.position);
            mMotor.FaceDirection(mMoveDirection);

            if (aTarget.IsHeavy)
            {
                // 重い敵には自分が飛びつく
                mMotor.GravityScale = 0f;
                mMotor.SetVerticalVelocity(0f);
                BeginTravel(aTarget.transform.position);
                return;
            }

            aTarget.BeginPull(transform.position + mMoveDirection * 1.6f, mCurrent.MoveSpeed);
            mPhase = Phase.PullWait;
            mPhaseTime = 0f;
        }

        void TickTravel(float iDeltaTime)
        {
            mMotor.GravityScale = 0f;
            mMotor.SetVerticalVelocity(0f);

            if (mTravelEnemy != null && mTravelEnemy.IsDead)
            {
                mTravelEnemy = null;
                mHitsEnabled = false;
                ArriveTravel();
                return;
            }

            Vector3 aDestination = mTravelEnemy != null ? ApproachPoint(mTravelEnemy) : mTravelTarget;
            Vector3 aOffset = aDestination - transform.position;
            float aStepLength = mCurrent.MoveSpeed * iDeltaTime;

            Vector3 aFlat = Flat(aOffset);
            if (aFlat.sqrMagnitude > 0.01f)
            {
                mMoveDirection = aFlat.normalized;
                mMotor.FaceDirection(mMoveDirection);
            }

            if (aOffset.magnitude <= aStepLength + 0.3f || mPhaseTime >= mTravelTimeout)
            {
                mMotor.Displace(Vector3.ClampMagnitude(aOffset, aStepLength));
                ArriveTravel();
            }
            else
            {
                mMotor.Displace(aOffset.normalized * aStepLength);
            }
        }

        void ArriveTravel()
        {
            if (mTravelToPoint)
            {
                mPhase = Phase.Hang;
                mPhaseTime = 0f;
                return;
            }

            mMotor.GravityScale = mCurrent.AirGravityScale;
            if (mTravelEnemy != null) mMotor.FaceDirection(DirectionTo(mTravelEnemy.transform.position));
            BeginStep(0);
        }

        Vector3 ApproachPoint(EnemyController iEnemy)
        {
            Vector3 aAway = Flat(transform.position - iEnemy.transform.position);
            aAway = aAway.sqrMagnitude > 0.01f ? aAway.normalized : -transform.forward;
            return iEnemy.transform.position + aAway * (iEnemy.Radius + 1.0f);
        }

        EnemyController FindEnemyInFront(float iRange)
        {
            EnemyController aBest = null;
            float aBestDistance = float.MaxValue;
            foreach (var aEnemy in EnemyController.Active)
            {
                if (aEnemy.IsDead) continue;
                Vector3 aOffset = aEnemy.transform.position - transform.position;
                float aDistance = aOffset.magnitude;
                if (aDistance > iRange || Vector3.Dot(Flat(aOffset).normalized, mMoveDirection) < 0.5f) continue;
                if (aDistance < aBestDistance)
                {
                    aBestDistance = aDistance;
                    aBest = aEnemy;
                }
            }
            return aBest;
        }
    }
}
