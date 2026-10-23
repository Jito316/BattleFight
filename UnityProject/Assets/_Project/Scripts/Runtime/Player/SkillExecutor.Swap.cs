using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: 切り替え・スワップキャンセル・チェンジアタック</summary>
    public partial class SkillExecutor
    {
        void HandleSwaps()
        {
            if (mSlots.Mode == SwapMode.Rack)
            {
                TrySwap(SlotType.AttackA, PlayerAction.Swap1);
                TrySwap(SlotType.AttackB, PlayerAction.Swap2);
                TrySwap(SlotType.Movement, PlayerAction.Swap3);
                mInput.Consume(PlayerAction.Swap4);
                return;
            }

            for (int aIndex = 0; aIndex < SkillSlotController.PresetCount; aIndex++)
            {
                if (mInput.Consume(PlayerInputReader.SwapAction(aIndex))) TrySelectPreset(aIndex);
            }
        }

        /// <summary>ラック方式: 1スロットだけ切り替える</summary>
        void TrySwap(SlotType iSlot, PlayerAction iAction)
        {
            if (!mInput.Consume(iAction)) return;

            bool aCancel = CanSwapCancel(iSlot);
            if (!mSlots.Cycle(iSlot)) return;

            OnSwapped(mSlots.GetCurrent(iSlot), iSlot == SlotType.AttackA);
            if (aCancel) SwapCancel(iSlot);
        }

        /// <summary>プリセット方式: 3スロットをまとめて切り替える</summary>
        void TrySelectPreset(int iIndex)
        {
            bool aCancel = CanPresetCancel(iIndex);
            SlotType? aCancelSlot = mCurrent != null ? mCurrent.Slot : null;
            if (!mSlots.SelectPreset(iIndex)) return;

            OnSwapped(mSlots.GetCurrent(SlotType.AttackA), true);
            if (aCancel && aCancelSlot.HasValue) SwapCancel(aCancelSlot.Value);
        }

        void OnSwapped(SkillData iShown, bool iUpdateMainWeapon)
        {
            mSwapStrikeUntil = Time.time + mSwapStrikeWindow;
            mSwapStrikeId++;
            SwapChain.OnSwap(Time.time);
            SoundPlayer.Play(Sfx.Swap, 0.35f);
            if (iShown == null) return;
            if (iUpdateMainWeapon) mWeapons.SetMainWeapon(iShown.Weapon);

            // 切り替えた武器種の色で足元を光らせる
            var aWeaponData = mSlots.GetWeaponData(iShown.Weapon);
            if (CombatFeedback.Instance != null && aWeaponData != null)
            {
                if (!CombatFeedback.Instance.PlayCue(EffectCue.WeaponSwap, transform.position, transform.rotation, 1.4f, aWeaponData.Color))
                    CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, 1.4f, aWeaponData.Color);
            }

            TryChangeAttack(iShown.Weapon);
        }

        /// <summary>
        /// チェンジアタック: 切り替えた瞬間、新しい武器で周りを一撃する(クールダウンあり)。
        /// 囲まれたときに切り替えで押し返せるようにする。これ自体はチェインを上げない。
        /// </summary>
        void TryChangeAttack(WeaponType iWeapon)
        {
            if (!ChangeAttackReady) return;
            mChangeAttackReadyAt = Time.time + mChangeAttackCooldown;

            var aData = mSlots.GetWeaponData(iWeapon);
            var aColor = aData != null ? aData.Color : Color.white;
            Vector3 aCenter = transform.position + Vector3.up;
            if (CombatFeedback.Instance != null
                && !CombatFeedback.Instance.PlayCue(EffectCue.ChangeAttack, transform.position, transform.rotation, mChangeAttackRadius, aColor))
                CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, mChangeAttackRadius, aColor);

            int aCount = Physics.OverlapSphereNonAlloc(aCenter, mChangeAttackRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            bool aAnyLanded = false;
            bool aAnyWeak = false;
            mTargetBuffer.Clear();
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aTarget = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                if (aTarget == null || aTarget.Team == mSelf.Team || aTarget.IsDead || mTargetBuffer.Contains(aTarget)) continue;
                mTargetBuffer.Add(aTarget);

                bool aWeak = IsWeak(aTarget, iWeapon);
                float aDamage = mChangeAttackDamage * SwapChain.DamageMultiplier * (aWeak ? mWeaknessDamageMultiplier : 1f);
                float aStagger = mChangeAttackStagger * (aWeak ? mWeaknessStaggerMultiplier : 1f);
                Vector3 aDirection = Flat(aTarget.transform.position - transform.position);
                var aOutcome = aTarget.ApplyHit(new HitInfo
                {
                    Damage = aDamage,
                    Stagger = aStagger,
                    ArmorBreak = mChangeAttackArmorBreak,
                    Knockback = mChangeAttackKnockback,
                    Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward,
                    Source = gameObject,
                    Reaction = 1.3f,
                });
                if (!aOutcome.Landed()) continue;
                aAnyLanded = true;
                aAnyWeak |= aWeak;
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.SpawnDamageNumber(aTarget.transform.position + Vector3.up * 2.2f, aTarget.LastDamage,
                        aWeak ? mWeakColor : aColor);
                }
            }

            if (!aAnyLanded) return;
            mStyle.RegisterHit($"change#{iWeapon}", iWeapon, mChangeAttackStylePoints, mSlots.Bonus.Kind == WeaponBonusKind.Arsenal);
            mStyle.Announce("CHANGE ATTACK", aColor);
            if (aAnyWeak) AnnounceWeak();
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.HitStop(0.05f);
                CombatFeedback.Instance.Shake(0.12f);
            }
        }

        static readonly Color mWeakColor = new Color(1f, 0.55f, 0.1f);

        void SwapCancel(SlotType iSlot)
        {
            bool aContinuing = mLastSwapCancelSlot == iSlot && Time.time - mLastSwapCancelTime < mSwapCancelStreakReset;
            mSwapCancelStreak = aContinuing ? mSwapCancelStreak + 1 : 1;
            mLastSwapCancelSlot = iSlot;
            mLastSwapCancelTime = Time.time;
            EndSkill();
            mStyle.AddBonus(mStyle.Config.SwapCancelBonus, "SWAP CANCEL", new Color(0.4f, 0.9f, 1f));
        }

        /// <summary>このプリセットに切り替えると、実行中の技のスロットの中身が変わってスワップキャンセルになるか</summary>
        public bool CanPresetCancel(int iIndex)
        {
            if (mCurrent == null || mSlots.Mode != SwapMode.Preset || iIndex == mSlots.PresetIndex) return false;
            if (iIndex < 0 || iIndex >= mSlots.Presets.Count) return false;
            if (mSlots.Presets[iIndex].Get(mCurrent.Slot) == mCurrent) return false;
            return CanSwapCancel(mCurrent.Slot);
        }

        bool CanSwapCancel(SlotType iSlot)
        {
            if (mCurrent == null || mIsFinisher || mCurrent.Slot != iSlot || mPhase != Phase.Recovery) return false;

            bool aContinuing = mLastSwapCancelSlot == iSlot && Time.time - mLastSwapCancelTime < mSwapCancelStreakReset;
            int aStreak = aContinuing ? mSwapCancelStreak : 0;
            float aRecovery = FrameTime.ToSeconds(mStep.RecoveryFrames);
            float aRequired = Mathf.Max(
                FrameTime.ToSeconds(mCurrent.SwapCancelFrame),
                aRecovery * Mathf.Min(0.9f, mSwapCancelStreakPenalty * aStreak));
            return mPhaseTime >= aRequired;
        }
    }
}
