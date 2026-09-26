using System;
using System.Collections;
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
    public int requiredCount = 5;
    public int enhanceCost = 500;
    public int tierEnhanceCount;
    public bool hasStar;

    [NonSerialized] private bool tierUnlockRulesEnabled;
    [NonSerialized] private int requiredTierEnhances = 10;
    [NonSerialized] private int greenToBlueFragmentCost = 5;
    [NonSerialized] private int blueToPurpleFragmentCost = 10;
    [NonSerialized] private int purpleToYellowFragmentCost = 15;
    [NonSerialized] private int yellowToRedFragmentCost = 20;
    [NonSerialized] private int yellowToRedDataChipCost = 100;

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

    public int RequiredTierEnhances => Mathf.Min(requiredTierEnhances,
        MaxLevel - (tier == ChipTier.Magic ? 1 : GetMaxLevelForTier((ChipTier)((int)tier - 1))));
    public bool IsTierUnlockReady => tierUnlockRulesEnabled
        ? tierEnhanceCount >= RequiredTierEnhances || IsAtTierCap
        : IsAtTierCap;
    public bool UsesRedDataChipForAdvance => tierUnlockRulesEnabled && tier == ChipTier.Epic;
    public int YellowToRedDataChipCost => yellowToRedDataChipCost;
    public int YellowToRedFragmentCost => yellowToRedFragmentCost;

    public int CurrentAdvanceCost
    {
        get
        {
            switch (tier)
            {
                case ChipTier.Magic: return greenToBlueFragmentCost;
                case ChipTier.Rare: return blueToPurpleFragmentCost;
                case ChipTier.Unique: return purpleToYellowFragmentCost;
                case ChipTier.Epic: return yellowToRedFragmentCost;
                default: return 0;
            }
        }
    }

    public bool HasAdvanceCurrency
    {
        get
        {
            if (tier >= ChipTier.Holographic) return false;
            if (tier == ChipTier.Epic && tierUnlockRulesEnabled)
            {
                bool hasFragments = count >= yellowToRedFragmentCost;
                bool hasRedGems = yellowToRedDataChipCost <= 0 || ChipManager.HasEnoughRedGems(yellowToRedDataChipCost);
                return hasFragments && hasRedGems;
            }
            return count >= CurrentAdvanceCost;
        }
    }

    public bool IsMaxEnhanceForCurrentFrame
    {
        get
        {
            if (tier >= ChipTier.Holographic)
            {
                return IsMaxOverall || level >= MaxLevel;
            }
            if (tierUnlockRulesEnabled)
            {
                return tierEnhanceCount >= RequiredTierEnhances || level >= MaxLevel;
            }
            return IsAtTierCap;
        }
    }

    public bool CanEnhance => !IsMaxEnhanceForCurrentFrame && ChipManager.HasEnoughDataChips(enhanceCost);
    public bool CanUpgrade => !tierUnlockRulesEnabled && !IsAtTierCap && count >= requiredCount && requiredCount > 0;
    public bool CanAdvanceTier => tierUnlockRulesEnabled
        ? tier < ChipTier.Holographic && IsTierUnlockReady && HasAdvanceCurrency
        : IsAtTierCap && tier < ChipTier.Holographic;

    public void ConfigureTierUnlockRules(
        int enhancesRequired,
        int greenToBlueCost,
        int blueToPurpleCost,
        int purpleToYellowCost,
        int yellowToRedCost,
        int yellowToRedFragments = 0)
    {
        tierUnlockRulesEnabled = true;
        requiredTierEnhances = Mathf.Max(1, enhancesRequired);
        greenToBlueFragmentCost = Mathf.Max(0, greenToBlueCost);
        blueToPurpleFragmentCost = Mathf.Max(0, blueToPurpleCost);
        purpleToYellowFragmentCost = Mathf.Max(0, purpleToYellowCost);
        yellowToRedDataChipCost = Mathf.Max(0, yellowToRedCost);
        yellowToRedFragmentCost = Mathf.Max(0, yellowToRedFragments);
        tierEnhanceCount = Mathf.Clamp(tierEnhanceCount, 0, requiredTierEnhances);

        if (tier < ChipTier.Holographic)
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
            if (tier == ChipTier.Epic)
            {
                if (yellowToRedDataChipCost > 0 && !ChipManager.TrySpendRedGems(yellowToRedDataChipCost))
                {
                    return false;
                }
                if (yellowToRedFragmentCost > 0)
                {
                    count -= yellowToRedFragmentCost;
                }
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
            if (tier < ChipTier.Holographic)
            {
                requiredCount = CurrentAdvanceCost;
            }
            else
            {
                requiredCount = 0;
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
            yellowToRedFragmentCost = this.yellowToRedFragmentCost,
            yellowToRedDataChipCost = this.yellowToRedDataChipCost
        };
    }
}

public class ChipsetController : MonoBehaviour
{
    [Header("Top Bar Currencies")]
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

    [Header("Equip Selection Mode")]
    [SerializeField] private Sprite equipBadgeSprite;
    private bool isEquipSelectMode;
    private ChipItemData pendingEquipChip;
    private int openedFromEquippedSlotIndex = -1;

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
    [SerializeField] private CanvasGroup enhanceBtnCanvasGroup;
    [SerializeField] private Button detailAdvanceTierBtn;
    [SerializeField] private TMP_Text detailAdvanceTierText;
    [SerializeField] private CanvasGroup advanceTierBtnCanvasGroup;
    [SerializeField] private Button detailEquipBtn;
    [SerializeField] private TMP_Text detailEquipBtnText;
    [SerializeField] private Button detailCloseBtn;

    [Header("Notice Panels (Missing Currency / Fragments)")]
    [SerializeField] private GameObject notEnoughFragmentsNotice;
    [SerializeField] private GameObject notEnoughChipsNotice;

    public GameObject NotEnoughFragmentsNotice => notEnoughFragmentsNotice;
    public GameObject NotEnoughChipsNotice => notEnoughChipsNotice;
    public CanvasGroup EnhanceBtnCanvasGroup => enhanceBtnCanvasGroup;
    public CanvasGroup AdvanceTierBtnCanvasGroup => advanceTierBtnCanvasGroup;

    [Header("Chipset Tier Unlock Costs")]
    [SerializeField, Min(1)] private int enhancesRequiredPerTier = 10;
    [SerializeField, Min(0)] private int greenToBlueFragmentCost = 5;
    [SerializeField, Min(0)] private int blueToPurpleFragmentCost = 10;
    [SerializeField, Min(0)] private int purpleToYellowFragmentCost = 15;
    [SerializeField, Min(0)] private int yellowToRedFragmentCost = 20;
    [SerializeField, Min(0)] private int yellowToRedDataChipCost = 100;

    public int GreenToBlueFragmentCost => greenToBlueFragmentCost;
    public int BlueToPurpleFragmentCost => blueToPurpleFragmentCost;
    public int PurpleToYellowFragmentCost => purpleToYellowFragmentCost;
    public int YellowToRedFragmentCost => yellowToRedFragmentCost;
    public int YellowToRedDataChipCost => yellowToRedDataChipCost;

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
    [SerializeField] private Sprite[] lockTierSprites = new Sprite[4]; // 0: Rare (Blue), 1: Unique (Purple), 2: Epic (Yellow), 3: Holo (Red)
    [SerializeField] private Sprite[] unlockedTierSprites = new Sprite[4]; // 0: Rare (Blue Open), 1: Unique (Purple Open), 2: Epic (Yellow Open), 3: Holo (Red Open)
    [SerializeField] private Sprite unlockedCheckSprite;
    [SerializeField] private ChipsetLevelVisualLibrary tierVisualLibrary;

