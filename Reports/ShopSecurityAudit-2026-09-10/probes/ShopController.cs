using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Cửa Hàng (Shop):
/// Sử dụng ChipManager và PlayerDataService để quản lý toàn bộ các giao dịch tiền tệ và đồng bộ dữ liệu.
/// Bảo mật cấp production:
/// 1. Concurrency Lock & Anti-Spam (chống spam click, double tap, re-entrancy).
/// 2. Validation Pipeline nghiêm ngặt (chống giá âm, overflow, claim trái phép).
/// 3. Atomic 2-Phase Transaction với rollback tự động khi có lỗi.
/// 4. Đồng bộ nguồn dữ liệu chuẩn (Single Source of Truth) giữa UI và logic.
/// 5. Structured Audit Logging cho mọi giao dịch.
/// </summary>
public sealed class ShopController : MonoBehaviour
{
    private const string ChipsetBoxCountKey = PlayerDataService.ChipsetBoxesKey;
    private const string DroneBoxCountKey = PlayerDataService.DroneBoxesKey;
    private const int MaxAllowedRewardAmount = 1_000_000;

    public enum CurrencyType
    {
        Free,
        RedGem,
        VND
    }

    public enum RewardType
    {
        RedGem,
        DataChip,
        Energy,
        ChipsetBox,
        DroneBox
    }

    [Serializable]
    public sealed class Offer
    {
        public string id;
        public string displayName;
        public Button button;
        public TMP_Text priceText;
        public CurrencyType currency;
        [Min(0)] public int price;
        public RewardType reward;
        [Min(1)] public int rewardAmount = 1;
        public bool oncePerDay;
    }

    [Header("Balances and Header UI")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text dataChipText;
    [SerializeField] private TMP_Text redGemText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    [Header("Offers")]
    [SerializeField] private Offer[] offers;

    [Header("Security and Anti-Spam")]
    [Tooltip("Thời gian giãn cách tối thiểu (giây) giữa 2 lần bấm mua liên tiếp để chống click spam/double tap.")]
    [SerializeField] private float transactionCooldown = 0.2f;

    private int currentEnergy;
    private int currentDataChips;
    private int currentRedGems;
    private int chipsetBoxes;
    private int droneBoxes;

    private bool isProcessingTransaction;
    private float lastTransactionTime = -10f;

    public bool IsProcessingTransaction => isProcessingTransaction;
    public float TransactionCooldown
    {
        get => transactionCooldown;
        set => transactionCooldown = Mathf.Max(0f, value);
    }
    public Offer[] Offers => offers;

    private void Awake()
    {
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, ChipManager.MaxEnergy);
        currentDataChips = ChipManager.DataChips;
        currentRedGems = ChipManager.RedGems;
        chipsetBoxes = PlayerDataService.ChipsetBoxes;
        droneBoxes = PlayerDataService.DroneBoxes;
    }

    private void Start()
    {
        BindButtons();
        RefreshView();
        ShowMessage("DAILY SHOP READY");
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged += HandleRedGemsChanged;
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;
        ChipManager.OnChipsetBoxesChanged += HandleChipsetBoxesChanged;
        ChipManager.OnDroneBoxesChanged += HandleDroneBoxesChanged;

        currentDataChips = ChipManager.DataChips;
        currentRedGems = ChipManager.RedGems;
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, ChipManager.MaxEnergy);
        chipsetBoxes = PlayerDataService.ChipsetBoxes;
        droneBoxes = PlayerDataService.DroneBoxes;

        BindButtons();
        RefreshView();
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged -= HandleRedGemsChanged;
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;
        ChipManager.OnChipsetBoxesChanged -= HandleChipsetBoxesChanged;
        ChipManager.OnDroneBoxesChanged -= HandleDroneBoxesChanged;

        CancelInvoke(nameof(HideToast));
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (offers == null) return;

