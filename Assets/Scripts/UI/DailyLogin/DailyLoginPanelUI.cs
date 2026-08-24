using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Panel Daily Login Reward (Tab 1 trong Reward Popup):
/// - Hiển thị 7 ngày phần thưởng trong ScrollView (hỗ trợ kéo cuộn mượt mà trên mobile).
/// - Khởi tạo hoặc tái sử dụng 7 item tương ứng từ Day 01 đến Day 07.
/// - Đồng bộ trạng thái thời gian thực với DailyLoginManager.
/// - Cập nhật đồng hồ đếm ngược mỗi 1 giây (không chạy polling nặng trong Update).
/// - Tự động nạp sprite icons từ Sprites/UI/icon tài nguyên.
/// </summary>
public class DailyLoginPanelUI : MonoBehaviour
{
    [Header("Scroll Area References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject dayItemPrefab;

    [Header("7 Pre-instantiated Day Items (Optional)")]
    [SerializeField] private DailyLoginItemUI[] dayItems = new DailyLoginItemUI[7];

    [Header("Resource Sprites (Auto-loaded if empty)")]
    [SerializeField] private Sprite energyIcon;
    [SerializeField] private Sprite redGemIcon;
    [SerializeField] private Sprite dataChipIcon;
    [SerializeField] private Sprite advanceStoneIcon;

    private Coroutine countdownCoroutine;
    private List<DailyLoginItemUI> instantiatedItems = new List<DailyLoginItemUI>();

    private void Awake()
    {
        EnsureSpritesLoaded();
    }

    private void OnEnable()
    {
        DailyLoginManager.OnDailyLoginStateChanged += HandleStateChanged;
        DailyLoginManager.OnDailyRewardClaimed += HandleRewardClaimed;

        RefreshAll();
        StartCountdownRoutine();
    }

    private void OnDisable()
    {
        DailyLoginManager.OnDailyLoginStateChanged -= HandleStateChanged;
        DailyLoginManager.OnDailyRewardClaimed -= HandleRewardClaimed;

        StopCountdownRoutine();
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

        DailyLoginManager mgr = DailyLoginManager.Instance;
        if (mgr == null) return;

        DailyLoginDatabase db = mgr.Database;
        if (db == null)
        {
            mgr.EnsureDatabaseLoaded();
            db = mgr.Database;
        }
        if (db == null) return;

        EnsureItemsCreated();

        for (int i = 0; i < 7; i++)
        {
            int dayIndex = i + 1;
            DailyLoginDayData dayData = db.GetDayData(dayIndex);
            DailyLoginState state = mgr.GetDayState(dayIndex);

            DailyLoginItemUI itemUI = GetItemUI(i);
            if (itemUI != null && dayData != null)
            {
                itemUI.Setup(dayData, state, OnClaimDayClicked, ResolveRewardIcon);
                itemUI.gameObject.SetActive(true);
            }
        }
    }

    private void EnsureItemsCreated()
    {
        if (instantiatedItems.Count >= 7) return;

        // 1. Kiểm tra mảng serialized dayItems trước
        if (dayItems != null && dayItems.Length >= 7 && dayItems[0] != null)
        {
            instantiatedItems = new List<DailyLoginItemUI>(dayItems);
            return;
        }

        // 2. Tìm trong contentContainer
        if (contentContainer != null)
        {
            DailyLoginItemUI[] found = contentContainer.GetComponentsInChildren<DailyLoginItemUI>(true);
            if (found != null && found.Length >= 7)
            {
                instantiatedItems = new List<DailyLoginItemUI>(found);
                return;
            }
            if (found != null && found.Length > 0)
            {
                instantiatedItems = new List<DailyLoginItemUI>(found);
            }
        }

        // 3. Nếu có prefab thì instantiate 7 ngày
        if (contentContainer != null && dayItemPrefab != null)
        {
            while (instantiatedItems.Count < 7)
            {
                GameObject obj = Instantiate(dayItemPrefab, contentContainer);
                DailyLoginItemUI item = obj.GetComponent<DailyLoginItemUI>();
                instantiatedItems.Add(item);
            }
        }
        else if (contentContainer != null && instantiatedItems.Count > 0 && instantiatedItems[0] != null)
        {
            while (instantiatedItems.Count < 7)
            {
                GameObject obj = Instantiate(instantiatedItems[0].gameObject, contentContainer);
                DailyLoginItemUI item = obj.GetComponent<DailyLoginItemUI>();
                instantiatedItems.Add(item);
            }
        }
    }

    private DailyLoginItemUI GetItemUI(int index)
    {
        if (index >= 0 && index < instantiatedItems.Count)
        {
            return instantiatedItems[index];
        }
        if (dayItems != null && index >= 0 && index < dayItems.Length)
        {
            return dayItems[index];
        }
        return null;
    }

    private void OnClaimDayClicked(int dayIndex)
    {
        if (DailyLoginManager.Instance == null) return;

        bool success = DailyLoginManager.Instance.TryClaimTodayReward();
        if (success)
        {
            RefreshAll();
        }
    }

    private void HandleStateChanged()
    {
        RefreshAll();
    }

    private void HandleRewardClaimed(int dayIndex, RewardData[] rewards)
    {
        RefreshAll();
    }

    private void StartCountdownRoutine()
    {
        StopCountdownRoutine();
        countdownCoroutine = StartCoroutine(CountdownUpdateRoutine());
    }

    private void StopCountdownRoutine()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine cập nhật đồng hồ đếm ngược 1 lần mỗi giây.
    /// Giảm tải tối đa cho GPU/CPU, hoàn toàn không chạy trong Update().
    /// </summary>
    private IEnumerator CountdownUpdateRoutine()
    {
        while (gameObject.activeInHierarchy)
        {
            if (DailyLoginManager.Instance != null)
            {
                string formatted = DailyLoginManager.Instance.GetRemainingTimeFormatted();
                int currentDay = DailyLoginManager.Instance.CurrentLoginDay;
                DailyLoginItemUI currentItem = GetItemUI(currentDay - 1);
                if (currentItem != null)
                {
                    currentItem.UpdateCountdownText(formatted);
                }
            }

            yield return new WaitForSecondsRealtime(1f);
        }
        countdownCoroutine = null;
    }

    public void SetReferencesForBuilder(
        ScrollRect sRect,
        Transform contentTr,
        DailyLoginItemUI[] items,
        Sprite energy,
        Sprite redGem,
        Sprite chip)
    {
        scrollRect = sRect;
        contentContainer = contentTr;
        dayItems = items;
        energyIcon = energy;
        redGemIcon = redGem;
        dataChipIcon = chip;
        if (items != null)
        {
            instantiatedItems = new List<DailyLoginItemUI>(items);
        }
    }
}
