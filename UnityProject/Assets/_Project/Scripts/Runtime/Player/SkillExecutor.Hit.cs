using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: 当たったときの処理(ダメージ・弱点・手ごたえ)</summary>
    public partial class SkillExecutor
    {
        static bool IsWeak(Damageable iTarget, WeaponType iWeapon)
        {
            var aEnemy = iTarget.GetComponent<EnemyController>();
            // 型を切り替える敵は、今の型の弱点で判定する
            return aEnemy != null && aEnemy.Profile != null && aEnemy.IsWeakTo(iWeapon);
        }

        void AnnounceWeak()
        {
            if (Time.unscaledTime - mLastWeakAnnounce < 0.6f) return;
            mLastWeakAnnounce = Time.unscaledTime;
            mStyle.AddBonus(mWeaknessStyleBonus, "WEAK!", mWeakColor);
        }

        /// <summary>
        /// 技のヒットをまとめて処理する(近接の判定・弾・爆発のすべてがここを通る)。
        /// ダメージ、武器種ボーナス、スワップストライク、スタイル、演出を反映する。
        /// </summary>
        public void ResolveHits(SkillHitContext iContext, List<Damageable> iTargets, Vector3 iSourcePosition)
        {
            if (iContext.Skill == null || iTargets.Count == 0) return;

            var aSkill = iContext.Skill;
            var aStep = iContext.Step;
            bool aSwapStrike = iContext.SwapStrikeId >= 0 && iContext.SwapStrikeId == mSwapStrikeId && SwapStrikeReady;
            bool aAnyLanded = false;
            bool aStyleRegistered = false;
            float aHitstop = 0f;
            var aBonus = mSlots.Bonus;
            var aWeaponData = mSlots.GetWeaponData(aSkill.Weapon);
            var aFeel = WeaponFeel.For(aSkill.Weapon);
            bool aAnyHeavy = false;
            bool aAnyArmorBroken = false;
            bool aAnyDeflected = false;
            // コンボが続くほど、当たりを大きく見せる。技の締めの一撃はさらに大きく
            float aComboScale = HitEmphasis.ComboScale(Combo.Count);
            bool aFinale = aSkill == mCurrent && iContext.StepIndex == mStepIndex && HitEmphasis.IsFinale(aSkill, iContext.StepIndex, mIsFinisher);
            // 見切り(敵だけが遅い)の間に近接で当てると、反撃として大きく見せる
            bool aPunish = EnemyTime.IsSlowed && IsMelee(aSkill) && aSkill == mCurrent;
            if (aPunish) aFinale = true;
            float aFinaleScale = aFinale ? 1.5f : 1f;
            Vector3 aKickDirection = transform.forward;

            bool aAnyWeak = false;
            int aLanded = 0;
            int aWeakLanded = 0;
            // スタイルランクが高いほど攻撃力が上がる(格好よく戦うほど強い)
            float aRankMultiplier = mStyle.Config.DamageMultiplier(mStyle.Meter.RankIndex);
            foreach (var aTarget in iTargets)
            {
                if (aTarget == null || aTarget.IsDead) continue;

                bool aWeak = IsWeak(aTarget, aSkill.Weapon);
                float aDamage = aStep.Damage * iContext.Multiplier * SwapChain.DamageMultiplier * aRankMultiplier;
                float aStagger = aStep.Stagger * iContext.Multiplier;
                if (aWeak)
                {
                    aDamage *= mWeaknessDamageMultiplier;
                    aStagger *= mWeaknessStaggerMultiplier;
                }
                if (aBonus.Boosts(aSkill.Weapon) && aWeaponData != null) aStagger *= 1f + aWeaponData.SynergyStaggerBonus;
                if (aSwapStrike)
                {
                    aDamage *= mSwapStrikeDamageMultiplier;
                    aStagger *= mSwapStrikeStaggerMultiplier;
                }

                Vector3 aDirection = Flat(aTarget.transform.position - iSourcePosition);
                var aOutcome = aTarget.ApplyHit(new HitInfo
                {
                    Damage = aDamage,
                    Stagger = aStagger,
                    ArmorBreak = aStep.ArmorBreak * iContext.Multiplier,
                    Knockback = aStep.Knockback * aFeel.KnockbackScale * aFinaleScale,
                    Launch = aStep.Launch,
                    Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward,
                    Source = gameObject,
                    Reaction = aFeel.ReactionScale * (aFinale ? 1.3f : 1f),
                });
                if (!aOutcome.Landed()) continue;

                aAnyLanded = true;
                aAnyWeak |= aWeak;
                aLanded++;
                if (aWeak) aWeakLanded++;
                // ヒットストップは、当てた敵だけを止める(ほかの敵・弾は動き続ける)
                float aTargetHitstop = aFeel.Hitstop(aStep.Hitstop * aComboScale * aFinaleScale, aOutcome == HitOutcome.ArmorBroken);
                if (aDirection.sqrMagnitude > 1e-4f) aKickDirection = aDirection;
                aHitstop = Mathf.Max(aHitstop, aTargetHitstop);
                var aEnemy = aTarget.GetComponent<EnemyController>();
                if (aEnemy != null) aEnemy.Freeze(aTargetHitstop);
                aAnyArmorBroken |= aOutcome == HitOutcome.ArmorBroken;

                // 近接の1段は1回だけ登録する。同じ段で複数の敵に当てた分は少しだけ加点
                bool aFirstForStep = iContext.Skill == mCurrent && iContext.StepIndex == mStepIndex ? !mStyleRegisteredThisStep : !aStyleRegistered;
                if (aFirstForStep)
                {
                    if (iContext.Skill == mCurrent && iContext.StepIndex == mStepIndex) mStyleRegisteredThisStep = true;
                    aStyleRegistered = true;
                    mStyle.RegisterHit($"{aSkill.name}#{iContext.StepIndex}", aSkill.Weapon, aStep.StylePoints * iContext.Multiplier,
                        aBonus.Kind == WeaponBonusKind.Arsenal);
                }
                else
                {
                    mStyle.AddBonus(aStep.StylePoints * 0.3f);
                }

                if (aOutcome == HitOutcome.ArmorBroken) mStyle.Announce("ARMOR BREAK", new Color(1f, 0.6f, 0.2f));

                if (CombatFeedback.Instance != null)
                {
                    var aColor = aSwapStrike ? new Color(1f, 0.9f, 0.2f)
                        : aWeak ? mWeakColor
                        : aOutcome == HitOutcome.Armored ? new Color(0.7f, 0.7f, 0.7f)
                        : Color.white;
                    // 火花(弱点・アーマー破壊・重い一撃は多く)。重い一撃は画面も大きく揺らす
                    bool aHeavy = aTarget.LastDamage >= 25f || aOutcome == HitOutcome.ArmorBroken;
                    aAnyHeavy |= aHeavy;
                    float aNumberScale = HitEmphasis.DamageNumberScale(aWeak, aSwapStrike, aOutcome == HitOutcome.ArmorBroken, aHeavy, aFinale);
                    CombatFeedback.Instance.SpawnDamageNumber(aTarget.transform.position + Vector3.up * 2.2f, aTarget.LastDamage, aColor, aNumberScale);
                    var aSparkColor = aWeak || aSwapStrike ? aColor : aWeaponData != null ? Color.Lerp(aWeaponData.Color, Color.white, 0.4f) : Color.white;
                    var aHitPoint = Vector3.Lerp(aTarget.transform.position + Vector3.up * 1.1f, iSourcePosition + Vector3.up * 1.1f, 0.25f);
                    int aSparks = Mathf.RoundToInt(((aHeavy || aWeak ? 9 : 5) + aFeel.ExtraSparks) * aComboScale * aFinaleScale);
                    CombatFeedback.Instance.SpawnSparks(aHitPoint, aSparkColor, aSparks, aFeel.SparkSpeed * (aHeavy ? 1.25f : 1f));
                    CombatFeedback.Instance.SpawnImpact(aFeel.Style, aHitPoint, aTarget.transform.position, aDirection, aSparkColor, aHeavy || aFinale);
                    if (aHeavy) CombatFeedback.Instance.Shake(0.22f * aFeel.ShakeScale);
                    if (aOutcome == HitOutcome.Armored) aAnyDeflected = true;
                    // アーマーが砕けた: 灰色の大きな欠片が飛び散る
                    if (aOutcome == HitOutcome.ArmorBroken) CombatFeedback.Instance.SpawnShards(aHitPoint, new Color(0.55f, 0.56f, 0.62f), 10, 0.28f);
                }
            }

            if (!aAnyLanded) return;
            Combo.AddHit(Time.time, aLanded);
            Drive.AddFromAttack(aLanded, aWeakLanded, aSwapStrike);
            if (aSwapStrike)
            {
                mSwapStrikeUntil = -1f;
                mStyle.AddBonus(mStyle.Config.SwapStrikeBonus, "SWAP STRIKE!", new Color(1f, 0.9f, 0.2f));
            }
            if (aAnyWeak) AnnounceWeak();
            if (SwapChain.OnHit(Time.time))
            {
                mStyle.AddBonus(mChainStyleBonusPerStep * SwapChain.Chain, $"CHAIN ×{SwapChain.Chain}", new Color(0.55f, 1f, 0.9f));
            }
            // 近接で当てたときは、プレイヤーも一緒に止まる(飛び道具は撃った本人は止まらない)。止まり終わったら少し踏み込む
            bool aMelee = IsMelee(aSkill) && aSkill == mCurrent;
            if (aMelee) mFrozenUntil = Mathf.Max(mFrozenUntil, Time.unscaledTime + aHitstop);
            if (aMelee && !mLungedThisStep && aFeel.Lunge > 0f)
            {
                mLungedThisStep = true;
                mMotor.AddImpulse(transform.forward * aFeel.Lunge);
            }

            // いちばん強い当たり(アーマー破壊・技の締め・大槌の重い一撃)は、画面を光らせてズームする
            bool aStrong = aAnyArmorBroken || aFinale || (aAnyHeavy && aSkill.Weapon == WeaponType.Hammer);
            var aWeaponColor = aWeaponData != null ? aWeaponData.Color : Color.white;
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.Shake((0.05f + aHitstop) * aFeel.ShakeScale * aComboScale);
                CombatFeedback.Instance.Kick(aKickDirection, 0.08f * aFeel.ShakeScale * (aAnyHeavy ? 2f : 1f) * aFinaleScale);
                if (aStrong)
                {
                    CombatFeedback.Instance.Flash(aAnyArmorBroken ? 0.6f : 0.45f);
                    CombatFeedback.Instance.Punch(5f);
                    CombatFeedback.Instance.Vignette(aWeaponColor, 0.7f);
                }
                else if (aAnyHeavy)
                {
                    CombatFeedback.Instance.Punch(2f);
                    CombatFeedback.Instance.Vignette(aWeaponColor, 0.35f);
                }
            }
            if (aStrong) Rumble.Play(0.8f, 0.6f, 0.25f);
            else if (aAnyHeavy) Rumble.Play(0.5f, 0.4f, 0.15f);
            else Rumble.Play(0.15f, 0.35f, 0.07f);

            SoundPlayer.Play(SfxSynth.HitFor(aSkill.Weapon), aAnyHeavy ? 1f : 0.8f, aAnyHeavy ? 0.9f : 1f);
            if (aAnyWeak) SoundPlayer.Play(Sfx.Weak, 0.5f);
            if (aAnyArmorBroken) SoundPlayer.Play(Sfx.ArmorBreak, 0.9f);
            // アーマーに弾かれた(割れていない): 金属音がして、近接ならプレイヤーが少し押し戻される
            if (aAnyDeflected && !aAnyArmorBroken)
            {
                SoundPlayer.Play(Sfx.Guard, 0.7f, 0.8f);
                if (aMelee) mMotor.AddImpulse(-transform.forward * 1.5f);
                if (CombatFeedback.Instance != null) CombatFeedback.Instance.Kick(-aKickDirection, 0.05f);
            }
        }

        static bool IsMelee(SkillData iSkill) =>
            iSkill.Behavior is not (SkillBehavior.Projectile or SkillBehavior.Beam or SkillBehavior.TargetedStrike);
    }
}
