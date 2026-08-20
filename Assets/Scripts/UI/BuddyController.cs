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

    [Header("Equipped Slots (3 Slots)")]
    [SerializeField] private BuddyCardUI[] equippedSlots = new BuddyCardUI[3];

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

    [Header("Toast Message")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    [Header("Sprites Database")]
    [SerializeField] private Sprite[] droneIcons;
    [SerializeField] private Sprite[] frameSprites;
    [SerializeField] private Sprite upgradeArrowSprite;
    [SerializeField] private Sprite lockSlotSprite;
    [SerializeField] private Sprite[] lockTierSprites = new Sprite[4]; // 0: Magic, 1: Rare, 2: Unique, 3: Epic
    [SerializeField] private Sprite unlockedCheckSprite;

    private int activeDeckIndex = 0;
    private bool sortByQuantity = true;
    private BuddyItemData selectedDetailBuddy;

    [SerializeField] private List<BuddyItemData> allBuddies = new List<BuddyItemData>();
    private int[][] deckEquippedIds = new int[3][];
    private bool[] slotUnlocked = new bool[] { true, true, false };
    private List<BuddyCardUI> spawnedInventoryCards = new List<BuddyCardUI>();

    private static readonly Color SelectedPresetColor = new Color32(255, 203, 73, 255);
    private static readonly Color NormalPresetColor = new Color32(18, 58, 68, 255);
    private static readonly Color SelectedPresetTextColor = new Color32(10, 20, 30, 255);
    private static readonly Color NormalPresetTextColor = new Color32(245, 255, 255, 255);

    public IReadOnlyList<BuddyItemData> AllBuddies => allBuddies;

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
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleCurrencyChanged;
        ChipManager.OnRedGemsChanged -= HandleCurrencyChanged;
        ChipManager.OnEnergyChanged -= HandleCurrencyChanged;
    }

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
        if (allBuddies.Count > 0) return;

        allBuddies = new List<BuddyItemData>
        {
            // 1. Drone Snowflake (Frost Sentinel)
            new BuddyItemData
            {
                id = 1,
                buddyName = "Frost Sentinel",
                iconKey = "drone-snowflake",
                tier = BuddyTier.Common,
                level = 1,
                count = 65,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Emits freezing pulses slowing hostile squads.",
                baseStatText = "All Weapons' Slow Effect <color=#FFCB49>15%</color>",
                magicPerkText = "Slow Duration +20%",
                rarePerkText = "Frost Aura Radius +30%",
                uniquePerkText = "Freeze Siphon +30%",
                epicPerkText = "Blizzard Surge +30%"
            },
            // 2. Drone Spider (Turret Buffer - As in user screenshot)
            new BuddyItemData
            {
                id = 2,
                buddyName = "Turret Buffer",
                iconKey = "drone-spider",
                tier = BuddyTier.Common,
                level = 1,
                count = 79,
                requiredCount = 3,
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
                count = 67,
                requiredCount = 3,
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
                count = 60,
                requiredCount = 3,
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
                count = 58,
                requiredCount = 3,
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
                count = 51,
                requiredCount = 3,
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
                count = 51,
                requiredCount = 3,
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
                count = 48,
                requiredCount = 3,
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
                count = 46,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Executes aerial bombardment on congested monster waves.",
                baseStatText = "Bombing Splash Damage <color=#FFCB49>+20%</color>",
                magicPerkText = "Air Bomb ATK +20%",
                rarePerkText = "Flight Speed +30%",
                uniquePerkText = "Bomb Radius +30%",
                epicPerkText = "Napalm Burn +30%"
            },
            // 10. Drone Stealth Wing (Armor Piercer)
            new BuddyItemData
            {
                id = 10,
                buddyName = "Armor Piercer",
                iconKey = "drone-stealth-wing",
                tier = BuddyTier.Common,
                level = 1,
                count = 38,
                requiredCount = 3,
                enhanceCost = 500,
                description = "Fires high-velocity darts piercing armored threats.",
                baseStatText = "Armor Penetration <color=#FFCB49>+25%</color>",
                magicPerkText = "Dart Speed +20%",
                rarePerkText = "Critical Strike +30%",
                uniquePerkText = "Bleed Damage +30%",
                epicPerkText = "Stealth Strike +30%"
            },
            // 11. Drone Laser Sentry (Beam Sentry)
            new BuddyItemData
            {
                id = 11,
                buddyName = "Beam Sentry",
                iconKey = "drone-laser-sentry",
                tier = BuddyTier.Rare,
                level = 1,
                count = 30,
                requiredCount = 5,
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
                count = 24,
                requiredCount = 7,
                enhanceCost = 1000,
                description = "Unleashes swirling electrical vortices annihilating crowds.",
                baseStatText = "All Weapons' Lightning ATK <color=#FFCB49>+35%</color>",
                magicPerkText = "Vortex Radius +20%",
                rarePerkText = "Zap Chains +30%",
                uniquePerkText = "Discharge ATK +30%",
                epicPerkText = "Supernova Surge +30%"
            }
        };

        deckEquippedIds[0] = new int[] { 1, -1, -2 };
        deckEquippedIds[1] = new int[] { 2, -1, -2 };
        deckEquippedIds[2] = new int[] { 3, -1, -2 };
    }

    private void SetupEventListeners()
    {
        if (preset1Btn != null) preset1Btn.onClick.AddListener(() => SwitchDeck(0));
        if (preset2Btn != null) preset2Btn.onClick.AddListener(() => SwitchDeck(1));
        if (preset3Btn != null) preset3Btn.onClick.AddListener(() => SwitchDeck(2));

        if (byTierBtn != null) byTierBtn.onClick.AddListener(() => SetSortMode(false));
        if (byQuantityBtn != null) byQuantityBtn.onClick.AddListener(() => SetSortMode(true));

        if (droneModeBtn != null) droneModeBtn.onClick.AddListener(() => ShowToast("Drone Hangar Active"));
        if (robotPetModeBtn != null) robotPetModeBtn.onClick.AddListener(() => ShowToast("Robot Pet unlocks at Chapter 12!"));

        if (detailCloseBtn != null) detailCloseBtn.onClick.AddListener(() => detailModal.SetActive(false));
        if (detailEnhanceBtn != null) detailEnhanceBtn.onClick.AddListener(EnhanceSelectedBuddy);
        if (detailAdvanceTierBtn != null) detailAdvanceTierBtn.onClick.AddListener(AdvanceTierSelectedBuddy);
        if (detailEquipBtn != null) detailEquipBtn.onClick.AddListener(ToggleEquipSelectedBuddy);
    }

    public void SwitchDeck(int deckIndex)
    {
        activeDeckIndex = deckIndex;
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
        for (int i = 0; i < 3; i++)
        {
            if (i >= equippedSlots.Length || equippedSlots[i] == null) continue;

            int buddyId = (currentDeck != null && i < currentDeck.Length) ? currentDeck[i] : -1;
            Sprite frame = GetFrameSprite(BuddyTier.Common);

            if (buddyId == -2 || !slotUnlocked[i])
            {
                equippedSlots[i].SetupLocked(frame, () => ShowToast($"Slot {i + 1} unlocks at Chapter 8!"));
            }
            else if (buddyId == -1)
            {
                equippedSlots[i].SetupEmpty(frame, () => ShowToast($"Slot {i + 1} is Empty. Select a drone from below to equip."));
            }
            else
            {
                BuddyItemData buddy = allBuddies.FirstOrDefault(b => b.id == buddyId);
                if (buddy != null)
                {
                    Sprite icon = GetIconSprite(buddy.iconKey);
                    Sprite buddyFrame = GetFrameSprite(buddy.tier);
                    equippedSlots[i].Setup(buddy, icon, buddyFrame, OpenDetailModal, QuickUpgradeBuddy);
                }
                else
                {
                    equippedSlots[i].SetupEmpty(frame);
                }
            }
        }
    }

    public void RefreshInventory()
    {
        if (inventoryContent == null || cardPrefab == null) return;

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
                GameObject obj = Instantiate(cardPrefab, inventoryContent);
                card = obj.GetComponent<BuddyCardUI>();
                spawnedInventoryCards.Add(card);
            }

            BuddyItemData data = sortedList[i];
            Sprite icon = GetIconSprite(data.iconKey);
            Sprite frame = GetFrameSprite(data.tier);
            card.Setup(data, icon, frame, OpenDetailModal, QuickUpgradeBuddy);
            card.gameObject.SetActive(true);
        }

        for (int i = sortedList.Count; i < spawnedInventoryCards.Count; i++)
        {
            spawnedInventoryCards[i].gameObject.SetActive(false);
        }
    }

    public void QuickUpgradeBuddy(BuddyItemData buddy)
    {
        OpenDetailModal(buddy);
    }

    public void OpenDetailModal(BuddyItemData buddy)
    {
        if (buddy == null || detailModal == null) return;
        selectedDetailBuddy = buddy;
        RefreshDetailModal();
        detailModal.SetActive(true);
    }

    public void RefreshDetailModal()
    {
        if (selectedDetailBuddy == null) return;

        // 1. Top Card
        if (detailTopCard != null)
        {
            Sprite icon = GetIconSprite(selectedDetailBuddy.iconKey);
            Sprite frame = GetFrameSprite(selectedDetailBuddy.tier);
            detailTopCard.Setup(selectedDetailBuddy, icon, frame);
        }

        // 2. Name & Tier
        if (detailNameText != null) detailNameText.text = selectedDetailBuddy.buddyName;
        if (detailTierText != null) detailTierText.text = selectedDetailBuddy.tier.ToString();

        // 3. Description & Base Stat
        if (detailDescText != null) detailDescText.text = selectedDetailBuddy.description;
        if (detailBaseStatText != null) detailBaseStatText.text = selectedDetailBuddy.baseStatText;

        // 4. 4 Tier Perk Rows with colored unlock tags
        string[] tierNames = { "Magic", "Rare", "Unique", "Epic" };
        string[] tierColors = { "#38BDF8", "#C084FC", "#FACC15", "#FB7185" };
        string[] perkTexts = {
            selectedDetailBuddy.magicPerkText,
            selectedDetailBuddy.rarePerkText,
            selectedDetailBuddy.uniquePerkText,
            selectedDetailBuddy.epicPerkText
        };

        for (int i = 0; i < 4; i++)
        {
            bool isUnlocked = (int)selectedDetailBuddy.tier > i;
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

        // 5. Enhance Button
        if (detailEnhanceCostText != null)
        {
            detailEnhanceCostText.text = $"{selectedDetailBuddy.enhanceCost}";
        }
        if (detailEnhanceBtn != null)
        {
            detailEnhanceBtn.interactable = selectedDetailBuddy.CanEnhance;
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
        bool isEquipped = deckEquippedIds[activeDeckIndex].Contains(selectedDetailBuddy.id);
        if (detailEquipBtnText != null)
        {
            detailEquipBtnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
        }
    }

    private void EnhanceSelectedBuddy()
    {
        if (selectedDetailBuddy == null) return;
        if (!selectedDetailBuddy.CanEnhance)
        {
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
            ShowToast("Not enough fragments to advance tier!");
            return;
        }

        if (selectedDetailBuddy.AdvanceTier())
        {
            RefreshTopBar();
            RefreshEquippedGrid();
            RefreshInventory();
            RefreshDetailModal();
            ShowToast($"Advanced {selectedDetailBuddy.buddyName} to {selectedDetailBuddy.tier} Tier!");
        }
    }

    private void ToggleEquipSelectedBuddy()
    {
        if (selectedDetailBuddy == null) return;

        int[] currentDeck = deckEquippedIds[activeDeckIndex];
        int indexInDeck = Array.IndexOf(currentDeck, selectedDetailBuddy.id);

        if (indexInDeck >= 0)
        {
            currentDeck[indexInDeck] = -1;
            ShowToast($"Unequipped {selectedDetailBuddy.buddyName}");
        }
        else
        {
            int emptyIndex = -1;
            for (int i = 0; i < currentDeck.Length; i++)
            {
                if (currentDeck[i] == -1 && slotUnlocked[i])
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
        }

        RefreshEquippedGrid();
        RefreshDetailModal();
    }

    private Sprite GetIconSprite(string key)
    {
        if (droneIcons == null || droneIcons.Length == 0) return null;
        return droneIcons.FirstOrDefault(s => s != null && s.name.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? droneIcons[0];
    }

    private Sprite GetFrameSprite(BuddyTier tier)
    {
        if (frameSprites == null || frameSprites.Length == 0) return null;
        switch (tier)
        {
            case BuddyTier.Common:
                return frameSprites.Length > 0 ? frameSprites[0] : null;
            case BuddyTier.Magic:
            case BuddyTier.Rare:
                return frameSprites.Length > 1 ? frameSprites[1] : frameSprites[0];
            case BuddyTier.Unique:
            case BuddyTier.Epic:
                return frameSprites.Length > 2 ? frameSprites[2] : frameSprites[0];
            case BuddyTier.Holographic:
                return frameSprites.Length > 3 ? frameSprites[3] : frameSprites[0];
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
