using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 斬撃の弧(三日月形の光)。振り始めから一気に弧が伸び(SweepTime)、そのあと根元から消えていく。
    /// 弧は自分の XZ 平面(+Z が正面)に描く。縦の振りは、呼ぶ側で 90 度回して置く。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SwingArcFx : MonoBehaviour
    {
        const int mSegments = 24;
        /// <summary>弧が伸びきるまでの時間</summary>
        public const float SweepTime = 0.06f;
        /// <summary>出てから消えるまでの時間</summary>
        public const float Lifetime = 0.22f;

        Mesh mMesh;
        Vector3[] mVertices;
        float mRadius;
        float mArcDegrees;
        float mThickness;
        float mDirection;
        float mElapsed;

        /// <summary>iDirection: +1 で右から左へ、-1 で左から右へ振る</summary>
        public void Play(float iRadius, float iArcDegrees, float iThickness, float iDirection, Color iColor)
        {
            mRadius = iRadius;
            mArcDegrees = iArcDegrees;
            mThickness = iThickness;
            mDirection = iDirection >= 0f ? 1f : -1f;
            mElapsed = 0f;
            var aRenderer = GetComponent<MeshRenderer>();
            aRenderer.material.color = iColor;
            aRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aRenderer.receiveShadows = false;

            // 使い回すときは、形(頂点)だけ作り直す
            if (mMesh != null)
            {
                Rebuild(0f);
                return;
            }
            mMesh = new Mesh { name = "SwingArc" };
            mMesh.MarkDynamic();
            mVertices = new Vector3[(mSegments + 1) * 2];
            var aTriangles = new int[mSegments * 6];
            for (int aIndex = 0; aIndex < mSegments; aIndex++)
            {
                int aVertex = aIndex * 2;
                int aTriangle = aIndex * 6;
                // 上からも下からも見えるよう、表裏の両方を張る
                aTriangles[aTriangle] = aVertex;
                aTriangles[aTriangle + 1] = aVertex + 2;
                aTriangles[aTriangle + 2] = aVertex + 1;
                aTriangles[aTriangle + 3] = aVertex + 1;
                aTriangles[aTriangle + 4] = aVertex + 2;
                aTriangles[aTriangle + 5] = aVertex + 3;
            }
            Rebuild(0f);
            mMesh.triangles = DoubleSided(aTriangles);
            GetComponent<MeshFilter>().sharedMesh = mMesh;
        }

        static int[] DoubleSided(int[] iTriangles)
        {
            var aBoth = new int[iTriangles.Length * 2];
            iTriangles.CopyTo(aBoth, 0);
            for (int aIndex = 0; aIndex < iTriangles.Length; aIndex += 3)
            {
                aBoth[iTriangles.Length + aIndex] = iTriangles[aIndex];
                aBoth[iTriangles.Length + aIndex + 1] = iTriangles[aIndex + 2];
                aBoth[iTriangles.Length + aIndex + 2] = iTriangles[aIndex + 1];
            }
            return aBoth;
        }

        void Update()
        {
            mElapsed += Time.unscaledDeltaTime;
            if (mElapsed >= Lifetime)
            {
                EffectPool.Release(gameObject);
                return;
            }
            Rebuild(mElapsed);
        }

        void OnDestroy()
        {
            if (mMesh != null) Destroy(mMesh);
        }

        /// <summary>弧の形: 先端は一気に伸び、根元は後から追いかけて消える。両端は細く、真ん中が太い</summary>
        void Rebuild(float iTime)
        {
            float aHead = Mathf.Clamp01(iTime / SweepTime);
            float aTail = Mathf.Clamp01((iTime - SweepTime * 0.5f) / (Lifetime - SweepTime * 0.5f));
            aTail *= aTail;
            float aFade = 1f - Mathf.Clamp01((iTime - SweepTime) / (Lifetime - SweepTime));
            float aHalf = mArcDegrees * 0.5f;
            for (int aIndex = 0; aIndex <= mSegments; aIndex++)
            {
                float aT = Mathf.Lerp(aTail, aHead, aIndex / (float)mSegments);
                float aAngle = Mathf.Lerp(aHalf, -aHalf, aT) * mDirection * Mathf.Deg2Rad;
                // 先端側ほど太く、両端は細く
                float aWidth = mThickness * Mathf.Sin(Mathf.PI * aT) * (0.35f + 0.65f * aT) * (0.3f + 0.7f * aFade);
                var aDirection = new Vector3(Mathf.Sin(aAngle), 0f, Mathf.Cos(aAngle));
                mVertices[aIndex * 2] = aDirection * mRadius;
                mVertices[aIndex * 2 + 1] = aDirection * Mathf.Max(0f, mRadius - aWidth);
            }
            mMesh.vertices = mVertices;
            mMesh.RecalculateBounds();
        }
    }
}
