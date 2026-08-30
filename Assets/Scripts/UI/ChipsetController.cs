using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChipTier
{
    Magic = 1,       // Tier 1: Max LV. 6
    Rare = 2,        // Tier 2: Max LV. 9
    Unique = 3,      // Tier 3: Max LV. 14
    Epic = 4,        // Tier 4: Max LV. 18
    Holographic = 5  // Tier 5 / Advance Tier: Max LV. 24 (Requires 10 Advance Stones to Breakthrough)
}

[Serializable]
public class ChipItemData
{
    public int id;
    public string chipName;
    public string iconKey;
    public ChipTier tier = ChipTier.Magic;
    public int level = 1;
    public int count = 0;
    public int requiredCount = 3;
    public int enhanceCost = 500;
    public int tierEnhanceCount;
    public bool hasStar;

    [NonSerialized] private bool tierUnlockRulesEnabled;
    [NonSerialized] private int requiredTierEnhances = 10;
    [NonSerialized] private int greenToBlueFragmentCost = 3;
    [NonSerialized] private int blueToPurpleFragmentCost = 5;
    [NonSerialized] private int purpleToYellowFragmentCost = 10;
    [NonSerialized] private int yellowToRedDataChipCost = 10;

    [Header("Stats Description")]
    [TextArea(2, 4)]
    public string description;
    public string baseStatsSummary;
    public string magicBonus;
    public string rareBonus;
    public string uniqueBonus;
    public string epicBonus;

    public static int GetMaxLevelForTier(ChipTier tier)
    {
        switch (tier)
        {
            case ChipTier.Magic: return 6;
            case ChipTier.Rare: return 9;
            case ChipTier.Unique: return 14;
            case ChipTier.Epic: return 18;
            case ChipTier.Holographic: return 24;
            default: return 6;
        }
    }

    public int MaxLevel => GetMaxLevelForTier(tier);
    public bool IsAtTierCap => level >= MaxLevel;
    public bool IsMaxOverall => tier == ChipTier.Holographic && level >= 24;
    public bool NeedsAdvanceStones => tier == ChipTier.Epic && IsAtTierCap;
    public int AdvanceStoneCost => NeedsAdvanceStones ? 10 : 0;

    public int RequiredTierEnhances => requiredTierEnhances;
    public bool IsTierUnlockReady => tierUnlockRulesEnabled
        ? tierEnhanceCount >= requiredTierEnhances
        : IsAtTierCap;
    public bool UsesRedDataChipForAdvance => tierUnlockRulesEnabled && tier == ChipTier.Epic;
    public int CurrentAdvanceCost
    {
        get
        {
            switch (tier)
            {
                case ChipTier.Magic: return greenToBlueFragmentCost;
                case ChipTier.Rare: return blueToPurpleFragmentCost;
                case ChipTier.Unique: return purpleToYellowFragmentCost;
                case ChipTier.Epic: return yellowToRedDataChipCost;
                default: return 0;
            }
        }
    }

    public bool HasAdvanceCurrency => tier < ChipTier.Holographic && (UsesRedDataChipForAdvance
        ? ChipManager.HasEnoughRedGems(CurrentAdvanceCost)
        : count >= CurrentAdvanceCost);

    public bool CanEnhance => tierUnlockRulesEnabled
        ? ChipManager.HasEnoughDataChips(enhanceCost) &&
          (tier == ChipTier.Holographic ? !IsMaxOverall : tierEnhanceCount < requiredTierEnhances)
        : ChipManager.DataChips >= enhanceCost && !IsAtTierCap && !IsMaxOverall;
    public bool CanUpgrade => !tierUnlockRulesEnabled && !IsAtTierCap && count >= requiredCount && requiredCount > 0;
    public bool CanAdvanceTier => tierUnlockRulesEnabled
        ? tier < ChipTier.Holographic && IsTierUnlockReady && HasAdvanceCurrency
        : IsAtTierCap && tier < ChipTier.Holographic;

    public void ConfigureTierUnlockRules(
        int enhancesRequired,
        int greenToBlueCost,
        int blueToPurpleCost,
        int purpleToYellowCost,
        int yellowToRedCost)
    {
        tierUnlockRulesEnabled = true;
        requiredTierEnhances = Mathf.Max(1, enhancesRequired);
        greenToBlueFragmentCost = Mathf.Max(0, greenToBlueCost);
        blueToPurpleFragmentCost = Mathf.Max(0, blueToPurpleCost);
        purpleToYellowFragmentCost = Mathf.Max(0, purpleToYellowCost);
        yellowToRedDataChipCost = Mathf.Max(0, yellowToRedCost);
        tierEnhanceCount = Mathf.Clamp(tierEnhanceCount, 0, requiredTierEnhances);

        if (tier < ChipTier.Epic)
        {
            requiredCount = CurrentAdvanceCost;
        }
    }

    public bool Enhance()
    {
        if (!CanEnhance) return false;
        if (!ChipManager.TrySpendDataChips(enhanceCost)) return false;
        if (tierUnlockRulesEnabled)
        {
            if (tier < ChipTier.Holographic)
            {
                tierEnhanceCount++;
            }
            level = Mathf.Min(level + 1, MaxLevel);
        }
        else
        {
            level++;
        }
        enhanceCost = Mathf.RoundToInt(enhanceCost * 1.35f);
        return true;
    }

    public void Upgrade()
    {
        if (!CanUpgrade) return;
        count -= requiredCount;
        level++;
        if (level >= MaxLevel)
        {
            requiredCount = 0;
        }
        else
        {
            requiredCount = Mathf.RoundToInt(requiredCount * 1.4f) + 1;
        }
    }

    public bool AdvanceTier()
    {
        if (!CanAdvanceTier) return false;

        if (tierUnlockRulesEnabled)
        {
            if (UsesRedDataChipForAdvance)
            {
                if (!ChipManager.TrySpendRedGems(CurrentAdvanceCost)) return false;
            }
            else
            {
                count -= CurrentAdvanceCost;
            }
        }
        else if (NeedsAdvanceStones)
        {
            if (!ChipManager.TrySpendAdvanceStones(10))
            {
                return false;
            }
        }

        tier = (ChipTier)((int)tier + 1);
        if (tierUnlockRulesEnabled)
        {
            tierEnhanceCount = 0;
            if (tier < ChipTier.Epic)
            {
                requiredCount = CurrentAdvanceCost;
            }
        }
        else
        {
            requiredCount = Mathf.Max(3, level + 1);
        }
        return true;
    }

    public ChipItemData Clone()
    {
        return new ChipItemData
        {
            id = this.id,
            chipName = this.chipName,
            iconKey = this.iconKey,
            tier = this.tier,
            level = this.level,
            count = this.count,
            requiredCount = this.requiredCount,
            enhanceCost = this.enhanceCost,
            tierEnhanceCount = this.tierEnhanceCount,
            hasStar = this.hasStar,
            description = this.description,
            baseStatsSummary = this.baseStatsSummary,
            magicBonus = this.magicBonus,
            rareBonus = this.rareBonus,
            uniqueBonus = this.uniqueBonus,
            epicBonus = this.epicBonus,
            tierUnlockRulesEnabled = this.tierUnlockRulesEnabled,
            requiredTierEnhances = this.requiredTierEnhances,
            greenToBlueFragmentCost = this.greenToBlueFragmentCost,
            blueToPurpleFragmentCost = this.blueToPurpleFragmentCost,
            purpleToYellowFragmentCost = this.purpleToYellowFragmentCost,
            yellowToRedDataChipCost = this.yellowToRedDataChipCost
        };
    }
}

