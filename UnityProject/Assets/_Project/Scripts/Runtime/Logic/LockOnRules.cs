using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>ロックオンの切り替え先の選び方</summary>
    public static class LockOnRules
    {
        /// <summary>
        /// 今の標的から見て、右(iDirection = +1)または左(-1)にいる候補のうち、いちばん角度が近いものの番号。
        /// 角度は自分(iOrigin)から見た水平の向きで比べる。いなければ -1
        /// </summary>
        public static int PickSide(Vector3 iOrigin, Vector3 iCurrent, IReadOnlyList<Vector3> iCandidates, int iDirection)
        {
            Vector3 aToCurrent = Flat(iCurrent - iOrigin);
            if (aToCurrent.sqrMagnitude < 1e-4f) return -1;
            int aBest = -1;
            float aBestAngle = float.MaxValue;
            for (int aIndex = 0; aIndex < iCandidates.Count; aIndex++)
            {
                Vector3 aToCandidate = Flat(iCandidates[aIndex] - iOrigin);
                if (aToCandidate.sqrMagnitude < 1e-4f) continue;
                // 右回りが +(上から見て時計回り)
                float aAngle = Vector3.SignedAngle(aToCurrent, aToCandidate, Vector3.up) * iDirection;
                if (aAngle <= 1f || aAngle >= aBestAngle) continue;
                aBestAngle = aAngle;
                aBest = aIndex;
            }
            return aBest;
        }

        static Vector3 Flat(Vector3 iVector) => new Vector3(iVector.x, 0f, iVector.z);
    }
}
