using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Prefab quái vật để spawn.")]
        public GameObject enemyPrefab;

        [Tooltip("Tỷ trọng xuất hiện (càng cao càng dễ ra so với các loại quái khác).")]
        [Range(1, 100)] public int spawnWeight = 10;

        [Tooltip("Thời gian sau bao nhiêu giây tính từ đầu trận thì loại quái này bắt đầu xuất hiện.")]
        public float unlockTime = 0f;
    }

    [Header("Player Target")]
    [Tooltip("Transform của Player để tính toán vị trí spawn xung quanh (tự tìm Tag 'Player' nếu để trống).")]
    [SerializeField] private Transform playerTransform;

    [Header("Enemy Prefabs")]
    [Tooltip("Danh sách các loại quái vật, tỷ lệ ra và mốc thời gian mở khóa.")]
    [SerializeField] private List<EnemySpawnEntry> enemyList = new List<EnemySpawnEntry>();

    [Header("Spawn Area (Ring around Player)")]
    [Tooltip("Bán kính tối thiểu (ngoài tầm nhìn camera để quái không hiện đột ngột trên màn hình).")]
    [SerializeField] private float minSpawnRadius = 10f;

    [Tooltip("Bán kính tối đa để sinh quái vật xung quanh người chơi.")]
    [SerializeField] private float maxSpawnRadius = 14f;

    [Header("Spawn Settings")]
    [Tooltip("Khoảng cách thời gian giữa các đợt sinh quái (giây).")]
    [SerializeField] private float baseSpawnInterval = 1.5f;

    [Tooltip("Số lượng quái sinh ra trong mỗi đợt.")]
    [SerializeField] private int enemiesPerSpawn = 2;

    [Tooltip("Giới hạn số lượng quái tối đa cùng lúc đang tồn tại trên bản đồ (giúp giữ mượt FPS).")]
    [SerializeField] private int maxEnemiesAlive = 60;

    [Header("Difficulty Scaling")]
    [Tooltip("Tăng độ khó (tần suất sinh quái) sau mỗi khoảng thời gian (giây).")]
    [SerializeField] private float difficultyScaleInterval = 30f;

    [Tooltip("Tỷ lệ giảm khoảng cách spawn sau mỗi mốc tăng độ khó (giúp quái ra nhanh hơn).")]
    [SerializeField, Range(0.01f, 0.5f)] private float spawnRateIncrease = 0.1f;

    [Tooltip("Khoảng thời gian spawn tối thiểu giữa các đợt quái (giới hạn tốc độ spawn tối đa).")]
    [SerializeField] private float minSpawnInterval = 0.3f;

    [Header("Despawn / Optimization")]
    [Tooltip("Khoảng cách tối đa so với Player, quái đi quá xa sẽ tự thu hồi về Pool để spawn lại gần.")]
    [SerializeField] private float maxDespawnDistance = 25f;

    [Tooltip("Chu kỳ kiểm tra thu hồi quái ở quá xa người chơi (giây).")]
    [SerializeField] private float despawnCheckInterval = 2f;

    private float spawnTimer;
    private float gameTimer;
    private float currentSpawnInterval;
    private float despawnCheckTimer;

    private readonly List<EnemyHealth> activeEnemies = new List<EnemyHealth>();

    public int CurrentActiveEnemiesCount => activeEnemies.Count;
    public float GameTime => gameTimer;

    private void Awake()
    {
        currentSpawnInterval = baseSpawnInterval;
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
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        gameTimer += Time.deltaTime;
        spawnTimer -= Time.deltaTime;
        despawnCheckTimer -= Time.deltaTime;

        UpdateDifficulty();

        if (spawnTimer <= 0f)
        {
            spawnTimer = currentSpawnInterval;
            TrySpawnWave();
        }

        if (despawnCheckTimer <= 0f)
        {
            despawnCheckTimer = despawnCheckInterval;
            CheckAndDespawnFarEnemies();
        }
    }

    private void UpdateDifficulty()
    {
        int difficultyStages = Mathf.FloorToInt(gameTimer / difficultyScaleInterval);
        currentSpawnInterval = Mathf.Max(
            minSpawnInterval,
            baseSpawnInterval * Mathf.Pow(1f - spawnRateIncrease, difficultyStages)
        );
    }

    private void TrySpawnWave()
    {
        if (enemyList == null || enemyList.Count == 0)
            return;

        for (int i = 0; i < enemiesPerSpawn; i++)
        {
            if (activeEnemies.Count >= maxEnemiesAlive)
                break;

            SpawnSingleEnemy();
        }
    }

    private void SpawnSingleEnemy()
    {
        GameObject prefabToSpawn = SelectRandomEnemyPrefab();
        if (prefabToSpawn == null)
            return;

        Vector2 spawnPosition = GetRandomSpawnPositionAroundPlayer();

        GameObject enemyObj;
        if (PoolManager.Instance != null)
        {
            enemyObj = PoolManager.Instance.Spawn(prefabToSpawn, spawnPosition, Quaternion.identity);
        }
        else
        {
            enemyObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }

        if (enemyObj == null) return;

        // Cài đặt mục tiêu Player trực tiếp cho AI
        EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.SetTarget(playerTransform);
        }

        // Đăng ký theo dõi trạng thái sống/chết của quái
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
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        if (enemy != null)
        {
            enemy.OnDeath -= HandleEnemyDeath;
            activeEnemies.Remove(enemy);
        }
    }

    private GameObject SelectRandomEnemyPrefab()
    {
        List<EnemySpawnEntry> availableEntries = new List<EnemySpawnEntry>();
        int totalWeight = 0;

        foreach (var entry in enemyList)
        {
            if (entry.enemyPrefab != null && gameTimer >= entry.unlockTime)
            {
                availableEntries.Add(entry);
                totalWeight += entry.spawnWeight;
            }
        }

        if (availableEntries.Count == 0 || totalWeight <= 0)
            return null;

        int randomWeight = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        foreach (var entry in availableEntries)
        {
            accumulatedWeight += entry.spawnWeight;
            if (randomWeight < accumulatedWeight)
            {
                return entry.enemyPrefab;
            }
        }

        return availableEntries[0].enemyPrefab;
    }

    private Vector2 GetRandomSpawnPositionAroundPlayer()
    {
        Vector2 playerPos = playerTransform.position;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomRadius = Random.Range(minSpawnRadius, maxSpawnRadius);

        Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
        return playerPos + offset;
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
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnRadius);
    }
}
