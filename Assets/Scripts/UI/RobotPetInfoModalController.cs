using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller điều khiển hiển thị Modal Hướng Dẫn & Thông Tin Robot Pet.
/// Hiển thị chi tiết về hệ thống 3 Slot trang bị, cơ chế chế tạo (Crafting) và danh sách kỹ năng độc quyền của từng Pet.
/// </summary>
public class RobotPetInfoModalController : MonoBehaviour
{
    private static RobotPetInfoModalController runtimeInstance;
    public static RobotPetInfoModalController Instance
    {
        get
        {
            if (runtimeInstance == null)
            {
                runtimeInstance = FindObjectOfType<RobotPetInfoModalController>(true);
                if (runtimeInstance == null)
                {
                    runtimeInstance = EnsureModalInCanvas();
                }
            }
            return runtimeInstance;
        }
        private set => runtimeInstance = value;
    }

    [Header("UI Elements")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text closeButtonText;
    [SerializeField] private Button backgroundDimButton;

    [Header("Sections & Guide Texts")]
    [SerializeField] private TMP_Text slotGuideTitleText;
    [SerializeField] private TMP_Text slotGuideBodyText;
    [SerializeField] private TMP_Text craftGuideTitleText;
    [SerializeField] private TMP_Text craftGuideBodyText;
    [SerializeField] private TMP_Text petListTitleText;
    [SerializeField] private TMP_Text noteText;

    [Header("Pet Rows")]
    [SerializeField] private List<TMP_Text> petNameTexts = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> petSkillTexts = new List<TMP_Text>();

    private void Awake()
    {
        if (runtimeInstance == null)
        {
            runtimeInstance = this;
        }
        else if (runtimeInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (backgroundDimButton != null)
        {
            backgroundDimButton.onClick.RemoveAllListeners();
            backgroundDimButton.onClick.AddListener(Close);
        }

        // Tự động xóa Button_X nếu còn sót lại
        Transform bx = transform.Find("ContentPanel/Button_X");
        if (bx != null)
        {
            if (Application.isPlaying) Destroy(bx.gameObject);
            else DestroyImmediate(bx.gameObject);
        }
    }

    private void OnEnable()
    {
        GameSettings.Changed -= HandleSettingsChanged;
        GameSettings.Changed += HandleSettingsChanged;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= HandleSettingsChanged;
    }

    private void HandleSettingsChanged()
    {
        if (gameObject.activeInHierarchy || (modalRoot != null && modalRoot.activeSelf))
        {
            UpdateTexts();
        }
    }

    public void Show()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        UpdateTexts();
    }

    public void Close()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateTexts()
    {
        bool vi = GameSettings.IsVietnamese;

        if (titleText != null)
        {
            titleText.text = vi ? "HƯỚNG DẪN ROBOT PET" : "ROBOT PET GUIDE";
        }

        if (subtitleText != null)
        {
            subtitleText.text = vi
                ? "Trang bị tối đa 3 Robot Pet đồng hành để hỗ trợ chiến đấu cùng Adam"
                : "Equip up to 3 Companion Pets to assist Adam in battle";
        }

        if (slotGuideTitleText != null)
        {
            slotGuideTitleText.text = vi ? "1. HỆ THỐNG 3 SLOTS TRANG BỊ" : "1. 3-SLOT LOADOUT SYSTEM";
        }

        if (slotGuideBodyText != null)
        {
            slotGuideBodyText.text = vi
                ? "• Nhấn vào các ô số 1, 2, 3 để chọn Slot muốn trang bị.\n• Nhấn vào bất kỳ Pet nào đã sở hữu trong kho để lắp vào Slot đó.\n• Mỗi Slot mang lại hiệu ứng bổ trợ chủ động và bị động trong trận đấu."
                : "• Tap slots 1, 2, 3 to choose which loadout slot to equip.\n• Tap any owned Pet in inventory to equip it into the selected slot.\n• Each equipped Pet provides unique combat support skills.";
        }

        if (craftGuideTitleText != null)
        {
            craftGuideTitleText.text = vi ? "2. CHẾ TẠO PET (CRAFTING)" : "2. PET CRAFTING";
        }

        if (craftGuideBodyText != null)
        {
            craftGuideBodyText.text = vi
                ? "• Thu thập Chipset từ Shop và Hộp Thưởng để làm nguyên liệu chế tạo.\n• Nhấn nút [Craft] để vào Lò Ấp (Incubator) và kích hoạt Robot Pet mới."
                : "• Collect Chipsets from Shop and Gacha boxes as crafting materials.\n• Tap the [Craft] button to enter the Incubator and activate new Pets.";
        }

        if (petListTitleText != null)
        {
            petListTitleText.text = vi ? "3. DANH SÁCH & HIỆU ỨNG ROBOT PET" : "3. PET LIST & COMBAT SKILLS";
        }

        if (noteText != null)
        {
            noteText.text = vi
                ? "* Lưu ý: Bạn có thể tự do thay đổi Pet giữa các trận chiến bất kỳ lúc nào!"
                : "* Note: You can freely switch equipped Pets between battles at any time!";
        }

        if (closeButtonText != null)
        {
            closeButtonText.text = vi ? "ĐÓNG" : "CLOSE";
        }

        // Cập nhật danh sách 7 pet
        var petSkillsVi = new (string name, string skill)[]
        {
            ("Pink Bat", "Phát sóng siêu âm gây rối loạn và choáng quái vật xung quanh."),
            ("Green Slime", "Tạo hào quang dính làm chậm tốc độ di chuyển của kẻ địch."),
            ("Cyber Spider", "Bắn lưới plasma giam giữ và phóng điện gây tê liệt mục tiêu."),
            ("Viper Bot", "Bắn phi tiêu độc cực mạnh, rút máu kẻ thù theo thời gian."),
            ("Cydog", "Trung thành bảo vệ Adam, gia tăng chỉ số Máu tối đa (Max HP)."),
            ("Iron Shell", "Tạo lá chắn từ trường kiên cố hấp thụ sát thương nhận vào."),
            ("Turbo Snail", "Để lại vệt hàn băng buốt giá làm đóng băng kẻ thù bám đuổi.")
        };

        var petSkillsEn = new (string name, string skill)[]
        {
            ("Pink Bat", "Emits supersonic waves that confuse and stun nearby enemies."),
            ("Green Slime", "Generates a sticky aura slowing enemy movements."),
            ("Cyber Spider", "Weaves plasma webs that trap and electrify foes."),
            ("Viper Bot", "Spits venom darts that inflict continuous poison damage."),
            ("Cydog", "Loyal companion increasing Adam's Max HP."),
            ("Iron Shell", "Forms an energy barrier absorbing incoming damage."),
            ("Turbo Snail", "Leaves a cryogenic trail that freezes pursuing enemies.")
        };

        var petData = vi ? petSkillsVi : petSkillsEn;
        for (int i = 0; i < petData.Length && i < petNameTexts.Count; i++)
        {
            if (petNameTexts[i] != null) petNameTexts[i].text = petData[i].name;
            if (i < petSkillTexts.Count && petSkillTexts[i] != null) petSkillTexts[i].text = petData[i].skill;
        }
    }

    public static RobotPetInfoModalController EnsureModalInCanvas(Canvas targetCanvas = null)
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogWarning("[RobotPetInfoModal] Không tìm thấy Canvas để khởi tạo modal!");
            return null;
        }

