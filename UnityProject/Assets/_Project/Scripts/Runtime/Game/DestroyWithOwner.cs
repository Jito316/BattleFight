using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 実行時に作ったデータ(エリートの複製など)を、使っている GameObject がすべて消えたときに消す。
    /// 分裂の分身のように、1つの複製を複数の敵が使うこともある。
    /// </summary>
    public class DestroyWithOwner : MonoBehaviour
    {
        static readonly Dictionary<Object, int> mUsers = new Dictionary<Object, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => mUsers.Clear();

        Object mTarget;

        public static void Attach(GameObject iOwner, Object iData)
        {
            if (iOwner == null || iData == null) return;
            iOwner.AddComponent<DestroyWithOwner>().mTarget = iData;
            mUsers[iData] = mUsers.TryGetValue(iData, out int aCount) ? aCount + 1 : 1;
        }

        void OnDestroy()
        {
            if (mTarget == null || !mUsers.TryGetValue(mTarget, out int aCount)) return;
            if (aCount > 1)
            {
                mUsers[mTarget] = aCount - 1;
                return;
            }
            mUsers.Remove(mTarget);
            Destroy(mTarget);
        }
    }
}
