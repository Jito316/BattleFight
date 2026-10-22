using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    public class LockOnSystem : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;
        [SerializeField, FormerlySerializedAs("viewTransform")] Transform mViewTransform;
        [SerializeField, FormerlySerializedAs("maxDistance")] float mMaxDistance = 25f;

        public EnemyController Target { get; private set; }

        void Update()
        {
            if (GamePause.IsPaused) return;
            if (mInput.Consume(PlayerAction.LockOn))
            {
                Target = Target != null ? null : FindBest();
            }

            if (Target == null)
            {
                Target = null;
                return;
            }

            // 倒したら次の敵へ自動で切り替え、離れすぎたら解除
            if (Target.IsDead) Target = FindBest();
            else if (Vector3.Distance(Target.transform.position, transform.position) > mMaxDistance * 1.5f) Target = null;
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
