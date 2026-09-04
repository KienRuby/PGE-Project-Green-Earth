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

        [Tooltip("Button của ô slot để bắt sự kiện bấm vào xem chi tiết.")]
        public Button slotButton;

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
    [SerializeField] private Color commonBackgroundColor = new Color32(245, 245, 245, 255);

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

    [Header("Pity Guarantees (Bảo Hiểm Số Lượt Quay Theo Bậc Màu)")]
    [Tooltip("Bật/Tắt toàn bộ hệ thống bảo hiểm roll.")]
    [SerializeField] private bool enablePitySystem = true;

    [InspectorName("Elite Pity (Rolls)")]
    [Min(0)]
    [Tooltip("Số lượt quay tối đa chắc chắn trúng Elite (Xanh) hoặc cao hơn. Mặc định 10. Đặt 0 để tắt.")]
    [SerializeField] private int elitePityThreshold = 10;

    [InspectorName("Epic Pity (Rolls)")]
    [Min(0)]
    [Tooltip("Số lượt quay tối đa chắc chắn trúng Epic (Tím) hoặc Legend. Mặc định 25. Đặt 0 để tắt.")]
    [SerializeField] private int epicPityThreshold = 25;

    [InspectorName("Legend Pity (Rolls)")]
    [Min(0)]
    [Tooltip("Số lượt quay tối đa chắc chắn 100% trúng Legend (Vàng kim). Mặc định 50. Đặt 0 để tắt.")]
    [SerializeField] private int legendPityThreshold = 50;

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

    [Tooltip("Text hiển thị thông tin / tiến trình bảo hiểm (ví dụ: 'Guaranteed in: 5 rolls'). Tùy chọn.")]
    [SerializeField] private TMP_Text pityCounterText;

    [Tooltip("Slider hiển thị tiến trình tích lũy bảo hiểm (tùy chọn).")]
    [SerializeField] private Slider pityProgressSlider;

    [Tooltip("Nút mở bảng xem chi tiết bảo hiểm lượt roll (PityInfoButton).")]
    [SerializeField] private Button pityInfoButton;

    [Tooltip("Panel giao diện hiển thị chi tiết tiến độ bảo hiểm lượt roll (PityGuaranteePanel).")]
    [SerializeField] private PityGuaranteePanel pityGuaranteePanel;

    [Header("Stat Tooltip & Lock Icon")]
    [Tooltip("Bảng hiển thị chi tiết cấp tiếp theo của chỉ số hoặc ??? khi đang khóa.")]
    [SerializeField] private LabStatTooltip statTooltip;

    [Tooltip("Sprite icon ổ khóa hiển thị trong ô khi đang khóa.")]
    [SerializeField] private Sprite lockIconSprite;

    // Public Read-Only Properties for Pity Guarantee Panel
    public bool IsPitySystemEnabled => enablePitySystem;
    public int ElitePityThreshold => elitePityThreshold;
    public int EpicPityThreshold => epicPityThreshold;
    public int LegendPityThreshold => legendPityThreshold;
    public int ElitePityCounter => elitePityCounter;
    public int EpicPityCounter => epicPityCounter;
    public int LegendPityCounter => legendPityCounter;
    public Color CommonRarityColor => commonBackgroundColor;
    public Color EliteRarityColor => eliteBackgroundColor;
    public Color EpicRarityColor => epicBackgroundColor;
    public Color LegendRarityColor => legendBackgroundColor;

    public event Action OnPityDataChanged;

    [Header("Upgrade Cost & Level Cap")]
    [Tooltip("Cấp độ tối đa của từng chỉ số trong Lab (mặc định: 10).")]
    [SerializeField] private int maxStatLevel = 10;

    public const int DefaultMaxLevel = 10;
    public int MaxStatLevel => maxStatLevel > 0 ? maxStatLevel : DefaultMaxLevel;

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

    [Tooltip("Màu khung viền phát sáng của ô item khi quay qua hoặc trúng thưởng.")]
    [SerializeField] private Color rollHighlightColor = new Color32(255, 215, 0, 255);

    [Tooltip("Sprite khung viền phát sáng màu vàng bao quanh ô.")]
    [SerializeField] private Sprite highlightBorderSprite;

    [Tooltip("Màu của hiệu ứng đèn rọi sáng bừng nhấp nháy khi trúng thưởng.")]
    [SerializeField] private Color highlightFlashColor = new Color32(255, 255, 180, 255);

    private RectTransform highlightFrameRoot;
    private Image highlightBorderImage;
    private Image highlightFlashImage;
    private Coroutine winningFlashCoroutine;

    private static readonly Color AffordableColor = new Color32(84, 180, 105, 255);
    private static readonly Color UnaffordableColor = new Color32(67, 105, 109, 255);
    private const int MaxEnergy = 100;

    private int currentChips;
    private int currentRedChips;
    private int currentEnergy;
    private int currentPrice;
    private int completedRolls;
    private int elitePityCounter;
    private int epicPityCounter;
    private int legendPityCounter;
    private ItemRarity? activeGuaranteedRarity;
    private int pendingItemIndex = -1;
    private bool isRolling;
    private bool hasInitialized;
    private DateTime nextEnergyRecoveryUtc;
    private Coroutine energyRecoveryCoroutine;

    private void OnValidate()
    {
        AssignRaritiesByRow();
#if UNITY_EDITOR
        if (highlightBorderSprite == null)
        {
            highlightBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Lab/khung_highlight_lab.png");
        }
#endif
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].slotBackground != null)
            {
                if (items[i].unlockedGroup != null && items[i].slotBackground.gameObject == items[i].unlockedGroup)
                {
                    items[i].slotBackground.color = Color.white;
                }
                else
                {
                    items[i].slotBackground.color = GetRarityColor(items[i].rarity);
                }
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

    public const string ChipsetBalanceKey = PlayerDataService.ChipsetsKey;
    public const string RedGemBalanceKey = PlayerDataService.RedGemsKey;
    public const string EnergyBalanceKey = PlayerDataService.EnergyKey;
    public const string NextEnergyUtcKey = PlayerDataService.NextEnergyUtcKey;
    public const string CompletedRollsKey = PlayerDataService.CompletedRollsKey;
    public const string ItemLevelKeyPrefix = PlayerDataService.ItemLevelKeyPrefix;

    public static string GetItemLevelKey(string itemName, int index)
    {
        return !string.IsNullOrEmpty(itemName)
            ? PlayerDataService.FormatItemLevelKey(itemName)
            : $"{ItemLevelKeyPrefix}Slot_{index}";
    }

    private void Start()
    {
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, MaxEnergy);
        currentChips = ChipManager.DataChips;
        currentRedChips = ChipManager.RedGems;
        completedRolls = PlayerDataService.CompletedRolls;
        elitePityCounter = PlayerDataService.LabElitePityCounter;
        epicPityCounter = PlayerDataService.LabEpicPityCounter;
        legendPityCounter = PlayerDataService.LabLegendPityCounter;
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
            int defaultLevel = item.startsUnlocked ? Mathf.Clamp(item.startingLevel, 1, MaxStatLevel) : 0;
            item.level = Mathf.Clamp(PlayerPrefs.GetInt(GetItemLevelKey(item.itemName, i), defaultLevel), 0, MaxStatLevel);
            RefreshItemView(item);

            int slotIndex = i;
            if (item.slotButton != null)
            {
                item.slotButton.onClick.RemoveAllListeners();
                item.slotButton.onClick.AddListener(() => OnSlotClicked(slotIndex));
            }
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(StartRoll);
        }

        if (pityInfoButton != null)
        {
            pityInfoButton.onClick.RemoveListener(OpenPityPanel);
            pityInfoButton.onClick.AddListener(OpenPityPanel);
        }

        if (resultText != null)
        {
            resultText.text = "ROLL FOR A RANDOM UPGRADE";
        }

        RecoverEnergyFromClock();
        RefreshMainView();
        RefreshPityUI();
        StartEnergyRecovery();
    }

    public void OpenPityPanel()
    {
        if (pityGuaranteePanel != null)
        {
            pityGuaranteePanel.Open();
        }
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged += HandleRedGemsChanged;
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;

        if (!hasInitialized)
        {
            return;
        }

        currentChips = ChipManager.DataChips;
        currentRedChips = ChipManager.RedGems;
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, MaxEnergy);
        completedRolls = PlayerDataService.CompletedRolls;
        currentPrice = basePrice + completedRolls * priceStep;
        elitePityCounter = PlayerDataService.LabElitePityCounter;
        epicPityCounter = PlayerDataService.LabEpicPityCounter;
        legendPityCounter = PlayerDataService.LabLegendPityCounter;

        RecoverEnergyFromClock();
        RefreshMainView();
        RefreshPityUI();
        StartEnergyRecovery();
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged -= HandleRedGemsChanged;
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;

        StopEnergyRecovery();

        if (!isRolling || pendingItemIndex < 0)
        {
            RestoreSlotHighlight();
            return;
        }

        StopAllCoroutines();
        winningFlashCoroutine = null;
        ResolvePendingItem();
        FinishRoll();
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(StartRoll);
        }

        if (pityInfoButton != null)
        {
            pityInfoButton.onClick.RemoveListener(OpenPityPanel);
        }
    }

    private void StartRoll()
    {
        if (statTooltip != null)
        {
            statTooltip.Hide();
        }

        if (isRolling || IsAllItemsMaxed() || !ChipManager.HasEnoughDataChips(currentPrice))
        {
            return;
        }

        activeGuaranteedRarity = null;
        if (enablePitySystem)
        {
            if (legendPityThreshold > 0 && (legendPityCounter + 1 >= legendPityThreshold))
            {
                activeGuaranteedRarity = ItemRarity.Legend;
            }
            else if (epicPityThreshold > 0 && (epicPityCounter + 1 >= epicPityThreshold))
            {
                activeGuaranteedRarity = ItemRarity.Epic;
            }
            else if (elitePityThreshold > 0 && (elitePityCounter + 1 >= elitePityThreshold))
            {
                activeGuaranteedRarity = ItemRarity.Elite;
            }
        }

        pendingItemIndex = ChooseWeightedItemIndex(activeGuaranteedRarity);
        if (pendingItemIndex < 0)
        {
            return;
        }

        ChipManager.TrySpendDataChips(currentPrice);
        currentChips = ChipManager.DataChips;
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
            int visualIndex = UnityEngine.Random.Range(0, items.Length);
            SetSlotHighlight(visualIndex);
            previousVisualIndex = visualIndex;

            float progress = Mathf.Clamp01(elapsed / rollDuration);
            float delay = Mathf.Lerp(rollStep * 0.6f, rollStep * 2f, progress);
            yield return new WaitForSecondsRealtime(delay);
            elapsed += delay;
        }

        // 1. Dừng tại ô trúng thưởng, bật khung viền phát sáng màu vàng
        SetSlotHighlight(pendingItemIndex);
        yield return new WaitForSecondsRealtime(0.35f);

        // 2. Mở khóa ô (nếu đang locked -> mở ra và hiển thị LV.01, icon) hoặc nâng cấp level
        ResolvePendingItem();

        // 3. Giữ khung viền vàng tại ô vừa mở/nâng cấp
        SetSlotHighlight(pendingItemIndex);

        // 4. Bật hiệu ứng: Thẻ sáng bừng lên như có đèn rọi vào nhấp nháy
        winningFlashCoroutine = StartCoroutine(WinningFlashRoutine());
        yield return winningFlashCoroutine;

        // 5. Kết thúc lượt quay, ẩn khung highlight
        FinishRoll();
    }

    private int ChooseWeightedItemIndex(ItemRarity? minGuaranteedRarity = null)
    {
        int selectedRarityIndex = ChooseWeightedRarityIndex(minGuaranteedRarity);
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
            if (items[i].rarity != selectedRarity || items[i].level >= MaxStatLevel)
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

    private int ChooseWeightedRarityIndex(ItemRarity? minGuaranteedRarity = null)
    {
        int minRarity = 0;
        int maxRarity = 3;

        if (minGuaranteedRarity.HasValue)
        {
            minRarity = (int)minGuaranteedRarity.Value;
            maxRarity = 3;
        }

        float totalWeight = 0f;
        for (int r = minRarity; r <= maxRarity; r++)
        {
            ItemRarity rarity = (ItemRarity)r;
            if (GetItemWeightForRarity(rarity) > 0f)
            {
                totalWeight += GetRarityWeight(rarity);
            }
        }

        // Nếu bậc bảo hiểm đã max hết toàn bộ 4 chỉ số, fallback sang các bậc còn lại có thể nâng cấp
        if (totalWeight <= 0f)
        {
            minRarity = 0;
            maxRarity = 3;
            for (int r = minRarity; r <= maxRarity; r++)
            {
                ItemRarity rarity = (ItemRarity)r;
                if (GetItemWeightForRarity(rarity) > 0f)
                {
                    totalWeight += GetRarityWeight(rarity);
                }
            }
        }

        if (totalWeight <= 0f)
        {
            return -1;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        int fallbackRarityIndex = minRarity;
        for (int r = minRarity; r <= maxRarity; r++)
        {
            ItemRarity rarity = (ItemRarity)r;
            float rarityWeight = GetItemWeightForRarity(rarity) > 0f
                ? GetRarityWeight(rarity)
                : 0f;

            if (rarityWeight <= 0f)
            {
                continue;
            }

            fallbackRarityIndex = r;
            if (roll < rarityWeight)
            {
                return r;
            }

            roll -= rarityWeight;
        }

        return fallbackRarityIndex;
    }

    private void ResolvePendingItem()
    {
        if (pendingItemIndex < 0 || pendingItemIndex >= items.Length)
        {
            return;
        }

        ItemEntry item = items[pendingItemIndex];
        bool wasLocked = item.level <= 0;
        item.level = Mathf.Min(MaxStatLevel, wasLocked ? 1 : item.level + 1);
        RefreshItemView(item);

        if (wasLocked && item.unlockedGroup != null)
        {
            StartCoroutine(PunchScaleRoutine(item.unlockedGroup.transform));
        }

        bool isMaxed = item.level >= MaxStatLevel;
        if (activeGuaranteedRarity.HasValue)
        {
            if (resultText != null)
            {
                string rName = item.rarity.ToString().ToUpperInvariant();
                resultText.text = wasLocked
                    ? $"★ GUARANTEED {rName}: UNLOCKED {item.itemName} ★"
                    : (isMaxed
                        ? $"★ GUARANTEED {rName}: {item.itemName} LV.{item.level:00} (MAX) ★"
                        : $"★ GUARANTEED {rName}: {item.itemName} LV.{item.level:00} ★");
            }
        }
        else
        {
            if (resultText != null)
            {
                resultText.text = wasLocked
                    ? $"UNLOCKED  {item.itemName}"
                    : (isMaxed
                        ? $"{item.itemName}  LEVEL {item.level:00} (MAX)"
                        : $"{item.itemName}  LEVEL {item.level:00}");
            }
        }

        // Cập nhật bộ đếm Pity độc lập cho từng bậc (Lam / Tím / Vàng)
        if (item.rarity == ItemRarity.Legend)
        {
            // Chỉ reset bảo hiểm chỉ số Vàng (Legend), không động đến Lam và Tím (vẫn tăng tiến độ bình thường)
            legendPityCounter = 0;
            elitePityCounter++;
            epicPityCounter++;
        }
        else if (item.rarity == ItemRarity.Epic)
        {
            // Chỉ reset bảo hiểm chỉ số Tím (Epic), không động đến Lam và Vàng (vẫn tăng tiến độ bình thường)
            epicPityCounter = 0;
            elitePityCounter++;
            legendPityCounter++;
        }
        else if (item.rarity == ItemRarity.Elite)
        {
            // Chỉ reset bảo hiểm chỉ số Lam (Elite), không động đến Tím và Vàng (vẫn tăng tiến độ bình thường)
            elitePityCounter = 0;
            epicPityCounter++;
            legendPityCounter++;
        }
        else // Common
        {
            // Không trúng bậc nào trong 3 bậc có bảo hiểm -> Tăng cả 3 bộ đếm
            elitePityCounter++;
            epicPityCounter++;
            legendPityCounter++;
        }

        completedRolls++;
        currentPrice = basePrice + completedRolls * priceStep;

        SaveState(item, pendingItemIndex);
        RefreshPityUI();
        activeGuaranteedRarity = null;
    }

    private IEnumerator PunchScaleRoutine(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 originalScale = Vector3.one;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(1.15f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
            target.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = originalScale;
        }
    }

    private void SaveState(ItemEntry item, int itemIndex)
    {
        PlayerDataService.CompletedRolls = completedRolls;
        PlayerDataService.LabPityCounter = elitePityCounter;
        PlayerDataService.LabElitePityCounter = elitePityCounter;
        PlayerDataService.LabEpicPityCounter = epicPityCounter;
        PlayerDataService.LabLegendPityCounter = legendPityCounter;
        if (item != null)
        {
            PlayerPrefs.SetInt(GetItemLevelKey(item.itemName, itemIndex), item.level);
        }
        PlayerPrefs.Save();
    }

    private void RefreshPityUI()
    {
        if (!enablePitySystem)
        {
            if (pityCounterText != null) pityCounterText.gameObject.SetActive(false);
            if (pityProgressSlider != null) pityProgressSlider.gameObject.SetActive(false);
            return;
        }

        int remLegend = legendPityThreshold > 0 ? Mathf.Max(0, legendPityThreshold - legendPityCounter) : -1;
        int remEpic = epicPityThreshold > 0 ? Mathf.Max(0, epicPityThreshold - epicPityCounter) : -1;
        int remElite = elitePityThreshold > 0 ? Mathf.Max(0, elitePityThreshold - elitePityCounter) : -1;

        if (pityCounterText != null)
        {
            pityCounterText.gameObject.SetActive(true);
            if (remLegend == 1)
            {
                pityCounterText.text = "<color=#FFCB49>★ NEXT: GUARANTEED LEGEND (GOLD)! ★</color>";
            }
            else if (remEpic == 1)
            {
                pityCounterText.text = "<color=#C05BF5>★ NEXT: GUARANTEED EPIC (PURPLE)! ★</color>";
            }
            else if (remElite == 1)
            {
                pityCounterText.text = "<color=#50E1DC>★ NEXT: GUARANTEED COLORED STAT! ★</color>";
            }
            else if (remElite > 1 && remLegend > 1)
            {
                pityCounterText.text = $"Guaranteed Colored: <color=#50E1DC>{remElite}</color> | Legend: <color=#FFCB49>{remLegend}</color>";
            }
            else if (remElite > 1)
            {
                pityCounterText.text = $"Guaranteed Colored in: <color=#50E1DC>{remElite}</color> rolls";
            }
            else if (remLegend > 1)
            {
                pityCounterText.text = $"Guaranteed Legend in: <color=#FFCB49>{remLegend}</color> rolls";
            }
            else
            {
                pityCounterText.text = "Guaranteed Roll Active";
            }
        }

        if (pityProgressSlider != null && elitePityThreshold > 0)
        {
            pityProgressSlider.gameObject.SetActive(true);
            pityProgressSlider.maxValue = elitePityThreshold;
            pityProgressSlider.value = elitePityCounter;
        }

        OnPityDataChanged?.Invoke();
    }

    private void FinishRoll()
    {
        RestoreSlotHighlight();
        RestoreSlotColor(pendingItemIndex);
        pendingItemIndex = -1;
        isRolling = false;
        RefreshMainView();
    }

    public bool IsItemMaxed(int index)
    {
        if (items == null || index < 0 || index >= items.Length || items[index] == null)
        {
            return false;
        }
        return items[index].level >= MaxStatLevel;
    }

    public bool IsAllItemsMaxed()
    {
        if (items == null || items.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].level < MaxStatLevel)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshItemView(ItemEntry item)
    {
        bool unlocked = item.level > 0;

        if (item.lockedGroup != null)
        {
            item.lockedGroup.SetActive(!unlocked);

            Image lockedCard = item.lockedGroup.transform.Find("LockIcon")?.GetComponent<Image>();
            if (lockedCard != null)
            {
                lockedCard.sprite = lockIconSprite != null ? lockIconSprite : item.itemIcon;
                lockedCard.color = Color.white;
                lockedCard.preserveAspect = true;
                lockedCard.rectTransform.anchorMin = new Vector2(0.5f, 0.52f);
                lockedCard.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
                lockedCard.rectTransform.anchoredPosition = Vector2.zero;
                lockedCard.rectTransform.sizeDelta = new Vector2(100f, 120f);
            }
        }

        if (item.unlockedGroup != null)
        {
            item.unlockedGroup.SetActive(unlocked);
            Image unlockedCard = item.unlockedGroup.GetComponent<Image>();
            if (unlockedCard != null && item.itemIcon != null && unlockedCard.sprite != item.itemIcon)
            {
                unlockedCard.sprite = item.itemIcon;
            }
        }

        if (item.iconImage != null && (item.unlockedGroup == null || item.iconImage.gameObject != item.unlockedGroup))
        {
            item.iconImage.sprite = item.itemIcon;
            item.iconImage.preserveAspect = true;
            item.iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.52f);
            item.iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
            item.iconImage.rectTransform.anchoredPosition = Vector2.zero;
            item.iconImage.rectTransform.sizeDelta = new Vector2(140f, 140f);
        }

        if (item.levelText != null)
        {
            if (item.level >= MaxStatLevel)
            {
                item.levelText.text = $"LV.{MaxStatLevel:00}";
            }
            else
            {
                item.levelText.text = $"LV.{Mathf.Max(1, item.level):00}";
            }
        }

        if (item.nameText != null)
        {
            item.nameText.text = item.itemName;
            item.nameText.gameObject.SetActive(false);
        }

        if (item.slotBackground != null)
        {
            if (item.unlockedGroup != null && item.slotBackground.gameObject == item.unlockedGroup)
            {
                item.slotBackground.color = Color.white;
            }
            else
            {
                item.slotBackground.color = GetRarityColor(item.rarity);
            }
        }

        if (statTooltip != null && statTooltip.IsShowing && items != null)
        {
            int index = Array.IndexOf(items, item);
            if (index >= 0 && statTooltip.CurrentSlotIndex == index)
            {
                RectTransform slotRt = item.slotBackground != null ? item.slotBackground.rectTransform : null;
                statTooltip.Show(index, slotRt, item.itemName, item.level, !unlocked, MaxStatLevel);
            }
        }
    }

    public void OnSlotClicked(int index)
    {
        if (items == null || index < 0 || index >= items.Length || items[index] == null)
        {
            return;
        }

        if (statTooltip != null)
        {
            ItemEntry item = items[index];
            RectTransform slotRt = item.slotBackground != null ? item.slotBackground.rectTransform : null;
            bool isLocked = item.level <= 0;
            statTooltip.Show(index, slotRt, item.itemName, item.level, isLocked, MaxStatLevel);
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
            redChipBalanceText.text = FormatChipAmount(currentRedChips);
        }

        bool allMaxed = IsAllItemsMaxed();

        if (priceText != null)
        {
            priceText.text = allMaxed ? "MAX" : currentPrice.ToString();
        }

        if (allMaxed && resultText != null && !isRolling)
        {
            resultText.text = "ALL STATS MAXED OUT (LV.10)";
        }

        bool canRoll = !isRolling && !allMaxed && ChipManager.HasEnoughDataChips(currentPrice) && GetTotalRarityWeight() > 0f;
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
        if (ChipManager.Energy >= MaxEnergy)
        {
            nextEnergyRecoveryUtc = now.AddMinutes(1d);
            PlayerPrefs.SetString(NextEnergyUtcKey, nextEnergyRecoveryUtc.ToString("o"));
            PlayerPrefs.Save();
            return;
        }

        if (now < nextEnergyRecoveryUtc)
        {
            return;
        }

        int elapsedRecoveryPoints = 1 + (int)Math.Floor((now - nextEnergyRecoveryUtc).TotalMinutes);
        int recoveredPoints = Math.Min(MaxEnergy - ChipManager.Energy, elapsedRecoveryPoints);
        if (recoveredPoints > 0)
        {
            ChipManager.AddEnergy(recoveredPoints);
            currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, MaxEnergy);
        }
        nextEnergyRecoveryUtc = nextEnergyRecoveryUtc.AddMinutes(recoveredPoints);

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
            if (items[i].rarity == rarity && items[i].level < MaxStatLevel)
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

    private void EnsureHighlightFrame()
    {
        if (highlightFrameRoot != null)
        {
            return;
        }

#if UNITY_EDITOR
        if (highlightBorderSprite == null)
        {
            highlightBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Lab/khung_highlight_lab.png");
        }
#endif

        GameObject frameGo = new GameObject("SelectionHighlightFrame", typeof(RectTransform));
        frameGo.layer = LayerMask.NameToLayer("UI");
        highlightFrameRoot = frameGo.GetComponent<RectTransform>();

        // 1. Flash Overlay (sáng bừng lên như đèn rọi)
        GameObject flashGo = new GameObject("FlashOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashGo.layer = LayerMask.NameToLayer("UI");
        RectTransform flashRt = flashGo.GetComponent<RectTransform>();
        flashRt.SetParent(highlightFrameRoot, false);
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero;
        flashRt.offsetMax = Vector2.zero;

        highlightFlashImage = flashGo.GetComponent<Image>();
        highlightFlashImage.color = Color.clear;
        highlightFlashImage.raycastTarget = false;

        // 2. Glowing Border (khung viền phát sáng màu vàng)
        GameObject borderGo = new GameObject("BorderImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderGo.layer = LayerMask.NameToLayer("UI");
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        borderRt.SetParent(highlightFrameRoot, false);
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        // Ôm trọn bên ngoài khung thẻ để phát sáng nổi bật (+6px mỗi cạnh)
        borderRt.offsetMin = new Vector2(-6f, -6f);
        borderRt.offsetMax = new Vector2(6f, 6f);

        highlightBorderImage = borderGo.GetComponent<Image>();
        if (highlightBorderSprite != null)
        {
            highlightBorderImage.sprite = highlightBorderSprite;
            highlightBorderImage.type = Image.Type.Sliced;
        }
        highlightBorderImage.color = rollHighlightColor;
        highlightBorderImage.raycastTarget = false;

        highlightFrameRoot.gameObject.SetActive(false);
    }

    private void SetSlotHighlight(int itemIndex)
    {
        if (items == null || itemIndex < 0 || itemIndex >= items.Length || items[itemIndex] == null)
        {
            return;
        }

        EnsureHighlightFrame();

        Transform parentSlot = null;
        if (items[itemIndex].unlockedGroup != null)
        {
            parentSlot = items[itemIndex].unlockedGroup.transform.parent;
        }
        if (parentSlot == null && items[itemIndex].lockedGroup != null)
        {
            parentSlot = items[itemIndex].lockedGroup.transform.parent;
        }
        if (parentSlot == null && items[itemIndex].slotButton != null)
        {
            parentSlot = items[itemIndex].slotButton.transform;
        }

        if (parentSlot != null)
        {
            highlightFrameRoot.SetParent(parentSlot, false);
            highlightFrameRoot.anchorMin = Vector2.zero;
            highlightFrameRoot.anchorMax = Vector2.one;
            highlightFrameRoot.offsetMin = Vector2.zero;
            highlightFrameRoot.offsetMax = Vector2.zero;
            highlightFrameRoot.localScale = Vector3.one;
            highlightFrameRoot.localRotation = Quaternion.identity;
            highlightFrameRoot.SetAsLastSibling();
            highlightFrameRoot.gameObject.SetActive(true);

            if (highlightFlashImage != null)
            {
                highlightFlashImage.color = Color.clear;
            }

            if (highlightBorderImage != null)
            {
                highlightBorderImage.color = rollHighlightColor;
            }
        }
    }

    private void RestoreSlotHighlight()
    {
        if (winningFlashCoroutine != null)
        {
            StopCoroutine(winningFlashCoroutine);
            winningFlashCoroutine = null;
        }

        if (highlightFlashImage != null)
        {
            highlightFlashImage.color = Color.clear;
        }

        if (highlightFrameRoot != null)
        {
            highlightFrameRoot.localScale = Vector3.one;
            highlightFrameRoot.gameObject.SetActive(false);
        }
    }

    private IEnumerator WinningFlashRoutine()
    {
        if (highlightFlashImage == null)
        {
            yield return new WaitForSecondsRealtime(0.85f);
            yield break;
        }

        float totalDuration = 0.9f;
        float elapsed = 0f;

        // Thẻ sáng bừng lên như có đèn rọi vào và nhấp nháy ~3 nhịp
        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / totalDuration);

            // Tần số nhấp nháy ~3.5 chu kỳ
            float wave = Mathf.Sin(progress * Mathf.PI * 7f);
            float envelope = 1f - progress;
            float flashAlpha = Mathf.Clamp01((0.35f + 0.5f * Mathf.Max(0f, wave)) * envelope);

            Color c = highlightFlashColor;
            c.a = flashAlpha;
            highlightFlashImage.color = c;

            if (highlightFrameRoot != null)
            {
                float scale = 1f + 0.08f * Mathf.Max(0f, wave) * envelope;
                highlightFrameRoot.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        if (highlightFlashImage != null)
        {
            highlightFlashImage.color = Color.clear;
        }

        if (highlightFrameRoot != null)
        {
            highlightFrameRoot.localScale = Vector3.one;
        }
    }

    private void SetSlotColor(int itemIndex, Color color)
    {
        // Highlight is cleanly handled by SelectionHighlightFrame (Glowing Yellow Border + Flash Overlay)
        // to preserve the card's original artwork without multiplicative color distortion.
    }

    private void RestoreSlotColor(int itemIndex)
    {
        if (items == null || itemIndex < 0 || itemIndex >= items.Length || items[itemIndex] == null)
        {
            return;
        }

        ItemEntry item = items[itemIndex];

        if (item.lockedGroup != null)
        {
            Image[] lockedImgs = item.lockedGroup.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < lockedImgs.Length; i++)
            {
                if (lockedImgs[i] != null)
                {
                    lockedImgs[i].color = Color.white;
                }
            }
        }

        if (item.unlockedGroup != null)
        {
            Image[] unlockedImgs = item.unlockedGroup.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < unlockedImgs.Length; i++)
            {
                if (unlockedImgs[i] != null)
                {
                    unlockedImgs[i].color = Color.white;
                }
            }
        }

        RefreshItemView(item);
    }

    private void HandleDataChipsChanged(int newAmount)
    {
        currentChips = newAmount;
        RefreshMainView();
    }

    private void HandleRedGemsChanged(int newAmount)
    {
        currentRedChips = newAmount;
        RefreshMainView();
    }

    private void HandleEnergyChanged(int newAmount)
    {
        currentEnergy = Mathf.Clamp(newAmount, 0, MaxEnergy);
        RefreshMainView();
    }

    private void HandleTestModeChanged(bool isTest)
    {
        currentChips = ChipManager.DataChips;
        currentRedChips = ChipManager.RedGems;
        currentEnergy = Mathf.Clamp(ChipManager.Energy, 0, MaxEnergy);
        RefreshMainView();
    }
}
