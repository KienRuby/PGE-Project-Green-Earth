using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa toàn bộ dữ liệu của 1 Chapter trong game:
/// Tên, số Chapter, ảnh nền xem trước, bóng boss (silhouette), số wave, năng lượng yêu cầu, câu thoại dẫn truyện
/// và BỘ WAVE RIÊNG BIỆT của Chapter đó (tự động nhận diện totalWaves và sinh ra số wave tương ứng với độ khó tăng 5-10% mỗi wave).
/// Thiết kế Data-Driven: Cho phép tạo Chapter mới và chỉnh sửa 100% thông số trực tiếp trên Inspector.
/// </summary>
[CreateAssetMenu(fileName = "Chapter_01_NewChapter", menuName = "PGE/Chapter Data", order = 10)]
public class ChapterData : ScriptableObject
{
    [Header("Basic Chapter Info")]
    [Tooltip("Số thứ tự Chapter hiển thị (ví dụ: 1, 2, 3, 4).")]
    public int chapterNumber = 1;

    [Tooltip("Tiêu đề Chapter (ví dụ: 'Dense Jungle 1', 'Toxic Swamp').")]
    public string chapterTitle = "Dense Jungle 1";

    [Tooltip("Số lượng Wave trong Chapter (ví dụ: 10). Khi thay đổi số này, hệ thống sẽ tự động sinh số wave tương ứng.")]
    public int totalWaves = 10;

    [Tooltip("Số Năng Lượng (Energy) tiêu hao khi bắt đầu màn chơi.")]
    public int energyCost = 10;

    [Header("Visual & Preview Assets")]
    [Tooltip("Ảnh nền xem trước của màn chơi (sàn rừng, sa mạc, phòng thí nghiệm,...). Dễ dàng thay thế sprite mới bất kỳ lúc nào.")]
    public Sprite previewBackground;

    [Tooltip("Bóng đen quái vật trùm (Boss Silhouette) hiển thị ở trung tâm khung xem trước. Dễ dàng thay thế sprite mới.")]
    public Sprite bossSilhouette;

    [Header("Map & Environment Configuration")]
    [Tooltip("Sprite sàn/bản đồ hiển thị trong màn chơi của Chapter này (nếu để trống sẽ dùng sprite nền mặc định).")]
    public Sprite mapGroundSprite;

    [Tooltip("Kích thước bản đồ (Rộng X, Cao Y theo đơn vị Unity).")]
    public Vector2 mapSize = new Vector2(40f, 40f);

    [Tooltip("Màu sắc / Tông màu chiếu sáng môi trường cho mặt sàn Chapter này.")]
    public Color mapColor = Color.white;

    [Tooltip("Chế độ vẽ Sprite sàn: Tiled (lặp lại texture gạch sàn) hoặc Simple (ảnh vẽ tay 1 tấm).")]
    public SpriteDrawMode groundDrawMode = SpriteDrawMode.Tiled;

    [Tooltip("Khoảng cách đệm an toàn từ mép bản đồ để nhân vật không bị lòi ra ngoài viền.")]
    [Range(0.1f, 3.0f)]
    public float playerBoundaryPadding = 0.6f;

    [Header("Story & Flavor")]
    [TextArea(2, 4)]
    [Tooltip("Câu thoại dẫn truyện / gợi mở xuất hiện phía dưới quái vật trùm.")]
    public string flavorText = "Going through the vines to look for you, mutants.";

    [Header("Scene Navigation")]
    [Tooltip("Tên Scene Gameplay chính cần nạp khi nhấn nút Start.")]
    public string gameplaySceneName = "GamePlay";

    [Tooltip("ID cấu hình bản đồ / cấp độ truyền cho Gameplay Scene nếu cần.")]
    public string levelConfigId = "dense_jungle_1";

    [Header("Progression & Lock Status")]
    [Tooltip("Đánh dấu Chapter này đang bị khóa.")]
    public bool isLocked = false;

    [Header("Chapter Victory Rewards (Phần thưởng khi vượt ải Chapter)")]
    [Tooltip("Số Chip Xanh (Data Chips) thưởng khi hoàn thành toàn bộ Chapter này.")]
    [Min(0)] public int victoryDataChipReward = 100;

