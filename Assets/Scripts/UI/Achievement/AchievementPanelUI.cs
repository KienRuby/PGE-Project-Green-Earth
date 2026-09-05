using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Panel Achievements (Tab 2 trong Reward Popup):
/// - Hiển thị danh sách nhiệm vụ thành tựu trong ScrollView.
/// - Sắp xếp thông minh: Nhiệm vụ có thể nhận thưởng nổi lên trên đầu -> Đang làm -> Đã nhận.
/// - Tái sử dụng GameObject / ItemUI (Object Reuse), không destroy/instantiate lại toàn bộ mỗi lần mở.
/// - Lắng nghe sự kiện OnAchievementUpdated để tự động refresh giao diện.
/// </summary>
public class AchievementPanelUI : MonoBehaviour
{
    [Header("Scroll Area References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject achievementItemPrefab;

    [Header("Pre-instantiated Items (Optional)")]
    [SerializeField] private List<AchievementItemUI> spawnedItems = new List<AchievementItemUI>();

    [Header("Resource Sprites (Auto-loaded if empty)")]
    [SerializeField] private Sprite energyIcon;
    [SerializeField] private Sprite redGemIcon;
    [SerializeField] private Sprite dataChipIcon;
    [SerializeField] private Sprite advanceStoneIcon;

    private void Awake()
    {
        EnsureSpritesLoaded();
    }

    private void OnEnable()
    {
        AchievementManager.OnAchievementUpdated += HandleAchievementUpdated;
        AchievementManager.OnAchievementClaimed += HandleAchievementClaimed;

        RefreshAll();
    }

    private void OnDisable()
    {
        AchievementManager.OnAchievementUpdated -= HandleAchievementUpdated;
        AchievementManager.OnAchievementClaimed -= HandleAchievementClaimed;
    }

    public void EnsureSpritesLoaded()
    {
        if (energyIcon != null && redGemIcon != null && dataChipIcon != null) return;

#if UNITY_EDITOR
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/icon tài nguyên.png")
            .OfType<Sprite>()
            .ToArray();

        if (sprites != null && sprites.Length > 0)
        {
            if (energyIcon == null) energyIcon = Array.Find(sprites, s => s.name == "engry") ?? sprites[0];
            if (redGemIcon == null) redGemIcon = Array.Find(sprites, s => s.name == "red") ?? sprites[0];
            if (dataChipIcon == null) dataChipIcon = Array.Find(sprites, s => s.name == "data") ?? sprites[0];
        }
#endif
    }

    public Sprite ResolveRewardIcon(RewardType type)
    {
        switch (type)
        {
            case RewardType.Energy: return energyIcon;
            case RewardType.RedGem: return redGemIcon;
            case RewardType.DataChip: return dataChipIcon;
            case RewardType.AdvanceStone: return advanceStoneIcon != null ? advanceStoneIcon : dataChipIcon;
            default: return dataChipIcon;
        }
    }

    public void RefreshAll()
    {
        EnsureSpritesLoaded();

        AchievementManager mgr = AchievementManager.Instance;
        if (mgr == null) return;

        List<AchievementDefinition> sortedList = mgr.GetSortedAchievements();
        if (sortedList == null || sortedList.Count == 0) return;

        EnsureSpawnedItemsCapacity(sortedList.Count);

        for (int i = 0; i < sortedList.Count; i++)
        {
            if (i >= spawnedItems.Count) break;

            AchievementDefinition def = sortedList[i];
            if (def == null) continue;

            AchievementItemUI itemUI = spawnedItems[i];
            if (itemUI != null)
            {
                int progress = mgr.GetProgress(def.id);
                AchievementState state = mgr.GetState(def.id);

                itemUI.Setup(def, progress, state, OnClaimAchievementClicked, ResolveRewardIcon);
                itemUI.gameObject.SetActive(true);
            }
        }

        // Ẩn các item thừa nếu danh sách ít hơn pool
        for (int i = sortedList.Count; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                spawnedItems[i].gameObject.SetActive(false);
            }
        }
    }

    public IReadOnlyList<AchievementItemUI> SpawnedItems => spawnedItems;

    public void EnsureSpawnedItemsCapacity(int count)
    {
        if (contentContainer == null) return;

        if (spawnedItems == null) spawnedItems = new List<AchievementItemUI>();

        // 1. Loại bỏ các phần tử null khỏi danh sách
        spawnedItems.RemoveAll(item => item == null);

        // 2. Tìm kiếm và nạp tất cả AchievementItemUI sẵn có trong contentContainer mà chưa có trong danh sách
        AchievementItemUI[] existing = contentContainer.GetComponentsInChildren<AchievementItemUI>(true);
        if (existing != null && existing.Length > 0)
        {
            foreach (var item in existing)
            {
                if (item != null && !spawnedItems.Contains(item))
                {
                    spawnedItems.Add(item);
                }
            }
        }

        // 3. Sắp xếp theo đúng thứ tự sibling index trong Hierarchy
        spawnedItems.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        // 4. Nếu vẫn chưa đủ số lượng mục cần hiển thị, tự động instantiate thêm
        while (spawnedItems.Count < count)
        {
            if (achievementItemPrefab != null)
            {
                GameObject obj = Instantiate(achievementItemPrefab, contentContainer);
                AchievementItemUI item = obj.GetComponent<AchievementItemUI>();
                if (item != null) spawnedItems.Add(item);
            }
            else if (spawnedItems.Count > 0 && spawnedItems[0] != null)
            {
                GameObject obj = Instantiate(spawnedItems[0].gameObject, contentContainer);
                AchievementItemUI item = obj.GetComponent<AchievementItemUI>();
                if (item != null) spawnedItems.Add(item);
            }
            else
            {
                break;
            }
        }
    }

    private void OnClaimAchievementClicked(string id)
    {
        if (AchievementManager.Instance == null) return;

        bool success = AchievementManager.Instance.TryClaimReward(id);
        if (success)
        {
            RefreshAll();
        }
    }

    private void HandleAchievementUpdated()
    {
        RefreshAll();
    }

    private void HandleAchievementClaimed(AchievementDefinition def)
    {
        RefreshAll();
    }

    public void SetReferencesForBuilder(
        ScrollRect sRect,
        Transform contentTr,
        List<AchievementItemUI> items,
        Sprite energy,
        Sprite redGem,
        Sprite chip)
    {
        scrollRect = sRect;
        contentContainer = contentTr;
        spawnedItems = items ?? new List<AchievementItemUI>();
        energyIcon = energy;
        redGemIcon = redGem;
        dataChipIcon = chip;
    }
}
