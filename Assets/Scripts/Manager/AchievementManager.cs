using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AchievementState
{
    [Tooltip("Chưa hoàn thành mục tiêu (Progress < Target). Nút 'Not achieved' bị disable.")]
    InProgress,

    [Tooltip("Đã đạt mục tiêu nhưng chưa nhận thưởng (Progress >= Target). Nút 'Get' sáng màu cyan.")]
    Completed,

    [Tooltip("Đã nhận thưởng (Claimed). Nút 'Obtained' bị disable.")]
    Claimed
}

[Serializable]
public class AchievementProgressData
{
    public string id;
    public int progress;
    public bool claimed;
}

/// <summary>
/// Quản lý tập trung toàn bộ tiến độ và trạng thái Achievements:
/// - Event-driven: lắng nghe các sự kiện gameplay từ GameEvents và DailyLoginManager (không polling trong Update).
/// - Data-driven từ AchievementDatabase.
/// - Lưu trữ độc lập cho từng Achievement qua PlayerPrefs ngay sau khi thay đổi/nhận thưởng.
/// - Tính toán fill progress chuẩn xác 0-100% (Mathf.Clamp01).
/// - Hỗ trợ sắp xếp ưu tiên: Hoàn thành chờ nhận -> Đang thực hiện -> Đã nhận.
/// - Chống spam / nhận thưởng trùng lặp.
/// </summary>
[DisallowMultipleComponent]
public sealed class AchievementManager : MonoBehaviour
{
    private static AchievementManager instance;
    public static AchievementManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AchievementManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[AchievementManager]");
                    instance = go.AddComponent<AchievementManager>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return instance;
        }
    }

    public const string ProgressKeyPrefix = "PGE.Achievement.Progress.";
    public const string ClaimedKeyPrefix = "PGE.Achievement.Claimed.";

    [Header("Configuration Database")]
    [SerializeField] private AchievementDatabase database;

    // Events
    public static event Action<AchievementDefinition> OnAchievementCompleted;
    public static event Action<AchievementDefinition> OnAchievementClaimed;
    public static event Action OnAchievementUpdated;

    private bool isProcessingClaim = false;

    public AchievementDatabase Database => database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            AchievementManager found = FindObjectOfType<AchievementManager>();
            if (found != null)
            {
                instance = found;
                if (Application.isPlaying) DontDestroyOnLoad(found.gameObject);
            }
            else
            {
                GameObject go = new GameObject("[AchievementManager]");
                instance = go.AddComponent<AchievementManager>();
                if (Application.isPlaying) DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureDatabaseLoaded();
        SubscribeToGameplayEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameplayEvents();
        if (instance == this) instance = null;
    }

    public void EnsureDatabaseLoaded()
    {
        if (database == null)
        {
#if UNITY_EDITOR
            database = UnityEditor.AssetDatabase.LoadAssetAtPath<AchievementDatabase>("Assets/Data/Achievements/AchievementDatabase.asset");
#endif
            if (database == null)
            {
                database = Resources.Load<AchievementDatabase>("AchievementDatabase");
            }

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<AchievementDatabase>();
                database.PopulateDefaultAchievements();
            }
        }
    }

    // =========================================================================
    // EVENT SUBSCRIPTION (EVENT-DRIVEN PROGRESS)
    // =========================================================================

    private void SubscribeToGameplayEvents()
    {
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnDroneTierAdvanced += HandleDroneTierAdvanced;
        GameEvents.OnChapterPlayed += HandleChapterPlayed;
        GameEvents.OnChapterCleared += HandleChapterCleared;
        DailyLoginManager.OnDailyRewardClaimed += HandleDailyRewardClaimed;
    }

    private void UnsubscribeFromGameplayEvents()
    {
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnDroneTierAdvanced -= HandleDroneTierAdvanced;
        GameEvents.OnChapterPlayed -= HandleChapterPlayed;
        GameEvents.OnChapterCleared -= HandleChapterCleared;
        DailyLoginManager.OnDailyRewardClaimed -= HandleDailyRewardClaimed;
    }

    private void HandleEnemyKilled()
    {
        AddProgress(AchievementType.EnemyKilled, 1);
    }

    private void HandleDroneTierAdvanced()
    {
        AddProgress(AchievementType.DroneTierAdvanced, 1);
    }

    private void HandleChapterPlayed(int _)
    {
        AddProgress(AchievementType.ChapterPlayed, 1);
    }

    private void HandleChapterCleared(int chapterNumber)
    {
        // Tăng số lần clear hoặc đặt max chapter cleared
        AddProgress(AchievementType.ChapterCleared, 1);
    }

    private void HandleDailyRewardClaimed(int _, RewardData[] __)
    {
        AddProgress(AchievementType.LoginRewardClaimed, 1);
    }

    // =========================================================================
    // PERSISTENCE KEYS & GETTERS/SETTERS
    // =========================================================================

    public static string GetProgressKey(string id) => $"{ProgressKeyPrefix}{id.Trim().ToLowerInvariant()}";
    public static string GetClaimedKey(string id) => $"{ClaimedKeyPrefix}{id.Trim().ToLowerInvariant()}";

    public int GetProgress(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;
        return PlayerPrefs.GetInt(GetProgressKey(id), 0);
    }

    public void SetProgress(string id, int value)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        int clamped = Mathf.Max(0, value);
        PlayerPrefs.SetInt(GetProgressKey(id), clamped);
        PlayerPrefs.Save();
    }

    public bool IsClaimed(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return PlayerPrefs.GetInt(GetClaimedKey(id), 0) == 1;
    }

    public void SetClaimed(string id, bool claimed = true)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        PlayerPrefs.SetInt(GetClaimedKey(id), claimed ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsCompleted(string id)
    {
        AchievementDefinition def = GetDefinition(id);
        if (def == null) return false;
        return GetProgress(id) >= def.targetValue;
    }

    public AchievementDefinition GetDefinition(string id)
    {
        EnsureDatabaseLoaded();
        return database != null ? database.GetAchievement(id) : null;
    }

    public AchievementState GetState(string id)
    {
        if (IsClaimed(id))
        {
            return AchievementState.Claimed;
        }

        if (IsCompleted(id))
        {
            return AchievementState.Completed;
        }

        return AchievementState.InProgress;
    }

    /// <summary>
    /// Tính tỷ lệ tiến độ chuẩn hóa từ 0.0f đến 1.0f (Mathf.Clamp01) để fill UI Progress bar.
    /// Không bao giờ vượt quá 100% kể cả khi tiến độ vượt mục tiêu (ví dụ 19/3).
    /// </summary>
    public float GetProgressNormalized(string id)
    {
        AchievementDefinition def = GetDefinition(id);
        if (def == null || def.targetValue <= 0) return 0f;
        int current = GetProgress(id);
        return Mathf.Clamp01((float)current / def.targetValue);
    }

    // =========================================================================
    // PROGRESS PROGRESSION LOGIC
    // =========================================================================

    public void AddProgress(AchievementType type, int amount = 1)
    {
        if (amount <= 0) return;
        EnsureDatabaseLoaded();
        if (database == null || database.Achievements == null) return;

        bool anyUpdated = false;

        foreach (var def in database.Achievements)
        {
            if (def != null && def.type == type)
            {
                int current = GetProgress(def.id);
                int next = current + amount;
                SetProgress(def.id, next);
                anyUpdated = true;

                // Kiểm tra xem vừa mới đạt mục tiêu
                if (current < def.targetValue && next >= def.targetValue)
                {
                    Debug.Log($"[AchievementManager] 🏆 Hoàn thành Achievement: '{def.title}' ({next}/{def.targetValue})!");
                    OnAchievementCompleted?.Invoke(def);
                }
            }
        }

        if (anyUpdated)
        {
            OnAchievementUpdated?.Invoke();
        }
    }

    public void SetProgressForType(AchievementType type, int value)
    {
        EnsureDatabaseLoaded();
        if (database == null || database.Achievements == null) return;

        bool anyUpdated = false;
        foreach (var def in database.Achievements)
        {
            if (def != null && def.type == type)
            {
                int current = GetProgress(def.id);
                SetProgress(def.id, value);
                anyUpdated = true;

                if (current < def.targetValue && value >= def.targetValue)
                {
                    OnAchievementCompleted?.Invoke(def);
                }
            }
        }

        if (anyUpdated)
        {
            OnAchievementUpdated?.Invoke();
        }
    }

    // =========================================================================
    // CLAIM REWARD TRANSACTION
    // =========================================================================

    public bool TryClaimReward(string id)
    {
        if (isProcessingClaim)
        {
            Debug.LogWarning("[AchievementManager] ⚠️ Đang xử lý nhận thưởng, vui lòng không spam!");
            return false;
        }

        AchievementDefinition def = GetDefinition(id);
        if (def == null)
        {
            Debug.LogError($"[AchievementManager] Không tìm thấy Achievement có ID: '{id}'!");
            return false;
        }

        if (IsClaimed(id))
        {
            Debug.LogWarning($"[AchievementManager] Achievement '{def.title}' đã nhận thưởng trước đó!");
            return false;
        }

        if (!IsCompleted(id))
        {
            Debug.LogWarning($"[AchievementManager] Achievement '{def.title}' chưa hoàn thành ({GetProgress(id)}/{def.targetValue})!");
            return false;
        }

        isProcessingClaim = true;

        try
        {
            // 1. Trao toàn bộ phần thưởng
            RewardService.GrantRewards(def.rewards);

            // 2. Ghi nhận trạng thái đã nhận ngay lập tức
            SetClaimed(id, true);
            PlayerPrefs.Save();

            Debug.Log($"[AchievementManager] ✅ Đã nhận thưởng Achievement '{def.title}' thành công!");

            // 3. Bắn event thông báo
            OnAchievementClaimed?.Invoke(def);
            OnAchievementUpdated?.Invoke();

            return true;
        }
        finally
        {
            isProcessingClaim = false;
        }
    }

    // =========================================================================
    // SORTING & NOTIFICATION BADGE
    // =========================================================================

    public bool HasAnyClaimableAchievement()
    {
        EnsureDatabaseLoaded();
        if (database == null || database.Achievements == null) return false;

        return database.Achievements.Any(a => a != null && IsCompleted(a.id) && !IsClaimed(a.id));
    }

    /// <summary>
    /// Trả về danh sách Achievement đã được sắp xếp thông minh:
    /// 1. Hoàn thành chờ nhận thưởng (Completed + Unclaimed) lên đầu tiên
    /// 2. Đang thực hiện (InProgress) ở giữa
    /// 3. Đã nhận (Claimed) xuống dưới cùng
    /// </summary>
    public List<AchievementDefinition> GetSortedAchievements()
    {
        EnsureDatabaseLoaded();
        if (database == null || database.Achievements == null) return new List<AchievementDefinition>();

        return database.Achievements
            .Where(a => a != null)
            .OrderBy(a =>
            {
                AchievementState state = GetState(a.id);
                switch (state)
                {
                    case AchievementState.Completed: return 0; // Ưu tiên cao nhất
                    case AchievementState.InProgress: return 1;
                    case AchievementState.Claimed: return 2;    // Đã nhận xuống cuối
                    default: return 3;
                }
            })
            .ThenBy(a => a.sortPriority)
            .ToList();
    }

    // =========================================================================
    // IN-EDITOR DEBUG CONTEXT CHEATS
    // =========================================================================

#if UNITY_EDITOR
    [ContextMenu("Debug: Complete All Achievements")]
    public void DebugCompleteAllAchievements()
    {
        EnsureDatabaseLoaded();
        if (database == null) return;

        foreach (var def in database.Achievements)
        {
            if (def != null)
            {
                SetProgress(def.id, def.targetValue);
            }
        }
        OnAchievementUpdated?.Invoke();
        Debug.Log("[AchievementManager] 🌟 Đã đặt toàn bộ Achievements về trạng thái Hoàn thành (Get)!");
    }

    [ContextMenu("Debug: Reset All Achievements")]
    public void DebugResetAllAchievements()
    {
        EnsureDatabaseLoaded();
        if (database == null) return;

        foreach (var def in database.Achievements)
        {
            if (def != null)
            {
                PlayerPrefs.DeleteKey(GetProgressKey(def.id));
                PlayerPrefs.DeleteKey(GetClaimedKey(def.id));
            }
        }
        PlayerPrefs.Save();
        OnAchievementUpdated?.Invoke();
        Debug.Log("[AchievementManager] 🧹 Đã reset toàn bộ tiến độ Achievements!");
    }

    [ContextMenu("Debug: Add 100 Enemy Kills")]
    public void DebugAdd100EnemyKills()
    {
        AddProgress(AchievementType.EnemyKilled, 100);
        Debug.Log("[AchievementManager] ⚔️ +100 Enemy Kills.");
    }

    [ContextMenu("Debug: Add 1 Drone Upgrade")]
    public void DebugAdd1DroneUpgrade()
    {
        AddProgress(AchievementType.DroneTierAdvanced, 1);
        Debug.Log("[AchievementManager] 🤖 +1 Drone Upgrade.");
    }
#endif
}