    [Tooltip("Số Ngọc Đỏ (Red Gems) thưởng khi hoàn thành toàn bộ Chapter này.")]
    [Min(0)] public int victoryRedGemReward = 10;

    [Header("Wave Progression & Auto-Generation")]
    [Tooltip("Tự động tạo và cập nhật danh sách Wave dựa trên totalWaves khi thay đổi trong Inspector.")]
    public bool autoGenerateWaves = true;

    [Tooltip("Hệ số tăng độ khó cơ bản cho toàn bộ Chapter (ví dụ: Chapter 1 = 1.0, Chapter 2 = 1.25, Chapter 3 = 1.5, ...).")]
    [Range(0.5f, 5.0f)]
    public float chapterDifficultyMultiplier = 1.0f;

    [Tooltip("Tỷ lệ tăng sức mạnh quái qua mỗi Wave (mặc định 0.08 = 8%, nằm trong khoảng 5% - 10%).")]
    [Range(0.05f, 0.20f)]
    public float wavePowerGrowthRate = 0.08f;

    [Tooltip("Thời gian mỗi Wave bình thường (giây).")]
    public float defaultWaveDuration = 30f;

    [Tooltip("Thời gian Wave Boss cuối cùng (giây).")]
    public float finalBossWaveDuration = 60f;

    [Header("Chapter Specific Enemy Pool & Boss")]
    [Tooltip("Danh sách quái vật đặc trưng cho Chapter này (nếu để trống sẽ dùng quái mặc định).")]
    public List<EnemySpawner.EnemySpawnEntry> chapterEnemyPool = new List<EnemySpawner.EnemySpawnEntry>();

    [Tooltip("Prefab Boss đặc trưng xuất hiện ở Wave cuối cùng của Chapter này.")]
    public GameObject chapterBossPrefab;

    [Header("Chapter Waves (Customizable)")]
    [Tooltip("Danh sách cấu hình từng Wave riêng biệt cho Chapter này. Bạn có thể tự do chỉnh sửa từng thông số trực tiếp tại đây.")]
    public List<EnemySpawner.WaveConfig> waves = new List<EnemySpawner.WaveConfig>();

    public void GenerateWaves()
    {
        totalWaves = Mathf.Max(1, totalWaves);
        waves = new List<EnemySpawner.WaveConfig>(totalWaves);

        for (int i = 0; i < totalWaves; i++)
        {
            int waveNum = i + 1;
            bool isLast = (waveNum == totalWaves);

            // Mỗi wave tăng sức mạnh 5% - 10% (mặc định wavePowerGrowthRate = 8%)
            float wavePower = (1.0f + i * wavePowerGrowthRate) * chapterDifficultyMultiplier;

            EnemySpawner.WaveConfig wave = new EnemySpawner.WaveConfig
            {
                waveName = isLast ? $"Wave {waveNum} - FINAL BOSS" : $"Wave {waveNum}",
                totalEnemiesToSpawn = Mathf.RoundToInt((isLast ? 15 + totalWaves * 2 : 6 + i * 3) * Mathf.Sqrt(chapterDifficultyMultiplier)),
                maxConcurrentEnemies = Mathf.Clamp(Mathf.RoundToInt((isLast ? 8 : 4 + i / 2) * Mathf.Sqrt(chapterDifficultyMultiplier)), 4, 15),
                spawnInterval = Mathf.Max(0.5f, 1.6f - i * 0.08f),
                enemiesPerSpawn = i >= 4 ? 2 : 1,
                enemyPool = BuildWaveEnemyPool(i, totalWaves),
                healthMultiplier = isLast ? wavePower * 1.25f : wavePower,
                damageMultiplier = isLast ? (1.0f + i * (wavePowerGrowthRate * 0.75f)) * chapterDifficultyMultiplier * 1.2f : (1.0f + i * (wavePowerGrowthRate * 0.75f)) * chapterDifficultyMultiplier,
                speedMultiplier = 1.0f + i * 0.03f,
                expMultiplier = 1.0f + i * 0.05f,
                isBossWave = isLast,
                customBossPrefab = isLast ? chapterBossPrefab : null,
                bossCount = 1,
                bossSpawnDelay = isLast ? 2.0f : 0f,
                breakDurationAfterWave = 2.0f,
                waveDuration = isLast ? finalBossWaveDuration : defaultWaveDuration
            };

            waves.Add(wave);
        }

        Debug.Log($"[ChapterData] ✅ Đã tự động tạo {totalWaves} Wave cho Chapter '{chapterTitle}' (Độ khó Chapter: x{chapterDifficultyMultiplier:F2}, Tăng sức mạnh mỗi wave: +{wavePowerGrowthRate * 100:F1}%).");
    }

