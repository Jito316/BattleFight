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
        static readonly Dictionary<Object, int> Users = new Dictionary<Object, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Users.Clear();

        Object target;

        public static void Attach(GameObject owner, Object data)
        {
            if (owner == null || data == null) return;
            owner.AddComponent<DestroyWithOwner>().target = data;
            Users[data] = Users.TryGetValue(data, out int count) ? count + 1 : 1;
        }

        void OnDestroy()
        {
            if (target == null || !Users.TryGetValue(target, out int count)) return;
            if (count > 1)
            {
                Users[target] = count - 1;
                return;
            }
            Users.Remove(target);
            Destroy(target);
        }
    }
}
