using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopController : MonoBehaviour
{
    private const string ChipsetBalanceKey = "PGE.Shop.Balance.Chipsets";
    private const string RedGemBalanceKey = "PGE.Shop.Balance.RedGems";
    private const string ChipsetBoxCountKey = "PGE.Shop.Inventory.ChipsetBoxes";
    private const string DroneBoxCountKey = "PGE.Shop.Inventory.DroneBoxes";

    public enum CurrencyType
    {
        Free,
        RedGem
    }

    public enum RewardType
    {
        RedGem,
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

    [Header("Balances")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text chipsetText;
    [SerializeField] private TMP_Text redGemText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private int startingEnergy = 24;
    [SerializeField] private int maximumEnergy = 50;
    [SerializeField] private int startingChipsets = 134936;
    [SerializeField] private int startingRedGems = 15516;

    [Header("Offers")]
    [SerializeField] private Offer[] offers;

    private int currentEnergy;
    private int currentChipsets;
    private int currentRedGems;
    private int chipsetBoxes;
    private int droneBoxes;

    private void Start()
    {
        currentEnergy = Mathf.Clamp(startingEnergy, 0, maximumEnergy);
        currentChipsets = Mathf.Max(0, PlayerPrefs.GetInt(ChipsetBalanceKey, startingChipsets));
        currentRedGems = Mathf.Max(0, PlayerPrefs.GetInt(RedGemBalanceKey, startingRedGems));
        chipsetBoxes = Mathf.Max(0, PlayerPrefs.GetInt(ChipsetBoxCountKey, 0));
        droneBoxes = Mathf.Max(0, PlayerPrefs.GetInt(DroneBoxCountKey, 0));

        for (int i = 0; i < offers.Length; i++)
        {
            int offerIndex = i;
            if (offers[i].button != null)
            {
                offers[i].button.onClick.AddListener(() => TryPurchase(offerIndex));
            }
        }

        RefreshView();
        ShowMessage("DAILY SHOP READY");
    }

    private void OnEnable()
    {
        currentChipsets = Mathf.Max(0, PlayerPrefs.GetInt(ChipsetBalanceKey, startingChipsets));
        currentRedGems = Mathf.Max(0, PlayerPrefs.GetInt(RedGemBalanceKey, startingRedGems));
        currentEnergy = Mathf.Clamp(PlayerPrefs.GetInt("PGE.Lab.Balance.Energy", startingEnergy), 0, maximumEnergy);
        RefreshView();
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

    private void TryPurchase(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= offers.Length)
        {
            return;
        }

        Offer offer = offers[offerIndex];
        if (offer.oncePerDay && WasClaimedToday(offer.id))
        {
            ShowMessage($"{offer.displayName} ALREADY CLAIMED TODAY");
            return;
        }

        if (offer.currency == CurrencyType.RedGem && currentRedGems < offer.price)
        {
            ShowMessage("NOT ENOUGH RED GEMS");
            return;
        }

        if (offer.currency == CurrencyType.RedGem)
        {
            currentRedGems -= offer.price;
        }

        GrantReward(offer.reward, offer.rewardAmount);

        if (offer.oncePerDay)
        {
            PlayerPrefs.SetString(GetDailyKey(offer.id), DateTime.UtcNow.ToString("yyyyMMdd"));
        }

        SaveState();
        RefreshView();
        ShowMessage(BuildSuccessMessage(offer));
    }

    private void GrantReward(RewardType reward, int amount)
    {
        switch (reward)
        {
            case RewardType.RedGem:
                currentRedGems += amount;
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
            energyText.text = $"{currentEnergy}/{maximumEnergy}";
        }

        if (chipsetText != null)
        {
            chipsetText.text = currentChipsets.ToString("N0");
        }

        if (redGemText != null)
        {
            redGemText.text = currentRedGems.ToString("N0");
        }

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

    private void ShowMessage(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(ChipsetBalanceKey, currentChipsets);
        PlayerPrefs.SetInt(RedGemBalanceKey, currentRedGems);
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
