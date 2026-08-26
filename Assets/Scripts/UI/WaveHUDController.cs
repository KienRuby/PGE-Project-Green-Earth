using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý toàn bộ giao diện Wave HUD trong màn chơi Gameplay:
/// - Góc trên bên trái: Vòng tròn thời gian Wave quay 360 độ ("WAVE", "01/10")
/// - Ở giữa phía trên: Cấp độ người chơi ("Lv01") và thanh nạp Kinh Nghiệm (EXP Bar)
/// - Góc trên bên phải: Nút Tạm dừng (Pause Button ||)
/// - Thông báo Wave, cảnh báo Boss xuất hiện và Màn hình Chiến Thắng (Stage Clear).
/// </summary>
public class WaveHUDController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Tham chiếu tới EnemySpawner (tự động tìm trong scene nếu để trống).")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Tooltip("Tham chiếu tới PlayerLevelController (tự động tìm trong scene nếu để trống).")]
    [SerializeField] private PlayerLevelController playerLevelController;

    [Header("1. Circular Wave Progress (Top-Left)")]
    [Tooltip("Image vòng tròn tiến trình thời gian của Wave (Image Type = Filled, Fill Method = Radial 360).")]
    [SerializeField] private Image waveRadialFillImage;

    [Tooltip("Text nhãn Wave (ví dụ: 'WAVE').")]
    [SerializeField] private TMP_Text waveLabelText;

    [Tooltip("Text hiển thị số thứ tự Wave (ví dụ: '01/10').")]
    [SerializeField] private TMP_Text waveNumberText;

    [Header("2. Player Level & EXP Bar (Top-Center)")]
    [Tooltip("Text hiển thị cấp độ người chơi (ví dụ: 'Lv01', 'Lv02').")]
    [SerializeField] private TMP_Text levelText;

    [Tooltip("Image thanh nạp kinh nghiệm (Image Type = Filled, Fill Method = Horizontal).")]
    [SerializeField] private Image expFillImage;

    [Tooltip("Slider kinh nghiệm (tùy chọn thay thế cho expFillImage).")]
    [SerializeField] private Slider expSlider;

    [Header("3. Pause Control (Top-Right)")]
    [Tooltip("Nút tạm dừng game ở góc trên bên phải.")]
    [SerializeField] private Button pauseButton;

    [Tooltip("Controller quản lý Pause Modal chi tiết (Stats, Chipset, Artifact).")]
    [SerializeField] private PauseModalController pauseModalController;

    [Tooltip("Panel Menu tạm dừng xuất hiện khi bấm Pause.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Nút tiếp tục chơi trong menu pause.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Nút thoát ra Menu chính trong menu pause.")]
    [SerializeField] private Button quitToMenuButton;

    [Header("4. Wave Announcement Banner")]
    [Tooltip("GameObject Banner thông báo giữa màn hình khi bắt đầu Wave mới.")]
    [SerializeField] private GameObject announcementBanner;

    [Tooltip("Text hiển thị nội dung thông báo Wave.")]
    [SerializeField] private TMP_Text announcementText;

    [Header("5. Boss Warning Alert")]
    [Tooltip("Khung cảnh báo màu đỏ chớp nháy khi Boss xuất hiện.")]
    [SerializeField] private GameObject bossWarningPanel;

    [Tooltip("Text cảnh báo Boss (ví dụ: 'WARNING: BOSS APPROACHING!').")]
    [SerializeField] private TMP_Text bossWarningText;

    [Header("6. Stage Clear / Victory Panel")]
    [Tooltip("Panel chiến thắng hiển thị sau khi hoàn thành toàn bộ Wave và hạ gục Boss.")]
    [SerializeField] private GameObject stageVictoryPanel;

    [Tooltip("Text tiêu đề chiến thắng.")]
    [SerializeField] private TMP_Text victoryTitleText;

    [Tooltip("Nút quay trở về Main Menu sau khi chiến thắng.")]
    [SerializeField] private Button returnToMenuButton;

    [Tooltip("Tên Scene Main Menu cần nạp.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("7. Chỉ báo Boss ngoài màn hình")]
    [Tooltip("Sprite hình tròn có chữ BOSS dùng để chỉ vị trí Boss ngoài màn hình.")]
    [SerializeField] private Sprite bossOffscreenSprite;

    [Tooltip("Kích thước hình tròn chỉ báo Boss trên Canvas.")]
    [SerializeField] private Vector2 bossIndicatorSize = new Vector2(150f, 150f);

    [Tooltip("Tự động tính kích thước chỉ báo theo tỉ lệ cạnh ngắn của màn hình để hiển thị đồng đều trên mọi độ phân giải.")]
    [SerializeField] private bool useResponsiveBossIndicatorSize = true;

    [Tooltip("Đường kính chỉ báo so với cạnh ngắn màn hình. Ví dụ 0.12 tương đương 12% chiều rộng ở màn hình dọc.")]
    [Range(0.06f, 0.25f)] [SerializeField] private float bossIndicatorScreenRatio = 0.12f;

    [Tooltip("Khoảng cách giữa chỉ báo Boss và mép màn hình, tính theo tỉ lệ cạnh ngắn. Giá trị 0.008 giữ vòng tròn gần sát mép.")]
    [Range(0f, 0.1f)] [SerializeField] private float bossIndicatorEdgePaddingRatio = 0.008f;

    [Tooltip("Vùng phía trên dành cho Wave, thanh Boss, EXP và Pause; chỉ báo Boss không đi vào vùng này.")]
    [Min(0f)] [SerializeField] private float bossIndicatorTopSafePadding = 0f;

    [Tooltip("Vùng phía dưới dành cho joystick; chỉ báo Boss không đi vào vùng này.")]
    [Min(0f)] [SerializeField] private float bossIndicatorBottomSafePadding = 0f;

    [Tooltip("Vùng đệm trong viewport để chỉ báo không nhấp nháy khi Boss đứng sát mép camera.")]
    [Range(0f, 0.15f)] [SerializeField] private float bossViewportMargin = 0.02f;

    [Tooltip("Tốc độ nhịp phóng to nhẹ của chỉ báo Boss. Đặt 0 để tắt nhịp.")]
    [Min(0f)] [SerializeField] private float bossIndicatorPulseSpeed = 4f;

    private Coroutine bannerCoroutine;
    private Coroutine bossWarningCoroutine;
    private bool isPaused;
    private RectTransform bossIndicatorRect;
    private Image bossIndicatorImage;
    private Camera worldCamera;

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }

        if (playerLevelController == null)
        {
            playerLevelController = FindObjectOfType<PlayerLevelController>();
        }

        if (pauseModalController == null)
        {
            pauseModalController = FindObjectOfType<PauseModalController>(true);
        }

        CreateBossOffscreenIndicator();

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (quitToMenuButton != null)
        {
            quitToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
            quitToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }

        if (announcementBanner != null) announcementBanner.SetActive(false);
        if (bossWarningPanel != null) bossWarningPanel.SetActive(false);
        if (stageVictoryPanel != null) stageVictoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Start()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted -= HandleWaveStarted;
            enemySpawner.OnWaveStarted += HandleWaveStarted;

            enemySpawner.OnWaveTimeProgressUpdated -= HandleWaveTimeProgressUpdated;
            enemySpawner.OnWaveTimeProgressUpdated += HandleWaveTimeProgressUpdated;

            enemySpawner.OnBossSpawned -= HandleBossSpawned;
            enemySpawner.OnBossSpawned += HandleBossSpawned;

            enemySpawner.OnStageVictory -= HandleStageVictory;
            enemySpawner.OnStageVictory += HandleStageVictory;

            UpdateWaveDisplay(enemySpawner.CurrentWaveNumber, enemySpawner.TotalWavesCount);
        }

        if (playerLevelController == null)
        {
            playerLevelController = PlayerLevelController.Instance;
        }

        if (playerLevelController != null)
        {
            playerLevelController.OnEXPChanged -= HandleExpChanged;
            playerLevelController.OnEXPChanged += HandleExpChanged;

            playerLevelController.OnLevelUp -= HandleLevelUp;
            playerLevelController.OnLevelUp += HandleLevelUp;

            UpdateLevelDisplay(playerLevelController.CurrentLevel);
            UpdateExpBar(playerLevelController.EXPProgress);
        }
        else
        {
            UpdateLevelDisplay(1);
            UpdateExpBar(0f);
        }
    }

    private void OnDestroy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted -= HandleWaveStarted;
            enemySpawner.OnWaveTimeProgressUpdated -= HandleWaveTimeProgressUpdated;
            enemySpawner.OnBossSpawned -= HandleBossSpawned;
            enemySpawner.OnStageVictory -= HandleStageVictory;
        }

        if (playerLevelController != null)
        {
            playerLevelController.OnEXPChanged -= HandleExpChanged;
            playerLevelController.OnLevelUp -= HandleLevelUp;
        }

        if (pauseButton != null) pauseButton.onClick.RemoveListener(TogglePause);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
        if (quitToMenuButton != null) quitToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
        if (returnToMenuButton != null) returnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);

        Time.timeScale = 1f;
    }

    private void HandleWaveStarted(int currentWave, int totalWaves)
    {
        UpdateWaveDisplay(currentWave, totalWaves);

        if (waveRadialFillImage != null)
        {
            waveRadialFillImage.fillAmount = 0f;
        }

        var config = enemySpawner != null ? enemySpawner.GetCurrentWaveConfig() : null;
        bool isBoss = config != null && config.isBossWave;

        if (isBoss)
        {
            ShowAnnouncement($"FINAL WAVE\n{config?.waveName ?? "BOSS FIGHT"}", 3.0f, new Color(1f, 0.3f, 0.3f, 1f));
        }
        else
        {
            ShowAnnouncement($"WAVE {currentWave}\n{config?.waveName ?? "START!"}", 2.0f, Color.white);
        }
    }

    private void HandleWaveTimeProgressUpdated(float progress, float timeRemaining)
    {
        if (waveRadialFillImage != null)
        {
            waveRadialFillImage.fillAmount = progress;
        }
    }

    private void HandleExpChanged(int currentExp, int maxExp, float progress)
    {
        UpdateExpBar(progress);
    }

    private void HandleLevelUp(int newLevel)
    {
        UpdateLevelDisplay(newLevel);
    }

    private void HandleBossSpawned(GameObject bossObj)
    {
        if (bossWarningCoroutine != null)
        {
            StopCoroutine(bossWarningCoroutine);
        }
        bossWarningCoroutine = StartCoroutine(PlayBossWarningRoutine(bossObj != null ? bossObj.name : "BOSS"));
    }

    private void LateUpdate()
    {
        UpdateBossOffscreenIndicator();
    }

    private void CreateBossOffscreenIndicator()
    {
        if (bossIndicatorRect != null || bossOffscreenSprite == null)
        {
            return;
        }

        GameObject indicator = new GameObject(
            "BossOffscreenIndicator",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        indicator.transform.SetParent(transform, false);

        bossIndicatorRect = indicator.GetComponent<RectTransform>();
        bossIndicatorRect.anchorMin = bossIndicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        bossIndicatorRect.pivot = new Vector2(0.5f, 0.5f);
        bossIndicatorRect.sizeDelta = bossIndicatorSize;

        bossIndicatorImage = indicator.GetComponent<Image>();
        bossIndicatorImage.sprite = bossOffscreenSprite;
        bossIndicatorImage.preserveAspect = true;
        bossIndicatorImage.raycastTarget = false;
        indicator.SetActive(false);
    }

    private void UpdateBossOffscreenIndicator()
    {
        if (bossIndicatorRect == null || enemySpawner == null || enemySpawner.IsStageCompleted)
        {
            SetBossIndicatorVisible(false);
            return;
        }

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null)
        {
            SetBossIndicatorVisible(false);
            return;
        }

        EnemyHealth offscreenBoss = null;
        Vector3 selectedViewportPosition = Vector3.zero;
        float nearestViewportDistance = float.MaxValue;

        for (int i = 0; i < enemySpawner.ActiveBosses.Count; i++)
        {
            EnemyHealth boss = enemySpawner.ActiveBosses[i];
            if (boss == null || boss.IsDead || !boss.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 viewportPosition = worldCamera.WorldToViewportPoint(boss.transform.position);
            if (IsViewportPositionVisible(viewportPosition, bossViewportMargin))
            {
                continue;
            }

            float viewportDistance = ((Vector2)viewportPosition - new Vector2(0.5f, 0.5f)).sqrMagnitude;
            if (viewportDistance < nearestViewportDistance)
            {
                nearestViewportDistance = viewportDistance;
                offscreenBoss = boss;
                selectedViewportPosition = viewportPosition;
            }
        }

        if (offscreenBoss == null)
        {
            SetBossIndicatorVisible(false);
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            SetBossIndicatorVisible(false);
            return;
        }

        float shortestCanvasSide = Mathf.Min(canvasRect.rect.width, canvasRect.rect.height);
        float responsiveDiameter = shortestCanvasSide * bossIndicatorScreenRatio;
        Vector2 currentIndicatorSize = useResponsiveBossIndicatorSize
            ? Vector2.one * responsiveDiameter
            : new Vector2(Mathf.Abs(bossIndicatorSize.x), Mathf.Abs(bossIndicatorSize.y));
        float currentEdgePadding = shortestCanvasSide * bossIndicatorEdgePaddingRatio;
        bossIndicatorRect.sizeDelta = currentIndicatorSize;

        Vector2 indicatorPosition = CalculateBossIndicatorPosition(
            selectedViewportPosition,
            canvasRect.rect.size,
            currentIndicatorSize,
            currentEdgePadding);

        float minimumY = -canvasRect.rect.height * 0.5f
            + currentIndicatorSize.y * 0.5f
            + bossIndicatorBottomSafePadding;
        float maximumY = canvasRect.rect.height * 0.5f
            - currentIndicatorSize.y * 0.5f
            - bossIndicatorTopSafePadding;
        indicatorPosition.y = minimumY <= maximumY
            ? Mathf.Clamp(indicatorPosition.y, minimumY, maximumY)
            : 0f;
        bossIndicatorRect.anchoredPosition = indicatorPosition;

        float pulse = bossIndicatorPulseSpeed > 0f
            ? 1f + Mathf.Sin(Time.unscaledTime * bossIndicatorPulseSpeed) * 0.04f
            : 1f;
        bossIndicatorRect.localScale = Vector3.one * pulse;
        SetBossIndicatorVisible(true);
    }

    private void SetBossIndicatorVisible(bool visible)
    {
        if (bossIndicatorRect != null && bossIndicatorRect.gameObject.activeSelf != visible)
        {
            bossIndicatorRect.gameObject.SetActive(visible);
        }
    }

    public static bool IsViewportPositionVisible(Vector3 viewportPosition, float margin)
    {
        float safeMargin = Mathf.Clamp(margin, 0f, 0.49f);
        return viewportPosition.z > 0f
            && viewportPosition.x >= safeMargin
            && viewportPosition.x <= 1f - safeMargin
            && viewportPosition.y >= safeMargin
            && viewportPosition.y <= 1f - safeMargin;
    }

    public static Vector2 CalculateBossIndicatorPosition(
        Vector3 viewportPosition,
        Vector2 canvasSize,
        Vector2 indicatorSize,
        float edgePadding)
    {
        Vector2 direction = new Vector2(
            (viewportPosition.x - 0.5f) * canvasSize.x,
            (viewportPosition.y - 0.5f) * canvasSize.y);

        if (viewportPosition.z <= 0f)
        {
            direction = -direction;
        }

        if (direction.sqrMagnitude <= 0.000001f)
        {
            direction = Vector2.up;
        }

        float halfWidth = Mathf.Max(0f, canvasSize.x * 0.5f - indicatorSize.x * 0.5f - edgePadding);
        float halfHeight = Mathf.Max(0f, canvasSize.y * 0.5f - indicatorSize.y * 0.5f - edgePadding);
        float horizontalScale = Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
        float verticalScale = Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
        return direction * Mathf.Min(horizontalScale, verticalScale);
    }

    private void HandleStageVictory()
    {
        if (stageVictoryPanel != null)
        {
            stageVictoryPanel.SetActive(true);
        }

        if (victoryTitleText != null)
        {
            victoryTitleText.text = "STAGE CLEAR!";
        }

        Debug.Log("[WaveHUD] 🎉 Hiển thị bảng Chiến Thắng màn chơi!");
    }

    private void UpdateWaveDisplay(int currentWave, int totalWaves)
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"{currentWave:00}/{totalWaves:00}";
        }

        if (waveLabelText != null)
        {
            waveLabelText.text = "WAVE";
        }
    }

    private void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv{level:00}";
        }
    }

    private void UpdateExpBar(float progress)
    {
        if (expFillImage != null)
        {
            expFillImage.fillAmount = progress;
        }

        if (expSlider != null)
        {
            expSlider.value = progress;
        }
    }

    public void TogglePause()
    {
        if (pauseModalController != null)
        {
            pauseModalController.TogglePause();
            isPaused = pauseModalController.IsPaused;
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pauseModalController != null)
        {
            pauseModalController.OpenPauseModal();
            isPaused = true;
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        if (pauseModalController != null)
        {
            pauseModalController.ResumeGame();
            isPaused = false;
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void ShowAnnouncement(string message, float duration = 2.0f, Color? textColor = null)
    {
        if (announcementBanner == null || announcementText == null) return;

        if (bannerCoroutine != null)
        {
            StopCoroutine(bannerCoroutine);
        }
        bannerCoroutine = StartCoroutine(PlayAnnouncementRoutine(message, duration, textColor ?? Color.white));
    }

    private IEnumerator PlayAnnouncementRoutine(string message, float duration, Color textColor)
    {
        announcementBanner.SetActive(true);
        announcementText.text = message;
        announcementText.color = textColor;

        yield return new WaitForSeconds(duration);

        announcementBanner.SetActive(false);
        bannerCoroutine = null;
    }

    private IEnumerator PlayBossWarningRoutine(string bossName)
    {
        if (bossWarningPanel != null)
        {
            bossWarningPanel.SetActive(true);
        }

        if (bossWarningText != null)
        {
            bossWarningText.text = "WARNING: BOSS APPROACHING!";
        }

        for (int i = 0; i < 3; i++)
        {
            if (bossWarningPanel != null) bossWarningPanel.SetActive(true);
            yield return new WaitForSeconds(0.35f);
            if (bossWarningPanel != null) bossWarningPanel.SetActive(false);
            yield return new WaitForSeconds(0.2f);
        }

        if (bossWarningPanel != null)
        {
            bossWarningPanel.SetActive(false);
        }
        bossWarningCoroutine = null;
    }

    public void OnReturnToMenuClicked()
    {
        Time.timeScale = 1f;
        Debug.Log($"[WaveHUD] Nạp Scene: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetReferencesForTesting(
        EnemySpawner spawner,
        PlayerLevelController levelCtrl,
        Image radialFill,
        TMP_Text waveText,
        TMP_Text lvlText,
        Image expFill)
    {
        enemySpawner = spawner;
        playerLevelController = levelCtrl;
        waveRadialFillImage = radialFill;
        waveNumberText = waveText;
        levelText = lvlText;
        expFillImage = expFill;

        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted += HandleWaveStarted;
            enemySpawner.OnWaveTimeProgressUpdated += HandleWaveTimeProgressUpdated;
            enemySpawner.OnBossSpawned += HandleBossSpawned;
            enemySpawner.OnStageVictory += HandleStageVictory;
        }

        if (playerLevelController != null)
        {
            playerLevelController.OnEXPChanged += HandleExpChanged;
            playerLevelController.OnLevelUp += HandleLevelUp;
        }
    }
}
