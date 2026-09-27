using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleFight
{
    /// <summary>アリーナのウェーブ進行、報酬(刻片)、ゲームオーバー/クリアとリスタート。</summary>
    public class ArenaDirector : MonoBehaviour
    {
        public enum GameState
        {
            Starting,
            Fighting,
            Intermission,
            Cleared,
            GameOver,
        }

        [Serializable]
        public class Wave
        {
            public string label = "WAVE";
            public EnemyProfile[] enemies;
        }

        [SerializeField] Wave[] waves;
        [SerializeField] Transform player;
        [SerializeField] PlayerInputReader input;
        [SerializeField] StyleRankSystem style;
        [SerializeField, Tooltip("敵の仮モデルに使うマテリアル(色は敵ごとに変える)")] Material enemyMaterial;
        [SerializeField] Vector3 arenaCenter;
        [SerializeField] float spawnRadius = 14f;
        [SerializeField] float firstWaveDelay = 1.5f;
        [SerializeField] float waveInterval = 2.5f;
        [SerializeField] float baseShardReward = 10f;

        readonly List<EnemyController> alive = new List<EnemyController>();
        Damageable playerDamageable;

        public GameState State { get; private set; }
        public int WaveNumber { get; private set; }
        public int WaveCount => waves != null ? waves.Length : 0;
        public string WaveLabel { get; private set; }
        public int Shards { get; private set; }
        public float ElapsedTime { get; private set; }

        public EnemyController Boss
        {
            get
            {
                foreach (var enemy in alive)
                {
                    if (enemy != null && !enemy.IsDead && enemy.Profile.isBoss) return enemy;
                }
                return null;
            }
        }

        void Start()
        {
            playerDamageable = player.GetComponent<Damageable>();
            playerDamageable.Died += OnPlayerDied;
            StartCoroutine(Run());
        }

        void OnDestroy()
        {
            if (playerDamageable != null) playerDamageable.Died -= OnPlayerDied;
        }

        IEnumerator Run()
        {
            State = GameState.Starting;
            yield return new WaitForSeconds(firstWaveDelay);

            for (int i = 0; i < WaveCount; i++)
            {
                WaveNumber = i + 1;
                WaveLabel = waves[i].label;
                Spawn(waves[i]);
                State = GameState.Fighting;
                while (alive.Count > 0) yield return null;

                if (i < WaveCount - 1)
                {
                    State = GameState.Intermission;
                    yield return new WaitForSeconds(waveInterval);
                }
            }
            State = GameState.Cleared;
        }

        void Spawn(Wave wave)
        {
            if (wave.enemies == null || wave.enemies.Length == 0) return;

            // プレイヤーの反対側を中心に扇状に出す
            Vector3 fromCenter = player.position - arenaCenter;
            float baseAngle = Mathf.Atan2(fromCenter.x, fromCenter.z) * Mathf.Rad2Deg + 180f;
            int count = wave.enemies.Length;
            float spread = count > 1 ? 150f / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                var profile = wave.enemies[i];
                if (profile == null) continue;
                float angle = (baseAngle - 75f * (count > 1 ? 1f : 0f) + spread * i) * Mathf.Deg2Rad;
                Vector3 position = arenaCenter + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * spawnRadius + Vector3.up * 0.1f;
                Vector3 look = arenaCenter - position;
                look.y = 0f;

                var enemy = EnemyFactory.Create(profile, position, Quaternion.LookRotation(look), player, enemyMaterial);
                alive.Add(enemy);
                enemy.Damageable.Died += () => OnEnemyDied(enemy);
            }
        }

        void OnEnemyDied(EnemyController enemy)
        {
            alive.Remove(enemy);
            float multiplier = style != null ? style.RewardMultiplier : 1f;
            Shards += Mathf.RoundToInt(baseShardReward * enemy.Profile.shardValue * multiplier);
        }

        void OnPlayerDied()
        {
            State = GameState.GameOver;
            StopAllCoroutines();
        }

        void Update()
        {
            if (State == GameState.Fighting || State == GameState.Intermission) ElapsedTime += Time.deltaTime;
            if (GamePause.IsPaused) return;
            if (input != null && input.Consume(PlayerAction.Restart)) Restart();
        }

        void Restart()
        {
            GamePause.Set(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
