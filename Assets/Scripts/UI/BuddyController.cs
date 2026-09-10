using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BuddyTier
{
    Common = 0,
    Magic = 1,
    Rare = 2,
    Unique = 3,
    Epic = 4,
    Holographic = 5
}

[Serializable]
public class BuddyItemData
{
    public int id;
    public string buddyName;
    public string iconKey;
    public BuddyTier tier = BuddyTier.Common;
    public int level = 1;
    public int count = 0;
    public int requiredCount = 3;
    public int enhanceCost = 500;
    public string description;
    public string baseStatText;
    public string magicPerkText;
    public string rarePerkText;
    public string uniquePerkText;
    public string epicPerkText;

    public bool CanEnhance => ChipManager.DataChips >= enhanceCost;
    public bool CanAdvanceTier => count >= requiredCount && requiredCount > 0;
    public bool CanUpgrade => CanEnhance || CanAdvanceTier;

    public bool Enhance()
    {
        if (ChipManager.DataChips < enhanceCost) return false;
        if (!ChipManager.TrySpendDataChips(enhanceCost)) return false;
        level++;
        enhanceCost = Mathf.RoundToInt(enhanceCost * 1.35f);
        return true;
    }

    public bool AdvanceTier()
    {
        if (!CanAdvanceTier) return false;
        count -= requiredCount;
        tier = (BuddyTier)Mathf.Min((int)tier + 1, (int)BuddyTier.Holographic);
        requiredCount = Mathf.RoundToInt(requiredCount * 1.6f) + 1;
        return true;
    }

    public BuddyItemData Clone()
    {
        return new BuddyItemData
        {
            id = this.id,
            buddyName = this.buddyName,
            iconKey = this.iconKey,
            tier = this.tier,
            level = this.level,
            count = this.count,
            requiredCount = this.requiredCount,
            enhanceCost = this.enhanceCost,
            description = this.description,
            baseStatText = this.baseStatText,
            magicPerkText = this.magicPerkText,
            rarePerkText = this.rarePerkText,
            uniquePerkText = this.uniquePerkText,
            epicPerkText = this.epicPerkText
        };
    }
}

public class BuddyController : MonoBehaviour
{
    [Header("Top Bar Currencies")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text chipCurrencyText;
    [SerializeField] private TMP_Text redCurrencyText;

    [Header("Top Mode Switcher")]
    [SerializeField] private Button droneModeBtn;
    [SerializeField] private Button robotPetModeBtn;
    [SerializeField] private Image droneModeBg;
    [SerializeField] private Image robotPetModeBg;

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

    [Header("Equipped Slots (3 Slots)")]
    [SerializeField] private BuddyCardUI[] equippedSlots = new BuddyCardUI[3];
    [SerializeField] private Transform slotIconBuddyContainer;
    [SerializeField] private List<BuddyCardUI> inventoryCards = new List<BuddyCardUI>();

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
    [SerializeField] private BuddyCardUI detailTopCard;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailTierText;
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private TMP_Text detailBaseStatText;

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

    [Header("Notices")]
    [SerializeField] private GameObject notEnoughFragmentsNotice;
    [SerializeField] private GameObject notEnoughChipsNotice;

    [Header("Toast Message")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    [Header("Sprites Database")]
    [SerializeField] private Sprite[] droneIcons;
    [SerializeField] private Sprite[] frameSprites;
    [SerializeField] private Sprite emptySlotFrameSprite;
    [SerializeField] private Sprite lockedSlotFrameSprite;
    [SerializeField] private Sprite upgradeArrowSprite;
    [SerializeField] private Sprite lockSlotSprite;
    [SerializeField] private Sprite[] lockTierSprites = new Sprite[4]; // 0: Magic, 1: Rare, 2: Unique, 3: Epic
    [SerializeField] private Sprite unlockedCheckSprite;

    private int activeDeckIndex = 0;
    private bool sortByQuantity = false;
    private BuddyItemData selectedDetailBuddy;
    private int openedFromEquippedSlotIndex = -1;

    [SerializeField] private List<BuddyItemData> allBuddies = new List<BuddyItemData>();
    private int[][] deckEquippedIds = new int[3][];
    private bool[] slotUnlocked = new bool[] { true, true, true };
    private List<BuddyCardUI> spawnedInventoryCards = new List<BuddyCardUI>();

    private static readonly Color SelectedPresetColor = new Color32(255, 203, 73, 255);
    private static readonly Color NormalPresetColor = new Color32(18, 58, 68, 255);
    private static readonly Color SelectedPresetTextColor = new Color32(10, 20, 30, 255);
    private static readonly Color NormalPresetTextColor = new Color32(245, 255, 255, 255);

    public IReadOnlyList<BuddyItemData> AllBuddies => allBuddies;

    private void Awake()
    {
        InitializeDatabase();
        AutoWireSlotIconBuddyIfMissing();
        AutoWireDetailModalReferencesIfMissing();
    }

    private void Start()
    {
        AutoWireSlotIconBuddyIfMissing();
        AutoWireDetailModalReferencesIfMissing();
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
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged -= HandleCurrencyChanged;
        ChipManager.OnEnergyChanged -= HandleCurrencyChanged;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                AutoWirePresetButtonsIfMissing();
                AutoWireSortButtonsIfMissing();
                AutoWireSlotIconBuddyIfMissing();
                AutoWireDetailModalReferencesIfMissing();
                InitializeDatabase();
                RefreshPresetButtons();
                RefreshSortButtons();
                RefreshEquippedGrid();
                RefreshInventory();
            };
        }
    }
#endif

    private void HandleCurrencyChanged(int _)
    {
        RefreshTopBar();
        if (detailModal != null && detailModal.activeSelf)
        {
            RefreshDetailModal();
        }
    }

