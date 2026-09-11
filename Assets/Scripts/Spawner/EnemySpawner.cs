using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Prefab quái vật để spawn.")]
        public GameObject enemyPrefab;

        [Tooltip("Tỷ trọng xuất hiện (càng cao càng dễ ra so với các loại quái khác).")]
        [Range(1, 100)] public int spawnWeight = 10;

        [Tooltip("Thời gian sau bao nhiêu giây tính từ đầu trận thì loại quái này bắt đầu xuất hiện.")]
        public float unlockTime = 0f;
    }

    [Serializable]
    public class WaveConfig
    {
        [Tooltip("Tên hoặc tiêu đề đợt quái (ví dụ: 'Wave 1', 'Wave 2', 'Final Boss Wave').")]
        public string waveName = "Wave 1";

        [Header("Enemy Count & Limit")]
        [Tooltip("Tổng số lượng quái sẽ sinh ra trong toàn bộ Wave này (ví dụ: 10 con).")]
        public int totalEnemiesToSpawn = 10;

        [Tooltip("Số lượng quái TỐI ĐA cùng lúc được phép tồn tại trên màn hình trong Wave này (ví dụ: 5 con). Giúp giữ FPS mượt mà và kiểm soát độ khó.")]
        public int maxConcurrentEnemies = 5;

        [Header("Spawn Rate")]
        [Tooltip("Khoảng cách thời gian giữa các lượt sinh quái (giây).")]
        public float spawnInterval = 1.2f;

        [Tooltip("Số lượng quái sinh ra trong mỗi lượt.")]
        public int enemiesPerSpawn = 1;

        [Header("Enemy Pool (Tùy chọn)")]
        [Tooltip("Danh sách loại quái và tỷ lệ ra cho riêng Wave này (nếu để trống sẽ dùng danh sách mặc định chung).")]
        public List<EnemySpawnEntry> enemyPool = new List<EnemySpawnEntry>();

        [Header("Stat Multipliers (Độ khó leo thang)")]
        [Tooltip("Hệ số nhân Máu quái trong Wave này (1.0 = bình thường, 1.5 = +50% máu).")]
        public float healthMultiplier = 1.0f;

        [Tooltip("Hệ số nhân Sát thương va chạm trong Wave này.")]
        public float damageMultiplier = 1.0f;

        [Tooltip("Hệ số nhân Tốc độ di chuyển của quái trong Wave này.")]
        public float speedMultiplier = 1.0f;

        [Header("EXP & Reward Settings")]
        [Tooltip("Hệ số nhân Điểm kinh nghiệm (EXP) rơi từ quái trong Wave này (1.0 = bình thường, 2.0 = x2 EXP).")]
        public float expMultiplier = 1.0f;

        [Header("Boss Wave Settings (Wave Cuối Cùng)")]
        [Tooltip("Đánh dấu Wave này là Wave có Boss (mặc định là Wave cuối cùng).")]
        public bool isBossWave = false;

        [Tooltip("Prefab Boss riêng biệt xuất hiện trong Wave này (nếu để trống sẽ tự động tạo Super Boss to x2 từ quái thường).")]
        public GameObject customBossPrefab;

        [Tooltip("Số lượng Boss xuất hiện trong Wave này.")]
        public int bossCount = 1;

        [Tooltip("Thời gian trễ trước khi Boss xuất hiện kể từ khi Wave bắt đầu (giây).")]
        public float bossSpawnDelay = 1.0f;

        [Header("Break Time")]
        [Tooltip("Thời gian nghỉ/chờ (giây) sau khi dọn sạch Wave này trước khi bắt đầu Wave kế tiếp.")]
        public float breakDurationAfterWave = 3.0f;

        [Header("Wave Duration & Timer")]
        [Tooltip("Thời gian diễn ra của Wave này (giây). Vòng tròn hiển thị sẽ quay 360 độ theo thời gian này, hết giờ sẽ tự động bước vào Wave tiếp theo bất kể còn quái hay không.")]
        public float waveDuration = 30f;
    }

    public enum WaveState
    {
        NotStarted,
        InWave,
        WaveBreak,
        BossFight,
        StageVictory,
        GameOver
    }

    [Header("Player Target")]
    [Tooltip("Transform của Player để tính toán vị trí spawn xung quanh (tự tìm Tag 'Player' nếu để trống).")]
    [SerializeField] private Transform playerTransform;

    [Header("Global Default Enemy Prefabs")]
    [Tooltip("Danh sách các loại quái vật mặc định khi Wave không cấu hình danh sách riêng.")]
    [SerializeField] private List<EnemySpawnEntry> defaultEnemyList = new List<EnemySpawnEntry>();

    [Header("Chapter Integration")]
    [Tooltip("Cơ sở dữ liệu Chapter để tự động nạp cấu hình Wave của Chapter đang chọn (tự tìm nếu để trống).")]
    [SerializeField] private ChapterDatabase chapterDatabase;

    [Header("Wave System Configuration")]
    [Tooltip("Bật chế độ phân chia theo từng Wave.")]
    [SerializeField] private bool useWaveSystem = true;

    [Tooltip("Danh sách cấu hình từng Wave trong màn chơi. Có thể tinh chỉnh mọi thông số trực tiếp trên Inspector.")]
    [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();

    [Header("Spawn Area (Ring around Player)")]
    [Tooltip("Bán kính tối thiểu (khoảng cách tối thiểu ngoài camera). Mặc định 3m.")]
    [SerializeField] private float minSpawnRadius = 3f;

    [Tooltip("Bán kính tối đa để sinh quái vật xung quanh người chơi. Mặc định 5m.")]
    [SerializeField] private float maxSpawnRadius = 5f;

    [Header("Despawn / Optimization")]
    [Tooltip("Khoảng cách tối đa so với Player, quái vật đi xa hơn khoảng cách này sẽ tự động được thu hồi để tối ưu hiệu năng. Mặc định 10m.")]
    [SerializeField] private float maxDespawnDistance = 10f;

    [Tooltip("Chu kỳ kiểm tra thu hồi quái ở quá xa người chơi (giây). Mặc định 2.0s.")]
    [SerializeField] private float despawnCheckInterval = 2.0f;

    [Tooltip("Thời gian chờ trước khi quái bắt đầu xuất hiện ở đầu mỗi Wave (giây). Mặc định 1.0s.")]
    [SerializeField] private float initialWaveSpawnDelay = 1.0f;

    [Header("Debug & Diagnostics")]
    [Tooltip("Bật in log tọa độ tính toán và tọa độ thực tế khi spawn quái vật để kiểm tra lỗi dồn quái.")]
    [SerializeField] private bool enableSpawnDebugLogs = false;

    [Header("Stage Victory Default Rewards")]
    [Tooltip("Data Chips nhận được khi hoàn thành chapter (Chapter 1 mặc định 1000, mỗi Chapter sau +20%).")]
    [Min(0)] [SerializeField] private int stageVictoryDataChipReward = 1000;

    [Tooltip("Red Gems nhận được khi hoàn thành chapter (Chapter 1 mặc định 20, mỗi Chapter sau +20%).")]
    [Min(0)] [SerializeField] private int stageVictoryRedGemReward = 20;

    [Header("Artifact Drop Settings")]
    [Tooltip("Tổng số Hộp Mù Cổ Vật xuất hiện ngẫu nhiên trên bản đồ trong 1 Chapter (mặc định 5, có thể tùy chỉnh).")]
    [Min(0)] [SerializeField] private int maxArtifactDropsPerChapter = 5;

    [Tooltip("Khoảng cách tối thiểu so với Player khi xuất hiện hộp trên bản đồ (mặc định 8m, ngoài tầm nhìn camera để hiện chỉ báo dấu '?').")]
    [SerializeField] private float minArtifactSpawnDistance = 8f;

    [Tooltip("Khoảng cách tối đa so với Player khi không tìm thấy MapBoundary.")]
    [SerializeField] private float maxArtifactSpawnDistance = 18f;

    [Tooltip("Tự động phân bổ sinh Hộp Cổ Vật ngẫu nhiên trên bản đồ xuyên suốt các Wave trong Chapter.")]
    [SerializeField] private bool autoDistributeArtifactSpawnsAcrossWaves = false;

    [Header("Artifact Timed Spawning")]
    [Tooltip("Bật cơ chế sinh Hộp Cổ Vật ngẫu nhiên theo thời gian (10-15s đầu trận, sau đó 20-60s mỗi lần). Mặc định BẬT.")]
    [SerializeField] private bool useTimedArtifactSpawning = true;

    [Tooltip("Thời gian tối thiểu chờ Hộp Cổ Vật đầu tiên xuất hiện khi vào trận đấu (giây). Mặc định 10s.")]
    [SerializeField] private float minInitialArtifactDelay = 10f;

    [Tooltip("Thời gian tối đa chờ Hộp Cổ Vật đầu tiên xuất hiện khi vào trận đấu (giây). Mặc định 15s.")]
    [SerializeField] private float maxInitialArtifactDelay = 15f;

    [Tooltip("Thời gian chờ tối thiểu để xuất hiện lại Hộp Cổ Vật tiếp theo (giây). Mặc định 20s.")]
    [SerializeField] private float minArtifactRespawnInterval = 20f;

    [Tooltip("Thời gian chờ tối đa để xuất hiện lại Hộp Cổ Vật tiếp theo (giây). Mặc định 60s.")]
    [SerializeField] private float maxArtifactRespawnInterval = 60f;

    [Tooltip("Chờ người chơi nhặt hộp hiện tại rồi mới đếm ngược 20-60s để xuất hiện hộp tiếp theo.")]
    [SerializeField] private bool waitPickupBeforeRespawnTimer = true;

    [Tooltip("Số lượng Hộp Cổ Vật tối đa cùng tồn tại đồng thời trên bản đồ chưa được nhặt.")]
    [SerializeField] private int maxConcurrentActiveArtifactBoxes = 1;

    [Tooltip("Tỷ lệ rơi Hộp Mù Cổ Vật khi tiêu diệt Boss. Mặc định 0 = Đã loại bỏ, Cổ Vật chỉ xuất hiện theo thời gian trận đấu.")]
    [Range(0f, 1f)] [SerializeField] private float bossArtifactDropChance = 0f;

    [Tooltip("Tỷ lệ rơi Hộp Mù Cổ Vật khi tiêu diệt quái Tinh Anh (Elite Creep). Mặc định 0 = Đã loại bỏ.")]
    [Range(0f, 1f)] [SerializeField] private float eliteArtifactDropChance = 0f;

    [Tooltip("Tỷ lệ rơi Hộp Mù Cổ Vật ngẫu nhiên từ quái thường (Creep). Mặc định 0 = Đã loại bỏ.")]
    [Range(0f, 1f)] [SerializeField] private float normalCreepArtifactDropChance = 0f;

    [Tooltip("Tự động sinh 1 Hộp Mù Cổ Vật gần Player khi hoàn thành mỗi Wave.")]
    [SerializeField] private bool dropArtifactOnWaveClear = false;

    public int MaxArtifactDropsPerChapter { get => maxArtifactDropsPerChapter; set => maxArtifactDropsPerChapter = Mathf.Max(0, value); }
    public float MinArtifactSpawnDistance { get => minArtifactSpawnDistance; set => minArtifactSpawnDistance = Mathf.Max(1f, value); }
    public float MaxArtifactSpawnDistance { get => maxArtifactSpawnDistance; set => maxArtifactSpawnDistance = Mathf.Max(minArtifactSpawnDistance, value); }
    public bool AutoDistributeArtifactSpawnsAcrossWaves { get => autoDistributeArtifactSpawnsAcrossWaves; set => autoDistributeArtifactSpawnsAcrossWaves = value; }
    public bool UseTimedArtifactSpawning { get => useTimedArtifactSpawning; set => useTimedArtifactSpawning = value; }
    public float MinInitialArtifactDelay { get => minInitialArtifactDelay; set => minInitialArtifactDelay = Mathf.Max(0f, value); }
    public float MaxInitialArtifactDelay { get => maxInitialArtifactDelay; set => maxInitialArtifactDelay = Mathf.Max(minInitialArtifactDelay, value); }
    public float MinArtifactRespawnInterval { get => minArtifactRespawnInterval; set => minArtifactRespawnInterval = Mathf.Max(0f, value); }
    public float MaxArtifactRespawnInterval { get => maxArtifactRespawnInterval; set => maxArtifactRespawnInterval = Mathf.Max(minArtifactRespawnInterval, value); }
    public bool WaitPickupBeforeRespawnTimer { get => waitPickupBeforeRespawnTimer; set => waitPickupBeforeRespawnTimer = value; }
    public int MaxConcurrentActiveArtifactBoxes { get => maxConcurrentActiveArtifactBoxes; set => maxConcurrentActiveArtifactBoxes = Mathf.Max(1, value); }
    public float ArtifactSpawnTimer => artifactSpawnTimer;
    public bool IsInitialArtifactSpawn => isInitialArtifactSpawn;
    public float BossArtifactDropChance { get => bossArtifactDropChance; set => bossArtifactDropChance = Mathf.Clamp01(value); }
    public float EliteArtifactDropChance { get => eliteArtifactDropChance; set => eliteArtifactDropChance = Mathf.Clamp01(value); }
    public float NormalCreepArtifactDropChance { get => normalCreepArtifactDropChance; set => normalCreepArtifactDropChance = Mathf.Clamp01(value); }
    public bool DropArtifactOnWaveClear { get => dropArtifactOnWaveClear; set => dropArtifactOnWaveClear = value; }
    public int ArtifactsSpawnedInChapter => artifactsSpawnedInChapter;

    [Header("Gameplay Event Settings")]
    [Tooltip("Tổng số Sự Kiện Gameplay xuất hiện ngẫu nhiên trên bản đồ trong 1 Chapter (mặc định 3).")]
    [Min(0)] [SerializeField] private int maxGameplayEventsPerChapter = 3;

    [Tooltip("Bật cơ chế sinh Sự Kiện Gameplay ngẫu nhiên theo thời gian. Mặc định BẬT.")]
    [SerializeField] private bool useTimedGameplayEventSpawning = true;

    [Tooltip("Thời gian tối thiểu chờ Sự Kiện đầu tiên xuất hiện (giây). Mặc định 25s.")]
    [SerializeField] private float minInitialGameplayEventDelay = 25f;

    [Tooltip("Thời gian tối đa chờ Sự Kiện đầu tiên xuất hiện (giây). Mặc định 40s.")]
    [SerializeField] private float maxInitialGameplayEventDelay = 40f;

    [Tooltip("Thời gian chờ tối thiểu để xuất hiện Sự Kiện tiếp theo (giây). Mặc định 50s.")]
    [SerializeField] private float minGameplayEventRespawnInterval = 50f;

    [Tooltip("Thời gian chờ tối đa để xuất hiện Sự Kiện tiếp theo (giây). Mặc định 90s.")]
    [SerializeField] private float maxGameplayEventRespawnInterval = 90f;

    [Tooltip("Chờ người chơi tương tác xong sự kiện hiện tại rồi mới đếm ngược để xuất hiện sự kiện tiếp theo.")]
    [SerializeField] private bool waitEventCompleteBeforeRespawnTimer = true;

    [Tooltip("Số lượng Sự Kiện tối đa cùng tồn tại đồng thời trên bản đồ chưa được tương tác.")]
    [SerializeField] private int maxConcurrentActiveGameplayEvents = 1;

    public int MaxGameplayEventsPerChapter { get => maxGameplayEventsPerChapter; set => maxGameplayEventsPerChapter = Mathf.Max(0, value); }
    public bool UseTimedGameplayEventSpawning { get => useTimedGameplayEventSpawning; set => useTimedGameplayEventSpawning = value; }
    public int GameplayEventsSpawnedInChapter => gameplayEventsSpawnedInChapter;
    public float GameplayEventSpawnTimer => gameplayEventSpawnTimer;

    // Runtime tracking
    private int artifactsSpawnedInChapter = 0;
    private readonly HashSet<int> artifactSpawnedWaveIndices = new HashSet<int>();
    private float artifactSpawnTimer = -1f;
    private bool isInitialArtifactSpawn = true;
    private bool isArtifactRespawnTimerActive = false;

    private int gameplayEventsSpawnedInChapter = 0;
    private float gameplayEventSpawnTimer = -1f;
    private bool isInitialGameplayEventSpawn = true;
    private bool isGameplayEventRespawnTimerActive = false;
    private int currentWaveIndex = 0;
    private int enemiesSpawnedInWave = 0;
    private int enemiesKilledInWave = 0;
    private int bossesSpawnedInWave = 0;
    private int bossesKilledInWave = 0;

    private float spawnTimer;
    private float gameTimer;
    private float breakTimer;
    private float bossSpawnTimer;
    private float despawnCheckTimer;
    private float waveElapsedTime;
    private bool isStageCompleted;

    private WaveState currentState = WaveState.NotStarted;

    private readonly List<EnemyHealth> activeEnemies = new List<EnemyHealth>();
    private readonly List<EnemyHealth> activeBosses = new List<EnemyHealth>();
    private readonly List<EnemySpawnEntry> reusableAvailableEntries = new List<EnemySpawnEntry>();

    // Events for UI & Audio
    public event Action<int, int> OnWaveStarted; // (currentWave 1-based, totalWaves)
    public event Action<int, int, int> OnWaveProgressChanged; // (killsInWave, totalInWave, activeAlive)
    public event Action<float, float> OnWaveTimeProgressUpdated; // (progress 0..1, timeRemaining)
    public event Action<int> OnWaveCompleted; // (completedWave 1-based)
    public event Action<GameObject> OnBossSpawned; // (bossObj)
    public event Action OnBossDefeated;
    public event Action OnStageVictory;

    // Public Getters for UI & Tests
    public WaveState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWavesCount => waves != null && waves.Count > 0 ? waves.Count : 1;
    public int EnemiesSpawnedInWave => enemiesSpawnedInWave;
    public int EnemiesKilledInWave => enemiesKilledInWave;
    public int TotalEnemiesInCurrentWave => GetCurrentWaveConfig() != null ? GetCurrentWaveConfig().totalEnemiesToSpawn : 0;
    public int GetActiveEnemyCount() => activeEnemies.Count + activeBosses.Count;
    public int CurrentActiveEnemiesCount => activeEnemies.Count;
    public int CurrentActiveBossesCount => activeBosses.Count;
    public EnemyHealth CurrentActiveBoss => activeBosses.Count > 0 ? activeBosses[0] : null;
    public IReadOnlyList<EnemyHealth> ActiveBosses => activeBosses;
    public float GameTime => gameTimer;
    public float WaveElapsedTime => waveElapsedTime;
    public float CurrentWaveDuration => GetCurrentWaveConfig() != null ? GetCurrentWaveConfig().waveDuration : 30f;
    public float CurrentWaveTimeProgress => CurrentWaveDuration > 0f ? Mathf.Clamp01(waveElapsedTime / CurrentWaveDuration) : 0f;
    public float BreakTimeRemaining => Mathf.Max(0f, breakTimer);
    public bool IsStageCompleted => isStageCompleted;
    public int StageVictoryDataChipReward => stageVictoryDataChipReward;
    public int StageVictoryRedGemReward => stageVictoryRedGemReward;
    public IReadOnlyList<WaveConfig> Waves => waves;
    public float MinSpawnRadius { get => minSpawnRadius; set => minSpawnRadius = Mathf.Max(0.5f, value); }
    public float MaxSpawnRadius { get => maxSpawnRadius; set => maxSpawnRadius = Mathf.Max(minSpawnRadius, value); }
    public float MaxDespawnDistance { get => maxDespawnDistance; set => maxDespawnDistance = Mathf.Max(maxSpawnRadius, value); }
    public float DespawnCheckInterval { get => despawnCheckInterval; set => despawnCheckInterval = Mathf.Max(0.1f, value); }
    public float InitialWaveSpawnDelay { get => initialWaveSpawnDelay; set => initialWaveSpawnDelay = Mathf.Max(0f, value); }

    public void StopSpawner()
    {
        currentState = WaveState.NotStarted;
        enabled = false;
    }

    public void ResumeSpawner()
    {
        enabled = true;
        if (currentState == WaveState.NotStarted && waves != null && waves.Count > 0)
        {
            StartWave(currentWaveIndex);
        }
    }


    private void Awake()
    {
        LoadSelectedChapterWaves();
    }

    public void LoadSelectedChapterWaves()
    {
        if (chapterDatabase == null)
        {
#if UNITY_EDITOR
            chapterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
#endif
            if (chapterDatabase == null)
            {
                chapterDatabase = Resources.Load<ChapterDatabase>("ChapterDatabase");
            }
        }

        int selectedIndex = PlayerDataService.SelectedChapterIndex;
        ChapterData currentChapter = chapterDatabase != null ? chapterDatabase.GetChapter(selectedIndex) : null;

        // Luôn nạp đúng phần thưởng Chapter: Chapter 1 cố định 1000 chips/20 gems, mỗi Chapter kế tiếp tăng 20%
        if (currentChapter != null)
        {
            stageVictoryDataChipReward = currentChapter.GetCalculatedDataChipReward();
            stageVictoryRedGemReward = currentChapter.GetCalculatedRedGemReward();
        }
        else
        {
            int chapterNum = selectedIndex + 1;
            stageVictoryDataChipReward = Mathf.RoundToInt(1000f * Mathf.Pow(1.2f, chapterNum - 1));
            stageVictoryRedGemReward = Mathf.RoundToInt(20f * Mathf.Pow(1.2f, chapterNum - 1));
        }

        if (CustomWaveConfigManager.HasCustomWaves(selectedIndex))
        {
            var customWaves = CustomWaveConfigManager.GetActiveWaves(selectedIndex);
            if (customWaves != null && customWaves.Count > 0)
            {
                waves = new List<WaveConfig>(customWaves);
                Debug.Log($"[EnemySpawner] 🛠️ Đã nạp {waves.Count} Custom Waves được tùy chỉnh từ MainMenu cho Chapter {selectedIndex + 1}! (Thưởng vượt ải: +{stageVictoryDataChipReward} Chips, +{stageVictoryRedGemReward} Gems)");
            }
        }
        else
        {
            if (currentChapter != null)
            {
                if (currentChapter.waves == null || currentChapter.waves.Count == 0 || currentChapter.waves.Count != currentChapter.totalWaves)
                {
                    currentChapter.GenerateWaves();
                }

                if (currentChapter.waves != null && currentChapter.waves.Count > 0)
                {
                    waves = new List<WaveConfig>(currentChapter.waves);
                }

                if (currentChapter.chapterEnemyPool != null && currentChapter.chapterEnemyPool.Count > 0)
                {
                    defaultEnemyList = new List<EnemySpawnEntry>(currentChapter.chapterEnemyPool);
                }

                if (currentChapter.chapterBossPrefab != null && waves.Count > 0)
                {
                    waves[waves.Count - 1].customBossPrefab = currentChapter.chapterBossPrefab;
                }

                Debug.Log($"[EnemySpawner] 🎮 Đã nạp thành công bộ Wave riêng của Chapter {currentChapter.chapterNumber}: '{currentChapter.chapterTitle}' ({waves.Count} waves, Thưởng vượt ải: +{stageVictoryDataChipReward} Chips, +{stageVictoryRedGemReward} Gems)!");
            }
            else if (waves == null || waves.Count == 0)
            {
                GenerateDefaultWaves(10);
            }
        }
    }

    public void SetChapterDatabaseForTesting(ChapterDatabase db)
    {
        chapterDatabase = db;
        LoadSelectedChapterWaves();
    }

    public void SetStageVictoryRewards(int dataChips, int redGems)
    {
        stageVictoryDataChipReward = Mathf.Max(0, dataChips);
        stageVictoryRedGemReward = Mathf.Max(0, redGems);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        ResetArtifactChapterCount();

        if (useWaveSystem && waves != null && waves.Count > 0)
        {
            StartWave(0);
        }
        else
        {
            currentState = WaveState.InWave;
        }
    }

    private void Update()
    {
        if (playerTransform == null || isStageCompleted)
            return;

        gameTimer += Time.deltaTime;
        despawnCheckTimer -= Time.deltaTime;

        if (useTimedArtifactSpawning)
        {
            UpdateTimedArtifactSpawning();
        }

        if (useTimedGameplayEventSpawning)
        {
            UpdateTimedGameplayEventSpawning();
        }

        if (despawnCheckTimer <= 0f)
        {
            despawnCheckTimer = despawnCheckInterval;
            CheckAndDespawnFarEnemies();
        }

#if UNITY_EDITOR
        // Phím tắt thử nghiệm nhanh trong Unity Editor: Nhấn 'B' để tạo ngay 1 Hộp Cổ Vật ngẫu nhiên trên bản đồ
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnRandomArtifactOnMap(bypassChapterLimit: true);
        }
        // Nhấn 'E' để tạo ngay 1 Sự Kiện Gameplay ngẫu nhiên trên bản đồ
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnRandomEventOnMap(bypassChapterLimit: true);
        }
