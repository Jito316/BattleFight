using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 演出(火花・衝撃波・斬撃・残像など)の使い回し。消すかわりに見えなくしてしまっておき、次に同じ種類を出すときに使う。
    /// 毎回作って消すと、敵が多い場面でメモリの片付け(GC)が走り、WebGL で引っかかりやすいため。
    /// シーンを読み直すとしまっていた物も消えるので、取り出すときに消えた物は飛ばす。
    /// </summary>
    public static class EffectPool
    {
        /// <summary>種類ごとに、しまっておく数の上限(これを超えた分は消す)</summary>
        const int mMaxFreePerKey = 96;

        static readonly Dictionary<string, Stack<GameObject>> mFree = new Dictionary<string, Stack<GameObject>>();
        static Transform mRoot;

        /// <summary>新しく作った数・使い回した数(テスト用)</summary>
        public static int CreatedCount { get; private set; }
        public static int ReusedCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mFree.Clear();
            mRoot = null;
            CreatedCount = 0;
            ReusedCount = 0;
        }

        /// <summary>iKey の演出を 1 つ取り出す。しまってある物がなければ iCreate で作る</summary>
        public static GameObject Get(string iKey, Func<GameObject> iCreate)
        {
            if (mFree.TryGetValue(iKey, out var aStack))
            {
                while (aStack.Count > 0)
                {
                    var aObject = aStack.Pop();
                    if (aObject == null) continue;
                    aObject.transform.SetParent(null, false);
                    aObject.SetActive(true);
                    ReusedCount++;
                    return aObject;
                }
            }
            var aCreated = iCreate();
            aCreated.name = iKey;
            aCreated.AddComponent<PooledEffect>().Key = iKey;
            CreatedCount++;
            return aCreated;
        }

        /// <summary>使い終わった演出をしまう(この仕組みで作った物でなければ消す)</summary>
        public static void Release(GameObject iObject)
        {
            if (iObject == null) return;
            var aPooled = iObject.GetComponent<PooledEffect>();
            if (aPooled == null)
            {
                UnityEngine.Object.Destroy(iObject);
                return;
            }
            if (!mFree.TryGetValue(aPooled.Key, out var aStack))
            {
                aStack = new Stack<GameObject>();
                mFree[aPooled.Key] = aStack;
            }
            if (aStack.Count >= mMaxFreePerKey)
            {
                UnityEngine.Object.Destroy(iObject);
                return;
            }
            iObject.SetActive(false);
            if (mRoot == null) mRoot = new GameObject("EffectPool").transform;
            iObject.transform.SetParent(mRoot, false);
            aStack.Push(iObject);
        }
    }
}
