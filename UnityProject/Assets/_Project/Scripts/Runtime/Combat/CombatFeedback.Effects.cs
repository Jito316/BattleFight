using UnityEngine;

namespace BattleFight
{
    /// <summary>CombatFeedback: 演出を出す(火花・衝撃波・斬撃・残像・欠片・ダメージの数字など)</summary>
    public partial class CombatFeedback
    {
        /// <summary>演出に使う、当たり判定のない形を作る(EffectPool がしまっておけるよう、ここでだけ作る)</summary>
        GameObject CreateShape(PrimitiveType iShape)
        {
            var aObject = GameObject.CreatePrimitive(iShape);
            // 当たり判定はすぐに消す(残ると、同じフレームの弾が壁として当たってしまう)
            DestroyImmediate(aObject.GetComponent<Collider>());
            var aRenderer = aObject.GetComponent<Renderer>();
            if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
            aRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aRenderer.receiveShadows = false;
            return aObject;
        }

        /// <summary>メッシュを後から入れる入れ物(残像・斬撃の弧)</summary>
        GameObject CreateMeshHolder()
        {
            var aObject = new GameObject();
            aObject.AddComponent<MeshFilter>();
            var aRenderer = aObject.AddComponent<MeshRenderer>();
            if (mEffectMaterial != null) aRenderer.sharedMaterial = mEffectMaterial;
            aRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aRenderer.receiveShadows = false;
            return aObject;
        }

        /// <summary>演出の差し替え表(テストで差し替えられるように公開)</summary>
        public EffectLibrary Effects
        {
            get => mEffects;
            set => mEffects = value;
        }

        /// <summary>
        /// 場面 iCue の演出が差し替えられていれば出して true を返す(呼び出し側は、いつもの演出を出さない)。
        /// iSize は場面の大きさ(衝撃波の半径など)、iColor は場面の色
        /// </summary>
        public bool PlayCue(EffectCue iCue, Vector3 iPosition, Quaternion iRotation, float iSize, Color iColor)
        {
            var aEntry = mEffects != null ? mEffects.Find(iCue) : null;
            if (aEntry == null) return false;
            var aObject = EffectPool.Get($"Cue_{iCue}_{aEntry.Prefab.GetInstanceID()}", () => Instantiate(aEntry.Prefab));
            aObject.SetActive(true);
            aObject.transform.SetPositionAndRotation(iPosition, iRotation);
            aObject.transform.localScale = Vector3.one * (aEntry.Scale * (aEntry.ScaleWithSize ? Mathf.Max(0.01f, iSize) : 1f));
            Fx<CueEffect>(aObject).Play(iColor, aEntry.UseColor, aEntry.Lifetime);
            return true;
        }

        /// <summary>向きのない場面(衝撃波・火花など)の PlayCue</summary>
        public bool PlayCue(EffectCue iCue, Vector3 iPosition, float iSize, Color iColor) =>
            PlayCue(iCue, iPosition, Quaternion.identity, iSize, iColor);

        /// <summary>種類ごとの部品(なければ足す)</summary>
        static T Fx<T>(GameObject iObject) where T : Component
        {
            var aFx = iObject.GetComponent<T>();
            return aFx != null ? aFx : iObject.AddComponent<T>();
        }

        /// <summary>ビームの見た目。from から to へ伸びる細い円柱を、duration 秒だけ出す。</summary>
        public void SpawnBeam(Vector3 iFrom, Vector3 iTo, float iRadius, Color iColor, float iDuration)
        {
            var aObject = EffectPool.Get("Beam", () => CreateShape(PrimitiveType.Cylinder));
            aObject.GetComponent<Renderer>().material.color = iColor;

            Vector3 aDirection = iTo - iFrom;
            aObject.transform.position = iFrom + aDirection * 0.5f;
            aObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, aDirection.normalized);
            aObject.transform.localScale = new Vector3(iRadius * 2f, aDirection.magnitude * 0.5f, iRadius * 2f);
            Fx<LifetimeFx>(aObject).Play(iDuration);
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

        /// <summary>倒した敵が砕け散り、刻片がプレイヤーへ飛んでくる。iBurst を外すと、刻片だけ出す(砕け散る演出を差し替えたとき)</summary>
        public void SpawnDefeatBurst(Vector3 iCenter, Color iBodyColor, float iSize, Transform iCollector, int iShards, bool iBurst = true)
        {
            for (int aIndex = 0; iBurst && aIndex < 10; aIndex++)
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
            if (iBurst) SpawnShockwave(new Vector3(iCenter.x, iCenter.y - iSize * 0.8f, iCenter.z), 1.5f * iSize, iBodyColor);
        }

