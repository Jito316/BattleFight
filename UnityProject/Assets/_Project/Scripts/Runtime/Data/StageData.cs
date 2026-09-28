using System;
using System.Collections.Generic;
using UnityEngine;

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
    }

    [Serializable]
    public class StageWave
    {
        public string label = "WAVE";
        public EnemyProfile[] enemies;
    }

    [Serializable]
    public class EndlessEntry
    {
        public EnemyProfile profile;
        [Tooltip("ウェーブの予算をどれだけ使うか(強い敵ほど高い)")]
        public float cost = 1f;
        [Tooltip("このウェーブから出てくる")]
        [Min(1)] public int minWave = 1;
    }

    [CreateAssetMenu(menuName = "BattleFight/Stage Data", fileName = "Stage_")]
    public class StageData : ScriptableObject
    {
        public string displayName = "ステージ";
        [TextArea] public string description;
        public StageKind kind = StageKind.Waves;

        [Header("Waves")]
        public StageWave[] waves;

        [Header("Training")]
        public EnemyProfile[] trainingDummies;

        [Header("Endless")]
        public List<EndlessEntry> endlessPool = new List<EndlessEntry>();
        public EnemyProfile[] endlessBosses;
        [Tooltip("このウェーブごとにボスが出る")]
        [Min(1)] public int bossEvery = 5;
    }
}
