using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý toàn bộ Màn hình Chọn Chapter (Chapter Screen):
/// - Chuyển đổi qua lại giữa các Chapter bằng nút [<] và [>].
/// - Tự động cập nhật toàn bộ Text, Ảnh nền xem trước (Preview Background), Bóng quái vật trùm (Boss Silhouette).
/// - Xử lý nút [Start]: Kiểm tra đủ Năng Lượng (Energy), trừ Energy qua ChipManager và chuyển cảnh GamePlay.
/// - Thiết kế Data-Driven: Kết nối trực tiếp với ChapterDatabase (ScriptableObject).
/// </summary>
public class ChapterScreenController : MonoBehaviour
{
    [Header("Chapter Database & Configuration")]
    [Tooltip("ScriptableObject chứa danh sách toàn bộ các Chapter trong game.")]
    [SerializeField] private ChapterDatabase chapterDatabase;

    [Tooltip("Chỉ số Chapter hiển thị ban đầu nếu chưa có dữ liệu lưu trữ (0 = Chapter 1, 3 = Chapter 4,...).")]
    [SerializeField] private int defaultChapterIndex = 0;

    [Header("Chapter Header & Navigation")]
    [Tooltip("Nút mũi tên chuyển về Chapter trước.")]
    [SerializeField] private Button prevChapterButton;

    [Tooltip("Nút mũi tên chuyển sang Chapter kế tiếp.")]
    [SerializeField] private Button nextChapterButton;

    [Tooltip("Text hiển thị số thứ tự Chapter (ví dụ: 'Chapter. 01').")]
    [SerializeField] private TMP_Text chapterSubtitleText;

    [Tooltip("Text hiển thị tên Chapter (ví dụ: 'Yellow Desert 1').")]
    [SerializeField] private TMP_Text chapterTitleText;

    [Header("Stage Preview Window")]
    [Tooltip("Image hiển thị ảnh nền xem trước của màn chơi (sàn rừng, sa mạc,...).")]
    [SerializeField] private Image previewBackgroundImage;

    [Tooltip("Image hiển thị bóng đen quái vật trùm (Boss Silhouette).")]
    [SerializeField] private Image bossSilhouetteImage;

    [Tooltip("Khung chứa biểu tượng và nhãn hiển thị khi Chapter đang bị khóa.")]
    [SerializeField] private GameObject lockOverlay;

    [Tooltip("Text hiển thị số đợt quái (ví dụ: 'WAVE: 01/10').")]
    [SerializeField] private TMP_Text waveBadgeText;

    [Tooltip("Text hiển thị câu thoại dẫn truyện phía dưới quái vật.")]
    [SerializeField] private TMP_Text flavorText;

    [Header("Start Action Button")]
    [Tooltip("Nút Start lớn màu xanh bắt đầu trận đấu.")]
    [SerializeField] private Button startButton;

    [Tooltip("Sprite nút Start ở trạng thái bình thường (Sprite 1: nút start_0).")]
    [SerializeField] private Sprite normalStartSprite;

    [Tooltip("Sprite nút Start khi nhấn vào (Sprite 2: nút start_1).")]
    [SerializeField] private Sprite pressedStartSprite;

    [Tooltip("Text nhãn hiển thị trên nút bắt đầu (ví dụ: 'Start' hoặc 'Locked').")]
    [SerializeField] private TMP_Text startButtonLabel;

    [Tooltip("Khung hiển thị chi phí năng lượng trên nút Start (tự ẩn khi Chapter bị khóa).")]
    [SerializeField] private GameObject costBox;

    [Tooltip("Text hiển thị chi phí năng lượng (ví dụ: 'X 10').")]
    [SerializeField] private TMP_Text energyCostText;

    [Tooltip("Image hiển thị biểu tượng năng lượng trên nút Start.")]
    [SerializeField] private Image energyCostIcon;

    [Tooltip("Màu nút khi đủ năng lượng.")]
    [SerializeField] private Color affordableButtonColor = new Color32(115, 205, 125, 255);

