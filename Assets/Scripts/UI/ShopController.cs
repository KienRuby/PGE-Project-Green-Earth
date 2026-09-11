using System;
using System.Collections.Generic;
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
    private const int PackageItemPieces = 7;

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

    private sealed class DevelopmentPack
    {
        public string Id;
        public int RedGems;
        public int DataChips;
        public int[] ChipsetIds;
        public int[] BuddyIds;
        public bool OnceOnly;
        public bool GrantsVip;
        public string SuccessMessage;
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
    private System.Random boxRandom = new System.Random();
    private List<ShopBoxDropRoller.Drop> lastBoxDrops = new List<ShopBoxDropRoller.Drop>();

    private sealed class BoxDropSnapshot
    {
        public RewardType Reward;
        public ShopBoxDropRoller.Drop[] Drops;
        public ChipItemData[] Chipsets;
        public bool[] ChipsetsExisted;
        public int[] BuddyPieces;
    }

    public bool IsProcessingTransaction => isProcessingTransaction;
    public float TransactionCooldown
    {
        get => transactionCooldown;
        set => transactionCooldown = Mathf.Max(0f, value);
    }
    public Offer[] Offers => offers;
    public IReadOnlyList<ShopBoxDropRoller.Drop> LastBoxDrops => lastBoxDrops;

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
                Debug.Log($"[SHOP] Purchase rejected: Rapid click detected. Cooldown remaining: {transactionCooldown - elapsed:F2}s");
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

        if (TryGetDevelopmentPack(offer, out DevelopmentPack developmentPack))
        {
            return ExecuteDevelopmentPackPurchase(offer, developmentPack);
        }

        // 2. Snapshot trước giao dịch (cho audit & rollback)
        int redGemsBefore = ChipManager.RedGems;
        int rewardBalanceBefore = GetRewardBalance(offer.reward);
        BoxDropSnapshot boxDropSnapshot = null;
        lastBoxDrops = new List<ShopBoxDropRoller.Drop>();

        if (IsShopBoxOpeningOffer(offer))
        {
            lastBoxDrops = ShopBoxDropRoller.Roll(
                offer.reward == RewardType.ChipsetBox
                    ? ShopBoxDropRoller.BoxCategory.Chipset
                    : ShopBoxDropRoller.BoxCategory.Buddy,
                offer.rewardAmount,
                boxRandom);
            boxDropSnapshot = CaptureBoxDropSnapshot(offer.reward, lastBoxDrops);
            if (!CanApplyBoxDrops(boxDropSnapshot))
            {
                lastBoxDrops.Clear();
                ShowMessage("ITEM PIECE LIMIT REACHED");
                return false;
            }
        }

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
            if (boxDropSnapshot != null)
            {
                ApplyBoxDrops(boxDropSnapshot);
            }
            else
            {
                GrantReward(offer.reward, offer.rewardAmount);
            }

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
            if (boxDropSnapshot != null)
            {
                RestoreBoxDropSnapshot(boxDropSnapshot);
                lastBoxDrops.Clear();
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

        ShowMessage(BuildSuccessMessage(offer, lastBoxDrops));
        return true;
    }

    private static bool IsShopBoxOpeningOffer(Offer offer)
    {
        if (offer == null || string.IsNullOrWhiteSpace(offer.id)) return false;

        if (offer.reward == RewardType.ChipsetBox)
        {
            return offer.id.StartsWith("chipset-box-", StringComparison.OrdinalIgnoreCase);
        }

        if (offer.reward == RewardType.DroneBox)
        {
            return offer.id.StartsWith("drone-box-", StringComparison.OrdinalIgnoreCase) ||
                   offer.id.StartsWith("daily-drone-", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static BoxDropSnapshot CaptureBoxDropSnapshot(
        RewardType reward,
        List<ShopBoxDropRoller.Drop> drops)
    {
        var snapshot = new BoxDropSnapshot
        {
            Reward = reward,
            Drops = drops.ToArray()
        };

        if (reward == RewardType.ChipsetBox)
        {
            snapshot.Chipsets = new ChipItemData[drops.Count];
            snapshot.ChipsetsExisted = new bool[drops.Count];
            for (int i = 0; i < drops.Count; i++)
            {
                snapshot.ChipsetsExisted[i] = PlayerDataService.HasChipsetItemData(drops[i].ItemId);
                snapshot.Chipsets[i] = GetSavedChipsetSnapshot(drops[i].ItemId);
            }
        }
        else
        {
            snapshot.BuddyPieces = new int[drops.Count];
            for (int i = 0; i < drops.Count; i++)
            {
                snapshot.BuddyPieces[i] = PlayerDataService.GetBuddyPieceCount(drops[i].ItemId);
            }
        }

        return snapshot;
    }

    private static bool CanApplyBoxDrops(BoxDropSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.Drops.Length; i++)
        {
            int current = snapshot.Reward == RewardType.ChipsetBox
                ? snapshot.Chipsets[i].count
                : snapshot.BuddyPieces[i];
            if (!CanAdd(current, snapshot.Drops[i].Pieces)) return false;
        }
        return true;
    }

    private static void ApplyBoxDrops(BoxDropSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.Drops.Length; i++)
        {
            ShopBoxDropRoller.Drop drop = snapshot.Drops[i];
            if (snapshot.Reward == RewardType.ChipsetBox)
            {
                SaveChipsetPieces(snapshot.Chipsets[i], drop.Pieces);
            }
            else
            {
                PlayerDataService.AddBuddyPieces(drop.ItemId, drop.Pieces);
            }
        }
    }

    private static void RestoreBoxDropSnapshot(BoxDropSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.Drops.Length; i++)
        {
            if (snapshot.Reward == RewardType.ChipsetBox)
            {
                RestoreChipsetSnapshot(snapshot.Chipsets[i], snapshot.ChipsetsExisted[i]);
            }
            else
            {
                PlayerDataService.SetBuddyPieceCount(snapshot.Drops[i].ItemId, snapshot.BuddyPieces[i]);
            }
        }
        PlayerPrefs.Save();
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

        if (TryGetDevelopmentPack(offer, out DevelopmentPack developmentPack) &&
            developmentPack.OnceOnly && WasPurchasedOnce(offer.id))
        {
            failureReason = $"{offer.displayName} ALREADY PURCHASED";
            return false;
        }

        // Chỉ giả lập riêng Welcome Package trong Editor/Development Build.
        // Bản release vẫn fail-closed cho đến khi tích hợp thanh toán cửa hàng thật.
        if (offer.currency == CurrencyType.VND)
        {
            if (developmentPack != null && IsDevelopmentPurchaseAvailable)
            {
                failureReason = string.Empty;
                return true;
            }

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

    private bool ExecuteDevelopmentPackPurchase(Offer offer, DevelopmentPack pack)
    {
        int displayedRedGemsBefore = ChipManager.RedGems;
        int displayedDataChipsBefore = ChipManager.DataChips;
        int savedRedGemsBefore = PlayerDataService.RedGems;
        int savedDataChipsBefore = PlayerDataService.DataChips;
        bool vipOwnedBefore = PlayerDataService.IsVipOwned;
        int[] chipsetIds = pack.ChipsetIds ?? Array.Empty<int>();
        int[] buddyIds = pack.BuddyIds ?? Array.Empty<int>();
        ChipItemData[] chipsetBefore = new ChipItemData[chipsetIds.Length];
        bool[] chipsetExisted = new bool[chipsetIds.Length];
        int[] buddyPiecesBefore = new int[buddyIds.Length];

        for (int i = 0; i < chipsetIds.Length; i++)
        {
            chipsetExisted[i] = PlayerDataService.HasChipsetItemData(chipsetIds[i]);
            chipsetBefore[i] = GetSavedChipsetSnapshot(chipsetIds[i]);
        }

        for (int i = 0; i < buddyIds.Length; i++)
        {
            buddyPiecesBefore[i] = PlayerDataService.GetBuddyPieceCount(buddyIds[i]);
        }

        if (!CanAdd(displayedRedGemsBefore, pack.RedGems) ||
            !CanAdd(displayedDataChipsBefore, pack.DataChips) ||
            !CanAdd(savedRedGemsBefore, pack.RedGems) ||
            !CanAdd(savedDataChipsBefore, pack.DataChips) ||
            HasChipsetOverflow(chipsetBefore) ||
            HasBuddyOverflow(buddyPiecesBefore))
        {
            ShowMessage("PACKAGE INVENTORY LIMIT REACHED");
            return false;
        }

        try
        {
            GrantPackageCurrency(pack.RedGems, pack.DataChips);
            for (int i = 0; i < chipsetBefore.Length; i++)
            {
                SaveChipsetPieces(chipsetBefore[i], PackageItemPieces);
            }
            for (int i = 0; i < buddyIds.Length; i++)
            {
                PlayerDataService.AddBuddyPieces(buddyIds[i], PackageItemPieces);
            }
            if (pack.GrantsVip) PlayerDataService.IsVipOwned = true;
            if (pack.OnceOnly) PlayerPrefs.SetInt(GetPurchasedOnceKey(offer.id), 1);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SHOP] Development package grant failed. Rolling back. Error: {ex}");
            RestorePackageCurrency(
                displayedRedGemsBefore,
                displayedDataChipsBefore,
                savedRedGemsBefore,
                savedDataChipsBefore);
            for (int i = 0; i < chipsetBefore.Length; i++)
            {
                RestoreChipsetSnapshot(chipsetBefore[i], chipsetExisted[i]);
            }
            for (int i = 0; i < buddyIds.Length; i++)
            {
                PlayerDataService.SetBuddyPieceCount(buddyIds[i], buddyPiecesBefore[i]);
            }
            PlayerDataService.IsVipOwned = vipOwnedBefore;
            PlayerPrefs.DeleteKey(GetPurchasedOnceKey(offer.id));
            PlayerPrefs.Save();
            ShowMessage("TRANSACTION FAILED");
            return false;
        }

        RefreshView();
        Debug.Log($"[SHOP] Development Purchase Success:\n" +
                  $"  ItemID: {offer.id}\n" +
                  $"  Reward: {pack.SuccessMessage}\n" +
                  $"  TransactionSuccess: true");
        ShowMessage(pack.SuccessMessage);
        return true;
    }

    private static bool IsDevelopmentPurchaseAvailable => Application.isEditor || Debug.isDebugBuild;

    private static bool TryGetDevelopmentPack(Offer offer, out DevelopmentPack pack)
    {
        pack = null;
        if (offer == null || offer.currency != CurrencyType.VND) return false;

        switch (offer.id)
        {
            case "vip-package":
                pack = new DevelopmentPack { Id = offer.id, RedGems = 10_000, OnceOnly = true, GrantsVip = true, SuccessMessage = "VIP UNLOCKED • 10,000 GEMS RECEIVED" };
                break;
            case "welcome-package":
                pack = new DevelopmentPack { Id = offer.id, RedGems = 3_000, DataChips = 30_000, ChipsetIds = new[] { 1, 3 }, OnceOnly = true, SuccessMessage = "3,000 GEMS • 30,000 DATA CHIPS • STANDARD GUN x7 • ROCKET PUNCH x7" };
                break;
            case "intermediate-pack":
                pack = new DevelopmentPack { Id = offer.id, RedGems = 3_000, DataChips = 50_000, ChipsetIds = new[] { 6, 8 }, OnceOnly = true, SuccessMessage = "3,000 GEMS • 50,000 DATA CHIPS • GUN TURRET x7 • SHOTGUN x7" };
                break;
            case "advanced-pack":
                pack = new DevelopmentPack { Id = offer.id, RedGems = 3_000, DataChips = 70_000, ChipsetIds = new[] { 10, 7 }, OnceOnly = true, SuccessMessage = "3,000 GEMS • 70,000 DATA CHIPS • HIGH-EXPLOSIVE MINE x7 • SPIKY DISCUS x7" };
                break;
            case "gun-pack":
                pack = new DevelopmentPack { Id = offer.id, RedGems = 3_000, ChipsetIds = new[] { 1, 8, 2 }, OnceOnly = true, SuccessMessage = "3,000 GEMS • STANDARD GUN x7 • SHOTGUN x7 • RIFLE x7" };
                break;
            case "drone-pack":
                pack = new DevelopmentPack { Id = offer.id, RedGems = 3_000, BuddyIds = new[] { 1, 6, 10 }, OnceOnly = true, SuccessMessage = "3,000 GEMS • SLOY x7 • MINE MAKER x7 • PURIFYING DRONE x7" };
                break;
            case "gem-1": pack = GemPack(offer.id, 160); break;
            case "gem-2": pack = GemPack(offer.id, 1_000); break;
            case "gem-3": pack = GemPack(offer.id, 2_400); break;
            case "gem-4": pack = GemPack(offer.id, 5_000); break;
            case "gem-5": pack = GemPack(offer.id, 13_000); break;
            case "gem-6": pack = GemPack(offer.id, 28_000); break;
        }

        return pack != null;
    }

    private static DevelopmentPack GemPack(string id, int amount)
    {
        return new DevelopmentPack { Id = id, RedGems = amount, SuccessMessage = $"RECEIVED {amount:N0} GEMS" };
    }

    private static bool CanAdd(int current, int amount)
    {
        return current >= 0 && amount >= 0 && (long)current + amount <= int.MaxValue;
    }

    private static bool HasChipsetOverflow(ChipItemData[] chipsets)
    {
        for (int i = 0; i < chipsets.Length; i++)
        {
            if (!CanAdd(chipsets[i].count, PackageItemPieces)) return true;
        }
        return false;
    }

    private static bool HasBuddyOverflow(int[] pieceCounts)
    {
        for (int i = 0; i < pieceCounts.Length; i++)
        {
            if (!CanAdd(pieceCounts[i], PackageItemPieces)) return true;
        }
        return false;
    }

    private static void GrantPackageCurrency(int redGems, int dataChips)
    {
        if (ChipManager.IsTestMode)
        {
            PlayerDataService.RedGems += redGems;
            PlayerDataService.DataChips += dataChips;
        }

        ChipManager.AddRedGems(redGems);
        ChipManager.AddDataChips(dataChips);
    }

    private static void RestorePackageCurrency(
        int displayedRedGems,
        int displayedDataChips,
        int savedRedGems,
        int savedDataChips)
    {
        PlayerDataService.RedGems = savedRedGems;
        PlayerDataService.DataChips = savedDataChips;

        if (ChipManager.IsTestMode)
        {
            ChipManager.RedGems = displayedRedGems;
            ChipManager.DataChips = displayedDataChips;
        }
    }

    private static ChipItemData GetSavedChipsetSnapshot(int chipsetId)
    {
        ChipItemData snapshot = null;
        var defaults = ChipsetController.CreateDefaultDatabase();
        for (int i = 0; i < defaults.Count; i++)
        {
            if (defaults[i] != null && defaults[i].id == chipsetId)
            {
                snapshot = defaults[i].Clone();
                break;
            }
        }

        if (snapshot == null)
        {
            throw new InvalidOperationException($"Missing chipset definition for ID {chipsetId}.");
        }

        if (PlayerDataService.LoadChipsetItemData(
                chipsetId,
                out int level,
                out int tier,
                out int count,
                out int requiredCount,
                out bool hasStar))
        {
            snapshot.level = level;
            snapshot.tier = (ChipTier)tier;
            snapshot.count = count;
            snapshot.requiredCount = requiredCount;
            snapshot.hasStar = hasStar;
        }

        return snapshot;
    }

    private static void SaveChipsetPieces(ChipItemData chipset, int amount)
    {
        PlayerDataService.SaveChipsetItemData(
            chipset.id,
            chipset.level,
            (int)chipset.tier,
            chipset.count + amount,
            chipset.requiredCount,
            chipset.hasStar);
    }

    private static void RestoreChipsetSnapshot(ChipItemData chipset, bool existed)
    {
        if (existed)
        {
            PlayerDataService.SaveChipsetItemData(
                chipset.id,
                chipset.level,
                (int)chipset.tier,
                chipset.count,
                chipset.requiredCount,
                chipset.hasStar);
            return;
        }

        string prefix = PlayerDataService.GetChipItemPrefix(chipset.id);
        PlayerPrefs.DeleteKey($"{prefix}Level");
        PlayerPrefs.DeleteKey($"{prefix}Tier");
        PlayerPrefs.DeleteKey($"{prefix}Count");
        PlayerPrefs.DeleteKey($"{prefix}ReqCount");
        PlayerPrefs.DeleteKey($"{prefix}HasStar");
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

    private string BuildSuccessMessage(Offer offer, IReadOnlyList<ShopBoxDropRoller.Drop> boxDrops)
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
                if (boxDrops != null && boxDrops.Count > 0)
                    return BuildBoxDropMessage("CHIPSET", boxDrops);
                return $"OPENED {offer.rewardAmount:N0} CHIPSET BOXES  •  TOTAL {chipsetBoxes:N0}";
            default:
                if (boxDrops != null && boxDrops.Count > 0)
                    return BuildBoxDropMessage("BUDDY", boxDrops);
                return $"OPENED {offer.rewardAmount:N0} DRONE BOXES  •  TOTAL {droneBoxes:N0}";
        }
    }

    private static string BuildBoxDropMessage(
        string category,
        IReadOnlyList<ShopBoxDropRoller.Drop> drops)
    {
        int totalPieces = 0;
        for (int i = 0; i < drops.Count; i++) totalPieces += drops[i].Pieces;
        return $"{category} BOX OPENED • {totalPieces:N0} PIECES ADDED";
    }

    public void SetBoxRandomSeedForTesting(int seed)
    {
        boxRandom = new System.Random(seed);
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
                if (TryGetDevelopmentPack(offer, out DevelopmentPack developmentPack) && developmentPack.OnceOnly)
                {
                    claimed |= WasPurchasedOnce(offer.id);
                }
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

    public static bool WasPurchasedOnce(string offerId)
    {
        return !string.IsNullOrWhiteSpace(offerId) &&
               PlayerPrefs.GetInt(GetPurchasedOnceKey(offerId), 0) == 1;
    }

    private static string GetPurchasedOnceKey(string offerId)
    {
        return $"PGE.Shop.Purchased.{offerId}";
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

/// <summary>
/// Pure, deterministic drop logic for Shop boxes. Persistence is handled by ShopController.
/// </summary>
public static class ShopBoxDropRoller
{
    public enum BoxCategory
    {
        Chipset,
        Buddy
    }

    public readonly struct Drop
    {
        public Drop(int itemId, int pieces)
        {
            ItemId = itemId;
            Pieces = pieces;
        }

        public int ItemId { get; }
        public int Pieces { get; }
    }

    private static readonly int[] ChipsetIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    private static readonly int[] BuddyIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    public static List<Drop> Roll(BoxCategory category, int boxCount, System.Random random)
    {
        if (boxCount <= 0) throw new ArgumentOutOfRangeException(nameof(boxCount));
        if (random == null) throw new ArgumentNullException(nameof(random));

        int[] itemIds = category == BoxCategory.Chipset ? ChipsetIds : BuddyIds;
        var totalsById = new Dictionary<int, int>();
        var orderedIds = new List<int>();

        for (int i = 0; i < boxCount; i++)
        {
            int rateRoll = random.Next(100);
            int pieces = rateRoll < 7 ? 7 : rateRoll < 30 ? 3 : 1;
            int itemId = itemIds[random.Next(itemIds.Length)];

            if (!totalsById.ContainsKey(itemId))
            {
                totalsById[itemId] = 0;
                orderedIds.Add(itemId);
            }

            totalsById[itemId] += pieces;
        }

        var results = new List<Drop>(orderedIds.Count);
        for (int i = 0; i < orderedIds.Count; i++)
        {
            int itemId = orderedIds[i];
            results.Add(new Drop(itemId, totalsById[itemId]));
        }

        return results;
    }
}
