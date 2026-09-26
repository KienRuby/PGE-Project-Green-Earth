using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển toàn bộ giao diện Tab Robot Pet (RobotPetPanel).
/// Quản lý việc trang bị Pet vào các slot 1, 2, 3.
/// CHỈ hiển thị những con Pet mà người chơi ĐÃ CRAFT THÀNH CÔNG (đã sở hữu).
/// Những Pet chưa craft tuyệt đối không xuất hiện trong danh sách để trang bị.
/// </summary>
public class RobotPetController : MonoBehaviour
{
    [Header("Slots Trang bị (Slot 1, 2, 3)")]
    [SerializeField] public Button[] slotButtons = new Button[3];
    [SerializeField] public Image[] slotImages = new Image[3];
    [SerializeField] public TMP_Text[] slotTexts = new TMP_Text[3];

    [Header("Sprite trạng thái Slot")]
    [SerializeField] public Sprite slotNormalSprite;
    [SerializeField] public Sprite slotActiveSprite;
    [SerializeField] public Color slotActiveColor = new Color(1f, 0.8f, 0.2f, 1f); // Vàng cam sáng
    [SerializeField] public Color slotNormalColor = new Color(0.9f, 0.45f, 0.35f, 1f); // Cam đỏ

    [Header("Sprite số 1, 2, 3 chuẩn Preset")]
    [SerializeField] public Sprite preset1YellowSprite;
    [SerializeField] public Sprite preset1RedSprite;
    [SerializeField] public Sprite preset2YellowSprite;
    [SerializeField] public Sprite preset2RedSprite;
    [SerializeField] public Sprite preset3YellowSprite;
    [SerializeField] public Sprite preset3RedSprite;

    [Header("Bảng thông tin chi tiết (PetDetailPanel)")]
    [SerializeField] public GameObject detailPanel;
    [SerializeField] public Image selectedPetCardImage;
    [SerializeField] public Image selectedPetIconImage;
    [SerializeField] public TMP_Text petNameText;
    [SerializeField] public TMP_Text petDescText;
    [SerializeField] public Button craftButton;
    [SerializeField] public Button infoButton;

    [Header("Nút sắp xếp")]
    [SerializeField] public Button byTierButton;
    [SerializeField] public Button byQuantityButton;

    [Header("Vùng chứa danh sách Pet sở hữu (Inventory)")]
    [SerializeField] public Transform inventoryContainer;
    [SerializeField] public GameObject emptyStateHint;
    [SerializeField] public GameObject petCardPrefab;

    [Header("Thanh cuộn Inventory")]
    [SerializeField] public ScrollRect inventoryScrollRect;
    [SerializeField] public RectTransform inventoryViewport;
    [SerializeField] public RectTransform inventoryContent;

    [Header("Cấu hình vị trí hàng Card Pet đã sở hữu")]
    [SerializeField] public float cardPosY = -1010f;
    [SerializeField] public float cardWidth = 157f;
    [SerializeField] public float cardHeight = 197f;
    [SerializeField] public float cardSpacing = 110f;
    [SerializeField] public int columns = 3;
    [SerializeField] public float cardRowSpacing = 225f;

    [Header("Sprite tài nguyên")]
    [SerializeField] public Sprite emptyCardFrameSprite;
    [SerializeField] public Sprite defaultPetIcon;

    private int activeSlotIndex = 0; // 0: Slot 1, 1: Slot 2, 2: Slot 3
    private int selectedPetId = 4;   // Mặc định Cydog (ID 4)
    private List<GameObject> spawnedCards = new List<GameObject>();
    private bool isSortedByQuantity = false;

    private static TMP_FontAsset cachedFont;
    private static Material cachedStrokeMaterial;

    private void Awake()
    {
        EnsureScrollView();
        HidePlaceholderCards();
        AutoWireReferencesIfMissing();
        SetupButtonListeners();
    }

    private void Start()
    {
        activeSlotIndex = PetService.ActiveSlotIndex;
        RefreshAll();
    }