    [Tooltip("Màu nút khi thiếu năng lượng.")]
    [SerializeField] private Color unaffordableButtonColor = new Color32(100, 130, 110, 255);

    [Tooltip("Màu nút khi Chapter bị khóa.")]
    [SerializeField] private Color lockedButtonColor = new Color32(70, 95, 90, 255);

    [Header("Side Mode Action Buttons")]
    [Tooltip("Nút Tower Def ở bên trái nút Start.")]
    [SerializeField] private Button towerDefButton;

    [Header("Tower Def Selection Modal")]
    [SerializeField] private Sprite towerDefPanelSprite;
    [SerializeField] private Sprite towerDefBoardSprite;
    [SerializeField] private Sprite towerDefBackSprite;
    [SerializeField] private Sprite towerDefStartSprite;

    [Tooltip("Nút Gem Mine ở bên phải nút Start.")]
    [SerializeField] private Button gemMineButton;

    [Tooltip("Modal Daily Gem Mine hiển thị khi bấm nút Gem Mine.")]
    [SerializeField] private DailyGemMineModalController gemMineModal;

    public Button TowerDefButton => towerDefButton;
    public Button GemMineButton => gemMineButton;
    public DailyGemMineModalController GemMineModal => gemMineModal;

    [Header("Preview Lighting & Silhouette Styling")]
    [Tooltip("Màu của quái vật/boss khi Chapter đã mở khóa (sáng rõ).")]
    [SerializeField] private Color unlockedBossColor = Color.white;

    [Tooltip("Màu của quái vật/boss khi Chapter chưa mở khóa (tối đen như mực).")]
    [SerializeField] private Color lockedBossColor = new Color32(0, 0, 0, 255);

    [Tooltip("Màu ảnh nền khi Chapter đã mở khóa (sáng rõ).")]
    [SerializeField] private Color unlockedBackgroundColor = Color.white;

    [Tooltip("Màu ảnh nền khi Chapter chưa mở khóa (tối sẫm lại).")]
    [SerializeField] private Color lockedBackgroundColor = new Color32(40, 50, 60, 255);

    public Color UnlockedBossColor => unlockedBossColor;
    public Color LockedBossColor => lockedBossColor;
    public Color UnlockedBackgroundColor => unlockedBackgroundColor;
    public Color LockedBackgroundColor => lockedBackgroundColor;

    private int currentChapterIndex;
    private ChapterData currentChapter;
    private GameObject towerDefModalRoot;
    private ScrollRect towerDefScrollRect;
    private readonly Button[] towerDefLevelStartButtons = new Button[TowerDefProgress.LevelCount];
    private readonly GameObject[] towerDefLevelLockedLabels = new GameObject[TowerDefProgress.LevelCount];

    public event System.Action<int> OnTowerDefLevelSelected;

