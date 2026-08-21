using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public sealed class VictoryPanelController : MonoBehaviour
{
    private static readonly Color RuntimeDim = new Color32(8, 14, 12, 222);
    private static readonly Color RuntimeBorder = new Color32(6, 10, 12, 255);
    private static readonly Color RuntimeCard = new Color32(156, 87, 52, 255);
    private static readonly Color RuntimeRibbon = new Color32(163, 151, 113, 255);
    private static readonly Color RuntimeNavy = new Color32(18, 30, 52, 255);
    private static readonly Color RuntimeGold = new Color32(255, 190, 61, 255);
    private static readonly Color RuntimeCream = new Color32(250, 250, 242, 255);

    [Header("Tham chiếu gameplay")]
    [Tooltip("EnemySpawner phát sự kiện khi người chơi hoàn thành toàn bộ chapter.")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Tooltip("Controller Game Over để khóa kết quả thua khi Victory đã xuất hiện.")]
    [SerializeField] private PlayerRunEndController playerRunEndController;

    [Header("Màn Victory")]
    [Tooltip("Toàn bộ panel Victory phủ lên gameplay.")]
    [SerializeField] private GameObject victoryPanel;

    [Tooltip("CanvasGroup dùng để làm hiệu ứng hiện dần.")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Tooltip("Thẻ kết quả được phóng nhẹ khi xuất hiện.")]
    [SerializeField] private RectTransform resultCard;

    [Tooltip("Vùng chứa các mảnh confetti.")]
    [SerializeField] private RectTransform confettiRoot;

    [Header("Nội dung kết quả")]
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text waveNumberText;
    [SerializeField] private TMP_Text dataChipRewardText;
    [SerializeField] private TMP_Text redGemRewardText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Chi tiết")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private Button detailsButton;
    [SerializeField] private Button closeDetailsButton;

    [Header("Nút thao tác")]
    [SerializeField] private Button vipTripleButton;
    [SerializeField] private TMP_Text vipButtonText;
    [SerializeField] private Button homeButton;

    [Header("Hiệu ứng")]
    [Tooltip("Thời gian panel hiện dần, dùng thời gian thực nên vẫn chạy khi game tạm dừng.")]
    [Min(0f)] [SerializeField] private float revealDuration = 0.35f;

    [Tooltip("Số mảnh giấy chúc mừng xuất hiện trên màn hình.")]
    [Range(0, 80)] [SerializeField] private int confettiCount = 36;

    [Header("Điều hướng")]
    [Tooltip("Scene được mở khi nhấn nút Home.")]
    [SerializeField] private string homeSceneName = "MainMenu";

    private readonly List<ConfettiPiece> confettiPieces = new List<ConfettiPiece>();
    private Coroutine revealRoutine;
    private Coroutine confettiRoutine;
    private bool victoryVisible;
    private bool vipBonusClaimed;
    private bool ownsGameplayPause;
    private float timeScaleBeforeVictory = 1f;
    private TMP_FontAsset runtimeFont;
    private Material runtimeFontMaterial;

    public bool IsVisible => victoryPanel != null && victoryPanel.activeSelf;
    public bool VipBonusClaimed => vipBonusClaimed;

    private sealed class ConfettiPiece
    {
        public RectTransform Rect;
        public float FallSpeed;
        public float RotationSpeed;
        public float DriftSpeed;
        public float Phase;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInGameplayScene()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "GamePlay", System.StringComparison.OrdinalIgnoreCase)
            || FindObjectOfType<VictoryPanelController>() != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.AddComponent<VictoryPanelController>();
        }
    }

    private void Awake()
    {
        if (victoryPanel == null)
        {
            BuildRuntimeFallbackUi();
        }

        ResolveReferences();
        SetPanelActive(victoryPanel, false);
        SetPanelActive(detailsPanel, false);
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (enemySpawner != null)
        {
            enemySpawner.OnStageVictory -= HandleStageVictory;
            enemySpawner.OnStageVictory += HandleStageVictory;
        }
    }

    private void Start()
    {
        if (enemySpawner != null && enemySpawner.IsStageCompleted)
        {
            HandleStageVictory();
        }
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnStageVictory -= HandleStageVictory;
        }
    }

    private void OnDestroy()
    {
        UnbindButtons();
        if (ownsGameplayPause)
        {
            Time.timeScale = 1f;
        }
    }

    private void ResolveReferences()
    {
        if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();
        if (playerRunEndController == null) playerRunEndController = FindObjectOfType<PlayerRunEndController>();
    }

    private void BindButtons()
    {
        if (detailsButton != null)
        {
            detailsButton.onClick.RemoveListener(ToggleDetails);
            detailsButton.onClick.AddListener(ToggleDetails);
        }
        if (closeDetailsButton != null)
        {
            closeDetailsButton.onClick.RemoveListener(CloseDetails);
            closeDetailsButton.onClick.AddListener(CloseDetails);
        }
        if (vipTripleButton != null)
        {
            vipTripleButton.onClick.RemoveListener(OnVipTripleClicked);
            vipTripleButton.onClick.AddListener(OnVipTripleClicked);
        }
        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(ReturnHome);
            homeButton.onClick.AddListener(ReturnHome);
        }
    }

    private void UnbindButtons()
    {
        if (detailsButton != null) detailsButton.onClick.RemoveListener(ToggleDetails);
        if (closeDetailsButton != null) closeDetailsButton.onClick.RemoveListener(CloseDetails);
        if (vipTripleButton != null) vipTripleButton.onClick.RemoveListener(OnVipTripleClicked);
        if (homeButton != null) homeButton.onClick.RemoveListener(ReturnHome);
    }

    private void HandleStageVictory()
    {
        if (victoryVisible)
        {
            return;
        }

        victoryVisible = true;
        vipBonusClaimed = false;
        timeScaleBeforeVictory = Time.timeScale > 0f ? Time.timeScale : 1f;
        ownsGameplayPause = true;
        Time.timeScale = 0f;

        if (playerRunEndController != null)
        {
            playerRunEndController.ResolveWithVictory();
        }

        PopulateResult();
        SetPanelActive(detailsPanel, false);
        SetPanelActive(victoryPanel, true);

        if (vipTripleButton != null) vipTripleButton.interactable = true;
        if (vipButtonText != null) vipButtonText.text = "VIP  GET 3X REWARD";
        if (feedbackText != null) feedbackText.text = "CHAPTER UNLOCKED!";

        StopVisualCoroutines();
        revealRoutine = StartCoroutine(PlayReveal());
        confettiRoutine = StartCoroutine(PlayConfetti());
    }

    private void PopulateResult()
    {
        int chapterNumber = PlayerDataService.SelectedChapterIndex + 1;
        int waves = enemySpawner != null ? enemySpawner.TotalWavesCount : 1;
        int dataReward = GetBaseDataChipReward();
        int gemReward = GetBaseRedGemReward();

        if (chapterText != null) chapterText.text = $"CHAPTER. {chapterNumber:00}";
        if (waveNumberText != null) waveNumberText.text = waves.ToString("00");
        UpdateRewardTexts(dataReward, gemReward);

        if (detailsText != null)
        {
            detailsText.text =
                $"CHAPTER {chapterNumber:00} COMPLETE\n" +
                $"WAVES CLEARED   {waves:00}/{waves:00}\n" +
                $"DATA CHIPS      +{dataReward:N0}\n" +
                $"RED GEMS        +{gemReward:N0}\n\n" +
                "NEXT CHAPTER UNLOCKED";
        }
    }

    public bool TryClaimVipTripleReward()
    {
        if (!victoryVisible || vipBonusClaimed)
        {
            return false;
        }

        if (!PlayerDataService.IsVipOwned)
        {
            if (feedbackText != null) feedbackText.text = "VIP REQUIRED - BUY VIP IN SHOP";
            return false;
        }

        int dataReward = GetBaseDataChipReward();
        int gemReward = GetBaseRedGemReward();
        ChipManager.AddDataChips(dataReward * 2);
        ChipManager.AddRedGems(gemReward * 2);
        vipBonusClaimed = true;

        UpdateRewardTexts(dataReward * 3, gemReward * 3);
        if (vipTripleButton != null) vipTripleButton.interactable = false;
        if (vipButtonText != null) vipButtonText.text = "VIP  3X RECEIVED";
        if (feedbackText != null) feedbackText.text = "VIP BONUS RECEIVED!";
        return true;
    }

    private void OnVipTripleClicked()
    {
        TryClaimVipTripleReward();
    }

    public void ToggleDetails()
    {
        if (detailsPanel != null) detailsPanel.SetActive(!detailsPanel.activeSelf);
    }

    public void CloseDetails()
    {
        SetPanelActive(detailsPanel, false);
    }

    public void ReturnHome()
    {
        ownsGameplayPause = false;
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(homeSceneName))
        {
            SceneManager.LoadScene(homeSceneName);
        }
    }

    private IEnumerator PlayReveal()
    {
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        if (resultCard != null) resultCard.localScale = Vector3.one * 0.78f;

        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = revealDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / revealDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = eased;
            if (resultCard != null) resultCard.localScale = Vector3.LerpUnclamped(Vector3.one * 0.78f, Vector3.one, eased);
            yield return null;
        }

        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        if (resultCard != null) resultCard.localScale = Vector3.one;
        revealRoutine = null;
    }

    private IEnumerator PlayConfetti()
    {
        CreateConfettiPieces();
        while (victoryPanel != null && victoryPanel.activeSelf)
        {
            float delta = Time.unscaledDeltaTime;
            for (int i = 0; i < confettiPieces.Count; i++)
            {
                ConfettiPiece piece = confettiPieces[i];
                Vector2 position = piece.Rect.anchoredPosition;
                position.y -= piece.FallSpeed * delta;
                position.x += Mathf.Sin(Time.unscaledTime * piece.DriftSpeed + piece.Phase) * 45f * delta;
                if (position.y < -1050f)
                {
                    position.y = Random.Range(980f, 1250f);
                    position.x = Random.Range(-560f, 560f);
                }
                piece.Rect.anchoredPosition = position;
                piece.Rect.Rotate(0f, 0f, piece.RotationSpeed * delta);
            }
            yield return null;
        }
        confettiRoutine = null;
    }

    private void CreateConfettiPieces()
    {
        if (confettiRoot == null || confettiPieces.Count > 0)
        {
            return;
        }

        Color[] colors =
        {
            new Color32(56, 221, 210, 255),
            new Color32(77, 230, 91, 255),
            new Color32(255, 193, 61, 255),
            new Color32(126, 247, 222, 255)
        };

        for (int i = 0; i < confettiCount; i++)
        {
            GameObject go = new GameObject($"Confetti_{i:00}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(confettiRoot, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Random.Range(18f, 54f), Random.Range(10f, 30f));
            rect.anchoredPosition = new Vector2(Random.Range(-560f, 560f), Random.Range(-900f, 1250f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Image image = go.GetComponent<Image>();
            image.color = colors[i % colors.Length];
            image.raycastTarget = false;

            confettiPieces.Add(new ConfettiPiece
            {
                Rect = rect,
                FallSpeed = Random.Range(170f, 430f),
                RotationSpeed = Random.Range(-220f, 220f),
                DriftSpeed = Random.Range(1.2f, 3.5f),
                Phase = Random.Range(0f, 6.28f)
            });
        }
    }

    private void StopVisualCoroutines()
    {
        if (revealRoutine != null) StopCoroutine(revealRoutine);
        if (confettiRoutine != null) StopCoroutine(confettiRoutine);
        revealRoutine = null;
        confettiRoutine = null;
    }

    private int GetBaseDataChipReward()
    {
        return enemySpawner != null ? enemySpawner.StageVictoryDataChipReward : 50;
    }

    private int GetBaseRedGemReward()
    {
        return enemySpawner != null ? enemySpawner.StageVictoryRedGemReward : 10;
    }

    private void UpdateRewardTexts(int dataReward, int gemReward)
    {
        if (dataChipRewardText != null) dataChipRewardText.text = $"GET {dataReward:N0}";
        if (redGemRewardText != null) redGemRewardText.text = $"GET {gemReward:N0}";
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void BuildRuntimeFallbackUi()
    {
        TMP_Text sampleText = GetComponentInChildren<TMP_Text>(true);
        if (sampleText != null)
        {
            runtimeFont = sampleText.font;
            runtimeFontMaterial = sampleText.fontSharedMaterial;
        }

        victoryPanel = CreateRuntimeOverlay("VictoryPanel", transform);
        panelCanvasGroup = victoryPanel.AddComponent<CanvasGroup>();

        GameObject confettiObject = new GameObject("ConfettiRoot", typeof(RectTransform));
        confettiObject.transform.SetParent(victoryPanel.transform, false);
        confettiRoot = confettiObject.GetComponent<RectTransform>();
        StretchRuntimeRect(confettiRoot);

        TMP_Text title = CreateRuntimeText("Title", victoryPanel.transform, "CHAPTER COMPLETE!", 88f, RuntimeGold);
        SetRuntimeRect(title.rectTransform, new Vector2(0f, 650f), new Vector2(1010f, 150f));

        CreateRuntimeFrame("RibbonLeft", victoryPanel.transform, new Vector2(-360f, 130f), new Vector2(250f, 220f), RuntimeRibbon);
        CreateRuntimeFrame("RibbonRight", victoryPanel.transform, new Vector2(360f, 130f), new Vector2(250f, 220f), RuntimeRibbon);
        GameObject card = CreateRuntimeFrame("ResultCard", victoryPanel.transform, new Vector2(0f, 120f), new Vector2(520f, 590f), RuntimeCard);
        resultCard = card.GetComponent<RectTransform>();

        chapterText = CreateRuntimeText("ChapterText", card.transform, "CHAPTER. 01", 47f, RuntimeCream);
        SetRuntimeRect(chapterText.rectTransform, new Vector2(0f, 205f), new Vector2(470f, 80f));
        waveNumberText = CreateRuntimeText("WaveNumberText", card.transform, "09", 150f, RuntimeGold);
        SetRuntimeRect(waveNumberText.rectTransform, new Vector2(0f, 35f), new Vector2(470f, 220f));
        TMP_Text wavesLabel = CreateRuntimeText("WavesLabel", card.transform, "WAVES", 65f, RuntimeCream);
        SetRuntimeRect(wavesLabel.rectTransform, new Vector2(0f, -150f), new Vector2(470f, 100f));

        dataChipRewardText = CreateRuntimeRewardRow("DataChipReward", new Vector2(-60f, -275f), "▣", new Color32(74, 211, 202, 255));
        redGemRewardText = CreateRuntimeRewardRow("RedGemReward", new Vector2(-60f, -405f), "◆", new Color32(224, 71, 77, 255));

        detailsButton = CreateRuntimeButton("DetailsButton", victoryPanel.transform, "DETAILS", new Vector2(330f, -340f), new Vector2(245f, 170f), new Color32(20, 30, 33, 255), 36f);
        feedbackText = CreateRuntimeText("FeedbackText", victoryPanel.transform, "CHAPTER UNLOCKED!", 31f, RuntimeGold);
        SetRuntimeRect(feedbackText.rectTransform, new Vector2(0f, -535f), new Vector2(850f, 65f));

        vipTripleButton = CreateRuntimeButton("VipTripleButton", victoryPanel.transform, "VIP  GET 3X REWARD", new Vector2(0f, -680f), new Vector2(650f, 145f), RuntimeNavy, 44f);
        vipButtonText = vipTripleButton.transform.Find("Label").GetComponent<TMP_Text>();
        homeButton = CreateRuntimeButton("HomeButton", victoryPanel.transform, "HOME", new Vector2(0f, -850f), new Vector2(390f, 105f), new Color32(78, 111, 128, 255), 38f);

        detailsPanel = CreateRuntimeOverlay("DetailsPanel", victoryPanel.transform);
        detailsPanel.GetComponent<Image>().color = new Color32(4, 8, 9, 235);
        GameObject detailsCard = CreateRuntimeFrame("DetailsCard", detailsPanel.transform, Vector2.zero, new Vector2(780f, 720f), RuntimeNavy);
        TMP_Text detailsTitle = CreateRuntimeText("Title", detailsCard.transform, "VICTORY DETAILS", 56f, RuntimeGold);
        SetRuntimeRect(detailsTitle.rectTransform, new Vector2(0f, 255f), new Vector2(700f, 90f));
        detailsText = CreateRuntimeText("DetailsText", detailsCard.transform, "CHAPTER COMPLETE", 40f, RuntimeCream);
        detailsText.alignment = TextAlignmentOptions.Left;
        detailsText.lineSpacing = 18f;
        SetRuntimeRect(detailsText.rectTransform, new Vector2(0f, 25f), new Vector2(620f, 340f));
        closeDetailsButton = CreateRuntimeButton("CloseButton", detailsCard.transform, "CLOSE", new Vector2(0f, -260f), new Vector2(360f, 105f), RuntimeRibbon, 38f);

        detailsPanel.SetActive(false);
        victoryPanel.SetActive(false);
        victoryPanel.transform.SetAsLastSibling();
    }

    private TMP_Text CreateRuntimeRewardRow(string name, Vector2 position, string iconValue, Color iconColor)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(victoryPanel.transform, false);
        SetRuntimeRect(row.GetComponent<RectTransform>(), position, new Vector2(640f, 105f));

        TMP_Text icon = CreateRuntimeText("Icon", row.transform, iconValue, 64f, iconColor);
        SetRuntimeRect(icon.rectTransform, new Vector2(-225f, 0f), new Vector2(90f, 90f));
        TMP_Text value = CreateRuntimeText("Value", row.transform, "GET 0", 52f, RuntimeCream);
        value.alignment = TextAlignmentOptions.Left;
        SetRuntimeRect(value.rectTransform, new Vector2(90f, 0f), new Vector2(470f, 95f));
        return value;
    }

    private GameObject CreateRuntimeOverlay(string objectName, Transform parent)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        StretchRuntimeRect(root.GetComponent<RectTransform>());
        Image image = root.GetComponent<Image>();
        image.color = RuntimeDim;
        image.raycastTarget = true;
        return root;
    }

    private GameObject CreateRuntimeFrame(string objectName, Transform parent, Vector2 position, Vector2 size, Color fill)
    {
        GameObject border = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        border.transform.SetParent(parent, false);
        border.GetComponent<Image>().color = RuntimeBorder;
        SetRuntimeRect(border.GetComponent<RectTransform>(), position, size);

        GameObject inner = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(border.transform, false);
        inner.GetComponent<Image>().color = fill;
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(8f, 8f);
        innerRect.offsetMax = new Vector2(-8f, -8f);
        return border;
    }

    private Button CreateRuntimeButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size, Color fill, float fontSize)
    {
        GameObject frame = CreateRuntimeFrame(objectName, parent, position, size, fill);
        Button button = frame.AddComponent<Button>();
        button.targetGraphic = frame.transform.Find("Fill").GetComponent<Image>();
        TMP_Text text = CreateRuntimeText("Label", frame.transform, label, fontSize, RuntimeCream);
        StretchRuntimeRect(text.rectTransform);
        return button;
    }

    private TMP_Text CreateRuntimeText(string objectName, Transform parent, string value, float size, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (runtimeFont != null) text.font = runtimeFont;
        if (runtimeFontMaterial != null) text.fontSharedMaterial = runtimeFontMaterial;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void SetRuntimeRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void StretchRuntimeRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
