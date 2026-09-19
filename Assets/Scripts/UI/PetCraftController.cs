using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển giao diện Chế Tạo Pet (Crafting Pet) theo thiết kế Hình 3.
/// Quản lý việc lựa chọn pet trên lồng ấp, hiển thị nguyên liệu yêu cầu,
/// và thực thi chế tạo hoặc thoát.
/// Đã được refactor tích hợp với PetService và PlayerDataService.
/// </summary>
public class PetCraftController : MonoBehaviour
{
    [System.Serializable]
    public class CraftIngredientData
    {
        public int chipsetId;
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
        public int id;
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
        SyncWithPetService();
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
        PetService.OnPetDataChanged += HandlePetDataChanged;
        SelectPet(selectedPetIndex);
    }

    private void OnDisable()
    {
        PetService.OnPetDataChanged -= HandlePetDataChanged;
    }

    private void HandlePetDataChanged()
    {
        if (gameObject.activeInHierarchy)
        {
            SelectPet(selectedPetIndex);
        }
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
            SyncWithPetService();
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

        // Lấy nguyên liệu thời gian thực từ PlayerDataService
        PetData svcPet = PetService.GetPetData(selectedPetIndex);

        // Cập nhật nguyên liệu 1
        if (svcPet != null && svcPet.ingredients != null && svcPet.ingredients.Count > 0)
        {
            var ing1 = svcPet.ingredients[0];
            int currentCount = PlayerDataService.GetChipsetPieceCount(ing1.chipsetId);
            int reqCount = ing1.requiredCount;

            if (ingredientSlot1 != null) ingredientSlot1.SetActive(true);

            Sprite frame1 = ing1.frameSprite != null ? ing1.frameSprite : PetService.GetChipsetFrame(0);
            if (ing1Frame != null && frame1 != null)
                ing1Frame.sprite = frame1;

            Sprite icon1 = ing1.iconSprite != null ? ing1.iconSprite : PetService.GetChipsetIcon(ing1.chipsetId);
            if (ing1Icon != null)
            {
                ing1Icon.sprite = icon1;
                ing1Icon.enabled = (icon1 != null);
            }

            if (ing1Level != null) ing1Level.text = ing1.levelText;
            if (ing1Progress != null) ing1Progress.text = $"{currentCount}/{reqCount}";
            if (ing1ProgressFill != null) ing1ProgressFill.fillAmount = Mathf.Clamp01((float)currentCount / Mathf.Max(1, reqCount));
            if (ing1Name != null) ing1Name.text = ing1.chipsetName;
            if (ing1Count != null)
            {
                string colorHex = currentCount >= reqCount ? "#FFCC00" : "#FF3333";
                ing1Count.text = $"<color={colorHex}>{currentCount}</color>/{reqCount}";
            }
        }
        else if (ingredientSlot1 != null)
        {
            ingredientSlot1.SetActive(false);
        }

        // Cập nhật nguyên liệu 2
        if (svcPet != null && svcPet.ingredients != null && svcPet.ingredients.Count > 1)
        {
            var ing2 = svcPet.ingredients[1];
            int currentCount = PlayerDataService.GetChipsetPieceCount(ing2.chipsetId);
            int reqCount = ing2.requiredCount;

            if (ingredientSlot2 != null) ingredientSlot2.SetActive(true);

            Sprite frame2 = ing2.frameSprite != null ? ing2.frameSprite : PetService.GetChipsetFrame(1);
            if (ing2Frame != null && frame2 != null)
                ing2Frame.sprite = frame2;

            Sprite icon2 = ing2.iconSprite != null ? ing2.iconSprite : PetService.GetChipsetIcon(ing2.chipsetId);
            if (ing2Icon != null)
            {
                ing2Icon.sprite = icon2;
                ing2Icon.enabled = (icon2 != null);
            }

            if (ing2Level != null) ing2Level.text = ing2.levelText;
            if (ing2Progress != null) ing2Progress.text = $"{currentCount}/{reqCount}";
            if (ing2ProgressFill != null) ing2ProgressFill.fillAmount = Mathf.Clamp01((float)currentCount / Mathf.Max(1, reqCount));
            if (ing2Name != null) ing2Name.text = ing2.chipsetName;
            if (ing2Count != null)
            {
                string colorHex = currentCount >= reqCount ? "#FFCC00" : "#FF3333";
                ing2Count.text = $"<color={colorHex}>{currentCount}</color>/{reqCount}";
            }
        }
        else if (ingredientSlot2 != null)
        {
            ingredientSlot2.SetActive(false);
        }

        // Cập nhật trạng thái nút Craft
        bool isAlreadyOwned = PetService.IsPetOwned(selectedPetIndex);
        bool canCraft = PetService.CanCraft(selectedPetIndex);

        if (craftButton != null)
        {
            craftButton.interactable = canCraft;

            TMP_Text btnText = craftButton.GetComponentInChildren<TMP_Text>(true);
            if (btnText != null)
            {
                if (isAlreadyOwned)
                {
                    btnText.text = "Crafted";
                }
                else
                {
                    btnText.text = "Craft";
                }
            }
        }
    }