    private void Awake()
    {
        if (prevChapterButton != null)
        {
            prevChapterButton.onClick.RemoveListener(OnPrevChapterClicked);
            prevChapterButton.onClick.AddListener(OnPrevChapterClicked);
        }

        if (nextChapterButton != null)
        {
            nextChapterButton.onClick.RemoveListener(OnNextChapterClicked);
            nextChapterButton.onClick.AddListener(OnNextChapterClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
            startButton.onClick.AddListener(OnStartButtonClicked);
            SetupStartButtonTransition();
        }

        if (towerDefButton != null)
        {
            towerDefButton.onClick.RemoveListener(OnTowerDefClicked);
            towerDefButton.onClick.AddListener(OnTowerDefClicked);
        }

        if (gemMineButton != null)
        {
            gemMineButton.onClick.RemoveListener(OnGemMineClicked);
            gemMineButton.onClick.AddListener(OnGemMineClicked);
        }

        EnsureHeaderReferencesBound();
    }

    private void EnsureHeaderReferencesBound()
    {
        if (chapterSubtitleText == null)
        {
            chapterSubtitleText = transform.Find("ChapterSelectorHeader/SubtitleText")?.GetComponent<TMP_Text>();
        }
        if (chapterTitleText == null)
        {
            chapterTitleText = transform.Find("ChapterSelectorHeader/TitleText")?.GetComponent<TMP_Text>()
                ?? transform.Find("ChapterSelectorHeader/ChapterTitleText")?.GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        // Khôi phục Chapter đã chọn từ PlayerDataService hoặc mặc định
        currentChapterIndex = PlayerDataService.SelectedChapterIndex;
        if (chapterDatabase != null && chapterDatabase.Count > 0)
        {
            currentChapterIndex = Mathf.Clamp(currentChapterIndex, 0, chapterDatabase.Count - 1);
        }
        else
        {
            currentChapterIndex = defaultChapterIndex;
        }

        EnsureHeaderReferencesBound();
        RefreshChapterView();
    }

    private void OnEnable()
    {
        EnsureHeaderReferencesBound();
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;
        RefreshChapterView();
    }

    private void OnDisable()
    {
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;
        if (towerDefModalRoot != null) towerDefModalRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (prevChapterButton != null) prevChapterButton.onClick.RemoveListener(OnPrevChapterClicked);
        if (nextChapterButton != null) nextChapterButton.onClick.RemoveListener(OnNextChapterClicked);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartButtonClicked);
        if (towerDefButton != null) towerDefButton.onClick.RemoveListener(OnTowerDefClicked);
        if (gemMineButton != null) gemMineButton.onClick.RemoveListener(OnGemMineClicked);
        if (towerDefModalRoot != null)
        {
            if (Application.isPlaying) Destroy(towerDefModalRoot);
            else DestroyImmediate(towerDefModalRoot);
        }
    }

    public void OnPrevChapterClicked()
    {
        if (chapterDatabase == null || chapterDatabase.Count <= 1) return;

        currentChapterIndex--;
        if (currentChapterIndex < 0)
        {
            currentChapterIndex = chapterDatabase.Count - 1; // Vòng lại Chapter cuối cùng
        }

        PlayerDataService.SelectedChapterIndex = currentChapterIndex;
        RefreshChapterView();
    }

    public void OnNextChapterClicked()
    {
        if (chapterDatabase == null || chapterDatabase.Count <= 1) return;

        currentChapterIndex++;
        if (currentChapterIndex >= chapterDatabase.Count)
        {
            currentChapterIndex = 0; // Vòng lại Chapter đầu tiên
        }

        PlayerDataService.SelectedChapterIndex = currentChapterIndex;
        RefreshChapterView();
    }

    public bool IsCurrentChapterLocked()
    {
        if (currentChapter != null && currentChapter.isLocked) return true;
        return currentChapterIndex > PlayerDataService.UnlockedChapterIndex;
    }

    /// <summary>
    /// Kiểm tra xem Chapter đang xem đã được người chơi chiến thắng/vượt qua hay chưa.
    /// </summary>
    public bool IsCurrentChapterCleared()
    {
        return IsChapterCleared(currentChapterIndex);
    }

    /// <summary>
    /// Kiểm tra xem một Chapter (0-based index) đã được chiến thắng/vượt qua hay chưa.
    /// </summary>
    public bool IsChapterCleared(int chapterIndex)
    {
        if (chapterIndex < 0) return false;
        if (currentChapter != null && currentChapterIndex == chapterIndex && currentChapter.isLocked)
        {
            return false;
        }
        return PlayerDataService.IsChapterCleared(chapterIndex);
    }

    public void RefreshChapterView()
    {
        EnsureHeaderReferencesBound();

        if (chapterDatabase != null && chapterDatabase.Count > 0)
        {
            currentChapter = chapterDatabase.GetChapter(currentChapterIndex);
        }

        bool isLocked = IsCurrentChapterLocked();
        bool isCleared = IsCurrentChapterCleared();

        if (currentChapter != null)
        {
            if (chapterSubtitleText != null)
            {
                chapterSubtitleText.gameObject.SetActive(true);
                chapterSubtitleText.text = $"Chapter. {currentChapter.chapterNumber:00}";
                chapterSubtitleText.ForceMeshUpdate(true, true);
            }

            if (chapterTitleText != null)
            {
                chapterTitleText.gameObject.SetActive(true);
                chapterTitleText.text = currentChapter.chapterTitle;
                chapterTitleText.ForceMeshUpdate(true, true);
            }

            if (previewBackgroundImage != null)
            {
                if (currentChapter.previewBackground != null)
                {
                    previewBackgroundImage.sprite = currentChapter.previewBackground;
                }
                previewBackgroundImage.color = isLocked ? lockedBackgroundColor : unlockedBackgroundColor;
            }

            if (bossSilhouetteImage != null)
            {
                if (currentChapter.bossSilhouette != null)
                {
                    bossSilhouetteImage.sprite = currentChapter.bossSilhouette;
                }
                bossSilhouetteImage.gameObject.SetActive(true);
                bossSilhouetteImage.color = isCleared ? unlockedBossColor : lockedBossColor;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(isLocked);
            }

            if (waveBadgeText != null)
            {
                waveBadgeText.text = $"WAVE 01/{currentChapter.totalWaves:00}";
            }

            if (flavorText != null)
            {
                flavorText.text = currentChapter.flavorText;
            }

            if (energyCostText != null)
            {
                energyCostText.text = $"X {currentChapter.energyCost}";
            }

            if (startButtonLabel != null)
            {
                startButtonLabel.text = isLocked ? "Locked" : "Start";
            }

            if (costBox != null)
            {
                costBox.SetActive(!isLocked);
            }
        }
        else
        {
            // Fallback hiển thị mẫu
            if (chapterSubtitleText != null)
            {
                chapterSubtitleText.gameObject.SetActive(true);
                chapterSubtitleText.text = "Chapter. 01";
                chapterSubtitleText.ForceMeshUpdate(true, true);
            }
            if (chapterTitleText != null)
            {
                chapterTitleText.gameObject.SetActive(true);
                chapterTitleText.text = "Yellow Desert 1";
                chapterTitleText.ForceMeshUpdate(true, true);
            }
            if (previewBackgroundImage != null) previewBackgroundImage.color = isLocked ? lockedBackgroundColor : unlockedBackgroundColor;
            if (bossSilhouetteImage != null)
            {
                bossSilhouetteImage.gameObject.SetActive(true);
                bossSilhouetteImage.color = isCleared ? unlockedBossColor : lockedBossColor;
            }
            if (waveBadgeText != null) waveBadgeText.text = "WAVE 01/05";
            if (flavorText != null) flavorText.text = "Mutant spores have been detected on the outskirts.";
            if (energyCostText != null) energyCostText.text = "X 5";
            if (startButtonLabel != null) startButtonLabel.text = isLocked ? "Locked" : "Start";
            if (lockOverlay != null) lockOverlay.SetActive(isLocked);
            if (costBox != null) costBox.SetActive(!isLocked);
        }

        // Nếu Chapter chưa mở khóa -> Ẩn hoàn toàn nút Start
        if (startButton != null)
        {
            startButton.gameObject.SetActive(!isLocked);
        }

        SetupStartButtonTransition();
        UpdateButtonState();
    }

    public void SetupStartButtonTransition()
    {
        if (startButton == null) return;

        if (normalStartSprite == null || pressedStartSprite == null)
        {
#if UNITY_EDITOR
            Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/nút start.png") as Sprite[];
            if (sprites != null)
            {
                foreach (var s in sprites)
                {
                    if (s == null) continue;
                    if (s.name == "nút start_0" && normalStartSprite == null) normalStartSprite = s;
                    if (s.name == "nút start_1" && pressedStartSprite == null) pressedStartSprite = s;
                }
            }
#endif
        }

        if (normalStartSprite != null && pressedStartSprite != null)
        {
            startButton.transition = Selectable.Transition.SpriteSwap;

            var btnImage = startButton.targetGraphic as Image ?? startButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = normalStartSprite;
                btnImage.color = Color.white;
                btnImage.raycastTarget = true;
            }

            SpriteState state = startButton.spriteState;
            state.highlightedSprite = normalStartSprite;
            state.pressedSprite = pressedStartSprite;
            state.selectedSprite = normalStartSprite;
            state.disabledSprite = normalStartSprite;
            startButton.spriteState = state;
        }
    }