    [Header("Audio SFX")]
    [SerializeField] private AudioClip equipSFX;
    [SerializeField] private AudioClip presetSwitchSFX;

    private int activeDeckIndex = 0; // Reference layout opens on Preset 1.
    private bool sortByQuantity = false; // Reference layout defaults to sorting by tier.
    private ChipItemData selectedDetailChip;

    // Database of all 24 chips with full user defined stats
    [SerializeField] private List<ChipItemData> allChips = new List<ChipItemData>();
    // 3 Decks holding IDs of equipped chips (10 slots each)
    private int[][] deckEquippedIds = new int[3][];
    private List<ChipsetCardUI> spawnedInventoryCards = new List<ChipsetCardUI>();
    private bool isStarted;
    private int lastEquipFrame = -1;
    private float lastEquipTime = -1f;

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
        EnsureLockTierSprites();
        LoadEquipSpritesIfMissing();
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
        isStarted = true;
    }

    private void Update()
    {
        ChipsetFrameShimmerMaterial.UpdateUnscaledAnimationClock();
    }

    public void EnsureLockTierSprites()
    {
        if (lockTierSprites == null || lockTierSprites.Length < 4)
        {
            Array.Resize(ref lockTierSprites, 4);
        }

        string[] lockNames = { "Lock_Blue", "Lock_Purple", "Lock_Yellow", "Lock_Red" };
        for (int i = 0; i < 4; i++)
        {
            if (lockTierSprites[i] == null)
            {
                lockTierSprites[i] = Resources.Load<Sprite>($"UI/Chipset/Locks/{lockNames[i]}")
                                  ?? Resources.Load<Sprite>($"Sprites/UI/Chipset/{lockNames[i]}")
                                  ?? Resources.Load<Sprite>($"UI/Chipset/{lockNames[i]}");
#if UNITY_EDITOR
                if (lockTierSprites[i] == null)
                {
                    lockTierSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Chipset/{lockNames[i]}.png");
                }
#endif
            }
        }

        if (unlockedTierSprites == null || unlockedTierSprites.Length < 4)
        {
            Array.Resize(ref unlockedTierSprites, 4);
        }

        string[] openLockNames = { "Lock_Blue_Open", "Lock_Purple_Open", "Lock_Yellow_Open", "Lock_Red_Open" };
        for (int i = 0; i < 4; i++)
        {
            if (unlockedTierSprites[i] == null)
            {
                unlockedTierSprites[i] = Resources.Load<Sprite>($"UI/Chipset/Locks/{openLockNames[i]}")
                                      ?? Resources.Load<Sprite>($"Sprites/UI/Chipset/{openLockNames[i]}")
                                      ?? Resources.Load<Sprite>($"UI/Chipset/{openLockNames[i]}");
#if UNITY_EDITOR
                if (unlockedTierSprites[i] == null)
                {
                    unlockedTierSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Chipset/{openLockNames[i]}.png");
                }
#endif
            }
        }
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged += HandleCurrencyChanged;
        ChipManager.OnAdvanceStonesChanged += HandleCurrencyChanged;
        PlayerDataService.OnChipsetPiecesChanged += HandleChipsetPiecesChanged;

        // Rewards may arrive while this tab is disabled and unsubscribed.
        ForceSyncAndRefresh();
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged -= HandleCurrencyChanged;
        ChipManager.OnAdvanceStonesChanged -= HandleCurrencyChanged;
        PlayerDataService.OnChipsetPiecesChanged -= HandleChipsetPiecesChanged;
    }

    private void OnDestroy()
    {
        ChipManager.OnDataChipsChanged -= HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged -= HandleCurrencyChanged;
        ChipManager.OnAdvanceStonesChanged -= HandleCurrencyChanged;
        PlayerDataService.OnChipsetPiecesChanged -= HandleChipsetPiecesChanged;
    }

    /// <summary>
    /// Đồng bộ lại dữ liệu chipset từ PlayerPrefs và vẽ lại UI toàn diện (được gọi từ OnEnable hoặc BottomNavigation).
    /// </summary>
    public void ForceSyncAndRefresh()
    {
        if (allChips == null || allChips.Count == 0)
        {
            InitializeDatabase();
        }
        else
        {
            SyncChipsetProgressFromSave();
        }

        if (isStarted)
        {
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            if (detailModal != null && detailModal.activeSelf && selectedDetailChip != null)
            {
                RefreshDetailModal();
            }
        }
    }

    private void HandleCurrencyChanged(int _)
    {
        RefreshTopBar();
        if (detailModal != null && detailModal.activeSelf && selectedDetailChip != null)
        {
            RefreshDetailModal();
        }
    }

    private void HandleChipsetPiecesChanged(int chipsetId, int newCount)
    {
        ChipItemData chipset = allChips.FirstOrDefault(item => item != null && item.id == chipsetId);
        if (chipset == null) return;

        chipset.count = newCount;
        RefreshEquippedGrid();
        RefreshInventory();
        if (detailModal != null && detailModal.activeSelf && selectedDetailChip != null && selectedDetailChip.id == chipsetId)
        {
            selectedDetailChip.count = newCount;
            RefreshDetailModal();
        }
    }

    public void InitializeDatabase()
    {
        if (allChips.Count == 0)
        {
            allChips = CreateSavedDatabase();
        }

        SyncChipsetProgressFromSave();
        ApplyTierUnlockRulesToCatalog();
        InitializeDefaultDecks();
        activeDeckIndex = PlayerDataService.ActiveChipsetDeckIndex;
        for (int i = 0; i < deckEquippedIds.Length; i++)
        {
            deckEquippedIds[i] = PlayerDataService.LoadChipsetDeck(i, deckEquippedIds[i]);
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
                yellowToRedDataChipCost,
                yellowToRedFragmentCost);
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
                count = 5,
                requiredCount = 5,
                enhanceCost = 500,
                description = "Always equipped, even when it is not included in your deck.",
                baseStatsSummary = "ATK <color=#FFCB49>53</color>\n<color=#FFCB49>Fast</color> ATK Speed",
                magicBonus = "ATK +15%",
                rareBonus = "ATK Speed +15%",
                uniqueBonus = "+5% Life Steal",
                epicBonus = "50% chance to ricochet again"
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
                requiredCount = 5,
                enhanceCost = 500,
                description = "Fires a rapid burst of bullets.",
                baseStatsSummary = "ATK <color=#FFCB49>10.5</color>\n<color=#FFCB49>Fast</color> ATK Speed",
                magicBonus = "ATK +25%",
                rareBonus = "ATK Speed +20%",
                uniqueBonus = "ATK +80%",
                epicBonus = "ATK Speed +55%\nATK +85%"
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
                requiredCount = 5,
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
                requiredCount = 5,
                enhanceCost = 500,
                description = "Throws a spinning blade that pierces enemies and returns to the player.",
                baseStatsSummary = "ATK <color=#FFCB49>36</color>\n<color=#FFCB49>Normal</color> ATK Speed",
                magicBonus = "ATK Speed +9%",
                rareBonus = "ATK Speed +18%\nGeneration speed -30%",
                uniqueBonus = "Spin Speed +36%",
                epicBonus = "ATK Speed +36%\nGeneration speed -36%\nGuaranteed Pierce (5s, CD 15s)"
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
                requiredCount = 5,
                enhanceCost = 500,
                description = "Fires a rain of bullets in multiple directions at once.",
                baseStatsSummary = "ATK <color=#FFCB49>19</color> | 3 shells\n<color=#FFCB49>Slow</color> ATK Speed",
                magicBonus = "ATK +5%",
                rareBonus = "ATK Speed +5%",
                uniqueBonus = "ATK +10%",
                epicBonus = "Adds +2 shells\n360° Bullet Storm (5s, CD 115s)"
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
                requiredCount = 5,
                enhanceCost = 500,
                description = "Deploys a turret that fires standard rounds.",
                baseStatsSummary = "Turret ATK <color=#FFCB49>27</color> | Duration 10s | CD 8.4s\n<color=#FFCB49>Normal</color> ATK Speed",
                magicBonus = "Turret Duration +20%",
                rareBonus = "Turret Cooldown -30%\nATK +15%",
                uniqueBonus = "Turret Duration +20%",
                epicBonus = "Turret Duration +30%\nATK +30%"
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
                requiredCount = 5,
                enhanceCost = 500,
                description = "Spins a spiky discus around the player to attack enemies.",
                baseStatsSummary = "Discus ATK <color=#FFCB49>30</color>\n<color=#FFCB49>Normal</color> Spin Speed",
                magicBonus = "+1 Discus",
                rareBonus = "Spin Speed +30%",
                uniqueBonus = "+1 Discus",
                epicBonus = "+1 Discus\nSpin Speed +35%"
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
                requiredCount = 5,
                enhanceCost = 500,
                description = "Deals heavy damage to nearby enemies with multiple pellets.",
                baseStatsSummary = "ATK <color=#FFCB49>86</color>\n<color=#FFCB49>Slow</color> ATK Speed",
                magicBonus = "ATK +15%",
                rareBonus = "ATK +15%",
                uniqueBonus = "ATK +25%",
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
                requiredCount = 5,
                enhanceCost = 500,
                hasStar = false,
                description = "Steals life from enemies.",
                baseStatsSummary = "Life Steal <color=#FFCB49>2.3%</color>",
                magicBonus = "All Weapons' +1% Life Steal",
                rareBonus = "All Weapons' +5% Life Steal",
                uniqueBonus = "All Weapons' +8% Life Steal",
                epicBonus = "All Weapons' +10% Life Steal"
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
                requiredCount = 5,
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
            LoadSavedChipProgress(chip);
        }
        return result;
    }

    private void SyncChipsetProgressFromSave()
    {
        // Update existing objects so equipped cards and an open detail keep their references.
        foreach (ChipItemData chip in allChips)
        {
            if (chip != null) LoadSavedChipProgress(chip);
        }
        ApplyTierUnlockRulesToCatalog();
    }

    private static void LoadSavedChipProgress(ChipItemData chip)
    {
        if (!PlayerDataService.LoadChipsetItemData(
                chip.id,
                out int savedLevel,
                out int savedTier,
                out int savedCount,
                out int savedRequiredCount,
                out bool savedHasStar))
        {
            return;
        }

        chip.tier = (ChipTier)Mathf.Clamp(savedTier, (int)ChipTier.Magic, (int)ChipTier.Holographic);
        chip.level = Mathf.Clamp(savedLevel, 1, ChipItemData.GetMaxLevelForTier(chip.tier));
        chip.count = Mathf.Max(0, savedCount);
        chip.requiredCount = Mathf.Max(0, savedRequiredCount);
        chip.hasStar = savedHasStar;
        chip.tierEnhanceCount = PlayerDataService.LoadChipsetTierEnhanceCount(chip.id);
        chip.enhanceCost = PlayerDataService.LoadChipsetEnhanceCost(chip.id, chip.enhanceCost);
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

        return equipped;
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
            if (byTierBg != null) byTierBg.raycastTarget = true;
            if (byTierBtn.targetGraphic != null) byTierBtn.targetGraphic.raycastTarget = true;
        }
        if (byQuantityBtn != null)
        {
            byQuantityBtn.onClick.RemoveAllListeners();
            byQuantityBtn.onClick.AddListener(() => SetSortMode(true));
            if (byQuantityBg != null) byQuantityBg.raycastTarget = true;
            if (byQuantityBtn.targetGraphic != null) byQuantityBtn.targetGraphic.raycastTarget = true;
        }

        if (blastFurnaceBtn != null) blastFurnaceBtn.onClick.AddListener(OpenFurnaceModal);
        if (furnaceCloseBtn != null) furnaceCloseBtn.onClick.AddListener(() => furnaceModal.SetActive(false));
        if (furnaceDismantleBtn != null) furnaceDismantleBtn.onClick.AddListener(ExecuteDismantle);

        if (chipsetModeBg != null) chipsetModeBg.raycastTarget = true;
        if (highTechModeBg != null) highTechModeBg.raycastTarget = true;

        if (chipsetModeBtn != null) chipsetModeBtn.onClick.AddListener(() => ShowToast("Chipset Configuration Active"));
        if (highTechModeBtn != null) highTechModeBtn.onClick.AddListener(() => ShowToast("High-Tech Chipset unlocks at Chapter 10!"));

        if (detailCloseBtn != null) detailCloseBtn.onClick.AddListener(CloseDetailModal);
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
            if (byTierBg != null) byTierBg.raycastTarget = true;
            if (byTierBtn.targetGraphic != null) byTierBtn.targetGraphic.raycastTarget = true;
        }

        if (byQuantityBtn != null)
        {
            if (byQuantityBg == null) byQuantityBg = byQuantityBtn.GetComponent<Image>() ?? byQuantityBtn.targetGraphic as Image;
            if (byQuantityText == null) byQuantityText = byQuantityBtn.GetComponentInChildren<TMP_Text>(true);
            if (byQuantityBg != null) byQuantityBg.raycastTarget = true;
            if (byQuantityBtn.targetGraphic != null) byQuantityBtn.targetGraphic.raycastTarget = true;
        }

        LoadSortSpritesIfMissing();
        LoadEquipSpritesIfMissing();
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

    public void LoadEquipSpritesIfMissing()
    {
        if (equipBadgeSprite == null || equipBadgeSprite.name.Equals("Equip", StringComparison.OrdinalIgnoreCase))
        {
            equipBadgeSprite = Resources.Load<Sprite>("UI/Chipset/badge-equip-arrow");
#if UNITY_EDITOR
            if (equipBadgeSprite == null)
            {
                equipBadgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Chipset/badge-equip-arrow.png");
            }
#endif
        }
    }

    public void LoadAudioClipsIfMissing()
    {
        if (equipSFX == null)
        {
            equipSFX = Resources.Load<AudioClip>("Audio/SFX_Equip_Chipset");
#if UNITY_EDITOR
            if (equipSFX == null)
            {
                equipSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Equip_Chipset.wav");
            }
#endif
        }

        if (presetSwitchSFX == null)
        {
            presetSwitchSFX = Resources.Load<AudioClip>("Audio/SFX_Preset_Switch");
#if UNITY_EDITOR
            if (presetSwitchSFX == null)
            {
                presetSwitchSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Preset_Switch.wav");
            }
#endif
        }
    }

    public void PlayEquipSFX()
    {
        LoadAudioClipsIfMissing();
        if (equipSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(equipSFX, 1f);
        }
    }

    public void PlayPresetSwitchSFX()
    {
        LoadAudioClipsIfMissing();
        if (presetSwitchSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(presetSwitchSFX, 0.9f);
        }
    }

    public void SwitchDeck(int deckIndex)
    {
        CancelEquipSelection();
        int prevDeck = activeDeckIndex;
        activeDeckIndex = Mathf.Clamp(deckIndex, 0, 2);
        PlayerDataService.ActiveChipsetDeckIndex = activeDeckIndex;
        RefreshPresetButtons();
        RefreshEquippedGrid();
        ShowToast($"Switched to Preset Deck {deckIndex + 1}");
        if (prevDeck != activeDeckIndex)
        {
            PlayPresetSwitchSFX();
        }
    }

    public void SetSortMode(bool quantitySort)
    {
        CancelEquipSelection();
        sortByQuantity = quantitySort;
        RefreshSortButtons();
        RefreshInventory();
    }

    private void RefreshTopBar()
    {
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
            byTierBg.raycastTarget = true;
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
            byQuantityBg.raycastTarget = true;
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
                    int slotIndex = i;
                    equippedSlots[i].Setup(chip, icon, frame, (c) => OpenDetailModalFromEquippedSlot(c, slotIndex), (c) => OpenDetailModalFromEquippedSlot(c, slotIndex));
                    equippedSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    int slotIndex = i;
                    equippedSlots[i].SetupEmpty(defaultFrame, () => ShowToast($"Slot {slotIndex + 1} is empty. Select a chip below to equip."));
                    equippedSlots[i].gameObject.SetActive(true);
                }
            }

            if (isEquipSelectMode && pendingEquipChip != null)
            {
                int targetSlot = i;
                equippedSlots[i].SetEquipTargetMode(true, equipBadgeSprite, () => OnEquipSlotSelected(targetSlot));
            }
            else
            {
                equippedSlots[i].SetEquipTargetMode(false);
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
            card.Setup(data, icon, frame, OpenDetailModalFromInventory, OpenDetailModalFromInventory);
            bool isSelected = (isEquipSelectMode && pendingEquipChip != null && pendingEquipChip.id == data.id);
            card.SetInventoryEquipState(isEquipSelectMode, isSelected);
            card.gameObject.SetActive(true);
        }

        for (int i = sortedList.Count; i < spawnedInventoryCards.Count; i++)
        {
            spawnedInventoryCards[i].gameObject.SetActive(false);
        }
    }

    public void QuickUpgradeChip(ChipItemData chip)
    {
        OpenDetailModalFromInventory(chip);
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

        OpenDetailModalFromInventory(chip);
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
                detailCloseBtn.onClick.AddListener(CloseDetailModal);
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

            if (perkRowIcons == null || perkRowIcons.Length < 4 || perkRowIcons.Any(img => img == null))
            {
                perkRowIcons = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    Transform row = detailModal.transform.Find($"ModalBox/PerkRow_{i}")
                                 ?? detailModal.transform.Find($"Box/PerkRow_{i}");
                    if (row != null)
                    {
                        perkRowIcons[i] = row.Find("LockIcon")?.GetComponent<Image>()
                                       ?? row.GetComponentInChildren<Image>();
                    }
                }
            }

            if (perkRowTexts == null || perkRowTexts.Length < 4 || perkRowTexts.Any(txt => txt == null))
            {
                perkRowTexts = new TMP_Text[4];
                for (int i = 0; i < 4; i++)
                {
                    Transform row = detailModal.transform.Find($"ModalBox/PerkRow_{i}")
                                 ?? detailModal.transform.Find($"Box/PerkRow_{i}");
                    if (row != null)
                    {
                        perkRowTexts[i] = row.Find("PerkText")?.GetComponent<TMP_Text>()
                                       ?? row.GetComponentInChildren<TMP_Text>();
                    }
                }
            }

            EnsureNoticeReferences();
        }
    }

    public void OpenDetailModalFromEquippedSlot(ChipItemData chip, int slotIndex)
    {
        openedFromEquippedSlotIndex = slotIndex;
        OpenDetailModal(chip);
    }

    public void OpenDetailModalFromInventory(ChipItemData chip)
    {
        openedFromEquippedSlotIndex = -1;
        OpenDetailModal(chip);
    }

    public void OpenDetailModal(ChipItemData chip)
    {
        if (chip == null) return;
        // Do not reopen detail modal right after equipping into a slot
        if (Time.frameCount == lastEquipFrame || (lastEquipTime > 0f && Time.unscaledTime - lastEquipTime < 0.35f))
        {
            return;
        }

        CancelEquipSelection();
        AutoWireDetailModalIfMissing();

        if (detailModal == null)
        {
            Debug.LogWarning("[ChipsetController] detailModal is not assigned or found in scene!");
            return;
        }

        selectedDetailChip = chip;
        detailModal.SetActive(true);
        detailModal.transform.SetAsLastSibling();
        RefreshDetailModal();
    }

    public void RefreshDetailModal()
    {
        if (selectedDetailChip == null) return;
        EnsureLockTierSprites();
        ApplyModalButtonsDesign(detailEnhanceBtn, detailAdvanceTierBtn);

        // 1. Mod Badge
        if (detailModBadgeText != null)
        {
            detailModBadgeText.text = $"MOD • UP TO LV{selectedDetailChip.MaxLevel:00}";
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
        if (detailBaseStatsText != null) detailBaseStatsText.text = GetDynamicBaseStatsSummary(selectedDetailChip);

        // 5. 4 Tier Perk Rows with color tags
        string[] tierNames = { "Rare", "Unique", "Epic", "Holo" };
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
                if (isUnlocked)
                {
                    if (i < unlockedTierSprites.Length && unlockedTierSprites[i] != null)
                    {
                        perkRowIcons[i].sprite = unlockedTierSprites[i];
                    }
                    else if (unlockedCheckSprite != null)
                    {
                        perkRowIcons[i].sprite = unlockedCheckSprite;
                    }

                    if (i == 3 && perkRowIcons[i].sprite != null)
                    {
                        perkRowIcons[i].material = ChipsetFrameShimmerMaterial.Get(perkRowIcons[i].sprite);
                    }
                    else
                    {
                        perkRowIcons[i].material = null;
                    }
                }
                else if (i < lockTierSprites.Length && lockTierSprites[i] != null)
                {
                    perkRowIcons[i].sprite = lockTierSprites[i];
                    if (i == 3) // Row 3 is Red Lock (Holographic)
                    {
                        perkRowIcons[i].material = ChipsetFrameShimmerMaterial.Get(lockTierSprites[i]);
                    }
                    else
                    {
                        perkRowIcons[i].material = null;
                    }
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
        EnsureNoticeReferences();
        bool isMaxEnhanceForFrame = selectedDetailChip.IsMaxEnhanceForCurrentFrame;
        bool hasEnoughDataChips = ChipManager.HasEnoughDataChips(selectedDetailChip.enhanceCost);
        bool canEnhance = !isMaxEnhanceForFrame && hasEnoughDataChips;

        if (detailEnhanceCostText != null)
        {
            detailEnhanceCostText.color = Color.white;
            detailEnhanceCostText.text = selectedDetailChip.tier < ChipTier.Holographic
                ? $"{selectedDetailChip.enhanceCost}  <color=#FDE047>({selectedDetailChip.tierEnhanceCount}/{selectedDetailChip.RequiredTierEnhances})</color>"
                : $"{selectedDetailChip.enhanceCost}";
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailEnhanceCostText.rectTransform);
            if (detailEnhanceCostText.transform.parent is RectTransform parentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            }
        }
        SetButtonBrightness(detailEnhanceBtn, enhanceBtnCanvasGroup, canEnhance);

        // 7. Advance Tier Button
        bool isMaxTier = selectedDetailChip.tier >= ChipTier.Holographic;
        bool hasFragments = !isMaxTier && selectedDetailChip.count >= selectedDetailChip.CurrentAdvanceCost;

        if (detailAdvanceTierText != null)
        {
            if (isMaxTier)
            {
                detailAdvanceTierText.text = "<size=24><b>MAX TIER</b></size>";
            }
            else
            {
                string countColor = hasFragments ? "#4ADE80" : "#BAE6FD";
                detailAdvanceTierText.text = $"<size=23><b>ADVANCE TIER</b></size>\n<size=18><color={countColor}>({selectedDetailChip.count}/{selectedDetailChip.CurrentAdvanceCost})</color></size>";
            }
        }
        SetButtonBrightness(detailAdvanceTierBtn, advanceTierBtnCanvasGroup, hasFragments);

        // 8. Equip Button (Only shown when opened from inventory below, hidden when viewing from equipped slot)
        bool showEquipBtn = (openedFromEquippedSlotIndex < 0);
        if (detailEquipBtn != null)
        {
            detailEquipBtn.gameObject.SetActive(showEquipBtn);
        }
        if (detailEquipBtnText != null)
        {
            detailEquipBtnText.text = "EQUIP";
        }
    }

    private void EnhanceSelectedChip()
    {
        if (selectedDetailChip == null) return;
        EnsureNoticeReferences();

        // 1. Nếu enchan tối đa của khung hiện tại thì sẽ không hiện gì
        if (selectedDetailChip.IsMaxEnhanceForCurrentFrame)
        {
            return;
        }

        // 2. Nếu ko đủ data chip thì hiện bảng ở hình 2
        if (!ChipManager.HasEnoughDataChips(selectedDetailChip.enhanceCost))
        {
            ShowNotice(notEnoughChipsNotice);
            return;
        }

        // 3. Đủ điều kiện: Nâng cấp!
        if (selectedDetailChip.Enhance())
        {
            SaveChipProgress(selectedDetailChip);
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            HideAllNoticesInstant();
            ShowToast($"Enhanced {selectedDetailChip.chipName} to LV.{selectedDetailChip.level:00}!");
        }
    }

    private void AdvanceTierSelectedChip()
    {
        if (selectedDetailChip == null) return;
        EnsureNoticeReferences();

        if (selectedDetailChip.tier >= ChipTier.Holographic)
        {
            return;
        }

        // 1. Nếu ko đủ mảnh thì hiện bảng ở hình 1
        if (selectedDetailChip.count < selectedDetailChip.CurrentAdvanceCost)
        {
            ShowNotice(notEnoughFragmentsNotice);
            return;
        }

        // 2. Nếu chưa hoàn thành Enhance của khung hiện tại
        if (!selectedDetailChip.IsTierUnlockReady)
        {
            ShowToast($"Enhance {selectedDetailChip.RequiredTierEnhances} times before advancing this frame!");
            return;
        }

        // 3. Nếu là Epic lên Holo cần thêm Red Gems
        if (!selectedDetailChip.HasAdvanceCurrency)
        {
            if (selectedDetailChip.tier == ChipTier.Epic)
            {
                ShowToast($"Need {selectedDetailChip.YellowToRedDataChipCost} Red Gems to advance!");
            }
            return;
        }

        // 4. Đủ mảnh và đủ điều kiện: Thăng bậc khung!
        if (selectedDetailChip.AdvanceTier())
        {
            SaveChipProgress(selectedDetailChip);
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            HideAllNoticesInstant();
            ShowToast($"Unlocked {GetFrameColorName(selectedDetailChip.tier)} frame for {selectedDetailChip.chipName}!");
        }
    }

    private void ToggleEquipSelectedChip()
    {
        if (selectedDetailChip == null) return;

        pendingEquipChip = selectedDetailChip;
        isEquipSelectMode = true;

        if (detailModal != null)
        {
            detailModal.SetActive(false);
        }

        RefreshEquippedGrid();
        UpdateInventoryEquipVisuals();
        ShowToast($"Tap a slot above to equip {pendingEquipChip.chipName}");
    }

    public void OnEquipSlotSelected(int slotIndex)
    {
        if (!isEquipSelectMode || pendingEquipChip == null) return;

        int[] currentDeck = deckEquippedIds[activeDeckIndex];
        if (currentDeck == null || slotIndex < 0 || slotIndex >= currentDeck.Length) return;

        // If this exact slot already has this chip equipped, do not unequip (cannot unequip from slot)
        if (currentDeck[slotIndex] == pendingEquipChip.id)
        {
            string chipName = pendingEquipChip.chipName;
            CancelEquipSelection();
            ShowToast($"{chipName} is already equipped in Slot {slotIndex + 1}!");
            return;
        }

        int existingIndex = Array.IndexOf(currentDeck, pendingEquipChip.id);
        int replacedChipId = currentDeck[slotIndex];
        bool isSwap = false;

        // If this chip was already equipped in another slot in this deck:
        // Swap slots: place the target slot's chip into existingIndex (or -1 if target slot was empty)
        if (existingIndex >= 0)
        {
            currentDeck[existingIndex] = replacedChipId;
            if (replacedChipId >= 0)
            {
                isSwap = true;
            }
        }

        // Equip into targeted slot
        currentDeck[slotIndex] = pendingEquipChip.id;

        string equipName = pendingEquipChip.chipName;
        isEquipSelectMode = false;
        pendingEquipChip = null;
        lastEquipFrame = Time.frameCount;
        lastEquipTime = Time.unscaledTime;
        if (detailModal != null) detailModal.SetActive(false);

        PlayerDataService.SaveChipsetDeck(activeDeckIndex, currentDeck);
        RefreshEquippedGrid();
        RefreshInventory();
        UpdateInventoryEquipVisuals();

        if (isSwap)
        {
            ChipItemData swappedChip = allChips.FirstOrDefault(c => c.id == replacedChipId);
            string swappedName = swappedChip != null ? swappedChip.chipName : $"Slot {existingIndex + 1}";
            ShowToast($"Swapped {equipName} with {swappedName}!");
        }
        else
        {
            ShowToast($"Equipped {equipName} to Slot {slotIndex + 1}!");
        }
        PlayEquipSFX();
    }

    public void CancelEquipSelection()
    {
        if (!isEquipSelectMode) return;
        isEquipSelectMode = false;
        pendingEquipChip = null;
        RefreshEquippedGrid();
        UpdateInventoryEquipVisuals();
    }

    public void UpdateInventoryEquipVisuals()
    {
        if (spawnedInventoryCards == null) return;
        foreach (var card in spawnedInventoryCards)
        {
            if (card == null || !card.gameObject.activeSelf) continue;
            bool isSelected = (isEquipSelectMode && pendingEquipChip != null && card.BoundData != null && card.BoundData.id == pendingEquipChip.id);
            card.SetInventoryEquipState(isEquipSelectMode, isSelected);
        }
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

    public static string GetDynamicBaseStatsSummary(ChipItemData chip)
    {
        if (chip == null) return string.Empty;
        int tierIdx = GetFrameIndex(chip.tier); // 0: Magic/Green, 1: Rare/Blue, 2: Unique/Purple, 3: Epic/Yellow, 4: Holo/Red

        switch (chip.id)
        {
            case 1: // Standard Gun
            {
                float atkMultiplier = tierIdx >= 1 ? 1.15f : 1.0f;
                float finalAtk = 53f * atkMultiplier;
                string speedText = tierIdx >= 2 ? "Very Fast" : "Fast";
                string summary = $"ATK <color=#FFCB49>{finalAtk:F0}</color>\n<color=#FFCB49>{speedText}</color> ATK Speed";
                if (tierIdx >= 3) summary += "\nLife Steal <color=#FFCB49>+5%</color>";
                if (tierIdx >= 4) summary += "\n<color=#FFCB49>50%</color> Ricochet Chance";
                return summary;
            }
            case 2: // Rifle
            {
                float atkMultiplier = 1.0f;
                if (tierIdx >= 1) atkMultiplier += 0.25f;
                if (tierIdx >= 3) atkMultiplier += 0.80f;
                if (tierIdx >= 4) atkMultiplier += 0.85f;
                float finalAtk = 10.5f * atkMultiplier;
                string speedText = tierIdx >= 4 ? "Extreme" : (tierIdx >= 2 ? "Ultra Fast" : "Fast");
                return $"ATK <color=#FFCB49>{finalAtk:F1}</color>\n<color=#FFCB49>{speedText}</color> ATK Speed";
            }
            case 3: // Rocket Punch
            {
                float dmgMultiplier = 1.0f;
                if (tierIdx >= 1) dmgMultiplier += 0.40f;
                if (tierIdx >= 4) dmgMultiplier += 1.80f;
                float directAtk = 70f * dmgMultiplier;
                float aoeAtk = 37f * dmgMultiplier;
                string speedText = tierIdx >= 2 ? "Normal" : "Slow";
                string summary = $"ATK <color=#FFCB49>{directAtk:F0}</color> / AoE ATK <color=#FFCB49>{aoeAtk:F0}</color>\n<color=#FFCB49>{speedText}</color> ATK Speed";
                if (tierIdx >= 3) summary += "\nAoE Radius <color=#FFCB49>+40%</color>";
                return summary;
            }
            case 4: // Spinning Blade
            {
                string speedText = tierIdx >= 4 ? "Ultra Fast" : (tierIdx >= 2 ? "Fast" : (tierIdx >= 1 ? "Fast" : "Normal"));
                string summary = $"ATK <color=#FFCB49>36</color>\n<color=#FFCB49>{speedText}</color> ATK Speed";
                if (tierIdx >= 3) summary += "\nSpin Speed <color=#FFCB49>+36%</color>";
                if (tierIdx >= 4) summary += "\n<color=#FFCB49>Guaranteed Pierce</color> (5s, CD 15s)";
                return summary;
            }
            case 5: // Multigun
            {
                float atkMultiplier = 1.0f;
                if (tierIdx >= 1) atkMultiplier += 0.05f;
                if (tierIdx >= 3) atkMultiplier += 0.10f;
                float finalAtk = 19f * atkMultiplier;
                int shells = tierIdx >= 4 ? 5 : 3;
                string speedText = tierIdx >= 2 ? "Normal" : "Slow";
                string summary = $"ATK <color=#FFCB49>{finalAtk:F0}</color> | {shells} shells\n<color=#FFCB49>{speedText}</color> ATK Speed";
                if (tierIdx >= 4) summary += "\n<color=#FFCB49>360° Bullet Storm</color> (5s, CD 115s)";
                return summary;
            }
            case 6: // Gun Turret
            {
                float dmgMultiplier = 1.0f;
                if (tierIdx >= 2) dmgMultiplier += 0.15f;
                if (tierIdx >= 4) dmgMultiplier += 0.30f;
                float finalAtk = 27f * dmgMultiplier;

                float duration = 10f;
                if (tierIdx >= 1) duration *= 1.20f;
                if (tierIdx >= 3) duration *= 1.20f;
                if (tierIdx >= 4) duration *= 1.30f;

                float cd = tierIdx >= 2 ? (8.4f * 0.70f) : 8.4f;
                return $"Turret ATK <color=#FFCB49>{finalAtk:F0}</color> | Duration {duration:F1}s | CD {cd:F1}s\n<color=#FFCB49>Normal</color> ATK Speed";
            }
            case 7: // Spiky Discus
            {
                int discusCount = 1;
                if (tierIdx >= 1) discusCount++;
                if (tierIdx >= 3) discusCount++;
                if (tierIdx >= 4) discusCount++;
                string spinSpeed = tierIdx >= 4 ? "Ultra Fast (+65%)" : (tierIdx >= 2 ? "Fast (+30%)" : "Normal");
                return $"Discus ATK <color=#FFCB49>30</color> | {discusCount} Discus\n<color=#FFCB49>{spinSpeed}</color> Spin Speed";
            }
            case 8: // Shotgun
            {
                float atkMultiplier = 1.0f;
                if (tierIdx >= 1) atkMultiplier += 0.15f;
                if (tierIdx >= 2) atkMultiplier += 0.15f;
                if (tierIdx >= 3) atkMultiplier += 0.25f;
                float finalAtk = 86f * atkMultiplier;
                string summary = $"ATK <color=#FFCB49>{finalAtk:F0}</color>\n<color=#FFCB49>Slow</color> ATK Speed";
                if (tierIdx >= 4) summary += "\n<color=#FFCB49>Double Fire</color> (10 pellets)";
                return summary;
            }
            case 9: // Energy Jumper Cables
            {
                float lifeSteal = 2.3f;
                if (tierIdx >= 1) lifeSteal += 1.0f;
                if (tierIdx >= 2) lifeSteal += 5.0f;
                if (tierIdx >= 3) lifeSteal += 8.0f;
                if (tierIdx >= 4) lifeSteal += 10.0f;
                string summary = $"Life Steal <color=#FFCB49>{lifeSteal:F1}%</color>";
                if (tierIdx >= 1) summary += "\n<color=#40DAD2>All Weapons Gain Life Steal</color>";
                return summary;
            }
            case 10: // High-Explosive Mine
            {
                float dmgMultiplier = 1.0f;
                if (tierIdx >= 1) dmgMultiplier += 0.20f;
                if (tierIdx >= 3) dmgMultiplier += 0.55f;
                if (tierIdx >= 4) dmgMultiplier += 1.44f;
                float finalAtk = 27f * dmgMultiplier;
                float cd = tierIdx >= 2 ? (5.55f * 0.80f) : 5.55f;
                return $"Mine AoE ATK <color=#FFCB49>{finalAtk:F0}</color>\nCooldown: {cd:F2}s";
            }
            default:
                return chip.baseStatsSummary ?? string.Empty;
        }
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

    public void CloseDetailModal()
    {
        openedFromEquippedSlotIndex = -1;
        HideAllNoticesInstant();
        if (detailModal != null) detailModal.SetActive(false);
    }

    public void SetButtonBrightness(Button btn, CanvasGroup cg, bool isBright)
    {
        if (btn == null) return;
        btn.interactable = true; // Giữ true để người chơi click vẫn nhận diện sự kiện mở bảng thông báo

        if (cg != null)
        {
            cg.alpha = isBright ? 1.0f : 0.48f;
        }
        else
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isBright ? Color.white : new Color(0.48f, 0.48f, 0.48f, 1f);
            }
        }
    }

    private Coroutine noticeDismissRoutine;

    public void ShowNotice(GameObject noticeObj)
    {
        if (noticeObj == null) return;

        EnsureNoticeReferences();

        if (notEnoughFragmentsNotice != null && notEnoughFragmentsNotice != noticeObj)
        {
            notEnoughFragmentsNotice.SetActive(false);
        }
        if (notEnoughChipsNotice != null && notEnoughChipsNotice != noticeObj)
        {
            notEnoughChipsNotice.SetActive(false);
        }

        if (noticeDismissRoutine != null)
        {
            StopCoroutine(noticeDismissRoutine);
            noticeDismissRoutine = null;
        }

        // Hiện bảng sắc nét ngay tức thì
        UIDissolveController.ShowInstant(noticeObj);

        // Tự động tắt sau 2 giây áp dụng hiệu ứng shader tan biến
        noticeDismissRoutine = StartCoroutine(AutoDismissNoticeRoutine(noticeObj));
    }

    private IEnumerator AutoDismissNoticeRoutine(GameObject noticeObj)
    {
        yield return new WaitForSecondsRealtime(2.0f);
        if (noticeObj != null && noticeObj.activeSelf)
        {
            UIDissolveController.HideWithEffect(noticeObj);
        }
        noticeDismissRoutine = null;
    }

    public void HideAllNoticesInstant()
    {
        if (noticeDismissRoutine != null)
        {
            StopCoroutine(noticeDismissRoutine);
            noticeDismissRoutine = null;
        }
        if (notEnoughFragmentsNotice != null) notEnoughFragmentsNotice.SetActive(false);
        if (notEnoughChipsNotice != null) notEnoughChipsNotice.SetActive(false);
    }

    public void EnsureNoticeReferences()
    {
        Transform boxParent = detailModal != null
            ? detailModal.transform.Find("ModalBox") ?? detailModal.transform.Find("Box") ?? detailModal.transform
            : null;

        if (boxParent != null)
        {
            if (notEnoughFragmentsNotice == null)
            {
                Transform existing = boxParent.Find("NotEnoughFragmentsNotice");
                if (existing != null)
                {
                    notEnoughFragmentsNotice = existing.gameObject;
                }
                else
                {
                    notEnoughFragmentsNotice = CreateNoticePanelRuntime(boxParent, "NotEnoughFragmentsNotice",
                        "You need to collect more Chipsets.",
                        "You can purchase Chipset Boxes at the\nShop.");
                }
            }

            if (notEnoughChipsNotice == null)
            {
                Transform existing = boxParent.Find("NotEnoughChipsNotice");
                if (existing != null)
                {
                    notEnoughChipsNotice = existing.gameObject;
                }
                else
                {
                    notEnoughChipsNotice = CreateNoticePanelRuntime(boxParent, "NotEnoughChipsNotice",
                        "Not enough Data Chips",
                        null);
                }
            }
        }

        if (enhanceBtnCanvasGroup == null && detailEnhanceBtn != null)
        {
            enhanceBtnCanvasGroup = detailEnhanceBtn.GetComponent<CanvasGroup>() ?? detailEnhanceBtn.gameObject.AddComponent<CanvasGroup>();
        }

        if (advanceTierBtnCanvasGroup == null && detailAdvanceTierBtn != null)
        {
            advanceTierBtnCanvasGroup = detailAdvanceTierBtn.GetComponent<CanvasGroup>() ?? detailAdvanceTierBtn.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private GameObject CreateNoticePanelRuntime(Transform parent, string name, string line1, string line2)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.SetParent(parent, false);
        rootRt.anchorMin = new Vector2(0.5f, 0.54f);
        rootRt.anchorMax = new Vector2(0.5f, 0.54f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = new Vector2(830f, 270f);

        Image borderImg = root.AddComponent<Image>();
        borderImg.color = new Color32(64, 218, 210, 255);
        borderImg.raycastTarget = false;

        Shadow shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 210);
        shadow.effectDistance = new Vector2(7f, -8f);
        shadow.useGraphicAlpha = true;

        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.layer = LayerMask.NameToLayer("UI");
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.SetParent(rootRt, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.offsetMin = new Vector2(6f, 6f);
        bgRt.offsetMax = new Vector2(-6f, -6f);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color32(11, 40, 56, 252);
        bgImg.raycastTarget = false;

        if (string.IsNullOrEmpty(line2))
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.layer = LayerMask.NameToLayer("UI");
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.SetParent(rootRt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 20f);
            textRt.offsetMax = new Vector2(-20f, -20f);

            TMP_Text txt = textObj.AddComponent<TextMeshProUGUI>();
            AssignRuntimeNoticeFont(txt);
            txt.text = line1;
            txt.fontSize = 36f;
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            ApplyRuntimeNoticeOutline(txt);
            txt.raycastTarget = false;
        }
        else
        {
            GameObject line1Obj = new GameObject("Line1", typeof(RectTransform));
            line1Obj.layer = LayerMask.NameToLayer("UI");
            RectTransform line1Rt = line1Obj.GetComponent<RectTransform>();
            line1Rt.SetParent(rootRt, false);
            line1Rt.anchorMin = new Vector2(0f, 0.50f);
            line1Rt.anchorMax = new Vector2(1f, 0.95f);
            line1Rt.offsetMin = new Vector2(20f, 0f);
            line1Rt.offsetMax = new Vector2(-20f, 0f);

            TMP_Text txt1 = line1Obj.AddComponent<TextMeshProUGUI>();
            AssignRuntimeNoticeFont(txt1);
            txt1.text = line1;
            txt1.fontSize = 32f;
            txt1.color = Color.white;
            txt1.fontStyle = FontStyles.Bold;
            txt1.alignment = TextAlignmentOptions.Center;
            ApplyRuntimeNoticeOutline(txt1);
            txt1.raycastTarget = false;

            GameObject line2Obj = new GameObject("Line2", typeof(RectTransform));
            line2Obj.layer = LayerMask.NameToLayer("UI");
            RectTransform line2Rt = line2Obj.GetComponent<RectTransform>();
            line2Rt.SetParent(rootRt, false);
            line2Rt.anchorMin = new Vector2(0f, 0.08f);
            line2Rt.anchorMax = new Vector2(1f, 0.52f);
            line2Rt.offsetMin = new Vector2(20f, 0f);
            line2Rt.offsetMax = new Vector2(-20f, 0f);

            TMP_Text txt2 = line2Obj.AddComponent<TextMeshProUGUI>();
            AssignRuntimeNoticeFont(txt2);
            txt2.text = line2;
            txt2.fontSize = 26f;
            txt2.color = new Color32(254, 209, 66, 255);
            txt2.fontStyle = FontStyles.Bold;
            txt2.alignment = TextAlignmentOptions.Center;
            ApplyRuntimeNoticeOutline(txt2);
            txt2.raycastTarget = false;
        }

        root.AddComponent<UIDissolveController>();
        root.SetActive(false);
        return root;
    }

    private void AssignRuntimeNoticeFont(TMP_Text text)
    {
        if (text == null || text.font != null) return;

        TMP_FontAsset referenceFont = detailNameText != null && detailNameText.font != null
            ? detailNameText.font
            : detailModBadgeText != null && detailModBadgeText.font != null
                ? detailModBadgeText.font
                : TMP_Settings.defaultFontAsset;

        if (referenceFont != null) text.font = referenceFont;
    }

    private static void ApplyRuntimeNoticeOutline(TMP_Text text)
    {
        if (text == null) return;
        ChipsetCardUI.EnsureFontAndMaterial(text);
    }

    private static Sprite cachedEnhanceSprite;
    private static Sprite cachedAdvanceSprite;
    private static Sprite cachedDataChipSprite;

    public static Sprite GetEnhanceSprite()
    {
        if (cachedEnhanceSprite != null) return cachedEnhanceSprite;
        cachedEnhanceSprite = Resources.Load<Sprite>("UI/btn_enhance_green");
#if UNITY_EDITOR
        if (cachedEnhanceSprite == null)
        {
            cachedEnhanceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/btn_enhance_green.png");
        }
#endif
        return cachedEnhanceSprite;
    }

    public static Sprite GetAdvanceSprite()
    {
        if (cachedAdvanceSprite != null) return cachedAdvanceSprite;
        cachedAdvanceSprite = Resources.Load<Sprite>("UI/btn_advance_tier_gold");
#if UNITY_EDITOR
        if (cachedAdvanceSprite == null)
        {
            cachedAdvanceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/btn_advance_tier_gold.png");
        }
#endif
        return cachedAdvanceSprite;
    }

    public static Sprite GetDataChipIcon()
    {
        if (cachedDataChipSprite != null) return cachedDataChipSprite;
        cachedDataChipSprite = Resources.Load<Sprite>("UI/icon_data_chip");
#if UNITY_EDITOR
        if (cachedDataChipSprite == null)
        {
            Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/icon tài nguyên.png")
                ?.OfType<Sprite>()
                ?.ToArray();
            if (sprites != null && sprites.Length > 0)
            {
                cachedDataChipSprite = Array.Find(sprites, s => s != null && s.name == "data") ?? sprites[0];
            }
        }
#endif
        return cachedDataChipSprite;
    }

    public static void ApplyModalButtonsDesign(Button enhanceBtn, Button advanceTierBtn)
    {
        if (enhanceBtn != null)
        {
            RectTransform btnRt = enhanceBtn.GetComponent<RectTransform>();
            if (btnRt != null) btnRt.sizeDelta = new Vector2(360f, 92f);

            Sprite enhanceSprite = GetEnhanceSprite();
            Image borderImg = enhanceBtn.GetComponent<Image>() ?? enhanceBtn.gameObject.AddComponent<Image>();
            if (borderImg != null)
            {
                if (enhanceSprite != null)
                {
                    borderImg.sprite = enhanceSprite;
                    borderImg.color = Color.white;
                    borderImg.type = Image.Type.Simple;
                }
                else
                {
                    borderImg.color = new Color32(6, 78, 59, 255);
                }
                borderImg.raycastTarget = true;
            }

            // Hide/disable child solid background so the textured frame sprite renders cleanly
            Transform bgT = enhanceBtn.transform.Find("Background");
            if (bgT != null)
            {
                Image bgImg = bgT.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = Color.clear;
                    bgImg.enabled = false;
                }
            }

            // Drop shadow
            Shadow shadow = enhanceBtn.GetComponent<Shadow>() ?? enhanceBtn.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0, 14, 24, 210);
            shadow.effectDistance = new Vector2(6f, -6f);
            shadow.useGraphicAlpha = true;

            enhanceBtn.targetGraphic = borderImg;

            // Label ("ENHANCE")
            Transform costRowT = enhanceBtn.transform.Find("CostRow");
            Transform labelT = enhanceBtn.transform.Find("Label")
                ?? enhanceBtn.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != enhanceBtn.transform && t.name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? enhanceBtn.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => costRowT == null || (t.transform.parent != costRowT && t.gameObject.name != "CostValue"))?.transform;

            if (labelT != null)
            {
                labelT.gameObject.SetActive(true);
                RectTransform labelRt = labelT.GetComponent<RectTransform>();
                if (labelRt != null)
                {
                    labelRt.anchorMin = new Vector2(0.5f, 0.68f);
                    labelRt.anchorMax = new Vector2(0.5f, 0.68f);
                    labelRt.pivot = new Vector2(0.5f, 0.5f);
                    labelRt.sizeDelta = new Vector2(320f, 32f);
                    labelRt.anchoredPosition = Vector2.zero;
                }

                TMP_Text labelTmp = labelT.GetComponent<TMP_Text>();
                if (labelTmp != null)
                {
                    labelTmp.enabled = true;
                    labelTmp.text = "ENHANCE";
                    labelTmp.fontSize = 25f;
                    labelTmp.fontStyle = FontStyles.Bold;
                    labelTmp.alignment = TextAlignmentOptions.Center;
                    labelTmp.color = Color.white;
                }
            }

            // CostRow
            if (costRowT != null)
            {
                RectTransform costRowRt = costRowT.GetComponent<RectTransform>();
                if (costRowRt != null)
                {
                    costRowRt.anchorMin = new Vector2(0.5f, 0.28f);
                    costRowRt.anchorMax = new Vector2(0.5f, 0.28f);
                    costRowRt.pivot = new Vector2(0.5f, 0.5f);
                    costRowRt.sizeDelta = new Vector2(340f, 34f);
                    costRowRt.anchoredPosition = Vector2.zero;
                }

                HorizontalLayoutGroup hlg = costRowT.GetComponent<HorizontalLayoutGroup>() ?? costRowT.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(0, 0, 0, 0);
                hlg.spacing = 16f; // Generous 16px gap between chip icon and cost text
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                Transform chipIconT = costRowT.Find("ChipIcon") ?? costRowT.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != costRowT && t.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0);
                Transform costValueT = costRowT.Find("CostValue") ?? costRowT.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != costRowT && t.GetComponent<TMP_Text>() != null);

                // ChipIcon (0) on LEFT, using existing project Data Chip icon
                if (chipIconT != null)
                {
                    chipIconT.SetSiblingIndex(0);
                    RectTransform iconRt = chipIconT.GetComponent<RectTransform>();
                    if (iconRt != null) iconRt.sizeDelta = new Vector2(28f, 28f);
                    Image iconImg = chipIconT.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        Sprite chipSprite = GetDataChipIcon();
                        if (chipSprite != null) iconImg.sprite = chipSprite;
                        iconImg.color = Color.white;
                        iconImg.preserveAspect = true;
                    }
                }

                if (costValueT != null)
                {
                    costValueT.SetSiblingIndex(1);
                    ContentSizeFitter csf = costValueT.GetComponent<ContentSizeFitter>() ?? costValueT.gameObject.AddComponent<ContentSizeFitter>();
                    csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

                    TMP_Text valTmp = costValueT.GetComponent<TMP_Text>();
                    if (valTmp != null)
                    {
                        valTmp.fontSize = 22f;
                        valTmp.fontStyle = FontStyles.Bold;
                        valTmp.alignment = TextAlignmentOptions.Left;
                        valTmp.enableWordWrapping = false;
                        valTmp.overflowMode = TextOverflowModes.Overflow;
                        valTmp.richText = true;
                        valTmp.color = Color.white;
                        valTmp.margin = new Vector4(0, 0, 0, 0);
                    }
                }

                if (costRowRt != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(costRowRt);
                }
            }
        }

        if (advanceTierBtn != null)
        {
            RectTransform btnRt = advanceTierBtn.GetComponent<RectTransform>();
            if (btnRt != null) btnRt.sizeDelta = new Vector2(360f, 85f);

            Sprite advanceSprite = GetAdvanceSprite();
            Image borderImg = advanceTierBtn.GetComponent<Image>() ?? advanceTierBtn.gameObject.AddComponent<Image>();
            if (borderImg != null)
            {
                if (advanceSprite != null)
                {
                    borderImg.sprite = advanceSprite;
                    borderImg.color = Color.white;
                    borderImg.type = Image.Type.Simple;
                }
                else
                {
                    borderImg.color = new Color32(2, 132, 199, 255);
                }
                borderImg.raycastTarget = true;
            }

            // Hide/disable child solid background
            Transform bgT = advanceTierBtn.transform.Find("Background");
            if (bgT != null)
            {
                Image bgImg = bgT.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = Color.clear;
                    bgImg.enabled = false;
                }
            }

            // Drop shadow
            Shadow shadow = advanceTierBtn.GetComponent<Shadow>() ?? advanceTierBtn.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0, 14, 24, 210);
            shadow.effectDistance = new Vector2(6f, -6f);
            shadow.useGraphicAlpha = true;

            advanceTierBtn.targetGraphic = borderImg;

            // Label (Dynamic multi-line text)
            Transform labelT = advanceTierBtn.transform.Find("Label");
            if (labelT != null)
            {
                RectTransform labelRt = labelT.GetComponent<RectTransform>();
                if (labelRt != null)
                {
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.sizeDelta = new Vector2(-20f, 0f);
                    labelRt.pivot = new Vector2(0.5f, 0.5f);
                    labelRt.anchoredPosition = Vector2.zero;
                }

                TMP_Text labelTmp = labelT.GetComponent<TMP_Text>();
                if (labelTmp != null)
                {
                    labelTmp.alignment = TextAlignmentOptions.Center;
                    labelTmp.enableWordWrapping = false;
                    labelTmp.richText = true;
                    labelTmp.color = Color.white;
                    labelTmp.lineSpacing = -10f;
                }
            }
        }
    }
}