    private void OnCraftButtonClicked()
    {
        if (!PetService.CanCraft(selectedPetIndex))
        {
            Debug.LogWarning($"[PetCraftController] Không đủ nguyên liệu hoặc đã sở hữu Pet ID {selectedPetIndex}!");
            return;
        }

        bool success = PetService.CraftPet(selectedPetIndex);
        if (success)
        {
            string petName = craftablePets[selectedPetIndex] != null ? craftablePets[selectedPetIndex].petName : "Pet";

            // Reload UI ngay lập tức
            SelectPet(selectedPetIndex);
        }
    }

    public void SyncWithPetService()
    {
        var svcList = PetService.AllPets;
        if (craftablePets == null) craftablePets = new List<CraftablePetData>();

        // Nếu đã có cấu hình trong scene, bảo lưu sprite frame/icon
        bool hadExisting = craftablePets.Count >= 7;

        if (!hadExisting)
        {
            craftablePets.Clear();
            for (int i = 0; i < svcList.Count; i++)
            {
                var sp = svcList[i];
                var cp = new CraftablePetData
                {
                    id = sp.id,
                    petName = sp.petName,
                    description = sp.description,
                    levelText = sp.levelText,
                    progressText = sp.progressText,
                    progressFillRatio = sp.progressFillRatio,
                    petIcon = sp.petIcon,
                    frameSprite = sp.frameSprite
                };

                for (int j = 0; j < sp.ingredients.Count; j++)
                {
                    var si = sp.ingredients[j];
                    cp.ingredients.Add(new CraftIngredientData
                    {
                        chipsetId = si.chipsetId,
                        itemName = si.chipsetName,
                        levelText = si.levelText,
                        progressText = si.progressText,
                        progressFillRatio = si.progressFillRatio,
                        requiredCount = si.requiredCount,
                        frameSprite = si.frameSprite != null ? si.frameSprite : PetService.GetChipsetFrame(j == 0 ? 0 : 1),
                        iconSprite = si.iconSprite != null ? si.iconSprite : PetService.GetChipsetIcon(si.chipsetId)
                    });
                }

                craftablePets.Add(cp);
            }
        }
        else
        {
            // Cập nhật ID, tên, icon và frame theo PetService
            for (int i = 0; i < craftablePets.Count && i < svcList.Count; i++)
            {
                craftablePets[i].id = svcList[i].id;
                craftablePets[i].petName = svcList[i].petName;
                craftablePets[i].description = svcList[i].description;
                for (int j = 0; j < craftablePets[i].ingredients.Count && j < svcList[i].ingredients.Count; j++)
                {
                    var sIng = svcList[i].ingredients[j];
                    craftablePets[i].ingredients[j].chipsetId = sIng.chipsetId;
                    craftablePets[i].ingredients[j].itemName = sIng.chipsetName;
                    craftablePets[i].ingredients[j].requiredCount = sIng.requiredCount;
                    craftablePets[i].ingredients[j].iconSprite = sIng.iconSprite != null ? sIng.iconSprite : PetService.GetChipsetIcon(sIng.chipsetId);
                    craftablePets[i].ingredients[j].frameSprite = sIng.frameSprite != null ? sIng.frameSprite : PetService.GetChipsetFrame(j == 0 ? 0 : 1);
                }
            }
        }
    }

    public void InitializeDefaultPetData()
    {
        SyncWithPetService();
    }
}
