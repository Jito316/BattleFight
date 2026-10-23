using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 敵の簡易AI。追跡 → 予備動作(色で予告) → 攻撃 → 硬直 を繰り返す。
    /// 同時に攻撃してくる敵の数はトークンで制限する(ボスは例外)。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Damageable))]
    public class EnemyController : MonoBehaviour
    {
        enum State
        {
            Chase,
            Windup,
            Attack,
            Recovery,
            Stagger,
            Pulled,
            Dead,
        }

        const int mMaxAttackTokens = 2;
        const float mFlashDuration = 0.08f;
        const float mDeathDuration = 0.5f;
        /// <summary>のけぞり・つぶれが元に戻るまでの時間</summary>
        const float mReactionDuration = 0.5f;

        /// <summary>見た目の部品をまとめる子の名前(EnemyFactory が作る)</summary>
        public const string VisualName = "Visual";

        public static readonly List<EnemyController> Active = new List<EnemyController>();
        static int mAttackTokensInUse;
        static readonly Collider[] mOverlapBuffer = new Collider[16];

        [SerializeField, FormerlySerializedAs("profile")] EnemyProfile mProfile;
        [SerializeField, FormerlySerializedAs("gravity")] float mGravity = -25f;
        [SerializeField, Tooltip("打ち上げられている間の重力(空中コンボ用に弱め)"), FormerlySerializedAs("juggleGravity")] float mJuggleGravity = -11f;

        CharacterController mController;
        Damageable mDamageable;
        Transform mTarget;
        Renderer[] mRenderers;
        Color[] mBaseColors;

        State mState;
        float mStateTime;
        float mStateDuration;
        EnemyAttack mAttack;
        float mCooldown;
        float mVerticalVelocity;
        Vector3 mKnockback;
        bool mGrounded;
        bool mJuggled;
        bool mHasToken;
        bool mTeleported;
        int mRepeatsLeft;
        readonly List<EnemyAttack> mAvailableAttacks = new List<EnemyAttack>();
        bool mAttackLanded;
        float mFlashTimer;
        float mFrozenUntil = -1f;
        Transform mVisual;
        Vector3 mReactionDirection;
        float mReactionStrength;
        float mReactionTime;
        float mStrafeSign = 1f;
        Vector3 mSlamDestination;
        Vector3 mAttackDirection;
        Vector3 mPullDestination;
        float mPullSpeed;

        // プレイヤーのスキル: 型ごとに変換した攻撃、今の型、次に切り替えるまでの時間
        readonly List<List<EnemyAttack>> mSkillAttacks = new List<List<EnemyAttack>>();
        int mSkillSetIndex;
        float mSwapTimer;
        Renderer mWeaponRenderer;
        int mWeaponRendererIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            mAttackTokensInUse = 0;
            PhaseChanged = null;
        }

        public EnemyProfile Profile => mProfile;
        public Damageable Damageable => mDamageable;
        public bool IsDead => mDamageable == null || mDamageable.IsDead;
        /// <summary>ヒットストップで止まっている(当てられた敵だけが止まる)</summary>
        public bool IsFrozen => Time.unscaledTime < mFrozenUntil;
        /// <summary>のけぞり・つぶれの途中か(テスト用)</summary>
        public bool IsReacting => mReactionStrength > 0f;
        public bool IsHeavy => mProfile != null && mProfile.Heavy;
        public bool IsBeingPulled => mState == State.Pulled;
        public bool IsTelegraphing => mState == State.Windup;
        public bool IsPhase2 { get; private set; }
        float mMoveSpeed => mProfile.MoveSpeed * (IsPhase2 ? mProfile.Phase2SpeedMultiplier : 1f);

        /// <summary>ボスが第二形態になった(HUD の告知用)</summary>
        public static event System.Action<EnemyController> PhaseChanged;
        public float Height => mController.height * transform.lossyScale.y;

        /// <summary>今の型(プレイヤーのスキルを使わない敵は null)</summary>
        public EnemySkillSet CurrentSkillSet => mProfile != null && mProfile.SkillSets != null && mProfile.SkillSets.Count > 0
            ? mProfile.SkillSets[Mathf.Clamp(mSkillSetIndex, 0, mProfile.SkillSets.Count - 1)]
            : null;

        /// <summary>最後に型を切り替えた時刻(HUD の演出用)</summary>
        public float SkillSetChangedAt { get; private set; } = -99f;

        /// <summary>予備動作中・攻撃中の攻撃(それ以外は null)</summary>
        public EnemyAttack CurrentAttack => mState == State.Windup || mState == State.Attack ? mAttack : null;
        /// <summary>予備動作の進み具合(0 〜 1)</summary>
        public float WindupProgress => mState == State.Windup && mAttack != null && mAttack.Windup > 0f ? Mathf.Clamp01(mStateTime / mAttack.Windup) : 0f;
        /// <summary>攻撃が出るまでの残り(実際の秒。見切りで敵が遅いときは長くなる)</summary>
        public float WindupRealTimeLeft => mState == State.Windup && mAttack != null
            ? Mathf.Max(0f, mAttack.Windup - mStateTime) / Mathf.Max(0.01f, EnemyTime.Scale) : 0f;
        /// <summary>飛びかかりの着地点</summary>
        public Vector3 SlamDestination => mSlamDestination;
        /// <summary>狙っている相手(プレイヤー)</summary>
        public Transform Target => mTarget;
        /// <summary>予備動作を見せるコンポーネント(テスト用)</summary>
        public EnemyTelegraph Telegraph { get; private set; }

        /// <summary>今の弱点。型が弱点を持っていればそれ、なければ敵の弱点</summary>
        public WeaponType[] Weaknesses
        {
            get
            {
                var aSet = CurrentSkillSet;
                if (aSet != null && aSet.Weaknesses != null && aSet.Weaknesses.Length > 0) return aSet.Weaknesses;
                return mProfile != null && mProfile.Weaknesses != null ? mProfile.Weaknesses : System.Array.Empty<WeaponType>();
            }
        }

        public bool IsWeakTo(WeaponType iWeapon)
        {
            foreach (var aWeapon in Weaknesses)
            {
                if (aWeapon == iWeapon) return true;
            }
            return false;
        }
        public float Radius => mController.radius * transform.lossyScale.x;
        public Vector3 CenterPoint => transform.position + Vector3.up * (Height * 0.5f);

        void Awake()
        {
            mController = GetComponent<CharacterController>();
            mDamageable = GetComponent<Damageable>();
        }

        void OnEnable()
        {
            Active.Add(this);
            mDamageable.Damaged += OnDamaged;
            mDamageable.Died += OnDied;
        }

        void OnDisable()
        {
            Active.Remove(this);
            ReleaseToken();
            mDamageable.Damaged -= OnDamaged;
            mDamageable.Died -= OnDied;
        }

        public void Initialize(EnemyProfile iNewProfile, Transform iPlayer)
        {
            mProfile = iNewProfile;
            mTarget = iPlayer;
            mVisual = transform.Find(VisualName);
            if (Telegraph == null)
            {
                Telegraph = gameObject.AddComponent<EnemyTelegraph>();
                Telegraph.Setup(this, CombatFeedback.Instance != null ? CombatFeedback.Instance.EffectMaterial : null);
            }
            mRenderers = GetComponentsInChildren<Renderer>();
            mBaseColors = new Color[mRenderers.Length];
            for (int aRendererIndex = 0; aRendererIndex < mRenderers.Length; aRendererIndex++) mBaseColors[aRendererIndex] = mRenderers[aRendererIndex].material.color;
            mCooldown = Random.Range(0.6f, 1.6f);
            mStrafeSign = Random.value < 0.5f ? -1f : 1f;
            mState = State.Chase;

            mSkillAttacks.Clear();
            if (mProfile.SkillSets != null)
            {
                foreach (var aSet in mProfile.SkillSets)
                {
                    mSkillAttacks.Add(EnemySkillConverter.ToAttacks(aSet.Skills, mProfile.SkillDamageScale, mProfile.SkillWindupScale));
                }
            }
            mSkillSetIndex = 0;
            mSwapTimer = mProfile.SkillSwapInterval;
            // 手に持つ武器(EnemyFactory が付ける)は、型の色で塗る
            var aWeapon = mVisual != null ? mVisual.Find("Weapon") : transform.Find("Weapon");
            mWeaponRenderer = aWeapon != null ? aWeapon.GetComponent<Renderer>() : null;
            mWeaponRendererIndex = mWeaponRenderer != null ? System.Array.IndexOf(mRenderers, mWeaponRenderer) : -1;
            ApplySkillSetColor();
        }

        /// <summary>次の型に切り替える(プレイヤーのプリセットの切り替えと同じ)。弱点も変わる</summary>
        public bool SwapSkillSet()
        {
            if (mProfile.SkillSets == null || mProfile.SkillSets.Count < 2 || IsDead) return false;
            mSkillSetIndex = (mSkillSetIndex + 1) % mProfile.SkillSets.Count;
            mSwapTimer = mProfile.SkillSwapInterval;
            SkillSetChangedAt = Time.time;
            if (mState == State.Chase) mAttack = null;
            ApplySkillSetColor();
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 1.2f, CurrentSkillSet.Color);
            return true;
        }

        void ApplySkillSetColor()
        {
            var aSet = CurrentSkillSet;
            if (aSet == null || mWeaponRenderer == null) return;
            if (mWeaponRendererIndex >= 0) mBaseColors[mWeaponRendererIndex] = aSet.Color;
            mWeaponRenderer.material.color = aSet.Color;
        }

        void Update()
        {
            if (mProfile == null) return;
            // 「見切り」(ジャスト回避)の間は敵だけ遅くなる。ヒットストップ中は止まる
            bool aFrozen = IsFrozen;
            float aDeltaTime = aFrozen ? 0f : EnemyTime.DeltaTime;
            UpdateReaction(aFrozen);

            if (mState == State.Dead)
            {
                mStateTime += aDeltaTime;
                transform.localScale = Vector3.one * (mProfile.Scale * Mathf.Max(0f, 1f - mStateTime / mDeathDuration));
                if (mStateTime >= mDeathDuration) Destroy(gameObject);
                return;
            }

            mStateTime += aDeltaTime;
            mCooldown -= aDeltaTime;

            // 型の切り替え(攻撃していないときだけ)
            if (mProfile.SkillSets != null && mProfile.SkillSets.Count > 1 && mProfile.SkillSwapInterval > 0f)
            {
                mSwapTimer -= aDeltaTime;
                if (mSwapTimer <= 0f && mState == State.Chase) SwapSkillSet();
            }

            Vector3 aPlanar = Vector3.zero;
            switch (mState)
            {
                case State.Chase:
                    aPlanar = TickChase(aDeltaTime);
                    break;
                case State.Windup:
                    aPlanar = TickWindup(aDeltaTime);
                    break;
                case State.Attack:
                    aPlanar = TickAttack();
                    break;
                case State.Recovery:
                    if (mStateTime >= mAttack.Recovery) EnterChase();
                    break;
                case State.Stagger:
                    if (mStateTime >= mStateDuration && (mGrounded || mProfile.Flying))
                    {
                        mJuggled = false;
                        EnterChase();
                    }
                    break;
                case State.Pulled:
                    aPlanar = TickPulled(aDeltaTime);
                    break;
            }

            // 自爆などでこのフレームに倒れた
            if (mState == State.Dead) return;

            if (mProfile.Flying && !mJuggled && mState != State.Pulled)
            {
                // 地面から一定の高さに浮く。急降下中は高さを保たない
                float aTargetY = GroundHeight() + mProfile.HoverHeight;
                mVerticalVelocity = mState == State.Attack ? 0f : Mathf.Clamp((aTargetY - transform.position.y) * 3f, -6f, 6f);
            }
            else if (mState != State.Pulled)
            {
                mVerticalVelocity += (mJuggled ? mJuggleGravity : mGravity) * aDeltaTime;
            }
            mKnockback = Vector3.Lerp(mKnockback, Vector3.zero, 1f - Mathf.Exp(-6f * aDeltaTime));

            var aFlags = mController.Move((aPlanar + mKnockback + Vector3.up * mVerticalVelocity) * aDeltaTime);
            mGrounded = (aFlags & CollisionFlags.Below) != 0;
            if (mGrounded && mVerticalVelocity < 0f)
            {
                mVerticalVelocity = -1f;
                mJuggled = false;
            }

            UpdateColor(aDeltaTime);
        }

        // ---------- 状態 ----------

        Vector3 TickChase(float iDeltaTime)
        {
            if (mTarget == null) return Vector3.zero;

            Vector3 aOffset = Flat(mTarget.position - transform.position);
            float aDistance = aOffset.magnitude;
            if (mAttack == null) mAttack = PickAttack();
            Face(aOffset, iDeltaTime);

            // 攻撃を持たない敵(訓練用の人形など)は向きを変えるだけ
            if (mAttack == null) return Separation() * mMoveSpeed * 0.6f;

            if (mCooldown <= 0f && aDistance <= mAttack.Range && TryTakeToken())
            {
                EnterWindup();
                return Vector3.zero;
            }

            Vector3 aDirection = aDistance > 0.01f ? aOffset / aDistance : transform.forward;
            Vector3 aMove;
            if (mProfile.PreferredDistance > 0f && aDistance < mProfile.PreferredDistance * 0.7f)
                aMove = -aDirection * (mMoveSpeed * 0.8f);
            else if (aDistance > mAttack.Range * 0.85f)
                aMove = aDirection * mMoveSpeed;
            else if (!mHasToken)
                aMove = Vector3.Cross(Vector3.up, aDirection) * (mStrafeSign * mMoveSpeed * 0.4f);
            else
                aMove = Vector3.zero;

            return aMove + Separation() * mMoveSpeed * 0.6f;
        }

        void EnterWindup()
        {
            mState = State.Windup;
            mStateTime = 0f;
            // 攻撃が来ることを音でも知らせる(重い攻撃ほど低い)
            SoundPlayer.Play(Sfx.EnemyWindup, mProfile.IsBoss ? 0.5f : 0.3f, mAttack.Damage >= 20f || mAttack.JumpSlam ? 0.75f : 1.1f);
            mAttackLanded = false;
            mTeleported = false;
            mRepeatsLeft = mAttack.Repeat;
            if (mAttack.JumpSlam)
            {
                mVerticalVelocity = 12f;
                mSlamDestination = mTarget.position;
            }
        }

        /// <summary>連続攻撃の2発目以降。予備動作を短くしてもう一度出す</summary>
        void EnterRepeatWindup()
        {
            mRepeatsLeft--;
            mState = State.Windup;
            mStateTime = Mathf.Max(0f, mAttack.Windup - mAttack.RepeatWindup);
            mAttackLanded = false;
        }

        /// <summary>予備動作の途中で消え、プレイヤーの背後に現れる(残りの予備動作は背後で見せる)</summary>
        void TeleportBehindTarget()
        {
            mTeleported = true;
            if (mTarget == null) return;
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 1f, mProfile.TelegraphColor);

            Vector3 aBehind = mTarget.position - Flat(mTarget.forward).normalized * (Radius + 1.8f);
            // アリーナの外に出ないようにする
            Vector3 aFlat = Flat(aBehind);
            if (aFlat.magnitude > 27f) aBehind = aFlat.normalized * 27f + Vector3.up * aBehind.y;
            aBehind.y = mTarget.position.y + 0.1f;

            mController.enabled = false;
            transform.position = aBehind;
            mController.enabled = true;
            Vector3 aLook = Flat(mTarget.position - transform.position);
            if (aLook.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(aLook);
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 1f, mProfile.TelegraphColor);
        }

        Vector3 TickWindup(float iDeltaTime)
        {
            if (mAttack.TeleportBehind && !mTeleported && mStateTime >= mAttack.Windup * 0.5f) TeleportBehindTarget();
            if (mStateTime < mAttack.Windup * 0.6f && mTarget != null) Face(Flat(mTarget.position - transform.position), iDeltaTime);

            Vector3 aPlanar = Vector3.zero;
            if (mAttack.JumpSlam)
            {
                Vector3 aOffset = Flat(mSlamDestination - transform.position);
                float aRemaining = Mathf.Max(0.05f, mAttack.Windup - mStateTime);
                aPlanar = Vector3.ClampMagnitude(aOffset / aRemaining, 20f);
            }

            if (mStateTime >= mAttack.Windup && (!mAttack.JumpSlam || mGrounded))
            {
                mState = State.Attack;
                mStateTime = 0f;
                OnAttackStart();
            }
            return aPlanar;
        }

        void OnAttackStart()
        {
            if (!mAttack.Ranged) SoundPlayer.Play(Sfx.Swing, 0.4f, mAttack.Damage >= 20f ? 0.55f : 0.75f);
            // 飛んでいる敵はプレイヤーへ向かって斜めに急降下する
            mAttackDirection = transform.forward;
            if (mProfile.Flying && mTarget != null)
            {
                Vector3 aOffset = mTarget.position + Vector3.up - CenterPoint;
                if (aOffset.sqrMagnitude > 0.01f) mAttackDirection = aOffset.normalized;
            }

            if (mAttack.Ranged)
            {
                FireProjectiles();
                mAttackLanded = true;
            }
            else if (mAttack.TargetedStrike)
            {
                if (mTarget != null)
                {
                    EnemyProjectile.Spawn(gameObject, mTarget.position + Vector3.up * 0.05f, Vector3.zero, mAttack.Radius, mAttack.Damage,
                        mAttack.Knockback, mProfile.TelegraphColor, iDelay: mAttack.StrikeDelay);
                }
                mAttackLanded = true;
            }
            else if (mAttack.Summon != null && mAttack.SummonCount > 0)
            {
                for (int aIndex = 0; aIndex < mAttack.SummonCount; aIndex++)
                {
                    float aAngle = 360f * aIndex / mAttack.SummonCount;
                    Vector3 aOffset = Quaternion.Euler(0f, aAngle, 0f) * Vector3.forward * (Radius + 2f);
                    if (ArenaDirector.Instance != null) ArenaDirector.Instance.SpawnExtra(mAttack.Summon, transform.position + aOffset);
                }
                if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 2f, mProfile.TelegraphColor);
                mAttackLanded = true;
            }

            if (mAttack.JumpSlam || mAttack.SelfDestruct || mAttack.Radius * mProfile.Scale >= 3f)
            {
                var aCenter = transform.TransformPoint(mAttack.Offset);
                if (CombatFeedback.Instance != null && !mAttack.Ranged && !mAttack.TargetedStrike)
                {
                    CombatFeedback.Instance.SpawnShockwave(new Vector3(aCenter.x, transform.position.y, aCenter.z),
                        mAttack.Radius * mProfile.Scale, mProfile.TelegraphColor);
                    CombatFeedback.Instance.Shake(0.2f);
                }
            }
        }

        void FireProjectiles()
        {
            if (mTarget == null) return;
            Vector3 aOrigin = CenterPoint + transform.forward * (Radius + 0.3f);
            Vector3 aAim = mTarget.position + Vector3.up * 1.1f - aOrigin;
            aAim = aAim.sqrMagnitude > 0.01f ? aAim.normalized : transform.forward;
            int aCount = Mathf.Max(1, mAttack.ProjectileCount);
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                float aAngle = aCount == 1 ? 0f : Mathf.Lerp(-mAttack.ProjectileSpread * 0.5f, mAttack.ProjectileSpread * 0.5f, aIndex / (aCount - 1f));
                Vector3 aDirection = Quaternion.AngleAxis(aAngle, Vector3.up) * aAim;
                EnemyProjectile.Spawn(gameObject, aOrigin, aDirection * mAttack.ProjectileSpeed, mAttack.ProjectileRadius, mAttack.Damage,
                    mAttack.Knockback, mProfile.TelegraphColor);
            }
        }

        Vector3 TickAttack()
        {
            DoAttackHit();
            if (mAttack.SelfDestruct && !IsDead)
            {
                mDamageable.Kill();
                return Vector3.zero;
            }

            Vector3 aPlanar = mAttack.Lunge > 0f && mAttack.Active > 0f
                ? mAttackDirection * (mAttack.Lunge / mAttack.Active)
                : Vector3.zero;

            if (mStateTime >= mAttack.Active)
            {
                if (mRepeatsLeft > 0 && !IsDead)
                {
                    EnterRepeatWindup();
                    return aPlanar;
                }
                mState = State.Recovery;
                mStateTime = 0f;
                ReleaseToken();
            }
            return aPlanar;
        }

        void DoAttackHit()
        {
            if (mAttackLanded) return;
            Vector3 aCenter = transform.TransformPoint(mAttack.Offset);
            float aRadius = mAttack.Radius * mProfile.Scale;
            int aCount = Physics.OverlapSphereNonAlloc(aCenter, aRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aVictim = mOverlapBuffer[aIndex].GetComponentInParent<Damageable>();
                if (aVictim == null || aVictim.Team != Team.Player || aVictim.IsDead) continue;

                Vector3 aDirection = Flat(aVictim.transform.position - transform.position);
                aVictim.ApplyHit(new HitInfo
                {
                    Damage = mAttack.Damage,
                    Stagger = 100f,
                    Knockback = mAttack.Knockback,
                    Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward,
                    Source = gameObject,
                });
                mAttackLanded = true;
                return;
            }
        }

        void EnterChase()
        {
            mState = State.Chase;
            mStateTime = 0f;
            mAttack = null;
            mCooldown = mProfile.AttackCooldown * Random.Range(0.7f, 1.3f) * (IsPhase2 ? mProfile.Phase2CooldownMultiplier : 1f);
        }

        void EnterStagger(float iDuration)
        {
            ReleaseToken();
            mAttack = null;
            mState = State.Stagger;
            mStateTime = 0f;
            mStateDuration = iDuration;
        }

        public void BeginPull(Vector3 iDestination, float iSpeed)
        {
            if (IsDead) return;
            ReleaseToken();
            mAttack = null;
            mState = State.Pulled;
            mStateTime = 0f;
            mPullDestination = iDestination;
            mPullSpeed = iSpeed;
            mVerticalVelocity = 0f;
        }

        Vector3 TickPulled(float iDeltaTime)
        {
            mVerticalVelocity = 0f;
            Vector3 aOffset = mPullDestination - transform.position;
            aOffset.y = 0f;
            if (aOffset.magnitude < 0.3f || mStateTime > 0.6f)
            {
                EnterStagger(0.7f);
                return Vector3.zero;
            }
            return aOffset.normalized * Mathf.Min(mPullSpeed, aOffset.magnitude / Mathf.Max(iDeltaTime, 1e-4f));
        }

        // ---------- 被弾 ----------

        void OnDamaged(HitInfo iHit, HitOutcome iOutcome)
        {
            if (mState == State.Dead) return;
            mFlashTimer = mFlashDuration;
            float aReaction = iHit.Reaction > 0f ? iHit.Reaction : 1f;
            if (iOutcome == HitOutcome.Armored) aReaction *= 0.35f;
            if (mProfile.IsBoss) aReaction *= 0.5f;
            React(iHit.Direction, aReaction);

            float aKnockScale = iOutcome == HitOutcome.Armored ? 0.15f : 1f;
            if (mProfile.IsBoss) aKnockScale *= 0.3f;
            mKnockback += iHit.Direction * (iHit.Knockback * aKnockScale);

            // 第二形態になったときは、そのひるみ(長め)を普通のひるみで上書きしない
            if (iOutcome != HitOutcome.Killed && CheckPhase2()) return;

            switch (iOutcome)
            {
                case HitOutcome.Killed:
                    return;
                case HitOutcome.ArmorBroken:
                    EnterStagger(mProfile.ArmorBreakStagger);
                    break;
                case HitOutcome.Armored:
                    return;
                default:
                    if (iHit.Stagger >= mProfile.StaggerThreshold)
                    {
                        EnterStagger(mProfile.StaggerDuration);
                        if (iHit.Launch > 0f && !mProfile.IsBoss)
                        {
                            mVerticalVelocity = iHit.Launch;
                            mJuggled = true;
                        }
                    }
                    break;
            }

            // 空中で攻撃を受けている間は落ちにくくする
            if (mJuggled && !mGrounded) mVerticalVelocity = Mathf.Max(mVerticalVelocity, 2.5f);
        }

        void OnDied()
        {
            ReleaseToken();
            mState = State.Dead;
            mStateTime = 0f;
            mController.enabled = false;
            Active.Remove(this);
            SoundPlayer.Play(Sfx.Defeat, mProfile.IsBoss ? 1f : 0.7f, mProfile.IsBoss ? 0.8f : 1f);
            // 砕け散って、刻片がプレイヤーへ飛んでいく
            if (CombatFeedback.Instance != null)
            {
                int aShards = Mathf.Clamp(Mathf.RoundToInt(mProfile.ShardValue * 3f), 2, 12);
                CombatFeedback.Instance.SpawnDefeatBurst(CenterPoint, mProfile.Color, Mathf.Max(0.6f, mProfile.Scale), mTarget, aShards);
                CombatFeedback.Instance.Shake(mProfile.IsBoss ? 0.45f : 0.12f);
            }
        }

        // ---------- 補助 ----------

        float GroundHeight()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var aHit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                return aHit.point.y;
            }
            return 0f;
        }

        /// <summary>今使える攻撃(第二形態では増える)</summary>
        List<EnemyAttack> AvailableAttacks()
        {
            mAvailableAttacks.Clear();
            if (mProfile.Attacks != null) mAvailableAttacks.AddRange(mProfile.Attacks);
            if (IsPhase2 && mProfile.Phase2Attacks != null) mAvailableAttacks.AddRange(mProfile.Phase2Attacks);
            // 今の型のスキル(プレイヤーと同じ技)
            if (mSkillAttacks.Count > 0) mAvailableAttacks.AddRange(mSkillAttacks[Mathf.Clamp(mSkillSetIndex, 0, mSkillAttacks.Count - 1)]);
            return mAvailableAttacks;
        }

        EnemyAttack PickAttack()
        {
            var aAttacks = AvailableAttacks();
            if (aAttacks.Count == 0) return null;
            float aTotal = 0f;
            foreach (var aAttack in aAttacks) aTotal += Mathf.Max(0f, aAttack.Weight);
            float aRoll = Random.value * aTotal;
            foreach (var aAttack in aAttacks)
            {
                aRoll -= Mathf.Max(0f, aAttack.Weight);
                if (aRoll <= 0f) return aAttack;
            }
            return aAttacks[0];
        }

        /// <summary>指定した攻撃をすぐに始める(テストやデバッグ用)。使える攻撃の番号で指定する</summary>
        public bool ForceAttack(int iIndex)
        {
            var aAttacks = AvailableAttacks();
            if (IsDead || iIndex < 0 || iIndex >= aAttacks.Count || mTarget == null) return false;
            ReleaseToken();
            mAttack = aAttacks[iIndex];
            TryTakeToken();
            EnterWindup();
            return true;
        }

        /// <summary>体力が一定を下回ったら第二形態になる。少しひるんで、アーマーが戻る</summary>
        bool CheckPhase2()
        {
            if (IsPhase2 || mProfile.Phase2HealthRatio <= 0f || IsDead) return false;
            if (mDamageable.Health > mDamageable.MaxHealth * mProfile.Phase2HealthRatio) return false;

            IsPhase2 = true;
            mDamageable.RestoreArmor();
            EnterStagger(1.2f);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 4f, mProfile.TelegraphColor);
                CombatFeedback.Instance.Shake(0.4f);
            }
            PhaseChanged?.Invoke(this);
            return true;
        }

        bool TryTakeToken()
        {
            if (mHasToken) return true;
            if (!mProfile.IsBoss && mAttackTokensInUse >= mMaxAttackTokens) return false;
            mHasToken = true;
            mAttackTokensInUse++;
            return true;
        }

        void ReleaseToken()
        {
            if (!mHasToken) return;
            mHasToken = false;
            mAttackTokensInUse = Mathf.Max(0, mAttackTokensInUse - 1);
        }

        Vector3 Separation()
        {
            Vector3 aPush = Vector3.zero;
            foreach (var aOther in Active)
            {
                if (aOther == this) continue;
                Vector3 aAway = Flat(transform.position - aOther.transform.position);
                float aMinDistance = (Radius + aOther.Radius) * 1.5f;
                float aDistance = aAway.magnitude;
                if (aDistance > 0.01f && aDistance < aMinDistance) aPush += aAway / aDistance * (1f - aDistance / aMinDistance);
            }
            return aPush;
        }

        void Face(Vector3 iDirection, float iDeltaTime)
        {
            if (iDirection.sqrMagnitude < 1e-4f) return;
            var aRotation = Quaternion.LookRotation(iDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, aRotation, mProfile.TurnSpeed * iDeltaTime);
        }

        /// <summary>
        /// 攻撃をガード・パリィで受け止められた。iDirection は敵からプレイヤーへの向き。
        /// ガード: 軽く弾かれる / パリィ: 大きく弾かれてよろける(攻撃は中断)
        /// </summary>
        public void Rebound(Vector3 iDirection, bool iParried)
        {
            if (IsDead) return;
            Vector3 aBack = -iDirection;
            aBack.y = 0f;
            if (aBack.sqrMagnitude > 1e-4f) mKnockback += aBack.normalized * (iParried ? 7f : 3f);
            React(aBack, iParried ? 1.8f : 0.8f);
            Freeze(iParried ? 0.16f : 0.05f);
            if (iParried) EnterStagger(mProfile.IsBoss ? GuardRules.ParryStagger * 0.5f : GuardRules.ParryStagger);
        }

        /// <summary>よろけているか(テスト用)</summary>
        public bool IsStaggered => mState == State.Stagger;

        /// <summary>ヒットストップ: しばらくこの敵だけ止める(ほかの敵・弾は動き続ける)</summary>
        public void Freeze(float iDuration)
        {
            if (iDuration <= 0f) return;
            mFrozenUntil = Mathf.Max(mFrozenUntil, Time.unscaledTime + iDuration);
        }

        /// <summary>被弾の向きにのけぞり、つぶれる。iStrength は 1 がふつう</summary>
        public void React(Vector3 iDirection, float iStrength)
        {
            Vector3 aLocal = transform.InverseTransformDirection(iDirection);
            aLocal.y = 0f;
            mReactionDirection = aLocal.sqrMagnitude > 1e-4f ? aLocal.normalized : Vector3.back;
            mReactionStrength = Mathf.Clamp(Mathf.Max(mReactionStrength * 0.5f, iStrength), 0f, 2f);
            mReactionTime = 0f;
        }

        /// <summary>
        /// のけぞり・つぶれ。当たった瞬間の形でヒットストップの間は止まって小刻みに震え、
        /// 止まり終わったら、はねるように元へ戻る。
        /// </summary>
        void UpdateReaction(bool iFrozen)
        {
            if (mVisual == null || mReactionStrength <= 0f) return;
            if (!iFrozen) mReactionTime += Time.deltaTime;
            if (mReactionTime >= mReactionDuration)
            {
                mReactionStrength = 0f;
                mVisual.localScale = Vector3.one;
                mVisual.localRotation = Quaternion.identity;
                mVisual.localPosition = Vector3.zero;
                return;
            }

            float aCurve = Mathf.Exp(-mReactionTime * 12f) * Mathf.Cos(mReactionTime * 28f);
            float aAmount = mReactionStrength * aCurve;
            mVisual.localScale = new Vector3(1f + 0.16f * aAmount, 1f - 0.2f * aAmount, 1f + 0.16f * aAmount);
            Vector3 aAxis = Vector3.Cross(Vector3.up, mReactionDirection);
            mVisual.localRotation = Quaternion.AngleAxis(18f * aAmount, aAxis);
            Vector3 aShake = iFrozen ? mReactionDirection * (Random.Range(-1f, 1f) * 0.06f * mReactionStrength) : Vector3.zero;
            mVisual.localPosition = mReactionDirection * (0.12f * aAmount) + aShake;
        }

        void UpdateColor(float iDeltaTime)
        {
            if (mRenderers == null) return;
            mFlashTimer -= iDeltaTime;
            for (int aRendererIndex = 0; aRendererIndex < mRenderers.Length; aRendererIndex++)
            {
                Color aColor = mBaseColors[aRendererIndex];
                if (mFlashTimer > 0f)
                {
                    aColor = Color.white;
                }
                else if (mState == State.Windup)
                {
                    float aProgress = Mathf.Clamp01(mStateTime / Mathf.Max(0.01f, mAttack.Windup));
                    float aBlink = Mathf.PingPong(mStateTime * 8f, 1f);
                    aColor = Color.Lerp(mBaseColors[aRendererIndex], mProfile.TelegraphColor, aProgress * (0.6f + 0.4f * aBlink));
                }
                else if (mState == State.Stagger)
                {
                    aColor = Color.Lerp(mBaseColors[aRendererIndex], Color.gray, 0.4f);
                }
                mRenderers[aRendererIndex].material.color = aColor;
            }
        }

        static Vector3 Flat(Vector3 iVector) => new Vector3(iVector.x, 0f, iVector.z);
    }
}
