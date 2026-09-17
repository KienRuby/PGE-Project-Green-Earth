using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển giao diện Chế Tạo Pet (Crafting Pet) theo thiết kế Hình 3.
/// Quản lý việc lựa chọn pet trên lồng ấp, hiển thị nguyên liệu yêu cầu,
/// và thực thi chế tạo hoặc thoát.
/// </summary>
public class PetCraftController : MonoBehaviour
{
    [System.Serializable]
    public class CraftIngredientData
    {
        public string itemName;
        public Sprite frameSprite;
        public Sprite iconSprite;
        public string levelText;
        public string progressText;
        public float progressFillRatio;
        public int currentCount;
        public int requiredCount;
    }

    [System.Serializable]
    public class CraftablePetData
    {
        public string petName;
        public Sprite petIcon;
        public string description;
        public string levelText = "LV.01";
        public string progressText = "0/3";
        public float progressFillRatio = 0f;
        public Sprite frameSprite;
        public List<CraftIngredientData> ingredients = new List<CraftIngredientData>();
    }

    [Header("Sprites")]
    [SerializeField] public Sprite domeNormalSprite;
    [SerializeField] public Sprite domeSelectedSprite;

    [Header("Incubator Shelves (7 Pets)")]
    [SerializeField] public Button[] domeButtons = new Button[7];
    [SerializeField] public Image[] domeImages = new Image[7];
    [SerializeField] public Image[] domePetIcons = new Image[7];

    [Header("Crafting Pet Section")]
    [SerializeField] public Image craftingPetFrame;
    [SerializeField] public Image craftingPetIcon;
    [SerializeField] public TMP_Text craftingPetLevel;
    [SerializeField] public TMP_Text craftingPetProgress;
    [SerializeField] public Image craftingPetProgressFill;
    [SerializeField] public TMP_Text craftingPetName;
    [SerializeField] public TMP_Text craftingPetDesc;

    [Header("Ingredient 1 Slot")]
    [SerializeField] public GameObject ingredientSlot1;
    [SerializeField] public Image ing1Frame;
    [SerializeField] public Image ing1Icon;
    [SerializeField] public TMP_Text ing1Level;
    [SerializeField] public TMP_Text ing1Progress;
    [SerializeField] public Image ing1ProgressFill;
    [SerializeField] public TMP_Text ing1Name;
    [SerializeField] public TMP_Text ing1Count;

    [Header("Ingredient 2 Slot")]
    [SerializeField] public GameObject ingredientSlot2;
    [SerializeField] public Image ing2Frame;
    [SerializeField] public Image ing2Icon;
    [SerializeField] public TMP_Text ing2Level;
    [SerializeField] public TMP_Text ing2Progress;
    [SerializeField] public Image ing2ProgressFill;
    [SerializeField] public TMP_Text ing2Name;
    [SerializeField] public TMP_Text ing2Count;

    [Header("Action Buttons")]
    [SerializeField] public Button craftButton;
    [SerializeField] public Button exitButton;

    [Header("Pet Definitions")]
    [SerializeField] public List<CraftablePetData> craftablePets = new List<CraftablePetData>();

    public int selectedPetIndex = 4; // Mặc định là Cydog (index 4) khớp với Hình 3

    [Header("Reference layout (1080 x 1920)")]
    [SerializeField] private RectTransform contentRoot;

    private void OnRectTransformDimensionsChange()
    {
        FitReferenceLayout();
    }

    private void FitReferenceLayout()
    {
        if (contentRoot == null) return;
        var rect = ((RectTransform)transform).rect;
        float scale = Mathf.Min(rect.width / 1080f, rect.height / 1920f);
        contentRoot.localScale = Vector3.one * Mathf.Max(0.001f, scale);
    }

    private void Awake()
    {
        SetupButtonListeners();
        if (craftablePets == null || craftablePets.Count == 0)
        {
            InitializeDefaultPetData();
        }
    }

    private void Start()
    {
        AutoWireTriggerButtonIfMissing();
        SelectPet(selectedPetIndex);
    }

