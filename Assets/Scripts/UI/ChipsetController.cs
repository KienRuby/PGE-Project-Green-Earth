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
    public bool hasStar;

    [Header("Stats Description")]
    [TextArea(2, 4)]
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

    public bool CanUpgrade => !IsAtTierCap && count >= requiredCount && requiredCount > 0;
    public bool CanAdvanceTier => IsAtTierCap && tier < ChipTier.Holographic;

    public void Upgrade()
    {
        if (!CanUpgrade) return;
        count -= requiredCount;
        level++;
        if (level >= MaxLevel)
        {
            // Reached tier cap
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
            hasStar = this.hasStar,
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

    [Header("Detail Modal")]
    [SerializeField] private GameObject detailModal;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailLevelText;
    [SerializeField] private TMP_Text detailTierText;
    [SerializeField] private TMP_Text detailBaseStatsText;
    [SerializeField] private TMP_Text detailMagicText;
    [SerializeField] private TMP_Text detailRareText;
    [SerializeField] private TMP_Text detailUniqueText;
    [SerializeField] private TMP_Text detailEpicText;
    [SerializeField] private Button detailUpgradeBtn;
    [SerializeField] private TMP_Text detailUpgradeBtnText;
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

        allChips = new List<ChipItemData>
        {
            // 1. Standard Gun
            new ChipItemData
            {
                id = 1,
                chipName = "Standard Gun",
                iconKey = "standard-gun",
                tier = ChipTier.Magic,
                level = 1,
                count = 22,
                requiredCount = 3,
                baseStatsSummary = "ATK 42 | Tốc độ đánh: Fast",
                magicBonus = "Magic: ATK +15%",
                rareBonus = "Rare: ATK Speed +15%",
                uniqueBonus = "Unique: +5% Life Steal (Hút máu)",
                epicBonus = "Epic: Adds Penetration Skill (Bắn xuyên mục tiêu)"
            },
            // 2. Rifle
            new ChipItemData
            {
                id = 2,
                chipName = "Rifle",
                iconKey = "rifle",
                tier = ChipTier.Holographic,
                level = 24,
                count = 20,
                requiredCount = 0,
                baseStatsSummary = "ATK 47.36 | Tốc độ đánh: Very fast",
                magicBonus = "Magic: ATK +25%",
                rareBonus = "Rare: ATK Speed +20%",
                uniqueBonus = "Unique: ATK +80%",
                epicBonus = "Epic: ATK Speed +35%"
            },
            // 3. Rocket Punch
            new ChipItemData
            {
                id = 3,
                chipName = "Rocket Punch",
                iconKey = "rocket-punch",
                tier = ChipTier.Magic,
                level = 1,
                count = 50,
                requiredCount = 3,
                baseStatsSummary = "ATK 50 / AoE ATK 27 | Tốc độ đánh: Slow",
                magicBonus = "Magic: ATK +40%",
                rareBonus = "Rare: ATK Speed +40%",
                uniqueBonus = "Unique: AoE ATK Range +40% (Tăng phạm vi nổ)",
                epicBonus = "Epic: ATK +180%"
            },
            // 4. Spinning Blade
            new ChipItemData
            {
                id = 4,
                chipName = "Spinning Blade",
                iconKey = "spinning-blade",
                tier = ChipTier.Epic,
                level = 14,
                count = 31,
                requiredCount = 9,
                baseStatsSummary = "ATK 82.8 | Tốc độ đánh: Fast",
                magicBonus = "Magic: ATK Speed +9%",
                rareBonus = "Rare: ATK Speed +18%",
                uniqueBonus = "Unique: Spin Speed +36% (Tăng tốc độ quay)",
                epicBonus = "Epic: ATK Speed +36%"
            },
            // 5. Multigun
            new ChipItemData
            {
                id = 5,
                chipName = "Multigun",
                iconKey = "multigun",
                tier = ChipTier.Magic,
                level = 6,
                count = 37,
                requiredCount = 3,
                baseStatsSummary = "ATK 28.5 | Tốc độ đánh: Slow | Số lượng đạn: 3 shells",
                magicBonus = "Magic: Adds +1 shells (+1 viên đạn)",
                rareBonus = "Rare: Adds +1 shells (+1 viên đạn)",
                uniqueBonus = "Unique: Adds +3 shells (+3 viên đạn)",
                epicBonus = "Epic: Adds +4 shells (+4 viên đạn)"
            },
            // 6. Gun Turret
            new ChipItemData
            {
                id = 6,
                chipName = "Gun Turret",
                iconKey = "gun-turret",
                tier = ChipTier.Magic,
                level = 1,
                count = 49,
                requiredCount = 3,
                baseStatsSummary = "ATK 27 | Tốc độ đánh: Fast | Thời gian tồn tại: 12s | Hồi chiêu: 8.4s",
                magicBonus = "Magic: Turret Duration +20% (Tăng thời gian tồn tại)",
                rareBonus = "Rare: Turret Cooldown -30% (Giảm hồi chiêu)",
                uniqueBonus = "Unique: Turret Duration +20%",
                epicBonus = "Epic: Turret Duration +30%"
            },
            // 7. Spiky Discus
            new ChipItemData
            {
                id = 7,
                chipName = "Spiky Discus",
                iconKey = "spiky-discus",
                tier = ChipTier.Magic,
                level = 1,
                count = 30,
                requiredCount = 3,
                baseStatsSummary = "ATK 30 | Tốc độ quay: Normal Spin Speed",
                magicBonus = "Magic: +1 Discus (+1 đĩa quay)",
                rareBonus = "Rare: Spin Speed +30% (Tăng tốc độ xoay)",
                uniqueBonus = "Unique: +1 Discus (+1 đĩa quay)",
                epicBonus = "Epic: Spin Speed +35%"
            },
            // 8. Shotgun
            new ChipItemData
            {
                id = 8,
                chipName = "Shotgun",
                iconKey = "shotgun",
                tier = ChipTier.Rare,
                level = 9,
                count = 49,
                requiredCount = 7,
                baseStatsSummary = "ATK 178.02 | Tốc độ đánh: Slow",
                magicBonus = "Magic: ATK +15%",
                rareBonus = "Rare: ATK +15%",
                uniqueBonus = "Unique: Adds Penetration Skill (Bắn xuyên mục tiêu)",
                epicBonus = "Epic: Fires two times in a row (Bắn liên tiếp 2 lần)"
            },
            // 9. Energy Jumper Cables
            new ChipItemData
            {
                id = 9,
                chipName = "Energy Jumper Cables",
                iconKey = "energy-jumper-cables",
                tier = ChipTier.Magic,
                level = 1,
                count = 38,
                requiredCount = 3,
                hasStar = true,
                baseStatsSummary = "Life Steal 2.3%",
                magicBonus = "Magic: All Weapons' +1% Life Steal (Mọi vũ khí +1% hút máu)",
                rareBonus = "Rare: All Weapons' +1% Life Steal",
                uniqueBonus = "Unique: All Weapons' +1% Life Steal",
                epicBonus = "Epic: All Weapons' +2% Life Steal"
            },
            // 10. High-Explosive Mine
            new ChipItemData
            {
                id = 10,
                chipName = "High-Explosive Mine",
                iconKey = "high-explosive-mine",
                tier = ChipTier.Magic,
                level = 1,
                count = 24,
                requiredCount = 3,
                baseStatsSummary = "Mine AoE ATK 27 | Hồi chiêu: 5.55s",
                magicBonus = "Magic: ATK +20%",
                rareBonus = "Rare: Cooldown -20% (Giảm hồi chiêu)",
                uniqueBonus = "Unique: ATK +55%",
                epicBonus = "Epic: ATK +144%"
            },
            // 11. Aiming Lens
            new ChipItemData
            {
                id = 11,
                chipName = "Aiming Lens",
                iconKey = "aiming-lens",
                tier = ChipTier.Magic,
                level = 1,
                count = 63,
                requiredCount = 3,
                hasStar = true,
                baseStatsSummary = "CRIT Rate +4% (Tỷ lệ chí mạng)",
                magicBonus = "Magic: All Weapons' CRIT Rate +3% (Mọi vũ khí +3% tỷ lệ chí mạng)",
                rareBonus = "Rare: All Weapons' CRIT Rate +3%",
                uniqueBonus = "Unique: All Weapons' CRIT Rate +4%",
                epicBonus = "Epic: All Weapons' CRIT Rate +5%"
            },
            // 12. Plasma Field
            new ChipItemData
            {
                id = 12,
                chipName = "Plasma Field",
                iconKey = "plasma-field",
                tier = ChipTier.Magic,
                level = 1,
                count = 52,
                requiredCount = 3,
                baseStatsSummary = "ATK 40/giây (kéo dài 3s) | Hồi chiêu: 7.5s | Thời gian tồn tại: 4.2s",
                magicBonus = "Magic: AoE ATK Range +25% (Tăng phạm vi ảnh hưởng)",
                rareBonus = "Rare: ATK Speed +10% (Tốc độ đánh)",
                uniqueBonus = "Unique: AoE ATK Range +35%",
                epicBonus = "Epic: ATK Speed +20%"
            },
            // 13. Laser Eye
            new ChipItemData
            {
                id = 13,
                chipName = "Laser Eye",
                iconKey = "laser-eye",
                tier = ChipTier.Magic,
                level = 1,
                count = 58,
                requiredCount = 3,
                baseStatsSummary = "ATK 5 | Tốc độ đánh: Very fast",
                magicBonus = "Magic: ATK +15%",
                rareBonus = "Rare: CRIT Rate +10% (Tỷ lệ chí mạng)",
                uniqueBonus = "Unique: ATK +15%",
                epicBonus = "Epic: ATK +100%"
            },
            // 14. Biochemical Mine
            new ChipItemData
            {
                id = 14,
                chipName = "Biochemical Mine",
                iconKey = "biochemical-mine",
                tier = ChipTier.Magic,
                level = 1,
                count = 48,
                requiredCount = 3,
                baseStatsSummary = "Khí độc ATK 14/giây | Thời gian tồn tại khí: 3s | Hồi chiêu: 7.7s",
                magicBonus = "Magic: AoE ATK Range +40% (Tăng phạm vi nổ)",
                rareBonus = "Rare: Cooldown -30% (Giảm thời gian hồi chiêu)",
                uniqueBonus = "Unique: ATK +77%",
                epicBonus = "Epic: ATK +144%"
            },
            // 15. Tesla Coil
            new ChipItemData
            {
                id = 15,
                chipName = "Tesla Coil",
                iconKey = "tesla-coil",
                tier = ChipTier.Unique,
                level = 14,
                count = 19,
                requiredCount = 15,
                baseStatsSummary = "ATK 86 | Tốc độ đánh: Slow | Số mục tiêu tấn công: 1",
                magicBonus = "Magic: Enemies Attacked: +1 (+1 mục tiêu bị giật điện)",
                rareBonus = "Rare: ATK Speed +20%",
                uniqueBonus = "Unique: Enemies Attacked: +1",
                epicBonus = "Epic: ATK +100%"
            },
            // 16. ATK Module
            new ChipItemData
            {
                id = 16,
                chipName = "ATK Module",
                iconKey = "atk-module",
                tier = ChipTier.Magic,
                level = 1,
                count = 35,
                requiredCount = 3,
                baseStatsSummary = "All Weapon ATK +19.6% (Tăng ATK toàn bộ vũ khí)",
                magicBonus = "Magic: All Weapons' ATK +7%",
                rareBonus = "Rare: All Weapons' ATK +8%",
                uniqueBonus = "Unique: All Weapons' ATK +9%",
                epicBonus = "Epic: All Weapons' ATK +10%"
            },
            // 17. Black Hole Mine
            new ChipItemData
            {
                id = 17,
                chipName = "Black Hole Mine",
                iconKey = "black-hole-mine",
                tier = ChipTier.Magic,
                level = 1,
                count = 42,
                requiredCount = 3,
                baseStatsSummary = "Mine AoE ATK 15 | Thời gian hút của hố đen: 1.5s | Hồi chiêu: 9.7s",
                magicBonus = "Magic: AoE ATK Range +10% (Tăng phạm vi hút)",
                rareBonus = "Rare: Cooldown -10% (Giảm hồi chiêu)",
                uniqueBonus = "Unique: Black Hole Duration +20% (Tăng thời gian hố đen tồn tại)",
                epicBonus = "Epic: Black Hole Duration +30%"
            },
            // 18. Sonic Boom
            new ChipItemData
            {
                id = 18,
                chipName = "Sonic Boom",
                iconKey = "sonic-boom",
                tier = ChipTier.Magic,
                level = 1,
                count = 28,
                requiredCount = 3,
                baseStatsSummary = "ATK 33 | Tốc độ đánh: Very slow",
                magicBonus = "Magic: ATK +15%",
                rareBonus = "Rare: AoE ATK Range +15% (Tăng phạm vi sóng âm)",
                uniqueBonus = "Unique: ATK +30%",
                epicBonus = "Epic: AoE ATK Range +35%"
            },
            // 19. Big Battery
            new ChipItemData
            {
                id = 19,
                chipName = "Big Battery",
                iconKey = "big-battery",
                tier = ChipTier.Magic,
                level = 1,
                count = 34,
                requiredCount = 3,
                baseStatsSummary = "HP +10% (Máu tối đa)",
                magicBonus = "Magic: HP +15%",
                rareBonus = "Rare: HP +20%",
                uniqueBonus = "Unique: HP +25%",
                epicBonus = "Epic: HP +40%"
            },
            // 20. Turret Module
            new ChipItemData
            {
                id = 20,
                chipName = "Turret Module",
                iconKey = "turret-module",
                tier = ChipTier.Magic,
                level = 1,
                count = 29,
                requiredCount = 3,
                baseStatsSummary = "All Turret Cooldown -7% (Giảm hồi chiêu toàn bộ trụ súng)",
                magicBonus = "Magic: Turret ATK Speed +5% (Tăng tốc bắn của trụ)",
                rareBonus = "Rare: Turret ATK Speed +10%",
                uniqueBonus = "Unique: Turret ATK Speed +10%",
                epicBonus = "Epic: Turret ATK Speed +25%"
            },
            // 21. Ice Turret
            new ChipItemData
            {
                id = 21,
                chipName = "Ice Turret",
                iconKey = "ice-turret",
                tier = ChipTier.Magic,
                level = 1,
                count = 31,
                requiredCount = 3,
                baseStatsSummary = "Thời gian sóng lạnh: 1s | Tốc độ đánh: Slow | Thời gian tồn tại: 11s | Hồi chiêu: 11s",
                magicBonus = "Magic: AoE ATK Range +10% (Tăng phạm vi ảnh hưởng)",
                rareBonus = "Rare: Cold Wave Duration +15% (Tăng thời gian làm chậm)",
                uniqueBonus = "Unique: AoE ATK Range +10%",
                epicBonus = "Epic: Cold Wave Duration +30%"
            },
            // 22. Invincible Shield
            new ChipItemData
            {
                id = 22,
                chipName = "Invincible Shield",
                iconKey = "invincible-shield",
                tier = ChipTier.Magic,
                level = 1,
                count = 26,
                requiredCount = 3,
                baseStatsSummary = "Thời gian bất tử: 2.3s | Hồi chiêu: 35s (Xóa toàn bộ hiệu ứng bất lợi khi kích hoạt)",
                magicBonus = "Magic: Duration +10% (Tăng thời gian bất tử)",
                rareBonus = "Rare: Cooldown -10% (Giảm hồi chiêu)",
                uniqueBonus = "Unique: Duration +9%",
                epicBonus = "Epic: Cooldown -9%"
            },
            // 23. Healing Turret
            new ChipItemData
            {
                id = 23,
                chipName = "Healing Turret",
                iconKey = "healing-turret",
                tier = ChipTier.Magic,
                level = 1,
                count = 33,
                requiredCount = 3,
                baseStatsSummary = "Hồi phục: 2 HP/giây | Thời gian tồn tại: 12s | Hồi chiêu: 11s",
                magicBonus = "Magic: Turret Duration +20% (Tăng thời gian tồn tại)",
                rareBonus = "Rare: Turret Range +37% (Tăng phạm vi hồi máu)",
                uniqueBonus = "Unique: Turret Range +60%",
                epicBonus = "Epic: Turret Duration +30%"
            },
            // 24. Flamethrower
            new ChipItemData
            {
                id = 24,
                chipName = "Flamethrower",
                iconKey = "flamethrower",
                tier = ChipTier.Magic,
                level = 1,
                count = 45,
                requiredCount = 3,
                baseStatsSummary = "ATK 102.46/giây (kéo dài 3s) | Tốc độ đánh: Normal",
                magicBonus = "Magic: AoE ATK Range +25% (Tăng tầm phun lửa)",
                rareBonus = "Rare: ATK +15%",
                uniqueBonus = "Unique: AoE ATK Range +25%",
                epicBonus = "Epic: ATK +100%"
            }
        };

        // Preset 3 equipped chips (Slots 1 to 10)
        deckEquippedIds[2] = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        // Preset 1 & 2
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
        if (detailUpgradeBtn != null) detailUpgradeBtn.onClick.AddListener(OnDetailActionButtonClicked);
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
        for (int i = 0; i < 10; i++)
        {
            if (i >= equippedSlots.Length || equippedSlots[i] == null) continue;

            int chipId = (currentDeck != null && i < currentDeck.Length) ? currentDeck[i] : -1;
            ChipItemData chip = allChips.FirstOrDefault(c => c.id == chipId);
            if (chip != null)
            {
                Sprite icon = GetIconSprite(chip.iconKey);
                Sprite frame = GetFrameSprite(chip.tier);
                equippedSlots[i].Setup(chip, icon, frame, OpenDetailModal, HandleCardAction);
                equippedSlots[i].gameObject.SetActive(true);
            }
            else
            {
                equippedSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void RefreshInventory()
    {
        if (inventoryContent == null || cardPrefab == null) return;

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
                GameObject obj = Instantiate(cardPrefab, inventoryContent);
                card = obj.GetComponent<ChipsetCardUI>();
                spawnedInventoryCards.Add(card);
            }

            ChipItemData data = sortedList[i];
            Sprite icon = GetIconSprite(data.iconKey);
            Sprite frame = GetFrameSprite(data.tier);
            card.Setup(data, icon, frame, OpenDetailModal, HandleCardAction);
            card.gameObject.SetActive(true);
        }

        for (int i = sortedList.Count; i < spawnedInventoryCards.Count; i++)
        {
            spawnedInventoryCards[i].gameObject.SetActive(false);
        }
    }

    public void HandleCardAction(ChipItemData chip)
    {
        if (chip == null) return;

        if (chip.CanAdvanceTier)
        {
            if (chip.NeedsAdvanceStones && !ChipManager.HasEnoughAdvanceStones(10))
            {
                ShowToast("Need 10 Advance Stones to Breakthrough Tier 5 (LV.24)!");
                return;
            }

            bool success = chip.AdvanceTier();
            if (success)
            {
                RefreshTopBar();
                RefreshEquippedGrid();
                RefreshInventory();
                if (detailModal != null && detailModal.activeSelf && selectedDetailChip == chip)
                {
                    RefreshDetailModal();
                }
                ShowToast($"ADVANCED {chip.chipName} to {chip.tier.ToString().ToUpper()} (Max LV.{chip.MaxLevel:00})!");
            }
            return;
        }

        if (chip.CanUpgrade)
        {
            chip.Upgrade();
            ChipManager.AddDataChips(100);
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            if (detailModal != null && detailModal.activeSelf && selectedDetailChip == chip)
            {
                RefreshDetailModal();
            }
            ShowToast($"Upgraded {chip.chipName} to LV.{chip.level:00}!");
            return;
        }

        if (chip.IsMaxOverall)
        {
            ShowToast($"{chip.chipName} is already MAX LEVEL & TIER!");
        }
        else if (chip.IsAtTierCap)
        {
            ShowToast($"{chip.chipName} has reached Tier Cap (LV.{chip.MaxLevel:00})! Advance Tier to continue.");
        }
        else
        {
            ShowToast("Not enough chip fragments to upgrade!");
        }
    }

    public void OpenDetailModal(ChipItemData chip)
    {
        if (chip == null || detailModal == null) return;
        selectedDetailChip = chip;
        RefreshDetailModal();
        detailModal.SetActive(true);
    }

    private void RefreshDetailModal()
    {
        if (selectedDetailChip == null) return;

        if (detailIcon != null) detailIcon.sprite = GetIconSprite(selectedDetailChip.iconKey);
        if (detailNameText != null) detailNameText.text = selectedDetailChip.chipName;

        string levelInfo = selectedDetailChip.IsMaxOverall
            ? $"LEVEL {selectedDetailChip.level:00} [MAX LEVEL]"
            : (selectedDetailChip.IsAtTierCap
                ? $"LEVEL {selectedDetailChip.level:00}/{selectedDetailChip.MaxLevel:00} [TIER CAP]"
                : $"LEVEL {selectedDetailChip.level:00}/{selectedDetailChip.MaxLevel:00}");

        if (detailLevelText != null) detailLevelText.text = levelInfo;
        if (detailTierText != null) detailTierText.text = $"TIER {(int)selectedDetailChip.tier} ({selectedDetailChip.tier.ToString().ToUpper()})";
        if (detailBaseStatsText != null) detailBaseStatsText.text = $"• {selectedDetailChip.baseStatsSummary}";

        // Formatting Perks: Active perks colored bright cyan/gold, Locked perks greyed out with unlock conditions
        bool hasMagic = selectedDetailChip.tier >= ChipTier.Magic;
        bool hasRare = selectedDetailChip.tier >= ChipTier.Rare;
        bool hasUnique = selectedDetailChip.tier >= ChipTier.Unique;
        bool hasEpic = selectedDetailChip.tier >= ChipTier.Epic;

        if (detailMagicText != null)
        {
            detailMagicText.text = hasMagic
                ? $"<color=#40DAD2>• {selectedDetailChip.magicBonus} <color=#22C55E>[ACTIVE]</color></color>"
                : $"<color=#88A0A8>• {selectedDetailChip.magicBonus} [Unlock at Tier 1 - Magic]</color>";
        }

        if (detailRareText != null)
        {
            detailRareText.text = hasRare
                ? $"<color=#40DAD2>• {selectedDetailChip.rareBonus} <color=#22C55E>[ACTIVE]</color></color>"
                : $"<color=#88A0A8>• {selectedDetailChip.rareBonus} [Unlock at Tier 2 - Rare (LV.09)]</color>";
        }

        if (detailUniqueText != null)
        {
            detailUniqueText.text = hasUnique
                ? $"<color=#40DAD2>• {selectedDetailChip.uniqueBonus} <color=#22C55E>[ACTIVE]</color></color>"
                : $"<color=#88A0A8>• {selectedDetailChip.uniqueBonus} [Unlock at Tier 3 - Unique (LV.14)]</color>";
        }

        if (detailEpicText != null)
        {
            detailEpicText.text = hasEpic
                ? $"<color=#40DAD2>• {selectedDetailChip.epicBonus} <color=#22C55E>[ACTIVE]</color></color>"
                : $"<color=#88A0A8>• {selectedDetailChip.epicBonus} [Unlock at Tier 4 - Epic (LV.18)]</color>";
        }

        if (detailUpgradeBtn != null)
        {
            if (selectedDetailChip.IsMaxOverall)
            {
                detailUpgradeBtn.interactable = false;
                if (detailUpgradeBtnText != null) detailUpgradeBtnText.text = "MAX LEVEL (LV.24)";
            }
            else if (selectedDetailChip.CanAdvanceTier)
            {
                if (selectedDetailChip.NeedsAdvanceStones)
                {
                    bool hasStones = ChipManager.HasEnoughAdvanceStones(10);
                    detailUpgradeBtn.interactable = hasStones;
                    if (detailUpgradeBtnText != null)
                    {
                        detailUpgradeBtnText.text = hasStones
                            ? "ADVANCE TIER (10 STONES) -> LV.24"
                            : $"NEED 10 STONES ({ChipManager.AdvanceStones}/10)";
                    }
                }
                else
                {
                    detailUpgradeBtn.interactable = true;
                    int nextTierLevel = ChipItemData.GetMaxLevelForTier((ChipTier)((int)selectedDetailChip.tier + 1));
                    if (detailUpgradeBtnText != null)
                    {
                        detailUpgradeBtnText.text = $"ADVANCE TIER -> MAX LV.{nextTierLevel:00}";
                    }
                }
            }
            else
            {
                detailUpgradeBtn.interactable = selectedDetailChip.CanUpgrade;
                if (detailUpgradeBtnText != null)
                {
                    detailUpgradeBtnText.text = selectedDetailChip.requiredCount > 0
                        ? $"UPGRADE ({selectedDetailChip.count}/{selectedDetailChip.requiredCount})"
                        : "UPGRADE";
                }
            }
        }

        bool isEquipped = deckEquippedIds[activeDeckIndex].Contains(selectedDetailChip.id);
        if (detailEquipBtnText != null)
        {
            detailEquipBtnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
        }
    }

    private void OnDetailActionButtonClicked()
    {
        if (selectedDetailChip != null)
        {
            HandleCardAction(selectedDetailChip);
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
            if (emptyIndex < 0) emptyIndex = 0;
            currentDeck[emptyIndex] = selectedDetailChip.id;
            ShowToast($"Equipped {selectedDetailChip.chipName} to Slot {emptyIndex + 1}");
        }

        RefreshEquippedGrid();
        RefreshDetailModal();
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
        return chipIcons.FirstOrDefault(s => s != null && s.name.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? chipIcons[0];
    }

    private Sprite GetFrameSprite(ChipTier tier)
    {
        if (frameSprites == null || frameSprites.Length == 0) return null;
        switch (tier)
        {
            case ChipTier.Magic:
                return frameSprites.Length > 0 ? frameSprites[0] : null;
            case ChipTier.Rare:
                return frameSprites.Length > 1 ? frameSprites[1] : frameSprites[0];
            case ChipTier.Unique:
            case ChipTier.Epic:
                return frameSprites.Length > 2 ? frameSprites[2] : frameSprites[0];
            case ChipTier.Holographic:
                return frameSprites.Length > 3 ? frameSprites[3] : (frameSprites.Length > 2 ? frameSprites[2] : frameSprites[0]);
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
