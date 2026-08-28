using System;
using UnityEngine;

/// <summary>
/// Quản lý cấp độ (Level) và thanh kinh nghiệm (EXP) trong trận chiến Gameplay:
/// - Khi tiêu diệt quái vật: Nhận điểm EXP.
/// - Khi thanh EXP đầy: Tăng cấp (Level Up) và phát sự kiện OnLevelUp.
/// - Chuẩn bị sẵn hook kết nối giao diện chọn Chipset/Kỹ năng sau này.
/// </summary>
public class PlayerLevelController : MonoBehaviour
{
    public static PlayerLevelController Instance { get; private set; }

    [Header("Level Settings")]
    [Tooltip("Cấp độ khởi đầu.")]
    [SerializeField] private int startingLevel = 1;

    [Tooltip("Lượng kinh nghiệm cần để đạt cấp 2.")]
    [SerializeField] private int baseExpForNextLevel = 30;

    [Tooltip("Lượng kinh nghiệm tăng thêm cần cho mỗi cấp tiếp theo.")]
    [SerializeField] private int expGrowthPerLevel = 20;

    private int currentLevel = 1;
    private int currentExp = 0;
    private bool levelUpLocked;

    // Events
    public event Action<int, int, float> OnEXPChanged; // (currentExp, maxExp, progress 0..1)
    public event Action<int> OnLevelUp;                // (newLevel)

    // Properties
    public int CurrentLevel => currentLevel;
    public int CurrentEXP => currentExp;
    public int MaxEXP => CalculateMaxExpForLevel(currentLevel);
    public float EXPProgress => MaxEXP > 0 ? Mathf.Clamp01((float)currentExp / MaxEXP) : 0f;
    public bool IsLevelUpLocked => levelUpLocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        currentLevel = Mathf.Max(1, startingLevel);
        currentExp = 0;
        levelUpLocked = false;
    }

    private void Start()
    {
        NotifyExpChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int CalculateMaxExpForLevel(int level)
    {
        level = Mathf.Max(1, level);
        return baseExpForNextLevel + (level - 1) * expGrowthPerLevel;
    }

    public void AddEXP(int amount)
    {
        if (amount <= 0 || levelUpLocked) return;

        currentExp += amount;

        while (currentExp >= MaxEXP)
        {
            currentExp -= MaxEXP;
            currentLevel++;
            Debug.Log($"[PlayerLevel] 🎉 LÊN CẤP! Level hiện tại: {currentLevel}, EXP dư: {currentExp}/{MaxEXP}");
            OnLevelUp?.Invoke(currentLevel);
        }

        NotifyExpChanged();
    }

    /// <summary>
    /// Khóa vĩnh viễn việc nhận EXP trong phần còn lại của trận sau khi đã thắng.
    /// Scene gameplay mới sẽ tạo controller mới và tự mở khóa trong Awake.
    /// </summary>
    public void LockLevelUpsForVictory()
    {
        levelUpLocked = true;
    }

    public void SetLevelAndExpForTesting(int level, int exp)
    {
        currentLevel = Mathf.Max(1, level);
        currentExp = Mathf.Max(0, exp);
        NotifyExpChanged();
    }

    private void NotifyExpChanged()
    {
        OnEXPChanged?.Invoke(currentExp, MaxEXP, EXPProgress);
    }
}
