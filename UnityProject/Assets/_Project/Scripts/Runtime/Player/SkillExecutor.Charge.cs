using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: 溜め・居合のカウンター</summary>
    public partial class SkillExecutor
    {
        bool OnCounter(HitInfo iHit)
        {
            if (mCurrent == null || mPhase != Phase.Charging) return false;
            ReleaseCharge(true);
            // カウンター: 受け止めた瞬間に止まって光り、殴った敵が弾かれる
            Freeze(0.14f);
            var aEnemy = !iHit.Ranged && iHit.Source != null ? iHit.Source.GetComponent<EnemyController>() : null;
            if (aEnemy != null) aEnemy.Rebound(iHit.Direction, false);
            SoundPlayer.Play(Sfx.Parry, 0.9f, 0.85f);
            Rumble.Play(0.6f, 0.8f, 0.16f);
            if (CombatFeedback.Instance != null)
            {
                var aColor = new Color(1f, 0.4f, 0.4f);
                CombatFeedback.Instance.Flash(0.45f);
                CombatFeedback.Instance.Punch(4f);
                CombatFeedback.Instance.Vignette(aColor, 0.6f);
                var aContact = transform.position + Vector3.up * 1.1f - iHit.Direction * 0.6f;
                if (!CombatFeedback.Instance.PlayCue(EffectCue.Counter, aContact, 1f, aColor))
                    CombatFeedback.Instance.SpawnSparks(aContact, aColor, 14, 9f);
            }
            return true;
        }

        void ReleaseCharge(bool iCounter)
        {
            mSelf.CounterHandler = null;
            float aMax = FrameTime.ToSeconds(mCurrent.ChargeFrames);
            float aRatio = iCounter || aMax <= 0f ? 1f : Mathf.Clamp01(mPhaseTime / aMax);
            mDamageMultiplier = Mathf.Lerp(mMinChargeMultiplier, mMaxChargeMultiplier, aRatio) * (iCounter ? mCounterMultiplier : 1f);

            if (iCounter)
            {
                mExtraInvulnerableUntil = Time.time + mCounterInvulnerableTime;
                mStyle.AddBonus(mStyle.Config.CounterBonus, "COUNTER!", new Color(1f, 0.4f, 0.4f));
            }

            var aTarget = mLockOn.Target;
            if (aTarget != null)
            {
                mMoveDirection = DirectionTo(aTarget.transform.position);
                mMotor.FaceDirection(mMoveDirection);
            }
            BeginStep(0);
        }
    }
}
