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

    [Header("2b. Equipped Artifact Slots (Under Level Bar)")]
    [Tooltip("Container chứa các icon Artifact đã trang bị trong run (nằm ngay dưới thanh Level).")]
    [SerializeField] private RectTransform equippedArtifactsContainer;
    private readonly System.Collections.Generic.List<GameObject> spawnedArtifactSlots = new System.Collections.Generic.List<GameObject>();

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

    [Header("8. Chỉ báo Hộp Cổ Vật ngoài màn hình")]
    [Tooltip("Sprite hình tròn có dấu ? dùng để chỉ vị trí Hộp Cổ Vật ngoài màn hình. Nếu để trống sẽ tự sinh đồ họa.")]
    [SerializeField] private Sprite artifactBoxOffscreenSprite;

    [Tooltip("Kích thước hình tròn chỉ báo Hộp Cổ Vật trên Canvas.")]
    [SerializeField] private Vector2 artifactBoxIndicatorSize = new Vector2(130f, 130f);

    [Tooltip("Tự động tính kích thước chỉ báo Hộp Cổ Vật theo tỉ lệ cạnh ngắn của màn hình.")]
    [SerializeField] private bool useResponsiveArtifactBoxIndicatorSize = true;

    [Tooltip("Đường kính chỉ báo so với cạnh ngắn màn hình.")]
    [Range(0.06f, 0.25f)] [SerializeField] private float artifactBoxIndicatorScreenRatio = 0.11f;

    [Tooltip("Khoảng cách giữa chỉ báo Hộp Cổ Vật và mép màn hình.")]
    [Range(0f, 0.1f)] [SerializeField] private float artifactBoxIndicatorEdgePaddingRatio = 0.008f;

    [Tooltip("Vùng đệm trong viewport để chỉ báo không nhấp nháy khi hộp đứng sát mép camera.")]
    [Range(0f, 0.15f)] [SerializeField] private float artifactBoxViewportMargin = 0.02f;

    [Tooltip("Tốc độ nhịp phóng to nhẹ của chỉ báo Hộp Cổ Vật.")]
    [Min(0f)] [SerializeField] private float artifactBoxIndicatorPulseSpeed = 4f;

    private Coroutine bannerCoroutine;
    private Coroutine bossWarningCoroutine;
    private bool isPaused;
    private RectTransform bossIndicatorRect;
    private Image bossIndicatorImage;
    private Camera worldCamera;

    private RectTransform artifactBoxIndicatorRect;
    private Image artifactBoxIndicatorImage;
    private TMP_Text artifactBoxQuestionMarkText;

    public RectTransform ArtifactBoxIndicatorRect => artifactBoxIndicatorRect;
    public bool IsArtifactBoxIndicatorVisible => artifactBoxIndicatorRect != null && artifactBoxIndicatorRect.gameObject.activeSelf;

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
        CreateArtifactBoxOffscreenIndicator();
        ArtifactFoundModalController.EnsureModalInScene(GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>());
        GameplayEventModalController.EnsureModalInScene(GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>());
        _ = PlayerArtifactInventory.Instance;

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

        EnsureArtifactSlotContainer();
    }

    private void Start()
    {
        if (PlayerArtifactInventory.Instance != null)
        {
            PlayerArtifactInventory.Instance.OnArtifactEquipped += HandleArtifactEquipped;
            RefreshArtifactSlots();
        }
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

        if (PlayerArtifactInventory.Instance != null)
        {
            PlayerArtifactInventory.Instance.OnArtifactEquipped -= HandleArtifactEquipped;
        }

        Time.timeScale = 1f;
    }

    private void EnsureArtifactSlotContainer()
    {
        if (equippedArtifactsContainer != null) return;

        Transform parentTarget = expFillImage != null ? expFillImage.transform.parent : transform;
        GameObject containerObj = new GameObject("EquippedArtifactsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerObj.transform.SetParent(parentTarget, false);

        equippedArtifactsContainer = containerObj.GetComponent<RectTransform>();
        equippedArtifactsContainer.anchorMin = new Vector2(0.5f, 1f);
        equippedArtifactsContainer.anchorMax = new Vector2(0.5f, 1f);
        equippedArtifactsContainer.pivot = new Vector2(0.5f, 1f);
        equippedArtifactsContainer.anchoredPosition = new Vector2(0f, -95f);
        equippedArtifactsContainer.sizeDelta = new Vector2(400f, 50f);

        HorizontalLayoutGroup hlg = containerObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
    }

    private void HandleArtifactEquipped(ArtifactData artifact)
    {
        RefreshArtifactSlots();
    }

    public void RefreshArtifactSlots()
    {
        EnsureArtifactSlotContainer();
        if (equippedArtifactsContainer == null) return;

        foreach (var slot in spawnedArtifactSlots)
        {
            if (slot != null) Destroy(slot);
        }
        spawnedArtifactSlots.Clear();

        if (PlayerArtifactInventory.Instance == null) return;

        foreach (var art in PlayerArtifactInventory.Instance.EquippedArtifacts)
        {
            if (art == null) continue;

            GameObject slot = new GameObject($"ArtifactSlot_{art.id}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(equippedArtifactsContainer, false);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(46f, 46f);

            Image bgImg = slot.GetComponent<Image>();
            bgImg.color = art.badgeBorderColor;

            GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(slot.transform, false);
            RectTransform innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3f, 3f);
            innerRt.offsetMax = new Vector2(-3f, -3f);

            Image innerImg = inner.GetComponent<Image>();
            innerImg.color = art.badgeBgColor;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(inner.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(30f, 30f);

            Image iconImg = iconObj.GetComponent<Image>();
            if (art.icon != null)
            {
                iconImg.sprite = art.icon;
                iconImg.color = Color.white;
            }
            else
            {
                iconImg.color = art.statType == ArtifactStatType.MaxHealthPercent ? new Color32(235, 60, 60, 255)
                    : (art.statType == ArtifactStatType.RangedDefensePercent ? new Color32(90, 180, 230, 255)
                    : (art.statType == ArtifactStatType.TurretAttackSpeedPercent ? new Color32(240, 180, 30, 255)
                    : new Color32(180, 90, 240, 255)));
            }

            spawnedArtifactSlots.Add(slot);
        }
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
        UpdateArtifactBoxOffscreenIndicator();
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

    private void CreateArtifactBoxOffscreenIndicator()
    {
        if (artifactBoxIndicatorRect != null)
        {
            return;
        }

        GameObject indicator = new GameObject(
            "ArtifactBoxOffscreenIndicator",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        indicator.transform.SetParent(transform, false);
        indicator.transform.SetAsLastSibling();

        artifactBoxIndicatorRect = indicator.GetComponent<RectTransform>();
        artifactBoxIndicatorRect.anchorMin = artifactBoxIndicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        artifactBoxIndicatorRect.pivot = new Vector2(0.5f, 0.5f);
        artifactBoxIndicatorRect.sizeDelta = artifactBoxIndicatorSize;

        artifactBoxIndicatorImage = indicator.GetComponent<Image>();
        artifactBoxIndicatorImage.preserveAspect = true;
        artifactBoxIndicatorImage.raycastTarget = false;

        if (artifactBoxOffscreenSprite != null)
        {
            artifactBoxIndicatorImage.sprite = artifactBoxOffscreenSprite;
        }
        else
        {
            artifactBoxIndicatorImage.sprite = CreateProceduralCircleBadgeSprite();
        }

        // Tạo Text dấu hỏi "?" ở tâm hình tròn
        GameObject textObj = new GameObject("QuestionMarkText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(indicator.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        artifactBoxQuestionMarkText = textObj.GetComponent<TextMeshProUGUI>();
        artifactBoxQuestionMarkText.text = "?";
        artifactBoxQuestionMarkText.alignment = TextAlignmentOptions.Center;
        artifactBoxQuestionMarkText.fontSize = 58;
        artifactBoxQuestionMarkText.fontStyle = FontStyles.Bold;
        artifactBoxQuestionMarkText.color = new Color32(255, 225, 40, 255); // Màu vàng kim nổi bật
        artifactBoxQuestionMarkText.raycastTarget = false;

        if (artifactBoxQuestionMarkText.font == null)
        {
            if (waveNumberText != null && waveNumberText.font != null)
            {
                artifactBoxQuestionMarkText.font = waveNumberText.font;
            }
            else
            {
                artifactBoxQuestionMarkText.font = TMP_Settings.defaultFontAsset;
            }
        }

        if (artifactBoxOffscreenSprite != null)
        {
            artifactBoxQuestionMarkText.gameObject.SetActive(false);
        }

        indicator.SetActive(false);
    }

    private Sprite CreateProceduralCircleBadgeSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = 60f;
        float outerRingInner = 52f;
        float innerRingOuter = 49f;
        float innerRingInner = 43f;

        Color32 cyanGlow = new Color32(46, 229, 240, 255);    // Cyan viền ngoài
        Color32 goldRing = new Color32(255, 204, 0, 255);     // Vàng kim viền trong
        Color32 darkBg = new Color32(14, 26, 42, 235);        // Nền tối xanh than
        Color32 ringGap = new Color32(8, 16, 26, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > outerRadius + 1.5f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist > outerRadius)
                {
                    float alpha = Mathf.Clamp01(outerRadius + 1.5f - dist) / 1.5f;
                    Color c = cyanGlow;
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
                else if (dist >= outerRingInner)
                {
                    tex.SetPixel(x, y, cyanGlow);
                }
                else if (dist > innerRingOuter)
                {
                    tex.SetPixel(x, y, ringGap);
                }
                else if (dist >= innerRingInner)
                {
                    tex.SetPixel(x, y, goldRing);
                }
                else
                {
                    tex.SetPixel(x, y, darkBg);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void UpdateArtifactBoxOffscreenIndicator()
    {
        if (artifactBoxIndicatorRect == null)
        {
            CreateArtifactBoxOffscreenIndicator();
        }

        if (artifactBoxIndicatorRect == null)
        {
            return;
        }

        var activeBoxes = ArtifactBoxPickup.ActiveBoxes;
        var activeEvents = GameplayEventPickup.ActiveEvents;
        int boxCount = activeBoxes != null ? activeBoxes.Count : 0;
        int eventCount = activeEvents != null ? activeEvents.Count : 0;

        if (boxCount == 0 && eventCount == 0)
        {
            SetArtifactBoxIndicatorVisible(false);
            return;
        }

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null)
        {
            SetArtifactBoxIndicatorVisible(false);
            return;
        }

        bool foundOffscreen = false;
        Vector3 selectedViewportPosition = Vector3.zero;
        float nearestViewportDistance = float.MaxValue;

        // 1. Kiểm tra các điểm Sự kiện tương tác trên bản đồ (Gameplay Event)
        if (activeEvents != null)
        {
            for (int i = 0; i < activeEvents.Count; i++)
            {
                var ev = activeEvents[i];
                if (ev == null || !ev.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 viewportPosition = worldCamera.WorldToViewportPoint(ev.transform.position);
                if (IsViewportPositionVisible(viewportPosition, artifactBoxViewportMargin))
                {
                    continue;
                }

                float viewportDistance = ((Vector2)viewportPosition - new Vector2(0.5f, 0.5f)).sqrMagnitude;
                if (viewportDistance < nearestViewportDistance)
                {
                    nearestViewportDistance = viewportDistance;
                    foundOffscreen = true;
                    selectedViewportPosition = viewportPosition;
                }
            }
        }

        // 2. Kiểm tra các Hộp Cổ Vật (Artifact Box)
        if (activeBoxes != null)
        {
            for (int i = 0; i < activeBoxes.Count; i++)
            {
                ArtifactBoxPickup box = activeBoxes[i];
                if (box == null || !box.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 viewportPosition = worldCamera.WorldToViewportPoint(box.transform.position);
                if (IsViewportPositionVisible(viewportPosition, artifactBoxViewportMargin))
                {
                    continue;
                }

                float viewportDistance = ((Vector2)viewportPosition - new Vector2(0.5f, 0.5f)).sqrMagnitude;
                if (viewportDistance < nearestViewportDistance)
                {
                    nearestViewportDistance = viewportDistance;
                    foundOffscreen = true;
                    selectedViewportPosition = viewportPosition;
                }
            }
        }

        if (!foundOffscreen)
        {
            SetArtifactBoxIndicatorVisible(false);
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            SetArtifactBoxIndicatorVisible(false);
            return;
        }

        float shortestCanvasSide = Mathf.Min(canvasRect.rect.width, canvasRect.rect.height);
        float responsiveDiameter = shortestCanvasSide * artifactBoxIndicatorScreenRatio;
        Vector2 currentIndicatorSize = useResponsiveArtifactBoxIndicatorSize
            ? Vector2.one * responsiveDiameter
            : new Vector2(Mathf.Abs(artifactBoxIndicatorSize.x), Mathf.Abs(artifactBoxIndicatorSize.y));
        float currentEdgePadding = shortestCanvasSide * artifactBoxIndicatorEdgePaddingRatio;
        artifactBoxIndicatorRect.sizeDelta = currentIndicatorSize;

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

        // Tránh đè lên chỉ báo Boss nếu cả hai cùng ở cùng một góc/mép màn hình
        if (bossIndicatorRect != null && bossIndicatorRect.gameObject.activeSelf)
        {
            float dist = Vector2.Distance(indicatorPosition, bossIndicatorRect.anchoredPosition);
            float minSpacing = (currentIndicatorSize.x + bossIndicatorRect.sizeDelta.x) * 0.52f;
            if (dist < minSpacing && minSpacing > 0.01f)
            {
                Vector2 tangent = new Vector2(-indicatorPosition.y, indicatorPosition.x);
                if (tangent.sqrMagnitude > 0.0001f)
                {
                    tangent.Normalize();
                    indicatorPosition += tangent * (minSpacing - dist);
                    indicatorPosition.y = minimumY <= maximumY
                        ? Mathf.Clamp(indicatorPosition.y, minimumY, maximumY)
                        : 0f;
                }
            }
        }

        artifactBoxIndicatorRect.anchoredPosition = indicatorPosition;

        float pulse = artifactBoxIndicatorPulseSpeed > 0f
            ? 1f + Mathf.Sin(Time.unscaledTime * artifactBoxIndicatorPulseSpeed) * 0.05f
            : 1f;
        artifactBoxIndicatorRect.localScale = Vector3.one * pulse;
        SetArtifactBoxIndicatorVisible(true);
    }

    private void SetArtifactBoxIndicatorVisible(bool visible)
    {
        if (artifactBoxIndicatorRect != null && artifactBoxIndicatorRect.gameObject.activeSelf != visible)
        {
            artifactBoxIndicatorRect.gameObject.SetActive(visible);
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
            UIDissolveController.ShowInstant(pausePanel);
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
            UIDissolveController.HideWithEffect(pausePanel);
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