    /// <summary>
    /// Phân bổ tỷ trọng xuất hiện giữa các loại quái vật (Creep thường, BigCreep to,...) vào từng Wave.
    /// </summary>
    private List<EnemySpawner.EnemySpawnEntry> BuildWaveEnemyPool(int waveIndex, int totalWavesCount)
    {
        if (chapterEnemyPool == null || chapterEnemyPool.Count == 0)
        {
            return new List<EnemySpawner.EnemySpawnEntry>();
        }

        if (chapterEnemyPool.Count == 1)
        {
            return new List<EnemySpawner.EnemySpawnEntry>(chapterEnemyPool);
        }

        List<EnemySpawner.EnemySpawnEntry> wavePool = new List<EnemySpawner.EnemySpawnEntry>();
        var creepEntry = chapterEnemyPool[0];
        var bigCreepEntry = chapterEnemyPool[1];

        int creepWeight;
        int bigCreepWeight;

        if (waveIndex == 0 || waveIndex == 1) // Wave 1-2: 100% quái nhỏ
        {
            creepWeight = 100;
            bigCreepWeight = 0;
        }
        else if (waveIndex < 4) // Wave 3-4: 70% nhỏ, 30% to
        {
            creepWeight = 70;
            bigCreepWeight = 30;
        }
        else if (waveIndex < 7) // Wave 5-7: 50% nhỏ, 50% to (Chia đều)
        {
            creepWeight = 50;
            bigCreepWeight = 50;
        }
        else // Wave 8 trở đi: 40% nhỏ, 60% to
        {
            creepWeight = 40;
            bigCreepWeight = 60;
        }

        if (creepEntry != null && creepEntry.enemyPrefab != null)
        {
            wavePool.Add(new EnemySpawner.EnemySpawnEntry
            {
                enemyPrefab = creepEntry.enemyPrefab,
                spawnWeight = creepWeight,
                unlockTime = 0f
            });
        }

        if (bigCreepEntry != null && bigCreepEntry.enemyPrefab != null && bigCreepWeight > 0)
        {
            wavePool.Add(new EnemySpawner.EnemySpawnEntry
            {
                enemyPrefab = bigCreepEntry.enemyPrefab,
                spawnWeight = bigCreepWeight,
                unlockTime = 0f
            });
        }

        // Bổ sung các loại quái khác nếu có trong danh sách
        for (int k = 2; k < chapterEnemyPool.Count; k++)
        {
            var extraEntry = chapterEnemyPool[k];
            if (extraEntry != null && extraEntry.enemyPrefab != null)
            {
                wavePool.Add(new EnemySpawner.EnemySpawnEntry
                {
                    enemyPrefab = extraEntry.enemyPrefab,
                    spawnWeight = extraEntry.spawnWeight,
                    unlockTime = extraEntry.unlockTime
                });
            }
        }

        return wavePool;
    }

    [ContextMenu("Regenerate Waves From Settings")]
    public void ContextRegenerateWaves()
    {
        GenerateWaves();
    }

    private void OnValidate()
    {
        totalWaves = Mathf.Max(1, totalWaves);
        if (autoGenerateWaves)
        {
            if (waves == null || waves.Count != totalWaves)
            {
                GenerateWaves();
            }
        }
    }
}

/// <summary>
/// Quản lý dữ liệu thông số Wave được người chơi tùy chỉnh tại Main Menu và truyền sang Gameplay Scene.
/// Hỗ trợ Clone độc lập bộ wave gốc để người chơi tự do tăng giảm số quái, EXP, thời gian mà không ảnh hưởng ScriptableObject gốc.
/// </summary>
public static class CustomWaveConfigManager
{
    private static readonly Dictionary<int, List<EnemySpawner.WaveConfig>> customWavesPerChapter = new Dictionary<int, List<EnemySpawner.WaveConfig>>();