    private void UpdateButtonState()
    {
        bool isLocked = IsCurrentChapterLocked();

        // Đảm bảo nút Start chỉ hiển thị khi Chapter đã mở khóa
        if (startButton != null)
        {
            startButton.gameObject.SetActive(!isLocked);
        }

        if (isLocked || startButton == null) return;

        int cost = currentChapter != null ? currentChapter.energyCost : 10;
        bool hasEnoughEnergy = ChipManager.HasEnoughEnergy(cost);

        var btnImage = startButton.targetGraphic as Image ?? startButton.GetComponent<Image>();
        if (btnImage != null)
        {
            if (normalStartSprite != null)
            {
                // Khi dùng Custom Sprite, giữ màu gốc và làm mờ nếu không đủ năng lượng
                btnImage.color = hasEnoughEnergy ? Color.white : unaffordableButtonColor;
            }
            else
            {
                btnImage.color = hasEnoughEnergy ? affordableButtonColor : unaffordableButtonColor;
            }
        }
    }

    private void HandleEnergyChanged(int amount)
    {
        UpdateButtonState();
    }

    private void HandleTestModeChanged(bool isTest)
    {
        UpdateButtonState();
    }

    public bool TryStartChapter(out string loadedSceneName, bool loadScene = true)
    {
        loadedSceneName = null;

        if (gemMineModal != null && gemMineModal.IsOpen)
        {
            Debug.LogWarning("[ChapterScreenController] Bỏ qua thao tác bắt đầu Chapter vì DailyGemMineModal đang mở.");
            return false;
        }

        if (IsCurrentChapterLocked())
        {
            Debug.LogWarning($"[ChapterScreen] Chapter {currentChapterIndex + 1} ({currentChapter?.chapterTitle}) đang bị khóa!");
            return false;
        }

        int cost = currentChapter != null ? currentChapter.energyCost : 10;

        if (!ChipManager.TrySpendEnergy(cost))
        {
            Debug.LogWarning($"[ChapterScreen] Không đủ năng lượng ({ChipManager.Energy}/{cost}).");
            return false;
        }

        PlayerDataService.SelectedChapterIndex = currentChapterIndex;

        loadedSceneName = currentChapter != null && !string.IsNullOrEmpty(currentChapter.gameplaySceneName)
            ? currentChapter.gameplaySceneName
            : "GamePlay";

        if (loadScene)
        {
            SceneManager.LoadScene(loadedSceneName);
        }
        return true;
    }