    private void AutoWireTriggerButtonIfMissing()
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform.root;
        foreach (var btn in searchRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name.Equals("CraftButton", StringComparison.OrdinalIgnoreCase))
            {
                btn.onClick.RemoveListener(OpenCraftScreenDefault);
                btn.onClick.AddListener(OpenCraftScreenDefault);
                btn.interactable = true;
                break;
            }
        }
    }

    private void OnEnable()
    {
        FitReferenceLayout();
        SelectPet(selectedPetIndex);
    }

    public void SetupButtonListeners()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(CloseCraftScreen);
            exitButton.onClick.AddListener(CloseCraftScreen);
        }

        if (craftButton != null)
        {
            craftButton.onClick.RemoveListener(OnCraftButtonClicked);
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }

        for (int i = 0; i < domeButtons.Length; i++)
        {
            int index = i;
            if (domeButtons[i] != null)
            {
                domeButtons[i].onClick.RemoveAllListeners();
                domeButtons[i].onClick.AddListener(() => SelectPet(index));
            }
        }
    }

    public void OpenCraftScreenDefault()
    {
        OpenCraftScreen(4);
    }

    public void OpenCraftScreen(int initialPetIndex = 4)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SelectPet(initialPetIndex);
    }

    public void CloseCraftScreen()
    {
        gameObject.SetActive(false);
    }

    public void SelectPet(int index)
    {
        if (craftablePets == null || craftablePets.Count == 0)
        {
            InitializeDefaultPetData();
        }

        if (index < 0 || index >= craftablePets.Count) return;
        selectedPetIndex = index;

        // Cập nhật trạng thái lồng ấp
        for (int i = 0; i < domeImages.Length; i++)
        {
            if (domeImages[i] != null)
            {
                bool isSelected = (i == selectedPetIndex);
                domeImages[i].sprite = isSelected ? domeSelectedSprite : domeNormalSprite;
            }
        }

        CraftablePetData pet = craftablePets[selectedPetIndex];
        if (pet == null) return;

        // Cập nhật khung thông tin Crafting Pet
        if (craftingPetFrame != null && pet.frameSprite != null) craftingPetFrame.sprite = pet.frameSprite;
        if (craftingPetIcon != null)
        {
            Sprite icon = pet.petIcon;
            if (icon == null && domePetIcons != null && index < domePetIcons.Length && domePetIcons[index] != null)
                icon = domePetIcons[index].sprite;
            craftingPetIcon.sprite = icon;
            craftingPetIcon.enabled = icon != null;
        }
        if (craftingPetLevel != null) craftingPetLevel.text = pet.levelText;
        if (craftingPetProgress != null) craftingPetProgress.text = pet.progressText;
        if (craftingPetProgressFill != null) craftingPetProgressFill.fillAmount = pet.progressFillRatio;
        if (craftingPetName != null) craftingPetName.text = pet.petName;
        if (craftingPetDesc != null) craftingPetDesc.text = pet.description;

        // Cập nhật nguyên liệu 1
        if (pet.ingredients != null && pet.ingredients.Count > 0)
        {
            var ing1 = pet.ingredients[0];
            if (ingredientSlot1 != null) ingredientSlot1.SetActive(true);
            if (ing1Frame != null && ing1.frameSprite != null) ing1Frame.sprite = ing1.frameSprite;
            if (ing1Icon != null && ing1.iconSprite != null) ing1Icon.sprite = ing1.iconSprite;
            if (ing1Level != null) ing1Level.text = ing1.levelText;
            if (ing1Progress != null) ing1Progress.text = ing1.progressText;
            if (ing1ProgressFill != null) ing1ProgressFill.fillAmount = ing1.progressFillRatio;
            if (ing1Name != null) ing1Name.text = ing1.itemName;
            if (ing1Count != null)
            {
                string colorHex = ing1.currentCount >= ing1.requiredCount ? "#FFCC00" : "#FF3333";
                ing1Count.text = $"<color={colorHex}>{ing1.currentCount}</color>/{ing1.requiredCount}";
            }
        }
        else if (ingredientSlot1 != null)
        {
            ingredientSlot1.SetActive(false);
        }

        // Cập nhật nguyên liệu 2
        if (pet.ingredients != null && pet.ingredients.Count > 1)
        {
            var ing2 = pet.ingredients[1];
            if (ingredientSlot2 != null) ingredientSlot2.SetActive(true);
            if (ing2Frame != null && ing2.frameSprite != null) ing2Frame.sprite = ing2.frameSprite;
            if (ing2Icon != null && ing2.iconSprite != null) ing2Icon.sprite = ing2.iconSprite;
            if (ing2Level != null) ing2Level.text = ing2.levelText;
            if (ing2Progress != null) ing2Progress.text = ing2.progressText;
            if (ing2ProgressFill != null) ing2ProgressFill.fillAmount = ing2.progressFillRatio;
            if (ing2Name != null) ing2Name.text = ing2.itemName;
            if (ing2Count != null)
            {
                string colorHex = ing2.currentCount >= ing2.requiredCount ? "#FFCC00" : "#FF3333";
                ing2Count.text = $"<color={colorHex}>{ing2.currentCount}</color>/{ing2.requiredCount}";
            }
        }
        else if (ingredientSlot2 != null)
        {
            ingredientSlot2.SetActive(false);
        }

        // Cập nhật trạng thái nút Craft
        bool canCraft = true;
        if (pet.ingredients != null)
        {
            foreach (var ing in pet.ingredients)
            {
                if (ing.currentCount < ing.requiredCount)
                {
                    canCraft = false;
                    break;
                }
            }
        }
        if (craftButton != null)
        {
            craftButton.interactable = canCraft;
        }
    }

    private void OnCraftButtonClicked()
    {
        CraftablePetData pet = craftablePets[selectedPetIndex];
        string petName = pet != null ? pet.petName : "Pet";
        Debug.Log($"[PetCraftController] Đang chế tạo: {petName}!");
    }

    public void InitializeDefaultPetData()
    {
        if (craftablePets == null) craftablePets = new List<CraftablePetData>();
        if (craftablePets.Count >= 7) return;
        craftablePets.Clear();

        string[] names = { "Pink Bat", "Green Slime", "Cyber Spider", "Viper Bot", "Cydog", "Iron Shell", "Turbo Snail" };
        string[] descs = {
            "Emits supersonic waves that confuse nearby enemies.",
            "Generates a sticky aura slowing enemy movements.",
            "Weaves plasma webs that trap and electrify foes.",
            "Spits venom darts that inflict continuous damage.",
            "Increases Bernard's Max HP.",
            "Forms an energy barrier absorbing incoming damage.",
            "Leaves a cryogenic trail that freezes pursuing enemies."
        };

        for (int i = 0; i < 7; i++)
        {
            var p = new CraftablePetData
            {
                petName = names[i],
                petIcon = domePetIcons != null && i < domePetIcons.Length && domePetIcons[i] != null
                    ? domePetIcons[i].sprite : null,
                frameSprite = craftingPetFrame != null ? craftingPetFrame.sprite : null,
                description = descs[i],
                levelText = "LV.01",
                progressText = "0/3",
                progressFillRatio = 0f
            };

            p.ingredients.Add(new CraftIngredientData
            {
                itemName = "Spiky discus",
                frameSprite = ing1Frame != null ? ing1Frame.sprite : null,
                iconSprite = ing1Icon != null ? ing1Icon.sprite : null,
                levelText = "LV.01",
                progressText = "7/9",
                progressFillRatio = 7f / 9f,
                currentCount = 7,
                requiredCount = 2
            });

            p.ingredients.Add(new CraftIngredientData
            {
                itemName = "Spiky discus",
                frameSprite = ing2Frame != null ? ing2Frame.sprite : null,
                iconSprite = ing2Icon != null ? ing2Icon.sprite : null,
                levelText = "LV.05",
                progressText = "1/15",
                progressFillRatio = 1f / 15f,
                currentCount = 1,
                requiredCount = 2
            });

            craftablePets.Add(p);
        }
    }
}