    /// <summary>
    /// Đánh dấu xem chapter có cấu hình wave tùy chỉnh hay không.
    /// </summary>
    public static bool HasCustomWaves(int chapterIndex)
    {
        return customWavesPerChapter.ContainsKey(chapterIndex) && customWavesPerChapter[chapterIndex] != null && customWavesPerChapter[chapterIndex].Count > 0;
    }

    /// <summary>
    /// Lấy danh sách Wave tùy chỉnh cho chapter cụ thể.
    /// </summary>
    public static List<EnemySpawner.WaveConfig> GetActiveWaves(int chapterIndex)
    {
        if (customWavesPerChapter.TryGetValue(chapterIndex, out var list))
        {
            return list;
        }
        return null;
    }

    /// <summary>
    /// Lưu danh sách Wave tùy biến cho chapter.
    /// </summary>
    public static void SetActiveCustomWaves(int chapterIndex, List<EnemySpawner.WaveConfig> waves)
    {
        if (waves == null)
        {
            customWavesPerChapter.Remove(chapterIndex);
            return;
        }

        customWavesPerChapter[chapterIndex] = CloneWaves(waves);
    }

    /// <summary>
    /// Tạo bản sao (Clone) độc lập từ ChapterData để người chơi chỉnh sửa.
    /// </summary>
    public static List<EnemySpawner.WaveConfig> CloneFromChapter(ChapterData chapter)
    {
        if (chapter == null) return new List<EnemySpawner.WaveConfig>();

        if (chapter.waves == null || chapter.waves.Count == 0 || chapter.waves.Count != chapter.totalWaves)
        {
            chapter.GenerateWaves();
        }

        return CloneWaves(chapter.waves);
    }

    /// <summary>
    /// Khôi phục về cấu hình Wave mặc định của ChapterData.
    /// </summary>
    public static void ResetToDefault(int chapterIndex, ChapterData chapter)
    {
        if (chapter != null)
        {
            chapter.GenerateWaves();
            customWavesPerChapter[chapterIndex] = CloneWaves(chapter.waves);
        }
        else
        {
            customWavesPerChapter.Remove(chapterIndex);
        }
    }

    /// <summary>
    /// Xóa toàn bộ cấu hình tùy chỉnh của tất cả chapter.
    /// </summary>
    public static void ClearAllCustomWaves()
    {
        customWavesPerChapter.Clear();
    }

    /// <summary>
    /// Tạo bản sao sâu (deep clone) của danh sách WaveConfig để tránh tham chiếu bộ nhớ chung.
    /// </summary>
    public static List<EnemySpawner.WaveConfig> CloneWaves(List<EnemySpawner.WaveConfig> source)
    {
        if (source == null) return new List<EnemySpawner.WaveConfig>();

        List<EnemySpawner.WaveConfig> cloned = new List<EnemySpawner.WaveConfig>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            EnemySpawner.WaveConfig src = source[i];
            if (src == null) continue;

            EnemySpawner.WaveConfig dst = new EnemySpawner.WaveConfig
            {
                waveName = src.waveName,
                totalEnemiesToSpawn = src.totalEnemiesToSpawn,
                maxConcurrentEnemies = src.maxConcurrentEnemies,
                spawnInterval = src.spawnInterval,
                enemiesPerSpawn = src.enemiesPerSpawn,
                enemyPool = src.enemyPool != null ? new List<EnemySpawner.EnemySpawnEntry>(src.enemyPool) : new List<EnemySpawner.EnemySpawnEntry>(),
                healthMultiplier = src.healthMultiplier,
                damageMultiplier = src.damageMultiplier,
                speedMultiplier = src.speedMultiplier,
                expMultiplier = src.expMultiplier,
                isBossWave = src.isBossWave,
                customBossPrefab = src.customBossPrefab,
                bossCount = src.bossCount,
                bossSpawnDelay = src.bossSpawnDelay,
                breakDurationAfterWave = src.breakDurationAfterWave,
                waveDuration = src.waveDuration
            };
            cloned.Add(dst);
        }
        return cloned;
    }
}