    public void OnStartButtonClicked()
    {
        if (gemMineModal != null && gemMineModal.IsOpen)
        {
            Debug.LogWarning("[ChapterScreenController] Bỏ qua StartButton vì DailyGemMineModal đang mở.");
            return;
        }

        TryStartChapter(out _);
    }

    public bool OwnsStartButton(Button candidate)
    {
        return candidate != null && startButton == candidate;
    }

    public void SetDatabaseForTesting(ChapterDatabase db, int chapterIndex = 0)
    {
        chapterDatabase = db;
        currentChapterIndex = chapterIndex;
        RefreshChapterView();
    }

    public void SetLockStateForTesting(GameObject lockObj, TMP_Text label, GameObject costObj)
    {
        lockOverlay = lockObj;
        startButtonLabel = label;
        costBox = costObj;
        RefreshChapterView();
    }

    public void SetStartButtonForTesting(Button btn, Sprite normal = null, Sprite pressed = null)
    {
        startButton = btn;
        normalStartSprite = normal;
        pressedStartSprite = pressed;
        SetupStartButtonTransition();
        RefreshChapterView();
    }

    public void SetPreviewImagesForTesting(Image bg, Image boss)
    {
        previewBackgroundImage = bg;
        bossSilhouetteImage = boss;
        RefreshChapterView();
    }

