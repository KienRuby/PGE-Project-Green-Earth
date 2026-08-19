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
                enemyPool = (chapterEnemyPool != null && chapterEnemyPool.Count > 0) ? new List<EnemySpawner.EnemySpawnEntry>(chapterEnemyPool) : new List<EnemySpawner.EnemySpawnEntry>(),
                healthMultiplier = isLast ? wavePower * 1.25f : wavePower,
                damageMultiplier = isLast ? (1.0f + i * (wavePowerGrowthRate * 0.75f)) * chapterDifficultyMultiplier * 1.2f : (1.0f + i * (wavePowerGrowthRate * 0.75f)) * chapterDifficultyMultiplier,
                speedMultiplier = 1.0f + i * 0.03f,
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
