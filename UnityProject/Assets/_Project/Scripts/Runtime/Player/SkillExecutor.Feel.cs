using UnityEngine;

namespace BattleFight
{
    /// <summary>SkillExecutor: 振りの見た目と音(振りの音・斬撃の弧・武器の振り・残像)</summary>
    public partial class SkillExecutor
    {
        /// <summary>技を出した瞬間の音。近接は風切り音(重い武器ほど低く)、銃は発砲音と反動</summary>
        void PlayAttackSound()
        {
            var aFeel = WeaponFeel.For(mCurrent.Weapon);
            if (mCurrent.Weapon == WeaponType.Gun)
            {
                SoundPlayer.Play(Sfx.Shot, 0.7f);
                if (aFeel.Recoil > 0f) mMotor.AddImpulse(-transform.forward * aFeel.Recoil);
                return;
            }
            if (!IsSwingAttack(mCurrent))
            {
                // 移動スキルなど: 軽い風切り音だけ
                SoundPlayer.Play(Sfx.Swing, 0.4f, 1.3f);
                return;
            }
            // 武器ごとの振りの音。コンボの段が進むほど、少しずつ高く鋭くする
            float aPitch = 1f + 0.06f * Mathf.Min(mStepIndex, 5);
            SoundPlayer.Play(SfxSynth.SwingFor(mCurrent.Weapon), 0.55f, aPitch);
            SpawnSwingArc();

            // 振り抜き: 振る向きへ体を素早くひねる。大槌は振り下ろしでつぶれる
            if (mBodyPose == null) mBodyPose = GetComponent<PlayerHitReaction>();
            if (mBodyPose != null)
            {
                mBodyPose.Twist(SwingSide() * (mCurrent.Weapon == WeaponType.Hammer ? 12f : 32f));
                if (mCurrent.Weapon == WeaponType.Hammer) mBodyPose.Squash(0.3f);
            }
        }

        /// <summary>振る攻撃か(攻撃A・B の近接。弾・ビーム・遠くへの一撃・移動スキルは振らない)</summary>
        static bool IsSwingAttack(SkillData iSkill) =>
            iSkill.Slot != SlotType.Movement && iSkill.Weapon != WeaponType.Gun && IsMelee(iSkill);

        /// <summary>この段の振る向き(+1: 右から左 / -1: 左から右)。段ごとに入れ替える</summary>
        float SwingSide() => mStepIndex % 2 == 0 ? 1f : -1f;

        /// <summary>斬撃の弧: 武器ごとに太さ・長さ・向きを変える。大槌は縦、ほかは横(段ごとに傾きを変える)</summary>
        void SpawnSwingArc()
        {
            if (CombatFeedback.Instance == null) return;
            var aStep = mStep;
            var aData = mSlots.GetWeaponData(mCurrent.Weapon);
            var aColor = Color.Lerp(aData != null ? aData.Color : Color.white, Color.white, 0.55f);
            float aSide = SwingSide();
            (float Radius, float Arc, float Thickness, bool Vertical, float Tilt) aShape = mCurrent.Weapon switch
            {
                WeaponType.Hammer => (2.0f, 120f, 0.7f, true, 0f),
                WeaponType.Chain => (3.2f, 200f, 0.25f, false, 10f),
                WeaponType.Staff => (1.8f, 140f, 0.3f, false, 25f),
                WeaponType.Gauntlet => (1.3f, 90f, 0.45f, false, 35f),
                _ => (2.2f, 150f, 0.35f, false, 15f + 10f * (mStepIndex % 3)),
            };
            // 広い範囲の技は、範囲に合わせて弧も大きく
            float aRadius = Mathf.Max(aShape.Radius, aStep.HitboxRadius >= 2.5f ? aStep.HitboxRadius : 0f);
            Vector3 aCenter = transform.position + Vector3.up * 1.1f + transform.forward * 0.2f;
            Quaternion aRotation = aShape.Vertical
                ? transform.rotation * Quaternion.Euler(0f, 0f, 90f)
                : transform.rotation * Quaternion.Euler(0f, 0f, aShape.Tilt * aSide);
            CombatFeedback.Instance.SpawnSwingArc(aCenter, aRotation, aRadius, aShape.Arc, aShape.Thickness, aSide, aColor);
        }

        /// <summary>移動スキルで無敵の間は、通ったあとに残像を残す</summary>
        void UpdateAfterimages()
        {
            bool aDodging = mCurrent != null && mCurrent.Slot == SlotType.Movement && mSelf.Invulnerable;
            if (!aDodging || CombatFeedback.Instance == null || Time.unscaledTime < mNextAfterimageTime) return;
            if (mBody == null) mBody = transform.Find("Body");
            mNextAfterimageTime = Time.unscaledTime + mAfterimageInterval;
            CombatFeedback.Instance.SpawnAfterimage(mBody, mAfterimageColor, 0.22f);
        }

        void UpdateWeaponPose()
        {
            // 近接の攻撃判定が出ている間(と振り抜きの少しあと)は、武器の軌跡を描く
            mWeapons.SetTrail(mCurrent != null && IsMelee(mCurrent)
                              && (mPhase == Phase.Active || (mPhase == Phase.Recovery && Progress(mStep.RecoveryFrames) < 0.25f)));
            if (mCurrent == null)
            {
                mWeapons.SetIdle();
                return;
            }

            var aStep = mStep;
            float aNormalized = mPhase switch
            {
                Phase.Startup => SwingCurve.Startup(Progress(aStep.StartupFrames)),
                Phase.Active => SwingCurve.Active(Progress(aStep.ActiveFrames)),
                Phase.Recovery => SwingCurve.Recovery(Progress(aStep.RecoveryFrames)),
                Phase.Charging => -SwingCurve.WindupDepth,
                _ => -0.6f,
            };
            mWeapons.SetSwing(aNormalized, mCurrent.Weapon == WeaponType.Hammer, mStepIndex % 2 == 0 ? 1f : -1f);
        }
    }
}
