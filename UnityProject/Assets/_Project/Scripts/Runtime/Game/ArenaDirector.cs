using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BattleFight
{
    /// <summary>
    /// ステージの進行。ステージ選択 → ウェーブ → クリア/ゲームオーバー → 次へ/もう一度/選択に戻る。
    /// ステージの種類: 決まったウェーブ / 訓練場(人形が復活する) / エンドレス(自動生成)。
    /// </summary>
    public class ArenaDirector : MonoBehaviour
    {
        public enum GameState
        {
            StageSelect,
            Starting,
            Fighting,
            Intermission,
            Cleared,
            GameOver,
        }

        const int MaxAliveEnemies = 16;

        [SerializeField] StageData[] stages;
        [SerializeField] Transform player;
        [SerializeField] PlayerInputReader input;
        [SerializeField] StyleRankSystem style;
        [SerializeField, Tooltip("敵の仮モデルに使うマテリアル(色は敵ごとに変える)")] Material enemyMaterial;
        [SerializeField] Vector3 arenaCenter;
        [SerializeField] float spawnRadius = 14f;
        [SerializeField] float firstWaveDelay = 1.5f;
        [SerializeField] float waveInterval = 2.5f;
        [SerializeField] float baseShardReward = 10f;
        [SerializeField] float trainingRespawnDelay = 1.5f;

        // シーンを読み直しても、選んだステージと「すぐ始めるか」を覚えておく
        static int selectedStage = -1;
        static bool startImmediately;

        readonly List<EnemyController> alive = new List<EnemyController>();
        readonly System.Random random = new System.Random();
        Damageable playerDamageable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            selectedStage = -1;
            startImmediately = false;
            Instance = null;
        }

        public static ArenaDirector Instance { get; private set; }

        public IReadOnlyList<StageData> Stages => stages;
        public StageData CurrentStage { get; private set; }
        public int StageIndex { get; private set; } = -1;
        public GameState State { get; private set; } = GameState.StageSelect;
        public int WaveNumber { get; private set; }
        /// <summary>エンドレスと訓練場では 0</summary>
        public int WaveCount => CurrentStage != null && CurrentStage.kind == StageKind.Waves && CurrentStage.waves != null ? CurrentStage.waves.Length : 0;
        public string WaveLabel { get; private set; }
        public int Shards { get; private set; }
        public float ElapsedTime { get; private set; }
        /// <summary>このプレイで記録を更新したか(クリア画面の表示用)</summary>
        public bool NewRecord { get; private set; }
        public bool HasNextStage => StageIndex >= 0 && NextStageIndex() >= 0;

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

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            playerDamageable = player.GetComponent<Damageable>();
            playerDamageable.Died += OnPlayerDied;

            if (startImmediately && selectedStage >= 0 && selectedStage < stages.Length)
            {
                startImmediately = false;
                StartStage(selectedStage);
            }
            else
            {
                EnterStageSelect();
            }
        }

        void OnDestroy()
        {
            if (playerDamageable != null) playerDamageable.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
        }

        void EnterStageSelect()
        {
            State = GameState.StageSelect;
            ThirdPersonCamera.SetCursorLocked(false);
        }

        public void StartStage(int index)
        {
            if (stages == null || index < 0 || index >= stages.Length) return;
            StopAllCoroutines();
            selectedStage = index;
            StageIndex = index;
            CurrentStage = stages[index];
            ElapsedTime = 0f;
            WaveNumber = 0;
#if !UNITY_WEBGL || UNITY_EDITOR
            ThirdPersonCamera.SetCursorLocked(true);
#endif
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            State = GameState.Starting;
            WaveLabel = CurrentStage.displayName;
            yield return new WaitForSeconds(firstWaveDelay);

            switch (CurrentStage.kind)
            {
                case StageKind.Training:
                    WaveLabel = CurrentStage.displayName;
                    if (CurrentStage.trainingDummies != null) Spawn(CurrentStage.trainingDummies);
                    State = GameState.Fighting;
                    yield break;

                case StageKind.Endless:
                    for (int wave = 1; ; wave++)
                    {
                        WaveNumber = wave;
                        bool boss = EndlessWaveGenerator.IsBossWave(wave, CurrentStage.bossEvery);
                        WaveLabel = boss ? "BOSS WAVE" : "WAVE";
                        Spawn(EndlessWaveGenerator.Generate(wave, CurrentStage.endlessPool, CurrentStage.endlessBosses,
                            CurrentStage.bossEvery, random));
                        State = GameState.Fighting;
                        while (alive.Count > 0) yield return null;

                        if (StageRecords.RecordWave(CurrentStage.name, wave)) NewRecord = true;
                        State = GameState.Intermission;
                        yield return new WaitForSeconds(waveInterval);
                    }

                default:
                    for (int i = 0; i < WaveCount; i++)
                    {
                        WaveNumber = i + 1;
                        WaveLabel = CurrentStage.waves[i].label;
                        Spawn(CurrentStage.waves[i].enemies);
                        State = GameState.Fighting;
                        while (alive.Count > 0) yield return null;

                        if (i < WaveCount - 1)
                        {
                            State = GameState.Intermission;
                            yield return new WaitForSeconds(waveInterval);
                        }
                    }
                    State = GameState.Cleared;
                    NewRecord = StageRecords.RecordClear(CurrentStage.name, ElapsedTime, style != null ? style.HighestRank : 0);
                    ThirdPersonCamera.SetCursorLocked(false);
                    break;
            }
        }

        void Spawn(IReadOnlyList<EnemyProfile> enemies)
        {
            if (enemies == null || enemies.Count == 0) return;

            // プレイヤーの反対側を中心に扇状に出す
            Vector3 fromCenter = player.position - arenaCenter;
            float baseAngle = Mathf.Atan2(fromCenter.x, fromCenter.z) * Mathf.Rad2Deg + 180f;
            int count = enemies.Count;
            float spread = count > 1 ? Mathf.Min(200f, 40f * count) : 0f;

            for (int i = 0; i < count; i++)
            {
                if (enemies[i] == null) continue;
                float angle = (baseAngle - spread * 0.5f + (count > 1 ? spread * i / (count - 1) : 0f)) * Mathf.Deg2Rad;
                Vector3 position = arenaCenter + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * spawnRadius;
                SpawnEnemy(enemies[i], position);
            }
        }

        /// <summary>ボスの召喚などで、ウェーブの途中に敵を足す(倒すまでウェーブは終わらない)</summary>
        public EnemyController SpawnExtra(EnemyProfile profile, Vector3 position)
        {
            if (profile == null || alive.Count >= MaxAliveEnemies || State == GameState.GameOver) return null;
            // アリーナの外に出ないようにする
            Vector3 offset = position - arenaCenter;
            offset.y = 0f;
            if (offset.magnitude > 26f) position = arenaCenter + offset.normalized * 26f;
            return SpawnEnemy(profile, position);
        }

        EnemyController SpawnEnemy(EnemyProfile profile, Vector3 position)
        {
            position.y = 0.1f;
            Vector3 look = player.position - position;
            look.y = 0f;
            var rotation = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;

            var enemy = EnemyFactory.Create(profile, position, rotation, player, enemyMaterial);
            alive.Add(enemy);
            enemy.Damageable.Died += () => OnEnemyDied(enemy, profile, position);
            return enemy;
        }

        void OnEnemyDied(EnemyController enemy, EnemyProfile profile, Vector3 spawnPosition)
        {
            alive.Remove(enemy);
            float multiplier = style != null ? style.RewardMultiplier : 1f;
            Shards += Mathf.RoundToInt(baseShardReward * profile.shardValue * multiplier);

            if (CurrentStage != null && CurrentStage.kind == StageKind.Training && State == GameState.Fighting)
            {
                StartCoroutine(RespawnLater(profile, spawnPosition));
            }
        }

        IEnumerator RespawnLater(EnemyProfile profile, Vector3 position)
        {
            yield return new WaitForSeconds(trainingRespawnDelay);
            if (State == GameState.Fighting) SpawnEnemy(profile, position);
        }

        void OnPlayerDied()
        {
            State = GameState.GameOver;
            StopAllCoroutines();
            ThirdPersonCamera.SetCursorLocked(false);
        }

        void Update()
        {
            if (GamePause.IsPaused) return;
            if (State == GameState.Fighting || State == GameState.Intermission) ElapsedTime += Time.deltaTime;
            if (State == GameState.StageSelect) return;

            var keyboard = Keyboard.current;
            if (input != null && input.Consume(PlayerAction.Restart)) Retry();
            else if (keyboard != null && keyboard.tKey.wasPressedThisFrame) BackToStageSelect();
            else if (State == GameState.Cleared && keyboard != null && keyboard.nKey.wasPressedThisFrame && HasNextStage) NextStage();
        }

        int NextStageIndex()
        {
            for (int i = StageIndex + 1; i < stages.Length; i++)
            {
                if (stages[i] != null && stages[i].kind == StageKind.Waves) return i;
            }
            return -1;
        }

        public void Retry() => Reload(selectedStage, true);

        public void NextStage() => Reload(NextStageIndex(), true);

        public void BackToStageSelect() => Reload(selectedStage, false);

        static void Reload(int stage, bool start)
        {
            selectedStage = stage;
            startImmediately = start && stage >= 0;
            GamePause.Set(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