        void SpawnDebris(Vector3 iPosition, Vector3 iVelocity, Color iColor, float iSize, float iLifetime, Transform iHoming)
        {
            var aObject = EffectPool.Get(iHoming != null ? "Shard" : "Debris", () => CreateShape(PrimitiveType.Cube));
            aObject.GetComponent<Renderer>().material.color = iColor;
            aObject.transform.position = iPosition;
            aObject.transform.rotation = Random.rotation;
            aObject.transform.localScale = Vector3.one * iSize;
            Fx<DebrisFx>(aObject).Play(iVelocity, iLifetime, iHoming);
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
        /// 残像: iRoot の下の見た目(メッシュ)を、その場に写し取って残し、縮みながら消す(回避の速さを見せる)。
        /// 骨で動くモデル(SkinnedMeshRenderer)は、その瞬間の形を焼き付けて写す
        /// </summary>
        public void SpawnAfterimage(Transform iRoot, Color iColor, float iLife)
        {
            if (iRoot == null) return;
            foreach (var aFilter in iRoot.GetComponentsInChildren<MeshFilter>())
            {
                var aSourceRenderer = aFilter.GetComponent<MeshRenderer>();
                if (aSourceRenderer == null || !aSourceRenderer.enabled) continue;
                var aObject = EffectPool.Get("Afterimage", CreateMeshHolder);
                aObject.transform.SetPositionAndRotation(aFilter.transform.position, aFilter.transform.rotation);
                aObject.transform.localScale = aFilter.transform.lossyScale;
                aObject.GetComponent<MeshFilter>().sharedMesh = aFilter.sharedMesh;
                aObject.GetComponent<MeshRenderer>().material.color = iColor;
                Fx<AfterimageFx>(aObject).Play(iLife);
            }
            foreach (var aSkinned in iRoot.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!aSkinned.enabled || aSkinned.sharedMesh == null) continue;
                var aObject = EffectPool.Get("Afterimage", CreateMeshHolder);
                var aAfterimage = Fx<AfterimageFx>(aObject);
                // 大きさも焼き付けるので、入れ物の大きさは 1 にする
                aSkinned.BakeMesh(aAfterimage.BakedMesh, true);
                aObject.transform.SetPositionAndRotation(aSkinned.transform.position, aSkinned.transform.rotation);
                aObject.transform.localScale = Vector3.one;
                aObject.GetComponent<MeshFilter>().sharedMesh = aAfterimage.BakedMesh;
                aObject.GetComponent<MeshRenderer>().material.color = iColor;
                aAfterimage.Play(iLife);
            }
        }

        /// <summary>
        /// 斬撃の弧: iRotation の向き(+Z が正面)に、三日月形の光を出す。iDirection で振る向き(+1: 右から左)
        /// </summary>
        public SwingArcFx SpawnSwingArc(Vector3 iCenter, Quaternion iRotation, float iRadius, float iArcDegrees, float iThickness,
            float iDirection, Color iColor)
        {
            var aObject = EffectPool.Get("SwingArc", CreateMeshHolder);
            aObject.transform.SetPositionAndRotation(iCenter, iRotation);
            var aArc = Fx<SwingArcFx>(aObject);
            aArc.Play(iRadius, iArcDegrees, iThickness, iDirection, iColor);
            return aArc;
        }

        /// <summary>アーマーが砕けた欠片: 大きめの破片が勢いよく飛び散る</summary>
        public void SpawnShards(Vector3 iPosition, Color iColor, int iCount, float iSize)
        {
            for (int aIndex = 0; aIndex < iCount; aIndex++)
            {
                var aObject = EffectPool.Get("ArmorShard", () => CreateShape(PrimitiveType.Cube));
                aObject.GetComponent<Renderer>().material.color = Color.Lerp(iColor, Color.white, Random.Range(0f, 0.3f));
                aObject.transform.position = iPosition + Random.insideUnitSphere * 0.3f;
                aObject.transform.rotation = Random.rotation;
                aObject.transform.localScale = new Vector3(iSize, iSize * 0.4f, iSize * Random.Range(0.6f, 1.2f));
                Vector3 aVelocity = Random.onUnitSphere * Random.Range(6f, 10f);
                aVelocity.y = Mathf.Abs(aVelocity.y) + 3f;
                Fx<DebrisFx>(aObject).Play(aVelocity, Random.Range(0.5f, 0.8f), null);
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
            var aObject = EffectPool.Get("Slash", () => CreateShape(PrimitiveType.Cube));
            Vector3 aForward = iDirection.sqrMagnitude > 1e-4f ? iDirection.normalized : Vector3.forward;
            // 攻撃の向きに直角な面で、斜めに走る線にする
            aObject.transform.position = iPosition;
            aObject.transform.rotation = Quaternion.LookRotation(aForward) * Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f));
            Fx<SlashFx>(aObject).Play(iLength, Color.Lerp(iColor, Color.white, 0.5f));
        }

        public void SpawnShockwave(Vector3 iPosition, float iRadius, Color iColor)
        {
            var aObject = EffectPool.Get("Shockwave", () => CreateShape(PrimitiveType.Cylinder));
            aObject.transform.position = new Vector3(iPosition.x, Mathf.Max(0.05f, iPosition.y), iPosition.z);
            Fx<ShockwaveFx>(aObject).Play(iRadius, iColor);
        }
    }
}