    public void OnTowerDefClicked()
    {
        EnsureTowerDefModal();
        if (towerDefModalRoot == null) return;
        RefreshTowerDefLevels();
        towerDefModalRoot.transform.SetAsLastSibling();
        towerDefModalRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        if (towerDefScrollRect != null) towerDefScrollRect.verticalNormalizedPosition = 1f;
    }

    public void CloseTowerDefModal()
    {
        if (towerDefModalRoot != null) towerDefModalRoot.SetActive(false);
    }

    private void EnsureTowerDefModal()
    {
        if (towerDefModalRoot != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || towerDefPanelSprite == null || towerDefBoardSprite == null ||
            towerDefBackSprite == null || towerDefStartSprite == null) return;

        UnityEngine.UI.Image backdrop = CreateTowerDefImage("TowerDefModal", canvas.transform, null);
        towerDefModalRoot = backdrop.gameObject;
        RectTransform rootRect = backdrop.rectTransform;
        StretchTowerDefRect(rootRect);
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);
        backdrop.raycastTarget = true;

        UnityEngine.UI.Image panel = CreateTowerDefImage("Panel", rootRect, towerDefPanelSprite);
        StretchTowerDefRect(panel.rectTransform);
        panel.rectTransform.offsetMin = new Vector2(10f, 10f);
        panel.rectTransform.offsetMax = new Vector2(-10f, -10f);
        panel.raycastTarget = true;

        UnityEngine.UI.Image backImage = CreateTowerDefImage("BackButton", panel.transform, towerDefBackSprite);
        RectTransform backRect = backImage.rectTransform;
        backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(42f, -40f);
        backRect.sizeDelta = new Vector2(112f, 112f);
        backImage.raycastTarget = true;
        UnityEngine.UI.Button backButton = backImage.gameObject.AddComponent<UnityEngine.UI.Button>();
        backButton.targetGraphic = backImage;
        backButton.onClick.AddListener(CloseTowerDefModal);

        GameObject scrollObject = new GameObject("LevelScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = (RectTransform)scrollObject.transform;
        scrollRect.anchorMin = new Vector2(0.07f, 0.16f);
        scrollRect.anchorMax = new Vector2(0.93f, 0.75f);
        scrollRect.offsetMin = scrollRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image viewportImage = CreateTowerDefImage("Viewport", scrollRect, null);
        RectTransform viewport = viewportImage.rectTransform;
        StretchTowerDefRect(viewport);
        viewportImage.raycastTarget = true;
        UnityEngine.UI.Mask mask = viewportImage.gameObject.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
        contentObject.transform.SetParent(viewport, false);
        RectTransform content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        UnityEngine.UI.VerticalLayoutGroup layout = contentObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 60f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        UnityEngine.UI.ContentSizeFitter fitter = contentObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        towerDefScrollRect = scrollObject.GetComponent<ScrollRect>();
        towerDefScrollRect.viewport = viewport;
        towerDefScrollRect.content = content;
        towerDefScrollRect.horizontal = false;
        towerDefScrollRect.vertical = true;
        towerDefScrollRect.movementType = ScrollRect.MovementType.Clamped;
        towerDefScrollRect.scrollSensitivity = 40f;

        for (int level = 1; level <= TowerDefProgress.LevelCount; level++)
            CreateTowerDefBoard(content, level);
        towerDefModalRoot.SetActive(false);
    }

