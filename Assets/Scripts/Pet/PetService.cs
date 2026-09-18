using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PetCraftIngredient
{
    public int chipsetId;
    public string chipsetName;
    public int requiredCount = 2;
    public string levelText = "LV.01";
    public string progressText = "7/9";
    public float progressFillRatio = 0.77f;
    public Sprite frameSprite;
    public Sprite iconSprite;
}

[System.Serializable]
public class PetData
{
    public int id;
    public string petName;
    public string description;
    public Sprite petIcon;
    public Sprite cardSprite;
    public Sprite frameSprite;
    public string levelText = "LV.01";
    public string progressText = "0/3";
    public float progressFillRatio = 0f;
    public List<PetCraftIngredient> ingredients = new List<PetCraftIngredient>();
}

/// <summary>
/// Quản lý dữ liệu và trạng thái nghiệp vụ cho hệ thống Pet & Crafting.
/// Tách biệt hoàn toàn giữa Data/State và UI/View.
/// Đảm bảo mặc định người chơi chưa sở hữu bất kỳ Pet nào.
/// </summary>
public static class PetService
{
    public const string OwnedKeyPrefix = "PGE.Pet.Owned.";
    public const string EquippedSlotsKey = "PGE.Pet.EquippedSlots";
    public const string ActiveSlotKey = "PGE.Pet.ActiveSlotIndex";
    public const int MaxEquippedSlots = 3;

    public static event Action<int> OnPetCrafted;
    public static event Action OnPetDataChanged;
    public static event Action<int, int> OnPetEquippedChanged;

    private static List<PetData> allPets;

    public static IReadOnlyList<PetData> AllPets
    {
        get
        {
            if (allPets == null || allPets.Count == 0)
            {
                InitializeDatabase();
            }
            return allPets;
        }
    }

    static PetService()
    {
        InitializeDatabase();
    }

    public static void InitializeDatabase()
    {
        allPets = new List<PetData>();

        string[] names = { "Pink Bat", "Green Slime", "Cyber Spider", "Viper Bot", "Cydog", "Iron Shell", "Turbo Snail" };
        string[] descs = {
            "Emits supersonic waves that confuse nearby enemies.",
            "Generates a sticky aura slowing enemy movements.",
            "Weaves plasma webs that trap and electrify foes.",
            "Spits venom darts that inflict continuous damage.",
            "Increases Adam's Max HP.",
            "Forms an energy barrier absorbing incoming damage.",
            "Leaves a cryogenic trail that freezes pursuing enemies."
        };

        // Recipes: (Chipset 1 to 10 from ChipsetController)
        (int c1, string n1, int c2, string n2)[] recipes = {
            (3, "Rocket punch", 5, "Multigun"),           // Pink Bat
            (4, "Spinning blade", 9, "Energy cables"),    // Green Slime
            (6, "Gun turret", 10, "High-explosive mine"), // Cyber Spider
            (2, "Rifle", 9, "Energy cables"),             // Viper Bot
            (7, "Spiky discus", 8, "Shotgun"),            // Cydog
            (6, "Gun turret", 1, "Standard gun"),         // Iron Shell
            (4, "Spinning blade", 7, "Spiky discus")      // Turbo Snail
        };

        for (int i = 0; i < 7; i++)
        {
            var pet = new PetData
            {
                id = i,
                petName = names[i],
                description = descs[i],
                levelText = "LV.01",
                progressText = "0/3",
                progressFillRatio = 0f
            };

            var (c1, n1, c2, n2) = recipes[i];

            pet.ingredients.Add(new PetCraftIngredient
            {
                chipsetId = c1,
                chipsetName = n1,
                requiredCount = 2,
                levelText = "LV.01",
                progressText = "7/9",
                progressFillRatio = 7f / 9f,
                iconSprite = GetChipsetIcon(c1),
                frameSprite = GetChipsetFrame(0)
            });

            pet.ingredients.Add(new PetCraftIngredient
            {
                chipsetId = c2,
                chipsetName = n2,
                requiredCount = 2,
                levelText = "LV.05",
                progressText = "1/15",
                progressFillRatio = 1f / 15f,
                iconSprite = GetChipsetIcon(c2),
                frameSprite = GetChipsetFrame(1)
            });

            allPets.Add(pet);
        }

        LoadAssetSpritesIfAvailable();
    }

    private static ChipsetLevelVisualLibrary cachedVisualLib;