#endif

        if (!useWaveSystem)
        {
            UpdateLegacyInfiniteSpawn();
            return;
        }

        UpdateWaveExecution();
    }

    private void UpdateWaveExecution()
    {
        switch (currentState)
        {
            case WaveState.WaveBreak:
                breakTimer -= Time.deltaTime;
                if (breakTimer <= 0f)
                {
                    StartNextWave();
                }
                break;

            case WaveState.InWave:
            case WaveState.BossFight:
                WaveConfig config = GetCurrentWaveConfig();
                if (config == null) return;

                waveElapsedTime += Time.deltaTime;
                float remainingTime = Mathf.Max(0f, config.waveDuration - waveElapsedTime);
                OnWaveTimeProgressUpdated?.Invoke(CurrentWaveTimeProgress, remainingTime);

                // Xử lý đếm ngược spawn Boss nếu là Boss wave
                if (config.isBossWave && bossesSpawnedInWave < config.bossCount)
                {
                    bossSpawnTimer -= Time.deltaTime;
                    if (bossSpawnTimer <= 0f)
                    {
                        SpawnBoss(config);
                    }
                }

                // Xử lý spawn quái thường của wave
                if (enemiesSpawnedInWave < config.totalEnemiesToSpawn)
                {
                    spawnTimer -= Time.deltaTime;
                    if (spawnTimer <= 0f)
                    {
                        spawnTimer = config.spawnInterval;
                        TrySpawnWaveBatch(config);
                    }
                }

                // Kiểm tra điều kiện hoàn thành Wave
                CheckWaveClearCondition(config);
                break;
        }
    }

    public WaveConfig GetCurrentWaveConfig()
    {
        if (waves == null || waves.Count == 0) return null;
        int clampedIndex = Mathf.Clamp(currentWaveIndex, 0, waves.Count - 1);
        return waves[clampedIndex];
    }

    public void StartWave(int waveIndex)
    {
        if (waves == null || waves.Count == 0) return;

        currentWaveIndex = Mathf.Clamp(waveIndex, 0, waves.Count - 1);
        WaveConfig config = waves[currentWaveIndex];

        enemiesSpawnedInWave = 0;
        enemiesKilledInWave = 0;
        bossesSpawnedInWave = 0;
        bossesKilledInWave = 0;
        waveElapsedTime = 0f;
        spawnTimer = initialWaveSpawnDelay; // Quái bắt đầu xuất hiện ngay sau 1 giây khi Wave bắt đầu
        bossSpawnTimer = config.bossSpawnDelay;

        currentState = config.isBossWave ? WaveState.BossFight : WaveState.InWave;

        Debug.Log($"[EnemySpawner] Bắt đầu {config.waveName} (Wave {currentWaveIndex + 1}/{waves.Count}): Thời lượng = {config.waveDuration}s, Tổng quái = {config.totalEnemiesToSpawn}, Giới hạn cùng lúc = {config.maxConcurrentEnemies}, Boss = {config.isBossWave}");

        OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Count);
        NotifyWaveProgress();

        // Kiểm tra cơ chế sinh Hộp Cổ Vật ngẫu nhiên trên bản đồ theo tiến trình Wave (khi không dùng timed spawning)
        if (!useTimedArtifactSpawning && ShouldSpawnArtifactOnWave(currentWaveIndex))
        {
            artifactSpawnedWaveIndices.Add(currentWaveIndex);
            SpawnRandomArtifactOnMap();
        }
    }

    private void StartNextWave()
    {
        if (currentWaveIndex + 1 < waves.Count)
        {
            StartWave(currentWaveIndex + 1);
        }
        else
        {
            TriggerStageVictory();
        }
    }

    private void TrySpawnWaveBatch(WaveConfig config)
    {
        if (config == null) return;

        for (int i = 0; i < config.enemiesPerSpawn; i++)
        {
            if (enemiesSpawnedInWave >= config.totalEnemiesToSpawn)
                break;

            if (activeEnemies.Count >= config.maxConcurrentEnemies)
                break;

            SpawnSingleWaveEnemy(config);
        }
    }

    private void SpawnSingleWaveEnemy(WaveConfig config)
    {
        GameObject prefabToSpawn = SelectEnemyPrefabForWave(config);
        if (prefabToSpawn == null)
            return;

        Vector2 spawnPosition = GetRandomSpawnPositionAroundPlayer();
        GameObject enemyObj = SpawnGameObject(prefabToSpawn, spawnPosition);
        if (enemyObj == null) return;

        if (enableSpawnDebugLogs)
        {
            Debug.Log($"[Spawn Test] Tọa độ tính toán: {spawnPosition} | Tọa độ thực tế của Enemy: {enemyObj.transform.position}");
        }

        enemiesSpawnedInWave++;

        ApplyEnemyModifiers(enemyObj, config.healthMultiplier, config.damageMultiplier, config.speedMultiplier, config.expMultiplier, false);

        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDeath -= HandleEnemyDeath;
            health.OnDeath += HandleEnemyDeath;

            if (!activeEnemies.Contains(health))
            {
                activeEnemies.Add(health);
            }
        }

        NotifyWaveProgress();
    }

    private void SpawnBoss(WaveConfig config)
    {
        if (config == null) return;

        bossesSpawnedInWave++;

        GameObject bossPrefab = config.customBossPrefab;
        if (bossPrefab == null)
        {
            bossPrefab = SelectEnemyPrefabForWave(config);
        }

        if (bossPrefab == null) return;

        Vector2 spawnPosition = GetRandomSpawnPositionAroundPlayer();
        GameObject bossObj = SpawnGameObject(bossPrefab, spawnPosition);
        if (bossObj == null) return;

        bossObj.name = $"Boss_{config.waveName}";

        // Tăng kích thước và chỉ số cho Boss
        bool isCustomBoss = config.customBossPrefab != null;
        float bossHealthMul = config.healthMultiplier * (isCustomBoss ? 1.0f : 8.0f);
        float bossDamageMul = config.damageMultiplier * (isCustomBoss ? 1.0f : 2.5f);
        float bossSpeedMul = config.speedMultiplier * (isCustomBoss ? 1.0f : 1.1f);
        float bossExpMul = config.expMultiplier * (isCustomBoss ? 2.0f : 5.0f);

        if (!isCustomBoss)
        {
            EnemyMovement em = bossObj.GetComponent<EnemyMovement>();
            BossMovement bm = bossObj.GetComponent<BossMovement>();
            if (em != null)
            {
                em.SetScaleMultiplier(1.8f);
            }
            else if (bm != null)
            {
                bm.SetScaleMultiplier(1.8f);
            }
            else
            {
                bossObj.transform.localScale *= 1.8f;
            }

            // Đổi màu viền đỏ/cam dữ dội cho Super Boss
            SpriteRenderer sr = bossObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1.0f, 0.4f, 0.4f, 1.0f);
            }
        }

        ApplyEnemyModifiers(bossObj, bossHealthMul, bossDamageMul, bossSpeedMul, bossExpMul, true);

        EnemyHealth health = bossObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDeath -= HandleBossDeath;
            health.OnDeath += HandleBossDeath;

            if (!activeBosses.Contains(health))
            {
                activeBosses.Add(health);
            }
        }

        Debug.Log($"[EnemySpawner] ⚠️ CẢNH BÁO: BOSS ĐÃ XUẤT HIỆN! ({bossObj.name})");
        OnBossSpawned?.Invoke(bossObj);
    }

    private void ApplyEnemyModifiers(GameObject enemyObj, float healthMul, float damageMul, float speedMul, float expMul, bool isBoss)
    {
        if (enemyObj == null) return;

        // Mục tiêu Player
        EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.SetTarget(playerTransform);
            movement.MoveSpeed = movement.BaseMoveSpeed * Mathf.Max(0.1f, speedMul);
        }

        BossMovement bossMovement = enemyObj.GetComponent<BossMovement>();
        if (bossMovement != null)
        {
            bossMovement.SetTarget(playerTransform);
            bossMovement.MoveSpeed = bossMovement.BaseMoveSpeed * Mathf.Max(0.1f, speedMul);
        }

        BossRangedAttack rangedAttack = enemyObj.GetComponent<BossRangedAttack>();
        if (rangedAttack != null)
        {
            rangedAttack.SetTarget(playerTransform);
            int scaledProjectileDamage = Mathf.RoundToInt(rangedAttack.BaseProjectileDamage * Mathf.Max(0.5f, damageMul));
            rangedAttack.SetProjectileDamage(scaledProjectileDamage);
        }

        // Máu quái & EXP
        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            int baseHealth = health.BaseMaxHealth;
            int scaledHealth = Mathf.RoundToInt(baseHealth * Mathf.Max(0.5f, healthMul));
            health.SetMaxHealth(scaledHealth, true);

            int baseExp = health.BaseExpReward;
            int scaledExp = Mathf.RoundToInt(baseExp * Mathf.Max(0f, expMul));
            health.SetExpReward(scaledExp);
        }

        // Sát thương va chạm
        EnemyContactDamage contactDamage = enemyObj.GetComponent<EnemyContactDamage>();
        if (contactDamage != null)
        {
            int baseDamage = contactDamage.BaseDamage;
            int scaledDamage = Mathf.RoundToInt(baseDamage * Mathf.Max(0.5f, damageMul));
            contactDamage.SetDamage(scaledDamage);
        }
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        if (enemy != null)
        {
            enemy.OnDeath -= HandleEnemyDeath;
            activeEnemies.Remove(enemy);
            enemiesKilledInWave++;
            NotifyWaveProgress();

            TryDropArtifactFromEnemy(enemy);
        }
    }

    private void TryDropArtifactFromEnemy(EnemyHealth enemy)
    {
        if (enemy == null) return;

        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        bool isElite = enemyComponent != null && enemyComponent.Type == EnemyType.EliteCreep;

        float chance = isElite ? eliteArtifactDropChance : normalCreepArtifactDropChance;
        if (chance > 0f && Random.value <= chance)
        {
            DropTable.SpawnArtifactBox(enemy.transform.position);
            Debug.Log($"[EnemySpawner] 🎁 Quái {(isElite ? "Elite" : "Thường")} đã rơi Hộp Cổ Vật (Artifact Box) tại {enemy.transform.position}!");
        }
    }

    private void HandleBossDeath(EnemyHealth boss)
    {
        if (boss != null)
        {
            boss.OnDeath -= HandleBossDeath;
            activeBosses.Remove(boss);
            bossesKilledInWave++;

            Debug.Log($"[EnemySpawner] 🎉 BOSS ĐÃ BỊ TIÊU DIỆT!");
            OnBossDefeated?.Invoke();

            if (bossArtifactDropChance > 0f && Random.value <= bossArtifactDropChance)
            {
                DropTable.SpawnArtifactBox(boss.transform.position);
                Debug.Log($"[EnemySpawner] 🎁 Boss đã rơi Hộp Cổ Vật (Artifact Box) tại {boss.transform.position}!");
            }

            // Đồng loạt tiêu diệt toàn bộ enemy trên sàn đấu khi Boss bị tiêu diệt
            KillAllActiveEnemies();
        }
    }

    private void CheckWaveClearCondition(WaveConfig config)
    {
        if (config == null) return;

        if (config.isBossWave)
        {
            // Với Wave Boss: Hoàn thành ải khi toàn bộ Boss bị tiêu diệt
            bool allBossesDead = bossesSpawnedInWave >= config.bossCount && activeBosses.Count == 0;
            if (allBossesDead)
            {
                CompleteCurrentWave(config);
            }
        }
        else
        {
            // Với Wave thường: Khi hết thời gian vòng quay (waveDuration), tự động hoàn thành và chuyển sang wave tiếp theo
            // không cần biết là đã tiêu diệt hết enemy chưa!
            if (waveElapsedTime >= config.waveDuration)
            {
                CompleteCurrentWave(config);
            }
        }
    }

    private void CompleteCurrentWave(WaveConfig config)
    {
        Debug.Log($"[EnemySpawner] ✅ Hoàn thành {config.waveName} (Wave {currentWaveIndex + 1}/{waves.Count})!");
        OnWaveCompleted?.Invoke(currentWaveIndex + 1);

        if (dropArtifactOnWaveClear && playerTransform != null)
        {
            Vector3 dropPos = playerTransform.position + (Vector3)Random.insideUnitCircle.normalized * 3.5f;
            DropTable.SpawnArtifactBox(dropPos);
            Debug.Log($"[EnemySpawner] 🎁 Hoàn thành Wave {currentWaveIndex + 1} - Đã rơi Hộp Cổ Vật tại {dropPos}!");
        }

        if (currentWaveIndex + 1 >= waves.Count)
        {
            TriggerStageVictory();
        }
        else
        {
            currentState = WaveState.WaveBreak;
            breakTimer = config.breakDurationAfterWave;
        }
    }

    #region Artifact Chapter Map Spawning
    /// <summary>
    /// Tính toán tọa độ ngẫu nhiên trên bản đồ (MapBoundary) cách xa Player tối thiểu minArtifactSpawnDistance
    /// để Hộp Cổ Vật xuất hiện ngoài màn hình và kích hoạt vòng tròn chỉ hướng dấu '?'.
    /// </summary>
    public Vector3 CalculateRandomArtifactMapPosition()
    {
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        if (MapBoundary.Instance != null)
        {
            Vector2 min = MapBoundary.Instance.MinBounds + Vector2.one * (MapBoundary.Instance.PlayerPadding + 1.5f);
            Vector2 max = MapBoundary.Instance.MaxBounds - Vector2.one * (MapBoundary.Instance.PlayerPadding + 1.5f);

            if (min.x > max.x) { float temp = min.x; min.x = max.x; max.x = temp; }
            if (min.y > max.y) { float temp = min.y; min.y = max.y; max.y = temp; }

            for (int attempt = 0; attempt < 15; attempt++)
            {
                float rx = Random.Range(min.x, max.x);
                float ry = Random.Range(min.y, max.y);
                Vector3 candidate = new Vector3(rx, ry, 0f);

                if (Vector3.Distance(candidate, playerPos) >= minArtifactSpawnDistance)
                {
                    return candidate;
                }
            }

            Vector2 clamped = MapBoundary.Instance.ClampSpawnPosition(playerPos + (Vector3)(Random.insideUnitCircle.normalized * minArtifactSpawnDistance), MapBoundary.Instance.PlayerPadding);
            return new Vector3(clamped.x, clamped.y, 0f);
        }

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        if (randomDir == Vector2.zero) randomDir = Vector2.right;
        float dist = Random.Range(minArtifactSpawnDistance, maxArtifactSpawnDistance);
        return playerPos + (Vector3)(randomDir * dist);
    }

    /// <summary>
    /// Sinh một Hộp Mù Cổ Vật ngẫu nhiên trên bản đồ, có kiểm soát tổng số lượng rơi trong 1 Chapter (mặc định là 5).
    /// </summary>
    public GameObject SpawnRandomArtifactOnMap(bool bypassChapterLimit = false)
    {
        if (!bypassChapterLimit && artifactsSpawnedInChapter >= maxArtifactDropsPerChapter)
        {
            Debug.Log($"[EnemySpawner] ℹ️ Đã đạt giới hạn tối đa {maxArtifactDropsPerChapter} Hộp Cổ Vật trong Chapter này ({artifactsSpawnedInChapter}/{maxArtifactDropsPerChapter}).");
            return null;
        }

        Vector3 spawnPos = CalculateRandomArtifactMapPosition();
        GameObject boxObj = DropTable.SpawnArtifactBox(spawnPos);
        artifactsSpawnedInChapter++;

        Debug.Log($"[EnemySpawner] 🎁 Đã sinh Hộp Cổ Vật ngẫu nhiên trên bản đồ ({artifactsSpawnedInChapter}/{maxArtifactDropsPerChapter}) tại {spawnPos}. Vòng tròn dấu '?' sẽ hiển thị ở mép màn hình!");
        return boxObj;
    }

    /// <summary>
    /// Kiểm tra xem Wave hiện tại có nằm trong lịch trình phân bổ sinh Hộp Cổ Vật không.
    /// </summary>
    public bool ShouldSpawnArtifactOnWave(int waveIndex)
    {
        if (!autoDistributeArtifactSpawnsAcrossWaves || maxArtifactDropsPerChapter <= 0) return false;
        if (artifactsSpawnedInChapter >= maxArtifactDropsPerChapter) return false;
        if (artifactSpawnedWaveIndices.Contains(waveIndex)) return false;

        int totalWaves = waves != null && waves.Count > 0 ? waves.Count : 10;
        if (maxArtifactDropsPerChapter >= totalWaves)
        {
            return true;
        }

        float step = (float)totalWaves / maxArtifactDropsPerChapter;
        for (int i = 0; i < maxArtifactDropsPerChapter; i++)
        {
            int targetWave = Mathf.FloorToInt(i * step);
            if (targetWave == waveIndex) return true;
        }
        return false;
    }

    /// <summary>
    /// Đặt lại biến đếm số lượng Hộp Cổ Vật khi bắt đầu Chapter mới hoặc bắt đầu trận đấu.
    /// Khởi tạo thời gian đếm ngược 10 - 15 giây cho lần xuất hiện đầu tiên.
    /// </summary>
    public void ResetArtifactChapterCount()
    {
        artifactsSpawnedInChapter = 0;
        artifactSpawnedWaveIndices.Clear();
        isInitialArtifactSpawn = true;
        isArtifactRespawnTimerActive = false;

        if (useTimedArtifactSpawning)
        {
            artifactSpawnTimer = Random.Range(minInitialArtifactDelay, maxInitialArtifactDelay);
            Debug.Log($"[EnemySpawner] ⏱️ Hộp Cổ Vật đầu tiên sẽ xuất hiện sau {artifactSpawnTimer:F1}s (khoảng {minInitialArtifactDelay}-{maxInitialArtifactDelay}s).");
        }
        else
        {
            artifactSpawnTimer = -1f;
        }
    }

    /// <summary>
    /// Cập nhật logic sinh Hộp Cổ Vật theo thời gian:
    /// - Khi vào trận đấu: đợi khoảng 10 - 15 giây mới xuất hiện lần đầu.
    /// - Sau đó muốn xuất hiện lại: đợi 20 - 60 giây.
    /// </summary>
    public void UpdateTimedArtifactSpawning(float? customDeltaTime = null)
    {
        if (!useTimedArtifactSpawning || isStageCompleted) return;
        if (artifactsSpawnedInChapter >= maxArtifactDropsPerChapter) return;

        float dt = customDeltaTime ?? Time.deltaTime;

        // 1. Trường hợp lần đầu vào trận đấu (chờ 10 - 15 giây)
        if (isInitialArtifactSpawn)
        {
            if (artifactSpawnTimer < 0f)
            {
                artifactSpawnTimer = Random.Range(minInitialArtifactDelay, maxInitialArtifactDelay);
            }

            artifactSpawnTimer -= dt;
            if (artifactSpawnTimer <= 0f)
            {
                isInitialArtifactSpawn = false;
                SpawnRandomArtifactOnMap();

                // Chuẩn bị cho các lần xuất hiện tiếp theo (20 - 60s)
                if (waitPickupBeforeRespawnTimer)
                {
                    isArtifactRespawnTimerActive = false;
                    artifactSpawnTimer = -1f; // Chờ người chơi nhặt hộp xong mới bắt đầu đếm 20-60s
                }
                else
                {
                    artifactSpawnTimer = Random.Range(minArtifactRespawnInterval, maxArtifactRespawnInterval);
                    isArtifactRespawnTimerActive = true;
                }
            }
            return;
        }

        // 2. Trường hợp các lần xuất hiện tiếp theo (chờ 20 - 60 giây)
        int activeBoxCount = ArtifactBoxPickup.ActiveBoxes != null ? ArtifactBoxPickup.ActiveBoxes.Count : 0;

        if (waitPickupBeforeRespawnTimer)
        {
            // Nếu vẫn còn hộp trên map chưa nhặt thì tiếp tục chờ người chơi nhặt
            if (activeBoxCount >= maxConcurrentActiveArtifactBoxes)
            {
                isArtifactRespawnTimerActive = false;
                artifactSpawnTimer = -1f;
                return;
            }

            // Hộp trên bản đồ đã được nhặt (hoặc số hộp < giới hạn cùng lúc): bắt đầu đếm ngược 20-60s
            if (!isArtifactRespawnTimerActive)
            {
                artifactSpawnTimer = Random.Range(minArtifactRespawnInterval, maxArtifactRespawnInterval);
                isArtifactRespawnTimerActive = true;
                Debug.Log($"[EnemySpawner] ⏱️ Hộp Cổ Vật tiếp theo sẽ xuất hiện lại sau {artifactSpawnTimer:F1}s (khoảng {minArtifactRespawnInterval}-{maxArtifactRespawnInterval}s).");
            }

            artifactSpawnTimer -= dt;
            if (artifactSpawnTimer <= 0f)
            {
                isArtifactRespawnTimerActive = false;
                SpawnRandomArtifactOnMap();
            }
        }
        else
        {
            if (!isArtifactRespawnTimerActive)
            {
                artifactSpawnTimer = Random.Range(minArtifactRespawnInterval, maxArtifactRespawnInterval);
                isArtifactRespawnTimerActive = true;
            }

            artifactSpawnTimer -= dt;
            if (artifactSpawnTimer <= 0f)
            {
                if (activeBoxCount < maxConcurrentActiveArtifactBoxes)
                {
                    isArtifactRespawnTimerActive = false;
                    SpawnRandomArtifactOnMap();
                    artifactSpawnTimer = Random.Range(minArtifactRespawnInterval, maxArtifactRespawnInterval);
                    isArtifactRespawnTimerActive = true;
                }
            }
        }
    }

    public void SetArtifactTimersForTesting(float initialMin, float initialMax, float respawnMin, float respawnMax, bool waitPickup = true)
    {
        useTimedArtifactSpawning = true;
        minInitialArtifactDelay = initialMin;
        maxInitialArtifactDelay = initialMax;
        minArtifactRespawnInterval = respawnMin;
        maxArtifactRespawnInterval = respawnMax;
        waitPickupBeforeRespawnTimer = waitPickup;
        ResetArtifactChapterCount();
    }

    /// <summary>
    /// Sinh một Sự Kiện Gameplay ngẫu nhiên trên bản đồ, có kiểm soát tổng số lượng rơi trong 1 Chapter (mặc định 3).
    /// </summary>
    public GameObject SpawnRandomEventOnMap(GameplayEventData specificEvent = null, bool bypassChapterLimit = false)
    {
        if (!bypassChapterLimit && gameplayEventsSpawnedInChapter >= maxGameplayEventsPerChapter)
        {
            Debug.Log($"[EnemySpawner] ℹ️ Đã đạt giới hạn tối đa {maxGameplayEventsPerChapter} Sự Kiện Gameplay trong Chapter này ({gameplayEventsSpawnedInChapter}/{maxGameplayEventsPerChapter}).");
            return null;
        }

        Vector3 spawnPos = CalculateRandomArtifactMapPosition();
        GameObject eventObj = new GameObject("GameplayEventPoint", typeof(CircleCollider2D), typeof(GameplayEventPickup));
        eventObj.transform.position = spawnPos;

        GameplayEventPickup pickup = eventObj.GetComponent<GameplayEventPickup>();
        if (specificEvent != null)
        {
            pickup.AssignedEvent = specificEvent;
        }
        else
        {
            pickup.AssignedEvent = GameplayEventDatabase.Instance.GetRandomEvent();
        }
        pickup.SetSpawnPosition(spawnPos);

        gameplayEventsSpawnedInChapter++;
        Debug.Log($"[EnemySpawner] ❓ Đã sinh Sự Kiện Gameplay ({pickup.AssignedEvent?.eventTitle ?? "Random"}) ({gameplayEventsSpawnedInChapter}/{maxGameplayEventsPerChapter}) tại {spawnPos}. Vòng tròn dấu '?' sẽ hiển thị ở mép màn hình!");
        return eventObj;
    }

    public void ResetGameplayEventChapterCount()
    {
        gameplayEventsSpawnedInChapter = 0;
        isInitialGameplayEventSpawn = true;
        isGameplayEventRespawnTimerActive = false;

        if (useTimedGameplayEventSpawning)
        {
            gameplayEventSpawnTimer = Random.Range(minInitialGameplayEventDelay, maxInitialGameplayEventDelay);
            Debug.Log($"[EnemySpawner] ⏱️ Sự Kiện Gameplay đầu tiên sẽ xuất hiện sau {gameplayEventSpawnTimer:F1}s.");
        }
        else
        {
            gameplayEventSpawnTimer = -1f;
        }
    }

    public void UpdateTimedGameplayEventSpawning(float? customDeltaTime = null)
    {
        if (!useTimedGameplayEventSpawning || isStageCompleted) return;
        if (gameplayEventsSpawnedInChapter >= maxGameplayEventsPerChapter) return;

        float dt = customDeltaTime ?? Time.deltaTime;

        if (isInitialGameplayEventSpawn)
        {
            if (gameplayEventSpawnTimer < 0f)
            {
                gameplayEventSpawnTimer = Random.Range(minInitialGameplayEventDelay, maxInitialGameplayEventDelay);
            }

            gameplayEventSpawnTimer -= dt;
            if (gameplayEventSpawnTimer <= 0f)
            {
                isInitialGameplayEventSpawn = false;
                SpawnRandomEventOnMap();

                if (waitEventCompleteBeforeRespawnTimer)
                {
                    isGameplayEventRespawnTimerActive = false;
                    gameplayEventSpawnTimer = -1f;
                }
                else
                {
                    gameplayEventSpawnTimer = Random.Range(minGameplayEventRespawnInterval, maxGameplayEventRespawnInterval);
                    isGameplayEventRespawnTimerActive = true;
                }
            }
            return;
        }

        int activeCount = GameplayEventPickup.ActiveEvents != null ? GameplayEventPickup.ActiveEvents.Count : 0;

        if (waitEventCompleteBeforeRespawnTimer)
        {
            if (activeCount >= maxConcurrentActiveGameplayEvents)
            {
                isGameplayEventRespawnTimerActive = false;
                gameplayEventSpawnTimer = -1f;
                return;
            }

            if (!isGameplayEventRespawnTimerActive)
            {
                gameplayEventSpawnTimer = Random.Range(minGameplayEventRespawnInterval, maxGameplayEventRespawnInterval);
                isGameplayEventRespawnTimerActive = true;
                Debug.Log($"[EnemySpawner] ⏱️ Sự Kiện Gameplay tiếp theo sẽ xuất hiện lại sau {gameplayEventSpawnTimer:F1}s.");
            }

            gameplayEventSpawnTimer -= dt;
            if (gameplayEventSpawnTimer <= 0f)
            {
                isGameplayEventRespawnTimerActive = false;
                SpawnRandomEventOnMap();
            }
        }
        else
        {
            if (activeCount >= maxConcurrentActiveGameplayEvents) return;

            if (!isGameplayEventRespawnTimerActive)
            {
                gameplayEventSpawnTimer = Random.Range(minGameplayEventRespawnInterval, maxGameplayEventRespawnInterval);
                isGameplayEventRespawnTimerActive = true;
            }

            gameplayEventSpawnTimer -= dt;
            if (gameplayEventSpawnTimer <= 0f)
            {
                if (activeCount < maxConcurrentActiveGameplayEvents)
                {
                    isGameplayEventRespawnTimerActive = false;
                    SpawnRandomEventOnMap();
                    gameplayEventSpawnTimer = Random.Range(minGameplayEventRespawnInterval, maxGameplayEventRespawnInterval);
                    isGameplayEventRespawnTimerActive = true;
                }
            }
        }
    }

    public void SetGameplayEventTimersForTesting(float initialMin, float initialMax, float respawnMin, float respawnMax, bool waitComplete = true)
    {
        useTimedGameplayEventSpawning = true;
        minInitialGameplayEventDelay = initialMin;
        maxInitialGameplayEventDelay = initialMax;
        minGameplayEventRespawnInterval = respawnMin;
        maxGameplayEventRespawnInterval = respawnMax;
        waitEventCompleteBeforeRespawnTimer = waitComplete;
        ResetGameplayEventChapterCount();
    }
    #endregion

    public void TriggerStageVictory()
    {
        if (isStageCompleted) return;

        isStageCompleted = true;
        currentState = WaveState.StageVictory;
        PlayerLevelController.Instance?.LockLevelUpsForVictory();

        Debug.Log($"[EnemySpawner] 🏆🏆 CHIẾN THẮNG MÀN CHƠI (STAGE CLEAR)! TOÀN BỘ WAVE ĐÃ ĐƯỢC CHINH PHỤC!");

        // Đồng loạt tiêu diệt toàn bộ quái vật còn lại trên bản đồ bằng animation Die & Fade out
        KillAllActiveEnemies();

        // Mở khóa Chapter kế tiếp nếu đang chơi màn cao nhất
        int currentSelected = PlayerDataService.SelectedChapterIndex;
        if (currentSelected >= PlayerDataService.UnlockedChapterIndex)
        {
            PlayerDataService.UnlockedChapterIndex = currentSelected + 1;
            Debug.Log($"[EnemySpawner] Đã mở khóa Chapter tiếp theo: {PlayerDataService.UnlockedChapterIndex + 1}");
        }

        // Tặng thưởng vượt ải
        ChipManager.AddDataChips(stageVictoryDataChipReward);
        ChipManager.AddRedGems(stageVictoryRedGemReward);

        OnStageVictory?.Invoke();
    }

    /// <summary>
    /// Đồng loạt tiêu diệt toàn bộ quái vật và quái phụ trên sàn đấu bằng animation Die & Fade out.
    /// Dùng khi Boss bị hạ gục hoặc khi hoàn thành ải / chiến thắng trận đấu.
    /// </summary>
    public void KillAllActiveEnemies()
    {
        // 1. Snapshot danh sách activeEnemies hiện tại để không bị lỗi collection modified
        List<EnemyHealth> enemiesToKill = new List<EnemyHealth>(activeEnemies);
        for (int i = 0; i < enemiesToKill.Count; i++)
        {
            EnemyHealth enemy = enemiesToKill[i];
            if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
            {
                enemy.InstantKill(true);
            }
        }

        // 2. Quét thêm bất kỳ quái nào trên Scene (phòng ngừa quái phụ/creep không nằm trong list)
        EnemyHealth[] allSceneEnemies = FindObjectsOfType<EnemyHealth>();
        for (int i = 0; i < allSceneEnemies.Length; i++)
        {
            EnemyHealth enemy = allSceneEnemies[i];
            if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
            {
                // Nếu đang trong wave boss và boss còn sống khác thì không diệt nhầm boss trừ khi là StageVictory
                if (currentState != WaveState.StageVictory && activeBosses.Contains(enemy))
                {
                    continue;
                }
                enemy.InstantKill(true);
            }
        }

        // 3. Tiêu hủy các viên đạn của quái/boss đang bay trên màn hình để người chơi không bị dính sát thương oan
        EnemyProjectile[] projectiles = FindObjectsOfType<EnemyProjectile>();
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] != null && projectiles[i].gameObject.activeInHierarchy)
            {
                projectiles[i].Despawn();
            }
        }
    }

    private void NotifyWaveProgress()
    {
        WaveConfig config = GetCurrentWaveConfig();
        int total = config != null ? config.totalEnemiesToSpawn : enemiesSpawnedInWave;
        OnWaveProgressChanged?.Invoke(enemiesKilledInWave, total, activeEnemies.Count);
    }

    private GameObject SpawnGameObject(GameObject prefab, Vector2 position)
    {
        if (PoolManager.Instance != null)
        {
            return PoolManager.Instance.Spawn(prefab, position, Quaternion.identity);
        }
        return Instantiate(prefab, position, Quaternion.identity);
    }

    private GameObject SelectEnemyPrefabForWave(WaveConfig config)
    {
        List<EnemySpawnEntry> pool = (config != null && config.enemyPool != null && config.enemyPool.Count > 0)
            ? config.enemyPool
            : defaultEnemyList;

        if (pool == null || pool.Count == 0)
            return null;

        reusableAvailableEntries.Clear();
        int totalWeight = 0;

        foreach (var entry in pool)
        {
            if (entry != null && entry.enemyPrefab != null && gameTimer >= entry.unlockTime)
            {
                reusableAvailableEntries.Add(entry);
                totalWeight += entry.spawnWeight;
            }
        }

        if (reusableAvailableEntries.Count == 0 || totalWeight <= 0)
            return pool[0]?.enemyPrefab;

        int randomWeight = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        foreach (var entry in reusableAvailableEntries)
        {
            accumulatedWeight += entry.spawnWeight;
            if (randomWeight < accumulatedWeight)
            {
                return entry.enemyPrefab;
            }
        }

        return reusableAvailableEntries[0].enemyPrefab;
    }

    private Camera cachedMainCamera;

    private Camera GetActiveCamera()
    {
        if (cachedMainCamera == null || !cachedMainCamera.isActiveAndEnabled)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
            {
                cachedMainCamera = FindObjectOfType<Camera>();
            }
        }
        return cachedMainCamera;
    }

    /// <summary>
    /// Kiểm tra một tọa độ có đang nằm trong khung nhìn của Camera (trên màn hình) hay không.
    /// </summary>
    public bool IsPositionInsideCameraView(Vector2 worldPos, float padding = 0.4f)
    {
        Camera cam = GetActiveCamera();
        if (cam == null)
        {
            Vector2 pPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
            return Mathf.Abs(worldPos.x - pPos.x) <= (3.2f + padding) && Mathf.Abs(worldPos.y - pPos.y) <= (5.5f + padding);
        }

        Vector2 camPos = (Vector2)cam.transform.position;
        float halfH = cam.orthographic ? cam.orthographicSize : 5.0f;
        float halfW = halfH * (cam.aspect > 0.01f ? cam.aspect : (9f / 16f));

        float minX = camPos.x - halfW - padding;
        float maxX = camPos.x + halfW + padding;
        float minY = camPos.y - halfH - padding;
        float maxY = camPos.y + halfH + padding;

        return worldPos.x >= minX && worldPos.x <= maxX && worldPos.y >= minY && worldPos.y <= maxY;
    }

    /// <summary>
    /// Tính toán vị trí sinh quái đảm bảo 100% NGOÀI MÀN HÌNH (không bao giờ sinh trong tầm mắt người chơi).
    /// </summary>
    private Vector2 GetRandomSpawnPositionAroundPlayer()
    {
        Camera cam = GetActiveCamera();
        Vector2 camPos = cam != null ? (Vector2)cam.transform.position : (playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero);

        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 5.0f;
        float halfW = halfH * (cam != null && cam.aspect > 0.01f ? cam.aspect : (9f / 16f));

        // Khoảng cách an toàn ngoài mép màn hình: tối thiểu 1.0m, tối đa 2.5m
        float minMargin = 1.0f;
        float maxMargin = 2.5f;

        // Xáo trộn ngẫu nhiên 4 hướng: 0=Trên, 1=Dưới, 2=Trái, 3=Phải
        int[] sides = new int[] { 0, 1, 2, 3 };
        for (int i = 0; i < sides.Length; i++)
        {
            int r = Random.Range(i, sides.Length);
            int tmp = sides[i];
            sides[i] = sides[r];
            sides[r] = tmp;
        }

        Vector2 selectedPos = Vector2.zero;
        bool foundValid = false;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            int side = sides[attempt % sides.Length];
            float margin = Random.Range(minMargin, maxMargin);
            Vector2 candidate = Vector2.zero;

            switch (side)
            {
                case 0: // Phía TRÊN màn hình
                    candidate.x = camPos.x + Random.Range(-halfW - margin, halfW + margin);
                    candidate.y = camPos.y + halfH + margin;
                    break;
                case 1: // Phía DƯỚI màn hình
                    candidate.x = camPos.x + Random.Range(-halfW - margin, halfW + margin);
                    candidate.y = camPos.y - halfH - margin;
                    break;
                case 2: // Phía TRÁI màn hình
                    candidate.x = camPos.x - halfW - margin;
                    candidate.y = camPos.y + Random.Range(-halfH - margin, halfH + margin);
                    break;
                case 3: // Phía PHẢI màn hình
                    candidate.x = camPos.x + halfW + margin;
                    candidate.y = camPos.y + Random.Range(-halfH - margin, halfH + margin);
                    break;
            }

            // Nếu có MapBoundary, clamp vào map
            if (MapBoundary.Instance != null)
            {
                candidate = MapBoundary.Instance.ClampSpawnPosition(candidate, 0.5f);
            }

            // KIỂM TRA BẮT BUỘC: Điểm sau khi giới hạn có thực sự nằm NGOÀI màn hình không?
            if (!IsPositionInsideCameraView(candidate, 0.3f))
            {
                selectedPos = candidate;
                foundValid = true;
                break;
            }
        }

        if (!foundValid)
        {
            // Trường hợp người chơi đứng sát góc bản đồ khiến 1-2 hướng bị clamp vào trong màn hình:
            // Tìm hướng đối diện với mép bản đồ gần nhất (hướng vào sâu trong lòng map)
            Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : camPos;
            Vector2 safeDir = Vector2.up;
            if (MapBoundary.Instance != null)
            {
                safeDir = (MapBoundary.Instance.MapCenter - playerPos).normalized;
                if (safeDir == Vector2.zero) safeDir = Vector2.up;
            }

            float safeRadius = Mathf.Max(halfH, halfW) + 2.0f;
            Vector2 fallback = camPos + safeDir * safeRadius;
            if (MapBoundary.Instance != null)
            {
                fallback = MapBoundary.Instance.ClampSpawnPosition(fallback, 0.5f);
            }

            if (IsPositionInsideCameraView(fallback, 0.2f))
            {
                fallback = camPos + safeDir * (Mathf.Max(halfH, halfW) + 1.5f);
            }

            selectedPos = fallback;
        }

        return selectedPos;
    }

    private void CheckAndDespawnFarEnemies()
    {
        if (playerTransform == null) return;

        Vector2 playerPos = playerTransform.position;
        float effectiveDespawnDist = Mathf.Max(maxDespawnDistance, 18.0f);
        float maxDistSqr = effectiveDespawnDist * effectiveDespawnDist;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = activeEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            Vector2 diff = (Vector2)enemy.transform.position - playerPos;
            if (diff.sqrMagnitude > maxDistSqr)
            {
                enemy.OnDeath -= HandleEnemyDeath;
                activeEnemies.RemoveAt(i);
                enemy.Despawn();
                // Giảm biến đếm để bù lượt spawn lại gần Player
                if (enemiesSpawnedInWave > enemiesKilledInWave)
                {
                    enemiesSpawnedInWave--;
                }
            }
        }
    }

    private void UpdateLegacyInfiniteSpawn()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = 1.5f;
            if (activeEnemies.Count < 30)
            {
                GameObject prefab = SelectEnemyPrefabForWave(null);
                if (prefab != null)
                {
                    GameObject enemyObj = SpawnGameObject(prefab, GetRandomSpawnPositionAroundPlayer());
                    if (enemyObj != null)
                    {
                        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
                        if (health != null)
                        {
                            health.OnDeath -= HandleEnemyDeath;
                            health.OnDeath += HandleEnemyDeath;
                            activeEnemies.Add(health);
                        }
                    }
                }
            }
        }
    }

    // ==========================================
    // Context Menu Wave Generators for Inspector
    // ==========================================

    [ContextMenu("Generate 10 Default Waves (Boss at Wave 10)")]
    public void Generate10DefaultWaves()
    {
        GenerateDefaultWaves(10);
    }

    [ContextMenu("Generate 5 Quick Waves (Boss at Wave 5)")]
    public void Generate5QuickWaves()
    {
        GenerateDefaultWaves(5);
    }

    public void GenerateDefaultWaves(int count)
    {
        count = Mathf.Max(1, count);
        waves = new List<WaveConfig>(count);

        for (int i = 0; i < count; i++)
        {
            int waveNum = i + 1;
            bool isLast = (waveNum == count);

            WaveConfig wave = new WaveConfig
            {
                waveName = isLast ? $"Wave {waveNum} - FINAL BOSS" : $"Wave {waveNum}",
                totalEnemiesToSpawn = isLast ? 15 + count * 2 : 6 + i * 3,
                maxConcurrentEnemies = isLast ? 8 : Mathf.Clamp(4 + i / 2, 4, 12),
                spawnInterval = Mathf.Max(0.6f, 1.6f - i * 0.1f),
                enemiesPerSpawn = i >= 4 ? 2 : 1,
                healthMultiplier = 1.0f + i * 0.15f,
                damageMultiplier = 1.0f + i * 0.1f,
                speedMultiplier = 1.0f + i * 0.04f,
                isBossWave = isLast,
                bossCount = 1,
                bossSpawnDelay = isLast ? 2.0f : 0f,
                breakDurationAfterWave = 2.0f,
                waveDuration = isLast ? 60f : 30f
            };

            waves.Add(wave);
        }

        Debug.Log($"[EnemySpawner] Đã tự động tạo {count} Wave cấu hình chuẩn. Wave {count} là Boss Wave!");
    }

    public void SetWavesForTesting(List<WaveConfig> testWaves)
    {
        waves = testWaves;
        currentWaveIndex = 0;
        isStageCompleted = false;
        currentState = WaveState.NotStarted;
    }

    public void SetPlayerForTesting(Transform player)
    {
        playerTransform = player;
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = GetActiveCamera();
        Vector3 center = cam != null ? cam.transform.position : (playerTransform != null ? playerTransform.position : transform.position);
        center.z = 0f;

        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 5.0f;
        float halfW = halfH * (cam != null && cam.aspect > 0.01f ? cam.aspect : (9f / 16f));

        // Khung nhìn màn hình Camera (Màu đỏ: Vùng cấm sinh quái - Tuyệt đối không sinh quái ở đây)
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
        Gizmos.DrawWireCube(center, new Vector3(halfW * 2f, halfH * 2f, 0f));

        // Vùng sinh quái an toàn ngoài màn hình (Màu xanh lá)
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.85f);
        Gizmos.DrawWireCube(center, new Vector3((halfW + 2.5f) * 2f, (halfH + 2.5f) * 2f, 0f));
    }
}