    private void CreateTowerDefBoard(Transform parent, int level)
    {
        UnityEngine.UI.Image board = CreateTowerDefImage($"Level{level:00}", parent, towerDefBoardSprite);
        RectTransform boardRect = board.rectTransform;
        board.raycastTarget = false;
        UnityEngine.UI.LayoutElement boardLayout = board.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        boardLayout.preferredHeight = 530f;

        TextMeshProUGUI label = CreateTowerDefText("LevelLabel", boardRect, $"Tower def LV.{level:00}");
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(36f, -18f);
        labelRect.sizeDelta = new Vector2(600f, 62f);
        label.fontSize = 43f;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        UnityEngine.UI.Image startImage = CreateTowerDefImage("StartButton", boardRect, towerDefStartSprite);
        RectTransform startRect = startImage.rectTransform;
        startRect.anchorMin = new Vector2(0.73f, 0.09f);
        startRect.anchorMax = new Vector2(0.97f, 0.28f);
        startRect.offsetMin = startRect.offsetMax = Vector2.zero;
        startImage.raycastTarget = true;
        UnityEngine.UI.Button startButton = startImage.gameObject.AddComponent<UnityEngine.UI.Button>();
        startButton.targetGraphic = startImage;
        startButton.onClick.AddListener(() => SelectTowerDefLevel(level));
        towerDefLevelStartButtons[level - 1] = startButton;

        TextMeshProUGUI lockedLabel = CreateTowerDefText("LockedLabel", boardRect, "LOCKED");
        RectTransform lockedRect = lockedLabel.rectTransform;
        lockedRect.anchorMin = new Vector2(0.73f, 0.09f);
        lockedRect.anchorMax = new Vector2(0.97f, 0.28f);
        lockedRect.offsetMin = lockedRect.offsetMax = Vector2.zero;
        lockedLabel.fontSize = 46f;
        lockedLabel.fontWeight = FontWeight.Black;
        lockedLabel.outlineColor = Color.black;
        lockedLabel.outlineWidth = 0.25f;
        lockedLabel.alignment = TextAlignmentOptions.Center;
        towerDefLevelLockedLabels[level - 1] = lockedLabel.gameObject;
    }

    private TextMeshProUGUI CreateTowerDefText(string name, Transform parent, string value)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = chapterTitleText != null ? chapterTitleText.font : TMP_Settings.defaultFontAsset;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private void RefreshTowerDefLevels()
    {
        for (int level = 1; level <= TowerDefProgress.LevelCount; level++)
        {
            bool unlocked = TowerDefProgress.IsLevelUnlocked(level);
            Button button = towerDefLevelStartButtons[level - 1];
            GameObject lockedLabel = towerDefLevelLockedLabels[level - 1];
            if (button != null) button.gameObject.SetActive(unlocked);
            if (lockedLabel != null) lockedLabel.SetActive(!unlocked);
        }
    }

    private void SelectTowerDefLevel(int level)
    {
        if (!TowerDefProgress.IsLevelUnlocked(level)) return;
        TowerDefProgress.SelectedLevel = level;
        OnTowerDefLevelSelected?.Invoke(level);

        if (Application.isPlaying)
        {
            if (Application.CanStreamedLevelBeLoaded("TowerDef"))
            {
                SceneManager.LoadScene("TowerDef");
            }
            else
            {
                Debug.LogWarning("[ChapterScreenController] Scene 'TowerDef' is not in Build Settings.");
            }
        }
    }

    private static UnityEngine.UI.Image CreateTowerDefImage(string name, Transform parent, Sprite sprite)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        gameObject.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = gameObject.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return image;
    }

    private static void StretchTowerDefRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void OnGemMineClicked()
    {
        Debug.Log("[ChapterScreenController] Nút Gem Mine được nhấn.");
        if (gemMineModal == null)
        {
            gemMineModal = FindObjectOfType<DailyGemMineModalController>(true);
        }

        if (gemMineModal != null)
        {
            gemMineModal.OpenModal();
        }
        else
        {
            Debug.LogWarning("[ChapterScreenController] Chưa gán gemMineModal trong ChapterScreenController.");
        }
    }

    public void SetSideModeButtonsForTesting(Button towerDef, Button gemMine)
    {
        towerDefButton = towerDef;
        gemMineButton = gemMine;
    }

    public void SetGemMineModalForTesting(DailyGemMineModalController modal)
    {
        gemMineModal = modal;
    }
}