    public static ChipsetLevelVisualLibrary VisualLibrary
    {
        get
        {
            if (cachedVisualLib == null)
            {
                cachedVisualLib = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
#if UNITY_EDITOR
                if (cachedVisualLib == null)
                {
                    cachedVisualLib = UnityEditor.AssetDatabase.LoadAssetAtPath<ChipsetLevelVisualLibrary>("Assets/Resources/ChipsetLevelVisualLibrary.asset");
                }
#endif
            }
            return cachedVisualLib;
        }
    }

    public static Sprite GetChipsetIcon(int chipsetId)
    {
        if (chipsetId < 1 || chipsetId > 10) return null;
        var lib = VisualLibrary;
        if (lib != null && lib.primaryChipIcons != null && lib.primaryChipIcons.Length >= chipsetId)
        {
            Sprite s = lib.primaryChipIcons[chipsetId - 1];
            if (s != null) return s;
        }
#if UNITY_EDITOR
        string atlasPath = "Assets/Sprites/UI/Chipset/icon chipset.png";
        var allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>().ToArray();
        string[] searchKeywords = { "Standard", "Rifle", "Punch", "Blade", "Multi", "Turret", "Discus", "Shotgun", "Cables", "Mine" };
        if (chipsetId >= 1 && chipsetId <= searchKeywords.Length)
        {
            string kw = searchKeywords[chipsetId - 1];
            return allSprites.FirstOrDefault(s => s.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
        }
#endif
        return null;
    }

    public static Sprite GetChipsetFrame(int tierIndex)
    {
        var lib = VisualLibrary;
        if (lib != null && lib.mainMenuTierFrames != null && tierIndex >= 0 && tierIndex < lib.mainMenuTierFrames.Length)
        {
            Sprite f = lib.mainMenuTierFrames[tierIndex];
            if (f != null) return f;
        }
#if UNITY_EDITOR
        string frameAtlasPath = "Assets/Sprites/UI/Chipset/khung chipset (1).png";
        var allFrames = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(frameAtlasPath).OfType<Sprite>().ToArray();
        if (allFrames != null && tierIndex >= 0 && tierIndex < allFrames.Length)
        {
            return allFrames[tierIndex];
        }
#endif
        return null;
    }

    private static void LoadAssetSpritesIfAvailable()
    {
        // Đồng bộ icon và frame chuẩn xác cho nguyên liệu của toàn bộ Pet
        for (int i = 0; i < allPets.Count; i++)
        {
            for (int j = 0; j < allPets[i].ingredients.Count; j++)
            {
                var ing = allPets[i].ingredients[j];
                if (ing.iconSprite == null)
                {
                    ing.iconSprite = GetChipsetIcon(ing.chipsetId);
                }
                if (ing.frameSprite == null)
                {
                    ing.frameSprite = GetChipsetFrame(j == 0 ? 0 : 1);
                }
            }
        }
#if UNITY_EDITOR
        string[] spriteNames = { "Pet_Bat", "Pet_Slime", "Pet_Spider", "Pet_Snake", "Pet_Dog", "Pet_Turtle", "Pet_Snail" };
        string[] cardNames = { "Card_Pet_Bat", "Card_Pet_Bat", "Card_Pet_Bat", "Card_Pet_Bat", "Card_Pet_Dog", "Card_Pet_Dog", "Card_Pet_Dog" };

        Sprite emptyCardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/RobotPet_Sliced/Card_Slot_Empty.png");
        Sprite defaultFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/Craft_Sliced/Panel_CraftingPet.png");

        for (int i = 0; i < allPets.Count && i < spriteNames.Length; i++)
        {
            if (allPets[i].petIcon == null)
            {
                allPets[i].petIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Buddy/Craft_Sliced/{spriteNames[i]}.png");
            }
            if (allPets[i].frameSprite == null)
            {
                allPets[i].frameSprite = defaultFrame;
            }
            if (allPets[i].cardSprite == null)
            {
                Sprite directCard = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Buddy/RobotPet_Sliced/{cardNames[i]}.png");
                allPets[i].cardSprite = directCard != null ? directCard : emptyCardSprite;
            }
        }
#endif
    }

    public static bool IsPetOwned(int petId)
    {
        return PlayerPrefs.GetInt(OwnedKeyPrefix + petId, 0) == 1;
    }

    public static void SetPetOwned(int petId, bool owned)
    {
        PlayerPrefs.SetInt(OwnedKeyPrefix + petId, owned ? 1 : 0);
        PlayerPrefs.Save();
        OnPetDataChanged?.Invoke();
    }

    public static List<int> GetOwnedPetIds()
    {
        List<int> result = new List<int>();
        for (int i = 0; i < AllPets.Count; i++)
        {
            if (IsPetOwned(allPets[i].id))
            {
                result.Add(allPets[i].id);
            }
        }
        return result;
    }

    public static PetData GetPetData(int petId)
    {
        return AllPets.FirstOrDefault(p => p.id == petId);
    }

    public static bool CanCraft(int petId)
    {
        if (IsPetOwned(petId))
        {
            return false;
        }

        PetData pet = GetPetData(petId);
        if (pet == null || pet.ingredients == null || pet.ingredients.Count == 0)
        {
            return false;
        }

        foreach (var ing in pet.ingredients)
        {
            int currentCount = PlayerDataService.GetChipsetPieceCount(ing.chipsetId);
            if (currentCount < ing.requiredCount)
            {
                return false;
            }
        }

        return true;
    }

    public static bool CraftPet(int petId)
    {
        if (!CanCraft(petId))
        {
            return false;
        }

        PetData pet = GetPetData(petId);
        if (pet == null) return false;

        foreach (var ing in pet.ingredients)
        {
            PlayerDataService.TrySpendChipsetPieces(ing.chipsetId, ing.requiredCount);
        }

        PlayerPrefs.SetInt(OwnedKeyPrefix + petId, 1);
        PlayerPrefs.Save();

        Debug.Log($"[PetService] Chế tạo thành công Pet: {pet.petName} (ID: {petId})!");

        OnPetCrafted?.Invoke(petId);
        OnPetDataChanged?.Invoke();

        return true;
    }

    public static int[] LoadEquippedSlots()
    {
        string raw = PlayerPrefs.GetString(EquippedSlotsKey, "-1,-1,-1");
        string[] parts = raw.Split(',');
        int[] result = new int[MaxEquippedSlots];
        for (int i = 0; i < MaxEquippedSlots; i++)
        {
            if (i < parts.Length && int.TryParse(parts[i].Trim(), out int id))
            {
                result[i] = (id >= 0 && IsPetOwned(id)) ? id : -1;
            }
            else
            {
                result[i] = -1;
            }
        }
        return result;
    }

    public static void SaveEquippedSlots(int[] slots)
    {
        if (slots == null || slots.Length != MaxEquippedSlots) return;
        string raw = string.Join(",", slots);
        PlayerPrefs.SetString(EquippedSlotsKey, raw);
        PlayerPrefs.Save();
    }

    public static int GetEquippedPetId(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxEquippedSlots) return -1;
        int[] slots = LoadEquippedSlots();
        return slots[slotIndex];
    }

