using UnityEngine;

namespace BattleFight
{
    /// <summary>CombatFeedback: 演出を出す(火花・衝撃波・斬撃・残像・欠片・ダメージの数字など)</summary>
    public partial class CombatFeedback
    {
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

        public void SpawnDamageNumber(Vector3 iPosition, float iAmount, Color iColor, float iScale = 1f)
        {
            DamageNumbers.Add(new DamageNumber
            {
                Position = iPosition + Random.insideUnitSphere * 0.3f,
                Text = Mathf.CeilToInt(iAmount).ToString(),
                Color = iColor,
                Time = Time.unscaledTime,
                Scale = iScale,
            });
        }

        /// <summary>
        /// 残像: iRoot の下の見た目(メッシュ)を、その場に写し取って残し、縮みながら消す(回避の速さを見せる)
        /// </summary>
        public void SpawnAfterimage(Transform iRoot, Color iColor, float iLife)
        {
            if (iRoot == null) return;
            foreach (var aFilter in iRoot.GetComponentsInChildren<MeshFilter>())
            {
                var aSourceRenderer = aFilter.GetComponent<MeshRenderer>();
                if (aSourceRenderer == null || !aSourceRenderer.enabled) continue;
                var aObject = new GameObject("Afterimage");
                aObject.transform.SetPositionAndRotation(aFilter.transform.position, aFilter.transform.rotation);
                aObject.transform.localScale = aFilter.transform.lossyScale;
                aObject.AddComponent<MeshFilter>().sharedMesh = aFilter.sharedMesh;
                var aRenderer = aObject.AddComponent<MeshRenderer>();
                if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
                aRenderer.material.color = iColor;
                aRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                aObject.AddComponent<AfterimageFx>().Play(iLife);
            }
        }

        /// <summary>
        /// 斬撃の弧: iRotation の向き(+Z が正面)に、三日月形の光を出す。iDirection で振る向き(+1: 右から左)
        /// </summary>
        public SwingArcFx SpawnSwingArc(Vector3 iCenter, Quaternion iRotation, float iRadius, float iArcDegrees, float iThickness,
            float iDirection, Color iColor)
        {
            var aObject = new GameObject("SwingArc");
            aObject.transform.SetPositionAndRotation(iCenter, iRotation);
            aObject.AddComponent<MeshFilter>();
            var aRenderer = aObject.AddComponent<MeshRenderer>();
            if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
            var aArc = aObject.AddComponent<SwingArcFx>();
            aArc.Play(iRadius, iArcDegrees, iThickness, iDirection, iColor);
            return aArc;
        }

        /// <summary>アーマーが砕けた欠片: 大きめの破片が勢いよく飛び散る</summary>
        public void SpawnShards(Vector3 iPosition, Color iColor, int iCount, float iSize)
        {
            for (int aIndex = 0; aIndex < iCount; aIndex++)
            {
                var aObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                aObject.name = "ArmorShard";
                DestroyImmediate(aObject.GetComponent<Collider>());
                var aRenderer = aObject.GetComponent<Renderer>();
                if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
                aRenderer.material.color = Color.Lerp(iColor, Color.white, Random.Range(0f, 0.3f));
                aObject.transform.position = iPosition + Random.insideUnitSphere * 0.3f;
                aObject.transform.rotation = Random.rotation;
                aObject.transform.localScale = new Vector3(iSize, iSize * 0.4f, iSize * Random.Range(0.6f, 1.2f));
                Vector3 aVelocity = Random.onUnitSphere * Random.Range(6f, 10f);
                aVelocity.y = Mathf.Abs(aVelocity.y) + 3f;
                aObject.AddComponent<DebrisFx>().Play(aVelocity, Random.Range(0.5f, 0.8f), null);
            }
        }

        /// <summary>
        /// 武器らしい当たりの見た目。剣: 鋭い斬撃の線 / 大槌: 足元の土煙の輪 / 杖: 魔力の輪 / 籠手: 短い衝撃の輪。
        /// iDirection は攻撃者から敵への水平な向き
        /// </summary>
        public void SpawnImpact(ImpactStyle iStyle, Vector3 iHitPoint, Vector3 iGround, Vector3 iDirection, Color iColor, bool iHeavy)
        {
            switch (iStyle)
            {
                case ImpactStyle.Slash:
                    SpawnSlash(iHitPoint, iDirection, iColor, iHeavy ? 2.6f : 2f);
                    break;
                case ImpactStyle.Lash:
                    SpawnSlash(iHitPoint, iDirection, iColor, 1.6f);
                    break;
                case ImpactStyle.Smash:
                    SpawnShockwave(iGround, iHeavy ? 3.2f : 2.4f, new Color(0.75f, 0.68f, 0.55f));
                    SpawnSparks(iGround + Vector3.up * 0.2f, new Color(0.6f, 0.55f, 0.45f), iHeavy ? 10 : 6, 5f);
                    break;
                case ImpactStyle.Arcane:
                    SpawnShockwave(iHitPoint, 1.2f, iColor);
                    break;
                case ImpactStyle.Punch:
                    SpawnShockwave(iHitPoint, iHeavy ? 1.4f : 0.9f, Color.white);
                    break;
            }
        }

        /// <summary>斬撃の線: 当たった場所を横切る細長い板が、一瞬で伸びて細くなって消える</summary>
        public void SpawnSlash(Vector3 iPosition, Vector3 iDirection, Color iColor, float iLength)
        {
            var aObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aObject.name = "Slash";
            DestroyImmediate(aObject.GetComponent<Collider>());
            if (mEffectMaterial != null) aObject.GetComponent<Renderer>().sharedMaterial = mEffectMaterial;
            Vector3 aForward = iDirection.sqrMagnitude > 1e-4f ? iDirection.normalized : Vector3.forward;
            // 攻撃の向きに直角な面で、斜めに走る線にする
            aObject.transform.position = iPosition;
            aObject.transform.rotation = Quaternion.LookRotation(aForward) * Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f));
            aObject.AddComponent<SlashFx>().Play(iLength, Color.Lerp(iColor, Color.white, 0.5f));
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
}