public class ChipsetController : MonoBehaviour
{
    [Header("Top Bar Currencies")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text chipCurrencyText;
    [SerializeField] private TMP_Text redCurrencyText;
    [SerializeField] private TMP_Text advanceStonesText;

    [Header("Top Mode Switcher")]
    [SerializeField] private Button chipsetModeBtn;
    [SerializeField] private Button highTechModeBtn;
    [SerializeField] private Image chipsetModeBg;
    [SerializeField] private Image highTechModeBg;

    [Header("Preset Decks")]
    [SerializeField] private Button preset1Btn;
    [SerializeField] private Button preset2Btn;
    [SerializeField] private Button preset3Btn;
    [SerializeField] private Image preset1Bg;
    [SerializeField] private Image preset2Bg;
    [SerializeField] private Image preset3Bg;
    [SerializeField] private TMP_Text preset1Text;
    [SerializeField] private TMP_Text preset2Text;
    [SerializeField] private TMP_Text preset3Text;

    [Header("Preset Button Sprites")]
    [SerializeField] private Sprite preset1YellowSprite;
    [SerializeField] private Sprite preset1RedSprite;
    [SerializeField] private Sprite preset2YellowSprite;
    [SerializeField] private Sprite preset2RedSprite;
    [SerializeField] private Sprite preset3YellowSprite;
    [SerializeField] private Sprite preset3RedSprite;

    [SerializeField] private Button blastFurnaceBtn;

    [Header("Equipped Grid")]
    [SerializeField] private ChipsetCardUI[] equippedSlots = new ChipsetCardUI[10];

    [Header("Sort Buttons")]
    [SerializeField] private Button byTierBtn;
    [SerializeField] private Button byQuantityBtn;
    [SerializeField] private Image byTierBg;
    [SerializeField] private Image byQuantityBg;
    [SerializeField] private TMP_Text byTierText;
    [SerializeField] private TMP_Text byQuantityText;

    [Header("Sort Button Sprites")]
    [SerializeField] private Sprite byTierYellowSprite;
    [SerializeField] private Sprite byTierGreenSprite;
    [SerializeField] private Sprite byQuantityYellowSprite;
    [SerializeField] private Sprite byQuantityGreenSprite;

    [Header("Inventory Scroll Area")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private GameObject cardPrefab;

    [Header("Detail Modal (Exact UI)")]
    [SerializeField] private GameObject detailModal;
    [SerializeField] private TMP_Text detailModBadgeText;
    [SerializeField] private ChipsetCardUI detailTopCard;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailTierText;
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private TMP_Text detailBaseStatsText;

    // 4 Tier Perk Rows
    [SerializeField] private Image[] perkRowIcons = new Image[4];
    [SerializeField] private TMP_Text[] perkRowTexts = new TMP_Text[4];

    [Header("Detail Action Buttons")]
    [SerializeField] private Button detailEnhanceBtn;
    [SerializeField] private TMP_Text detailEnhanceCostText;
    [SerializeField] private Button detailAdvanceTierBtn;
    [SerializeField] private TMP_Text detailAdvanceTierText;
    [SerializeField] private Button detailEquipBtn;
    [SerializeField] private TMP_Text detailEquipBtnText;
    [SerializeField] private Button detailCloseBtn;

    [Header("Chipset Tier Unlock Costs")]
    [SerializeField, Min(1)] private int enhancesRequiredPerTier = 10;
    [SerializeField, Min(0)] private int greenToBlueFragmentCost = 3;
    [SerializeField, Min(0)] private int blueToPurpleFragmentCost = 5;
    [SerializeField, Min(0)] private int purpleToYellowFragmentCost = 10;
    [SerializeField, Min(0)] private int yellowToRedDataChipCost = 10;

    [Header("Blast Furnace Modal")]
    [SerializeField] private GameObject furnaceModal;
    [SerializeField] private TMP_Text furnaceDescText;
    [SerializeField] private Button furnaceDismantleBtn;
    [SerializeField] private Button furnaceCloseBtn;

    [Header("Toast Message")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    [Header("Sprites Database")]
    [SerializeField] private Sprite[] chipIcons;
    [SerializeField] private Sprite[] frameSprites; // 0: Green, 1: Blue, 2: Purple, 3: Yellow, 4: Red
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite upgradeArrowSprite;
    [SerializeField] private Sprite advanceStoneSprite;
    [SerializeField] private Sprite[] lockTierSprites = new Sprite[4]; // 0: Magic, 1: Rare, 2: Unique, 3: Epic
    [SerializeField] private Sprite unlockedCheckSprite;
    [SerializeField] private ChipsetLevelVisualLibrary tierVisualLibrary;

    private int activeDeckIndex = 0; // Reference layout opens on Preset 1.
    private bool sortByQuantity = false; // Reference layout defaults to sorting by tier.
    private ChipItemData selectedDetailChip;

    // Database of all 24 chips with full user defined stats
    [SerializeField] private List<ChipItemData> allChips = new List<ChipItemData>();
    // 3 Decks holding IDs of equipped chips (10 slots each)
    private int[][] deckEquippedIds = new int[3][];
    private List<ChipsetCardUI> spawnedInventoryCards = new List<ChipsetCardUI>();

    private static readonly Color SelectedPresetColor = new Color32(255, 203, 73, 255);
    private static readonly Color NormalPresetColor = new Color32(18, 58, 68, 255);
    private static readonly Color SelectedPresetTextColor = new Color32(10, 20, 30, 255);
    private static readonly Color NormalPresetTextColor = new Color32(245, 255, 255, 255);

    public IReadOnlyList<ChipItemData> AllChips => allChips;

    private void Awake()
    {
        if (tierVisualLibrary == null)
        {
            tierVisualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        }
        InitializeDatabase();
    }

    private void Start()
    {
        SetupEventListeners();
        RefreshTopBar();
        RefreshPresetButtons();
        RefreshSortButtons();
        RefreshEquippedGrid();
        RefreshInventory();
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged += HandleCurrencyChanged;
        ChipManager.OnEnergyChanged += HandleCurrencyChanged;
        ChipManager.OnAdvanceStonesChanged += HandleCurrencyChanged;
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged -= HandleCurrencyChanged;
        ChipManager.OnEnergyChanged -= HandleCurrencyChanged;
        ChipManager.OnAdvanceStonesChanged -= HandleCurrencyChanged;
    }

    private void HandleCurrencyChanged(int _)
    {
        RefreshTopBar();
        if (detailModal != null && detailModal.activeSelf && selectedDetailChip != null)
        {
            RefreshDetailModal();
        }
    }

    public void InitializeDatabase()
    {
        if (allChips.Count == 0)
        {
            allChips = CreateSavedDatabase();
        }

        ApplyTierUnlockRulesToCatalog();
        InitializeDefaultDecks();
        activeDeckIndex = PlayerDataService.ActiveChipsetDeckIndex;
        for (int i = 0; i < deckEquippedIds.Length; i++)
        {
            deckEquippedIds[i] = PlayerDataService.LoadChipsetDeck(i, deckEquippedIds[i]);
            if (deckEquippedIds[i] == null || deckEquippedIds[i].Length == 0 || deckEquippedIds[i].All(id => id <= 0))
            {
                deckEquippedIds[i] = GetDefaultDeckIds(i);
            }
        }
    }

    private void ApplyTierUnlockRulesToCatalog()
    {
        foreach (ChipItemData chip in allChips)
        {
            chip?.ConfigureTierUnlockRules(
                enhancesRequiredPerTier,
                greenToBlueFragmentCost,
                blueToPurpleFragmentCost,
                purpleToYellowFragmentCost,
                yellowToRedDataChipCost);
        }
    }

    /// <summary>
    /// Nguồn catalog dùng chung cho MainMenu và lựa chọn Chipset khi lên cấp trong Gameplay.
    /// Mỗi lần gọi trả về một danh sách mới để tiến trình trong run không sửa dữ liệu menu.
    /// </summary>
    public static List<ChipItemData> CreateDefaultDatabase()
    {
        return new List<ChipItemData>
        {
            // 1. Standard Gun (Row 1 Col 1)
            new ChipItemData
            {
                id = 1,
                chipName = "Standard Gun",
                iconKey = "standard-gun",
                tier = ChipTier.Magic,
                level = 1,
                count = 3,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Always equipped, even when it is not included in your deck.",
                baseStatsSummary = "ATK <color=#FFCB49>53.13</color>\n<color=#FFCB49>Fast</color> ATK Speed",
                magicBonus = "ATK +15%",
                rareBonus = "ATK Speed +15%",
                uniqueBonus = "+5% Life Steal",
                epicBonus = "Adds Penetration Skill"
            },
            // 2. Rifle (Row 1 Col 2)
            new ChipItemData
            {
                id = 2,
                chipName = "Rifle",
                iconKey = "rifle",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Fires a rapid burst of bullets.",
                baseStatsSummary = "ATK <color=#FFCB49>10.5</color>\n<color=#FFCB49>Fast</color> ATK Speed",
                magicBonus = "ATK +25%",
                rareBonus = "ATK Speed +20%",
                uniqueBonus = "ATK +80%",
                epicBonus = "ATK Speed +35%"
            },
            // 3. Rocket Punch (Row 1 Col 3)
            new ChipItemData
            {
                id = 3,
                chipName = "Rocket Punch",
                iconKey = "rocket-punch",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Launches a rocket-powered fist that deals area damage.",
                baseStatsSummary = "ATK <color=#FFCB49>70</color> / AoE ATK <color=#FFCB49>37</color>\n<color=#FFCB49>Slow</color> ATK Speed",
                magicBonus = "ATK +40%",
                rareBonus = "ATK Speed +40%",
                uniqueBonus = "AoE ATK Range +40%",
                epicBonus = "ATK +180%"
            },
            // 4. Spinning Blade (Row 1 Col 4)
            new ChipItemData
            {
                id = 4,
                chipName = "Spinning Blade",
                iconKey = "spinning-blade",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Throws a spinning blade that pierces enemies and returns to the player.",
                baseStatsSummary = "ATK <color=#FFCB49>36</color>\n<color=#FFCB49>Fast</color> ATK Speed",
                magicBonus = "ATK Speed +9%",
                rareBonus = "ATK Speed +18%",
                uniqueBonus = "Spin Speed +36%",
                epicBonus = "ATK Speed +36%"
            },
            // 5. Multigun (Row 1 Col 5)
            new ChipItemData
            {
                id = 5,
                chipName = "Multigun",
                iconKey = "multigun",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Fires a rain of bullets in multiple directions at once.",
                baseStatsSummary = "ATK <color=#FFCB49>19</color> | 4 shells\n<color=#FFCB49>Slow</color> ATK Speed",
                magicBonus = "Adds +1 shells",
                rareBonus = "Adds +1 shells",
                uniqueBonus = "Adds +3 shells",
                epicBonus = "Adds +4 shells"
            },
            // 6. Gun Turret (Row 2 Col 1)
            new ChipItemData
            {
                id = 6,
                chipName = "Gun Turret",
                iconKey = "gun-turret",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Deploys a turret that fires standard rounds.",
                baseStatsSummary = "ATK <color=#FFCB49>27</color> | Duration 14.4s | CD 8.4s\n<color=#FFCB49>Fast</color> ATK Speed",
                magicBonus = "Turret Duration +20%",
                rareBonus = "Turret Cooldown -30%",
                uniqueBonus = "Turret Duration +20%",
                epicBonus = "Turret Duration +30%"
            },
            // 7. Spiky Discus (Row 2 Col 2)
            new ChipItemData
            {
                id = 7,
                chipName = "Spiky Discus",
                iconKey = "spiky-discus",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Spins a spiky discus around the player to attack enemies.",
                baseStatsSummary = "ATK <color=#FFCB49>30</color>\n<color=#FFCB49>Normal</color> Spin Speed",
                magicBonus = "+1 Discus",
                rareBonus = "Spin Speed +30%",
                uniqueBonus = "+1 Discus",
                epicBonus = "Spin Speed +35%"
            },
            // 8. Shotgun (Row 2 Col 3)
            new ChipItemData
            {
                id = 8,
                chipName = "Shotgun",
                iconKey = "shotgun",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Deals heavy damage to nearby enemies with multiple pellets.",
                baseStatsSummary = "ATK <color=#FFCB49>86</color>\n<color=#FFCB49>Slow</color> ATK Speed",
                magicBonus = "ATK +15%",
                rareBonus = "ATK +15%",
                uniqueBonus = "Adds Penetration Skill",
                epicBonus = "Fires two times in a row"
            },
            // 9. Energy Jumper Cables (Row 2 Col 4)
            new ChipItemData
            {
                id = 9,
                chipName = "Energy Jumper Cables",
                iconKey = "energy-jumper-cables",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                hasStar = false,
                description = "Steals life from enemies.",
                baseStatsSummary = "Life Steal <color=#FFCB49>2.3%</color>",
                magicBonus = "All Weapons' +1% Life Steal",
                rareBonus = "All Weapons' +1% Life Steal",
                uniqueBonus = "All Weapons' +1% Life Steal",
                epicBonus = "All Weapons' +2% Life Steal"
            },
            // 10. High-Explosive Mine (Row 2 Col 5)
            new ChipItemData
            {
                id = 10,
                chipName = "High-Explosive Mine",
                iconKey = "high-explosive-mine",
                tier = ChipTier.Magic,
                level = 1,
                count = 0,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Periodically places powerful explosive mines on the ground.",
                baseStatsSummary = "Mine AoE ATK <color=#FFCB49>27</color>\nCooldown: 5.55s",
                magicBonus = "ATK +20%",
                rareBonus = "Cooldown -20%",
                uniqueBonus = "ATK +55%",
                epicBonus = "ATK +144%"
            }
        };
    }

    /// <summary>
    /// Catalog dùng chung đã phủ tiến trình Chipset người chơi lưu từ MainMenu.
    /// </summary>
    public static List<ChipItemData> CreateSavedDatabase()
    {
        List<ChipItemData> result = CreateDefaultDatabase();
        foreach (ChipItemData chip in result)
        {
            if (!PlayerDataService.LoadChipsetItemData(
                    chip.id,
                    out int savedLevel,
                    out int savedTier,
                    out int savedCount,
                    out int savedRequiredCount,
                    out bool savedHasStar))
            {
                continue;
            }

            chip.tier = (ChipTier)Mathf.Clamp(savedTier, (int)ChipTier.Magic, (int)ChipTier.Holographic);
            chip.level = Mathf.Clamp(savedLevel, 1, ChipItemData.GetMaxLevelForTier(chip.tier));
            chip.count = Mathf.Max(0, savedCount);
            chip.requiredCount = Mathf.Max(0, savedRequiredCount);
            chip.hasStar = savedHasStar;
            chip.tierEnhanceCount = PlayerDataService.LoadChipsetTierEnhanceCount(chip.id);
            chip.enhanceCost = PlayerDataService.LoadChipsetEnhanceCost(chip.id, chip.enhanceCost);
        }
        return result;
    }

    /// <summary>
    /// Gameplay chỉ rút các Chipset thuộc preset đang hoạt động, theo đúng thứ tự deck.
    /// </summary>
    public static List<ChipItemData> CreateGameplayDatabase()
    {
        List<ChipItemData> savedCatalog = CreateSavedDatabase();
        int activeDeck = PlayerDataService.ActiveChipsetDeckIndex;
        int[] equippedIds = PlayerDataService.LoadChipsetDeck(activeDeck, GetDefaultDeckIds(activeDeck));
        return SelectEquippedCatalog(savedCatalog, equippedIds);
    }

    public static List<ChipItemData> SelectEquippedCatalog(
        IReadOnlyList<ChipItemData> source,
        IEnumerable<int> equippedIds)
    {
        if (source == null) return new List<ChipItemData>();

        Dictionary<int, ChipItemData> byId = source
            .Where(chip => chip != null)
            .GroupBy(chip => chip.id)
            .ToDictionary(group => group.Key, group => group.First());

        List<ChipItemData> equipped = (equippedIds ?? Enumerable.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .Where(byId.ContainsKey)
            .Select(id => byId[id].Clone())
            .ToList();

        return equipped.Count > 0
            ? equipped
            : source.Where(chip => chip != null).Select(chip => chip.Clone()).ToList();
    }

    private static int[] GetDefaultDeckIds(int deckIndex)
    {
        return new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    }

    private static void SaveChipProgress(ChipItemData chip)
    {
        if (chip == null) return;
        PlayerDataService.SaveChipsetItemData(
            chip.id,
            chip.level,
            (int)chip.tier,
            chip.count,
            chip.requiredCount,
            chip.hasStar);
        PlayerDataService.SaveChipsetTierEnhanceCount(chip.id, chip.tierEnhanceCount);
        PlayerDataService.SaveChipsetEnhanceCost(chip.id, chip.enhanceCost);
    }

    private void InitializeDefaultDecks()
    {
        // Preset 1 equipped chips (Slots 1 to 10 matching user screenshot):
        // 1: Standard Gun (Yellow LV.18)
        // 2: Rifle (Holo LV.24)
        // 3: Rocket Punch (Blue LV.06)
        // 4: Spinning Blade (Purple LV.14)
        // 5: Multigun (Blue LV.06)
        // 6: Gun Turret (Green LV.01)
        // 7: Spiky Discus (Green LV.01)
        // 8: Shotgun (Blue LV.09)
        // 9: Energy Jumper Cables (Green LV.01 Star)
        // 10: High-Explosive Mine (Green LV.01)
        deckEquippedIds[0] = GetDefaultDeckIds(0);

        // Presets 2 & 3
        deckEquippedIds[1] = GetDefaultDeckIds(1);
        deckEquippedIds[2] = GetDefaultDeckIds(2);
    }

    private void SetupEventListeners()
    {
        AutoWireSortButtonsIfMissing();

        if (preset1Btn != null) preset1Btn.onClick.AddListener(() => SwitchDeck(0));
        if (preset2Btn != null) preset2Btn.onClick.AddListener(() => SwitchDeck(1));
        if (preset3Btn != null) preset3Btn.onClick.AddListener(() => SwitchDeck(2));

        if (byTierBtn != null)
        {
            byTierBtn.onClick.RemoveAllListeners();
            byTierBtn.onClick.AddListener(() => SetSortMode(false));
        }
        if (byQuantityBtn != null)
        {
            byQuantityBtn.onClick.RemoveAllListeners();
            byQuantityBtn.onClick.AddListener(() => SetSortMode(true));
        }

        if (blastFurnaceBtn != null) blastFurnaceBtn.onClick.AddListener(OpenFurnaceModal);
        if (furnaceCloseBtn != null) furnaceCloseBtn.onClick.AddListener(() => furnaceModal.SetActive(false));
        if (furnaceDismantleBtn != null) furnaceDismantleBtn.onClick.AddListener(ExecuteDismantle);

        if (chipsetModeBtn != null) chipsetModeBtn.onClick.AddListener(() => ShowToast("Chipset Configuration Active"));
        if (highTechModeBtn != null) highTechModeBtn.onClick.AddListener(() => ShowToast("High-Tech Chipset unlocks at Chapter 10!"));

        if (detailCloseBtn != null) detailCloseBtn.onClick.AddListener(() => detailModal.SetActive(false));
        if (detailEnhanceBtn != null) detailEnhanceBtn.onClick.AddListener(EnhanceSelectedChip);
        if (detailAdvanceTierBtn != null) detailAdvanceTierBtn.onClick.AddListener(AdvanceTierSelectedChip);
        if (detailEquipBtn != null) detailEquipBtn.onClick.AddListener(ToggleEquipSelectedChip);
    }

    public void AutoWireSortButtonsIfMissing()
    {
        if (byTierBtn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            byTierBtn = btns.FirstOrDefault(b => b.name.Equals("ByTierBtn", StringComparison.OrdinalIgnoreCase)
                                              || b.name.Equals("ByTier", StringComparison.OrdinalIgnoreCase)
                                              || b.name.Equals("By Tile", StringComparison.OrdinalIgnoreCase)
                                              || b.name.Equals("ByTileBtn", StringComparison.OrdinalIgnoreCase));
        }

        if (byQuantityBtn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            byQuantityBtn = btns.FirstOrDefault(b => b.name.Equals("ByQtyBtn", StringComparison.OrdinalIgnoreCase)
                                                  || b.name.Equals("ByQuantityBtn", StringComparison.OrdinalIgnoreCase)
                                                  || b.name.Equals("By Quantity", StringComparison.OrdinalIgnoreCase)
                                                  || b.name.Equals("ByQty", StringComparison.OrdinalIgnoreCase));
        }

        if (byTierBtn != null)
        {
            if (byTierBg == null) byTierBg = byTierBtn.GetComponent<Image>() ?? byTierBtn.targetGraphic as Image;
            if (byTierText == null) byTierText = byTierBtn.GetComponentInChildren<TMP_Text>(true);
        }

        if (byQuantityBtn != null)
        {
            if (byQuantityBg == null) byQuantityBg = byQuantityBtn.GetComponent<Image>() ?? byQuantityBtn.targetGraphic as Image;
            if (byQuantityText == null) byQuantityText = byQuantityBtn.GetComponentInChildren<TMP_Text>(true);
        }

        LoadSortSpritesIfMissing();
    }

    public void LoadSortSpritesIfMissing()
    {
        if (byTierYellowSprite != null && byTierGreenSprite != null && byQuantityYellowSprite != null && byQuantityGreenSprite != null)
            return;

#if UNITY_EDITOR
        string path = "Assets/Sprites/UI/Chipset/nút màn chipset.png";
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        foreach (var s in sprites)
        {
            if (s.name.Equals("By TileYellow", StringComparison.OrdinalIgnoreCase) || s.name.Equals("By TierYellow", StringComparison.OrdinalIgnoreCase) || s.name.Equals("By Tile Yellow", StringComparison.OrdinalIgnoreCase) || s.name.Equals("By Tier Yellow", StringComparison.OrdinalIgnoreCase))
                byTierYellowSprite = s;
            else if (s.name.Equals("By Tile Green", StringComparison.OrdinalIgnoreCase) || s.name.Equals("By Tier Green", StringComparison.OrdinalIgnoreCase) || s.name.Equals("ByTileGreen", StringComparison.OrdinalIgnoreCase) || s.name.Equals("ByTierGreen", StringComparison.OrdinalIgnoreCase))
                byTierGreenSprite = s;
            else if (s.name.Equals("By QuantityYellow", StringComparison.OrdinalIgnoreCase) || s.name.Equals("By Quantity Yellow", StringComparison.OrdinalIgnoreCase) || s.name.Equals("ByQtyYellow", StringComparison.OrdinalIgnoreCase))
                byQuantityYellowSprite = s;
            else if (s.name.Equals("ByQuantityGreen", StringComparison.OrdinalIgnoreCase) || s.name.Equals("By Quantity Green", StringComparison.OrdinalIgnoreCase) || s.name.Equals("ByQtyGreen", StringComparison.OrdinalIgnoreCase))
                byQuantityGreenSprite = s;
        }
#endif
    }

    public void SwitchDeck(int deckIndex)
    {
        activeDeckIndex = Mathf.Clamp(deckIndex, 0, 2);
        PlayerDataService.ActiveChipsetDeckIndex = activeDeckIndex;
        RefreshPresetButtons();
        RefreshEquippedGrid();
        ShowToast($"Switched to Preset Deck {deckIndex + 1}");
    }

    public void SetSortMode(bool quantitySort)
    {
        sortByQuantity = quantitySort;
        RefreshSortButtons();
        RefreshInventory();
    }

    private void RefreshTopBar()
    {
        if (energyText != null) energyText.text = $"{ChipManager.Energy}/{ChipManager.MaxEnergy}";
        if (chipCurrencyText != null) chipCurrencyText.text = $"{ChipManager.DataChips:N0}";
        if (redCurrencyText != null) redCurrencyText.text = $"{ChipManager.RedGems:N0}";
        if (advanceStonesText != null) advanceStonesText.text = $"{ChipManager.AdvanceStones:N0}";
    }

    public void AutoWirePresetButtonsIfMissing()
    {
        if (preset1Btn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            preset1Btn = btns.FirstOrDefault(b => b.name.Equals("Preset1Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Deck1Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("1", StringComparison.OrdinalIgnoreCase));
        }

        if (preset2Btn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            preset2Btn = btns.FirstOrDefault(b => b.name.Equals("Preset2Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Deck2Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("2", StringComparison.OrdinalIgnoreCase));
        }

        if (preset3Btn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            preset3Btn = btns.FirstOrDefault(b => b.name.Equals("Preset3Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Deck3Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("3", StringComparison.OrdinalIgnoreCase));
        }

        if (preset1Btn != null)
        {
            if (preset1Bg == null) preset1Bg = preset1Btn.GetComponent<Image>() ?? preset1Btn.targetGraphic as Image;
            if (preset1Text == null) preset1Text = preset1Btn.GetComponentInChildren<TMP_Text>(true);
        }

        if (preset2Btn != null)
        {
            if (preset2Bg == null) preset2Bg = preset2Btn.GetComponent<Image>() ?? preset2Btn.targetGraphic as Image;
            if (preset2Text == null) preset2Text = preset2Btn.GetComponentInChildren<TMP_Text>(true);
        }

        if (preset3Btn != null)
        {
            if (preset3Bg == null) preset3Bg = preset3Btn.GetComponent<Image>() ?? preset3Btn.targetGraphic as Image;
            if (preset3Text == null) preset3Text = preset3Btn.GetComponentInChildren<TMP_Text>(true);
        }

        LoadPresetSpritesIfMissing();
    }

    public void LoadPresetSpritesIfMissing()
    {
        if (preset1YellowSprite != null && preset1RedSprite != null &&
            preset2YellowSprite != null && preset2RedSprite != null &&
            preset3YellowSprite != null && preset3RedSprite != null)
            return;

#if UNITY_EDITOR
        string path = "Assets/Sprites/UI/Chipset/nút màn chipset.png";
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        foreach (var s in sprites)
        {
            if (s.name.Equals("1 Yellow", StringComparison.OrdinalIgnoreCase)) preset1YellowSprite = s;
            else if (s.name.Equals("1 Red", StringComparison.OrdinalIgnoreCase)) preset1RedSprite = s;
            else if (s.name.Equals("2 Yellow", StringComparison.OrdinalIgnoreCase)) preset2YellowSprite = s;
            else if (s.name.Equals("2 Red", StringComparison.OrdinalIgnoreCase)) preset2RedSprite = s;
            else if (s.name.Equals("3 Yellow", StringComparison.OrdinalIgnoreCase)) preset3YellowSprite = s;
            else if (s.name.Equals("3 Red", StringComparison.OrdinalIgnoreCase)) preset3RedSprite = s;
        }
#endif
    }

    private void RefreshPresetButtons()
    {
        AutoWirePresetButtonsIfMissing();

        // 1. Preset 1 (activeDeckIndex == 0 -> 1 Yellow, else -> 1 Red)
        if (preset1Bg != null)
        {
            Sprite s = activeDeckIndex == 0 ? preset1YellowSprite : preset1RedSprite;
            if (s != null)
            {
                preset1Bg.sprite = s;
                preset1Bg.color = Color.white;
            }
            else
            {
                preset1Bg.color = activeDeckIndex == 0 ? SelectedPresetColor : NormalPresetColor;
            }
        }

        // 2. Preset 2 (activeDeckIndex == 1 -> 2 Yellow, else -> 2 Red)
        if (preset2Bg != null)
        {
            Sprite s = activeDeckIndex == 1 ? preset2YellowSprite : preset2RedSprite;
            if (s != null)
            {
                preset2Bg.sprite = s;
                preset2Bg.color = Color.white;
            }
            else
            {
                preset2Bg.color = activeDeckIndex == 1 ? SelectedPresetColor : NormalPresetColor;
            }
        }

        // 3. Preset 3 (activeDeckIndex == 2 -> 3 Yellow, else -> 3 Red)
        if (preset3Bg != null)
        {
            Sprite s = activeDeckIndex == 2 ? preset3YellowSprite : preset3RedSprite;
            if (s != null)
            {
                preset3Bg.sprite = s;
                preset3Bg.color = Color.white;
            }
            else
            {
                preset3Bg.color = activeDeckIndex == 2 ? SelectedPresetColor : NormalPresetColor;
            }
        }

        // Clear overlapping text if sprite has numbers
        if (preset1Text != null)
        {
            if (preset1YellowSprite != null) preset1Text.text = string.Empty;
            else preset1Text.color = activeDeckIndex == 0 ? SelectedPresetTextColor : NormalPresetTextColor;
        }

        if (preset2Text != null)
        {
            if (preset2YellowSprite != null) preset2Text.text = string.Empty;
            else preset2Text.color = activeDeckIndex == 1 ? SelectedPresetTextColor : NormalPresetTextColor;
        }

        if (preset3Text != null)
        {
            if (preset3YellowSprite != null) preset3Text.text = string.Empty;
            else preset3Text.color = activeDeckIndex == 2 ? SelectedPresetTextColor : NormalPresetTextColor;
        }
    }

    private void RefreshSortButtons()
    {
        AutoWireSortButtonsIfMissing();

        bool isByQuantity = sortByQuantity; // false = By Tier (By Tile), true = By Quantity

        // 1. By Tier (By Tile): Khi chọn (!isByQuantity) -> Vàng (By TileYellow), khi không chọn -> Xanh (By Tile Green)
        if (byTierBg != null)
        {
            Sprite tierSprite = !isByQuantity ? byTierYellowSprite : byTierGreenSprite;
            if (tierSprite != null)
            {
                byTierBg.sprite = tierSprite;
                byTierBg.color = Color.white;
            }
            else
            {
                byTierBg.color = !isByQuantity ? SelectedPresetColor : NormalPresetColor;
            }
        }

        // 2. By Quantity: Khi chọn (isByQuantity) -> Vàng (By QuantityYellow), khi không chọn -> Xanh (ByQuantityGreen)
        if (byQuantityBg != null)
        {
            Sprite qtySprite = isByQuantity ? byQuantityYellowSprite : byQuantityGreenSprite;
            if (qtySprite != null)
            {
                byQuantityBg.sprite = qtySprite;
                byQuantityBg.color = Color.white;
            }
            else
            {
                byQuantityBg.color = isByQuantity ? SelectedPresetColor : NormalPresetColor;
            }
        }

        // Nếu sprite đã có sẵn chữ pixel art, xóa text đè lên để tránh bị nhân đôi chữ
        if (byTierText != null)
        {
            if (byTierYellowSprite != null) byTierText.text = string.Empty;
            else byTierText.color = !isByQuantity ? SelectedPresetTextColor : NormalPresetTextColor;
        }

        if (byQuantityText != null)
        {
            if (byQuantityYellowSprite != null) byQuantityText.text = string.Empty;
            else byQuantityText.color = isByQuantity ? SelectedPresetTextColor : NormalPresetTextColor;
        }
    }

    public void RefreshEquippedGrid()
    {
        int[] currentDeck = deckEquippedIds[activeDeckIndex];
        Sprite defaultFrame = GetFrameSprite(ChipTier.Magic);

        for (int i = 0; i < 10; i++)
        {
            if (i >= equippedSlots.Length || equippedSlots[i] == null) continue;

            int chipId = (currentDeck != null && i < currentDeck.Length) ? currentDeck[i] : -1;
            if (chipId == -1)
            {
                int slotIndex = i;
                equippedSlots[i].SetupEmpty(defaultFrame, () => ShowToast($"Slot {slotIndex + 1} is empty. Select a chip below to equip."));
                equippedSlots[i].gameObject.SetActive(true);
            }
            else
            {
                ChipItemData chip = allChips.FirstOrDefault(c => c.id == chipId);
                if (chip != null)
                {
                    Sprite icon = GetIconSprite(chip.iconKey);
                    Sprite frame = GetFrameSprite(chip.tier);
                    equippedSlots[i].Setup(chip, icon, frame, OpenDetailModal, QuickUpgradeChip);
                    equippedSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    int slotIndex = i;
                    equippedSlots[i].SetupEmpty(defaultFrame, () => ShowToast($"Slot {slotIndex + 1} is empty. Select a chip below to equip."));
                    equippedSlots[i].gameObject.SetActive(true);
                }
            }
        }
    }

    private void EnsureInventoryCardsInitialized()
    {
        if (spawnedInventoryCards.Count > 0) return;
        if (inventoryContent == null) return;

        foreach (Transform child in inventoryContent)
        {
            if (cardPrefab != null && child.gameObject == cardPrefab)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            ChipsetCardUI card = child.GetComponent<ChipsetCardUI>();
            if (card != null)
            {
                spawnedInventoryCards.Add(card);
            }
        }
    }

    public void RefreshInventory()
    {
        if (inventoryContent == null) return;

        EnsureInventoryCardsInitialized();

        // Sort items
        List<ChipItemData> sortedList = new List<ChipItemData>(allChips);
        if (sortByQuantity)
        {
            sortedList = sortedList.OrderByDescending(c => c.count).ThenByDescending(c => (int)c.tier).ToList();
        }
        else
        {
            sortedList = sortedList.OrderByDescending(c => (int)c.tier).ThenByDescending(c => c.level).ToList();
        }

        // Reuse or instantiate cards
        for (int i = 0; i < sortedList.Count; i++)
        {
            ChipsetCardUI card;
            if (i < spawnedInventoryCards.Count)
            {
                card = spawnedInventoryCards[i];
            }
            else
            {
                if (cardPrefab == null) break;
                GameObject obj = Instantiate(cardPrefab, inventoryContent);
                card = obj.GetComponent<ChipsetCardUI>();
                spawnedInventoryCards.Add(card);
            }

            ChipItemData data = sortedList[i];
            Sprite icon = GetIconSprite(data.iconKey);
            Sprite frame = GetFrameSprite(data.tier);
            card.Setup(data, icon, frame, OpenDetailModal, QuickUpgradeChip);
            card.gameObject.SetActive(true);
        }

        for (int i = sortedList.Count; i < spawnedInventoryCards.Count; i++)
        {
            spawnedInventoryCards[i].gameObject.SetActive(false);
        }
    }

    public void QuickUpgradeChip(ChipItemData chip)
    {
        OpenDetailModal(chip);
    }

    public void HandleCardAction(ChipItemData chip)
    {
        if (chip == null) return;

        if (chip.CanAdvanceTier)
        {
            AdvanceTierSelectedChip();
            return;
        }

        if (chip.CanEnhance)
        {
            EnhanceSelectedChip();
            return;
        }

        OpenDetailModal(chip);
    }

    public void AutoWireDetailModalIfMissing()
    {
        if (detailModal == null)
        {
            var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            detailModal = allGo.FirstOrDefault(g => (g.name == "ChipsetDetailModal" || g.name == "DetailModal") && g.scene.isLoaded);
            if (detailModal == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    Transform t = canvas.transform.Find("ChipsetDetailModal")
                               ?? canvas.transform.Find("Content/ChipsetDetailModal")
                               ?? canvas.transform.Find("Content/ChipsetPanel/ChipsetDetailModal")
                               ?? canvas.transform.Find("DetailModal");
                    if (t != null) detailModal = t.gameObject;
                }
            }
        }

        if (detailModal != null)
        {
            if (detailCloseBtn == null)
            {
                var btns = detailModal.GetComponentsInChildren<Button>(true);
                detailCloseBtn = btns.FirstOrDefault(b => b.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (detailCloseBtn != null)
            {
                detailCloseBtn.onClick.RemoveAllListeners();
                detailCloseBtn.onClick.AddListener(() => detailModal.SetActive(false));
            }

            if (detailEnhanceBtn == null)
            {
                var btns = detailModal.GetComponentsInChildren<Button>(true);
                detailEnhanceBtn = btns.FirstOrDefault(b => b.name.IndexOf("Enhance", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (detailEnhanceBtn != null)
            {
                detailEnhanceBtn.onClick.RemoveAllListeners();
                detailEnhanceBtn.onClick.AddListener(EnhanceSelectedChip);
            }

            if (detailAdvanceTierBtn == null)
            {
                var btns = detailModal.GetComponentsInChildren<Button>(true);
                detailAdvanceTierBtn = btns.FirstOrDefault(b => b.name.IndexOf("Advance", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (detailAdvanceTierBtn != null)
            {
                detailAdvanceTierBtn.onClick.RemoveAllListeners();
                detailAdvanceTierBtn.onClick.AddListener(AdvanceTierSelectedChip);
            }

            if (detailEquipBtn == null)
            {
                var btns = detailModal.GetComponentsInChildren<Button>(true);
                detailEquipBtn = btns.FirstOrDefault(b => b.name.IndexOf("Equip", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (detailEquipBtn != null)
            {
                detailEquipBtn.onClick.RemoveAllListeners();
                detailEquipBtn.onClick.AddListener(ToggleEquipSelectedChip);
            }

            if (detailTopCard == null) detailTopCard = detailModal.GetComponentInChildren<ChipsetCardUI>(true);
            if (detailNameText == null) detailNameText = detailModal.transform.Find("ModalBox/Name")?.GetComponent<TMP_Text>() ?? detailModal.transform.Find("Box/Name")?.GetComponent<TMP_Text>();
            if (detailTierText == null) detailTierText = detailModal.transform.Find("ModalBox/Tier")?.GetComponent<TMP_Text>() ?? detailModal.transform.Find("Box/Tier")?.GetComponent<TMP_Text>();
            if (detailDescText == null) detailDescText = detailModal.transform.Find("ModalBox/Description")?.GetComponent<TMP_Text>() ?? detailModal.transform.Find("Box/Desc")?.GetComponent<TMP_Text>();
            if (detailBaseStatsText == null) detailBaseStatsText = detailModal.transform.Find("ModalBox/BaseStat")?.GetComponent<TMP_Text>() ?? detailModal.transform.Find("Box/BaseStats")?.GetComponent<TMP_Text>();
            if (detailModBadgeText == null) detailModBadgeText = detailModal.transform.Find("ModalBox/ModBadge/BadgeLabel")?.GetComponent<TMP_Text>();
        }
    }

    public void OpenDetailModal(ChipItemData chip)
    {
        if (chip == null) return;
        AutoWireDetailModalIfMissing();

        if (detailModal == null)
        {
            Debug.LogWarning("[ChipsetController] detailModal is not assigned or found in scene!");
            return;
        }

        selectedDetailChip = chip;
        RefreshDetailModal();
        detailModal.SetActive(true);
        detailModal.transform.SetAsLastSibling();
    }

    public void RefreshDetailModal()
    {
        if (selectedDetailChip == null) return;

        // 1. Mod Badge
        if (detailModBadgeText != null)
        {
            detailModBadgeText.text = $"🔧 Mod able (up to LV{selectedDetailChip.MaxLevel:00}) 🔧";
        }

        // 2. Top Card
        if (detailTopCard != null)
        {
            Sprite icon = GetIconSprite(selectedDetailChip.iconKey);
            Sprite frame = GetFrameSprite(selectedDetailChip.tier);
            detailTopCard.Setup(selectedDetailChip, icon, frame);
            detailTopCard.UseDetailBottomBarLayout();
        }

        // 3. Name & Tier Subtitle
        if (detailNameText != null) detailNameText.text = selectedDetailChip.chipName;
        if (detailTierText != null)
        {
            detailTierText.text = GetFrameColorName(selectedDetailChip.tier);
        }

        // 4. Description & Base Stats
        if (detailDescText != null) detailDescText.text = selectedDetailChip.description;
        if (detailBaseStatsText != null) detailBaseStatsText.text = selectedDetailChip.baseStatsSummary;

        // 5. 4 Tier Perk Rows with color tags
        string[] tierNames = { "Magic", "Rare", "Unique", "Epic" };
        string[] tierColors = { "#38BDF8", "#C084FC", "#FACC15", "#FB7185" };
        string[] perkTexts = {
            selectedDetailChip.magicBonus,
            selectedDetailChip.rareBonus,
            selectedDetailChip.uniqueBonus,
            selectedDetailChip.epicBonus
        };

        for (int i = 0; i < 4; i++)
        {
            bool isUnlocked = IsTierPerkUnlocked(selectedDetailChip.tier, i);
            if (i < perkRowIcons.Length && perkRowIcons[i] != null)
            {
                if (isUnlocked && unlockedCheckSprite != null)
                {
                    perkRowIcons[i].sprite = unlockedCheckSprite;
                }
                else if (i < lockTierSprites.Length && lockTierSprites[i] != null)
                {
                    perkRowIcons[i].sprite = lockTierSprites[i];
                }
            }

            if (i < perkRowTexts.Length && perkRowTexts[i] != null)
            {
                if (isUnlocked)
                {
                    perkRowTexts[i].text = $"<color=#40DAD2>{perkTexts[i]}</color> <color=#22C55E>[ACTIVE]</color>";
                }
                else
                {
                    perkRowTexts[i].text = $"{perkTexts[i]}(<color={tierColors[i]}>{tierNames[i]}</color>Unlock)";
                }
            }
        }

        // 6. Enhance Button
        if (detailEnhanceCostText != null)
        {
            detailEnhanceCostText.text = selectedDetailChip.tier < ChipTier.Holographic
                ? $"{selectedDetailChip.enhanceCost}  ({selectedDetailChip.tierEnhanceCount}/{selectedDetailChip.RequiredTierEnhances})"
                : $"{selectedDetailChip.enhanceCost}";
        }
        if (detailEnhanceBtn != null)
        {
            detailEnhanceBtn.interactable = selectedDetailChip.CanEnhance;
        }

        // 7. Advance Tier Button
        if (detailAdvanceTierText != null)
        {
            if (selectedDetailChip.tier >= ChipTier.Holographic)
            {
                detailAdvanceTierText.text = "MAX TIER";
            }
            else if (!selectedDetailChip.IsTierUnlockReady)
            {
                detailAdvanceTierText.text = $"Enhance {selectedDetailChip.tierEnhanceCount}/{selectedDetailChip.RequiredTierEnhances}";
            }
            else if (selectedDetailChip.UsesRedDataChipForAdvance)
            {
                detailAdvanceTierText.text = $"Advance RED ({ChipManager.RedGems}/{selectedDetailChip.CurrentAdvanceCost})";
            }
            else
            {
                ChipTier nextTier = (ChipTier)((int)selectedDetailChip.tier + 1);
                detailAdvanceTierText.text = $"Advance {GetFrameColorName(nextTier)} ({selectedDetailChip.count}/{selectedDetailChip.CurrentAdvanceCost})";
            }
        }
        if (detailAdvanceTierBtn != null)
        {
            detailAdvanceTierBtn.interactable = selectedDetailChip.CanAdvanceTier;
        }

        // 8. Equip / Unequip Button
        bool isEquipped = deckEquippedIds[activeDeckIndex].Contains(selectedDetailChip.id);
        if (detailEquipBtnText != null)
        {
            detailEquipBtnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
        }
    }

    private void EnhanceSelectedChip()
    {
        if (selectedDetailChip == null) return;
        if (!selectedDetailChip.CanEnhance)
        {
            ShowToast(selectedDetailChip.IsTierUnlockReady && selectedDetailChip.tier < ChipTier.Holographic
                ? "Enhance requirement complete. Advance the chipset frame now!"
                : "Not enough Data Chips to enhance!");
            return;
        }

        if (selectedDetailChip.Enhance())
        {
            SaveChipProgress(selectedDetailChip);
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            ShowToast($"Enhanced {selectedDetailChip.chipName} to LV.{selectedDetailChip.level:00}!");
        }
    }

    private void AdvanceTierSelectedChip()
    {
        if (selectedDetailChip == null) return;
        if (selectedDetailChip.tier >= ChipTier.Holographic)
        {
            ShowToast("This chipset has already unlocked the red frame!");
            return;
        }
        if (!selectedDetailChip.IsTierUnlockReady)
        {
            ShowToast($"Enhance {selectedDetailChip.RequiredTierEnhances} times before advancing this frame!");
            return;
        }
        if (!selectedDetailChip.HasAdvanceCurrency)
        {
            ShowToast(selectedDetailChip.UsesRedDataChipForAdvance
                ? "Not enough Red Data Chips to unlock the red frame!"
                : "Not enough chipset fragments to unlock the next frame!");
            return;
        }
        if (!selectedDetailChip.CanAdvanceTier)
        {
            ShowToast("This chipset frame cannot advance further!");
            return;
        }

        if (selectedDetailChip.AdvanceTier())
        {
            SaveChipProgress(selectedDetailChip);
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            ShowToast($"Unlocked {GetFrameColorName(selectedDetailChip.tier)} frame for {selectedDetailChip.chipName}!");
        }
    }

    private void ToggleEquipSelectedChip()
    {
        if (selectedDetailChip == null) return;

        int[] currentDeck = deckEquippedIds[activeDeckIndex];
        int indexInDeck = Array.IndexOf(currentDeck, selectedDetailChip.id);

        if (indexInDeck >= 0)
        {
            // Unequip
            currentDeck[indexInDeck] = -1;
            ShowToast($"Unequipped {selectedDetailChip.chipName}");
        }
        else
        {
            // Find empty slot or replace slot 0
            int emptyIndex = Array.IndexOf(currentDeck, -1);
            if (emptyIndex >= 0)
            {
                currentDeck[emptyIndex] = selectedDetailChip.id;
                ShowToast($"Equipped {selectedDetailChip.chipName} to Slot {emptyIndex + 1}");
            }
            else
            {
                currentDeck[0] = selectedDetailChip.id;
                ShowToast($"Replaced Slot 1 with {selectedDetailChip.chipName}");
            }
        }

        PlayerDataService.SaveChipsetDeck(activeDeckIndex, currentDeck);
        RefreshEquippedGrid();
        RefreshDetailModal();
        RefreshInventory();
    }

    public void OpenFurnaceModal()
    {
        if (furnaceModal == null) return;
        int totalSparePieces = allChips.Where(c => c.count > 10).Sum(c => c.count - 10);
        long gainedCurrency = totalSparePieces * 250L;
        if (furnaceDescText != null)
        {
            furnaceDescText.text = $"Recycle spare chip fragments into Chipset Currency!\n\nAvailable spare fragments: <color=#50E1DC>{totalSparePieces}</color>\nEstimated return: <color=#FFD232>+{gainedCurrency:N0} Chips</color>";
        }
        furnaceModal.SetActive(true);
    }

    private void ExecuteDismantle()
    {
        int dismantledPieces = 0;
        foreach (var chip in allChips)
        {
            if (chip.count > 10)
            {
                int remove = chip.count - 10;
                chip.count = 10;
                SaveChipProgress(chip);
                dismantledPieces += remove;
            }
        }

        long gainedCurrency = dismantledPieces * 250L;
        ChipManager.AddDataChips((int)Math.Min(gainedCurrency, int.MaxValue));
        RefreshTopBar();
        RefreshEquippedGrid();
        RefreshInventory();
        if (furnaceModal != null) furnaceModal.SetActive(false);
        ShowToast($"Dismantled {dismantledPieces} fragments! Gained +{gainedCurrency:N0} Chips!");
    }

    public void LoadVisualLibraryIfMissing()
    {
        if (tierVisualLibrary == null)
        {
            tierVisualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
#if UNITY_EDITOR
            if (tierVisualLibrary == null)
            {
                tierVisualLibrary = UnityEditor.AssetDatabase.LoadAssetAtPath<ChipsetLevelVisualLibrary>("Assets/Resources/ChipsetLevelVisualLibrary.asset");
            }
#endif
        }

        if (tierVisualLibrary != null)
        {
            if (tierVisualLibrary.primaryChipIcons != null && tierVisualLibrary.primaryChipIcons.Length > 0 &&
                (chipIcons == null || chipIcons.Length == 0 || chipIcons.Any(s => s == null)))
            {
                chipIcons = tierVisualLibrary.primaryChipIcons;
            }

            if (tierVisualLibrary.mainMenuTierFrames != null && tierVisualLibrary.mainMenuTierFrames.Length > 0 &&
                (frameSprites == null || frameSprites.Length == 0 || frameSprites.Any(s => s == null)))
            {
                frameSprites = tierVisualLibrary.mainMenuTierFrames;
            }
        }
    }

    public Sprite GetIconSprite(string key)
    {
        LoadVisualLibraryIfMissing();

        Sprite[] icons = (tierVisualLibrary != null && tierVisualLibrary.primaryChipIcons != null && tierVisualLibrary.primaryChipIcons.Length > 0)
            ? tierVisualLibrary.primaryChipIcons
            : chipIcons;

        if (icons == null || icons.Length == 0)
        {
            LoadChipIconsIfMissing();
            icons = chipIcons;
        }

        if (icons == null || icons.Length == 0) return null;
        if (string.IsNullOrEmpty(key)) return icons[0];

        // 1. Nếu key là số ID (1..10)
        if (int.TryParse(key, out int id) && id >= 1 && id <= icons.Length)
        {
            if (icons[id - 1] != null) return icons[id - 1];
        }

        string cleanKey = key.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();

        // 2. Tra cứu trực tiếp theo vị trí index 10 chipset chuẩn trong Resources
        if (cleanKey.Contains("standard") || cleanKey.Contains("tieuchuan") || cleanKey == "1") return icons.Length > 0 ? icons[0] : null;
        if (cleanKey.Contains("rifle") || cleanKey.Contains("truong") || cleanKey == "2") return icons.Length > 1 ? icons[1] : null;
        if (cleanKey.Contains("punch") || cleanKey.Contains("rocket") || cleanKey.Contains("tenlua") || cleanKey == "3") return icons.Length > 2 ? icons[2] : null;
        if (cleanKey.Contains("blade") || cleanKey.Contains("spinning") || cleanKey.Contains("dao") || cleanKey == "4") return icons.Length > 3 ? icons[3] : null;
        if (cleanKey.Contains("multi") || cleanKey.Contains("datia") || cleanKey == "5") return icons.Length > 4 ? icons[4] : null;
        if (cleanKey.Contains("turret") || cleanKey.Contains("thap") || cleanKey == "6") return icons.Length > 5 ? icons[5] : null;
        if (cleanKey.Contains("discus") || cleanKey.Contains("spiky") || cleanKey.Contains("gai") || cleanKey == "7") return icons.Length > 6 ? icons[6] : null;
        if (cleanKey.Contains("shotgun") || cleanKey.Contains("san") || cleanKey == "8") return icons.Length > 7 ? icons[7] : null;
        if (cleanKey.Contains("cable") || cleanKey.Contains("jumper") || cleanKey.Contains("hoimau") || cleanKey == "9") return icons.Length > 8 ? icons[8] : null;
        if (cleanKey.Contains("mine") || cleanKey.Contains("min") || cleanKey.Contains("explosive") || cleanKey == "10") return icons.Length > 9 ? icons[9] : null;

        // 3. Khớp theo tên sprite
        Sprite match = icons.FirstOrDefault(s => s != null && (
            string.Equals(s.name, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant(), cleanKey) ||
            s.name.Replace(" ", "").Replace("-", "").Replace("_", "").IndexOf(cleanKey, StringComparison.OrdinalIgnoreCase) >= 0
        ));
        if (match != null) return match;

        return icons[0];
    }

    private static Sprite FindIconIn(Sprite[] source, params string[] keywords)
    {
        if (source == null) return null;
        foreach (var s in source)
        {
            if (s == null) continue;
            string sName = s.name.ToLowerInvariant();
            foreach (var kw in keywords)
            {
                if (sName.Contains(kw.ToLowerInvariant()))
                    return s;
            }
        }
        return source.FirstOrDefault(s => s != null);
    }

    public static int GetFrameIndex(ChipTier tier)
    {
        return Mathf.Clamp((int)tier - 1, 0, 4);
    }

    public static bool IsTierPerkUnlocked(ChipTier tier, int perkRowIndex)
    {
        return perkRowIndex >= 0 && perkRowIndex < 4 && GetFrameIndex(tier) > perkRowIndex;
    }

    private static string GetFrameColorName(ChipTier tier)
    {
        switch (tier)
        {
            case ChipTier.Magic: return "GREEN";
            case ChipTier.Rare: return "BLUE";
            case ChipTier.Unique: return "PURPLE";
            case ChipTier.Epic: return "YELLOW";
            case ChipTier.Holographic: return "RED";
            default: return "GREEN";
        }
    }

    public void LoadChipIconsIfMissing()
    {
        LoadVisualLibraryIfMissing();
        if (chipIcons != null && chipIcons.Length > 0 && chipIcons.All(s => s != null))
            return;

#if UNITY_EDITOR
        string path = "Assets/Sprites/UI/Chipset/icon chipset.png";
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if (sprites != null && sprites.Length > 0)
        {
            chipIcons = sprites;
        }
#endif
    }

    public void LoadFrameSpritesIfMissing()
    {
        LoadVisualLibraryIfMissing();
        if (frameSprites != null && frameSprites.Length >= 5 && frameSprites.All(s => s != null))
            return;

#if UNITY_EDITOR
        string path = "Assets/Sprites/UI/Chipset/khung chipset (1).png";
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if (sprites != null && sprites.Length > 0)
        {
            Sprite green = sprites.FirstOrDefault(s => s.name.Equals("ChipsetGreen", StringComparison.OrdinalIgnoreCase));
            Sprite blue = sprites.FirstOrDefault(s => s.name.Equals("ChipsetBlue", StringComparison.OrdinalIgnoreCase));
            Sprite purple = sprites.FirstOrDefault(s => s.name.Equals("ChipsetPurple", StringComparison.OrdinalIgnoreCase));
            Sprite yellow = sprites.FirstOrDefault(s => s.name.Equals("ChipsetYelloe", StringComparison.OrdinalIgnoreCase) || s.name.Equals("ChipsetYellow", StringComparison.OrdinalIgnoreCase));
            Sprite red = sprites.FirstOrDefault(s => s.name.Equals("ChipsetRed", StringComparison.OrdinalIgnoreCase));

            frameSprites = new Sprite[5]
            {
                green ?? sprites[0],
                blue ?? green ?? sprites[0],
                purple ?? green ?? sprites[0],
                yellow ?? green ?? sprites[0],
                red ?? green ?? sprites[0]
            };
        }
#endif
    }

    public Sprite GetFrameSprite(ChipTier tier)
    {
        LoadVisualLibraryIfMissing();
        int index = GetFrameIndex(tier);

        if (tierVisualLibrary != null &&
            tierVisualLibrary.mainMenuTierFrames != null &&
            tierVisualLibrary.mainMenuTierFrames.Length > index &&
            tierVisualLibrary.mainMenuTierFrames[index] != null)
        {
            return tierVisualLibrary.mainMenuTierFrames[index];
        }

        if (frameSprites != null && frameSprites.Length > index && frameSprites[index] != null)
        {
            return frameSprites[index];
        }

        return frameSprites != null && frameSprites.Length > 0 ? frameSprites[0] : null;
    }

    private void ShowToast(string message)
    {
        if (toastRoot == null || toastText == null) return;
        toastText.text = message;
        toastRoot.SetActive(false);
        toastRoot.SetActive(true);
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), 2.5f);
    }

    private void HideToast()
    {
        if (toastRoot != null) toastRoot.SetActive(false);
    }
}
