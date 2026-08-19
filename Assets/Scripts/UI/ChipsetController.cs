using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChipTier
{
    Common = 0,
    Magic = 1,
    Rare = 2,
    Unique = 3,
    Epic = 4,
    Holographic = 5
}

[Serializable]
public class ChipItemData
{
    public int id;
    public string chipName;
    public string iconKey;
    public ChipTier tier;
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

    public bool CanUpgrade => count >= requiredCount && requiredCount > 0;

    public void Upgrade()
    {
        if (!CanUpgrade) return;
        count -= requiredCount;
        level++;
        // Required fragments increase progressively
        requiredCount = Mathf.RoundToInt(requiredCount * 1.5f) + 1;
    }
}

public class ChipsetController : MonoBehaviour
{
    [Header("Top Bar Currencies")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text chipCurrencyText;
    [SerializeField] private TMP_Text redCurrencyText;

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
    [SerializeField] private Sprite[] frameSprites; // 0: Common, 1: Rare, 2: Epic, 3: Holographic/Unique
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite upgradeArrowSprite;

    private int activeDeckIndex = 2; // Default to Preset 3 (index 2) as in screenshot
    private bool sortByQuantity = true;
    private ChipItemData selectedDetailChip;

    // Database of all 15 chips with full user defined stats
    [SerializeField] private List<ChipItemData> allChips = new List<ChipItemData>();
    // 3 Decks holding IDs of equipped chips (10 slots each)
    private int[][] deckEquippedIds = new int[3][];
    private List<ChipsetCardUI> spawnedInventoryCards = new List<ChipsetCardUI>();

    private int currentEnergy = 370;
    private int currentMaxEnergy = 50;
    private long currentChips = 956467;
    private long currentRedChips = 98762732;

    private static readonly Color SelectedPresetColor = new Color32(255, 203, 73, 255);
    private static readonly Color NormalPresetColor = new Color32(18, 58, 68, 255);
    private static readonly Color SelectedPresetTextColor = new Color32(10, 20, 30, 255);
    private static readonly Color NormalPresetTextColor = new Color32(245, 255, 255, 255);

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

    private void InitializeDatabase()
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
                tier = ChipTier.Common,
                level = 1,
                count = 22,
                requiredCount = 3,
                baseStatsSummary = "ATK 42 | Tốc độ đánh: Fast",
                magicBonus = "Magic: ATK +15%",
                rareBonus = "Rare: ATK Speed +15%",
                uniqueBonus = "Unique: +5% Life Steal (Hút máu)",
                epicBonus = "Epic: Adds Penetration Skill (Bắn xuyên mục tiêu)"
            },
            // 2. Rifle (Holographic/Prismatic in screenshot)
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
                tier = ChipTier.Common,
                level = 1,
                count = 50,
                requiredCount = 3,
                baseStatsSummary = "ATK 50 / AoE ATK 27 | Tốc độ đánh: Slow",
                magicBonus = "Magic: ATK +40%",
                rareBonus = "Rare: ATK Speed +40%",
                uniqueBonus = "Unique: AoE ATK Range +40% (Tăng phạm vi nổ)",
                epicBonus = "Epic: ATK +180%"
            },
            // 4. Spinning Blade (Epic Purple in screenshot)
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
                tier = ChipTier.Common,
                level = 6,
                count = 37,
                requiredCount = 3,
                baseStatsSummary = "ATK 28.5 | Tốc độ đánh: Slow | 3 shells",
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
                tier = ChipTier.Common,
                level = 1,
                count = 49,
                requiredCount = 3,
                baseStatsSummary = "ATK 27 | Tốc độ đánh: Fast | Tồn tại: 12s | Hồi chiêu: 8.4s",
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
                tier = ChipTier.Common,
                level = 1,
                count = 30,
                requiredCount = 3,
                baseStatsSummary = "ATK 30 | Tốc độ quay: Normal Spin Speed",
                magicBonus = "Magic: +1 Discus (+1 đĩa quay)",
                rareBonus = "Rare: Spin Speed +30% (Tăng tốc độ xoay)",
                uniqueBonus = "Unique: +1 Discus (+1 đĩa quay)",
                epicBonus = "Epic: Spin Speed +35%"
            },
            // 8. Shotgun (Rare Blue in screenshot)
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
            // 9. Energy Jumper Cables (has star ⭐ in screenshot)
            new ChipItemData
            {
                id = 9,
                chipName = "Energy Jumper Cables",
                iconKey = "energy-jumper-cables",
                tier = ChipTier.Common,
                level = 1,
                count = 38,
                requiredCount = 3,
                hasStar = true,
                baseStatsSummary = "Life Steal 2.3%",
                magicBonus = "Magic: All Weapons' +1% Life Steal",
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
                tier = ChipTier.Common,
                level = 1,
                count = 24,
                requiredCount = 3,
                baseStatsSummary = "Mine AoE ATK 27 | Hồi chiêu: 5.55s",
                magicBonus = "Magic: ATK +20%",
                rareBonus = "Rare: Cooldown -20% (Giảm hồi chiêu)",
                uniqueBonus = "Unique: ATK +55%",
                epicBonus = "Epic: ATK +144%"
            },
            // 11. Aiming Lens (has star ⭐ in inventory screenshot)
            new ChipItemData
            {
                id = 11,
                chipName = "Aiming Lens",
                iconKey = "aiming-lens",
                tier = ChipTier.Common,
                level = 1,
                count = 63,
                requiredCount = 3,
                hasStar = true,
                baseStatsSummary = "CRIT Rate +4% (Tỷ lệ chí mạng)",
                magicBonus = "Magic: All Weapons' CRIT Rate +3%",
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
                tier = ChipTier.Common,
                level = 1,
                count = 52,
                requiredCount = 3,
                baseStatsSummary = "ATK 40/giây (kéo dài 3s) | Hồi chiêu: 7.5s | Tồn tại: 4.2s",
                magicBonus = "Magic: AoE ATK Range +25%",
                rareBonus = "Rare: ATK Speed +10%",
                uniqueBonus = "Unique: AoE ATK Range +35%",
                epicBonus = "Epic: ATK Speed +20%"
            },
            // 13. Laser Eye
            new ChipItemData
            {
                id = 13,
                chipName = "Laser Eye",
                iconKey = "laser-eye",
                tier = ChipTier.Common,
                level = 1,
                count = 58,
                requiredCount = 3,
                baseStatsSummary = "ATK 5 | Tốc độ đánh: Very fast",
                magicBonus = "Magic: ATK +15%",
                rareBonus = "Rare: CRIT Rate +10%",
                uniqueBonus = "Unique: ATK +15%",
                epicBonus = "Epic: ATK +100%"
            },
            // 14. Biochemical Mine
            new ChipItemData
            {
                id = 14,
                chipName = "Biochemical Mine",
                iconKey = "biochemical-mine",
                tier = ChipTier.Common,
                level = 1,
                count = 48,
                requiredCount = 3,
                baseStatsSummary = "Khí độc ATK 14/giây | Tồn tại: 3s | Hồi chiêu: 7.7s",
                magicBonus = "Magic: AoE ATK Range +40%",
                rareBonus = "Rare: Cooldown -30%",
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
                level = 18,
                count = 19,
                requiredCount = 15,
                baseStatsSummary = "ATK 86 | Tốc độ đánh: Slow | Số mục tiêu: 1",
                magicBonus = "Magic: Enemies Attacked: +1 (+1 mục tiêu bị giật điện)",
                rareBonus = "Rare: ATK Speed +20%",
                uniqueBonus = "Unique: Enemies Attacked: +1",
                epicBonus = "Epic: ATK +100%"
            }
        };

        // Preset 3 equipped chips (Slots 1 to 10 as shown in screenshot)
        deckEquippedIds[2] = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        // Preset 1 & 2 fallback
        deckEquippedIds[0] = new int[] { 11, 12, 13, 14, 15, 1, 2, 3, 4, 5 };
        deckEquippedIds[1] = new int[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6 };
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
        if (detailUpgradeBtn != null) detailUpgradeBtn.onClick.AddListener(UpgradeSelectedDetailChip);
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
        if (energyText != null) energyText.text = $"{currentEnergy}/{currentMaxEnergy}";
        if (chipCurrencyText != null) chipCurrencyText.text = $"{currentChips:N0}";
        if (redCurrencyText != null) redCurrencyText.text = $"{currentRedChips:N0}";
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
                equippedSlots[i].Setup(chip, icon, frame, OpenDetailModal, QuickUpgradeChip);
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
        if (chip == null || !chip.CanUpgrade)
        {
            ShowToast("Not enough chip fragments!");
            return;
        }

        chip.Upgrade();
        currentChips += 100; // Small bonus
        RefreshTopBar();
        RefreshEquippedGrid();
        RefreshInventory();
        if (detailModal != null && detailModal.activeSelf && selectedDetailChip == chip)
        {
            RefreshDetailModal();
        }

        ShowToast($"Upgraded {chip.chipName} to LV.{chip.level:00}!");
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
        if (detailLevelText != null) detailLevelText.text = $"LEVEL {selectedDetailChip.level:00}";
        if (detailTierText != null) detailTierText.text = selectedDetailChip.tier.ToString().ToUpper();
        if (detailBaseStatsText != null) detailBaseStatsText.text = $"• {selectedDetailChip.baseStatsSummary}";

        if (detailMagicText != null) detailMagicText.text = $"• {selectedDetailChip.magicBonus}";
        if (detailRareText != null) detailRareText.text = $"• {selectedDetailChip.rareBonus}";
        if (detailUniqueText != null) detailUniqueText.text = $"• {selectedDetailChip.uniqueBonus}";
        if (detailEpicText != null) detailEpicText.text = $"• {selectedDetailChip.epicBonus}";

        if (detailUpgradeBtn != null)
        {
            detailUpgradeBtn.interactable = selectedDetailChip.CanUpgrade;
            if (detailUpgradeBtnText != null)
            {
                detailUpgradeBtnText.text = selectedDetailChip.requiredCount > 0
                    ? $"UPGRADE ({selectedDetailChip.count}/{selectedDetailChip.requiredCount})"
                    : "MAX LEVEL";
            }
        }

        bool isEquipped = deckEquippedIds[activeDeckIndex].Contains(selectedDetailChip.id);
        if (detailEquipBtnText != null)
        {
            detailEquipBtnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
        }
    }

    private void UpgradeSelectedDetailChip()
    {
        if (selectedDetailChip != null)
        {
            QuickUpgradeChip(selectedDetailChip);
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
        currentChips += gainedCurrency;
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
            case ChipTier.Common:
                return frameSprites.Length > 0 ? frameSprites[0] : null;
            case ChipTier.Magic:
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
