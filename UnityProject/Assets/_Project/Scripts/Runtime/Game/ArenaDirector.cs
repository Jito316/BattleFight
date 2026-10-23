using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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

        const int mMaxAliveEnemies = 16;

        [SerializeField, FormerlySerializedAs("stages")] StageData[] mStages;
        [SerializeField, FormerlySerializedAs("player")] Transform mPlayer;
        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;
        [SerializeField, FormerlySerializedAs("style")] StyleRankSystem mStyle;
        [SerializeField, Tooltip("敵の仮モデルに使うマテリアル(色は敵ごとに変える)"), FormerlySerializedAs("enemyMaterial")] Material mEnemyMaterial;
        [SerializeField, FormerlySerializedAs("arenaCenter")] Vector3 mArenaCenter;
        [SerializeField, FormerlySerializedAs("spawnRadius")] float mSpawnRadius = 14f;
        [SerializeField, FormerlySerializedAs("firstWaveDelay")] float mFirstWaveDelay = 1.5f;
        [SerializeField, FormerlySerializedAs("waveInterval")] float mWaveInterval = 2.5f;
        [SerializeField, FormerlySerializedAs("baseShardReward")] float mBaseShardReward = 10f;
        [SerializeField, FormerlySerializedAs("trainingRespawnDelay")] float mTrainingRespawnDelay = 1.5f;

        [Header("探索")]
        [SerializeField, Tooltip("アリーナの地形(探索中は隠す)"), FormerlySerializedAs("arenaRoot")] GameObject mArenaRoot;
        [SerializeField, Tooltip("探索マップ(探索のステージでだけ出す)"), FormerlySerializedAs("exploration")] ExplorationRegion mExploration;
        [SerializeField, FormerlySerializedAs("slots")] SkillSlotController mSlots;

        // シーンを読み直しても、選んだステージと「すぐ始めるか」を覚えておく
        static int mSelectedStage = -1;
        static bool mStartImmediately;

        readonly List<EnemyController> mAlive = new List<EnemyController>();
        readonly System.Random mRandom = new System.Random();
        Damageable mPlayerDamageable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mSelectedStage = -1;
            mStartImmediately = false;
            Instance = null;
            EliteChanceOverride = -1f;
        }

        /// <summary>エリートが出る確率を固定する(0 以上のとき。テストで出ないようにする/必ず出す)</summary>
        public static float EliteChanceOverride { get; set; } = -1f;

        /// <summary>ウェーブの敵がエリートになる確率(エンドレスは進むほど上がる)</summary>
        float EliteChance()
        {
            if (EliteChanceOverride >= 0f) return EliteChanceOverride;
            if (CurrentStage == null) return 0f;
            // 幕が進むほど出やすく(第1幕 約5% → 第5幕 約15%)。各幕の最初のウェーブでは出さない
            if (CurrentStage.Kind == StageKind.Waves) return WaveNumber <= 1 ? 0f : 0.03f + 0.025f * Mathf.Max(1, StageIndex);
            if (CurrentStage.Kind == StageKind.Endless) return Mathf.Min(0.3f, 0.08f + WaveNumber * 0.01f);
            return 0f;
        }

        public static ArenaDirector Instance { get; private set; }

        /// <summary>次に始めるステージを、このウェーブから始める(遊んで確かめる用。0 なら最初から)</summary>
        int mStartWaveIndex;
        /// <summary>プレイヤーが倒れない(遊んで確かめる用。ステージを始め直しても続く)</summary>
        bool mPlaytestGod;
        const float PlaytestGodHealth = 100000f;

        public IReadOnlyList<StageData> Stages => mStages;
        public StageData CurrentStage { get; private set; }
        public int StageIndex { get; private set; } = -1;
        public GameState State { get; private set; } = GameState.StageSelect;
        public int WaveNumber { get; private set; }
        /// <summary>エンドレスと訓練場では 0</summary>
        public int WaveCount => CurrentStage != null && CurrentStage.Kind == StageKind.Waves && CurrentStage.Waves != null ? CurrentStage.Waves.Length : 0;
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
                foreach (var aEnemy in mAlive)
                {
                    if (aEnemy != null && !aEnemy.IsDead && aEnemy.Profile.IsBoss) return aEnemy;
                }
                return null;
            }
        }

        void Awake()
        {
            Instance = this;
            if (mExploration != null) mExploration.gameObject.SetActive(false);
        }

        public bool IsExploring => CurrentStage != null && CurrentStage.Kind == StageKind.Exploration;
        public ExplorationRegion Exploration => mExploration;

        void Start()
        {
            mPlayerDamageable = mPlayer.GetComponent<Damageable>();
            mPlayerDamageable.Died += OnPlayerDied;

            if (mStartImmediately && mSelectedStage >= 0 && mSelectedStage < mStages.Length)
            {
                mStartImmediately = false;
                StartStage(mSelectedStage);
            }
            else
            {
                EnterStageSelect();
            }
        }

        void OnDestroy()
        {
            if (mPlayerDamageable != null) mPlayerDamageable.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
        }

        void EnterStageSelect()
        {
            State = GameState.StageSelect;
            ThirdPersonCamera.SetCursorLocked(false);
        }

        public void StartStage(int iIndex)
        {
            if (mStages == null || iIndex < 0 || iIndex >= mStages.Length) return;
            StopAllCoroutines();
            mSelectedStage = iIndex;
            StageIndex = iIndex;
            CurrentStage = mStages[iIndex];
            ElapsedTime = 0f;
            WaveNumber = 0;

            // ステージの見た目(色・光・霧)
            StageThemeApplier.Apply(CurrentStage.Theme, mArenaRoot != null ? mArenaRoot.transform : null, Sun(), Camera.main);

            // 探索のステージだけ探索マップを出し、アリーナの地形は隠す
            bool aExploring = IsExploring && mExploration != null;
            if (mArenaRoot != null) mArenaRoot.SetActive(!aExploring);
            if (aExploring) mExploration.Begin(this, mPlayer, mSlots, mEnemyMaterial);
            else if (mExploration != null) mExploration.gameObject.SetActive(false);
#if !UNITY_WEBGL || UNITY_EDITOR
            ThirdPersonCamera.SetCursorLocked(true);
#endif
            StartCoroutine(Run());
        }

        Light mSun;

        /// <summary>シーンの太陽(向きの光)</summary>
        Light Sun()
        {
            if (mSun != null) return mSun;
            foreach (var aLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (aLight.type != LightType.Directional) continue;
                mSun = aLight;
                break;
            }
            return mSun;
        }

        IEnumerator Run()
        {
            State = GameState.Starting;
            WaveLabel = CurrentStage.DisplayName;
            yield return new WaitForSeconds(mFirstWaveDelay);

            switch (CurrentStage.Kind)
            {
                case StageKind.Exploration:
                    // 敵は部屋ごとの EncounterZone が出す。ボスを倒すと CompleteExploration でクリア
                    WaveLabel = CurrentStage.DisplayName;
                    State = GameState.Fighting;
                    yield break;

                case StageKind.Training:
                    WaveLabel = CurrentStage.DisplayName;
                    if (CurrentStage.TrainingDummies != null) Spawn(CurrentStage.TrainingDummies);
                    State = GameState.Fighting;
                    yield break;

                case StageKind.Endless:
                    for (int aWave = 1; ; aWave++)
                    {
                        WaveNumber = aWave;
                        bool aBoss = EndlessWaveGenerator.IsBossWave(aWave, CurrentStage.BossEvery);
                        WaveLabel = aBoss ? "BOSS WAVE" : "WAVE";
                        Spawn(EndlessWaveGenerator.Generate(aWave, CurrentStage.EndlessPool, CurrentStage.EndlessBosses,
                            CurrentStage.BossEvery, mRandom));
                        State = GameState.Fighting;
                        while (mAlive.Count > 0) yield return null;

                        if (StageRecords.RecordWave(CurrentStage.name, aWave)) NewRecord = true;
                        State = GameState.Intermission;
                        yield return new WaitForSeconds(mWaveInterval);
                    }

                default:
                    int aFirstWave = Mathf.Clamp(mStartWaveIndex, 0, Mathf.Max(0, WaveCount - 1));
                    mStartWaveIndex = 0;
                    for (int aIndex = aFirstWave; aIndex < WaveCount; aIndex++)
                    {
                        WaveNumber = aIndex + 1;
                        WaveLabel = CurrentStage.Waves[aIndex].Label;
                        Spawn(CurrentStage.Waves[aIndex].Enemies);
                        State = GameState.Fighting;
                        while (mAlive.Count > 0) yield return null;

                        if (aIndex < WaveCount - 1)
                        {
                            State = GameState.Intermission;
                            yield return new WaitForSeconds(mWaveInterval);
                        }
                    }
                    State = GameState.Cleared;
                    NewRecord = StageRecords.RecordClear(CurrentStage.name, ElapsedTime, mStyle != null ? mStyle.HighestRank : 0);
                    ThirdPersonCamera.SetCursorLocked(false);
                    break;
            }
        }

        /// <summary>
        /// 遊んで確かめる用。ブラウザのコンソールから
        /// unityInstance.SendMessage('GameDirector', 'PlaytestCommand', '1 4') で第1幕のウェーブ4から、
        /// 'god' でプレイヤーが倒れなくなる。'room 2' で探索の 2 番目の部屋へ、'attack 0' でいちばん近い敵が 0 番目の攻撃を出す
        /// </summary>
        public void PlaytestCommand(string iCommand)
        {
            var aWords = (iCommand ?? "").Trim().Split(' ');
            if (aWords[0] == "god")
            {
                mPlaytestGod = true;
                return;
            }
            // 探索で、N 番目の部屋へ移る
            if (aWords[0] == "room" && aWords.Length > 1 && int.TryParse(aWords[1], out int aRoom))
            {
                if (ExplorationRegion.Active != null) Debug.Log($"[Playtest] room {aRoom}: {ExplorationRegion.Active.WarpToRoom(aRoom)} / {ExplorationRegion.Active.RoomCount}");
                return;
            }
            // いちばん近い敵に、N 番目の攻撃をすぐ出させる(パリィやジャスト回避の見え方を確かめる)
            if (aWords[0] == "attack" && aWords.Length > 1 && int.TryParse(aWords[1], out int aAttack))
            {
                EnemyController aNearest = null;
                foreach (var aEnemy in mAlive)
                {
                    if (aEnemy == null || aEnemy.IsDead) continue;
                    if (aNearest == null || (aEnemy.transform.position - mPlayer.position).sqrMagnitude < (aNearest.transform.position - mPlayer.position).sqrMagnitude) aNearest = aEnemy;
                }
                if (aNearest != null && aNearest.ForceAttack(aAttack))
                    Debug.Log($"[Playtest] attack {aNearest.Profile.DisplayName} {aNearest.CurrentAttack.Name} windup {aNearest.CurrentAttack.Windup}");
                return;
            }
            if (!int.TryParse(aWords[0], out int aStage)) return;
            mStartWaveIndex = aWords.Length > 1 && int.TryParse(aWords[1], out int aWave) ? aWave - 1 : 0;
            StartCoroutine(PlaytestStart(aStage));
        }

        /// <summary>読み込み直後に呼ばれても、Start(ステージ選択へ)のあとで始める</summary>
        IEnumerator PlaytestStart(int iStage)
        {
            yield return null;
            // 前のステージの敵が残らないように片付ける
            foreach (var aEnemy in mAlive) if (aEnemy != null) Destroy(aEnemy.gameObject);
            mAlive.Clear();
            StartStage(iStage);
        }

        void Spawn(IReadOnlyList<EnemyProfile> iEnemies)
        {
            if (iEnemies == null || iEnemies.Count == 0) return;

            // プレイヤーの反対側を中心に扇状に出す
            Vector3 aFromCenter = mPlayer.position - mArenaCenter;
            float aBaseAngle = Mathf.Atan2(aFromCenter.x, aFromCenter.z) * Mathf.Rad2Deg + 180f;
            int aCount = iEnemies.Count;
            float aSpread = aCount > 1 ? Mathf.Min(200f, 40f * aCount) : 0f;
            int aElites = 0;

            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                if (iEnemies[aIndex] == null) continue;
                float aAngle = (aBaseAngle - aSpread * 0.5f + (aCount > 1 ? aSpread * aIndex / (aCount - 1) : 0f)) * Mathf.Deg2Rad;
                Vector3 aPosition = mArenaCenter + new Vector3(Mathf.Sin(aAngle), 0f, Mathf.Cos(aAngle)) * mSpawnRadius;
                // ときどきエリート(巨大・俊足・分裂・障壁)になる
                // 1 ウェーブのエリートは上限まで(序盤の幕は 1 体まで)
                var aAffix = aElites < MaxElitesPerWave() ? EliteMaker.Roll(iEnemies[aIndex], EliteChance(), mRandom) : EliteAffix.None;
                if (aAffix != EliteAffix.None)
                {
                    aElites++;
                    SpawnElite(iEnemies[aIndex], aAffix, aPosition);
                }
                else
                {
                    SpawnEnemy(iEnemies[aIndex], aPosition);
                }
            }
        }

        /// <summary>1 ウェーブに出るエリートの上限(第1〜2幕は 1 体、そのあとは 2 体。エンドレスは 3 体)</summary>
        int MaxElitesPerWave()
        {
            if (EliteChanceOverride >= 0f) return int.MaxValue;
            if (CurrentStage != null && CurrentStage.Kind == StageKind.Endless) return 3;
            return StageIndex <= 2 ? 1 : 2;
        }

        /// <summary>ボスの召喚などで、ウェーブの途中に敵を足す(倒すまでウェーブは終わらない)</summary>
        public EnemyController SpawnExtra(EnemyProfile iProfile, Vector3 iPosition)
        {
            // 倒れずに消えた敵(ウェーブの外で消したものなど)は数えない
            mAlive.RemoveAll(iEnemy => iEnemy == null);
            if (iProfile == null || mAlive.Count >= mMaxAliveEnemies || State == GameState.GameOver) return null;
            if (IsExploring) return SpawnEnemy(iProfile, iPosition, true);
            // アリーナの外に出ないようにする
            Vector3 aOffset = iPosition - mArenaCenter;
            aOffset.y = 0f;
            if (aOffset.magnitude > 26f) iPosition = mArenaCenter + aOffset.normalized * 26f;
            return SpawnEnemy(iProfile, iPosition);
        }

        /// <summary>エリートを出す(元のデータは変えず、強化した複製を使う)</summary>
        public EnemyController SpawnElite(EnemyProfile iProfile, EliteAffix iAffix, Vector3 iPosition)
        {
            if (iProfile == null || State == GameState.GameOver) return null;
            var aElite = EliteMaker.Create(iProfile, iAffix);
            var aEnemy = SpawnEnemy(aElite, iPosition, IsExploring);
            // 敵が消えたら、複製したデータも消す
            DestroyWithOwner.Attach(aEnemy.gameObject, aElite);
            return aEnemy;
        }

        /// <summary>探索: 部屋の敵をその場所(高さもそのまま)に出す</summary>
        public EnemyController SpawnAt(EnemyProfile iProfile, Vector3 iPosition)
        {
            if (iProfile == null || State == GameState.GameOver) return null;
            return SpawnEnemy(iProfile, iPosition, true);
        }

        /// <summary>探索: 最後のボスを倒した</summary>
        public void CompleteExploration()
        {
            if (!IsExploring || State == GameState.GameOver) return;
            State = GameState.Cleared;
            NewRecord = StageRecords.RecordClear(CurrentStage.name, ElapsedTime, mStyle != null ? mStyle.HighestRank : 0);
            ThirdPersonCamera.SetCursorLocked(false);
        }

        EnemyController SpawnEnemy(EnemyProfile iProfile, Vector3 iPosition, bool iKeepHeight = false)
        {
            iPosition.y = iKeepHeight ? iPosition.y + 0.1f : 0.1f;
            Vector3 aLook = mPlayer.position - iPosition;
            aLook.y = 0f;
            var aRotation = aLook.sqrMagnitude > 0.01f ? Quaternion.LookRotation(aLook) : Quaternion.identity;

            var aEnemy = EnemyFactory.Create(iProfile, iPosition, aRotation, mPlayer, mEnemyMaterial);
            mAlive.Add(aEnemy);
            aEnemy.Damageable.Died += () => OnEnemyDied(aEnemy, iProfile, iPosition);
            return aEnemy;
        }

        void OnEnemyDied(EnemyController iEnemy, EnemyProfile iProfile, Vector3 iSpawnPosition)
        {
            mAlive.Remove(iEnemy);
            float aMultiplier = mStyle != null ? mStyle.RewardMultiplier : 1f;
            Shards += Mathf.RoundToInt(mBaseShardReward * iProfile.ShardValue * aMultiplier);

            if (iProfile.Elite != EliteAffix.None)
            {
                if (mStyle != null) mStyle.AddBonus(80f, GameText.Get("announce.elite_down"), new Color(1f, 0.82f, 0.3f));
                // 分裂: 小さな分身が2体出る
                if (iProfile.Elite == EliteAffix.Splitter && State != GameState.GameOver && iEnemy != null)
                {
                    var aSplit = EliteMaker.CreateSplit(iProfile.EliteSource != null ? iProfile.EliteSource : iProfile);
                    for (int aIndex = 0; aIndex < 2; aIndex++)
                    {
                        Vector3 aOffset = Quaternion.Euler(0f, 90f + 180f * aIndex, 0f) * iEnemy.transform.forward * 1.2f;
                        var aChild = SpawnEnemy(aSplit, iEnemy.transform.position + aOffset, true);
                        DestroyWithOwner.Attach(aChild.gameObject, aSplit);
                    }
                }
            }

            // ウェーブ最後の1体・ボスを倒した瞬間はスローモーションにして、カメラをその敵へ寄せる
            bool aWaveStage = CurrentStage != null && (CurrentStage.Kind == StageKind.Waves || CurrentStage.Kind == StageKind.Endless);
            bool aLastOfWave = aWaveStage && State == GameState.Fighting && mAlive.Count == 0;
            bool aBoss = iProfile != null && iProfile.IsBoss;
            if ((aLastOfWave || aBoss) && CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SlowMotion(0.25f, 0.9f);
                CombatFeedback.Instance.Shake(0.35f);
                if (iEnemy != null) CombatFeedback.Instance.KillCam(iEnemy.CenterPoint, aBoss ? 1.2f : 0.9f);
            }

            if (CurrentStage != null && CurrentStage.Kind == StageKind.Training && State == GameState.Fighting)
            {
                StartCoroutine(RespawnLater(iProfile, iSpawnPosition));
            }
        }

        IEnumerator RespawnLater(EnemyProfile iProfile, Vector3 iPosition)
        {
            yield return new WaitForSeconds(mTrainingRespawnDelay);
            if (State == GameState.Fighting) SpawnEnemy(iProfile, iPosition);
        }

        void OnPlayerDied()
        {
            State = GameState.GameOver;
            StopAllCoroutines();
            ThirdPersonCamera.SetCursorLocked(false);
        }

        void Update()
        {
            if (mPlaytestGod && mPlayerDamageable.MaxHealth < PlaytestGodHealth) mPlayerDamageable.Configure(Team.Player, PlaytestGodHealth, 0f, 0f);
            if (GamePause.IsPaused) return;
            if (State == GameState.Fighting || State == GameState.Intermission) ElapsedTime += Time.deltaTime;
            if (State == GameState.StageSelect) return;

            var aKeyboard = Keyboard.current;
            if (mInput != null && mInput.Consume(PlayerAction.Restart)) Retry();
            else if (aKeyboard != null && aKeyboard.tKey.wasPressedThisFrame) BackToStageSelect();
            else if (State == GameState.Cleared && aKeyboard != null && aKeyboard.nKey.wasPressedThisFrame && HasNextStage) NextStage();
        }

        int NextStageIndex()
        {
            for (int aStageIndex = StageIndex + 1; aStageIndex < mStages.Length; aStageIndex++)
            {
                if (mStages[aStageIndex] != null && mStages[aStageIndex].Kind == StageKind.Waves) return aStageIndex;
            }
            return -1;
        }

        public void Retry() => Reload(mSelectedStage, true);

        public void NextStage() => Reload(NextStageIndex(), true);

        public void BackToStageSelect() => Reload(mSelectedStage, false);

        static void Reload(int iStage, bool iStart)
        {
            mSelectedStage = iStage;
            mStartImmediately = iStart && iStage >= 0;
            GamePause.Set(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
