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
    [Tooltip("Bán kính tối thiểu (ngoài tầm nhìn camera để quái không hiện đột ngột trên màn hình).")]
    [SerializeField] private float minSpawnRadius = 6f;

    [Tooltip("Bán kính tối đa để sinh quái vật xung quanh người chơi.")]
    [SerializeField] private float maxSpawnRadius = 9f;

    [Header("Despawn / Optimization")]
    [Tooltip("Khoảng cách tối đa so với Player, quái thường đi quá xa sẽ tự thu hồi về Pool để spawn lại gần (Boss không bị despawn).")]
    [SerializeField] private float maxDespawnDistance = 20f;

    [Tooltip("Chu kỳ kiểm tra thu hồi quái ở quá xa người chơi (giây).")]
    [SerializeField] private float despawnCheckInterval = 1.5f;

    [Header("Debug & Diagnostics")]
    [Tooltip("Bật in log tọa độ tính toán và tọa độ thực tế khi spawn quái vật để kiểm tra lỗi dồn quái.")]
    [SerializeField] private bool enableSpawnDebugLogs = false;

    // Runtime tracking
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
    public IReadOnlyList<WaveConfig> Waves => waves;

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
        if (CustomWaveConfigManager.HasCustomWaves(selectedIndex))
        {
            var customWaves = CustomWaveConfigManager.GetActiveWaves(selectedIndex);
            if (customWaves != null && customWaves.Count > 0)
            {
                waves = new List<WaveConfig>(customWaves);
                Debug.Log($"[EnemySpawner] 🛠️ Đã nạp {waves.Count} Custom Waves được tùy chỉnh từ MainMenu cho Chapter {selectedIndex + 1}!");
            }
        }
        else
        {
            ChapterData currentChapter = chapterDatabase != null ? chapterDatabase.GetChapter(selectedIndex) : null;

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

                Debug.Log($"[EnemySpawner] 🎮 Đã nạp thành công bộ Wave riêng của Chapter {currentChapter.chapterNumber}: '{currentChapter.chapterTitle}' ({waves.Count} waves, Độ khó Chapter: x{currentChapter.chapterDifficultyMultiplier:F2})!");
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

        if (despawnCheckTimer <= 0f)
        {
            despawnCheckTimer = despawnCheckInterval;
            CheckAndDespawnFarEnemies();
        }

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
        spawnTimer = 0.2f; // Spawn ngay lượt đầu tiên sau 0.2s
        bossSpawnTimer = config.bossSpawnDelay;

        currentState = config.isBossWave ? WaveState.BossFight : WaveState.InWave;

        Debug.Log($"[EnemySpawner] Bắt đầu {config.waveName} (Wave {currentWaveIndex + 1}/{waves.Count}): Thời lượng = {config.waveDuration}s, Tổng quái = {config.totalEnemiesToSpawn}, Giới hạn cùng lúc = {config.maxConcurrentEnemies}, Boss = {config.isBossWave}");

        OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Count);
        NotifyWaveProgress();
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
            bossObj.transform.localScale *= 1.8f;

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

    public void TriggerStageVictory()
    {
        if (isStageCompleted) return;

        isStageCompleted = true;
        currentState = WaveState.StageVictory;

        Debug.Log($"[EnemySpawner] 🏆🏆 CHIẾN THẮNG MÀN CHƠI (STAGE CLEAR)! TOÀN BỘ WAVE ĐÃ ĐƯỢC CHINH PHỤC!");

        // Mở khóa Chapter kế tiếp nếu đang chơi màn cao nhất
        int currentSelected = PlayerDataService.SelectedChapterIndex;
        if (currentSelected >= PlayerDataService.UnlockedChapterIndex)
        {
            PlayerDataService.UnlockedChapterIndex = currentSelected + 1;
            Debug.Log($"[EnemySpawner] Đã mở khóa Chapter tiếp theo: {PlayerDataService.UnlockedChapterIndex + 1}");
        }

        // Tặng thưởng vượt ải
        ChipManager.AddDataChips(50);
        ChipManager.AddRedGems(10);

        OnStageVictory?.Invoke();
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

    private Vector2 GetRandomSpawnPositionAroundPlayer()
    {
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomRadius = Random.Range(minSpawnRadius, maxSpawnRadius);

        Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
        Vector2 rawSpawnPos = playerPos + offset;

        if (MapBoundary.Instance != null)
        {
            return MapBoundary.Instance.ClampSpawnPosition(rawSpawnPos);
        }

        return rawSpawnPos;
    }

    private void CheckAndDespawnFarEnemies()
    {
        if (playerTransform == null) return;

        Vector2 playerPos = playerTransform.position;
        float maxDistSqr = maxDespawnDistance * maxDespawnDistance;

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
        Transform target = playerTransform;
#if UNITY_EDITOR
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
#endif
        if (target == null) target = transform;

        // Vòng tròn xanh lá: Bán kính sinh quái tối thiểu (ngoài tầm nhìn camera)
        Gizmos.color = new Color(0f, 1f, 0f, 0.85f);
        Gizmos.DrawWireSphere(target.position, minSpawnRadius);

        // Vòng tròn đỏ: Bán kính sinh quái tối đa xung quanh Player
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
        Gizmos.DrawWireSphere(target.position, maxSpawnRadius);
    }
}
