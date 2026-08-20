using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BuddyTier
{
    Common = 1,
    Rare = 2,
    Epic = 3,
    Holographic = 4
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
    public float atkBonus = 15f;
    public float fireRateBonus = 10f;
    public string specialAbility;
    public string description;

    public bool CanUpgrade => count >= requiredCount && requiredCount > 0;

    public void Upgrade()
    {
        if (!CanUpgrade) return;
        count -= requiredCount;
        level++;
        atkBonus += 5f;
        fireRateBonus += 2.5f;
        requiredCount = Mathf.RoundToInt(requiredCount * 1.5f) + 1;
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
            atkBonus = this.atkBonus,
            fireRateBonus = this.fireRateBonus,
            specialAbility = this.specialAbility,
            description = this.description
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

    [Header("Detail Modal")]
    [SerializeField] private GameObject detailModal;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailLevelText;
    [SerializeField] private TMP_Text detailTierText;
    [SerializeField] private TMP_Text detailStatsText;
    [SerializeField] private TMP_Text detailAbilityText;
    [SerializeField] private Button detailUpgradeBtn;
    [SerializeField] private TMP_Text detailUpgradeBtnText;
    [SerializeField] private Button detailEquipBtn;
    [SerializeField] private TMP_Text detailEquipBtnText;
    [SerializeField] private Button detailCloseBtn;

    [Header("Toast Message")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    [Header("Sprites Database")]
    [SerializeField] private Sprite[] droneIcons;
    [SerializeField] private Sprite[] frameSprites; // 0: Common, 1: Rare, 2: Epic, 3: Holographic
    [SerializeField] private Sprite upgradeArrowSprite;
    [SerializeField] private Sprite lockSlotSprite;

    private int activeDeckIndex = 0; // Default to Preset 1 (index 0) as in screenshot
    private bool sortByQuantity = true;
    private BuddyItemData selectedDetailBuddy;

    // Database of all drones matching screenshot
    [SerializeField] private List<BuddyItemData> allBuddies = new List<BuddyItemData>();
    // 3 Decks holding IDs of equipped buddies (3 slots each)
    private int[][] deckEquippedIds = new int[3][];
    private bool[] slotUnlocked = new bool[] { true, true, false }; // Slot 1 unlocked, Slot 2 unlocked, Slot 3 locked
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
    }

    public void InitializeDatabase()
    {
        if (allBuddies.Count > 0) return;

        allBuddies = new List<BuddyItemData>
        {
            // 1. Drone Snowflake (Equipped in Slot 1 in screenshot)
            new BuddyItemData
            {
                id = 1,
                buddyName = "Snowflake Drone",
                iconKey = "drone-snowflake",
                tier = BuddyTier.Common,
                level = 1,
                count = 65,
                requiredCount = 3,
                atkBonus = 24f,
                fireRateBonus = 12f,
                specialAbility = "Frost Nova: Emits a freezing pulse slowing enemies by 35% every 8s.",
                description = "Tactical support drone that freezes approaching hostiles and shields the player."
            },
            // 2. Drone Spider (79/3 in screenshot)
            new BuddyItemData
            {
                id = 2,
                buddyName = "Spider Quad Drone",
                iconKey = "drone-spider",
                tier = BuddyTier.Common,
                level = 1,
                count = 79,
                requiredCount = 3,
                atkBonus = 28f,
                fireRateBonus = 15f,
                specialAbility = "Web Net: Traps the closest 3 enemies, immobilizing them for 2.5s.",
                description = "Agile quad-legged autonomous unit specializing in ground crowd control."
            },
            // 3. Drone Antenna Eye (67/3 in screenshot)
            new BuddyItemData
            {
                id = 3,
                buddyName = "Antenna Eye Drone",
                iconKey = "drone-antenna-eye",
                tier = BuddyTier.Common,
                level = 1,
                count = 67,
                requiredCount = 3,
                atkBonus = 22f,
                fireRateBonus = 20f,
                specialAbility = "Radar Scan: Increases player Critical Hit Rate by +10% within radius.",
                description = "Reconnaissance eye drone providing critical targeting data to all weapon systems."
            },
            // 4. Drone Cross Visor (60/3 in screenshot)
            new BuddyItemData
            {
                id = 4,
                buddyName = "Cross Visor Drone",
                iconKey = "drone-cross-visor",
                tier = BuddyTier.Common,
                level = 1,
                count = 60,
                requiredCount = 3,
                atkBonus = 32f,
                fireRateBonus = 10f,
                specialAbility = "Overcharge Laser: Fires a continuous twin laser beam dealing 180% damage.",
                description = "Heavy assault robot head unit with high-powered dual optical blasters."
            },
            // 5. Drone Capsule (58/3 in screenshot)
            new BuddyItemData
            {
                id = 5,
                buddyName = "Capsule Drone",
                iconKey = "drone-capsule",
                tier = BuddyTier.Common,
                level = 1,
                count = 58,
                requiredCount = 3,
                atkBonus = 20f,
                fireRateBonus = 14f,
                specialAbility = "Nano Repair: Restores +2 HP every 5 seconds to the player.",
                description = "Defensive support drone equipped with automated nano-repair capsules."
            },
            // 6. Drone Spiky Mine (51/3 in screenshot)
            new BuddyItemData
            {
                id = 6,
                buddyName = "Spiky Mine Drone",
                iconKey = "drone-spiky-mine",
                tier = BuddyTier.Common,
                level = 1,
                count = 51,
                requiredCount = 3,
                atkBonus = 36f,
                fireRateBonus = 8f,
                specialAbility = "Shrapnel Blast: Detonates explosive spikes when enemies get too close.",
                description = "Heavy defensive orb that repels swarming monsters with lethal shrapnel."
            },
            // 7. Drone Octagon Shield (51/3 in screenshot)
            new BuddyItemData
            {
                id = 7,
                buddyName = "Octagon Shield Drone",
                iconKey = "drone-octagon-shield",
                tier = BuddyTier.Common,
                level = 1,
                count = 51,
                requiredCount = 3,
                atkBonus = 18f,
                fireRateBonus = 10f,
                specialAbility = "Barrier Field: Absorbs up to 200 incoming projectile damage.",
                description = "Guardian drone projecting an 8-sided geometric forcefield around the hero."
            },
            // 8. Drone Claw Magnet (48/3 in screenshot)
            new BuddyItemData
            {
                id = 8,
                buddyName = "Claw Magnet Drone",
                iconKey = "drone-claw-magnet",
                tier = BuddyTier.Common,
                level = 1,
                count = 48,
                requiredCount = 3,
                atkBonus = 26f,
                fireRateBonus = 12f,
                specialAbility = "Magnetic Pull: Pulls dropped EXP and Data Chips from 50% farther away.",
                description = "Utility scavenger drone maximizing resource collection speed."
            },
            // 9. Drone Dual Rotor (46/3 in screenshot)
            new BuddyItemData
            {
                id = 9,
                buddyName = "Dual Rotor Drone",
                iconKey = "drone-dual-rotor",
                tier = BuddyTier.Common,
                level = 1,
                count = 46,
                requiredCount = 3,
                atkBonus = 30f,
                fireRateBonus = 18f,
                specialAbility = "Air Strike: Drops mini-bombs over clustered enemy squads.",
                description = "High-speed twin propeller drone providing rapid aerial bombardment."
            },
            // 10. Drone Stealth Wing (38/3 in screenshot)
            new BuddyItemData
            {
                id = 10,
                buddyName = "Stealth Wing Drone",
                iconKey = "drone-stealth-wing",
                tier = BuddyTier.Common,
                level = 1,
                count = 38,
                requiredCount = 3,
                atkBonus = 34f,
                fireRateBonus = 16f,
                specialAbility = "Plasma Dart: Bypasses 50% of enemy armor defense.",
                description = "Advanced stealth unit delivering precision armor-piercing strikes."
            },
            // 11. Drone Laser Sentry
            new BuddyItemData
            {
                id = 11,
                buddyName = "Laser Sentry Drone",
                iconKey = "drone-laser-sentry",
                tier = BuddyTier.Rare,
                level = 1,
                count = 30,
                requiredCount = 5,
                atkBonus = 45f,
                fireRateBonus = 22f,
                specialAbility = "Lock-on Beam: Continuously burns boss targets with escalating damage.",
                description = "Target-tracking sentry drone optimized for single-target boss elimination."
            },
            // 12. Drone Plasma Orb
            new BuddyItemData
            {
                id = 12,
                buddyName = "Plasma Orb Drone",
                iconKey = "drone-plasma-orb",
                tier = BuddyTier.Epic,
                level = 1,
                count = 24,
                requiredCount = 7,
                atkBonus = 60f,
                fireRateBonus = 14f,
                specialAbility = "Plasma Storm: Creates electrical vortices zapping all nearby threats.",
                description = "Experimental high-energy plasma core drone unleashing devastating storms."
            }
        };

        // Preset 1: Slot 1 equipped with Snowflake Drone (ID 1), Slot 2 Empty (-1), Slot 3 Locked (-2)
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
        if (detailUpgradeBtn != null) detailUpgradeBtn.onClick.AddListener(UpgradeSelectedDetailBuddy);
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
                // Locked Slot
                equippedSlots[i].SetupLocked(frame, () => ShowToast($"Slot {i + 1} unlocks at Chapter 8!"));
            }
            else if (buddyId == -1)
            {
                // Empty Slot
                equippedSlots[i].SetupEmpty(frame, () => ShowToast($"Slot {i + 1} is Empty. Select a drone from below to equip."));
            }
            else
            {
                // Equipped Drone
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

        // Sort items
        List<BuddyItemData> sortedList = new List<BuddyItemData>(allBuddies);
        if (sortByQuantity)
        {
            sortedList = sortedList.OrderByDescending(b => b.count).ThenByDescending(b => (int)b.tier).ToList();
        }
        else
        {
            sortedList = sortedList.OrderByDescending(b => (int)b.tier).ThenByDescending(b => b.level).ToList();
        }

        // Reuse or instantiate cards
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
        if (buddy == null || !buddy.CanUpgrade)
        {
            ShowToast("Not enough drone fragments to upgrade!");
            return;
        }

        buddy.Upgrade();
        ChipManager.AddDataChips(150);
        RefreshTopBar();
        RefreshEquippedGrid();
        RefreshInventory();
        if (detailModal != null && detailModal.activeSelf && selectedDetailBuddy == buddy)
        {
            RefreshDetailModal();
        }
        ShowToast($"Upgraded {buddy.buddyName} to LV.{buddy.level:00}!");
    }

    public void OpenDetailModal(BuddyItemData buddy)
    {
        if (buddy == null || detailModal == null) return;
        selectedDetailBuddy = buddy;
        RefreshDetailModal();
        detailModal.SetActive(true);
    }

    private void RefreshDetailModal()
    {
        if (selectedDetailBuddy == null) return;

        if (detailIcon != null) detailIcon.sprite = GetIconSprite(selectedDetailBuddy.iconKey);
        if (detailNameText != null) detailNameText.text = selectedDetailBuddy.buddyName;
        if (detailLevelText != null) detailLevelText.text = $"LEVEL {selectedDetailBuddy.level:00}";
        if (detailTierText != null) detailTierText.text = selectedDetailBuddy.tier.ToString().ToUpper();
        if (detailStatsText != null) detailStatsText.text = $"• Drone ATK: +{selectedDetailBuddy.atkBonus:0}%\n• Fire Rate: +{selectedDetailBuddy.fireRateBonus:0}%\n• {selectedDetailBuddy.description}";
        if (detailAbilityText != null) detailAbilityText.text = $"• <color=#40DAD2>{selectedDetailBuddy.specialAbility}</color>";

        if (detailUpgradeBtn != null)
        {
            detailUpgradeBtn.interactable = selectedDetailBuddy.CanUpgrade;
            if (detailUpgradeBtnText != null)
            {
                detailUpgradeBtnText.text = selectedDetailBuddy.requiredCount > 0
                    ? $"UPGRADE ({selectedDetailBuddy.count}/{selectedDetailBuddy.requiredCount})"
                    : "MAX LEVEL";
            }
        }

        bool isEquipped = deckEquippedIds[activeDeckIndex].Contains(selectedDetailBuddy.id);
        if (detailEquipBtnText != null)
        {
            detailEquipBtnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
        }
    }

    private void UpgradeSelectedDetailBuddy()
    {
        if (selectedDetailBuddy != null)
        {
            QuickUpgradeBuddy(selectedDetailBuddy);
        }
    }

    private void ToggleEquipSelectedBuddy()
    {
        if (selectedDetailBuddy == null) return;

        int[] currentDeck = deckEquippedIds[activeDeckIndex];
        int indexInDeck = Array.IndexOf(currentDeck, selectedDetailBuddy.id);

        if (indexInDeck >= 0)
        {
            // Unequip
            currentDeck[indexInDeck] = -1;
            ShowToast($"Unequipped {selectedDetailBuddy.buddyName}");
        }
        else
        {
            // Find empty slot (among unlocked slots 0 and 1)
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
                // Replace slot 0 if all full
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
            case BuddyTier.Rare:
                return frameSprites.Length > 1 ? frameSprites[1] : frameSprites[0];
            case BuddyTier.Epic:
                return frameSprites.Length > 2 ? frameSprites[2] : frameSprites[0];
            case BuddyTier.Holographic:
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
