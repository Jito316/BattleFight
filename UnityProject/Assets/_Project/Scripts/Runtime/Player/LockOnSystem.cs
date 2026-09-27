using UnityEngine;

namespace BattleFight
{
    public class LockOnSystem : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform viewTransform;
        [SerializeField] float maxDistance = 25f;

        public EnemyController Target { get; private set; }

        void Update()
        {
            if (input.Consume(PlayerAction.LockOn))
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
            else if (Vector3.Distance(Target.transform.position, transform.position) > maxDistance * 1.5f) Target = null;
        }

        EnemyController FindBest()
        {
            Transform view = viewTransform != null ? viewTransform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;

            EnemyController best = null;
            float bestScore = float.MaxValue;
            foreach (var enemy in EnemyController.Active)
            {
                if (enemy.IsDead) continue;
                Vector3 to = enemy.transform.position - transform.position;
                float distance = to.magnitude;
                if (distance > maxDistance) continue;
                float facing = Vector3.Dot(Vector3.ProjectOnPlane(to, Vector3.up).normalized, forward);
                float score = distance * (2f - facing);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }
            return best;
        }
    }
}