    private void OnEnable()
    {
        PetService.OnPetCrafted += HandlePetCrafted;
        PetService.OnPetDataChanged += HandlePetDataChanged;
        PetService.OnPetEquippedChanged += HandlePetEquippedChanged;

        activeSlotIndex = PetService.ActiveSlotIndex;
        RefreshAll();
    }

    private void OnDisable()
    {
        PetService.OnPetCrafted -= HandlePetCrafted;
        PetService.OnPetDataChanged -= HandlePetDataChanged;
        PetService.OnPetEquippedChanged -= HandlePetEquippedChanged;
    }

    private void HandlePetCrafted(int petId)
    {
        selectedPetId = petId;

        // Nếu slot đang chọn còn trống, tự động trang bị luôn Pet vừa chế tạo
        if (PetService.GetEquippedPetId(activeSlotIndex) == -1)
        {
            PetService.EquipPet(activeSlotIndex, petId);
        }

        RefreshAll();
    }

    private void HandlePetDataChanged()
    {
        RefreshAll();
    }

    private void HandlePetEquippedChanged(int slot, int petId)
    {
        RefreshSlots();
        RefreshInventory();
    }

    public void SetupButtonListeners()
    {
        // 3 nút Slot 1, 2, 3
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            if (slotButtons[i] != null)
            {
                if (slotImages != null && i < slotImages.Length && slotImages[i] != null) slotImages[i].raycastTarget = true;
                if (slotButtons[i].targetGraphic != null) slotButtons[i].targetGraphic.raycastTarget = true;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotButtonClicked(index));
            }
        }

        // Nút Craft trong PetDetailPanel
        if (craftButton != null)
        {
            craftButton.onClick.RemoveListener(OpenCraftScreen);
            craftButton.onClick.AddListener(OpenCraftScreen);
        }

        // Nút Info trong PetDetailPanel
        if (infoButton != null)
        {
            Image infoImg = infoButton.GetComponent<Image>() ?? infoButton.targetGraphic as Image;
            if (infoImg != null) infoImg.raycastTarget = true;
            if (infoButton.targetGraphic != null) infoButton.targetGraphic.raycastTarget = true;
            infoButton.onClick.RemoveListener(OpenInfoModal);
            infoButton.onClick.AddListener(OpenInfoModal);
        }

        // Nút sắp xếp
        if (byTierButton != null)
        {
            Image img = byTierButton.GetComponent<Image>() ?? byTierButton.targetGraphic as Image;
            if (img != null) img.raycastTarget = true;
            if (byTierButton.targetGraphic != null) byTierButton.targetGraphic.raycastTarget = true;
            byTierButton.onClick.RemoveListener(OnByTierClicked);
            byTierButton.onClick.AddListener(OnByTierClicked);
        }

        if (byQuantityButton != null)
        {
            Image img = byQuantityButton.GetComponent<Image>() ?? byQuantityButton.targetGraphic as Image;
            if (img != null) img.raycastTarget = true;
            if (byQuantityButton.targetGraphic != null) byQuantityButton.targetGraphic.raycastTarget = true;
            byQuantityButton.onClick.RemoveListener(OnByQuantityClicked);
            byQuantityButton.onClick.AddListener(OnByQuantityClicked);
        }
    }

    public void OnSlotButtonClicked(int slotIndex)
    {
        activeSlotIndex = Mathf.Clamp(slotIndex, 0, PetService.MaxEquippedSlots - 1);
        PetService.ActiveSlotIndex = activeSlotIndex;

        // Nếu slot này đã có Pet, chọn luôn Pet đó để xem chi tiết
        int equippedPetId = PetService.GetEquippedPetId(activeSlotIndex);
        if (equippedPetId >= 0 && PetService.IsPetOwned(equippedPetId))
        {
            selectedPetId = equippedPetId;
        }

        RefreshAll();
    }

    public void OpenCraftScreen()
    {
        PetCraftController craftCtrl = FindObjectOfType<PetCraftController>(true);
        if (craftCtrl != null)
        {
            craftCtrl.OpenCraftScreen(selectedPetId >= 0 ? selectedPetId : 4);
        }
        else
        {
            Debug.LogWarning("[RobotPetController] Không tìm thấy PetCraftController trong Scene!");
        }
    }

    public void OpenInfoModal()
    {
        if (RobotPetInfoModalController.Instance != null)
        {
            RobotPetInfoModalController.Instance.Show();
        }
        else
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            var modal = RobotPetInfoModalController.EnsureModalInCanvas(canvas);
            if (modal != null) modal.Show();
        }
    }

    private void OnByTierClicked()
    {
        isSortedByQuantity = false;
        RefreshInventory();
    }

    private void OnByQuantityClicked()
    {
        isSortedByQuantity = true;
        RefreshInventory();
    }

    public void RefreshAll()
    {
        RefreshSlots();
        RefreshDetailPanel();
        RefreshInventory();
    }

    public void LoadPresetSpritesIfMissing()
    {
        if (preset1YellowSprite != null && preset1RedSprite != null &&
            preset2YellowSprite != null && preset2RedSprite != null &&
            preset3YellowSprite != null && preset3RedSprite != null)
            return;

        Sprite[] allLoadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in allLoadedSprites)
        {
            if (s == null) continue;
            if (preset1YellowSprite == null && s.name.Equals("1 Yellow", StringComparison.OrdinalIgnoreCase)) preset1YellowSprite = s;
            else if (preset1RedSprite == null && s.name.Equals("1 Red", StringComparison.OrdinalIgnoreCase)) preset1RedSprite = s;
            else if (preset2YellowSprite == null && s.name.Equals("2 Yellow", StringComparison.OrdinalIgnoreCase)) preset2YellowSprite = s;
            else if (preset2RedSprite == null && s.name.Equals("2 Red", StringComparison.OrdinalIgnoreCase)) preset2RedSprite = s;
            else if (preset3YellowSprite == null && s.name.Equals("3 Yellow", StringComparison.OrdinalIgnoreCase)) preset3YellowSprite = s;
            else if (preset3RedSprite == null && s.name.Equals("3 Red", StringComparison.OrdinalIgnoreCase)) preset3RedSprite = s;
        }