        Transform existing = targetCanvas.transform.Find("RobotPetInfoModal");
        if (existing != null)
        {
            var existingCtrl = existing.GetComponent<RobotPetInfoModalController>();
            if (existingCtrl != null)
            {
                Transform oldBx = existing.Find("ContentPanel/Button_X");
                if (oldBx != null)
                {
                    if (Application.isPlaying) Destroy(oldBx.gameObject);
                    else DestroyImmediate(oldBx.gameObject);
                }
                return existingCtrl;
            }
        }

        // Tạo root Modal
        GameObject root = new GameObject("RobotPetInfoModal", typeof(RectTransform), typeof(RobotPetInfoModalController));
        root.transform.SetParent(targetCanvas.transform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;

        // Background Dim
        GameObject dimObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image), typeof(Button));
        dimObj.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.sizeDelta = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0.02f, 0.05f, 0.09f, 0.85f);
        Button dimBtn = dimObj.GetComponent<Button>();

        // Center Panel Frame
        GameObject panelObj = new GameObject("ContentPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(root.transform, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(640f, 840f);
        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.05f, 0.09f, 0.15f, 0.98f);

        // Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -22f);
        titleRt.sizeDelta = new Vector2(580f, 40f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "ROBOT PET GUIDE";
        titleTmp.fontSize = 24f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.18f, 0.9f, 0.98f, 1f);

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(panelObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 1f);
        subRt.anchorMax = new Vector2(0.5f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -68f);
        subRt.sizeDelta = new Vector2(580f, 44f);
        TextMeshProUGUI subTmp = subObj.GetComponent<TextMeshProUGUI>();
        subTmp.text = "Equip up to 3 Companion Pets to assist Adam in battle";
        subTmp.fontSize = 15f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.75f, 0.85f, 0.95f, 1f);

        // Scroll Area for Guide
        GameObject scrollObj = new GameObject("GuideScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(panelObj.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 1f);
        scrollRt.anchorMax = new Vector2(0.5f, 1f);
        scrollRt.pivot = new Vector2(0.5f, 1f);
        scrollRt.anchoredPosition = new Vector2(0f, -120f);
        scrollRt.sizeDelta = new Vector2(580f, 610f);

        ScrollRect sRect = scrollObj.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpObj.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRt = vpObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;
        sRect.viewport = vpRt;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(vpObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 1f);
        contentRt.anchorMax = new Vector2(0.5f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(560f, 600f);
        sRect.content = contentRt;

        VerticalLayoutGroup vLayout = contentObj.GetComponent<VerticalLayoutGroup>();
        vLayout.spacing = 14f;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Section 1: Slots
        CreateSectionBox(contentRt, "Section_Slots", out TMP_Text s1Title, out TMP_Text s1Body);
        s1Title.text = "1. HỆ THỐNG 3 SLOTS TRANG BỊ";
        s1Title.color = new Color(1f, 0.85f, 0.3f, 1f);

        // Section 2: Crafting
        CreateSectionBox(contentRt, "Section_Crafting", out TMP_Text s2Title, out TMP_Text s2Body);
        s2Title.text = "2. CHẾ TẠO PET (CRAFTING)";
        s2Title.color = new Color(0.35f, 0.85f, 1f, 1f);

        // Section 3: Pet List Header
        GameObject petHeaderObj = new GameObject("Section_PetHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        petHeaderObj.transform.SetParent(contentRt, false);
        TextMeshProUGUI petHeaderTmp = petHeaderObj.GetComponent<TextMeshProUGUI>();
        petHeaderTmp.text = "3. DANH SÁCH & HIỆU ỨNG ROBOT PET";
        petHeaderTmp.fontSize = 17f;
        petHeaderTmp.fontStyle = FontStyles.Bold;
        petHeaderTmp.color = new Color(0.2f, 0.95f, 0.7f, 1f);
        petHeaderTmp.alignment = TextAlignmentOptions.Left;

        // Create 7 pet rows
        List<TMP_Text> names = new List<TMP_Text>();
        List<TMP_Text> skills = new List<TMP_Text>();
        for (int i = 0; i < 7; i++)
        {
            CreatePetRow(contentRt, $"PetRow_{i}", out TMP_Text pName, out TMP_Text pSkill);
            names.Add(pName);
            skills.Add(pSkill);
        }

        // Note
        GameObject noteObj = new GameObject("NoteText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noteObj.transform.SetParent(contentRt, false);
        TextMeshProUGUI noteTmp = noteObj.GetComponent<TextMeshProUGUI>();
        noteTmp.text = "* Lưu ý: Bạn có thể tự do thay đổi Pet giữa các trận chiến bất kỳ lúc nào!";
        noteTmp.fontSize = 13f;
        noteTmp.fontStyle = FontStyles.Italic;
        noteTmp.alignment = TextAlignmentOptions.Center;
        noteTmp.color = new Color(0.6f, 0.7f, 0.8f, 1f);

        // Close Button
        GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRt.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRt.pivot = new Vector2(0.5f, 0f);
        closeBtnRt.anchoredPosition = new Vector2(0f, 22f);
        closeBtnRt.sizeDelta = new Vector2(220f, 48f);
        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.1f, 0.75f, 0.85f, 1f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.text = "CLOSE";
        closeTxt.fontSize = 18f;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = new Color(0.04f, 0.08f, 0.14f, 1f);

        // Wire Controller
        RobotPetInfoModalController ctrl = root.GetComponent<RobotPetInfoModalController>();
        ctrl.modalRoot = root;
        ctrl.titleText = titleTmp;
        ctrl.subtitleText = subTmp;
        ctrl.closeButton = closeBtn;
        ctrl.closeButtonText = closeTxt;
        ctrl.backgroundDimButton = dimBtn;
        ctrl.slotGuideTitleText = s1Title;
        ctrl.slotGuideBodyText = s1Body;
        ctrl.craftGuideTitleText = s2Title;
        ctrl.craftGuideBodyText = s2Body;
        ctrl.petListTitleText = petHeaderTmp;
        ctrl.noteText = noteTmp;
        ctrl.petNameTexts = names;
        ctrl.petSkillTexts = skills;

        closeBtn.onClick.AddListener(ctrl.Close);
        dimBtn.onClick.AddListener(ctrl.Close);

        ctrl.UpdateTexts();

        root.SetActive(false);
        return ctrl;
    }

    private static void CreateSectionBox(Transform parent, string name, out TMP_Text titleOut, out TMP_Text bodyOut)
    {
        GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        box.transform.SetParent(parent, false);
        Image img = box.GetComponent<Image>();
        img.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

        VerticalLayoutGroup v = box.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 12, 12);
        v.spacing = 6f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        ContentSizeFitter csf = box.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 16f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleOut = titleTmp;

        GameObject bodyObj = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(box.transform, false);
        TextMeshProUGUI bodyTmp = bodyObj.GetComponent<TextMeshProUGUI>();
        bodyTmp.fontSize = 14f;
        bodyTmp.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        bodyTmp.lineSpacing = 6f;
        bodyOut = bodyTmp;
    }

    private static void CreatePetRow(Transform parent, string name, out TMP_Text nameOut, out TMP_Text skillOut)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        row.transform.SetParent(parent, false);
        Image img = row.GetComponent<Image>();
        img.color = new Color(0.06f, 0.11f, 0.18f, 0.9f);

        VerticalLayoutGroup v = row.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 10, 10);
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        ContentSizeFitter csf = row.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject nameObj = new GameObject("PetName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 16f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = new Color(1f, 0.8f, 0.2f, 1f);
        nameOut = nameTmp;

        GameObject skillObj = new GameObject("PetSkill", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI skillTmp = skillObj.GetComponent<TextMeshProUGUI>();
        skillTmp.fontSize = 13.5f;
        skillTmp.color = new Color(0.8f, 0.88f, 0.95f, 1f);
        skillOut = skillTmp;
    }
}
