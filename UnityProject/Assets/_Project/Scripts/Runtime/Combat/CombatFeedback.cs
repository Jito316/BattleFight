using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>ヒットストップ、画面揺れ、ダメージ表示、衝撃波などの手応えの演出</summary>
    public class CombatFeedback : MonoBehaviour
    {
        public struct DamageNumber
        {
            public Vector3 Position;
            public string Text;
            public Color Color;
            public float Time;
        }

        public const float DamageNumberLifetime = 0.8f;

        [SerializeField, FormerlySerializedAs("cameraRig")] ThirdPersonCamera mCameraRig;
        [SerializeField, Tooltip("衝撃波に使うマテリアル"), FormerlySerializedAs("effectMaterial")] Material mEffectMaterial;
        [SerializeField, FormerlySerializedAs("hitStopTimeScale")] float mHitStopTimeScale = 0.05f;
        [SerializeField, FormerlySerializedAs("maxHitStop")] float mMaxHitStop = 0.15f;

        float mHitStopEnd;
        float mSlowMotionEnd;
        float mSlowMotionScale = 1f;

        public static CombatFeedback Instance { get; private set; }
        /// <summary>実行時に作るエフェクトや弾に使うマテリアル(シェーダーがビルドに残るようシーンから参照)</summary>
        public Material EffectMaterial => mEffectMaterial;
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
        public void SpawnBeam(Vector3 iFrom, Vector3 iTo, float iRadius, Color iColor, float iDuration)
        {
            var aObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aObject.name = "Beam";
            DestroyImmediate(aObject.GetComponent<Collider>());
            var aRenderer = aObject.GetComponent<Renderer>();
            if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
            aRenderer.material.color = iColor;

            Vector3 aDirection = iTo - iFrom;
            aObject.transform.position = iFrom + aDirection * 0.5f;
            aObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, aDirection.normalized);
            aObject.transform.localScale = new Vector3(iRadius * 2f, aDirection.magnitude * 0.5f, iRadius * 2f);
            Destroy(aObject, iDuration);
        }

        void Update()
        {
            if (!GamePause.IsPaused) Time.timeScale = CurrentTimeScale();
            DamageNumbers.RemoveAll(iNumber => Time.unscaledTime - iNumber.Time > DamageNumberLifetime);
        }

        /// <summary>ヒットストップ(ごく短く止める)とスローモーション(しばらく遅くする)のうち、遅い方</summary>
        float CurrentTimeScale()
        {
            float aScale = 1f;
            if (Time.unscaledTime < mSlowMotionEnd) aScale = mSlowMotionScale;
            if (Time.unscaledTime < mHitStopEnd) aScale = Mathf.Min(aScale, mHitStopTimeScale);
            return aScale;
        }

        public bool InSlowMotion => Time.unscaledTime < mSlowMotionEnd;

        public void HitStop(float iDuration)
        {
            if (iDuration <= 0f || GamePause.IsPaused) return;
            mHitStopEnd = Mathf.Max(mHitStopEnd, Time.unscaledTime + Mathf.Min(iDuration, mMaxHitStop));
            Time.timeScale = CurrentTimeScale();
        }

        /// <summary>全体をしばらく遅くする(ウェーブ最後の1体を倒した瞬間、チェンジドライブなど)</summary>
        public void SlowMotion(float iScale, float iDuration)
        {
            if (GamePause.IsPaused) return;
            mSlowMotionScale = Mathf.Clamp(iScale, 0.05f, 1f);
            mSlowMotionEnd = Time.unscaledTime + iDuration;
            Time.timeScale = CurrentTimeScale();
        }

        /// <summary>当たった場所から火花を散らす(小さな欠片が飛んで消える)</summary>
        public void SpawnSparks(Vector3 iPosition, Color iColor, int iCount, float iSpeed = 7f)
        {
            for (int aIndex = 0; aIndex < iCount; aIndex++)
            {
                var aVelocity = (Random.onUnitSphere + Vector3.up * 0.6f).normalized * (iSpeed * Random.Range(0.5f, 1.2f));
                SpawnDebris(iPosition, aVelocity, iColor, Random.Range(0.08f, 0.16f), Random.Range(0.25f, 0.45f), null);
            }
        }

        /// <summary>倒した敵が砕け散り、刻片がプレイヤーへ飛んでくる</summary>
        public void SpawnDefeatBurst(Vector3 iCenter, Color iBodyColor, float iSize, Transform iCollector, int iShards)
        {
            for (int aIndex = 0; aIndex < 10; aIndex++)
            {
                var aVelocity = (Random.onUnitSphere + Vector3.up).normalized * Random.Range(4f, 9f);
                SpawnDebris(iCenter, aVelocity, iBodyColor, Random.Range(0.18f, 0.32f) * iSize, Random.Range(0.5f, 0.8f), null);
            }
            var aShardColor = new Color(0.55f, 0.85f, 1f);
            for (int aIndex = 0; aIndex < iShards; aIndex++)
            {
                var aVelocity = (Random.onUnitSphere + Vector3.up * 1.5f).normalized * Random.Range(3f, 6f);
                SpawnDebris(iCenter, aVelocity, aShardColor, 0.14f, 0.9f, iCollector);
            }
            SpawnShockwave(new Vector3(iCenter.x, iCenter.y - iSize * 0.8f, iCenter.z), 1.5f * iSize, iBodyColor);
        }

        void SpawnDebris(Vector3 iPosition, Vector3 iVelocity, Color iColor, float iSize, float iLifetime, Transform iHoming)
        {
            var aObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aObject.name = iHoming != null ? "Shard" : "Debris";
            DestroyImmediate(aObject.GetComponent<Collider>());
            var aRenderer = aObject.GetComponent<Renderer>();
            if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
            aRenderer.material.color = iColor;
            aObject.transform.position = iPosition;
            aObject.transform.rotation = Random.rotation;
            aObject.transform.localScale = Vector3.one * iSize;
            aObject.AddComponent<DebrisFx>().Play(iVelocity, iLifetime, iHoming);
        }

        public void Shake(float iAmount)
        {
            if (mCameraRig != null) mCameraRig.AddShake(iAmount);
        }

        public void SpawnDamageNumber(Vector3 iPosition, float iAmount, Color iColor)
        {
            DamageNumbers.Add(new DamageNumber
            {
                Position = iPosition + Random.insideUnitSphere * 0.3f,
                Text = Mathf.CeilToInt(iAmount).ToString(),
                Color = iColor,
                Time = Time.unscaledTime,
            });
        }

        public void SpawnShockwave(Vector3 iPosition, float iRadius, Color iColor)
        {
            var aObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aObject.name = "Shockwave";
            DestroyImmediate(aObject.GetComponent<Collider>());
            if (mEffectMaterial != null) aObject.GetComponent<Renderer>().sharedMaterial = mEffectMaterial;
            aObject.transform.position = new Vector3(iPosition.x, Mathf.Max(0.05f, iPosition.y), iPosition.z);
            aObject.AddComponent<ShockwaveFx>().Play(iRadius, iColor);
        }
    }

    /// <summary>火花・砕けた欠片・飛んでくる刻片。homing があると、少しあとからそこへ吸い寄せられる</summary>
    public class DebrisFx : MonoBehaviour
    {
        Vector3 mVelocity;
        float mLifetime;
        float mElapsed;
        float mSize;
        Transform mHoming;

        public void Play(Vector3 iStartVelocity, float iLife, Transform iTarget)
        {
            mVelocity = iStartVelocity;
            mLifetime = iLife;
            mHoming = iTarget;
            mSize = transform.localScale.x;
        }

        void Update()
        {
            float aDeltaTime = Time.deltaTime;
            mElapsed += aDeltaTime;
            float aProgress = mElapsed / mLifetime;
            if (mHoming != null && aProgress > 0.35f)
            {
                // 刻片: 途中からプレイヤーへ向かう
                Vector3 aOffset = mHoming.position + Vector3.up - transform.position;
                mVelocity = Vector3.Lerp(mVelocity, aOffset.normalized * 22f, 1f - Mathf.Exp(-10f * aDeltaTime));
                if (aOffset.sqrMagnitude < 0.4f) aProgress = 1f;
            }
            else
            {
                mVelocity += Physics.gravity * (0.6f * aDeltaTime);
            }
            transform.position += mVelocity * aDeltaTime;
            transform.Rotate(mVelocity * (40f * aDeltaTime), Space.World);
            transform.localScale = Vector3.one * (mSize * Mathf.Clamp01(1.2f - aProgress));
            if (aProgress >= 1f) Destroy(gameObject);
        }
    }

    public class ShockwaveFx : MonoBehaviour
    {
        const float mDuration = 0.25f;

        float mRadius;
        float mElapsed;

        public void Play(float iNewRadius, Color iColor)
        {
            mRadius = iNewRadius;
            GetComponent<Renderer>().material.color = iColor;
            transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        void Update()
        {
            mElapsed += Time.deltaTime;
            float aProgress = mElapsed / mDuration;
            float aDiameter = mRadius * 2f * Mathf.Sqrt(Mathf.Clamp01(aProgress));
            transform.localScale = new Vector3(aDiameter, 0.02f * (1f - aProgress) + 0.005f, aDiameter);
            if (aProgress >= 1f) Destroy(gameObject);
        }
    }
}