    public void InitializeDatabase()
    {
        activeDeckIndex = PlayerDataService.ActiveBuddyDeckIndex;
        if (deckEquippedIds == null || deckEquippedIds.Length != 3)
        {
            deckEquippedIds = new int[3][];
        }
        for (int d = 0; d < 3; d++)
        {
            int[] loaded = PlayerDataService.LoadBuddyDeck(d, new int[] { -1, -1, -1 });
            if (loaded == null || loaded.Length != 3)
            {
                int[] fixedDeck = new int[3] { -1, -1, -1 };
                if (loaded != null)
                {
                    for (int i = 0; i < Mathf.Min(loaded.Length, 3); i++)
                    {
                        fixedDeck[i] = loaded[i];
                    }
                }
                loaded = fixedDeck;
            }
            deckEquippedIds[d] = loaded;
        }
        if (slotUnlocked == null || slotUnlocked.Length != 3)
        {
            slotUnlocked = new bool[] { true, true, true };
        }

        PlayerPrefs.DeleteKey("PGE.Buddy.1.Count");
        PlayerPrefs.DeleteKey("PGE.Buddy.1.RequiredCount");
        PlayerPrefs.Save();

        if (allBuddies.Count > 0)
        {
            foreach (var b in allBuddies)
            {
                if (b == null) continue;
                b.count = 0;
                b.requiredCount = 10;
                if (b.id == 1 || b.iconKey == "drone-snowflake")
                {
                    b.buddyName = "Sloy";
                    b.tier = BuddyTier.Common;
                    b.description = "Fires shells that slow down enemies.";
                    b.baseStatText = "Drone ATK 20.4, Slow ATK Speed";
                }
            }
            return;
        }

        allBuddies = new List<BuddyItemData>
        {
            // 1. Drone Snowflake (Sloy / Frost Sentinel)
            new BuddyItemData
            {
                id = 1,
                buddyName = "Sloy",
                iconKey = "drone-snowflake",
                tier = BuddyTier.Common,
                level = 8,
                count = 0,
                requiredCount = 10,
                enhanceCost = 3500,
                description = "Fires shells that slow down enemies.",
                baseStatText = "Drone ATK 20.4, Slow ATK Speed",
                magicPerkText = "Frost Shell +20%",
                rarePerkText = "Frost Shell +20%",
                uniquePerkText = "Area Slow +30%",
                epicPerkText = "Blizzard Blast +30%"
            },
            // 2. Drone Spider (Turret Buffer - As in user screenshot)
            new BuddyItemData
            {
                id = 2,
                buddyName = "Turret Buffer",
                iconKey = "drone-spider",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Improves the skills of all Turrets.",
                baseStatText = "All Turrets' Duration <color=#FFCB49>10%</color>",
                magicPerkText = "Turret Duration +20%",
                rarePerkText = "Turret Duration +30%",
                uniquePerkText = "Turret Duration +30%",
                epicPerkText = "Turret Duration +30%"
            },
            // 3. Drone Antenna Eye (Radar Eye)
            new BuddyItemData
            {
                id = 3,
                buddyName = "Radar Eye",
                iconKey = "drone-antenna-eye",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Scans hostiles and pinpoints critical weaknesses.",
                baseStatText = "All Weapons' CRIT Rate <color=#FFCB49>+5%</color>",
                magicPerkText = "CRIT Damage +20%",
                rarePerkText = "Scan Range +30%",
                uniquePerkText = "Weakpoint Bonus +30%",
                epicPerkText = "Target Lock +30%"
            },
            // 4. Drone Cross Visor (Assault Blaster)
            new BuddyItemData
            {
                id = 4,
                buddyName = "Assault Blaster",
                iconKey = "drone-cross-visor",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Continuous twin blaster providing direct firepower.",
                baseStatText = "All Weapons' ATK <color=#FFCB49>+12%</color>",
                magicPerkText = "Blaster ATK +20%",
                rarePerkText = "Fire Rate +30%",
                uniquePerkText = "Dual Shot ATK +30%",
                epicPerkText = "Overheat Surge +30%"
            },
            // 5. Drone Capsule (Nano Healer)
            new BuddyItemData
            {
                id = 5,
                buddyName = "Nano Healer",
                iconKey = "drone-capsule",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Dispatches automated nano-capsules to regenerate health.",
                baseStatText = "Player HP Recovery <color=#FFCB49>+2 HP/s</color>",
                magicPerkText = "Heal Amount +20%",
                rarePerkText = "Repair Speed +30%",
                uniquePerkText = "Shield Battery +30%",
                epicPerkText = "Emergency Revive +30%"
            },
            // 6. Drone Spiky Mine (Mine Layer)
            new BuddyItemData
            {
                id = 6,
                buddyName = "Mine Layer",
                iconKey = "drone-spiky-mine",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Deploys cluster shrapnel mines around the player.",
                baseStatText = "Mine AoE Range <color=#FFCB49>+15%</color>",
                magicPerkText = "Mine ATK +20%",
                rarePerkText = "Mine Cooldown -30%",
                uniquePerkText = "Cluster Count +30%",
                epicPerkText = "Shrapnel Blast +30%"
            },
            // 7. Drone Octagon Shield (Aegis Defender)
            new BuddyItemData
            {
                id = 7,
                buddyName = "Aegis Defender",
                iconKey = "drone-octagon-shield",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Projects a geometric barrier blocking incoming enemy fire.",
                baseStatText = "Player Shield Defense <color=#FFCB49>+18%</color>",
                magicPerkText = "Barrier Duration +20%",
                rarePerkText = "Cooldown -30%",
                uniquePerkText = "Damage Absorption +30%",
                epicPerkText = "Pulse Reflection +30%"
            },
            // 8. Drone Claw Magnet (Scavenger Unit)
            new BuddyItemData
            {
                id = 8,
                buddyName = "Scavenger Unit",
                iconKey = "drone-claw-magnet",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Magnetically attracts dropped chips and energy cells.",
                baseStatText = "Resource Vacuum Radius <color=#FFCB49>+35%</color>",
                magicPerkText = "Pickup Range +20%",
                rarePerkText = "Chip Drop Rate +30%",
                uniquePerkText = "Exp Attraction +30%",
                epicPerkText = "Scrap Recycling +30%"
            },
            // 9. Drone Dual Rotor (Air Striker)
            new BuddyItemData
            {
                id = 9,
                buddyName = "Air Striker",
                iconKey = "drone-dual-rotor",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Executes aerial bombardment on congested monster waves.",
                baseStatText = "Bombing Splash Damage <color=#FFCB49>+20%</color>",
                magicPerkText = "Air Bomb ATK +20%",
                rarePerkText = "Flight Speed +30%",
                uniquePerkText = "Bomb Radius +30%",
                epicPerkText = "Napalm Burn +30%"
            },
            // 10. Purifying Drone (Matches User Screenshot 2)
            new BuddyItemData
            {
                id = 10,
                buddyName = "Purifying Drone",
                iconKey = "drone-stealth-wing",
                tier = BuddyTier.Common,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 500,
                description = "Increase the ratio of\nAilment Resistance",
                baseStatText = "Ailment Resistance 5%",
                magicPerkText = "Ailment Resistance +5%",
                rarePerkText = "Ailment Resistance +7%",
                uniquePerkText = "Ailment Resistance +9%",
                epicPerkText = "Remove Ailment Instantly (cooldown 30s)"
            },
            // 11. Drone Laser Sentry (Beam Sentry)
            new BuddyItemData
            {
                id = 11,
                buddyName = "Beam Sentry",
                iconKey = "drone-laser-sentry",
                tier = BuddyTier.Rare,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 750,
                description = "Locks onto highest HP targets with continuous thermal beams.",
                baseStatText = "Boss Target Damage <color=#FFCB49>+30%</color>",
                magicPerkText = "Beam ATK +20%",
                rarePerkText = "Burn Duration +30%",
                uniquePerkText = "Beam Width +30%",
                epicPerkText = "Thermal Meltdown +30%"
            },
            // 12. Drone Plasma Orb (Plasma Vortex)
            new BuddyItemData
            {
                id = 12,
                buddyName = "Plasma Vortex",
                iconKey = "drone-plasma-orb",
                tier = BuddyTier.Epic,
                level = 1,
                count = 0,
                requiredCount = 10,
                enhanceCost = 1000,
                description = "Unleashes swirling electrical vortices annihilating crowds.",
                baseStatText = "All Weapons' Lightning ATK <color=#FFCB49>+35%</color>",
                magicPerkText = "Vortex Radius +20%",
                rarePerkText = "Zap Chains +30%",
                uniquePerkText = "Discharge ATK +30%",
                epicPerkText = "Supernova Surge +30%"
            }
        };
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

        if (droneModeBtn != null) droneModeBtn.onClick.AddListener(() => ShowToast("Drone Hangar Active"));
        if (robotPetModeBtn != null) robotPetModeBtn.onClick.AddListener(() => ShowToast("Robot Pet unlocks at Chapter 12!"));

        if (detailCloseBtn != null) detailCloseBtn.onClick.AddListener(() => UIDissolveController.HideWithEffect(detailModal));
        if (detailEnhanceBtn != null) detailEnhanceBtn.onClick.AddListener(EnhanceSelectedBuddy);
        if (detailAdvanceTierBtn != null) detailAdvanceTierBtn.onClick.AddListener(AdvanceTierSelectedBuddy);
        if (detailEquipBtn != null) detailEquipBtn.onClick.AddListener(ToggleEquipSelectedBuddy);
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

    public void AutoWireEquippedSlotsIfMissing()
    {
        if (equippedSlots == null || equippedSlots.Length != 3)
        {
            equippedSlots = new BuddyCardUI[3];
        }

        Transform equippedRow = transform.Find("BoardBackground/EquippedRow") ??
                                transform.Find("EquippedBoard/EquippedRow") ??
                                transform.Find("EquippedRow") ??
                                GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("EquippedRow", StringComparison.OrdinalIgnoreCase));

        if (equippedRow != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform slotT = equippedRow.Find($"EquippedSlot_{i}");
                if (slotT == null && i < equippedRow.childCount)
                {
                    slotT = equippedRow.GetChild(i);
                }

                if (slotT != null)
                {
                    BuddyCardUI card = slotT.GetComponent<BuddyCardUI>() ?? slotT.gameObject.AddComponent<BuddyCardUI>();
                    if (slotT.GetComponent<Button>() == null)
                    {
                        slotT.gameObject.AddComponent<Button>();
                    }
                    card.EnsureProgressBar();
                    equippedSlots[i] = card;
                }
            }
        }
    }

    public void AutoWireInventoryContainerIfMissing()
    {
        if (slotIconBuddyContainer == null)
        {
            slotIconBuddyContainer = transform.Find("Content/SlotIconBuddy") ??
                                     transform.Find("BoardBackground/SlotIconBuddy") ??
                                     transform.Find("SlotIconBuddy") ??
                                     GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("SlotIconBuddy", StringComparison.OrdinalIgnoreCase));
        }

        if (slotIconBuddyContainer != null)
        {
            inventoryCards.Clear();
            string[] targetSlotNames = new string[5]
            {
                "drone-snowflake",
                "drone-spider",
                "drone-stealth-wing",
                "drone-antenna-eye",
                "drone-cross-visor"
            };

            for (int i = 0; i < targetSlotNames.Length; i++)
            {
                Transform child = slotIconBuddyContainer.Find(targetSlotNames[i]);
                if (child == null && i < slotIconBuddyContainer.childCount)
                {
                    child = slotIconBuddyContainer.GetChild(i);
                }

                if (child != null)
                {
                    BuddyCardUI card = child.GetComponent<BuddyCardUI>() ?? child.gameObject.AddComponent<BuddyCardUI>();
                    if (child.GetComponent<Button>() == null)
                    {
                        child.gameObject.AddComponent<Button>();
                    }
                    card.EnsureProgressBar();
                    inventoryCards.Add(card);
                }
            }
        }
    }

    public void AutoWireSlotIconBuddyIfMissing()
    {
        AutoWireEquippedSlotsIfMissing();
        AutoWireInventoryContainerIfMissing();
    }

    public void EnsureEquippedSlotsMatchTemplate()
    {
        if (slotIconBuddyContainer == null)
        {
            slotIconBuddyContainer = transform.Find("Content/SlotIconBuddy") ??
                                     transform.Find("SlotIconBuddy") ??
                                     transform.Find("BoardBackground/SlotIconBuddy") ??
                                     GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("SlotIconBuddy", StringComparison.OrdinalIgnoreCase));
        }

        Transform template = null;
        if (slotIconBuddyContainer != null && slotIconBuddyContainer.childCount > 0)
        {
            template = slotIconBuddyContainer.GetChild(0); // drone-snowflake clean template
        }
        else
        {
            template = transform.Find("Content/SlotIconBuddy/drone-snowflake") ??
                       transform.Find("SlotIconBuddy/drone-snowflake") ??
                       GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("drone-snowflake", StringComparison.OrdinalIgnoreCase));
        }

        Transform equippedRowT = transform.Find("BoardBackground/EquippedRow") ??
                                 transform.Find("EquippedBoard/EquippedRow") ??
                                 transform.Find("EquippedRow") ??
                                 GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("EquippedRow", StringComparison.OrdinalIgnoreCase));

        if (template == null || equippedRowT == null) return;

        for (int i = 0; i < 3; i++)
        {
            Transform existingSlot = equippedRowT.Find($"EquippedSlot_{i}");
            if (existingSlot == null && i < equippedRowT.childCount)
            {
                existingSlot = equippedRowT.GetChild(i);
            }

            bool needsRebuild = existingSlot == null ||
                                existingSlot.Find("UpgradeArrowGroup") != null ||
                                existingSlot.Find("NormalContentGroup") != null ||
                                existingSlot.Find("EmptySlotGroup") != null ||
                                existingSlot.Find("BottomBar") != null ||
                                existingSlot.Find("Fill") == null;

            if (needsRebuild)
            {
                GameObject clone = Instantiate(template.gameObject, equippedRowT);
                clone.name = $"EquippedSlot_{i}";
                clone.transform.localScale = Vector3.one;
                clone.transform.localRotation = Quaternion.identity;

                RectTransform cloneRect = clone.GetComponent<RectTransform>();
                RectTransform templateRect = template.GetComponent<RectTransform>();
                if (cloneRect != null && templateRect != null)
                {
                    cloneRect.sizeDelta = templateRect.sizeDelta;
                }

                if (existingSlot != null)
                {
                    clone.transform.SetSiblingIndex(existingSlot.GetSiblingIndex());
                    if (Application.isPlaying) Destroy(existingSlot.gameObject);
                    else DestroyImmediate(existingSlot.gameObject);
                }

                existingSlot = clone.transform;
            }

            BuddyCardUI card = existingSlot.GetComponent<BuddyCardUI>() ?? existingSlot.gameObject.AddComponent<BuddyCardUI>();
            if (existingSlot.GetComponent<Button>() == null)
            {
                existingSlot.gameObject.AddComponent<Button>();
            }
            card.EnsureProgressBar();
            if (card.ProgressFillImage != null)
            {
                card.ProgressFillImage.color = Color.white;
            }
            if (i < equippedSlots.Length)
            {
                equippedSlots[i] = card;
            }
        }
    }

    public void AutoWireDetailModalReferencesIfMissing()
    {
        if (detailModal == null)
        {
            Transform modalT = transform.Find("BuddyDetailModal") ??
                               transform.root.Find("Canvas/BuddyDetailModal") ??
                               GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("BuddyDetailModal", StringComparison.OrdinalIgnoreCase));
            if (modalT != null) detailModal = modalT.gameObject;
        }

        if (detailModal == null) return;

        Transform modalRoot = detailModal.transform.Find("ModalBox") ?? detailModal.transform;

        Transform modBadgeT = modalRoot.Find("ModBadge");
        if (modBadgeT != null) modBadgeT.gameObject.SetActive(false);

        // 1. Top Card
        if (detailTopCard == null)
        {
            Transform topCardT = modalRoot.Find("TopCard") ?? modalRoot.Find("IconFrame");
            if (topCardT != null)
            {
                detailTopCard = topCardT.GetComponent<BuddyCardUI>();
                if (detailTopCard == null)
                {
                    var oldChipset = topCardT.GetComponent<ChipsetCardUI>();
                    if (oldChipset != null)
                    {
                        if (Application.isPlaying) Destroy(oldChipset);
                        else DestroyImmediate(oldChipset);
                    }
                    detailTopCard = topCardT.gameObject.AddComponent<BuddyCardUI>();
                }
            }
        }

        if (detailTopCard != null)
        {
            detailTopCard.EnsureProgressBar();
        }

        // 2. Texts
        if (detailNameText == null)
        {
            Transform t = modalRoot.Find("Name");
            if (t != null) detailNameText = t.GetComponent<TMP_Text>();
        }
        if (detailTierText == null)
        {
            Transform t = modalRoot.Find("Tier");
            if (t != null) detailTierText = t.GetComponent<TMP_Text>();
        }
        if (detailDescText == null)
        {
            Transform t = modalRoot.Find("Description");
            if (t != null) detailDescText = t.GetComponent<TMP_Text>();
        }
        if (detailBaseStatText == null)
        {
            Transform t = modalRoot.Find("BaseStat") ?? modalRoot.Find("StatsBox/StatText") ?? modalRoot.Find("StatsBox");
            if (t != null) detailBaseStatText = t.GetComponent<TMP_Text>();
        }

        // 3. Perk rows
        if (perkRowIcons == null || perkRowIcons.Length < 4) perkRowIcons = new Image[4];
        if (perkRowTexts == null || perkRowTexts.Length < 4) perkRowTexts = new TMP_Text[4];

        for (int i = 0; i < 4; i++)
        {
            if (perkRowIcons[i] == null || perkRowTexts[i] == null)
            {
                Transform row = modalRoot.Find($"PerkRow_{i}");
                if (row != null)
                {
                    if (perkRowIcons[i] == null)
                    {
                        Transform iconT = row.Find("LockIcon");
                        if (iconT != null) perkRowIcons[i] = iconT.GetComponent<Image>();
                    }
                    if (perkRowTexts[i] == null)
                    {
                        Transform textT = row.Find("PerkText");
                        if (textT != null) perkRowTexts[i] = textT.GetComponent<TMP_Text>();
                    }
                }
            }
        }

        // 4. Action Buttons
        if (detailEquipBtn == null)
        {
            Transform t = modalRoot.Find("EquipBtn");
            if (t != null)
            {
                detailEquipBtn = t.GetComponent<Button>();
                if (detailEquipBtnText == null) detailEquipBtnText = t.GetComponentInChildren<TMP_Text>(true);
            }
        }

        Transform enhBtnT = modalRoot.Find("EnhanceBtn");
        if (enhBtnT != null)
        {
            if (detailEnhanceBtn == null) detailEnhanceBtn = enhBtnT.GetComponent<Button>();
            if (detailEnhanceCostText == null || detailEnhanceCostText.gameObject.name == "Label")
            {
                detailEnhanceCostText = enhBtnT.Find("CostRow/CostValue")?.GetComponent<TMP_Text>()
                    ?? enhBtnT.Find("CostValue")?.GetComponent<TMP_Text>()
                    ?? enhBtnT.Find("Cost")?.GetComponent<TMP_Text>();
            }

            Transform labelT = enhBtnT.Find("Label");
            if (labelT != null)
            {
                TMP_Text labelText = labelT.GetComponent<TMP_Text>();
                if (labelText != null && (string.IsNullOrEmpty(labelText.text) || int.TryParse(labelText.text, out _)))
                {
                    labelText.text = "Enhance";
                }
            }
        }

        if (detailAdvanceTierBtn == null)
        {
            Transform t = modalRoot.Find("AdvanceTierBtn");
            if (t != null)
            {
                detailAdvanceTierBtn = t.GetComponent<Button>();
                if (detailAdvanceTierText == null) detailAdvanceTierText = t.Find("Label")?.GetComponent<TMP_Text>() ?? t.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (detailCloseBtn == null)
        {
            Transform t = modalRoot.Find("CloseBtn") ?? modalRoot.Find("Close");
            if (t != null) detailCloseBtn = t.GetComponent<Button>();
        }

        if (notEnoughFragmentsNotice == null)
        {
            Transform t = modalRoot.Find("NotEnoughFragmentsNotice") ?? detailModal.transform.Find("NotEnoughFragmentsNotice");
            if (t != null) notEnoughFragmentsNotice = t.gameObject;
        }

        if (notEnoughChipsNotice == null)
        {
            Transform t = modalRoot.Find("NotEnoughChipsNotice") ?? detailModal.transform.Find("NotEnoughChipsNotice");
            if (t != null) notEnoughChipsNotice = t.gameObject;
        }

        // Setup listeners if buttons are available
        if (detailCloseBtn != null)
        {
            detailCloseBtn.onClick.RemoveAllListeners();
            detailCloseBtn.onClick.AddListener(() => UIDissolveController.HideWithEffect(detailModal));
        }
        if (detailEnhanceBtn != null)
        {
            detailEnhanceBtn.onClick.RemoveAllListeners();
            detailEnhanceBtn.onClick.AddListener(EnhanceSelectedBuddy);
        }
        if (detailAdvanceTierBtn != null)
        {
            detailAdvanceTierBtn.onClick.RemoveAllListeners();
            detailAdvanceTierBtn.onClick.AddListener(AdvanceTierSelectedBuddy);
        }
        if (detailEquipBtn != null)
        {
            detailEquipBtn.onClick.RemoveAllListeners();
            detailEquipBtn.onClick.AddListener(ToggleEquipSelectedBuddy);
        }
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
        activeDeckIndex = deckIndex;
        PlayerDataService.ActiveBuddyDeckIndex = activeDeckIndex;
        RefreshPresetButtons();
        RefreshEquippedGrid();
        ShowToast($"Switched to Buddy Preset {deckIndex + 1}");
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
    }

    public void AutoWirePresetButtonsIfMissing()
    {
        if (preset1Btn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            preset1Btn = btns.FirstOrDefault(b => b.name.Equals("Preset1Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Preset1", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Deck1Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("1", StringComparison.OrdinalIgnoreCase));
        }

        if (preset2Btn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            preset2Btn = btns.FirstOrDefault(b => b.name.Equals("Preset2Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Preset2", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Deck2Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("2", StringComparison.OrdinalIgnoreCase));
        }

        if (preset3Btn == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            preset3Btn = btns.FirstOrDefault(b => b.name.Equals("Preset3Btn", StringComparison.OrdinalIgnoreCase)
                                               || b.name.Equals("Preset3", StringComparison.OrdinalIgnoreCase)
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

        bool isByQuantity = sortByQuantity;

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
        EnsureEquippedSlotsMatchTemplate();
        AutoWireEquippedSlotsIfMissing();

        if (deckEquippedIds == null || activeDeckIndex >= deckEquippedIds.Length || deckEquippedIds[activeDeckIndex] == null || deckEquippedIds[activeDeckIndex].Length != 3)
        {
            InitializeDatabase();
        }
        int[] currentDeck = deckEquippedIds[activeDeckIndex];

        for (int i = 0; i < equippedSlots.Length && i < 3; i++)
        {
            if (equippedSlots[i] == null) continue;

            int slotIndex = i;
            int buddyId = (currentDeck != null && i < currentDeck.Length) ? currentDeck[i] : -1;
            Sprite frame = GetFrameSprite(BuddyTier.Common);

            if (buddyId == -2 || (slotUnlocked != null && i < slotUnlocked.Length && !slotUnlocked[i]))
            {
                equippedSlots[i].SetupLocked(frame, () => ShowToast($"Slot {slotIndex + 1} locked!"));
            }
            else if (buddyId <= 0)
            {
                equippedSlots[i].SetupEmpty(emptySlotFrameSprite ?? frame, () => ShowToast("Empty Slot! Please select a Drone below to equip."));
            }
            else
            {
                BuddyItemData buddy = allBuddies.FirstOrDefault(b => b.id == buddyId);
                if (buddy != null)
                {
                    Sprite icon = GetIconSprite(buddy);
                    Sprite buddyFrame = GetFrameSprite(buddy.tier);
                    equippedSlots[i].Setup(buddy, icon, buddyFrame, (b) => OpenDetailModalFromEquippedSlot(b, slotIndex), QuickUpgradeBuddy);
                }
                else
                {
                    equippedSlots[i].SetupEmpty(emptySlotFrameSprite ?? frame, () => ShowToast("Empty Slot! Please select a Drone below to equip."));
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

            BuddyCardUI card = child.GetComponent<BuddyCardUI>();
            if (card != null)
            {
                spawnedInventoryCards.Add(card);
            }
        }
    }

    public void RefreshInventory()
    {
        AutoWireInventoryContainerIfMissing();

        // 1. Populate fixed inventory cards in SlotIconBuddy (always visible, never empty)
        if (inventoryCards != null && inventoryCards.Count > 0)
        {
            for (int i = 0; i < inventoryCards.Count; i++)
            {
                BuddyCardUI card = inventoryCards[i];
                if (card == null) continue;

                string cardName = card.gameObject.name;
                BuddyItemData data = allBuddies.FirstOrDefault(b =>
                    (!string.IsNullOrEmpty(b.iconKey) && cardName.IndexOf(b.iconKey, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(b.buddyName) && cardName.IndexOf(b.buddyName, StringComparison.OrdinalIgnoreCase) >= 0));

                if (data == null && i < allBuddies.Count)
                {
                    data = allBuddies[i];
                }

                if (data != null)
                {
                    Sprite icon = GetIconSprite(data);
                    Sprite frame = GetFrameSprite(data.tier);
                    card.Setup(data, icon, frame, (b) => OpenDetailModalFromInventory(b), QuickUpgradeBuddy);
                    card.gameObject.SetActive(true);
                }
            }
        }

        // 2. Populate scroll view if active
        if (inventoryContent != null && inventoryContent.gameObject.activeInHierarchy)
        {
            EnsureInventoryCardsInitialized();

            List<BuddyItemData> sortedList = new List<BuddyItemData>(allBuddies);
            if (sortByQuantity)
            {
                sortedList = sortedList.OrderByDescending(b => b.count).ThenByDescending(b => (int)b.tier).ToList();
            }
            else
            {
                sortedList = sortedList.OrderByDescending(b => (int)b.tier).ThenByDescending(b => b.level).ToList();
            }

            for (int i = 0; i < sortedList.Count; i++)
            {
                BuddyCardUI card;
                if (i < spawnedInventoryCards.Count)
                {
                    card = spawnedInventoryCards[i];
                }
                else
                {
                    if (cardPrefab == null) break;
                    GameObject obj = Instantiate(cardPrefab, inventoryContent);
                    card = obj.GetComponent<BuddyCardUI>();
                    spawnedInventoryCards.Add(card);
                }

                BuddyItemData data = sortedList[i];
                Sprite icon = GetIconSprite(data);
                Sprite frame = GetFrameSprite(data.tier);
                card.Setup(data, icon, frame, (b) => OpenDetailModalFromInventory(b), QuickUpgradeBuddy);
                card.gameObject.SetActive(true);
            }

            for (int i = sortedList.Count; i < spawnedInventoryCards.Count; i++)
            {
                spawnedInventoryCards[i].gameObject.SetActive(false);
            }
        }
    }

    public void QuickUpgradeBuddy(BuddyItemData buddy)
    {
        OpenDetailModal(buddy);
    }

    public void OpenDetailModalFromEquippedSlot(BuddyItemData buddy, int slotIndex)
    {
        openedFromEquippedSlotIndex = slotIndex;
        OpenDetailModal(buddy);
    }

    public void OpenDetailModalFromInventory(BuddyItemData buddy)
    {
        openedFromEquippedSlotIndex = -1;
        OpenDetailModal(buddy);
    }

    public void OpenDetailModal(BuddyItemData buddy)
    {
        if (buddy == null) return;
        selectedDetailBuddy = buddy;
        RefreshDetailModal();
        if (detailModal != null)
        {
            UIDissolveController.ShowInstant(detailModal);
        }
    }

    public void RefreshDetailModal()
    {
        if (selectedDetailBuddy == null) return;
        AutoWireDetailModalReferencesIfMissing();

        // 1. Top Card (Clones exact visual from slot)
        Sprite icon = GetIconSprite(selectedDetailBuddy);
        Sprite frame = GetFrameSprite(selectedDetailBuddy.tier);

        if (detailTopCard != null)
        {
            detailTopCard.Setup(selectedDetailBuddy, icon, frame);
            if (detailTopCard.DroneIconImage != null)
            {
                detailTopCard.DroneIconImage.sprite = icon;
                detailTopCard.DroneIconImage.color = Color.white;
                detailTopCard.DroneIconImage.enabled = (icon != null);
                detailTopCard.DroneIconImage.gameObject.SetActive(icon != null);
            }
        }
        else if (detailModal != null)
        {
            Transform iconT = detailModal.transform.Find("ModalBox/TopCard/NormalContentGroup/Icon") ??
                             detailModal.transform.Find("ModalBox/TopCard/Icon");
            if (iconT != null)
            {
                Image img = iconT.GetComponent<Image>();
                if (img != null && icon != null)
                {
                    img.sprite = icon;
                    img.color = Color.white;
                    img.enabled = true;
                    img.gameObject.SetActive(true);
                }
            }
        }

        // 2. Name & Tier Label with color
        if (detailNameText != null) detailNameText.text = selectedDetailBuddy.buddyName;
        if (detailTierText != null)
        {
            string tierColorHex;
            switch (selectedDetailBuddy.tier)
            {
                case BuddyTier.Magic: tierColorHex = "#38BDF8"; break;
                case BuddyTier.Rare: tierColorHex = "#38BDF8"; break; // Blue as in requirements
                case BuddyTier.Unique: tierColorHex = "#F472B6"; break; // Pink-Purple
                case BuddyTier.Epic: tierColorHex = "#FACC15"; break; // Yellow
                case BuddyTier.Holographic: tierColorHex = "#FB7185"; break; // Red
                default: tierColorHex = "#22C55E"; break; // Common Green
            }
            detailTierText.text = $"<color={tierColorHex}>{selectedDetailBuddy.tier.ToString().ToUpper()}</color>";
        }

        // 3. Description & Base Stat
        if (detailDescText != null) detailDescText.text = selectedDetailBuddy.description;
        if (detailBaseStatText != null) detailBaseStatText.text = selectedDetailBuddy.baseStatText;

        // 4. 4 Tier Perk Rows with icons and active tags
        string[] tierNames = { "Rare", "Unique", "Epic", "Holo" };
        string[] tierColors = { "#38BDF8", "#F472B6", "#FACC15", "#FB7185" };
        string[] perkTexts = {
            selectedDetailBuddy.magicPerkText,
            selectedDetailBuddy.rarePerkText,
            selectedDetailBuddy.uniquePerkText,
            selectedDetailBuddy.epicPerkText
        };

        for (int i = 0; i < 4; i++)
        {
            bool isUnlocked = (int)selectedDetailBuddy.tier > i;
            if (perkRowIcons != null && i < perkRowIcons.Length && perkRowIcons[i] != null)
            {
                if (isUnlocked && unlockedCheckSprite != null)
                {
                    perkRowIcons[i].sprite = unlockedCheckSprite;
                }
                else if (lockTierSprites != null && i < lockTierSprites.Length && lockTierSprites[i] != null)
                {
                    perkRowIcons[i].sprite = lockTierSprites[i];
                }
            }

            if (perkRowTexts != null && i < perkRowTexts.Length && perkRowTexts[i] != null)
            {
                if (isUnlocked)
                {
                    perkRowTexts[i].text = $"<color=#40DAD2>{perkTexts[i]}</color> <color=#22C55E>[ACTIVE]</color>";
                }
                else
                {
                    perkRowTexts[i].text = $"{perkTexts[i]} (<color={tierColors[i]}>{tierNames[i]}</color> Unlock)";
                }
            }
        }

        // 5. Enhance Button
        if (detailEnhanceBtn != null)
        {
            detailEnhanceBtn.interactable = selectedDetailBuddy.CanEnhance;
            if (detailEnhanceCostText == null || detailEnhanceCostText.gameObject.name == "Label")
            {
                detailEnhanceCostText = detailEnhanceBtn.transform.Find("CostRow/CostValue")?.GetComponent<TMP_Text>()
                    ?? detailEnhanceBtn.transform.Find("CostValue")?.GetComponent<TMP_Text>();
            }

            Transform labelT = detailEnhanceBtn.transform.Find("Label");
            if (labelT != null)
            {
                TMP_Text labelText = labelT.GetComponent<TMP_Text>();
                if (labelText != null && (string.IsNullOrEmpty(labelText.text) || int.TryParse(labelText.text, out _)))
                {
                    labelText.text = "Enhance";
                }
            }
        }
        if (detailEnhanceCostText != null)
        {
            detailEnhanceCostText.text = $"{selectedDetailBuddy.enhanceCost}";
        }

        // 6. Advance Tier Button
        if (detailAdvanceTierText != null)
        {
            detailAdvanceTierText.text = selectedDetailBuddy.requiredCount > 0
                ? $"Advance Tier ({selectedDetailBuddy.count}/{selectedDetailBuddy.requiredCount})"
                : "MAX TIER";
        }
        if (detailAdvanceTierBtn != null)
        {
            detailAdvanceTierBtn.interactable = selectedDetailBuddy.CanAdvanceTier;
        }

        // 7. Equip / Unequip Button
        int[] currentDeck = (deckEquippedIds != null && activeDeckIndex < deckEquippedIds.Length)
            ? deckEquippedIds[activeDeckIndex]
            : null;
        bool isEquipped = currentDeck != null && currentDeck.Contains(selectedDetailBuddy.id);

        if (detailEquipBtnText != null)
        {
            if (openedFromEquippedSlotIndex >= 0)
            {
                detailEquipBtnText.text = "UNEQUIP";
            }
            else
            {
                detailEquipBtnText.text = isEquipped ? "EQUIPPED" : "EQUIP";
            }
        }

        if (detailEquipBtn != null)
        {
            if (openedFromEquippedSlotIndex >= 0)
            {
                detailEquipBtn.interactable = true;
            }
            else
            {
                detailEquipBtn.interactable = !isEquipped;
            }
        }
    }

    private void EnhanceSelectedBuddy()
    {
        if (selectedDetailBuddy == null) return;
        if (!selectedDetailBuddy.CanEnhance)
        {
            if (notEnoughChipsNotice != null)
            {
                notEnoughChipsNotice.SetActive(false);
                notEnoughChipsNotice.SetActive(true);
                CancelInvoke(nameof(HideNotices));
                Invoke(nameof(HideNotices), 2.0f);
            }
            ShowToast("Not enough Data Chips to enhance!");
            return;
        }

        if (selectedDetailBuddy.Enhance())
        {
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            ShowToast($"Enhanced {selectedDetailBuddy.buddyName} to LV.{selectedDetailBuddy.level:00}!");
        }
    }

    private void AdvanceTierSelectedBuddy()
    {
        if (selectedDetailBuddy == null) return;
        if (!selectedDetailBuddy.CanAdvanceTier)
        {
            if (notEnoughFragmentsNotice != null)
            {
                notEnoughFragmentsNotice.SetActive(false);
                notEnoughFragmentsNotice.SetActive(true);
                CancelInvoke(nameof(HideNotices));
                Invoke(nameof(HideNotices), 2.0f);
            }
            ShowToast("Not enough fragments to advance tier!");
            return;
        }

        if (selectedDetailBuddy.AdvanceTier())
        {
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            GameEvents.RaiseDroneTierAdvanced(selectedDetailBuddy.iconKey ?? selectedDetailBuddy.buddyName ?? selectedDetailBuddy.id.ToString(), (int)selectedDetailBuddy.tier);
            ShowToast($"Advanced {selectedDetailBuddy.buddyName} to {selectedDetailBuddy.tier} Tier!");
        }
    }

    private void HideNotices()
    {
        if (notEnoughFragmentsNotice != null) notEnoughFragmentsNotice.SetActive(false);
        if (notEnoughChipsNotice != null) notEnoughChipsNotice.SetActive(false);
    }

    private void ToggleEquipSelectedBuddy()
    {
        if (selectedDetailBuddy == null) return;

        int[] currentDeck = (deckEquippedIds != null && activeDeckIndex < deckEquippedIds.Length)
            ? deckEquippedIds[activeDeckIndex]
            : null;
        if (currentDeck == null) return;

        if (openedFromEquippedSlotIndex >= 0)
        {
            int slot = openedFromEquippedSlotIndex;
            if (slot < currentDeck.Length)
            {
                currentDeck[slot] = -1;
            }
            PlayerDataService.SaveBuddyDeck(activeDeckIndex, currentDeck);
            ShowToast($"Unequipped {selectedDetailBuddy.buddyName} from Slot {slot + 1}");
            openedFromEquippedSlotIndex = -1;

            if (detailModal != null)
            {
                UIDissolveController.HideWithEffect(detailModal);
            }
            RefreshEquippedGrid();
            RefreshInventory();
            return;
        }

        int indexInDeck = Array.IndexOf(currentDeck, selectedDetailBuddy.id);
        if (indexInDeck >= 0)
        {
            currentDeck[indexInDeck] = -1;
            PlayerDataService.SaveBuddyDeck(activeDeckIndex, currentDeck);
            ShowToast($"Unequipped {selectedDetailBuddy.buddyName} from Slot {indexInDeck + 1}");
            if (detailModal != null)
            {
                UIDissolveController.HideWithEffect(detailModal);
            }
            RefreshEquippedGrid();
            RefreshInventory();
            return;
        }

        // Equip to first empty slot (index 0, 1, 2)
        int emptyIndex = -1;
        for (int i = 0; i < currentDeck.Length; i++)
        {
            if (currentDeck[i] <= 0 && (slotUnlocked == null || i >= slotUnlocked.Length || slotUnlocked[i]))
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex >= 0)
        {
            currentDeck[emptyIndex] = selectedDetailBuddy.id;
            ShowToast($"Equipped {selectedDetailBuddy.buddyName} to Slot {emptyIndex + 1}");
        }
        else
        {
            currentDeck[0] = selectedDetailBuddy.id;
            ShowToast($"Replaced Slot 1 with {selectedDetailBuddy.buddyName}");
        }

        PlayerDataService.SaveBuddyDeck(activeDeckIndex, currentDeck);
        if (detailModal != null)
        {
            UIDissolveController.HideWithEffect(detailModal);
        }
        RefreshEquippedGrid();
        RefreshInventory();
    }

    private static bool IsValidDroneSprite(Sprite s)
    {
        if (s == null) return false;
        string n = s.name.ToLowerInvariant();
        if (n.Contains("lock") || n.Contains("quantity") || n.Contains("empty") || n.Contains("button") || n.Contains("bg") || n.Contains("khung") || n.Contains("bar") || n.Contains("slot"))
            return false;
        return true;
    }

    public void LoadDroneIconsIfMissing()
    {
        if (droneIcons != null && droneIcons.Length > 0 && droneIcons.Any(s => IsValidDroneSprite(s)))
            return;

#if UNITY_EDITOR
        string path = "Assets/Sprites/UI/Buddy/icon buddy.png";
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => IsValidDroneSprite(s))
            .ToArray();
        if (sprites != null && sprites.Length > 0)
        {
            droneIcons = sprites;
        }
#endif
    }

    public Sprite GetIconSprite(string key, int buddyId = -1)
    {
        LoadDroneIconsIfMissing();
        if (droneIcons == null || droneIcons.Length == 0) return null;

        var validDrones = droneIcons.Where(s => IsValidDroneSprite(s)).ToList();
        if (validDrones.Count == 0)
        {
            validDrones = droneIcons.Where(s => s != null).ToList();
            if (validDrones.Count == 0) return null;
        }

        if (!string.IsNullOrEmpty(key))
        {
            // 1. Exact name match
            Sprite match = validDrones.FirstOrDefault(s => s.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // 2. Normalized clean match (strip drone-, drone_, etc.)
            string cleanKey = key.Replace("drone-", "").Replace("drone_", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
            match = validDrones.FirstOrDefault(s =>
            {
                string cleanName = s.name.Replace("drone-", "").Replace("drone_", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
                return cleanName.Equals(cleanKey, StringComparison.OrdinalIgnoreCase);
            });
            if (match != null) return match;

            // 3. Substring matching
            match = validDrones.FirstOrDefault(s => s.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                   key.IndexOf(s.name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) return match;

            // 4. Semantic keyword recognition for drone archetypes
            if (cleanKey.Contains("spider") || cleanKey.Contains("mine"))
                match = validDrones.FirstOrDefault(s => s.name.IndexOf("spider", StringComparison.OrdinalIgnoreCase) >= 0);
            else if (cleanKey.Contains("snow") || cleanKey.Contains("flake") || cleanKey.Contains("ice") || cleanKey.Contains("freeze") || cleanKey.Contains("capsule") || cleanKey.Contains("healer"))
                match = validDrones.FirstOrDefault(s => s.name.IndexOf("snowflake", StringComparison.OrdinalIgnoreCase) >= 0);
            else if (cleanKey.Contains("stealth") || cleanKey.Contains("wing") || cleanKey.Contains("shadow") || cleanKey.Contains("shield") || cleanKey.Contains("missile"))
                match = validDrones.FirstOrDefault(s => s.name.IndexOf("stealth", StringComparison.OrdinalIgnoreCase) >= 0);
            else if (cleanKey.Contains("antenna") || cleanKey.Contains("eye") || cleanKey.Contains("seeker") || cleanKey.Contains("magnet"))
                match = validDrones.FirstOrDefault(s => s.name.IndexOf("antenna", StringComparison.OrdinalIgnoreCase) >= 0);
            else if (cleanKey.Contains("cross") || cleanKey.Contains("visor") || cleanKey.Contains("vulcan") || cleanKey.Contains("rotor") || cleanKey.Contains("laser"))
                match = validDrones.FirstOrDefault(s => s.name.IndexOf("cross", StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null) return match;
        }

        // 5. If buddyId is specified, cyclically map across available drone icons
        if (buddyId > 0)
        {
            int idx = (buddyId - 1) % validDrones.Count;
            return validDrones[idx];
        }

        // 6. Default to first valid drone sprite
        return validDrones[0];
    }

    public Sprite GetIconSprite(BuddyItemData buddy)
    {
        if (buddy == null) return GetIconSprite(string.Empty);
        return GetIconSprite(buddy.iconKey, buddy.id);
    }

    public void LoadFrameSpritesIfMissing()
    {
#if UNITY_EDITOR
        if (frameSprites == null || frameSprites.Length < 6 || frameSprites.Any(s => s == null))
        {
            Sprite green = null;
            string iconPath = "Assets/Sprites/UI/Buddy/icon buddy.png";
            var iconSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(iconPath).OfType<Sprite>().ToArray();
            green = iconSprites?.FirstOrDefault(s => s.name.Equals("openLocke", StringComparison.OrdinalIgnoreCase));

            Sprite blue = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Blue.png");
            Sprite purple = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Purple.png");
            Sprite yellow = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Yellow.png");
            Sprite red = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Red.png");

            frameSprites = new Sprite[6]
            {
                green,            // Common
                blue ?? green,    // Magic
                purple ?? green,  // Rare
                yellow ?? green,  // Unique
                yellow ?? green,  // Epic
                red ?? green      // Holographic
            };
        }

        if (emptySlotFrameSprite == null)
        {
            string framePath = "Assets/Sprites/UI/Buddy/nút màn buddy.png";
            Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(framePath).OfType<Sprite>().ToArray();
            emptySlotFrameSprite = sprites?.FirstOrDefault(s => s.name.Equals("Empty", StringComparison.OrdinalIgnoreCase));
        }

        if (lockTierSprites == null || lockTierSprites.Length < 4 || lockTierSprites.Any(s => s == null))
        {
            string chipsetPath = "Assets/Sprites/UI/Chipset/khung chipset.png";
            var csSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(chipsetPath).OfType<Sprite>().ToArray();
            lockTierSprites = new Sprite[4]
            {
                csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Blue", StringComparison.OrdinalIgnoreCase)),
                csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Purple", StringComparison.OrdinalIgnoreCase)),
                csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Yellow", StringComparison.OrdinalIgnoreCase)),
                csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Red", StringComparison.OrdinalIgnoreCase))
            };
        }
#endif
    }


    public Sprite GetFrameSprite(BuddyTier tier)
    {
        LoadFrameSpritesIfMissing();
        int index = (int)tier;
        if (frameSprites != null && index >= 0 && index < frameSprites.Length && frameSprites[index] != null)
        {
            return frameSprites[index];
        }
        return (frameSprites != null && frameSprites.Length > 0) ? frameSprites[0] : null;
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
