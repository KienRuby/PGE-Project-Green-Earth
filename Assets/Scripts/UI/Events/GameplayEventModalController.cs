using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller điều khiển giao diện Popup sự kiện tương tác (Gameplay Event) trong màn chơi.
/// Quản lý việc tạm dừng game (Time.timeScale = 0), hiển thị tiêu đề, ảnh minh họa CRT scanlines,
/// lời dẫn thoại, các nút bấm rẽ nhánh phong cách răng cưa pixel và áp dụng phần thưởng chỉ số.
/// </summary>
public class GameplayEventModalController : MonoBehaviour
{
    private static GameplayEventModalController runtimeInstance;
    private static bool isInitializing = false;

    public static GameplayEventModalController Instance
    {
        get
        {
            if (runtimeInstance == null && !isInitializing)
            {
                isInitializing = true;
                try
                {
                    runtimeInstance = FindObjectOfType<GameplayEventModalController>(true);
                    if (runtimeInstance == null)
                    {
                        runtimeInstance = EnsureModalInScene();
                    }
                }
                finally
                {
                    isInitializing = false;
                }
            }
            return runtimeInstance;
        }
        private set => runtimeInstance = value;
    }

    [Header("Root & Backdrop")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private Image dimBackground;

    [Header("Event Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image illustrationImage;
    [SerializeField] private Image scanlineOverlay;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text rewardText;

    [Header("Options Container")]
    [SerializeField] private RectTransform optionsContainer;
    [SerializeField] private Button buttonPrefab;

    private float previousTimeScale = 1f;
    private bool ownsTimeScale = false;
    private Action onCompletedCallback;
    private GameplayEventData currentEvent;
    private GameplayEventOption selectedOption;

    private static Sprite cachedTealButtonSprite;
    private static Sprite cachedScanlineSprite;

    private void Awake()
    {
        if (runtimeInstance == null)
        {
            runtimeInstance = this;
        }

        if (modalRoot == null)
        {
            modalRoot = gameObject;
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (runtimeInstance == this)
        {
            runtimeInstance = null;
        }

        if (ownsTimeScale)
        {
            Time.timeScale = previousTimeScale;
            ownsTimeScale = false;
        }
    }

    public void Show(GameplayEventData eventData, Action onCompleted = null)
    {
        if (eventData == null)
        {
            Debug.LogWarning("[GameplayEventModalController] eventData truyền vào bị null!");
            onCompleted?.Invoke();
            return;
        }

        currentEvent = eventData;
        onCompletedCallback = onCompleted;
        selectedOption = null;

        if (!ownsTimeScale)
        {
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            ownsTimeScale = true;
        }

        if (modalRoot != null) modalRoot.SetActive(true);
        gameObject.SetActive(true);

        // 1. Tiêu đề
        if (titleText != null)
        {
            titleText.text = currentEvent.eventTitle;
        }

        // 2. Ảnh minh họa CRT
        if (illustrationImage != null)
        {
            if (currentEvent.illustrationSprite != null)
            {
                illustrationImage.sprite = currentEvent.illustrationSprite;
            }
            else
            {
                illustrationImage.sprite = GenerateProceduralEventIllustration(currentEvent.eventId);
            }
            illustrationImage.gameObject.SetActive(true);
        }

        // 3. Trạng thái ban đầu: LUÔN là màn hình đọc tình huống và bấm nút lựa chọn.
        // Tuyệt đối KHÔNG hiển thị chỉ số thưởng trước khi người chơi chọn.
        if (dialogueText != null)
        {
            dialogueText.text = currentEvent.introDialogueText;
            dialogueText.gameObject.SetActive(true);
        }

        if (rewardText != null)
        {
            rewardText.text = "";
            rewardText.gameObject.SetActive(false);
        }

        PopulateChoiceButtons(currentEvent.options);
    }

    private void PopulateChoiceButtons(List<GameplayEventOption> options)
    {
        ClearButtons();

        if (options == null || options.Count == 0)
        {
            CreateSingleLeaveButton(null);
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            GameplayEventOption opt = options[i];
            Button btn = CreateButton(opt.buttonText);
            btn.onClick.AddListener(() => OnOptionSelected(opt));
        }
    }

    private void OnOptionSelected(GameplayEventOption option)
    {
        selectedOption = option;

        // Nếu bấm Leave ngay ở màn lựa chọn
        if (string.Equals(option.buttonText, "Leave.", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(option.resultStoryText) && string.IsNullOrEmpty(option.rewardText))
        {
            FinishAndClose();
            return;
        }

        // Chuyển sang Result State (như ảnh 4, 5)
        if (dialogueText != null)
        {
            dialogueText.text = option.resultStoryText;
        }

        if (rewardText != null)
        {
            rewardText.text = option.rewardText;
            rewardText.gameObject.SetActive(!string.IsNullOrEmpty(option.rewardText));
        }

        ClearButtons();
        CreateSingleLeaveButton(option);
    }

    private void CreateSingleLeaveButton(GameplayEventOption optToApply)
    {
        ClearButtons();
        selectedOption = optToApply;
        Button btn = CreateButton("Leave.");
        btn.onClick.AddListener(FinishAndClose);
    }

    private void FinishAndClose()
    {
        if (selectedOption != null)
        {
            ApplyReward(selectedOption);
        }

        if (ownsTimeScale)
        {
            Time.timeScale = previousTimeScale;
            ownsTimeScale = false;
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        Action callback = onCompletedCallback;
        onCompletedCallback = null;
        callback?.Invoke();
    }

    private void ApplyReward(GameplayEventOption option)
    {
        if (option == null || option.rewardType == EventRewardType.None) return;

        switch (option.rewardType)
        {
            case EventRewardType.StatBuff_MoveSpeedPercent:
                PlayerMovement movement = FindObjectOfType<PlayerMovement>();
                if (movement != null)
                {
                    movement.AddMoveSpeedPercent(option.rewardValue);
                    Debug.Log($"[GameplayEvent] 🏃 Áp dụng tăng tốc chạy +{option.rewardValue}%. Tốc độ mới: {movement.EffectiveSpeed}");
                }
                break;

            case EventRewardType.Health_ConsumePercent:
                PlayerHealth healthToDamage = FindObjectOfType<PlayerHealth>();
                if (healthToDamage != null)
                {
                    int dmg = Mathf.RoundToInt(healthToDamage.CurrentHealth * (option.rewardValue / 100f));
                    // Đảm bảo không làm chết người chơi do sự kiện
                    dmg = Mathf.Min(dmg, healthToDamage.CurrentHealth - 1);
                    if (dmg > 0)
                    {
                        healthToDamage.TakeDamage(dmg);
                        Debug.Log($"[GameplayEvent] ⚡ Tiêu hao -{option.rewardValue}% HP ({dmg} sát thương). Còn lại: {healthToDamage.CurrentHealth}");
                    }

                    if (string.Equals(option.rewardItemId, "big-battery", StringComparison.OrdinalIgnoreCase))
                    {
                        healthToDamage.AddMaxHealth(25);
                        Debug.Log($"[GameplayEvent] 🔋 [Big Battery] Level +1: Tăng Máu tối đa +25!");
                    }
                }
                break;

            case EventRewardType.Health_HealPercent:
                PlayerHealth healthToHeal = FindObjectOfType<PlayerHealth>();
                if (healthToHeal != null)
                {
                    int heal = Mathf.RoundToInt(100 * (option.rewardValue / 100f));
                    healthToHeal.TakeDamage(-heal); // TakeDamage nhận số âm để hồi máu hoặc logic hồi máu
                    Debug.Log($"[GameplayEvent] 💖 Hồi phục +{option.rewardValue}% HP (+{heal} HP).");
                }
                break;

            case EventRewardType.Artifact_Grant:
                if (!string.IsNullOrEmpty(option.rewardItemId))
                {
                    ArtifactData art = ArtifactDatabase.Instance.GetById(option.rewardItemId);
                    if (art != null && PlayerArtifactInventory.Instance != null)
                    {
                        PlayerArtifactInventory.Instance.EquipArtifact(art);
                        Debug.Log($"[GameplayEvent] 🎁 Người chơi nhận được Cổ vật: {art.artifactName}");
                    }
                }
                break;

            case EventRewardType.ArtifactBox_Open:
                // Mở mystery artifact
                if (PlayerArtifactInventory.Instance != null)
                {
                    ArtifactData randomArt = ArtifactDatabase.Instance.GetRandomArtifact();
                    if (randomArt != null)
                    {
                        PlayerArtifactInventory.Instance.EquipArtifact(randomArt);
                        Debug.Log($"[GameplayEvent] 🎁 Người chơi mở Hộp Cổ vật nhận: {randomArt.artifactName}");
                    }
                }
                break;
        }
    }

    private void ClearButtons()
    {
        if (optionsContainer == null) return;
        for (int i = optionsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = optionsContainer.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private Button CreateButton(string label)
    {
        GameObject btnObj = new GameObject("OptionBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(optionsContainer, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 64f);

        Image img = btnObj.GetComponent<Image>();
        img.sprite = GetOrCreateTealButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.85f, 1f, 0.95f, 1f);
        cb.pressedColor = new Color(0.6f, 0.9f, 0.85f, 1f);
        btn.colors = cb;

        // Text bên trong nút
        GameObject textObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16, 4);
        textRt.offsetMax = new Vector2(-16, -4);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;

        return btn;
    }

    public static Sprite GetOrCreateTealButtonSprite()
    {
        if (cachedTealButtonSprite != null) return cachedTealButtonSprite;

        // Thử tìm sprite nút có sẵn trong Resources
        Sprite existing = Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Get");
        if (existing != null)
        {
            cachedTealButtonSprite = existing;
            return cachedTealButtonSprite;
        }

        // Tự tạo Sprite nút răng cưa pixel màu Cyan/Teal chuẩn 128x36
        int width = 128;
        int height = 36;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 tealFill = new Color32(24, 176, 159, 255);      // #18B09F
        Color32 darkTealBorder = new Color32(10, 80, 75, 255);  // Viền ngoài tối
        Color32 innerTealHighlight = new Color32(90, 230, 215, 255); // Đường viền dạ quang trong
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Bo góc khuyết pixel 4 góc
                bool isCornerCut = (x < 2 && (y < 2 || y >= height - 2)) ||
                                   (x >= width - 2 && (y < 2 || y >= height - 2));
                if (isCornerCut)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                // Viền ngoài cùng
                bool isOuterBorder = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                if (isOuterBorder)
                {
                    tex.SetPixel(x, y, darkTealBorder);
                    continue;
                }

                // Họa tiết răng cưa pixel ở đáy (đặc trưng nút game)
                if (y == 1 && (x % 6 == 0 || x % 6 == 1))
                {
                    tex.SetPixel(x, y, innerTealHighlight);
                    continue;
                }

                // Viền sáng bên trong
                bool isInnerBorder = x == 1 || x == width - 2 || y == height - 2;
                if (isInnerBorder)
                {
                    tex.SetPixel(x, y, innerTealHighlight);
                    continue;
                }

                tex.SetPixel(x, y, tealFill);
            }
        }

        tex.Apply();
        // 9-slice border (left: 8, bottom: 8, right: 8, top: 8)
        cachedTealButtonSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
        return cachedTealButtonSprite;
    }

    public static Sprite GetOrCreateScanlineSprite()
    {
        if (cachedScanlineSprite != null) return cachedScanlineSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color32 lineDark = new Color32(0, 0, 0, 85);
        Color32 lineClear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            Color32 c = (y % 2 == 0) ? lineDark : lineClear;
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        cachedScanlineSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedScanlineSprite;
    }

    private static Sprite GenerateProceduralEventIllustration(string eventId)
    {
        int w = 256;
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 baseColor = new Color32(35, 45, 30, 255);
        Color32 highlight = new Color32(110, 140, 90, 255);
        Color32 accent = new Color32(180, 210, 130, 255);

        if (string.Equals(eventId, "underground_bunker", StringComparison.OrdinalIgnoreCase))
        {
            baseColor = new Color32(40, 20, 10, 255);
            highlight = new Color32(180, 90, 20, 255);
            accent = new Color32(245, 180, 50, 255);
        }
        else if (string.Equals(eventId, "assasinator", StringComparison.OrdinalIgnoreCase))
        {
            baseColor = new Color32(30, 25, 35, 255);
            highlight = new Color32(140, 80, 60, 255);
            accent = new Color32(230, 160, 90, 255);
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                float ny = (float)y / h;

                // Tạo nền chuyển sắc hoàng hôn / phế tích
                Color c = Color.Lerp(baseColor, highlight, ny * 0.8f);

                // Silhouette ở giữa
                float dx = Mathf.Abs(nx - 0.5f);
                if (ny < 0.65f && dx < 0.25f * (1f - ny * 0.5f))
                {
                    c = Color.Lerp(c, (Color)baseColor * 0.5f, 0.8f);
                }

                // Điểm nhấn phát sáng (lõi đèn / kiếm năng lượng / đèn hầm)
                if (Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.45f)) < 0.15f)
                {
                    c = Color.Lerp(c, accent, 0.6f);
                }

                // Hiệu ứng scanline sọc ngang tích hợp
                if (y % 4 < 2)
                {
                    c *= 0.75f;
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        TMP_FontAsset font = null;
#if UNITY_EDITOR
        font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
#endif
        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }

    public static GameplayEventModalController EnsureModalInScene(Canvas targetCanvas = null)
    {
        if (runtimeInstance != null) return runtimeInstance;

        GameplayEventModalController existing = FindObjectOfType<GameplayEventModalController>(true);
        if (existing != null)
        {
            runtimeInstance = existing;
            return existing;
        }

        if (targetCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c != null && c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)
                {
                    targetCanvas = c;
                    break;
                }
            }
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[GameplayEventModalController] Không tìm thấy Canvas để khởi tạo Modal!");
            return null;
        }

        return CreateRuntimeModal(targetCanvas.transform as RectTransform);
    }

    public static GameplayEventModalController CreateRuntimeModal(RectTransform canvasParent)
    {
        if (canvasParent == null) return null;

        GameplayEventModalController existing = canvasParent.GetComponentInChildren<GameplayEventModalController>(true);
        if (existing != null)
        {
            runtimeInstance = existing;
            return existing;
        }

        TMP_FontAsset defaultFont = GetDefaultFont();

        // 1. Root Modal
        GameObject root = new GameObject("GameplayEventModal", typeof(RectTransform));
        root.transform.SetParent(canvasParent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // 2. Dim Background (Lớp tối)
        GameObject bgObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.04f, 0.06f, 0.94f);

        // 3. Content Panel (Căn giữa)
        GameObject contentObj = new GameObject("ContentPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(root.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(520f, 850f);
        contentRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 4. Header Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(contentObj.transform, false);
        LayoutElement titleLe = titleObj.AddComponent<LayoutElement>();
        titleLe.minHeight = 50f;
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Event Title";
        titleTmp.fontSize = 38;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        if (defaultFont != null) titleTmp.font = defaultFont;

        // 5. Illustration Frame (CRT)
        GameObject frameObj = new GameObject("IllustrationFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(contentObj.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.sizeDelta = new Vector2(320f, 320f);
        LayoutElement frameLe = frameObj.AddComponent<LayoutElement>();
        frameLe.preferredWidth = 320f;
        frameLe.preferredHeight = 320f;
        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.preserveAspect = true;

        // Scanline overlay
        GameObject scanlineObj = new GameObject("Scanlines", typeof(RectTransform), typeof(Image));
        scanlineObj.transform.SetParent(frameObj.transform, false);
        RectTransform slRt = scanlineObj.GetComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero;
        slRt.anchorMax = Vector2.one;
        slRt.offsetMin = Vector2.zero;
        slRt.offsetMax = Vector2.zero;
        Image slImg = scanlineObj.GetComponent<Image>();
        slImg.sprite = GetOrCreateScanlineSprite();
        slImg.type = Image.Type.Tiled;
        slImg.color = new Color(1f, 1f, 1f, 0.4f);
        slImg.raycastTarget = false;

        // 6. Dialogue / Story Text
        GameObject storyObj = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        storyObj.transform.SetParent(contentObj.transform, false);
        LayoutElement storyLe = storyObj.AddComponent<LayoutElement>();
        storyLe.minHeight = 90f;
        TextMeshProUGUI storyTmp = storyObj.GetComponent<TextMeshProUGUI>();
        storyTmp.text = "Event story text...";
        storyTmp.fontSize = 22;
        storyTmp.fontStyle = FontStyles.Normal;
        storyTmp.alignment = TextAlignmentOptions.Center;
        storyTmp.color = new Color(0.9f, 0.95f, 0.95f, 1f);
        storyTmp.lineSpacing = 12f;
        if (defaultFont != null) storyTmp.font = defaultFont;

        // 7. Reward Text (Màu vàng kim)
        GameObject rewardObj = new GameObject("RewardText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewardObj.transform.SetParent(contentObj.transform, false);
        LayoutElement rewardLe = rewardObj.AddComponent<LayoutElement>();
        rewardLe.minHeight = 45f;
        TextMeshProUGUI rewardTmp = rewardObj.GetComponent<TextMeshProUGUI>();
        rewardTmp.text = "Reward Text";
        rewardTmp.fontSize = 28;
        rewardTmp.fontStyle = FontStyles.Bold;
        rewardTmp.alignment = TextAlignmentOptions.Center;
        rewardTmp.color = new Color32(255, 194, 54, 255); // #FFC236
        if (defaultFont != null) rewardTmp.font = defaultFont;
        rewardObj.SetActive(false);

        // 8. Options Container (Chứa các nút)
        GameObject optsObj = new GameObject("OptionsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        optsObj.transform.SetParent(contentObj.transform, false);
        RectTransform optsRt = optsObj.GetComponent<RectTransform>();
        optsRt.sizeDelta = new Vector2(480f, 220f);
        LayoutElement optsLe = optsObj.AddComponent<LayoutElement>();
        optsLe.minHeight = 120f;
        VerticalLayoutGroup optsVlg = optsObj.GetComponent<VerticalLayoutGroup>();
        optsVlg.childAlignment = TextAnchor.MiddleCenter;
        optsVlg.spacing = 14f;
        optsVlg.childControlWidth = false;
        optsVlg.childControlHeight = false;

        // 9. Gắn Controller
        GameplayEventModalController controller = root.AddComponent<GameplayEventModalController>();
        controller.modalRoot = root;
        controller.dimBackground = bgImg;
        controller.titleText = titleTmp;
        controller.illustrationImage = frameImg;
        controller.scanlineOverlay = slImg;
        controller.dialogueText = storyTmp;
        controller.rewardText = rewardTmp;
        controller.optionsContainer = optsRt;

        root.SetActive(false);
        runtimeInstance = controller;
        return controller;
    }
}
