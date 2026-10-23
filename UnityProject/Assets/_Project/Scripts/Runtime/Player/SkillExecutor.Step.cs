using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: 技の発動と、段の進行(構え → 攻撃判定 → 硬直)・当たり判定の範囲</summary>
    public partial class SkillExecutor
    {
        bool TryStartFromInput()
        {
            var aFinisher = mSlots.MasteryFinisher;
            if (aFinisher != null && mInput.Consume(PlayerAction.Finisher))
            {
                StartSkill(aFinisher, true);
                return true;
            }

            if (TryStartSlot(SlotType.Movement)) return true;
            if (TryStartSlot(SlotType.AttackA)) return true;
            if (TryStartSlot(SlotType.AttackB)) return true;

            if (mInput.Peek(PlayerAction.Jump) && mMotor.IsGrounded)
            {
                mInput.Consume(PlayerAction.Jump);
                if (mCurrent != null) EndSkill();
                return mMotor.TryJump();
            }
            return false;
        }

        bool TryStartSlot(SlotType iSlot)
        {
            var aAction = PlayerInputReader.ActionFor(iSlot);
            if (!mInput.Peek(aAction)) return false;

            var aSkill = mSlots.GetCurrent(iSlot);
            if (aSkill == null) return false;
            // 空中で使えない技は、着地まで先行入力として残す
            if (!aSkill.UsableInAir && !mMotor.IsGrounded) return false;

            mInput.Consume(aAction);
            if (mCurrent != null) EndSkill();
            StartSkill(aSkill, false);
            return true;
        }

        void StartSkill(SkillData iSkill, bool iFinisher)
        {
            mCurrent = iSkill;
            mIsFinisher = iFinisher;
            mSkillTime = 0f;
            mDamageMultiplier = 1f;
            mHitsEnabled = true;
            mJustDodgeAwarded = false;
            mTravelEnemy = null;
            mTravelToPoint = false;

            mWeapons.ShowTemporary(iSkill.Weapon);
            if (iSkill.Slot == SlotType.Movement)
            {
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, 1.1f, new Color(0.7f, 0.68f, 0.62f));
                }
                Rumble.Play(0.1f, 0.25f, 0.05f);
            }

            var aTarget = mLockOn.Target;
            Vector3 aDesired = Flat(mMotor.DesiredDirection);
            if (iSkill.Behavior == SkillBehavior.DashStrike || iSkill.Behavior == SkillBehavior.Blink)
                mMoveDirection = aDesired.sqrMagnitude > 0.01f ? aDesired.normalized : transform.forward;
            else if (aTarget != null)
                mMoveDirection = DirectionTo(aTarget.transform.position);
            else
                mMoveDirection = aDesired.sqrMagnitude > 0.01f ? aDesired.normalized : transform.forward;
            mMotor.FaceDirection(mMoveDirection);

            if (mMotor.IsGrounded)
            {
                mMotor.GravityScale = 1f;
            }
            else
            {
                mMotor.GravityScale = iSkill.AirGravityScale;
                if (mMotor.VerticalVelocity < 0f) mMotor.SetVerticalVelocity(0f);
            }

            switch (iSkill.Behavior)
            {
                case SkillBehavior.ChargeCounter:
                    mPhase = Phase.Charging;
                    mPhaseTime = 0f;
                    mSelf.CounterHandler = OnCounter;
                    break;
                case SkillBehavior.Grapple:
                    BeginGrapple();
                    break;
                case SkillBehavior.Pull:
                    BeginPull();
                    break;
                case SkillBehavior.Blink:
                    Blink();
                    BeginStep(0);
                    break;
                case SkillBehavior.Boost:
                    mMotor.GravityScale = iSkill.AirGravityScale;
                    mMotor.SetVerticalVelocity(iSkill.JumpVelocity);
                    BeginStep(0);
                    break;
                default:
                    BeginStep(0);
                    break;
            }
        }

        void BeginStep(int iIndex)
        {
            mStepIndex = iIndex;
            mPhase = Phase.Startup;
            mPhaseTime = 0f;
            mChainQueued = false;
            mStyleRegisteredThisStep = false;
            mLungedThisStep = false;
            // 振りかぶり: 振る向きと逆へ体をひねる。大槌は縦に伸びる
            if (mCurrent != null && IsSwingAttack(mCurrent))
            {
                if (mBodyPose == null) mBodyPose = GetComponent<PlayerHitReaction>();
                if (mBodyPose != null)
                {
                    mBodyPose.Twist(-SwingSide() * (mCurrent.Weapon == WeaponType.Hammer ? 8f : 18f));
                    if (mCurrent.Weapon == WeaponType.Hammer) mBodyPose.Squash(-0.15f);
                }
            }
            mHitThisStep.Clear();

            if (!mMotor.IsGrounded && mCurrent.Behavior != SkillBehavior.HammerJump && mMotor.VerticalVelocity < 0f)
            {
                mMotor.SetVerticalVelocity(0f);
            }
        }

        void TickSkill(float iDeltaTime)
        {
            mPhaseTime += iDeltaTime;
            mSkillTime += iDeltaTime;

            mSelf.Invulnerable = mSkillTime < FrameTime.ToSeconds(mCurrent.InvulnerableFrames)
                                || Time.time < mExtraInvulnerableUntil
                                || (mPhase == Phase.Travel && mCurrent.Behavior == SkillBehavior.Grapple);

            // 同じボタンで次の段を予約
            bool aInStep = mPhase == Phase.Startup || mPhase == Phase.Active || mPhase == Phase.Recovery;
            if (aInStep && !mChainQueued && !mCurrent.AutoChain && mStepIndex + 1 < mCurrent.StepCount
                && mInput.Consume(PlayerInputReader.ActionFor(mCurrent.Slot)))
            {
                mChainQueued = true;
            }

            switch (mPhase)
            {
                case Phase.Startup:
                {
                    ApplyForward(iDeltaTime);
                    float aDuration = FrameTime.ToSeconds(mStep.StartupFrames);
                    if (mPhaseTime >= aDuration)
                    {
                        mPhaseTime -= aDuration;
                        mPhase = Phase.Active;
                        OnActiveStart();
                    }
                    break;
                }
                case Phase.Active:
                {
                    ApplyForward(iDeltaTime);
                    DoHitbox();
                    float aDuration = FrameTime.ToSeconds(mStep.ActiveFrames);
                    if (mPhaseTime >= aDuration)
                    {
                        mPhaseTime -= aDuration;
                        if (mCurrent.Behavior == SkillBehavior.HammerJump && mStepIndex == 0)
                        {
                            mMotor.GravityScale = 1f;
                            mMotor.SetVerticalVelocity(mCurrent.JumpVelocity);
                            mPhase = Phase.Airborne;
                            mPhaseTime = 0f;
                        }
                        else
                        {
                            mPhase = Phase.Recovery;
                        }
                    }
                    break;
                }
                case Phase.Recovery:
                    TickRecovery();
                    break;
                case Phase.Charging:
                {
                    float aMax = FrameTime.ToSeconds(mCurrent.ChargeFrames);
                    if (!mInput.IsHeld(PlayerInputReader.ActionFor(mCurrent.Slot)) || mPhaseTime >= aMax)
                    {
                        ReleaseCharge(false);
                    }
                    break;
                }
                case Phase.Airborne:
                    mMotor.Displace(mMoveDirection * (mCurrent.DashDistance / 0.8f) * iDeltaTime);
                    if (mPhaseTime > 0.1f && mMotor.IsGrounded)
                    {
                        if (mCurrent.StepCount > 1) BeginStep(1);
                        else EndSkill();
                    }
                    break;
                case Phase.Travel:
                    TickTravel(iDeltaTime);
                    break;
                case Phase.PullWait:
                    if (mTravelEnemy == null || !mTravelEnemy.IsBeingPulled || mPhaseTime > 0.5f) BeginStep(0);
                    break;
                case Phase.Hang:
                    TickHang();
                    break;
            }
        }

        /// <summary>ポイントにぶら下がっている間。次のワイヤー・ジャンプ・空中攻撃につなげられる。</summary>
        void TickHang()
        {
            mMotor.GravityScale = 0f;
            mMotor.SetVerticalVelocity(0f);

            if (mInput.Consume(PlayerAction.Jump))
            {
                Vector3 aForward = Flat(mMotor.DesiredDirection);
                if (aForward.sqrMagnitude < 0.01f) aForward = transform.forward;
                EndSkill();
                mMotor.SetVerticalVelocity(mGrappleJumpVelocity);
                mMotor.AddImpulse(aForward.normalized * mGrappleJumpForward);
                return;
            }

            if (TryStartFromInput()) return;
            if (mPhaseTime >= mGrappleHangTime) EndSkill();
        }

        void TickRecovery()
        {
            var aStep = mStep;
            float aDuration = FrameTime.ToSeconds(aStep.RecoveryFrames);
            bool aHasNext = mStepIndex + 1 < mCurrent.StepCount;

            if (aHasNext && (mChainQueued || mCurrent.AutoChain))
            {
                float aChainAt = mCurrent.AutoChain ? aDuration : FrameTime.ToSeconds(aStep.ChainFrame);
                if (mPhaseTime >= aChainAt)
                {
                    BeginStep(mStepIndex + 1);
                    return;
                }
            }

            if (!mIsFinisher && mPhaseTime >= FrameTime.ToSeconds(aStep.CancelFrame) && TryStartFromInput()) return;

            if (mPhaseTime >= aDuration && !(aHasNext && mCurrent.AutoChain)) EndSkill();
        }

        void ApplyForward(float iDeltaTime)
        {
            var aStep = mStep;
            float aTotal = FrameTime.ToSeconds(aStep.StartupFrames + aStep.ActiveFrames);
            if (aTotal <= 0f || Mathf.Approximately(aStep.ForwardMove, 0f)) return;

            var aTarget = mLockOn.Target;
            if (aTarget != null && mCurrent.Behavior != SkillBehavior.DashStrike)
            {
                float aDistance = Flat(aTarget.transform.position - transform.position).magnitude;
                if (aDistance <= mStopDistance + aTarget.Radius) return;
            }
            mMotor.Displace(mMoveDirection * (aStep.ForwardMove * iDeltaTime / aTotal));
        }

        void OnActiveStart()
        {
            var aStep = mStep;
            var aData = mSlots.GetWeaponData(mCurrent.Weapon);
            var aColor = aData != null ? aData.Color : Color.white;
            PlayAttackSound();

            switch (mCurrent.Behavior)
            {
                case SkillBehavior.Projectile:
                    FireProjectiles(aColor);
                    return;
                case SkillBehavior.TargetedStrike:
                    FireStrike(aColor);
                    return;
                case SkillBehavior.Beam:
                    if (CombatFeedback.Instance != null)
                    {
                        GetBeam(out var aFrom, out var aOffset);
                        CombatFeedback.Instance.SpawnBeam(aFrom, aOffset, mCurrent.BeamRadius, aColor,
                            Mathf.Max(0.05f, FrameTime.ToSeconds(aStep.ActiveFrames)));
                    }
                    break;
            }

            if (aStep.HitboxRadius >= 2.5f && mCurrent.Behavior != SkillBehavior.Beam && CombatFeedback.Instance != null)
            {
                var aCenter = transform.TransformPoint(aStep.HitboxOffset);
                CombatFeedback.Instance.SpawnShockwave(new Vector3(aCenter.x, transform.position.y + 0.05f, aCenter.z),
                    aStep.HitboxRadius, aColor);
            }
            DoHitbox();
        }

        SkillHitContext CurrentContext() => new SkillHitContext
        {
            Skill = mCurrent,
            StepIndex = mStepIndex,
            Multiplier = mDamageMultiplier,
            SwapStrikeId = SwapStrikeReady ? mSwapStrikeId : -1,
        };

        void DoHitbox()
        {
            var aStep = mStep;
            if (!mHitsEnabled) return;
            if (mCurrent.Behavior == SkillBehavior.Projectile || mCurrent.Behavior == SkillBehavior.TargetedStrike) return;

            int aCount;
            if (mCurrent.Behavior == SkillBehavior.Beam)
            {
                GetBeam(out var aFrom, out var aOffset);
                aCount = Physics.OverlapCapsuleNonAlloc(aFrom, aOffset, mCurrent.BeamRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            }
            else
            {
                if (aStep.HitboxRadius <= 0f) return;
                Vector3 aCenter = transform.TransformPoint(aStep.HitboxOffset);
                aCount = Physics.OverlapSphereNonAlloc(aCenter, aStep.HitboxRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            }

            mTargetBuffer.Clear();
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                // 探索: ひび割れた壁(決まった武器種の技で崩れる)
                var aWall = mOverlapBuffer[aIndex].GetComponentInParent<BreakableWall>();
                if (aWall != null)
                {
                    aWall.TryBreak(mCurrent);
                    continue;
                }

                var aTarget = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                if (aTarget == null || aTarget.Team == mSelf.Team || aTarget.IsDead || !mHitThisStep.Add(aTarget)) continue;
                mTargetBuffer.Add(aTarget);
            }
            if (mTargetBuffer.Count > 0) ResolveHits(CurrentContext(), mTargetBuffer, transform.position);
        }
    }
}
