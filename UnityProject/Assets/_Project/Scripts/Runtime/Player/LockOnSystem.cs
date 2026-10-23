using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// ロックオン。狙っている敵の足元に、脈打つ輪を出す。
    /// ロックオン中は Z・C(右スティックを左右に倒す)で、標的を左右の敵へ切り替える。
    /// </summary>
    public class LockOnSystem : MonoBehaviour
    {
        const int mRingSegments = 48;
        /// <summary>右スティックをこれより倒したら、切り替えの入力として扱う</summary>
        const float mStickFlick = 0.7f;
        static readonly Color mRingColor = new Color(1f, 0.85f, 0.35f);

        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;
        [SerializeField, FormerlySerializedAs("viewTransform")] Transform mViewTransform;
        [SerializeField, FormerlySerializedAs("maxDistance")] float mMaxDistance = 25f;

        LineRenderer mRing;
        float mRingPopTime = -99f;
        bool mStickHeld;
        readonly List<EnemyController> mCandidates = new List<EnemyController>();
        readonly List<Vector3> mCandidatePositions = new List<Vector3>();

        public EnemyController Target { get; private set; }
        /// <summary>足元の輪を出しているか(テスト用)</summary>
        public bool ShowingRing => mRing != null && mRing.enabled;

        void Update()
        {
            if (GamePause.IsPaused) return;
            if (mInput.Consume(PlayerAction.LockOn))
            {
                SetTarget(Target != null ? null : FindBest());
            }

            if (Target == null)
            {
                Target = null;
                mInput.Consume(PlayerAction.LockOnLeft);
                mInput.Consume(PlayerAction.LockOnRight);
                UpdateRing();
                return;
            }

            // 倒したら次の敵へ自動で切り替え、離れすぎたら解除
            if (Target.IsDead) SetTarget(FindBest());
            else if (Vector3.Distance(Target.transform.position, transform.position) > mMaxDistance * 1.5f) SetTarget(null);

            if (Target != null)
            {
                int aSwitch = 0;
                if (mInput.Consume(PlayerAction.LockOnLeft)) aSwitch = -1;
                if (mInput.Consume(PlayerAction.LockOnRight)) aSwitch = 1;
                // 右スティックを左右に倒した瞬間(倒したままでは続けて切り替えない)
                var aGamepad = Gamepad.current;
                float aStick = aGamepad != null ? aGamepad.rightStick.x.ReadValue() : 0f;
                if (!mStickHeld && Mathf.Abs(aStick) >= mStickFlick) aSwitch = aStick > 0f ? 1 : -1;
                mStickHeld = Mathf.Abs(aStick) >= mStickFlick * 0.5f;
                if (aSwitch != 0) SwitchTarget(aSwitch);
            }
            UpdateRing();
        }

        /// <summary>標的を、今の標的から見て右(+1)・左(-1)の敵へ切り替える。いなければそのまま</summary>
        public bool SwitchTarget(int iDirection)
        {
            if (Target == null) return false;
            mCandidates.Clear();
            mCandidatePositions.Clear();
            foreach (var aEnemy in EnemyController.Active)
            {
                if (aEnemy == Target || aEnemy.IsDead) continue;
                if (Vector3.Distance(aEnemy.transform.position, transform.position) > mMaxDistance) continue;
                mCandidates.Add(aEnemy);
                mCandidatePositions.Add(aEnemy.transform.position);
            }
            int aPick = LockOnRules.PickSide(transform.position, Target.transform.position, mCandidatePositions, iDirection);
            if (aPick < 0) return false;
            SetTarget(mCandidates[aPick]);
            SoundPlayer.Play(Sfx.Swap, 0.3f, 1.3f);
            return true;
        }

        void SetTarget(EnemyController iTarget)
        {
            if (iTarget != null && iTarget != Target) mRingPopTime = Time.unscaledTime;
            Target = iTarget;
        }

        /// <summary>標的の足元の輪: ゆっくり回りながら脈打ち、標的を変えた瞬間に大きく弾む</summary>
        void UpdateRing()
        {
            if (mRing == null)
            {
                var aObject = new GameObject("LockOnRing");
                mRing = aObject.AddComponent<LineRenderer>();
                mRing.loop = true;
                mRing.useWorldSpace = true;
                mRing.positionCount = mRingSegments;
                mRing.widthMultiplier = 0.07f;
                mRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mRing.receiveShadows = false;
                if (CombatFeedback.Instance != null && CombatFeedback.Instance.EffectMaterial != null)
                {
                    mRing.sharedMaterial = CombatFeedback.Instance.EffectMaterial;
                }
                mRing.material.color = mRingColor;
                mRing.enabled = false;
            }

            bool aShow = Target != null && !Target.IsDead;
            mRing.enabled = aShow;
            if (!aShow) return;

            float aPop = Mathf.Clamp01(1f - (Time.unscaledTime - mRingPopTime) / 0.2f);
            float aPulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
            float aRadius = (Target.Radius + 0.45f) * aPulse * (1f + 0.5f * aPop);
            float aSpin = Time.unscaledTime * 40f;
            Vector3 aCenter = Target.transform.position + Vector3.up * 0.06f;
            for (int aIndex = 0; aIndex < mRingSegments; aIndex++)
            {
                float aAngle = (aSpin + 360f * aIndex / mRingSegments) * Mathf.Deg2Rad;
                // 4 か所だけ少し内側へへこませて、回っているのが分かるようにする
                float aNotch = 1f - 0.08f * Mathf.Pow(Mathf.Abs(Mathf.Cos(aAngle * 2f)), 16f);
                mRing.SetPosition(aIndex, aCenter + new Vector3(Mathf.Cos(aAngle), 0f, Mathf.Sin(aAngle)) * (aRadius * aNotch));
            }
            mRing.widthMultiplier = 0.07f * (1f + aPop);
        }

        void OnDestroy()
        {
            if (mRing != null) Destroy(mRing.gameObject);
        }

        EnemyController FindBest()
        {
            Transform aView = mViewTransform != null ? mViewTransform : transform;
            Vector3 aForward = Vector3.ProjectOnPlane(aView.forward, Vector3.up).normalized;

            EnemyController aBest = null;
            float aBestScore = float.MaxValue;
            foreach (var aEnemy in EnemyController.Active)
            {
                if (aEnemy.IsDead) continue;
                Vector3 aOffset = aEnemy.transform.position - transform.position;
                float aDistance = aOffset.magnitude;
                if (aDistance > mMaxDistance) continue;
                float aFacing = Vector3.Dot(Vector3.ProjectOnPlane(aOffset, Vector3.up).normalized, aForward);
                float aScore = aDistance * (2f - aFacing);
                if (aScore < aBestScore)
                {
                    aBestScore = aScore;
                    aBest = aEnemy;
                }
            }
            return aBest;
        }
    }
}
