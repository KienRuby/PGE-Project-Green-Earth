using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using PGE.Auth;

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

    [Header("Header Image & Sprite")]
    [SerializeField] private UnityEngine.UI.Image headerImage;
    [SerializeField] private Sprite headerSprite;

    [Header("Account / Login Section")]
    [SerializeField] private GameObject accountCard;
    [SerializeField] private bool hideAccountSection = false;

    public bool IsGameplayMode
    {
        get
        {
            if (hideAccountSection) return true;
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return string.Equals(activeScene, "GamePlay", System.StringComparison.OrdinalIgnoreCase);
        }
        set => hideAccountSection = value;
    }

    [Header("UI Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text accountPromptText;
    [SerializeField] private TMP_Text googleLoginText;
    [SerializeField] private TMP_Text appleSignInText;
    [SerializeField] private TMP_Text bgmText;
    [SerializeField] private TMP_Text sfxText;
    [SerializeField] private TMP_Text languageText;
    [SerializeField] private TMP_Text showDamageText;
    [SerializeField] private TMP_Text joystickText;
    [SerializeField] private TMP_Text screenShakeText;
    [SerializeField] private TMP_Text copyFeedbackText;
    [SerializeField] private TMP_Text saveStateText;
    [SerializeField] private TMP_Text reviewText;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_Text closeHintText;

    [Header("Button References")]
    [SerializeField] private UnityEngine.UI.Button googleLoginButton;
    [SerializeField] private UnityEngine.UI.Button appleSignInButton;
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
    [SerializeField] private GameObject languageOptionsPanel;

    [Header("Button Sprites (Từ 'nút màn setting.png')")]
    [SerializeField] private Sprite bgmOnSprite;
    [SerializeField] private Sprite bgmOffSprite;
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;
    [SerializeField] private Sprite englishOnSprite;
    [SerializeField] private Sprite englishOffSprite;
    [SerializeField] private Sprite showDamageOnSprite;
    [SerializeField] private Sprite showDamageOffSprite;
    [SerializeField] private Sprite dynamicPadOnSprite;
    [SerializeField] private Sprite fixedPadOnSprite;
    [SerializeField] private Sprite dynamicFixedPadOffSprite;
    [SerializeField] private Sprite screenShakeOnSprite;
    [SerializeField] private Sprite screenShakeOffSprite;
    [SerializeField] private Sprite reviewOnSprite;
    [SerializeField] private Sprite googleLoginSprite;
    [SerializeField] private Sprite appleSignInSprite;

    private void Awake()
    {
        Instance = this;
        AutoWireReferencesIfMissing();
        ApplyLayoutForCurrentScene();
        EnsureLanguageOptionsPanel();
        BindButtonListeners();
    }

    private void OnEnable()
    {
        AutoWireReferencesIfMissing();
        ApplyLayoutForCurrentScene();
        EnsureLanguageOptionsPanel();
        BindButtonListeners();
        RefreshLabels();
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
        AutoWireReferencesIfMissing();
        ApplyLayoutForCurrentScene();
        BindButtonListeners();
        RefreshLabels();
        if (EventSystem.current != null && firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    public void Close()
    {
        if (languageOptionsPanel != null) languageOptionsPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    public void BindButtonListeners()
    {
        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.RemoveAllListeners();
            googleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
        }

        if (appleSignInButton != null)
        {
            appleSignInButton.onClick.RemoveAllListeners();
            appleSignInButton.onClick.AddListener(OnAppleSignInClicked);
        }

        if (copyIdButton != null)
        {
            copyIdButton.onClick.RemoveListener(CopyPlayerId);
            copyIdButton.onClick.AddListener(CopyPlayerId);
        }

        if (bgmButton != null)
        {
            bgmButton.onClick.RemoveAllListeners();
            bgmButton.onClick.AddListener(() =>
            {
                GameSettings.BgmEnabled = !GameSettings.BgmEnabled;
                RefreshLabels();
            });
        }

        if (sfxButton != null)
        {
            sfxButton.onClick.RemoveAllListeners();
            sfxButton.onClick.AddListener(() =>
            {
                GameSettings.SfxEnabled = !GameSettings.SfxEnabled;
                RefreshLabels();
            });
        }

        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(ToggleLanguageOptions);
        }

        if (showDamageButton != null)
        {
            showDamageButton.onClick.RemoveAllListeners();
            showDamageButton.onClick.AddListener(() =>
            {
                GameSettings.ShowDamage = !GameSettings.ShowDamage;
                RefreshLabels();
            });
        }

        if (joystickButton != null)
        {
            joystickButton.onClick.RemoveAllListeners();
            joystickButton.onClick.AddListener(() =>
            {
                // Chuyển vòng lặp: 0 (Dynamic Pad ON) -> 1 (Fixed Pad ON) -> 2 (Dynamic/Fixed Pad OFF)
                GameSettings.JoystickMode = (GameSettings.JoystickMode + 1) % 3;
                RefreshLabels();
            });
        }

        if (screenShakeButton != null)
        {
            screenShakeButton.onClick.RemoveAllListeners();
            screenShakeButton.onClick.AddListener(() =>
            {
                GameSettings.ScreenShake = !GameSettings.ScreenShake;
                RefreshLabels();
            });
        }

        if (reviewButton != null)
        {
            reviewButton.onClick.RemoveAllListeners();
            reviewButton.onClick.AddListener(OpenStoreReview);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnGoogleLoginClicked()
    {
        if (GoogleAuthManager.Instance != null && !GoogleAuthManager.Instance.IsLoggedIn)
        {
            GoogleAuthManager.Instance.SignInWithGoogle((success, user) =>
            {
                if (success)
                {
                    CloudSaveSyncService.LoadFromCloud();
                }
                RefreshLabels();
            });
        }
        else if (GoogleAuthManager.Instance != null && GoogleAuthManager.Instance.IsLoggedIn)
        {
            GoogleAuthManager.Instance.SignOut(() =>
            {
                RefreshLabels();
            });
        }
        else
        {
            GameSettings.GoogleAccount = string.IsNullOrEmpty(GameSettings.GoogleAccount)
                ? $"Google_{GameSettings.LocalPlayerId.Substring(0, 6)}"
                : string.Empty;
            RefreshLabels();
        }
    }

    private void OnAppleSignInClicked()
    {
        if (AppleAuthManager.Instance != null && !AppleAuthManager.Instance.IsLoggedIn)
        {
            AppleAuthManager.Instance.SignInWithApple((success, user) =>
            {
                if (success)
                {
                    CloudSaveSyncService.LoadFromCloud();
                }
                RefreshLabels();
            });
        }
        else if (AppleAuthManager.Instance != null && AppleAuthManager.Instance.IsLoggedIn)
        {
            AppleAuthManager.Instance.SignOut(() =>
            {
                RefreshLabels();
            });
        }
        else
        {
            GameSettings.AppleAccount = string.IsNullOrEmpty(GameSettings.AppleAccount)
                ? $"Apple_{GameSettings.LocalPlayerId.Substring(0, 6)}"
                : string.Empty;
            RefreshLabels();
        }
    }

    public void RefreshLabels()
    {
        LoadSettingSpritesIfMissing();

        bool vi = GameSettings.IsVietnamese;

        // Header Setting Sprite
        if (headerImage != null && headerSprite != null)
        {
            Transform border = headerImage.transform.Find("Border");
            if (border != null) border.gameObject.SetActive(false);
            var shadow = headerImage.GetComponent<UnityEngine.UI.Shadow>();
            if (shadow != null) shadow.enabled = false;
            headerImage.sprite = headerSprite;
            headerImage.color = Color.white;
            if (titleText != null) titleText.text = string.Empty;
        }
        else if (titleText != null)
        {
            Transform border = headerImage != null ? headerImage.transform.Find("Border") : null;
            if (border != null) border.gameObject.SetActive(true);
            titleText.text = vi ? "CÀI ĐẶT" : "Setting";
        }

        if (accountPromptText != null)
            accountPromptText.text = vi ? "Đăng nhập để lưu dữ liệu của bạn!" : "Log in and save your data!";

        // 1. BGM Button (BGM ON: Sáng, BGM OFF: Tối)
        SetButtonSprite(bgmButton, GameSettings.BgmEnabled ? bgmOnSprite : bgmOffSprite, bgmText, vi ? "NHẠC NỀN" : "BGM", GameSettings.BgmEnabled);

        // 2. SFX Button (SFX ON: Sáng, SFX OFF: Tối)
        SetButtonSprite(sfxButton, GameSettings.SfxEnabled ? sfxOnSprite : sfxOffSprite, sfxText, vi ? "HIỆU ỨNG" : "SFX", GameSettings.SfxEnabled);

        // 3. Language Button (English / English ON: Sáng, English OFF: Tối)
        bool isEnglish = GameSettings.Language == GameSettings.EnglishLanguage;
        SetButtonSprite(languageButton, isEnglish ? englishOnSprite : englishOffSprite, languageText, GameSettings.Language, isEnglish);

        // 4. Show Damage Button (Show Damage On: Sáng, Show Damage Off: Tối)
        SetButtonSprite(showDamageButton, GameSettings.ShowDamage ? showDamageOnSprite : showDamageOffSprite, showDamageText, vi ? "HIỆN SÁT THƯƠNG" : "Show Damage", GameSettings.ShowDamage);

        // 5. Joystick Button (3 Nút: Dynamic Pad On (Sáng) -> Fixed Pad ON (Sáng) -> Dynamic-Fixed Pad OFF (Tối))
        Sprite joystickSprite;
        switch (GameSettings.JoystickMode)
        {
            case 0:
                joystickSprite = dynamicPadOnSprite ?? fixedPadOnSprite;
                break;
            case 1:
                joystickSprite = fixedPadOnSprite ?? dynamicPadOnSprite;
                break;
            default:
                joystickSprite = dynamicFixedPadOffSprite ?? fixedPadOnSprite;
                break;
        }
        SetButtonSprite(joystickButton, joystickSprite, joystickText, vi ? "CẦN ĐIỀU KHIỂN" : "Dynamic/ Fixed Pad", GameSettings.JoystickEnabled);

        // 6. Screen Shake Button (Screen Shake ON: Sáng, Screen Shake OFF: Tối)
        SetButtonSprite(screenShakeButton, GameSettings.ScreenShake ? screenShakeOnSprite : screenShakeOffSprite, screenShakeText, vi ? "RUNG MÀN HÌNH" : "Screen Shake", GameSettings.ScreenShake);

        // 7. Write a review Button
        SetButtonSprite(reviewButton, reviewOnSprite, reviewText, vi ? "VIẾT ĐÁNH GIÁ" : "Write a review", true);

        // 8. Google & Apple Buttons
        bool isGoogleLoggedIn = (GoogleAuthManager.Instance != null && GoogleAuthManager.Instance.IsLoggedIn) || GameSettings.IsLoggedInGoogle;
        bool isAppleLoggedIn = (AppleAuthManager.Instance != null && AppleAuthManager.Instance.IsLoggedIn) || GameSettings.IsLoggedInApple;

        if (googleLoginButton != null)
        {
            string googleLabel = isGoogleLoggedIn
                ? (vi ? "GOOGLE: ĐÃ ĐĂNG NHẬP (LOGGED IN)" : "GOOGLE: LOGGED IN")
                : (vi ? "ĐĂNG NHẬP GOOGLE" : "LOG IN WITH GOOGLE");
            if (googleLoginSprite != null)
                SetButtonSprite(googleLoginButton, googleLoginSprite, googleLoginText, null, true);
            else if (googleLoginText != null)
                googleLoginText.text = googleLabel;
        }

        if (appleSignInButton != null)
        {
            string appleLabel = isAppleLoggedIn
                ? (vi ? "APPLE: ĐÃ ĐĂNG NHẬP (SIGNED IN)" : "APPLE: SIGNED IN")
                : (vi ? "ĐĂNG NHẬP APPLE" : "SIGN IN WITH APPLE");
            if (appleSignInSprite != null)
                SetButtonSprite(appleSignInButton, appleSignInSprite, appleSignInText, null, true);
            else if (appleSignInText != null)
                appleSignInText.text = appleLabel;
        }

        // 9. Version & Close hint
        if (versionText != null)
            versionText.text = $"VERSION : {Application.version}";

        if (saveStateText != null)
        {
            if (isGoogleLoggedIn)
            {
                string userName = GoogleAuthManager.Instance?.CurrentUser?.displayName ?? "Google Account";
                saveStateText.text = vi
                    ? $"ĐỒNG BỘ ĐÁM MÂY GOOGLE  <color=#FFC236>BẬT</color>\n<size=23>{userName} • Đã lưu đám mây</size>"
                    : $"GOOGLE CLOUD SYNC  <color=#FFC236>ON</color>\n<size=23>{userName} • Cloud secured</size>";
            }
            else if (isAppleLoggedIn)
            {
                string userName = AppleAuthManager.Instance?.CurrentUser?.displayName ?? "Apple ID";
                saveStateText.text = vi
                    ? $"ĐỒNG BỘ ĐÁM MÂY APPLE  <color=#FFC236>BẬT</color>\n<size=23>{userName} • Đã lưu đám mây</size>"
                    : $"APPLE CLOUD SYNC  <color=#FFC236>ON</color>\n<size=23>{userName} • Cloud secured</size>";
            }
            else
            {
                saveStateText.text = vi
                    ? "TỰ ĐỘNG LƯU CỤC BỘ  <color=#FFC236>BẬT</color>\n<size=23>Tiến trình được lưu trên thiết bị này</size>"
                    : "LOCAL AUTO SAVE  <color=#FFC236>ON</color>\n<size=23>Progress is stored on this device</size>";
            }
        }

        if (copyFeedbackText != null) copyFeedbackText.text = vi ? "SAO CHÉP ID" : "COPY ID";
        if (closeHintText != null) closeHintText.text = vi ? "Chạm bánh răng lần nữa để đóng" : "Tap the gear again to close";
    }

    private void SetButtonSprite(UnityEngine.UI.Button button, Sprite sprite, TMP_Text textComponent, string labelTitle, bool isEnabled)
    {
        if (button == null) return;

        UnityEngine.UI.Image img = button.GetComponent<UnityEngine.UI.Image>()
                                ?? button.targetGraphic as UnityEngine.UI.Image;
        if (img != null && sprite != null)
        {
            Transform border = button.transform.Find("Border");
            if (border != null) border.gameObject.SetActive(false);

            Transform bg = button.transform.Find("Background");
            if (bg != null) bg.gameObject.SetActive(false);

            var shadow = button.GetComponent<UnityEngine.UI.Shadow>();
            if (shadow != null) shadow.enabled = false;

            img.sprite = sprite;
            img.color = Color.white;
        }
        else if (img != null && sprite == null)
        {
            Transform border = button.transform.Find("Border");
            if (border != null) border.gameObject.SetActive(true);

            var shadow = button.GetComponent<UnityEngine.UI.Shadow>();
            if (shadow != null) shadow.enabled = true;
        }

        if (textComponent != null)
        {
            if (sprite != null)
            {
                // Khi nút đã có ảnh vẽ sẵn chữ pixel sắc nét, ẩn text đè để giao diện không bị trùng lặp
                textComponent.text = string.Empty;
            }
            else if (!string.IsNullOrEmpty(labelTitle))
            {
                SetToggleLabel(textComponent, labelTitle, isEnabled);
            }
        }
    }

    public void LoadSettingSpritesIfMissing()
    {
#if UNITY_EDITOR
        if (bgmOnSprite == null || englishOnSprite == null || headerSprite == null)
        {
            string spriteSheetPath = "Assets/Sprites/UI/setting/nút màn setting.png";
            Sprite[] allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
                .OfType<Sprite>()
                .ToArray();

            foreach (var spr in allSprites)
            {
                switch (spr.name)
                {
                    case "Setting": headerSprite = spr; break;
                    case "BGM ON": bgmOnSprite = spr; break;
                    case "BGM OFF": bgmOffSprite = spr; break;
                    case "SFX ON": sfxOnSprite = spr; break;
                    case "SFX OFF": sfxOffSprite = spr; break;
                    case "English":
                    case "English ON": englishOnSprite = spr; break;
                    case "English OFF": englishOffSprite = spr; break;
                    case "Show Damage On": showDamageOnSprite = spr; break;
                    case "Show Damage Off": showDamageOffSprite = spr; break;
                    case "Dynamic Pad On": dynamicPadOnSprite = spr; break;
                    case "Fixed Pad ON": fixedPadOnSprite = spr; break;
                    case "Dynamic-Fixed Pad OFF": dynamicFixedPadOffSprite = spr; break;
                    case "Screen Shake ON": screenShakeOnSprite = spr; break;
                    case "Screen Shake OFF": screenShakeOffSprite = spr; break;
                    case "Write a review On": reviewOnSprite = spr; break;
                    case "Log in with Google": googleLoginSprite = spr; break;
                    case "Sign in with Apple": appleSignInSprite = spr; break;
                }
            }
        }
#endif
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
        LoadSettingSpritesIfMissing();

        // 1. Tìm các đối tượng văn bản (TMP_Text)
        if (titleText == null) titleText = FindMatchingComponent<TMP_Text>("Title", "Header");
        if (accountPromptText == null) accountPromptText = FindMatchingComponent<TMP_Text>("Prompt", "Log in and save", "SaveDataPrompt");
        if (saveStateText == null) saveStateText = FindMatchingComponent<TMP_Text>("SaveState");
        if (versionText == null) versionText = FindMatchingComponent<TMP_Text>("Version");
        if (closeHintText == null) closeHintText = FindMatchingComponent<TMP_Text>("CloseHint");

        // 2. Tìm và liên kết các nút (Buttons)
        if (googleLoginButton == null) googleLoginButton = FindMatchingComponent<UnityEngine.UI.Button>("Log in with Google", "GoogleLoginButton", "Google");
        if (googleLoginText == null && googleLoginButton != null) googleLoginText = googleLoginButton.GetComponentInChildren<TMP_Text>(true);

        if (appleSignInButton == null) appleSignInButton = FindMatchingComponent<UnityEngine.UI.Button>("Sign in with Apple", "AppleSignInButton", "Apple");
        if (appleSignInText == null && appleSignInButton != null) appleSignInText = appleSignInButton.GetComponentInChildren<TMP_Text>(true);

        if (bgmButton == null) bgmButton = FindMatchingComponent<UnityEngine.UI.Button>("BgmButton", "BGM");
        if (bgmText == null && bgmButton != null) bgmText = bgmButton.GetComponentInChildren<TMP_Text>(true);

        if (sfxButton == null) sfxButton = FindMatchingComponent<UnityEngine.UI.Button>("SfxButton", "SFX");
        if (sfxText == null && sfxButton != null) sfxText = sfxButton.GetComponentInChildren<TMP_Text>(true);

        if (languageButton == null) languageButton = FindMatchingComponent<UnityEngine.UI.Button>("LanguageButton", "Language");
        if (languageText == null && languageButton != null) languageText = languageButton.GetComponentInChildren<TMP_Text>(true);

        if (showDamageButton == null) showDamageButton = FindMatchingComponent<UnityEngine.UI.Button>("ShowDamageButton", "ShowDamage");
        if (showDamageText == null && showDamageButton != null) showDamageText = showDamageButton.GetComponentInChildren<TMP_Text>(true);

        if (joystickButton == null) joystickButton = FindMatchingComponent<UnityEngine.UI.Button>("JoystickModeButton", "JoystickButton", "Joystick");
        if (joystickText == null && joystickButton != null) joystickText = joystickButton.GetComponentInChildren<TMP_Text>(true);

        if (screenShakeButton == null) screenShakeButton = FindMatchingComponent<UnityEngine.UI.Button>("ScreenShakeButton", "ShakeButton", "ScreenShake");
        if (screenShakeText == null && screenShakeButton != null) screenShakeText = screenShakeButton.GetComponentInChildren<TMP_Text>(true);

        if (reviewButton == null) reviewButton = FindMatchingComponent<UnityEngine.UI.Button>("ReviewButton", "Review");
        if (reviewText == null && reviewButton != null) reviewText = reviewButton.GetComponentInChildren<TMP_Text>(true);

        if (copyIdButton == null) copyIdButton = FindMatchingComponent<UnityEngine.UI.Button>("CopyIdButton", "Copy");
        if (copyFeedbackText == null && copyIdButton != null) copyFeedbackText = copyIdButton.GetComponentInChildren<TMP_Text>(true);

        if (headerImage == null)
        {
            Transform h = transform.Find("SafeContent/Header");
            if (h != null) headerImage = h.GetComponent<UnityEngine.UI.Image>();
        }

        if (accountCard == null)
        {
            Transform acc = transform.Find("SafeContent/AccountCard");
            if (acc != null) accountCard = acc.gameObject;
            else
            {
                var accComp = FindMatchingComponent<Transform>("AccountCard", "Account");
                if (accComp != null && accComp != transform) accountCard = accComp.gameObject;
            }
        }

        if (firstSelectedButton == null) firstSelectedButton = bgmButton;
    }

    public void ApplyLayoutForCurrentScene()
    {
        bool gameplay = IsGameplayMode;

        if (accountCard != null)
        {
            accountCard.SetActive(!gameplay);
        }
        if (accountPromptText != null) accountPromptText.gameObject.SetActive(!gameplay);
        if (googleLoginButton != null) googleLoginButton.gameObject.SetActive(!gameplay);
        if (appleSignInButton != null) appleSignInButton.gameObject.SetActive(!gameplay);

        if (gameplay)
        {
            ApplyGameplayLayout();
        }
    }

    public void ApplyGameplayLayout()
    {
        // 1. Header Setting
        Transform header = transform.Find("SafeContent/Header");
        if (header != null)
        {
            RectTransform rt = header.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, 500f);
                rt.sizeDelta = new Vector2(680f, 96f);
            }
        }
        if (titleText != null)
        {
            titleText.rectTransform.anchoredPosition = new Vector2(0f, 500f);
            titleText.rectTransform.sizeDelta = new Vector2(650f, 85f);
        }

        // 2. Row 1: BGM (-149, 350) & SFX (149, 350)
        if (bgmButton != null)
        {
            RectTransform rt = bgmButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(-149f, 350f);
                rt.sizeDelta = new Vector2(282f, 160f);
            }
        }
        if (sfxButton != null)
        {
            RectTransform rt = sfxButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(149f, 350f);
                rt.sizeDelta = new Vector2(282f, 160f);
            }
        }

        // 3. Row 2: English (0, 180)
        if (languageButton != null)
        {
            RectTransform rt = languageButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, 180f);
                rt.sizeDelta = new Vector2(580f, 160f);
            }
        }

        // 4. Row 3: Show Damage (0, 10)
        if (showDamageButton != null)
        {
            RectTransform rt = showDamageButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, 10f);
                rt.sizeDelta = new Vector2(580f, 160f);
            }
        }

        // 5. Row 4: Joystick Mode (0, -160)
        if (joystickButton != null)
        {
            RectTransform rt = joystickButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -160f);
                rt.sizeDelta = new Vector2(580f, 160f);
            }
        }

        // 6. Row 5: Screen Shake (0, -330)
        if (screenShakeButton != null)
        {
            RectTransform rt = screenShakeButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -330f);
                rt.sizeDelta = new Vector2(580f, 160f);
            }
        }

        // 7. Row 6: Review (0, -500)
        if (reviewButton != null)
        {
            RectTransform rt = reviewButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -500f);
                rt.sizeDelta = new Vector2(580f, 160f);
            }
        }

        // Bottom labels
        if (versionText != null)
        {
            versionText.rectTransform.anchoredPosition = new Vector2(0f, -615f);
        }
        if (closeHintText != null)
        {
            closeHintText.rectTransform.anchoredPosition = new Vector2(0f, -665f);
        }
    }

    public void ToggleLanguageOptions()
    {
        EnsureLanguageOptionsPanel();
        if (languageOptionsPanel == null) return;

        languageOptionsPanel.SetActive(!languageOptionsPanel.activeSelf);
        if (languageOptionsPanel.activeSelf) languageOptionsPanel.transform.SetAsLastSibling();
    }

    public void SelectLanguage(string language)
    {
        GameSettings.Language = language;
        PGEGameLocalization.ApplySavedLanguage();
        if (languageOptionsPanel != null) languageOptionsPanel.SetActive(false);
        RefreshLabels();
    }

    private void EnsureLanguageOptionsPanel()
    {
        if (languageOptionsPanel == null)
        {
            Transform existing = transform.Find("SafeContent/LanguageOptionsPanel");
            if (existing != null) languageOptionsPanel = existing.gameObject;
        }

        if (languageOptionsPanel != null || languageButton == null) return;

        Transform parent = languageButton.transform.parent;
        TMP_FontAsset font = languageText != null ? languageText.font : FindFont(GetComponent<RectTransform>());
        languageOptionsPanel = CreateFrame(
            "LanguageOptionsPanel",
            parent,
            Vector2.zero,
            new Vector2(680f, 390f),
            CardColor,
            BorderColor);

        string[] languages = GameSettings.SupportedLanguages;
        for (int i = 0; i < languages.Length; i++)
        {
            string language = languages[i];
            UnityEngine.UI.Button option = CreateButton(
                language + "Button",
                languageOptionsPanel.transform,
                new Vector2(0f, 120f - i * 80f),
                new Vector2(600f, 66f),
                language,
                30f,
                font,
                out _);
            option.onClick.AddListener(() => SelectLanguage(language));
        }

        languageOptionsPanel.transform.SetAsLastSibling();
        languageOptionsPanel.SetActive(false);
    }

    private T FindMatchingComponent<T>(params string[] possibleNames) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (string target in possibleNames)
        {
            string cleanTarget = target.Replace(" ", "").ToLowerInvariant();
            foreach (T comp in components)
            {
                string objName = comp.gameObject.name.Replace(" ", "").ToLowerInvariant();
                if (objName.Equals(cleanTarget) || objName.Contains(cleanTarget))
                {
                    return comp;
                }
            }
        }
        return null;
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

        GameObject headerFrame = CreateFrame("Header", content.transform, new Vector2(0f, 640f), new Vector2(850f, 105f), HeaderColor, BorderColor);
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
        controller.headerImage = headerFrame.GetComponent<UnityEngine.UI.Image>();
        controller.titleText = title;
        controller.accountCard = account;
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

        controller.EnsureLanguageOptionsPanel();
        controller.BindButtonListeners();
        controller.ApplyLayoutForCurrentScene();
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
