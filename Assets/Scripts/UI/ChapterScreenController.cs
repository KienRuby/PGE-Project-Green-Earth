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

    [Tooltip("Text hiển thị tên Chapter (ví dụ: 'Grassland Outskirts').")]
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

        RefreshChapterView();
    }

    private void OnEnable()
    {
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;
        RefreshChapterView();
    }

    private void OnDisable()
    {
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;
    }

    private void OnDestroy()
    {
        if (prevChapterButton != null) prevChapterButton.onClick.RemoveListener(OnPrevChapterClicked);
        if (nextChapterButton != null) nextChapterButton.onClick.RemoveListener(OnNextChapterClicked);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartButtonClicked);
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

    public void RefreshChapterView()
    {
        if (chapterDatabase != null && chapterDatabase.Count > 0)
        {
            currentChapter = chapterDatabase.GetChapter(currentChapterIndex);
        }

        bool isLocked = IsCurrentChapterLocked();

        if (currentChapter != null)
        {
            if (chapterSubtitleText != null)
            {
                chapterSubtitleText.text = $"Chapter. {currentChapter.chapterNumber:00}";
            }

            if (chapterTitleText != null)
            {
                chapterTitleText.text = currentChapter.chapterTitle;
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
                bossSilhouetteImage.color = isLocked ? lockedBossColor : unlockedBossColor;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(isLocked);
            }

            if (waveBadgeText != null)
            {
                waveBadgeText.text = $"WAVE: 01/{currentChapter.totalWaves:00}";
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
            if (chapterSubtitleText != null) chapterSubtitleText.text = "Chapter. 01";
            if (chapterTitleText != null) chapterTitleText.text = "Grassland Outskirts";
            if (previewBackgroundImage != null) previewBackgroundImage.color = isLocked ? lockedBackgroundColor : unlockedBackgroundColor;
            if (bossSilhouetteImage != null)
            {
                bossSilhouetteImage.gameObject.SetActive(true);
                bossSilhouetteImage.color = isLocked ? lockedBossColor : unlockedBossColor;
            }
            if (waveBadgeText != null) waveBadgeText.text = "WAVE: 01/05";
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

        Debug.Log($"[ChapterScreen] Bắt đầu Chapter: {currentChapterIndex + 1} ({currentChapter?.chapterTitle}), Nạp scene: {loadedSceneName}");
        if (loadScene)
        {
            SceneManager.LoadScene(loadedSceneName);
        }
        return true;
    }

    public void OnStartButtonClicked()
    {
        TryStartChapter(out _);
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
}