        for (int i = 0; i < offers.Length; i++)
        {
            Offer offer = offers[i];
            if (offer?.button != null)
            {
                offer.button.onClick.RemoveAllListeners();
                offer.button.onClick.AddListener(() => TryPurchase(offer, bypassCooldown: false));
            }
        }
    }

    private void UnbindButtons()
    {
        if (offers == null) return;

        for (int i = 0; i < offers.Length; i++)
        {
            if (offers[i]?.button != null)
            {
                offers[i].button.onClick.RemoveAllListeners();
            }
        }
    }

    private void HandleDataChipsChanged(int newAmount)
    {
        currentDataChips = newAmount;
        RefreshView();
    }

    private void HandleRedGemsChanged(int newAmount)
    {
        currentRedGems = newAmount;
        RefreshView();
    }

    private void HandleEnergyChanged(int newAmount)
    {
        currentEnergy = Mathf.Clamp(newAmount, 0, ChipManager.MaxEnergy);
        RefreshView();
    }

    private void HandleChipsetBoxesChanged(int newAmount)
    {
        chipsetBoxes = newAmount;
        RefreshView();
    }

    private void HandleDroneBoxesChanged(int newAmount)
    {
        droneBoxes = newAmount;
        RefreshView();
    }

    private void HandleTestModeChanged(bool isTest)
    {
        currentDataChips = ChipManager.DataChips;
        currentRedGems = ChipManager.RedGems;
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, ChipManager.MaxEnergy);
        chipsetBoxes = PlayerDataService.ChipsetBoxes;
        droneBoxes = PlayerDataService.DroneBoxes;
        RefreshView();
    }

    public bool TryPurchase(int offerIndex)
    {
        return TryPurchase(offerIndex, bypassCooldown: false);
    }

    public bool TryPurchase(int offerIndex, bool bypassCooldown)
    {
        if (offers == null || offerIndex < 0 || offerIndex >= offers.Length)
        {
            Debug.LogWarning($"[SHOP] Purchase rejected: Invalid offer index {offerIndex}.");
            return false;
        }

        return TryPurchase(offers[offerIndex], bypassCooldown);
    }

    public bool TryPurchaseById(string offerId, bool bypassCooldown = false)
    {
        if (offers == null || string.IsNullOrWhiteSpace(offerId)) return false;
        for (int i = 0; i < offers.Length; i++)
        {
            if (offers[i] != null && offers[i].id == offerId)
            {
                return TryPurchase(offers[i], bypassCooldown);
            }
        }
        return false;
    }

    public bool TryPurchase(Offer offer, bool bypassCooldown = false)
    {
        // 1. Concurrency Lock: Chống re-entrancy / gọi song song
        if (isProcessingTransaction)
        {
            Debug.LogWarning("[SHOP] Purchase rejected: Another transaction is currently in progress.");
            return false;
        }

        // 2. Anti-Spam / Debounce Check
        if (!bypassCooldown && transactionCooldown > 0f)
        {
            float elapsed = Time.unscaledTime - lastTransactionTime;
            if (elapsed < transactionCooldown)
            {
                Debug.LogWarning($"[SHOP] Purchase rejected: Rapid click detected. Cooldown remaining: {transactionCooldown - elapsed:F2}s");
                return false;
            }
        }

        isProcessingTransaction = true;
        try
        {
            return ExecuteTransaction(offer);
        }
        finally
        {
            isProcessingTransaction = false;
            lastTransactionTime = Time.unscaledTime;
        }
    }

    private bool ExecuteTransaction(Offer offer)
    {
        // 1. Validation Pipeline
        if (!ValidateOffer(offer, out string failureReason))
        {
            ShowMessage(failureReason);
            return false;
        }

        // 2. Snapshot trước giao dịch (cho audit & rollback)
        int redGemsBefore = ChipManager.RedGems;
        int rewardBalanceBefore = GetRewardBalance(offer.reward);

        // 3. Phase 1: Khấu trừ tiền tệ
        bool deducted = false;
        if (offer.currency == CurrencyType.RedGem && offer.price > 0)
        {
            deducted = ChipManager.TrySpendRedGems(offer.price);
            if (!deducted)
            {
                ShowMessage("NOT ENOUGH RED GEMS");
                return false;
            }
        }

        // 4. Phase 2: Trao thưởng & Lưu dữ liệu (với Rollback nếu phát sinh lỗi)
        try
        {
            GrantReward(offer.reward, offer.rewardAmount);

            if (offer.oncePerDay)
            {
                PlayerPrefs.SetString(GetDailyKey(offer.id), DateTime.UtcNow.ToString("yyyyMMdd"));
            }

            SaveState();
        }
        catch (Exception ex)
        {
            // CRITICAL ROLLBACK: Hoàn trả tiền nếu trao quà hoặc lưu thất bại
            Debug.LogError($"[SHOP] Critical: Exception during GrantReward/SaveState! Rolling back transaction. Error: {ex}");
            if (deducted && offer.currency == CurrencyType.RedGem)
            {
                ChipManager.AddRedGems(offer.price);
            }
            ShowMessage("TRANSACTION FAILED");
            return false;
        }

        // 5. Cập nhật UI
        RefreshView();

        // 6. Structured Audit Log (Requirement 19)
        int redGemsAfter = ChipManager.RedGems;
        int rewardBalanceAfter = GetRewardBalance(offer.reward);
        Debug.Log($"[SHOP] Transaction Success:\n" +
                  $"  ItemID: {offer.id}\n" +
                  $"  Price: {offer.price} {offer.currency}\n" +
                  $"  BalanceBefore: {redGemsBefore}\n" +
                  $"  BalanceAfter: {redGemsAfter}\n" +
                  $"  Reward: {offer.reward} x{offer.rewardAmount}\n" +
                  $"  QuantityBefore: {rewardBalanceBefore}\n" +
                  $"  QuantityAfter: {rewardBalanceAfter}\n" +
                  $"  TransactionSuccess: true");

        ShowMessage(BuildSuccessMessage(offer));
        return true;
    }

    private bool ValidateOffer(Offer offer, out string failureReason)
    {
        if (offer == null)
        {
            failureReason = "INVALID OFFER";
            return false;
        }

        if (string.IsNullOrWhiteSpace(offer.id))
        {
            failureReason = "INVALID OFFER ID";
            return false;
        }

        // Kiểm tra rewardAmount không âm, không = 0, và không vượt ngưỡng an toàn
        if (offer.rewardAmount <= 0)
        {
            failureReason = "INVALID REWARD AMOUNT";
            return false;
        }

        if (offer.rewardAmount > MaxAllowedRewardAmount)
        {
            failureReason = "REWARD AMOUNT EXCEEDS LIMIT";
            return false;
        }

        // Kiểm tra lượt nhận trong ngày
        if (offer.oncePerDay && WasClaimedToday(offer.id))
        {
            failureReason = $"{offer.displayName} ALREADY CLAIMED TODAY";
            return false;
        }

        // VND / IAP: Fail-closed an toàn
        if (offer.currency == CurrencyType.VND)
        {
            failureReason = "IAP PAYMENT COMING SOON";
            return false;
        }

        // Free: Bắt buộc giá = 0
        if (offer.currency == CurrencyType.Free)
        {
            if (offer.price != 0)
            {
                failureReason = "FREE OFFER MUST HAVE ZERO PRICE";
                return false;
            }
        }

        // RedGem: Bắt buộc giá > 0 và người chơi đủ Gem
        if (offer.currency == CurrencyType.RedGem)
        {
            if (offer.price <= 0)
            {
                failureReason = "INVALID PRICE";
                return false;
            }

            if (!ChipManager.HasEnoughRedGems(offer.price))
            {
                failureReason = "NOT ENOUGH RED GEMS";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private void GrantReward(RewardType reward, int amount)
    {
        if (amount <= 0) return;

        switch (reward)
        {
            case RewardType.RedGem:
                ChipManager.AddRedGems(amount);
                break;
            case RewardType.DataChip:
                ChipManager.AddDataChips(amount);
                break;
            case RewardType.Energy:
                ChipManager.AddEnergy(amount);
                break;
            case RewardType.ChipsetBox:
                PlayerDataService.AddChipsetBoxes(amount);
                chipsetBoxes = PlayerDataService.ChipsetBoxes;
                break;
            case RewardType.DroneBox:
                PlayerDataService.AddDroneBoxes(amount);
                droneBoxes = PlayerDataService.DroneBoxes;
                break;
        }
    }

    private int GetRewardBalance(RewardType reward)
    {
        switch (reward)
        {
            case RewardType.RedGem: return ChipManager.RedGems;
            case RewardType.DataChip: return ChipManager.DataChips;
            case RewardType.Energy: return ChipManager.Energy;
            case RewardType.ChipsetBox: return PlayerDataService.ChipsetBoxes;
            case RewardType.DroneBox: return PlayerDataService.DroneBoxes;
            default: return 0;
        }
    }

    private string BuildSuccessMessage(Offer offer)
    {
        switch (offer.reward)
        {
            case RewardType.RedGem:
                return $"RECEIVED {offer.rewardAmount:N0} RED GEMS";
            case RewardType.DataChip:
                return $"RECEIVED {offer.rewardAmount:N0} DATA CHIPS";
            case RewardType.Energy:
                return $"RESTORED {offer.rewardAmount:N0} ENERGY";
            case RewardType.ChipsetBox:
                return $"OPENED {offer.rewardAmount:N0} CHIPSET BOXES  •  TOTAL {chipsetBoxes:N0}";
            default:
                return $"OPENED {offer.rewardAmount:N0} DRONE BOXES  •  TOTAL {droneBoxes:N0}";
        }
    }

    public void SetOffersForTesting(Offer[] testOffers)
    {
        offers = testOffers;
        BindButtons();
    }

    private void RefreshView()
    {
        if (energyText != null)
        {
            energyText.text = $"{currentEnergy}/{ChipManager.MaxEnergy}";
        }

        if (dataChipText != null)
        {
            dataChipText.text = currentDataChips.ToString("N0");
        }

        if (redGemText != null)
        {
            redGemText.text = currentRedGems.ToString("N0");
        }

        if (offers != null)
        {
            for (int i = 0; i < offers.Length; i++)
            {
                Offer offer = offers[i];
                if (offer == null) continue;

                bool claimed = offer.oncePerDay && WasClaimedToday(offer.id);
                if (offer.button != null)
                {
                    offer.button.interactable = !claimed;
                }

                if (offer.priceText != null)
                {
                    offer.priceText.text = claimed
                        ? "CLAIMED"
                        : offer.currency == CurrencyType.Free
                            ? "FREE"
                            : $"x{offer.price:N0}";
                }
            }
        }
    }

    private void ShowMessage(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        if (toastRoot != null && toastText != null)
        {
            toastText.text = message;
            toastRoot.SetActive(false);
            toastRoot.SetActive(true);
            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), 2.2f);
        }
    }

    private void HideToast()
    {
        if (toastRoot != null) toastRoot.SetActive(false);
    }

    private void SaveState()
    {
        PlayerPrefs.Save();
    }

    public static bool WasClaimedToday(string offerId)
    {
        if (string.IsNullOrWhiteSpace(offerId)) return false;
        return PlayerPrefs.GetString(GetDailyKey(offerId), string.Empty) ==
            DateTime.UtcNow.ToString("yyyyMMdd");
    }

    private static string GetDailyKey(string offerId)
    {
        return $"PGE.Shop.Daily.{offerId}";
    }

    [ContextMenu("Reset Daily Shop Claims")]
    public void ResetDailyShopClaims()
    {
        if (offers != null)
        {
            for (int i = 0; i < offers.Length; i++)
            {
                if (offers[i] != null && !string.IsNullOrEmpty(offers[i].id))
                {
                    PlayerPrefs.DeleteKey(GetDailyKey(offers[i].id));
                }
            }
        }
        PlayerPrefs.Save();
        RefreshView();
        Debug.Log("[ShopController] All daily shop claims have been reset!");
    }
}
