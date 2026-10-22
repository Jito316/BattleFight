using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    public enum StageKind
    {
        /// <summary>決まったウェーブを最後まで倒すとクリア</summary>
        Waves,
        /// <summary>攻撃してこない人形が倒しても復活する。スキルを試す場所</summary>
        Training,
        /// <summary>ウェーブが自動生成され続ける。何ウェーブまで行けるか</summary>
        Endless,
        /// <summary>マップを探索してスキルを集める(メトロイドヴァニア)。スキルが進入制限を開く鍵になる</summary>
        Exploration,
    }

    [Serializable]
    public class StageWave
    {
        [FormerlySerializedAs("label")] public string Label = "WAVE";
        [FormerlySerializedAs("enemies")] public EnemyProfile[] Enemies;
    }

    [Serializable]
    public class EndlessEntry
    {
        [FormerlySerializedAs("profile")] public EnemyProfile Profile;
        [Tooltip("ウェーブの予算をどれだけ使うか(強い敵ほど高い)")]
        [FormerlySerializedAs("cost")] public float Cost = 1f;
        [Tooltip("このウェーブから出てくる")]
        [Min(1)] [FormerlySerializedAs("minWave")] public int MinWave = 1;
    }

    [CreateAssetMenu(menuName = "BattleFight/Stage Data", fileName = "Stage_")]
    public class StageData : ScriptableObject
    {
        [FormerlySerializedAs("displayName")] public string DisplayName = "ステージ";
        [TextArea] [FormerlySerializedAs("description")] public string Description;
        [FormerlySerializedAs("kind")] public StageKind Kind = StageKind.Waves;

        [Header("Waves")]
        [FormerlySerializedAs("waves")] public StageWave[] Waves;

        [Header("Training")]
        [FormerlySerializedAs("trainingDummies")] public EnemyProfile[] TrainingDummies;

        [Header("Endless")]
        [FormerlySerializedAs("endlessPool")] public List<EndlessEntry> EndlessPool = new List<EndlessEntry>();
        [FormerlySerializedAs("endlessBosses")] public EnemyProfile[] EndlessBosses;
        [Tooltip("このウェーブごとにボスが出る")]
        [Min(1)] [FormerlySerializedAs("bossEvery")] public int BossEvery = 5;
    }
}
