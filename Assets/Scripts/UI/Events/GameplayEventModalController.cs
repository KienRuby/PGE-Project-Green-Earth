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
    private static Sprite cachedDialogCardSprite;
    private static Sprite cachedInnerFrameSprite;

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

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
        }
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // 1. Tiêu đề
        if (titleText != null)
        {
            titleText.text = currentEvent.eventTitle;
        }

        // 2. Ảnh minh họa CRT
        if (illustrationImage != null)
        {
            Sprite spriteToUse = currentEvent.illustrationSprite;
            if (spriteToUse == null)
            {
                spriteToUse = Resources.Load<Sprite>($"Events/{currentEvent.eventId}");
            }
            if (spriteToUse == null)
            {
                spriteToUse = GenerateProceduralEventIllustration(currentEvent.eventId);
            }
            illustrationImage.sprite = spriteToUse;
            illustrationImage.gameObject.SetActive(true);
        }

        // 3. Trạng thái ban đầu: LUÔN là màn hình đọc tình huống và bấm nút lựa chọn.
        // Tuyệt đối KHÔNG hiển thị chỉ số thưởng trước khi người chơi chọn.
        if (dialogueText != null)
        {
            dialogueText.text = currentEvent.introDialogueText;
            dialogueText.gameObject.SetActive(true);
            dialogueText.ForceMeshUpdate();
        }

        if (rewardText != null)
        {
            rewardText.text = "";
            rewardText.gameObject.SetActive(false);
        }

        PopulateChoiceButtons(currentEvent.options);
        RebuildModalLayout();
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

        RebuildModalLayout();
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
            dialogueText.ForceMeshUpdate();
        }

        if (rewardText != null)
        {
            rewardText.text = option.rewardText;
            rewardText.gameObject.SetActive(!string.IsNullOrEmpty(option.rewardText));
            if (rewardText.gameObject.activeSelf)
            {
                rewardText.ForceMeshUpdate();
            }
        }

        ClearButtons();
        CreateSingleLeaveButton(option);
        RebuildModalLayout();
    }

    private void CreateSingleLeaveButton(GameplayEventOption optToApply)
    {
        ClearButtons();
        selectedOption = optToApply;
        Button btn = CreateButton("Leave.");
        btn.onClick.AddListener(FinishAndClose);
        RebuildModalLayout();
    }

    private void RebuildModalLayout()
    {
        if (dialogueText != null)
        {
            dialogueText.ForceMeshUpdate();
        }
        if (rewardText != null && rewardText.gameObject.activeSelf)
        {
            rewardText.ForceMeshUpdate();
        }

        Canvas.ForceUpdateCanvases();

        if (optionsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
        }
        if (modalRoot != null)
        {
            Transform cp = modalRoot.transform.Find("ContentPanel");
            if (cp is RectTransform cpRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cpRt);
            }
        }
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
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private Button CreateButton(string label)
    {
        GameObject btnObj = new GameObject("OptionBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(optionsContainer, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(840f, 92f);

        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        le.minHeight = 92f;
        le.preferredHeight = 92f;
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = GetOrCreateTealButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.7f, 0.95f, 0.95f, 1f);
        cb.selectedColor = Color.white;
        btn.colors = cb;

        // Text bên trong nút
        GameObject textObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20, 4);
        textRt.offsetMax = new Vector2(-20, -4);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 30;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = true;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(6, 26, 34, 255); // #061A22: Contrast cực cao trên nền Cyan
        tmp.raycastTarget = false;

        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;

        return btn;
    }

    public static Sprite GetOrCreateTealButtonSprite()
    {
        if (cachedTealButtonSprite != null) return cachedTealButtonSprite;

        // Tự tạo Sprite nút răng cưa pixel màu Cyan/Teal chuẩn 128x36 (Test07 asserts 128x36)
        // Thiết kế đồng bộ hoàn hảo với mẫu nút cyan trong hình ảnh thứ 2
        int width = 128;
        int height = 36;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 cyanFill = new Color32(0, 241, 233, 255);        // #00F1E9
        Color32 darkBorder = new Color32(2, 2, 2, 255);          // Viền ngoài đen sắc nét
        Color32 bevelDark = new Color32(0, 137, 148, 255);       // Gờ nổi 3D đáy nút (#008994)
        Color32 topHighlight = new Color32(110, 255, 250, 255);  // Viền sáng trên cùng
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Bo góc vát pixel 4 góc
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
                    tex.SetPixel(x, y, darkBorder);
                    continue;
                }

                // Gờ đổ bóng 3D ở mép dưới
                if (y >= 1 && y <= 4)
                {
                    tex.SetPixel(x, y, bevelDark);
                    continue;
                }

                if (y == 5)
                {
                    tex.SetPixel(x, y, darkBorder);
                    continue;
                }

                // Highlight cạnh trên
                if (y == height - 2)
                {
                    tex.SetPixel(x, y, topHighlight);
                    continue;
                }

                tex.SetPixel(x, y, cyanFill);
            }
        }

        tex.Apply();
        // 9-slice border (left: 8, bottom: 8, right: 8, top: 6)
        cachedTealButtonSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 6));
        return cachedTealButtonSprite;
    }

    public static Sprite GetOrCreateDialogCardSprite()
    {
        if (cachedDialogCardSprite != null) return cachedDialogCardSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 bg = new Color32(11, 20, 30, 252);          // #0B141E - Đậm đặc, chặn triệt để xuyên nền
        Color32 border = new Color32(0, 229, 216, 255);     // #00E5D8 - Viền neon cyan sắc nét
        Color32 innerLine = new Color32(27, 54, 68, 255);   // #1B3644
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Vát góc 45 độ 3px
                if ((x < 3 && y < 3 - x) || (x >= size - 3 && y < x - (size - 4)) ||
                    (x < 3 && y >= size - 3 + x) || (x >= size - 3 && y >= size - 1 - (x - (size - 3))))
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                bool isBorder = (x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2 ||
                                (x < 4 && y <= 4 - x) || (x >= size - 4 && y <= x - (size - 5)) ||
                                (x < 4 && y >= size - 4 + x) || (x >= size - 4 && y >= size - 1 - (x - (size - 4))));
                if (isBorder)
                {
                    tex.SetPixel(x, y, border);
                    continue;
                }

                if (x == 2 || x == size - 3 || y == 2 || y == size - 3)
                {
                    tex.SetPixel(x, y, innerLine);
                    continue;
                }

                tex.SetPixel(x, y, bg);
            }
        }

        tex.Apply();
        cachedDialogCardSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        return cachedDialogCardSprite;
    }

    public static Sprite GetOrCreateInnerFrameSprite()
    {
        if (cachedInnerFrameSprite != null) return cachedInnerFrameSprite;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 bg = new Color32(6, 11, 18, 255);           // #060B12
        Color32 border = new Color32(24, 48, 66, 255);      // #183042
        Color32 cyanCorner = new Color32(0, 241, 233, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                if (isBorder)
                {
                    bool isCornerAccent = (x < 4 || x >= size - 4) && (y < 4 || y >= size - 4);
                    tex.SetPixel(x, y, isCornerAccent ? cyanCorner : border);
                    continue;
                }

                tex.SetPixel(x, y, bg);
            }
        }

        tex.Apply();
        cachedInnerFrameSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
        return cachedInnerFrameSprite;
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

        // Bổ sung Canvas riêng có Sorting Order cao (250) để đè lên trên tất cả màn Level Up / HUD / Popups
        Canvas modalCanvas = root.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 250;
        root.AddComponent<GraphicRaycaster>();

        // 2. Dim Background (Lớp tối che kín màn hình gameplay và chặn tap xuyên)
        GameObject bgObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.05f, 0.08f, 0.94f); // Tối đậm đà 94%, không để lộ mảng chữ nền
        bgImg.raycastTarget = true;

        // 3. Content Panel (Khung card sự kiện phong cách Sci-Fi Cyberpunk)
        GameObject contentObj = new GameObject("ContentPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(root.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(920f, 0f);
        contentRt.anchoredPosition = Vector2.zero;

        Image cardImg = contentObj.GetComponent<Image>();
        cardImg.sprite = GetOrCreateDialogCardSprite();
        cardImg.type = Image.Type.Sliced;
        cardImg.color = Color.white;
        cardImg.raycastTarget = true;

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 32, 36);
        vlg.spacing = 16f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. Header Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleObj.transform.SetParent(contentObj.transform, false);
        LayoutElement titleLe = titleObj.GetComponent<LayoutElement>();
        titleLe.minHeight = 52f;
        titleLe.preferredHeight = 56f;
        titleLe.flexibleHeight = 0f;
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Event Title";
        titleTmp.fontSize = 42;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;
        if (defaultFont != null) titleTmp.font = defaultFont;

        // 5. Illustration Frame (Tỉ lệ 3:2 landscape chuẩn cho hình minh họa mới)
        GameObject frameObj = new GameObject("IllustrationFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        frameObj.transform.SetParent(contentObj.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.sizeDelta = new Vector2(840f, 480f);
        LayoutElement frameLe = frameObj.GetComponent<LayoutElement>();
        frameLe.preferredWidth = 840f;
        frameLe.preferredHeight = 480f;
        frameLe.minHeight = 420f;
        frameLe.flexibleHeight = 0f;

        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.sprite = GetOrCreateInnerFrameSprite();
        frameImg.type = Image.Type.Sliced;
        frameImg.color = Color.white;

        // Ảnh hiển thị bên trong khung viền
        GameObject illustObj = new GameObject("IllustrationImage", typeof(RectTransform), typeof(Image));
        illustObj.transform.SetParent(frameObj.transform, false);
        RectTransform illustRt = illustObj.GetComponent<RectTransform>();
        illustRt.anchorMin = Vector2.zero;
        illustRt.anchorMax = Vector2.one;
        illustRt.offsetMin = new Vector2(4, 4);
        illustRt.offsetMax = new Vector2(-4, -4);
        Image illustImg = illustObj.GetComponent<Image>();
        illustImg.type = Image.Type.Simple;
        illustImg.preserveAspect = true;
        illustImg.color = Color.white;

        // Scanline overlay (Hiệu ứng CRT công nghệ cao tinh tế)
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
        slImg.color = new Color(1f, 1f, 1f, 0.15f);
        slImg.raycastTarget = false;

        // 6. Dialogue / Story Text
        GameObject storyObj = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        storyObj.transform.SetParent(contentObj.transform, false);
        LayoutElement storyLe = storyObj.GetComponent<LayoutElement>();
        storyLe.minHeight = 50f;
        storyLe.preferredHeight = -1; // Để TextMeshPro tự tính toán chiều cao chính xác theo nội dung
        storyLe.flexibleHeight = 0f;
        TextMeshProUGUI storyTmp = storyObj.GetComponent<TextMeshProUGUI>();
        storyTmp.text = "Event story text...";
        storyTmp.fontSize = 28;
        storyTmp.fontStyle = FontStyles.Normal;
        storyTmp.alignment = TextAlignmentOptions.Top;
        storyTmp.color = new Color32(220, 242, 248, 255);
        storyTmp.lineSpacing = 6f;
        storyTmp.margin = new Vector4(16, 0, 16, 0);
        storyTmp.enableWordWrapping = true;
        storyTmp.raycastTarget = false;
        if (defaultFont != null) storyTmp.font = defaultFont;

        // 7. Reward Text (Màu vàng kim nổi bật sau khi lựa chọn)
        GameObject rewardObj = new GameObject("RewardText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        rewardObj.transform.SetParent(contentObj.transform, false);
        LayoutElement rewardLe = rewardObj.GetComponent<LayoutElement>();
        rewardLe.minHeight = 44f;
        rewardLe.preferredHeight = -1; // Tự co giãn theo nội dung thưởng
        rewardLe.flexibleHeight = 0f;
        TextMeshProUGUI rewardTmp = rewardObj.GetComponent<TextMeshProUGUI>();
        rewardTmp.text = "Reward Text";
        rewardTmp.fontSize = 32;
        rewardTmp.fontStyle = FontStyles.Bold;
        rewardTmp.alignment = TextAlignmentOptions.Center;
        rewardTmp.color = new Color32(255, 198, 54, 255); // #FFC636
        rewardTmp.enableWordWrapping = true;
        rewardTmp.raycastTarget = false;
        if (defaultFont != null) rewardTmp.font = defaultFont;
        rewardObj.SetActive(false);

        // 8. Options Container (Chứa các nút rẽ nhánh)
        GameObject optsObj = new GameObject("OptionsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        optsObj.transform.SetParent(contentObj.transform, false);
        RectTransform optsRt = optsObj.GetComponent<RectTransform>();
        optsRt.sizeDelta = new Vector2(840f, 0f);
        VerticalLayoutGroup optsVlg = optsObj.GetComponent<VerticalLayoutGroup>();
        optsVlg.childAlignment = TextAnchor.MiddleCenter;
        optsVlg.spacing = 14f;
        optsVlg.childControlWidth = true;
        optsVlg.childControlHeight = true;
        optsVlg.childForceExpandWidth = true;
        optsVlg.childForceExpandHeight = false;

        // 9. Gắn Controller
        GameplayEventModalController controller = root.AddComponent<GameplayEventModalController>();
        controller.modalRoot = root;
        controller.dimBackground = bgImg;
        controller.titleText = titleTmp;
        controller.illustrationImage = illustImg;
        controller.scanlineOverlay = slImg;
        controller.dialogueText = storyTmp;
        controller.rewardText = rewardTmp;
        controller.optionsContainer = optsRt;

        root.SetActive(false);
        runtimeInstance = controller;
        return controller;
    }
}
