using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Cửa Hàng (Shop):
/// Sử dụng ChipManager để quản lý toàn bộ các giao dịch tiền tệ và đồng bộ dữ liệu.
/// 1. Mua gói nạp Gem đỏ (VND / Miễn phí).
/// 2. Đổi Gem đỏ lấy Data Chip xanh (2.000 / 4.250 / 12.200 Data Chips).
/// 3. Mở hòm trang bị (Chipset Box / Drone Box).
/// 4. Tương thích hoàn hảo với Chế độ Test Vô Hạn Chip của ChipManager.
/// </summary>
public sealed class ShopController : MonoBehaviour
{
    private const string ChipsetBoxCountKey = "PGE.Shop.Inventory.ChipsetBoxes";
    private const string DroneBoxCountKey = "PGE.Shop.Inventory.DroneBoxes";

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

    [Header("Balances & Header UI")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text dataChipText;
    [SerializeField] private TMP_Text redGemText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Offers")]
    [SerializeField] private Offer[] offers;

    private int currentEnergy;
    private int currentDataChips;
    private int currentRedGems;
    private int chipsetBoxes;
    private int droneBoxes;

    private void Awake()
    {
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, ChipManager.MaxEnergy);
        currentDataChips = ChipManager.DataChips;
        currentRedGems = ChipManager.RedGems;
        chipsetBoxes = Mathf.Max(0, PlayerPrefs.GetInt(ChipsetBoxCountKey, 0));
        droneBoxes = Mathf.Max(0, PlayerPrefs.GetInt(DroneBoxCountKey, 0));
    }

    private void Start()
    {
        if (offers != null)
        {
            for (int i = 0; i < offers.Length; i++)
            {
                int offerIndex = i;
                if (offers[i].button != null)
                {
                    offers[i].button.onClick.AddListener(() => TryPurchase(offerIndex));
                }
            }
        }

        RefreshView();
        ShowMessage("DAILY SHOP READY");
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged += HandleRedGemsChanged;
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;

        currentDataChips = ChipManager.DataChips;
        currentRedGems = ChipManager.RedGems;
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, ChipManager.MaxEnergy);
        RefreshView();
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged -= HandleRedGemsChanged;
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;
    }

    private void OnDestroy()
    {
        if (offers == null)
        {
            return;
        }

        for (int i = 0; i < offers.Length; i++)
        {
            if (offers[i].button != null)
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

    private void HandleTestModeChanged(bool isTest)
    {
        currentDataChips = ChipManager.DataChips;
        currentRedGems = ChipManager.RedGems;
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, ChipManager.MaxEnergy);
        RefreshView();
    }

    public bool TryPurchase(int offerIndex)
    {
        if (offerIndex < 0 || offers == null || offerIndex >= offers.Length)
        {
            return false;
        }

        Offer offer = offers[offerIndex];
        if (offer.oncePerDay && WasClaimedToday(offer.id))
        {
            ShowMessage($"{offer.displayName} ALREADY CLAIMED TODAY");
            return false;
        }

        // VND purchases: Fail-closed không trao reward cho đến khi có payment/IAP flow thật
        if (offer.currency == CurrencyType.VND)
        {
            ShowMessage("IAP PAYMENT COMING SOON");
            return false;
        }

        // Kiểm tra số dư Gem đỏ
        if (offer.currency == CurrencyType.RedGem && !ChipManager.HasEnoughRedGems(offer.price))
        {
            ShowMessage("NOT ENOUGH RED GEMS");
            return false;
        }

        // Khấu trừ Gem đỏ
        if (offer.currency == CurrencyType.RedGem && offer.price > 0)
        {
            if (!ChipManager.TrySpendRedGems(offer.price))
            {
                ShowMessage("NOT ENOUGH RED GEMS");
                return false;
            }
        }

        // Trao phần thưởng (Data Chip / Gem / Energy / Box)
        GrantReward(offer.reward, offer.rewardAmount);

        if (offer.oncePerDay)
        {
            PlayerPrefs.SetString(GetDailyKey(offer.id), DateTime.UtcNow.ToString("yyyyMMdd"));
        }

        SaveState();
        RefreshView();
        ShowMessage(BuildSuccessMessage(offer));
        return true;
    }

    public void SetOffersForTesting(Offer[] testOffers)
    {
        offers = testOffers;
    }

    private void GrantReward(RewardType reward, int amount)
    {
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
                chipsetBoxes += amount;
                break;
            case RewardType.DroneBox:
                droneBoxes += amount;
                break;
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
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(ChipsetBoxCountKey, chipsetBoxes);
        PlayerPrefs.SetInt(DroneBoxCountKey, droneBoxes);
        PlayerPrefs.Save();
    }

    private static bool WasClaimedToday(string offerId)
    {
        return PlayerPrefs.GetString(GetDailyKey(offerId), string.Empty) ==
            DateTime.UtcNow.ToString("yyyyMMdd");
    }

    private static string GetDailyKey(string offerId)
    {
        return $"PGE.Shop.Daily.{offerId}";
    }
}