#if UNITY_EDITOR
        if (preset1YellowSprite == null || preset1RedSprite == null ||
            preset2YellowSprite == null || preset2RedSprite == null ||
            preset3YellowSprite == null || preset3RedSprite == null)
        {
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
        }
#endif
    }

    /// <summary>
    /// Cập nhật hiển thị 3 Slot [ 1 ] [ 2 ] [ 3 ]
    /// </summary>
    public void RefreshSlots()
    {
        LoadPresetSpritesIfMissing();

        for (int i = 0; i < slotButtons.Length; i++)
        {
            bool isActive = (i == activeSlotIndex);

            if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
            {
                slotImages[i].raycastTarget = true;
                slotImages[i].color = Color.white; // Luôn giữ màu gốc sắc nét của Sprite chuẩn

                Sprite targetSprite = null;
                if (i == 0) targetSprite = isActive ? preset1YellowSprite : preset1RedSprite;
                else if (i == 1) targetSprite = isActive ? preset2YellowSprite : preset2RedSprite;
                else if (i == 2) targetSprite = isActive ? preset3YellowSprite : preset3RedSprite;

                if (targetSprite != null)
                {
                    slotImages[i].sprite = targetSprite;
                }
            }

            if (slotTexts != null && i < slotTexts.Length && slotTexts[i] != null)
            {
                // Nếu sprite đã có sẵn số pixel art (1, 2, 3), xóa text đè lên để tránh bị nhân đôi / lệch font
                if (preset1YellowSprite != null)
                {
                    slotTexts[i].text = string.Empty;
                }
                else
                {
                    slotTexts[i].text = (i + 1).ToString();
                    slotTexts[i].color = isActive ? new Color(0.1f, 0.1f, 0.1f, 1f) : Color.white;
                }
            }
        }
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết Pet được chọn trong PetDetailPanel.
    /// </summary>
    public void RefreshDetailPanel()
    {
        // Kiểm tra xem slot đang chọn (activeSlotIndex: 0, 1, hoặc 2) có Pet nào được trang bị không
        int equippedPetId = PetService.GetEquippedPetId(activeSlotIndex);
        bool hasEquippedPet = (equippedPetId >= 0 && PetService.IsPetOwned(equippedPetId));

        if (hasEquippedPet)
        {
            PetData pet = PetService.GetPetData(equippedPetId);
            if (pet != null)
            {
                if (petNameText != null) petNameText.text = pet.petName;
                if (petDescText != null) petDescText.text = pet.description;

                bool hasBakedCard = pet.cardSprite != null && !pet.cardSprite.name.Contains("Empty");
                if (selectedPetCardImage != null)
                {
                    selectedPetCardImage.sprite = hasBakedCard ? pet.cardSprite : emptyCardFrameSprite;
                    selectedPetCardImage.color = Color.white;
                }

                if (selectedPetIconImage != null)
                {
                    if (hasBakedCard)
                    {
                        selectedPetIconImage.enabled = false;
                    }
                    else
                    {
                        Sprite icon = pet.petIcon != null ? pet.petIcon : defaultPetIcon;
                        selectedPetIconImage.sprite = icon;
                        selectedPetIconImage.enabled = icon != null;
                    }
                }
                return;
            }
        }

        // Nếu slot đang chọn CHƯA trang bị Pet nào:
        // TUYỆT ĐỐI KHÔNG hiện hình con chó (Cydog)! Hiện khung rỗng và thông báo slot trống
        if (selectedPetCardImage != null)
        {
            if (emptyCardFrameSprite != null)
            {
                selectedPetCardImage.sprite = emptyCardFrameSprite;
            }
            selectedPetCardImage.color = new Color(1f, 1f, 1f, 0.7f);
        }

        if (selectedPetIconImage != null)
        {
            selectedPetIconImage.enabled = false;
        }

        if (petNameText != null)
        {
            petNameText.text = $"Slot {activeSlotIndex + 1} (Empty)";
        }

        if (petDescText != null)
        {
            petDescText.text = "Chưa có Robot Pet nào được trang bị vào slot này.\nHãy chế tạo và trang bị Pet!";
        }
    }

    public void HidePlaceholderCards()
    {
        string[] placeholders = { "RobotCard", "BatCard", "DogCard" };
        foreach (var name in placeholders)
        {
            Transform t = transform.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Cập nhật danh sách Pet trong Inventory:
    /// QUY TẮC BẮT BUỘC: CHỈ hiển thị những Pet mà người chơi ĐÃ CRAFT THÀNH CÔNG.
    /// Nếu chưa sở hữu con nào, danh sách hoàn toàn rỗng.
    /// </summary>
    public void RefreshInventory()
    {
        EnsureScrollView();
        HidePlaceholderCards();
        List<int> ownedIds = PetService.GetOwnedPetIds();

        // Xóa các card cũ đã tạo
        foreach (var c in spawnedCards)
        {
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
        spawnedCards.Clear();

        if (inventoryContent != null)
        {
            for (int i = inventoryContent.childCount - 1; i >= 0; i--)
            {
                Transform child = inventoryContent.GetChild(i);
                if (child != null && child.name.StartsWith("Card_Pet_"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        if (inventoryContainer == null) return;

        // Nếu chưa sở hữu Pet nào
        if (ownedIds == null || ownedIds.Count == 0)
        {
            if (emptyStateHint != null)
            {
                emptyStateHint.SetActive(true);
            }
            if (inventoryContent != null)
            {
                inventoryContent.sizeDelta = new Vector2(inventoryContent.sizeDelta.x, 500f);
            }
            return;
        }

        if (emptyStateHint != null)
        {
            emptyStateHint.SetActive(false);
        }

        // Lấy danh sách PetData đã sở hữu
        List<PetData> ownedPets = new List<PetData>();
        foreach (int id in ownedIds)
        {
            PetData p = PetService.GetPetData(id);
            if (p != null) ownedPets.Add(p);
        }

        // Sắp xếp
        if (isSortedByQuantity)
        {
            ownedPets = ownedPets.OrderByDescending(p => p.id).ToList();
        }
        else
        {
            ownedPets = ownedPets.OrderBy(p => p.id).ToList();
        }

        // Render từng card cho Pet đã sở hữu
        for (int i = 0; i < ownedPets.Count; i++)
        {
            PetData pet = ownedPets[i];
            int pId = pet.id;
            GameObject cardObj = CreatePetCardUI(pet, i, ownedPets.Count);
            spawnedCards.Add(cardObj);
        }

        // Cập nhật chiều cao của Content trong ScrollView để cuộn mượt mà
        int cols = Mathf.Max(1, columns);
        int rowCount = (ownedPets.Count + cols - 1) / cols;
        float totalHeight = Mathf.Max(430f, rowCount * cardRowSpacing + 20f);
        if (inventoryContent != null)
        {
            inventoryContent.sizeDelta = new Vector2(inventoryContent.sizeDelta.x, totalHeight);
        }
    }

    private GameObject CreatePetCardUI(PetData pet, int index, int totalCount)
    {
        GameObject cardGo = new GameObject($"Card_Pet_{pet.petName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cardGo.transform.SetParent(inventoryContainer, false);

        RectTransform rect = cardGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        // Tính vị trí X và Y theo lưới cột (mặc định 3 cột mỗi hàng)
        int cols = Mathf.Max(1, columns);
        int col = index % cols;
        int row = index / cols;

        float xPos;
        if (totalCount < cols)
        {
            // Nếu tổng số card ít hơn số cột (1 hoặc 2 card), căn giữa dòng đầu tiên
            float totalWidth = totalCount * cardWidth + (totalCount - 1) * cardSpacing;
            float startX = -totalWidth / 2f + cardWidth / 2f;
            xPos = startX + index * (cardWidth + cardSpacing);
        }
        else
        {
            // Lưới 3 cột chuẩn: col 0 = -267, col 1 = 0, col 2 = +267 (khớp với cardSpacing 110f và cardWidth 157f)
            float startX = -(cols - 1) * (cardWidth + cardSpacing) / 2f;
            xPos = startX + col * (cardWidth + cardSpacing);
        }

        // Bắt đầu từ 0f bên dưới Viewport (đã cách 2 nút By Tier / By Quantity một khoảng an toàn)
        float yPos = inventoryScrollRect != null ? (-row * cardRowSpacing) : (cardPosY - row * cardRowSpacing);
        rect.anchoredPosition = new Vector2(xPos, yPos);
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);

        Image cardImg = cardGo.GetComponent<Image>();
        bool hasBakedCard = pet.cardSprite != null && !pet.cardSprite.name.Contains("Empty");
        cardImg.sprite = hasBakedCard ? pet.cardSprite : emptyCardFrameSprite;
        cardImg.preserveAspect = true;

        EnsureFontAndMaterial();

        // Level Text
        GameObject lvlGo = new GameObject("Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lvlGo.transform.SetParent(cardGo.transform, false);
        RectTransform lvlRect = lvlGo.GetComponent<RectTransform>();
        lvlRect.anchorMin = new Vector2(0f, 1f);
        lvlRect.anchorMax = new Vector2(1f, 1f);
        lvlRect.pivot = new Vector2(0.5f, 1f);
        lvlRect.anchoredPosition = new Vector2(0f, -10f);
        lvlRect.sizeDelta = new Vector2(0f, 36f);

        TextMeshProUGUI lvlText = lvlGo.GetComponent<TextMeshProUGUI>();
        lvlText.text = pet.levelText;
        lvlText.font = cachedFont;
        if (cachedStrokeMaterial != null) lvlText.fontSharedMaterial = cachedStrokeMaterial;
        lvlText.fontSize = 22f;
        lvlText.fontStyle = FontStyles.Bold;
        lvlText.color = Color.white;
        lvlText.alignment = TextAlignmentOptions.Center;
        lvlText.raycastTarget = false;

        // Count / Equipped Tag Text
        GameObject countGo = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        countGo.transform.SetParent(cardGo.transform, false);
        RectTransform countRect = countGo.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(0.5f, 0f);
        countRect.anchoredPosition = new Vector2(0f, 35f);
        countRect.sizeDelta = new Vector2(0f, 32f);

        TextMeshProUGUI countText = countGo.GetComponent<TextMeshProUGUI>();
        int equippedSlot = PetService.GetEquippedSlotIndex(pet.id);
        if (equippedSlot >= 0)
        {
            countText.text = $"[Slot {equippedSlot + 1}]";
            countText.color = new Color(1f, 0.85f, 0.3f, 1f); // Màu vàng trang bị
        }
        else
        {
            countText.text = pet.progressText;
            countText.color = Color.white;
        }
        countText.font = cachedFont;
        if (cachedStrokeMaterial != null) countText.fontSharedMaterial = cachedStrokeMaterial;
        countText.fontSize = 20f;
        countText.fontStyle = FontStyles.Bold;
        countText.alignment = TextAlignmentOptions.Center;
        countText.raycastTarget = false;

        // Nếu Card không có sprite nướng sẵn (Slime, Spider, Snake, Turtle, Snail), thêm Image PetIcon ở giữa
        if (!hasBakedCard && pet.petIcon != null)
        {
            GameObject iconGo = new GameObject("PetIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(cardGo.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 15f);
            iconRect.sizeDelta = new Vector2(100f, 100f);

            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = pet.petIcon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        // Button listener: Click vào Pet card đã sở hữu => Chọn và Trang bị vào activeSlotIndex
        Button cardBtn = cardGo.GetComponent<Button>();
        int petId = pet.id;
        cardBtn.onClick.AddListener(() => OnPetCardClicked(petId));

        return cardGo;
    }

    /// <summary>
    /// Xử lý khi người chơi bấm vào một Card Pet trong danh sách đã sở hữu:
    /// 1. Cập nhật chi tiết Pet được chọn lên bảng PetDetailPanel.
    /// 2. Trang bị Pet đó vào Slot đang kích hoạt (Slot 1, 2, hoặc 3).
    /// </summary>
    public void OnPetCardClicked(int petId)
    {
        selectedPetId = petId;

        // Kiểm tra nếu Pet này đã trang bị ở slot hiện tại thì unequip
        int currentSlot = PetService.GetEquippedSlotIndex(petId);
        if (currentSlot == activeSlotIndex)
        {
            PetService.UnequipPet(activeSlotIndex);
        }
        else
        {
            PetService.EquipPet(activeSlotIndex, petId);
        }

        RefreshAll();
    }

    private void EnsureFontAndMaterial()
    {
        if (cachedFont == null)
        {
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts/Nunito/Nunito SDF");
#if UNITY_EDITOR
            if (cachedFont == null)
            {
                cachedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
            }
#endif
        }

        if (cachedStrokeMaterial == null)
        {
            cachedStrokeMaterial = Resources.Load<Material>("Fonts/Nunito/Nunito SDF - Stroke");
#if UNITY_EDITOR
            if (cachedStrokeMaterial == null)
            {
                cachedStrokeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");
            }
#endif
        }
    }

    public void EnsureScrollView()
    {
        if (inventoryScrollRect != null && inventoryContent != null)
        {
            // Luôn đảm bảo vị trí bắt đầu từ -1005f (dưới 2 nút By Tier / By Quantity) và chiều cao 430f
            RectTransform srt = inventoryScrollRect.GetComponent<RectTransform>();
            if (srt != null)
            {
                srt.anchorMin = new Vector2(0.5f, 1f);
                srt.anchorMax = new Vector2(0.5f, 1f);
                srt.pivot = new Vector2(0.5f, 1f);
                srt.anchoredPosition = new Vector2(0f, -1005f);
                srt.sizeDelta = new Vector2(1000f, 430f);
            }
            inventoryContainer = inventoryContent;
            return;
        }

        Transform existingScroll = transform.Find("InventoryScrollView");
        GameObject scrollGo;
        if (existingScroll != null)
        {
            scrollGo = existingScroll.gameObject;
        }
        else
        {
            scrollGo = new GameObject("InventoryScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
        }

        RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.5f, 1f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 1f);
        scrollRectTransform.anchoredPosition = new Vector2(0f, -1005f);
        scrollRectTransform.sizeDelta = new Vector2(1000f, 430f);

        // Đảm bảo ScrollView vẽ trước (nằm dưới) 2 nút By Tier và By Quantity
        Transform bt = transform.Find("RobotByTier");
        if (bt != null)
        {
            scrollGo.transform.SetSiblingIndex(Mathf.Max(0, bt.GetSiblingIndex() - 1));
        }

        inventoryScrollRect = scrollGo.GetComponent<ScrollRect>();
        inventoryScrollRect.horizontal = false;
        inventoryScrollRect.vertical = true;
        inventoryScrollRect.movementType = ScrollRect.MovementType.Elastic;
        inventoryScrollRect.elasticity = 0.1f;
        inventoryScrollRect.inertia = true;
        inventoryScrollRect.decelerationRate = 0.135f;
        inventoryScrollRect.scrollSensitivity = 30f;

        // Viewport
        Transform vpTransform = scrollGo.transform.Find("Viewport");
        GameObject vpGo;
        if (vpTransform != null)
        {
            vpGo = vpTransform.gameObject;
        }
        else
        {
            vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            vpGo.transform.SetParent(scrollGo.transform, false);
        }

        inventoryViewport = vpGo.GetComponent<RectTransform>();
        inventoryViewport.anchorMin = Vector2.zero;
        inventoryViewport.anchorMax = Vector2.one;
        inventoryViewport.pivot = new Vector2(0.5f, 0.5f);
        inventoryViewport.offsetMin = Vector2.zero;
        inventoryViewport.offsetMax = Vector2.zero;

        Image vpImg = vpGo.GetComponent<Image>();
        if (vpImg != null)
        {
            vpImg.color = new Color(0f, 0f, 0f, 0f);
            vpImg.raycastTarget = true;
        }

        RectMask2D mask = vpGo.GetComponent<RectMask2D>() ?? vpGo.AddComponent<RectMask2D>();

        // Content
        Transform contentTransform = vpGo.transform.Find("Content");
        GameObject contentGo;
        if (contentTransform != null)
        {
            contentGo = contentTransform.gameObject;
        }
        else
        {
            contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
        }

        inventoryContent = contentGo.GetComponent<RectTransform>();
        inventoryContent.anchorMin = new Vector2(0.5f, 1f);
        inventoryContent.anchorMax = new Vector2(0.5f, 1f);
        inventoryContent.pivot = new Vector2(0.5f, 1f);
        inventoryContent.anchoredPosition = Vector2.zero;
        inventoryContent.sizeDelta = new Vector2(1000f, 430f);

        inventoryScrollRect.viewport = inventoryViewport;
        inventoryScrollRect.content = inventoryContent;

        inventoryContainer = inventoryContent;
    }

    public void AutoWireReferencesIfMissing()
    {
        EnsureScrollView();

        if (inventoryContainer == null)
        {
            inventoryContainer = inventoryContent != null ? inventoryContent : transform;
        }

        // Auto wire slot buttons
        string[] presetNames = { "RobotPreset1", "RobotPreset2", "RobotPreset3" };
        for (int i = 0; i < 3; i++)
        {
            if (slotButtons[i] == null)
            {
                Transform t = transform.Find(presetNames[i]);
                if (t != null)
                {
                    Button btn = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                    slotButtons[i] = btn;
                    slotImages[i] = t.GetComponent<Image>();
                    if (slotImages[i] != null) slotImages[i].raycastTarget = true;
                    if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = true;
                    slotTexts[i] = t.GetComponentInChildren<TMP_Text>(true);
                }
            }
        }

        // Auto wire PetDetailPanel
        if (detailPanel == null)
        {
            Transform dp = transform.Find("PetDetailPanel");
            if (dp != null) detailPanel = dp.gameObject;
        }

        if (detailPanel != null)
        {
            if (selectedPetCardImage == null)
            {
                Transform c = detailPanel.transform.Find("SelectedPetCard");
                if (c != null) selectedPetCardImage = c.GetComponent<Image>();
            }

            if (selectedPetIconImage == null && selectedPetCardImage != null)
            {
                Transform iconTransform = selectedPetCardImage.transform.Find("PetIcon");
                if (iconTransform != null)
                {
                    selectedPetIconImage = iconTransform.GetComponent<Image>();
                }
                else
                {
                    GameObject iconGo = new GameObject("PetIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconGo.transform.SetParent(selectedPetCardImage.transform, false);
                    RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(0f, 15f);
                    iconRect.sizeDelta = new Vector2(100f, 100f);

                    selectedPetIconImage = iconGo.GetComponent<Image>();
                    selectedPetIconImage.preserveAspect = true;
                    selectedPetIconImage.raycastTarget = false;
                }
            }

            if (petNameText == null)
            {
                Transform n = detailPanel.transform.Find("PetName");
                if (n != null) petNameText = n.GetComponent<TMP_Text>();
            }

            if (petDescText == null)
            {
                Transform d = detailPanel.transform.Find("Description");
                if (d != null) petDescText = d.GetComponent<TMP_Text>();
            }

            if (craftButton == null)
            {
                Transform cb = detailPanel.transform.Find("CraftButton");
                if (cb != null) craftButton = cb.GetComponent<Button>();
            }

            if (infoButton == null)
            {
                Transform ib = detailPanel.transform.Find("Button_Info");
                if (ib != null)
                {
                    infoButton = ib.GetComponent<Button>();
                }
                else
                {
                    infoButton = CreateInfoButtonInDetailPanel(detailPanel.transform);
                }
            }
        }

        // Auto wire sort buttons
        if (byTierButton == null)
        {
            Transform bt = transform.Find("RobotByTier");
            if (bt != null) byTierButton = bt.GetComponent<Button>() ?? bt.gameObject.AddComponent<Button>();
        }
        if (byTierButton != null)
        {
            Image img = byTierButton.GetComponent<Image>() ?? byTierButton.targetGraphic as Image;
            if (img != null) img.raycastTarget = true;
            if (byTierButton.targetGraphic != null) byTierButton.targetGraphic.raycastTarget = true;
        }

        if (byQuantityButton == null)
        {
            Transform bq = transform.Find("RobotByQuantity");
            if (bq != null) byQuantityButton = bq.GetComponent<Button>() ?? bq.gameObject.AddComponent<Button>();
        }
        if (byQuantityButton != null)
        {
            Image img = byQuantityButton.GetComponent<Image>() ?? byQuantityButton.targetGraphic as Image;
            if (img != null) img.raycastTarget = true;
            if (byQuantityButton.targetGraphic != null) byQuantityButton.targetGraphic.raycastTarget = true;
        }

        // Sprites
#if UNITY_EDITOR
        if (emptyCardFrameSprite == null)
        {
            emptyCardFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/RobotPet_Sliced/Card_Slot_Empty.png");
        }
#endif

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RobotPetInfoModalController.EnsureModalInCanvas(canvas);
        }
    }

    private Button CreateInfoButtonInDetailPanel(Transform detailPanelTransform)
    {
        if (detailPanelTransform == null) return null;

        GameObject infoBtnObj = new GameObject("Button_Info", typeof(RectTransform), typeof(Image), typeof(Button));
        infoBtnObj.transform.SetParent(detailPanelTransform, false);

        RectTransform rt = infoBtnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(43.25f, -40f);
        rt.sizeDelta = new Vector2(52f, 52f);

        Image img = infoBtnObj.GetComponent<Image>();
        Sprite s = null;
#if UNITY_EDITOR
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Shop/icon_info_cyan.png")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/RobotPet_Sliced/Icon_Info.png");
#endif
        if (s == null)
        {
            s = Resources.Load<Sprite>("UI/Shop/icon_info_cyan");
        }
        img.sprite = s;
        img.preserveAspect = true;
        img.raycastTarget = true;

        Button btn = infoBtnObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        return btn;
    }
}
