using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LabUpgradeController : MonoBehaviour
{
    public enum ItemRarity
    {
        Common,
        Elite,
        Epic,
        Legend
    }

    [Serializable]
    public class ItemEntry
    {
        [Header("Item Settings")]
        [Tooltip("Tên item hiển thị sau khi được mở khóa.")]
        public string itemName;

        [Tooltip("Hình ảnh của item hiển thị trong ô khi đã mở khóa.")]
        public Sprite itemIcon;

        [FormerlySerializedAs("itemType")]
        [Tooltip("Độ hiếm của item được tự động xác định theo hàng: Common, Elite, Epic hoặc Legend.")]
        public ItemRarity rarity;

        [Min(0f)]
        [Tooltip("Tỷ lệ tương đối của item bên trong cùng một loại. Ví dụ 10 dễ trúng gấp đôi 5; đặt 0 để không thể quay trúng item này.")]
        public float dropWeight = 1f;

        [Tooltip("Bật nếu item này được mở khóa sẵn khi bắt đầu game; tắt để item bắt đầu ở trạng thái khóa.")]
        public bool startsUnlocked;

        [Min(1)]
        [Tooltip("Cấp độ ban đầu của item khi mục 'Starts Unlocked' được bật.")]
        public int startingLevel = 1;

        [Header("Slot View")]
        [Tooltip("Nhóm GameObject hiển thị ổ khóa và chữ LOCKED.")]
        public GameObject lockedGroup;

        [Tooltip("Nhóm GameObject hiển thị hình, tên và cấp độ của item đã mở khóa.")]
        public GameObject unlockedGroup;

        [Tooltip("Image dùng để hiển thị sprite của item sau khi mở khóa.")]
        public Image iconImage;

        [Tooltip("Text hiển thị cấp độ hiện tại của item.")]
        public TMP_Text levelText;

        [Tooltip("Text hiển thị tên của item.")]
        public TMP_Text nameText;

        [Tooltip("Ảnh nền của ô item, dùng để đổi màu khi khóa, mở khóa hoặc đang được quay chọn.")]
        public Image slotBackground;

        [NonSerialized] public int level;
    }

    [Header("Items & Chances")]
    [Tooltip("Danh sách toàn bộ item có thể quay trúng cùng tỷ lệ, trạng thái ban đầu và tham chiếu giao diện.")]
    [SerializeField] private ItemEntry[] items;

    [Header("Rarity Backgrounds & Random Rates")]
    [FormerlySerializedAs("type1Weight")]
    [InspectorName("Common Random Weight")]
    [Min(0f)]
    [Tooltip("Tỷ lệ quay trúng hàng Common (hàng 1). Đặt 0 để tắt toàn bộ item Common.")]
    [SerializeField] private float commonWeight = 55f;

    [InspectorName("Common Background Color")]
    [Tooltip("Màu nền của toàn bộ item Common ở hàng 1, kể cả khi item còn khóa.")]
    [SerializeField] private Color commonBackgroundColor = new Color32(48, 94, 111, 255);

    [FormerlySerializedAs("type2Weight")]
    [InspectorName("Elite Random Weight")]
    [Min(0f)]
    [Tooltip("Tỷ lệ quay trúng hàng Elite (hàng 2). Đặt 0 để tắt toàn bộ item Elite.")]
    [SerializeField] private float eliteWeight = 30f;

    [InspectorName("Elite Background Color")]
    [Tooltip("Màu nền xanh dương của toàn bộ item Elite ở hàng 2, kể cả khi item còn khóa.")]
    [SerializeField] private Color eliteBackgroundColor = new Color32(38, 82, 145, 255);

    [FormerlySerializedAs("type3Weight")]
    [InspectorName("Epic Random Weight")]
    [Min(0f)]
    [Tooltip("Tỷ lệ quay trúng hàng Epic (hàng 3). Đặt 0 để tắt toàn bộ item Epic.")]
    [SerializeField] private float epicWeight = 12f;

    [InspectorName("Epic Background Color")]
    [Tooltip("Màu nền tím của toàn bộ item Epic ở hàng 3, kể cả khi item còn khóa.")]
    [SerializeField] private Color epicBackgroundColor = new Color32(94, 55, 142, 255);

    [FormerlySerializedAs("type4Weight")]
    [InspectorName("Legend Random Weight")]
    [Min(0f)]
    [Tooltip("Tỷ lệ quay trúng hàng Legend (hàng 4). Đặt 0 để tắt toàn bộ item Legend.")]
    [SerializeField] private float legendWeight = 3f;

    [InspectorName("Legend Background Color")]
    [Tooltip("Màu nền vàng của toàn bộ item Legend ở hàng 4, kể cả khi item còn khóa.")]
    [SerializeField] private Color legendBackgroundColor = new Color32(170, 128, 35, 255);

    [Header("UI References")]
    [Tooltip("Nút UPGRADE dùng để bắt đầu một lượt quay ngẫu nhiên.")]
    [SerializeField] private Button upgradeButton;

    [Tooltip("Text hiển thị năng lượng hiện tại theo định dạng hiện tại/100.")]
    [SerializeField] private TMP_Text energyBalanceText;

    [Tooltip("Text hiển thị số chip xanh hiện tại; tự rút gọn thành k/m và sẽ giảm khi nâng cấp.")]
    [SerializeField] private TMP_Text chipBalanceText;

    [Tooltip("Text hiển thị số chip đỏ hiện tại; tự rút gọn thành k/m khi số lượng lớn.")]
    [SerializeField] private TMP_Text redChipBalanceText;

    [Tooltip("Text hiển thị giá chip xanh cần trả cho lượt nâng cấp kế tiếp.")]
    [SerializeField] private TMP_Text priceText;

    [Tooltip("Text thông báo kết quả quay: item vừa mở khóa hoặc cấp độ vừa tăng.")]
    [SerializeField] private TMP_Text resultText;

    [Tooltip("Ảnh nền của nút UPGRADE, dùng để đổi màu khi đủ hoặc thiếu chip.")]
    [SerializeField] private Image upgradeBackground;

    [Header("Test Resources")]
    [Min(0)]
    [Tooltip("Năng lượng hiện tại khi bắt đầu để thử nghiệm; giá trị runtime được giới hạn tối đa là 100.")]
    [SerializeField] private int startingEnergy = 30;

    [InspectorName("Starting Green Chips")]
    [Min(0)]
    [Tooltip("Số chip xanh khi bắt đầu để thử nghiệm và thanh toán các lượt UPGRADE.")]
    [SerializeField] private int startingChips = 700;

    [Min(0)]
    [Tooltip("Số chip đỏ khi bắt đầu để thử nghiệm; hiện tại chỉ dùng để hiển thị.")]
    [SerializeField] private int startingRedChips = 10;

    [Header("Upgrade Cost")]
    [Tooltip("Giá chip xanh của lượt UPGRADE đầu tiên.")]
    [SerializeField] private int basePrice = 300;

    [Tooltip("Số chip xanh cộng thêm vào giá sau mỗi lượt quay hoàn tất.")]
    [SerializeField] private int priceStep = 150;

    [Header("Roll Animation")]
    [Min(0.1f)]
    [Tooltip("Tổng thời gian chạy hiệu ứng quay qua các ô item, tính bằng giây.")]
    [SerializeField] private float rollDuration = 1.35f;

    [Min(0.02f)]
    [Tooltip("Khoảng thời gian chuyển vùng sáng sang ô item tiếp theo trong lúc quay, tính bằng giây.")]
    [SerializeField] private float rollStep = 0.075f;

    [Tooltip("Màu nền tạm thời của ô item đang được hiệu ứng quay đi qua.")]
    [SerializeField] private Color rollHighlightColor = new Color32(255, 203, 73, 255);

    private static readonly Color AffordableColor = new Color32(84, 180, 105, 255);
    private static readonly Color UnaffordableColor = new Color32(67, 105, 109, 255);
    private const int MaxEnergy = 100;

    private int currentChips;
    private int currentEnergy;
    private int currentPrice;
    private int completedRolls;
    private int pendingItemIndex = -1;
    private bool isRolling;
    private bool hasInitialized;
    private DateTime nextEnergyRecoveryUtc;
    private Coroutine energyRecoveryCoroutine;

    private void OnValidate()
    {
        AssignRaritiesByRow();
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].slotBackground != null)
            {
                items[i].slotBackground.color = GetRarityColor(items[i].rarity);
            }
        }
    }

    private void AssignRaritiesByRow()
    {
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                items[i].rarity = (ItemRarity)Mathf.Clamp(i / 4, 0, 3);
            }
        }
    }

    public const string ChipsetBalanceKey = "PGE.Shop.Balance.Chipsets";
    public const string RedGemBalanceKey = "PGE.Shop.Balance.RedGems";
    public const string EnergyBalanceKey = "PGE.Lab.Balance.Energy";
    public const string NextEnergyUtcKey = "PGE.Lab.NextEnergyUtc";
    public const string CompletedRollsKey = "PGE.Lab.CompletedRolls";
    public const string ItemLevelKeyPrefix = "PGE.Lab.ItemLevel.";

    public static string GetItemLevelKey(string itemName, int index)
    {
        return !string.IsNullOrEmpty(itemName)
            ? $"{ItemLevelKeyPrefix}{itemName.Trim().ToUpperInvariant()}"
            : $"{ItemLevelKeyPrefix}Slot_{index}";
    }

    private void Start()
    {
        currentEnergy = Mathf.Clamp(PlayerPrefs.GetInt(EnergyBalanceKey, startingEnergy), 0, MaxEnergy);
        currentChips = Mathf.Max(0, PlayerPrefs.GetInt(ChipsetBalanceKey, startingChips));
        startingRedChips = Mathf.Max(0, PlayerPrefs.GetInt(RedGemBalanceKey, startingRedChips));
        completedRolls = Mathf.Max(0, PlayerPrefs.GetInt(CompletedRollsKey, 0));
        currentPrice = basePrice + completedRolls * priceStep;

        string savedUtcStr = PlayerPrefs.GetString(NextEnergyUtcKey, string.Empty);
        if (DateTime.TryParse(savedUtcStr, null, DateTimeStyles.RoundtripKind, out DateTime parsedUtc))
        {
            nextEnergyRecoveryUtc = parsedUtc;
        }
        else
        {
            nextEnergyRecoveryUtc = DateTime.UtcNow.AddMinutes(1d);
        }

        hasInitialized = true;
        AssignRaritiesByRow();

        for (int i = 0; i < items.Length; i++)
        {
            ItemEntry item = items[i];
            int defaultLevel = item.startsUnlocked ? Mathf.Max(1, item.startingLevel) : 0;
            item.level = PlayerPrefs.GetInt(GetItemLevelKey(item.itemName, i), defaultLevel);
            RefreshItemView(item);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(StartRoll);
        }

        if (resultText != null)
        {
            resultText.text = "ROLL FOR A RANDOM UPGRADE";
        }

        RecoverEnergyFromClock();
        RefreshMainView();
        StartEnergyRecovery();
    }

    private void OnEnable()
    {
        if (!hasInitialized)
        {
            return;
        }

        currentChips = Mathf.Max(0, PlayerPrefs.GetInt(ChipsetBalanceKey, currentChips));
        startingRedChips = Mathf.Max(0, PlayerPrefs.GetInt(RedGemBalanceKey, startingRedChips));
        currentEnergy = Mathf.Clamp(PlayerPrefs.GetInt(EnergyBalanceKey, currentEnergy), 0, MaxEnergy);

        RecoverEnergyFromClock();
        RefreshMainView();
        StartEnergyRecovery();
    }

    private void OnDisable()
    {
        StopEnergyRecovery();

        if (!isRolling || pendingItemIndex < 0)
        {
            return;
        }

        StopAllCoroutines();
        ResolvePendingItem();
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(StartRoll);
        }
    }

    private void StartRoll()
    {
        if (isRolling || currentChips < currentPrice)
        {
            return;
        }

        pendingItemIndex = ChooseWeightedItemIndex();
        if (pendingItemIndex < 0)
        {
            return;
        }

        currentChips -= currentPrice;
        isRolling = true;
        RefreshMainView();
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        float elapsed = 0f;
        int previousVisualIndex = -1;

        while (elapsed < rollDuration)
        {
            RestoreSlotColor(previousVisualIndex);

            int visualIndex = UnityEngine.Random.Range(0, items.Length);
            SetSlotColor(visualIndex, rollHighlightColor);
            previousVisualIndex = visualIndex;

            float progress = Mathf.Clamp01(elapsed / rollDuration);
            float delay = Mathf.Lerp(rollStep * 0.6f, rollStep * 2f, progress);
            yield return new WaitForSecondsRealtime(delay);
            elapsed += delay;
        }

        RestoreSlotColor(previousVisualIndex);
        SetSlotColor(pendingItemIndex, rollHighlightColor);
        yield return new WaitForSecondsRealtime(0.3f);
        ResolvePendingItem();
    }

    private int ChooseWeightedItemIndex()
    {
        int selectedRarityIndex = ChooseWeightedRarityIndex();
        if (selectedRarityIndex < 0)
        {
            return -1;
        }

        ItemRarity selectedRarity = (ItemRarity)selectedRarityIndex;
        float itemTotalWeight = GetItemWeightForRarity(selectedRarity);
        float roll = UnityEngine.Random.value * itemTotalWeight;
        int fallbackIndex = -1;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].rarity != selectedRarity)
            {
                continue;
            }

            float weight = Mathf.Max(0f, items[i].dropWeight);
            if (weight <= 0f)
            {
                continue;
            }

            fallbackIndex = i;
            if (roll < weight)
            {
                return i;
            }

            roll -= weight;
        }

        return fallbackIndex;
    }

    private int ChooseWeightedRarityIndex()
    {
        float totalWeight = GetTotalRarityWeight();
        if (totalWeight <= 0f)
        {
            return -1;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        int fallbackRarityIndex = -1;
        for (int rarityIndex = 0; rarityIndex < 4; rarityIndex++)
        {
            ItemRarity rarity = (ItemRarity)rarityIndex;
            float rarityWeight = GetItemWeightForRarity(rarity) > 0f
                ? GetRarityWeight(rarity)
                : 0f;

            if (rarityWeight <= 0f)
            {
                continue;
            }

            fallbackRarityIndex = rarityIndex;
            if (roll < rarityWeight)
            {
                return rarityIndex;
            }

            roll -= rarityWeight;
        }

        return fallbackRarityIndex;
    }

    private void ResolvePendingItem()
    {
        if (pendingItemIndex < 0 || pendingItemIndex >= items.Length)
        {
            FinishRoll();
            return;
        }

        ItemEntry item = items[pendingItemIndex];
        bool wasLocked = item.level <= 0;
        item.level = wasLocked ? 1 : item.level + 1;
        RefreshItemView(item);

        if (resultText != null)
        {
            resultText.text = wasLocked
                ? $"UNLOCKED  {item.itemName}"
                : $"{item.itemName}  LEVEL {item.level:00}";
        }

        completedRolls++;
        currentPrice = basePrice + completedRolls * priceStep;

        SaveState(item, pendingItemIndex);
        FinishRoll();
    }

    private void SaveState(ItemEntry item, int itemIndex)
    {
        PlayerPrefs.SetInt(ChipsetBalanceKey, currentChips);
        PlayerPrefs.SetInt(CompletedRollsKey, completedRolls);
        if (item != null)
        {
            PlayerPrefs.SetInt(GetItemLevelKey(item.itemName, itemIndex), item.level);
        }
        PlayerPrefs.Save();
    }

    private void FinishRoll()
    {
        RestoreSlotColor(pendingItemIndex);
        pendingItemIndex = -1;
        isRolling = false;
        RefreshMainView();
    }

    private void RefreshItemView(ItemEntry item)
    {
        bool unlocked = item.level > 0;

        if (item.lockedGroup != null)
        {
            item.lockedGroup.SetActive(!unlocked);
        }

        if (item.unlockedGroup != null)
        {
            item.unlockedGroup.SetActive(unlocked);
        }

        if (item.iconImage != null)
        {
            item.iconImage.sprite = item.itemIcon;
        }

        if (item.levelText != null)
        {
            item.levelText.text = $"LV.{Mathf.Max(1, item.level):00}";
        }

        if (item.nameText != null)
        {
            item.nameText.text = item.itemName;
        }

        if (item.slotBackground != null)
        {
            item.slotBackground.color = GetRarityColor(item.rarity);
        }
    }

    private void RefreshMainView()
    {
        if (energyBalanceText != null)
        {
            energyBalanceText.text = $"{currentEnergy}/{MaxEnergy}";
        }

        if (chipBalanceText != null)
        {
            chipBalanceText.text = FormatChipAmount(currentChips);
        }

        if (redChipBalanceText != null)
        {
            redChipBalanceText.text = FormatChipAmount(startingRedChips);
        }

        if (priceText != null)
        {
            priceText.text = currentPrice.ToString();
        }

        bool canRoll = !isRolling && currentChips >= currentPrice && GetTotalRarityWeight() > 0f;
        if (upgradeButton != null)
        {
            upgradeButton.interactable = canRoll;
        }

        if (upgradeBackground != null)
        {
            upgradeBackground.color = canRoll ? AffordableColor : UnaffordableColor;
        }
    }

    private void StartEnergyRecovery()
    {
        if (energyRecoveryCoroutine == null)
        {
            energyRecoveryCoroutine = StartCoroutine(EnergyRecoveryRoutine());
        }
    }

    private void StopEnergyRecovery()
    {
        if (energyRecoveryCoroutine == null)
        {
            return;
        }

        StopCoroutine(energyRecoveryCoroutine);
        energyRecoveryCoroutine = null;
    }

    private IEnumerator EnergyRecoveryRoutine()
    {
        WaitForSecondsRealtime refreshInterval = new WaitForSecondsRealtime(1f);
        while (true)
        {
            yield return refreshInterval;
            RecoverEnergyFromClock();
        }
    }

    private void RecoverEnergyFromClock()
    {
        DateTime now = DateTime.UtcNow;
        if (currentEnergy >= MaxEnergy)
        {
            nextEnergyRecoveryUtc = now.AddMinutes(1d);
            PlayerPrefs.SetInt(EnergyBalanceKey, currentEnergy);
            PlayerPrefs.SetString(NextEnergyUtcKey, nextEnergyRecoveryUtc.ToString("o"));
            PlayerPrefs.Save();
            return;
        }

        if (now < nextEnergyRecoveryUtc)
        {
            return;
        }

        int elapsedRecoveryPoints = 1 + (int)Math.Floor((now - nextEnergyRecoveryUtc).TotalMinutes);
        int recoveredPoints = Math.Min(MaxEnergy - currentEnergy, elapsedRecoveryPoints);
        currentEnergy += recoveredPoints;
        nextEnergyRecoveryUtc = nextEnergyRecoveryUtc.AddMinutes(recoveredPoints);

        PlayerPrefs.SetInt(EnergyBalanceKey, currentEnergy);
        PlayerPrefs.SetString(NextEnergyUtcKey, nextEnergyRecoveryUtc.ToString("o"));
        PlayerPrefs.Save();

        if (energyBalanceText != null)
        {
            energyBalanceText.text = $"{currentEnergy}/{MaxEnergy}";
        }
    }

    private float GetTotalRarityWeight()
    {
        float totalWeight = 0f;
        for (int rarityIndex = 0; rarityIndex < 4; rarityIndex++)
        {
            ItemRarity rarity = (ItemRarity)rarityIndex;
            if (GetItemWeightForRarity(rarity) > 0f)
            {
                totalWeight += GetRarityWeight(rarity);
            }
        }

        return totalWeight;
    }

    private float GetItemWeightForRarity(ItemRarity rarity)
    {
        float totalWeight = 0f;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].rarity == rarity)
            {
                totalWeight += Mathf.Max(0f, items[i].dropWeight);
            }
        }

        return totalWeight;
    }

    private float GetRarityWeight(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Mathf.Max(0f, commonWeight);
            case ItemRarity.Elite:
                return Mathf.Max(0f, eliteWeight);
            case ItemRarity.Epic:
                return Mathf.Max(0f, epicWeight);
            case ItemRarity.Legend:
                return Mathf.Max(0f, legendWeight);
            default:
                return 0f;
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Elite:
                return eliteBackgroundColor;
            case ItemRarity.Epic:
                return epicBackgroundColor;
            case ItemRarity.Legend:
                return legendBackgroundColor;
            default:
                return commonBackgroundColor;
        }
    }

    private static string FormatChipAmount(int amount)
    {
        if (amount >= 1_000_000)
        {
            return (amount / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "m";
        }

        if (amount >= 10_000)
        {
            return (amount / 1_000d).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        }

        return amount.ToString(CultureInfo.InvariantCulture);
    }

    private void SetSlotColor(int itemIndex, Color color)
    {
        if (itemIndex < 0 || itemIndex >= items.Length || items[itemIndex].slotBackground == null)
        {
            return;
        }

        items[itemIndex].slotBackground.color = color;
    }

    private void RestoreSlotColor(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= items.Length)
        {
            return;
        }

        RefreshItemView(items[itemIndex]);
    }
}