    public static bool IsPetEquipped(int petId)
    {
        if (petId < 0) return false;
        int[] slots = LoadEquippedSlots();
        return slots.Any(id => id == petId);
    }

    public static int GetEquippedSlotIndex(int petId)
    {
        if (petId < 0) return -1;
        int[] slots = LoadEquippedSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == petId) return i;
        }
        return -1;
    }

    public static bool EquipPet(int slotIndex, int petId)
    {
        if (slotIndex < 0 || slotIndex >= MaxEquippedSlots) return false;

        if (petId >= 0 && !IsPetOwned(petId))
        {
            Debug.LogWarning($"[PetService] Không thể trang bị Pet ID {petId} vì người chơi chưa sở hữu!");
            return false;
        }

        int[] slots = LoadEquippedSlots();

        if (petId >= 0)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == petId && i != slotIndex)
                {
                    slots[i] = -1;
                }
            }
        }

        slots[slotIndex] = petId;
        SaveEquippedSlots(slots);

        OnPetEquippedChanged?.Invoke(slotIndex, petId);
        OnPetDataChanged?.Invoke();
        return true;
    }

    public static void UnequipPet(int slotIndex)
    {
        EquipPet(slotIndex, -1);
    }

    public static int ActiveSlotIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 0), 0, MaxEquippedSlots - 1);
        set
        {
            PlayerPrefs.SetInt(ActiveSlotKey, Mathf.Clamp(value, 0, MaxEquippedSlots - 1));
            PlayerPrefs.Save();
        }
    }

    public static void ResetPetDataForTesting()
    {
        for (int i = 0; i < 20; i++)
        {
            PlayerPrefs.DeleteKey(OwnedKeyPrefix + i);
        }
        PlayerPrefs.DeleteKey(EquippedSlotsKey);
        PlayerPrefs.DeleteKey(ActiveSlotKey);
        PlayerPrefs.Save();
        OnPetDataChanged?.Invoke();
    }
}
