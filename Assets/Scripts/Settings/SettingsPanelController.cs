using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Màn Settings phong cách pixel/tech được tạo theo Canvas hiện tại.
/// Panel chỉ phủ vùng nội dung để TopBar và BottomNavigation vẫn hiển thị.
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    private static readonly Color32 PageColor = new Color32(7, 59, 82, 252);
    private static readonly Color32 CardColor = new Color32(18, 67, 82, 255);
    private static readonly Color32 ButtonColor = new Color32(63, 163, 157, 255);
    private static readonly Color32 HeaderColor = new Color32(27, 184, 174, 255);
    private static readonly Color32 BorderColor = new Color32(100, 235, 222, 255);
    private static readonly Color32 DarkBorderColor = new Color32(3, 29, 39, 255);
    private static readonly Color32 OnColor = new Color32(255, 194, 54, 255);
    private static readonly Color32 OffColor = new Color32(255, 102, 92, 255);

    public static SettingsPanelController Instance { get; private set; }
    public bool IsOpen => gameObject.activeSelf;

    [Header("UI Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bgmText;
    [SerializeField] private TMP_Text sfxText;
    [SerializeField] private TMP_Text languageText;
    [SerializeField] private TMP_Text showDamageText;
    [SerializeField] private TMP_Text joystickText;
    [SerializeField] private TMP_Text screenShakeText;
    [SerializeField] private TMP_Text copyFeedbackText;
    [SerializeField] private TMP_Text saveStateText;
    [SerializeField] private TMP_Text reviewText;
    [SerializeField] private TMP_Text closeHintText;

    [Header("Button References")]
    [SerializeField] private UnityEngine.UI.Button copyIdButton;
    [SerializeField] private UnityEngine.UI.Button bgmButton;
    [SerializeField] private UnityEngine.UI.Button sfxButton;
    [SerializeField] private UnityEngine.UI.Button languageButton;
    [SerializeField] private UnityEngine.UI.Button showDamageButton;
    [SerializeField] private UnityEngine.UI.Button joystickButton;
    [SerializeField] private UnityEngine.UI.Button screenShakeButton;
    [SerializeField] private UnityEngine.UI.Button reviewButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private UnityEngine.UI.Button firstSelectedButton;

    private void Awake()
    {
        Instance = this;
        AutoWireReferencesIfMissing();
        BindButtonListeners();
    }

    private void Start()
    {
        RefreshLabels();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshLabels();
        if (EventSystem.current != null && firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void BindButtonListeners()
    {
        if (copyIdButton != null) { copyIdButton.onClick.RemoveListener(CopyPlayerId); copyIdButton.onClick.AddListener(CopyPlayerId); }
        if (bgmButton != null) { bgmButton.onClick.RemoveAllListeners(); bgmButton.onClick.AddListener(() => { GameSettings.BgmEnabled = !GameSettings.BgmEnabled; RefreshLabels(); }); }
        if (sfxButton != null) { sfxButton.onClick.RemoveAllListeners(); sfxButton.onClick.AddListener(() => { GameSettings.SfxEnabled = !GameSettings.SfxEnabled; RefreshLabels(); }); }
        if (languageButton != null) { languageButton.onClick.RemoveAllListeners(); languageButton.onClick.AddListener(() => { GameSettings.Language = GameSettings.Language == "English" ? "Tiếng Việt" : "English"; RefreshLabels(); }); }
        if (showDamageButton != null) { showDamageButton.onClick.RemoveAllListeners(); showDamageButton.onClick.AddListener(() => { GameSettings.ShowDamage = !GameSettings.ShowDamage; RefreshLabels(); }); }
        if (joystickButton != null) { joystickButton.onClick.RemoveAllListeners(); joystickButton.onClick.AddListener(() => { GameSettings.DynamicJoystick = !GameSettings.DynamicJoystick; RefreshLabels(); }); }
        if (screenShakeButton != null) { screenShakeButton.onClick.RemoveAllListeners(); screenShakeButton.onClick.AddListener(() => { GameSettings.ScreenShake = !GameSettings.ScreenShake; RefreshLabels(); }); }
        if (reviewButton != null) { reviewButton.onClick.RemoveAllListeners(); reviewButton.onClick.AddListener(OpenStoreReview); }
        if (closeButton != null) { closeButton.onClick.RemoveListener(Close); closeButton.onClick.AddListener(Close); }
    }

    public void RefreshLabels()
    {
        bool vi = GameSettings.IsVietnamese;
        if (titleText != null) titleText.text = vi ? "CÀI ĐẶT" : "SETTINGS";
        SetToggleLabel(bgmText, vi ? "NHẠC NỀN" : "BGM", GameSettings.BgmEnabled, vi ? "Âm lượng 100%" : "Vol. 100%");
        SetToggleLabel(sfxText, vi ? "HIỆU ỨNG" : "SFX", GameSettings.SfxEnabled, vi ? "Âm lượng 100%" : "Vol. 100%");
        if (languageText != null) languageText.text = vi ? "TIẾNG VIỆT" : "ENGLISH";
        SetToggleLabel(showDamageText, vi ? "HIỆN SÁT THƯƠNG" : "SHOW DAMAGE", GameSettings.ShowDamage);
        if (joystickText != null)
        {
            joystickText.text = vi
                ? (GameSettings.DynamicJoystick ? "CẦN ĐIỀU KHIỂN:  <color=#FFC236>ĐỘNG</color>" : "CẦN ĐIỀU KHIỂN:  <color=#FFC236>CỐ ĐỊNH</color>")
                : (GameSettings.DynamicJoystick ? "JOYSTICK:  <color=#FFC236>DYNAMIC</color>" : "JOYSTICK:  <color=#FFC236>FIXED</color>");
        }
        SetToggleLabel(screenShakeText, vi ? "RUNG MÀN HÌNH" : "SCREEN SHAKE", GameSettings.ScreenShake);
        if (saveStateText != null)
        {
            saveStateText.text = vi
                ? "TỰ ĐỘNG LƯU CỤC BỘ  <color=#FFC236>BẬT</color>\n<size=23>Tiến trình được lưu trên thiết bị này</size>"
                : "LOCAL AUTO SAVE  <color=#FFC236>ON</color>\n<size=23>Progress is stored on this device</size>";
        }
        if (copyFeedbackText != null) copyFeedbackText.text = vi ? "SAO CHÉP ID" : "COPY ID";
        if (reviewText != null) reviewText.text = vi ? "VIẾT ĐÁNH GIÁ" : "WRITE A REVIEW";
        if (closeHintText != null) closeHintText.text = vi ? "Chạm bánh răng lần nữa để đóng" : "Tap the gear again to close";
    }

    private static void SetToggleLabel(TMP_Text label, string title, bool enabled, string secondLine = null)
    {
        if (label == null) return;
        Color32 stateColor = enabled ? OnColor : OffColor;
        string color = ColorUtility.ToHtmlStringRGB(stateColor);
        bool vi = GameSettings.IsVietnamese;
        string state = enabled ? (vi ? "BẬT" : "ON") : (vi ? "TẮT" : "OFF");
        label.text = string.IsNullOrEmpty(secondLine)
            ? $"{title}  <color=#{color}>{state}</color>"
            : $"{title}  <color=#{color}>{state}</color>\n<size=25>{secondLine}</size>";
    }

    private void CopyPlayerId()
    {
        GUIUtility.systemCopyBuffer = GameSettings.LocalPlayerId;
        if (copyFeedbackText != null) copyFeedbackText.text = GameSettings.IsVietnamese ? "ĐÃ SAO CHÉP!" : "COPIED!";
        CancelInvoke(nameof(RestoreCopyLabel));
        Invoke(nameof(RestoreCopyLabel), 1.2f);
    }

    private void RestoreCopyLabel()
    {
        if (copyFeedbackText != null) copyFeedbackText.text = GameSettings.IsVietnamese ? "SAO CHÉP ID" : "COPY ID";
    }

    private static void OpenStoreReview()
    {
        string packageId = string.IsNullOrEmpty(Application.identifier)
            ? "com.pge.greenearth"
            : Application.identifier;
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.OpenURL($"market://details?id={packageId}");
#else
        Application.OpenURL($"https://play.google.com/store/apps/details?id={packageId}");
#endif
    }

    public void AutoWireReferencesIfMissing()
    {
        Transform safeContent = transform.Find("SafeContent") ?? transform;
        
        if (titleText == null) titleText = safeContent.Find("Title")?.GetComponent<TMP_Text>();
        if (saveStateText == null) saveStateText = safeContent.Find("AccountCard/SaveState")?.GetComponent<TMP_Text>();
        if (closeHintText == null) closeHintText = safeContent.Find("CloseHint")?.GetComponent<TMP_Text>();

        if (copyIdButton == null) copyIdButton = safeContent.Find("AccountCard/CopyIdButton")?.GetComponent<UnityEngine.UI.Button>();
        if (copyFeedbackText == null && copyIdButton != null) copyFeedbackText = copyIdButton.GetComponentInChildren<TMP_Text>(true);

        if (bgmButton == null) bgmButton = safeContent.Find("BgmButton")?.GetComponent<UnityEngine.UI.Button>();
        if (bgmText == null && bgmButton != null) bgmText = bgmButton.GetComponentInChildren<TMP_Text>(true);

        if (sfxButton == null) sfxButton = safeContent.Find("SfxButton")?.GetComponent<UnityEngine.UI.Button>();
        if (sfxText == null && sfxButton != null) sfxText = sfxButton.GetComponentInChildren<TMP_Text>(true);

        if (languageButton == null) languageButton = safeContent.Find("LanguageButton")?.GetComponent<UnityEngine.UI.Button>();
        if (languageText == null && languageButton != null) languageText = languageButton.GetComponentInChildren<TMP_Text>(true);

        if (showDamageButton == null) showDamageButton = safeContent.Find("ShowDamageButton")?.GetComponent<UnityEngine.UI.Button>();
        if (showDamageText == null && showDamageButton != null) showDamageText = showDamageButton.GetComponentInChildren<TMP_Text>(true);

        if (joystickButton == null) joystickButton = safeContent.Find("JoystickModeButton")?.GetComponent<UnityEngine.UI.Button>();
        if (joystickText == null && joystickButton != null) joystickText = joystickButton.GetComponentInChildren<TMP_Text>(true);

        if (screenShakeButton == null) screenShakeButton = safeContent.Find("ScreenShakeButton")?.GetComponent<UnityEngine.UI.Button>();
        if (screenShakeText == null && screenShakeButton != null) screenShakeText = screenShakeButton.GetComponentInChildren<TMP_Text>(true);

        if (reviewButton == null) reviewButton = safeContent.Find("ReviewButton")?.GetComponent<UnityEngine.UI.Button>();
        if (reviewText == null && reviewButton != null) reviewText = reviewButton.GetComponentInChildren<TMP_Text>(true);

        if (firstSelectedButton == null) firstSelectedButton = bgmButton;
    }

    public static SettingsPanelController CreateRuntimePanel(RectTransform canvasParent)
    {
        if (canvasParent == null) return null;

        SettingsPanelController existing = canvasParent.GetComponentInChildren<SettingsPanelController>(true);
        if (existing != null) return existing;

        return BuildPanelHierarchy(canvasParent);
    }

    public static SettingsPanelController BuildPanelHierarchy(RectTransform canvasParent)
    {
        TMP_FontAsset font = FindFont(canvasParent);
        GameObject root = CreateRect("SettingsPanel", canvasParent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, Vector2.zero, Vector2.one, new Vector2(0f, 220f), new Vector2(0f, -165f));

        UnityEngine.UI.Image page = root.AddComponent<UnityEngine.UI.Image>();
        page.color = PageColor;
        page.raycastTarget = true;

        BuildDecorations(root.transform);

        GameObject content = CreateRect("SafeContent", root.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(900f, 1450f);

        CreateFrame("Header", content.transform, new Vector2(0f, 640f), new Vector2(850f, 105f), HeaderColor, BorderColor);
        TMP_Text title = CreateText("Title", content.transform, "SETTINGS", 52f, Color.white, font);
        SetRect(title.rectTransform, new Vector2(0f, 640f), new Vector2(820f, 90f));

        GameObject account = CreateFrame("AccountCard", content.transform, new Vector2(0f, 490f), new Vector2(850f, 175f), CardColor, BorderColor);
        TMP_Text avatar = CreateText("PlayerBadge", account.transform, "PGE", 37f, OnColor, font);
        SetRect(avatar.rectTransform, new Vector2(-350f, 22f), new Vector2(120f, 90f));
        TMP_Text uid = CreateText("PlayerId", account.transform, $"UID: {GameSettings.LocalPlayerId}", 28f, Color.white, font, TextAlignmentOptions.Left);
        SetRect(uid.rectTransform, new Vector2(35f, 40f), new Vector2(610f, 48f));
        TMP_Text save = CreateText("SaveState", account.transform, "LOCAL AUTO SAVE  <color=#FFC236>ON</color>\n<size=23>Progress is stored on this device</size>", 28f, Color.white, font, TextAlignmentOptions.Left);
        SetRect(save.rectTransform, new Vector2(0f, -35f), new Vector2(540f, 78f));
        UnityEngine.UI.Button copy = CreateButton("CopyIdButton", account.transform, new Vector2(320f, -38f), new Vector2(160f, 76f), "COPY ID", 25f, font, out TMP_Text copyText);

        UnityEngine.UI.Button bgm = CreateButton("BgmButton", content.transform, new Vector2(-215f, 335f), new Vector2(390f, 112f), string.Empty, 34f, font, out TMP_Text bgmLabel);
        UnityEngine.UI.Button sfx = CreateButton("SfxButton", content.transform, new Vector2(215f, 335f), new Vector2(390f, 112f), string.Empty, 34f, font, out TMP_Text sfxLabel);
        UnityEngine.UI.Button language = CreateButton("LanguageButton", content.transform, new Vector2(0f, 205f), new Vector2(620f, 105f), string.Empty, 35f, font, out TMP_Text languageLabel);
        UnityEngine.UI.Button damage = CreateButton("ShowDamageButton", content.transform, new Vector2(0f, 80f), new Vector2(620f, 105f), string.Empty, 33f, font, out TMP_Text damageLabel);
        UnityEngine.UI.Button joystick = CreateButton("JoystickModeButton", content.transform, new Vector2(0f, -45f), new Vector2(620f, 105f), string.Empty, 32f, font, out TMP_Text joystickLabel);
        UnityEngine.UI.Button shake = CreateButton("ScreenShakeButton", content.transform, new Vector2(0f, -170f), new Vector2(620f, 105f), string.Empty, 33f, font, out TMP_Text shakeLabel);
        UnityEngine.UI.Button review = CreateButton("ReviewButton", content.transform, new Vector2(0f, -295f), new Vector2(620f, 105f), "WRITE A REVIEW", 35f, font, out _);

        TMP_Text version = CreateText("Version", content.transform, $"VERSION : {Application.version}", 30f, Color.white, font);
        SetRect(version.rectTransform, new Vector2(0f, -540f), new Vector2(600f, 60f));
        TMP_Text closeHint = CreateText("CloseHint", content.transform, "Tap the gear again to close", 23f, new Color32(126, 205, 207, 255), font);
        SetRect(closeHint.rectTransform, new Vector2(0f, -595f), new Vector2(600f, 45f));

        SettingsPanelController controller = root.AddComponent<SettingsPanelController>();
        controller.titleText = title;
        controller.bgmText = bgmLabel;
        controller.sfxText = sfxLabel;
        controller.languageText = languageLabel;
        controller.showDamageText = damageLabel;
        controller.joystickText = joystickLabel;
        controller.screenShakeText = shakeLabel;
        controller.copyFeedbackText = copyText;
        controller.saveStateText = save;
        controller.reviewText = review.GetComponentInChildren<TMP_Text>(true);
        controller.closeHintText = closeHint;

        controller.copyIdButton = copy;
        controller.bgmButton = bgm;
        controller.sfxButton = sfx;
        controller.languageButton = language;
        controller.showDamageButton = damage;
        controller.joystickButton = joystick;
        controller.screenShakeButton = shake;
        controller.reviewButton = review;
        controller.firstSelectedButton = bgm;

        controller.BindButtonListeners();
        controller.RefreshLabels();
        root.SetActive(false);
        return controller;
    }

    private static TMP_FontAsset FindFont(RectTransform canvasParent)
    {
        TMP_Text sample = canvasParent.GetComponentInChildren<TMP_Text>(true);
        if (sample != null && sample.font != null) return sample.font;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private static void BuildDecorations(Transform parent)
    {
        for (int i = 0; i < 20; i++)
        {
            GameObject motif = CreateRect($"PixelMotif_{i:00}", parent);
            UnityEngine.UI.Image image = motif.AddComponent<UnityEngine.UI.Image>();
            image.color = i % 3 == 0 ? new Color32(39, 184, 205, 80) : new Color32(47, 136, 162, 50);
            image.raycastTarget = false;
            RectTransform rect = motif.GetComponent<RectTransform>();
            float x = ((i * 137) % 1000) - 500f;
            float y = ((i * 223) % 1300) - 650f;
            SetRect(rect, new Vector2(x, y), new Vector2(i % 4 == 0 ? 24f : 12f, i % 4 == 0 ? 24f : 12f));
            rect.localEulerAngles = new Vector3(0f, 0f, i % 2 == 0 ? 45f : 0f);
        }
    }

    private static GameObject CreateFrame(string name, Transform parent, Vector2 position, Vector2 size, Color fill, Color border)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, size);
        UnityEngine.UI.Image outer = root.AddComponent<UnityEngine.UI.Image>();
        outer.color = DarkBorderColor;
        outer.raycastTarget = false;
        UnityEngine.UI.Shadow shadow = root.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color32(0, 0, 0, 190);
        shadow.effectDistance = new Vector2(6f, -7f);

        GameObject borderObject = CreateRect("Border", root.transform);
        UnityEngine.UI.Image borderImage = borderObject.AddComponent<UnityEngine.UI.Image>();
        borderImage.color = border;
        borderImage.raycastTarget = false;
        Stretch(borderObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        GameObject backgroundObject = CreateRect("Background", borderObject.transform);
        UnityEngine.UI.Image background = backgroundObject.AddComponent<UnityEngine.UI.Image>();
        background.color = fill;
        background.raycastTarget = false;
        Stretch(backgroundObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        GameObject highlightObject = CreateRect("Highlight", backgroundObject.transform);
        UnityEngine.UI.Image highlight = highlightObject.AddComponent<UnityEngine.UI.Image>();
        highlight.color = new Color32(186, 255, 240, 80);
        highlight.raycastTarget = false;
        Stretch(highlightObject.GetComponent<RectTransform>(), new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);
        return root;
    }

    private static UnityEngine.UI.Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, string label, float fontSize, TMP_FontAsset font, out TMP_Text text)
    {
        GameObject root = CreateFrame(name, parent, position, size, ButtonColor, BorderColor);
        UnityEngine.UI.Image target = root.GetComponent<UnityEngine.UI.Image>();
        target.raycastTarget = true;
        UnityEngine.UI.Button button = root.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = target;
        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(210, 255, 246, 255);
        colors.pressedColor = new Color32(145, 215, 206, 255);
        colors.selectedColor = new Color32(225, 255, 248, 255);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        text = CreateText("Label", root.transform, label, fontSize, Color.white, font);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 5f), new Vector2(-12f, -5f));
        return button;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = CreateRect(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = new Color32(3, 25, 35, 255);
        text.outlineWidth = 0.18f;
        return text;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return go;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
