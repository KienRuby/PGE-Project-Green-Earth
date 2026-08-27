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
    public bool hasStar;

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

    public bool CanEnhance => ChipManager.DataChips >= enhanceCost && !IsAtTierCap && !IsMaxOverall;
    public bool CanUpgrade => !IsAtTierCap && count >= requiredCount && requiredCount > 0;
    public bool CanAdvanceTier => IsAtTierCap && tier < ChipTier.Holographic;

    public bool Enhance()
    {
        if (ChipManager.DataChips < enhanceCost) return false;
        if (!ChipManager.TrySpendDataChips(enhanceCost)) return false;
        level++;
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

        if (NeedsAdvanceStones)
        {
            if (!ChipManager.TrySpendAdvanceStones(10))
            {
                return false;
            }
        }

        tier = (ChipTier)((int)tier + 1);
        requiredCount = Mathf.Max(3, level + 1);
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
            hasStar = this.hasStar,
            description = this.description,
            baseStatsSummary = this.baseStatsSummary,
            magicBonus = this.magicBonus,
            rareBonus = this.rareBonus,
            uniqueBonus = this.uniqueBonus,
            epicBonus = this.epicBonus
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
    [SerializeField] private Sprite[] frameSprites; // 0: Magic, 1: Rare, 2: Epic, 3: Holographic
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite upgradeArrowSprite;
    [SerializeField] private Sprite advanceStoneSprite;
    [SerializeField] private Sprite[] lockTierSprites = new Sprite[4]; // 0: Magic, 1: Rare, 2: Unique, 3: Epic
    [SerializeField] private Sprite unlockedCheckSprite;

    private int activeDeckIndex = 2; // Default to Preset 3 (index 2)
    private bool sortByQuantity = true;
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
        if (allChips.Count > 0) return;

        allChips = CreateDefaultDatabase();
        InitializeDefaultDecks();
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
                tier = ChipTier.Epic,
                level = 18,
                count = 451,
                requiredCount = 15,
                enhanceCost = 1200,
                description = "Standard kinetic firearm with consistent rate of fire.",
                baseStatsSummary = "ATK <color=#FFCB49>185</color>\n<color=#FFCB49>Fast</color> ATK Speed",
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
                tier = ChipTier.Holographic,
                level = 24,
                count = 449,
                requiredCount = 0,
                enhanceCost = 2000,
                description = "High-caliber precision rifle dealing devastating critical damage.",
                baseStatsSummary = "ATK <color=#FFCB49>320</color>\n<color=#FFCB49>Very fast</color> ATK Speed",
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
                tier = ChipTier.Rare,
                level = 6,
                count = 470,
                requiredCount = 7,
                enhanceCost = 650,
                description = "Launches heavy mechanical fists inflicting area blast shockwaves.",
                baseStatsSummary = "ATK <color=#FFCB49>95</color> / AoE ATK <color=#FFCB49>60</color>\n<color=#FFCB49>Slow</color> ATK Speed",
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
                tier = ChipTier.Unique,
                level = 14,
                count = 468,
                requiredCount = 9,
                enhanceCost = 900,
                description = "Whirling razor disc cleaving nearby enemies continuously.",
                baseStatsSummary = "ATK <color=#FFCB49>142.8</color>\n<color=#FFCB49>Fast</color> ATK Speed",
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
                tier = ChipTier.Rare,
                level = 6,
                count = 423,
                requiredCount = 3,
                enhanceCost = 650,
                description = "Multi-barrel shotgun scattering projectiles across wide cones.",
                baseStatsSummary = "ATK <color=#FFCB49>48.5</color> | 4 shells\n<color=#FFCB49>Slow</color> ATK Speed",
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
                count = 501,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Automated deployable turret firing at closest hostiles.",
                baseStatsSummary = "ATK <color=#FFCB49>27</color> | Duration 12s | CD 8.4s\n<color=#FFCB49>Fast</color> ATK Speed",
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
                count = 479,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Rebounding serrated blade ricocheting off dungeon walls.",
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
                tier = ChipTier.Rare,
                level = 9,
                count = 450,
                requiredCount = 7,
                enhanceCost = 650,
                description = "Heavy pump-action shotgun delivering point-blank destruction.",
                baseStatsSummary = "ATK <color=#FFCB49>178.02</color>\n<color=#FFCB49>Slow</color> ATK Speed",
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
                count = 391,
                requiredCount = 3,
                enhanceCost = 500,
                hasStar = true,
                description = "Siphons biological essence from wounded enemies to heal player.",
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
                count = 390,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Drops high-yield proximity charges detonating upon enemy contact.",
                baseStatsSummary = "Mine AoE ATK <color=#FFCB49>27</color>\nCooldown: 5.55s",
                magicBonus = "ATK +20%",
                rareBonus = "Cooldown -20%",
                uniqueBonus = "ATK +55%",
                epicBonus = "ATK +144%"
            },
            // 11. Sonic Boom (Inv Row 1 Col 2)
            new ChipItemData
            {
                id = 11,
                chipName = "Sonic Boom",
                iconKey = "sonic-boom",
                tier = ChipTier.Magic,
                level = 1,
                count = 513,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Emits concussive sonic waves pushing enemies back.",
                baseStatsSummary = "Knockback: <color=#FFCB49>180%</color> | CD: 6.2s\nStuns for 0.6s",
                magicBonus = "AoE Range +15%",
                rareBonus = "Cooldown -20%",
                uniqueBonus = "Stun Duration +0.4s",
                epicBonus = "Knockback +100%"
            },
            // 12. Healing Turret (Inv Row 1 Col 3)
            new ChipItemData
            {
                id = 12,
                chipName = "Healing Turret",
                iconKey = "healing-turret",
                tier = ChipTier.Magic,
                level = 1,
                count = 502,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Medical beacon emitting regenerative restorative pulses.",
                baseStatsSummary = "Heal: <color=#FFCB49>2 HP/s</color> | Duration 12s | CD 11s",
                magicBonus = "Turret Duration +20%",
                rareBonus = "Turret Range +37%",
                uniqueBonus = "Turret Range +60%",
                epicBonus = "Turret Duration +30%"
            },
            // 13. Aiming Lens (Inv Row 1 Col 4)
            new ChipItemData
            {
                id = 13,
                chipName = "Aiming Lens",
                iconKey = "aiming-lens",
                tier = ChipTier.Magic,
                level = 1,
                count = 498,
                requiredCount = 3,
                enhanceCost = 500,
                hasStar = true,
                description = "Tactical scope magnifying weak points and critical precision.",
                baseStatsSummary = "CRIT Rate <color=#FFCB49>+8%</color>\nCRIT DMG <color=#FFCB49>+25%</color>",
                magicBonus = "CRIT Rate +4%",
                rareBonus = "CRIT DMG +15%",
                uniqueBonus = "CRIT Rate +6%",
                epicBonus = "CRIT DMG +40%"
            },
            // 14. Ice Turret (Inv Row 2 Col 2)
            new ChipItemData
            {
                id = 14,
                chipName = "Ice Turret",
                iconKey = "ice-turret",
                tier = ChipTier.Magic,
                level = 1,
                count = 494,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Projects cryogenic bursts freezing and slowing monsters.",
                baseStatsSummary = "Freeze: 1s | Duration: 11s | CD: 11s\n<color=#FFCB49>Slow</color> ATK Speed",
                magicBonus = "AoE ATK Range +10%",
                rareBonus = "Cold Wave Duration +15%",
                uniqueBonus = "AoE ATK Range +10%",
                epicBonus = "Cold Wave Duration +30%"
            },
            // 15. Flamethrower (Inv Row 2 Col 4)
            new ChipItemData
            {
                id = 15,
                chipName = "Flamethrower",
                iconKey = "flamethrower",
                tier = ChipTier.Epic,
                level = 18,
                count = 486,
                requiredCount = 15,
                enhanceCost = 1200,
                description = "Continuous napalm jet incinerating monsters in a forward stream.",
                baseStatsSummary = "ATK <color=#FFCB49>102.46/s</color> (3s duration)\n<color=#FFCB49>Normal</color> ATK Speed",
                magicBonus = "AoE ATK Range +25%",
                rareBonus = "ATK +15%",
                uniqueBonus = "AoE ATK Range +25%",
                epicBonus = "ATK +100%"
            },
            // 16. ATK Module (Inv Row 3 Col 1)
            new ChipItemData
            {
                id = 16,
                chipName = "ATK Module",
                iconKey = "atk-module",
                tier = ChipTier.Magic,
                level = 1,
                count = 483,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Reinforced weapon servo amplifying base attack power across all weapons.",
                baseStatsSummary = "Overall ATK <color=#FFCB49>+5%</color>",
                magicBonus = "ATK +5%",
                rareBonus = "ATK +5%",
                uniqueBonus = "ATK +10%",
                epicBonus = "ATK +20%"
            },
            // 17. Laser Eye (Inv Row 3 Col 2)
            new ChipItemData
            {
                id = 17,
                chipName = "Laser Eye",
                iconKey = "laser-eye",
                tier = ChipTier.Magic,
                level = 1,
                count = 473,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Orbital targeting laser sweeping high-heat beams through lines of hostiles.",
                baseStatsSummary = "Laser ATK <color=#FFCB49>36/s</color> | CD: 4.8s",
                magicBonus = "Laser Width +20%",
                rareBonus = "ATK +15%",
                uniqueBonus = "Laser Duration +1s",
                epicBonus = "Dual Lasers"
            },
            // 18. Black Hole Mine (Inv Row 3 Col 3)
            new ChipItemData
            {
                id = 18,
                chipName = "Black Hole Mine",
                iconKey = "black-hole-mine",
                tier = ChipTier.Magic,
                level = 1,
                count = 467,
                requiredCount = 3,
                enhanceCost = 500,
                hasStar = true,
                description = "Implosive vortex pulling nearby targets inward and crushing them.",
                baseStatsSummary = "Vortex Radius: <color=#FFCB49>4.5m</color> | CD: 8.0s\nSlows targets by 60%",
                magicBonus = "Pull Radius +15%",
                rareBonus = "Vortex Duration +1.2s",
                uniqueBonus = "Pull Force +40%",
                epicBonus = "Creates Secondary Explosion"
            },
            // 19. Invincible Shield (Inv Row 3 Col 4)
            new ChipItemData
            {
                id = 19,
                chipName = "Invincible Shield",
                iconKey = "invincible-shield",
                tier = ChipTier.Magic,
                level = 1,
                count = 458,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Triggers temporary impenetrable invulnerability bubble.",
                baseStatsSummary = "Invincibility: <color=#FFCB49>2.3s</color> | CD: 35s\nRemoves all status debuffs",
                magicBonus = "Duration +10%",
                rareBonus = "Cooldown -10%",
                uniqueBonus = "Duration +9%",
                epicBonus = "Cooldown -9%"
            },
            // 20. Big Battery
            new ChipItemData
            {
                id = 20,
                chipName = "Big Battery",
                iconKey = "big-battery",
                tier = ChipTier.Magic,
                level = 1,
                count = 450,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Extended power capacitor reducing cooldowns across all equipped chips.",
                baseStatsSummary = "Cooldown Reduction <color=#FFCB49>4%</color>",
                magicBonus = "All Skills CD -2%",
                rareBonus = "All Skills CD -3%",
                uniqueBonus = "All Skills CD -4%",
                epicBonus = "All Skills CD -5%"
            },
            // 21. Plasma Field
            new ChipItemData
            {
                id = 21,
                chipName = "Plasma Field",
                iconKey = "plasma-field",
                tier = ChipTier.Magic,
                level = 1,
                count = 440,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Static aura generating periodic ionized electrical discharge.",
                baseStatsSummary = "Shock DMG: <color=#FFCB49>18</color> every 1.5s",
                magicBonus = "Shock Radius +15%",
                rareBonus = "Shock Frequency +20%",
                uniqueBonus = "Chains to 2 additional targets",
                epicBonus = "Shock DMG +100%"
            },
            // 22. Biochemical Mine
            new ChipItemData
            {
                id = 22,
                chipName = "Biochemical Mine",
                iconKey = "biochemical-mine",
                tier = ChipTier.Magic,
                level = 1,
                count = 430,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Toxic canister leaving corrosive gas clouds that poison enemies.",
                baseStatsSummary = "Gas Poison: <color=#FFCB49>12 DMG/s</color> for 5s",
                magicBonus = "Gas Radius +20%",
                rareBonus = "Gas Duration +2s",
                uniqueBonus = "Poison Tick +50%",
                epicBonus = "Reduces Enemy Defense by 30%"
            },
            // 23. Tesla Coil
            new ChipItemData
            {
                id = 23,
                chipName = "Tesla Coil",
                iconKey = "tesla-coil",
                tier = ChipTier.Magic,
                level = 1,
                count = 420,
                requiredCount = 3,
                enhanceCost = 500,
                description = "High-voltage pylon discharging chained lightning through dense crowds.",
                baseStatsSummary = "Chain ATK <color=#FFCB49>32</color> | 3 Targets",
                magicBonus = "Chain Targets +1",
                rareBonus = "Lightning Damage +20%",
                uniqueBonus = "Chain Range +30%",
                epicBonus = "Forks to 6 Targets"
            },
            // 24. Turret Module
            new ChipItemData
            {
                id = 24,
                chipName = "Turret Module",
                iconKey = "turret-module",
                tier = ChipTier.Magic,
                level = 1,
                count = 410,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Specialized hardware unit enhancing all deployed turrets.",
                baseStatsSummary = "Turret ATK +10%\nTurret HP +15%",
                magicBonus = "Turret ATK +5%",
                rareBonus = "Turret Cooldown -10%",
                uniqueBonus = "Turret ATK Speed +10%",
                epicBonus = "Turret ATK Speed +25%"
            }
        };
    }

    private void InitializeDefaultDecks()
    {
        // Preset 3 equipped chips (Slots 1 to 10 matching user screenshot):
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
        deckEquippedIds[2] = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Presets 1 & 2
        deckEquippedIds[0] = new int[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        deckEquippedIds[1] = new int[] { 21, 22, 23, 24, 1, 2, 3, 4, 5, 6 };
    }

    private void SetupEventListeners()
    {
        if (preset1Btn != null) preset1Btn.onClick.AddListener(() => SwitchDeck(0));
        if (preset2Btn != null) preset2Btn.onClick.AddListener(() => SwitchDeck(1));
        if (preset3Btn != null) preset3Btn.onClick.AddListener(() => SwitchDeck(2));

        if (byTierBtn != null) byTierBtn.onClick.AddListener(() => SetSortMode(false));
        if (byQuantityBtn != null) byQuantityBtn.onClick.AddListener(() => SetSortMode(true));

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

    public void SwitchDeck(int deckIndex)
    {
        activeDeckIndex = deckIndex;
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

    private void RefreshPresetButtons()
    {
        if (preset1Bg != null) preset1Bg.color = activeDeckIndex == 0 ? SelectedPresetColor : NormalPresetColor;
        if (preset2Bg != null) preset2Bg.color = activeDeckIndex == 1 ? SelectedPresetColor : NormalPresetColor;
        if (preset3Bg != null) preset3Bg.color = activeDeckIndex == 2 ? SelectedPresetColor : NormalPresetColor;

        if (preset1Text != null) preset1Text.color = activeDeckIndex == 0 ? SelectedPresetTextColor : NormalPresetTextColor;
        if (preset2Text != null) preset2Text.color = activeDeckIndex == 1 ? SelectedPresetTextColor : NormalPresetTextColor;
        if (preset3Text != null) preset3Text.color = activeDeckIndex == 2 ? SelectedPresetTextColor : NormalPresetTextColor;
    }

    private void RefreshSortButtons()
    {
        if (byQuantityBg != null) byQuantityBg.color = sortByQuantity ? SelectedPresetColor : NormalPresetColor;
        if (byTierBg != null) byTierBg.color = !sortByQuantity ? SelectedPresetColor : NormalPresetColor;

        if (byQuantityText != null) byQuantityText.color = sortByQuantity ? SelectedPresetTextColor : NormalPresetTextColor;
        if (byTierText != null) byTierText.color = !sortByQuantity ? SelectedPresetTextColor : NormalPresetTextColor;
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

    public void OpenDetailModal(ChipItemData chip)
    {
        if (chip == null || detailModal == null) return;
        selectedDetailChip = chip;
        RefreshDetailModal();
        detailModal.SetActive(true);
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
        }

        // 3. Name & Tier Subtitle
        if (detailNameText != null) detailNameText.text = selectedDetailChip.chipName;
        if (detailTierText != null)
        {
            detailTierText.text = selectedDetailChip.tier == ChipTier.Magic ? "Common" : selectedDetailChip.tier.ToString();
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
            bool isUnlocked = (int)selectedDetailChip.tier > i;
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
            detailEnhanceCostText.text = $"{selectedDetailChip.enhanceCost}";
        }
        if (detailEnhanceBtn != null)
        {
            detailEnhanceBtn.interactable = selectedDetailChip.CanEnhance;
        }

        // 7. Advance Tier Button
        if (detailAdvanceTierText != null)
        {
            detailAdvanceTierText.text = selectedDetailChip.requiredCount > 0
                ? $"Advance Tier ({selectedDetailChip.count}/{selectedDetailChip.requiredCount})"
                : "MAX TIER";
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
            ShowToast("Not enough Data Chips to enhance!");
            return;
        }

        if (selectedDetailChip.Enhance())
        {
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
        if (selectedDetailChip.NeedsAdvanceStones && !ChipManager.HasEnoughAdvanceStones(10))
        {
            ShowToast("Need 10 Advance Stones to Breakthrough Tier 5 (LV.24)!");
            return;
        }
        if (!selectedDetailChip.CanAdvanceTier)
        {
            ShowToast("Not enough fragments to advance tier!");
            return;
        }

        if (selectedDetailChip.AdvanceTier())
        {
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            ShowToast($"Advanced {selectedDetailChip.chipName} to {selectedDetailChip.tier.ToString().ToUpper()} Tier!");
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

    private Sprite GetIconSprite(string key)
    {
        if (chipIcons == null || chipIcons.Length == 0) return null;
        if (string.IsNullOrEmpty(key)) return chipIcons[0];

        string cleanKey = key.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();

        // 1. Khớp chính xác tên sprite
        Sprite match = chipIcons.FirstOrDefault(s => s != null && (
            string.Equals(s.name, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant(), cleanKey)
        ));
        if (match != null) return match;

        // 2. Ánh xạ số cho icon chipset (1..10)
        string numKey = null;
        if (cleanKey.Contains("highexplosive") || cleanKey.Contains("mine") && !cleanKey.Contains("blackhole") && !cleanKey.Contains("biochemical")) numKey = "1";
        else if (cleanKey.Contains("energyjumper") || cleanKey.Contains("jumpercable")) numKey = "2";
        else if (cleanKey.Contains("shotgun")) numKey = "3";
        else if (cleanKey.Contains("spiky") || cleanKey.Contains("discus") || cleanKey.Contains("spicky")) numKey = "4";
        else if (cleanKey.Contains("gunturret") || cleanKey.Equals("turret")) numKey = "5";
        else if (cleanKey.Contains("multigun")) numKey = "6";
        else if (cleanKey.Contains("spinningblade") || cleanKey.Contains("blade")) numKey = "7";
        else if (cleanKey.Contains("rocketpunch") || cleanKey.Contains("punch")) numKey = "8";
        else if (cleanKey.Contains("standardgun") || cleanKey.Equals("gun") || cleanKey.Equals("pistol")) numKey = "9";
        else if (cleanKey.Contains("rifle") || cleanKey.Contains("assault")) numKey = "10";

        if (!string.IsNullOrEmpty(numKey))
        {
            match = chipIcons.FirstOrDefault(s => s != null && s.name == numKey);
            if (match != null) return match;
        }

        return chipIcons[0];
    }

    private Sprite GetFrameSprite(ChipTier tier)
    {
        if (frameSprites == null || frameSprites.Length == 0) return null;
        switch (tier)
        {
            case ChipTier.Magic:
                return frameSprites.Length > 0 ? frameSprites[0] : null; // Green
            case ChipTier.Rare:
                return frameSprites.Length > 1 ? frameSprites[1] : frameSprites[0]; // Blu
            case ChipTier.Unique:
                return frameSprites.Length > 2 ? frameSprites[2] : (frameSprites.Length > 1 ? frameSprites[1] : frameSprites[0]); // Tím
            case ChipTier.Epic:
                return frameSprites.Length > 3 ? frameSprites[3] : (frameSprites.Length > 2 ? frameSprites[2] : frameSprites[0]); // Yello
            case ChipTier.Holographic:
                return frameSprites.Length > 4 ? frameSprites[4] : (frameSprites.Length > 3 ? frameSprites[3] : frameSprites[0]); // Red / Holographic
            default:
                return frameSprites[0];
        }
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
