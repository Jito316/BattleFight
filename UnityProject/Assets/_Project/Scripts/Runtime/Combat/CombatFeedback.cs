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

        public static CombatFeedback Instance { get; private set; }
        public List<DamageNumber> DamageNumbers { get; } = new List<DamageNumber>();

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (Time.timeScale < 1f && Time.unscaledTime >= hitStopEnd) Time.timeScale = 1f;
            DamageNumbers.RemoveAll(n => Time.unscaledTime - n.time > DamageNumberLifetime);
        }

        public void HitStop(float duration)
        {
            if (duration <= 0f) return;
            hitStopEnd = Mathf.Max(hitStopEnd, Time.unscaledTime + Mathf.Min(duration, maxHitStop));
            Time.timeScale = hitStopTimeScale;
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
