using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: コンボの区切り・チェンジドライブ</summary>
    public partial class SkillExecutor
    {
        /// <summary>コンボが時間切れで途切れたら、数に応じてスタイルのボーナス</summary>
        void TickCombo()
        {
            int aFinished = Combo.Tick(Time.time);
            if (aFinished < 10) return;
            mStyle.AddBonus(aFinished * mStyle.Config.ComboBonusPerHit, $"{aFinished} HITS  {ComboCounter.Rating(aFinished)}", new Color(1f, 0.85f, 0.4f));
        }

        /// <summary>
        /// チェンジドライブ: 各プリセットの攻撃A の武器に次々と持ち替え、周りの敵をまとめて攻撃する。
        /// 武器種が違う攻撃が続くので、どの敵にも弱点が入りやすい。出している間は無敵。
        /// </summary>
        System.Collections.IEnumerator ChangeDrive()
        {
            if (!Drive.TryConsume()) yield break;
            IsDriving = true;
            if (mCurrent != null) EndSkill();
            mStyle.AddBonus(100f, "CHANGE DRIVE", new Color(0.9f, 0.95f, 1f));
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SlowMotion(0.5f, 1.4f);
                CombatFeedback.Instance.Shake(0.3f);
            }

            foreach (var aSkill in DriveSkills())
            {
                yield return new WaitForSeconds(mDriveInterval);
                if (mSelf.IsDead) break;
                mWeapons.SetMainWeapon(aSkill.Weapon);
                var aData = mSlots.GetWeaponData(aSkill.Weapon);
                var aColor = aData != null ? aData.Color : Color.white;
                if (CombatFeedback.Instance != null
                    && !CombatFeedback.Instance.PlayCue(EffectCue.ChangeDrive, transform.position, transform.rotation, mDriveRadius, aColor))
                {
                    CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, mDriveRadius, aColor);
                    CombatFeedback.Instance.SpawnSparks(transform.position + Vector3.up, aColor, 14, 11f);
                }

                int aCount = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, mDriveRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
                mTargetBuffer.Clear();
                for (int aIndex = 0; aIndex < aCount; aIndex++)
                {
                    var aTarget = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                    if (aTarget == null || aTarget.Team == mSelf.Team || aTarget.IsDead || mTargetBuffer.Contains(aTarget)) continue;
                    mTargetBuffer.Add(aTarget);
                }
                // 持ち替えるたびにチェインがつながる
                SwapChain.OnSwap(Time.time);
                if (mTargetBuffer.Count > 0)
                {
                    ResolveHits(new SkillHitContext { Skill = aSkill, StepIndex = 0, Multiplier = mDriveMultiplier, SwapStrikeId = -1 },
                        new List<Damageable>(mTargetBuffer), transform.position);
                }
            }

            yield return new WaitForSeconds(mDriveInterval);
            var aMain = mSlots.GetCurrent(SlotType.AttackA);
            if (aMain != null) mWeapons.SetMainWeapon(aMain.Weapon);
            mExtraInvulnerableUntil = Time.time + 0.4f;
            IsDriving = false;
        }

        /// <summary>チェンジドライブで使う技: プリセット方式は各プリセットの攻撃A、ラック方式は攻撃A のラック</summary>
        List<SkillData> DriveSkills()
        {
            var aSkills = new List<SkillData>();
            if (mSlots.Mode == SwapMode.Preset)
            {
                foreach (var aPreset in mSlots.Presets)
                {
                    if (aPreset.AttackA != null) aSkills.Add(aPreset.AttackA);
                }
            }
            else
            {
                foreach (var aSkill in mSlots.GetRack(SlotType.AttackA).Skills)
                {
                    if (aSkill != null) aSkills.Add(aSkill);
                }
            }
            if (aSkills.Count == 0 && mSlots.GetCurrent(SlotType.AttackA) != null) aSkills.Add(mSlots.GetCurrent(SlotType.AttackA));
            return aSkills;
        }
    }
}
