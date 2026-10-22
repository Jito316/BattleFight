using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>ヒットストップ、画面揺れ、ダメージ表示、衝撃波などの手応えの演出</summary>
    public class CombatFeedback : MonoBehaviour
    {
        public struct DamageNumber
        {
            public Vector3 position;
            public string text;
            public Color color;
            public float time;
        }

        public const float DamageNumberLifetime = 0.8f;

        [SerializeField] ThirdPersonCamera cameraRig;
        [SerializeField, Tooltip("衝撃波に使うマテリアル")] Material effectMaterial;
        [SerializeField] float hitStopTimeScale = 0.05f;
        [SerializeField] float maxHitStop = 0.15f;

        float hitStopEnd;
        float slowMotionEnd;
        float slowMotionScale = 1f;

        public static CombatFeedback Instance { get; private set; }
        /// <summary>実行時に作るエフェクトや弾に使うマテリアル(シェーダーがビルドに残るようシーンから参照)</summary>
        public Material EffectMaterial => effectMaterial;
        public List<DamageNumber> DamageNumbers { get; } = new List<DamageNumber>();

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GamePause.IsPaused) GamePause.Set(false);
            Time.timeScale = 1f;
        }

        /// <summary>ビームの見た目。from から to へ伸びる細い円柱を、duration 秒だけ出す。</summary>
        public void SpawnBeam(Vector3 from, Vector3 to, float radius, Color color, float duration)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Beam";
            DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            if (effectMaterial != null) renderer.sharedMaterial = effectMaterial;
            renderer.material.color = color;

            Vector3 direction = to - from;
            go.transform.position = from + direction * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            go.transform.localScale = new Vector3(radius * 2f, direction.magnitude * 0.5f, radius * 2f);
            Destroy(go, duration);
        }

        void Update()
        {
            if (!GamePause.IsPaused) Time.timeScale = CurrentTimeScale();
            DamageNumbers.RemoveAll(n => Time.unscaledTime - n.time > DamageNumberLifetime);
        }

        /// <summary>ヒットストップ(ごく短く止める)とスローモーション(しばらく遅くする)のうち、遅い方</summary>
        float CurrentTimeScale()
        {
            float scale = 1f;
            if (Time.unscaledTime < slowMotionEnd) scale = slowMotionScale;
            if (Time.unscaledTime < hitStopEnd) scale = Mathf.Min(scale, hitStopTimeScale);
            return scale;
        }

        public bool InSlowMotion => Time.unscaledTime < slowMotionEnd;

        public void HitStop(float duration)
        {
            if (duration <= 0f || GamePause.IsPaused) return;
            hitStopEnd = Mathf.Max(hitStopEnd, Time.unscaledTime + Mathf.Min(duration, maxHitStop));
            Time.timeScale = CurrentTimeScale();
        }

        /// <summary>全体をしばらく遅くする(ウェーブ最後の1体を倒した瞬間、チェンジドライブなど)</summary>
        public void SlowMotion(float scale, float duration)
        {
            if (GamePause.IsPaused) return;
            slowMotionScale = Mathf.Clamp(scale, 0.05f, 1f);
            slowMotionEnd = Time.unscaledTime + duration;
            Time.timeScale = CurrentTimeScale();
        }

        /// <summary>当たった場所から火花を散らす(小さな欠片が飛んで消える)</summary>
        public void SpawnSparks(Vector3 position, Color color, int count, float speed = 7f)
        {
            for (int i = 0; i < count; i++)
            {
                var velocity = (Random.onUnitSphere + Vector3.up * 0.6f).normalized * (speed * Random.Range(0.5f, 1.2f));
                SpawnDebris(position, velocity, color, Random.Range(0.08f, 0.16f), Random.Range(0.25f, 0.45f), null);
            }
        }

        /// <summary>倒した敵が砕け散り、刻片がプレイヤーへ飛んでくる</summary>
        public void SpawnDefeatBurst(Vector3 center, Color bodyColor, float size, Transform collector, int shards)
        {
            for (int i = 0; i < 10; i++)
            {
                var velocity = (Random.onUnitSphere + Vector3.up).normalized * Random.Range(4f, 9f);
                SpawnDebris(center, velocity, bodyColor, Random.Range(0.18f, 0.32f) * size, Random.Range(0.5f, 0.8f), null);
            }
            var shardColor = new Color(0.55f, 0.85f, 1f);
            for (int i = 0; i < shards; i++)
            {
                var velocity = (Random.onUnitSphere + Vector3.up * 1.5f).normalized * Random.Range(3f, 6f);
                SpawnDebris(center, velocity, shardColor, 0.14f, 0.9f, collector);
            }
            SpawnShockwave(new Vector3(center.x, center.y - size * 0.8f, center.z), 1.5f * size, bodyColor);
        }

        void SpawnDebris(Vector3 position, Vector3 velocity, Color color, float size, float lifetime, Transform homing)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = homing != null ? "Shard" : "Debris";
            DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            if (effectMaterial != null) renderer.sharedMaterial = effectMaterial;
            renderer.material.color = color;
            go.transform.position = position;
            go.transform.rotation = Random.rotation;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<DebrisFx>().Play(velocity, lifetime, homing);
        }

        public void Shake(float amount)
        {
            if (cameraRig != null) cameraRig.AddShake(amount);
        }

        public void SpawnDamageNumber(Vector3 position, float amount, Color color)
        {
            DamageNumbers.Add(new DamageNumber
            {
                position = position + Random.insideUnitSphere * 0.3f,
                text = Mathf.CeilToInt(amount).ToString(),
                color = color,
                time = Time.unscaledTime,
            });
        }

        public void SpawnShockwave(Vector3 position, float radius, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Shockwave";
            DestroyImmediate(go.GetComponent<Collider>());
            if (effectMaterial != null) go.GetComponent<Renderer>().sharedMaterial = effectMaterial;
            go.transform.position = new Vector3(position.x, Mathf.Max(0.05f, position.y), position.z);
            go.AddComponent<ShockwaveFx>().Play(radius, color);
        }
    }

    /// <summary>火花・砕けた欠片・飛んでくる刻片。homing があると、少しあとからそこへ吸い寄せられる</summary>
    public class DebrisFx : MonoBehaviour
    {
        Vector3 velocity;
        float lifetime;
        float elapsed;
        float size;
        Transform homing;

        public void Play(Vector3 startVelocity, float life, Transform target)
        {
            velocity = startVelocity;
            lifetime = life;
            homing = target;
            size = transform.localScale.x;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float t = elapsed / lifetime;
            if (homing != null && t > 0.35f)
            {
                // 刻片: 途中からプレイヤーへ向かう
                Vector3 to = homing.position + Vector3.up - transform.position;
                velocity = Vector3.Lerp(velocity, to.normalized * 22f, 1f - Mathf.Exp(-10f * dt));
                if (to.sqrMagnitude < 0.4f) t = 1f;
            }
            else
            {
                velocity += Physics.gravity * (0.6f * dt);
            }
            transform.position += velocity * dt;
            transform.Rotate(velocity * (40f * dt), Space.World);
            transform.localScale = Vector3.one * (size * Mathf.Clamp01(1.2f - t));
            if (t >= 1f) Destroy(gameObject);
        }
    }

    public class ShockwaveFx : MonoBehaviour
    {
        const float Duration = 0.25f;

        float radius;
        float elapsed;

        public void Play(float newRadius, Color color)
        {
            radius = newRadius;
            GetComponent<Renderer>().material.color = color;
            transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;
            float diameter = radius * 2f * Mathf.Sqrt(Mathf.Clamp01(t));
            transform.localScale = new Vector3(diameter, 0.02f * (1f - t) + 0.005f, diameter);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
