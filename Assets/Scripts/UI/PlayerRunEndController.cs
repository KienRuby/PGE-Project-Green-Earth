using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public sealed class PlayerRunEndController : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerDeathController playerDeathController;
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Revive Panel")]
    [SerializeField] private GameObject revivePanel;
    [SerializeField] private CanvasGroup reviveCanvasGroup;
    [SerializeField] private RectTransform reviveContent;
    [SerializeField] private Button noButton;
    [FormerlySerializedAs("vipReviveButton")]
    [SerializeField] private Button gemReviveButton;
    [SerializeField] private Button adReviveButton;
    [SerializeField] private TMP_Text reviveFeedbackText;
    [SerializeField, Min(0)] private int reviveGemCost = 200;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text wavesText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text dataChipRewardText;
    [SerializeField] private TMP_Text redGemRewardText;
    [SerializeField] private Button detailsButton;
    [SerializeField] private DamageDetailsPopup damageDetailsPopup;
    [FormerlySerializedAs("homeButton")]
    [SerializeField] private Button getRewardButton;
    [SerializeField] private Button vipTripleButton;
    [SerializeField] private TMP_Text gameOverFeedbackText;

    [Header("Reward Settings")]
    [SerializeField, Min(0)] private int baseDataChipReward = 25;
    [SerializeField, Min(0)] private int dataChipsPerCompletedWave = 30;
    [SerializeField, Min(0)] private int dataChipsPerKill = 2;
    [SerializeField, Min(0)] private int redGemsPerCompletedWave = 1;

    [Header("Navigation")]
    [SerializeField] private string homeSceneName = "MainMenu";

    private bool resultResolved;
    private bool rewardsGranted;
    private bool ownsGameplayPause;
    private bool waitingForRewardedAd;
    private float timeScaleBeforePrompt = 1f;
    private Coroutine reviveRevealRoutine;
    private int pendingDataChipReward;
    private int pendingRedGemReward;

    /// <summary>
    /// Adapter quảng cáo đăng ký callback này, hiển thị rewarded ad rồi gọi completion(true)
    /// chỉ khi người chơi đã xem đủ và nhận reward.
    /// </summary>
    public static event Action<Action<bool>> OnRewardedReviveRequested;

    public bool IsRevivePromptVisible => revivePanel != null && revivePanel.activeSelf;
    public bool IsGameOverVisible => gameOverPanel != null && gameOverPanel.activeSelf;

    private void Awake()
    {
        ResolveGameplayReferences();

        EnsureDetailsUiComponents();
        SetPanelActive(revivePanel, false);
        SetPanelActive(gameOverPanel, false);
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveGameplayReferences();
        EnsureDetailsUiComponents();
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
        }

        if (playerDeathController != null)
        {
            playerDeathController.OnDeathCompleted -= HandleDeathSequenceCompleted;
            playerDeathController.OnDeathCompleted += HandleDeathSequenceCompleted;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
        }
        if (playerDeathController != null)
        {
            playerDeathController.OnDeathCompleted -= HandleDeathSequenceCompleted;
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

    private void ResolveGameplayReferences()
    {
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerDeathController == null && playerHealth != null)
            playerDeathController = playerHealth.GetComponent<PlayerDeathController>();
        if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();
    }

    private void BindButtons()
    {
        if (noButton != null)
        {
            noButton.onClick.RemoveListener(ChooseNo);
            noButton.onClick.AddListener(ChooseNo);
        }

        if (gemReviveButton != null)
        {
            gemReviveButton.onClick.RemoveListener(OnGemReviveClicked);
            gemReviveButton.onClick.AddListener(OnGemReviveClicked);
        }

        if (adReviveButton != null)
        {
            adReviveButton.onClick.RemoveListener(OnAdReviveClicked);
            adReviveButton.onClick.AddListener(OnAdReviveClicked);
        }

        if (getRewardButton != null)
        {
            getRewardButton.onClick.RemoveListener(ClaimRewardAndReturnHome);
            getRewardButton.onClick.AddListener(ClaimRewardAndReturnHome);
        }

        if (vipTripleButton != null)
        {
            vipTripleButton.onClick.RemoveListener(ClaimTripleRewardAndReturnHome);
            vipTripleButton.onClick.AddListener(ClaimTripleRewardAndReturnHome);
        }

        if (detailsButton != null)
        {
            detailsButton.onClick.RemoveListener(OnDetailsClicked);
            detailsButton.onClick.AddListener(OnDetailsClicked);
        }
    }

    private void UnbindButtons()
    {
        if (noButton != null) noButton.onClick.RemoveListener(ChooseNo);
        if (gemReviveButton != null) gemReviveButton.onClick.RemoveListener(OnGemReviveClicked);
        if (adReviveButton != null) adReviveButton.onClick.RemoveListener(OnAdReviveClicked);
        if (getRewardButton != null) getRewardButton.onClick.RemoveListener(ClaimRewardAndReturnHome);
        if (vipTripleButton != null) vipTripleButton.onClick.RemoveListener(ClaimTripleRewardAndReturnHome);
        if (detailsButton != null) detailsButton.onClick.RemoveListener(OnDetailsClicked);
    }

    private void HandlePlayerDeath()
    {
        if (resultResolved)
            return;

        timeScaleBeforePrompt = Time.timeScale > 0f ? Time.timeScale : 1f;
        ownsGameplayPause = true;
        Time.timeScale = 0f;
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(revivePanel, false);

        if (playerDeathController == null || !playerDeathController.enabled)
        {
            ShowRevivePrompt();
        }
    }

    private void HandleDeathSequenceCompleted()
    {
        if (!resultResolved && playerHealth != null && playerHealth.IsDead)
        {
            ShowRevivePrompt();
        }
    }

    private void ShowRevivePrompt()
    {
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(revivePanel, true);
        waitingForRewardedAd = false;
        RefreshReviveButtons();

        if (reviveFeedbackText != null)
        {
            reviveFeedbackText.text = string.Empty;
        }

        if (reviveRevealRoutine != null) StopCoroutine(reviveRevealRoutine);
        reviveRevealRoutine = StartCoroutine(PlayReviveReveal());
    }

    public bool TryGemRevive()
    {
        if (resultResolved || playerHealth == null)
            return false;

        if (!ChipManager.TrySpendRedGems(reviveGemCost))
        {
            if (reviveFeedbackText != null)
                reviveFeedbackText.text = $"NOT ENOUGH GEMS ({ChipManager.RedGems:N0}/{reviveGemCost:N0})";
            RefreshReviveButtons();
            return false;
        }

        if (!CompleteRevive())
        {
            ChipManager.AddRedGems(reviveGemCost);
            return false;
        }

        return true;
    }

    [Obsolete("Use TryGemRevive().")]
    public bool TryVipRevive()
    {
        return TryGemRevive();
    }

    private bool CompleteRevive()
    {
        if (resultResolved || playerHealth == null || !playerHealth.Revive(0.5f, 2f))
            return false;

        if (playerDeathController != null)
        {
            playerDeathController.ResetForRevive();
        }

        SetPanelActive(revivePanel, false);
        Time.timeScale = timeScaleBeforePrompt;
        ownsGameplayPause = false;
        return true;
    }

    private void OnGemReviveClicked()
    {
        TryGemRevive();
    }

    private void OnAdReviveClicked()
    {
        if (resultResolved || waitingForRewardedAd)
            return;

        Action<Action<bool>> request = OnRewardedReviveRequested;
        if (request == null)
        {
            if (reviveFeedbackText != null)
                reviveFeedbackText.text = "REWARDED ADS NOT CONFIGURED";
            return;
        }

        waitingForRewardedAd = true;
        RefreshReviveButtons();
        if (reviveFeedbackText != null) reviveFeedbackText.text = "LOADING AD...";
        request.Invoke(CompleteRewardedAd);
    }

    public void CompleteRewardedAd(bool rewardEarned)
    {
        if (!waitingForRewardedAd || resultResolved)
            return;

        waitingForRewardedAd = false;
        if (rewardEarned && CompleteRevive())
            return;

        if (reviveFeedbackText != null)
            reviveFeedbackText.text = rewardEarned ? "REVIVE FAILED" : "AD NOT COMPLETED";
        RefreshReviveButtons();
    }

    private void RefreshReviveButtons()
    {
        if (gemReviveButton != null)
            gemReviveButton.interactable = !waitingForRewardedAd && ChipManager.HasEnoughRedGems(reviveGemCost);
        if (adReviveButton != null)
            adReviveButton.interactable = !waitingForRewardedAd;
        if (noButton != null)
            noButton.interactable = !waitingForRewardedAd;
    }

    private IEnumerator PlayReviveReveal()
    {
        if (reviveCanvasGroup != null) reviveCanvasGroup.alpha = 0f;
        if (reviveContent != null) reviveContent.localScale = Vector3.one * 0.78f;

        const float duration = 0.34f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * (1f - t) * 0.12f;
            if (reviveCanvasGroup != null) reviveCanvasGroup.alpha = eased;
            if (reviveContent != null) reviveContent.localScale = Vector3.one * (Mathf.Lerp(0.78f, 1f, eased) + overshoot);
            yield return null;
        }

        if (reviveCanvasGroup != null) reviveCanvasGroup.alpha = 1f;
        if (reviveContent != null) reviveContent.localScale = Vector3.one;
        reviveRevealRoutine = null;
    }

    public void ChooseNo()
    {
        if (resultResolved)
            return;

        resultResolved = true;
        SetPanelActive(revivePanel, false);
        PopulateGameOverResult();
        SetPanelActive(gameOverPanel, true);
    }

    private void PopulateGameOverResult()
    {
        int chapterNumber = PlayerDataService.SelectedChapterIndex + 1;
        int totalWaves = enemySpawner != null ? enemySpawner.TotalWavesCount : 1;
        int currentWaveIndex = enemySpawner != null ? enemySpawner.CurrentWaveIndex : 0;
        int completedWaves = enemySpawner != null && enemySpawner.CurrentState == EnemySpawner.WaveState.WaveBreak
            ? currentWaveIndex + 1
            : currentWaveIndex;
        completedWaves = Mathf.Clamp(completedWaves, 0, totalWaves);

        float currentWaveProgress = enemySpawner != null ? enemySpawner.CurrentWaveTimeProgress : 0f;
        float stageProgress = CalculateStageProgress(currentWaveIndex, currentWaveProgress, totalWaves);
        int kills = enemySpawner != null ? enemySpawner.EnemiesKilledInWave : 0;
        pendingDataChipReward = baseDataChipReward + completedWaves * dataChipsPerCompletedWave + kills * dataChipsPerKill;
        pendingRedGemReward = completedWaves * redGemsPerCompletedWave;
        rewardsGranted = false;

        if (chapterText != null) chapterText.text = $"CHAPTER. {chapterNumber:00}";
        if (wavesText != null) wavesText.text = $"{Mathf.Clamp(currentWaveIndex + 1, 1, totalWaves):00} / {totalWaves:00} WAVES";
        if (progressText != null) progressText.text = $"STAGE PROGRESS  {Mathf.RoundToInt(stageProgress * 100f)}%";
        if (dataChipRewardText != null) dataChipRewardText.text = $"Get {pendingDataChipReward:N0}";
        if (redGemRewardText != null) redGemRewardText.text = $"Get {pendingRedGemReward:N0}";
        if (gameOverFeedbackText != null) gameOverFeedbackText.text = string.Empty;
        if (getRewardButton != null) getRewardButton.interactable = true;
        if (vipTripleButton != null) vipTripleButton.interactable = true;
        EnsureDetailsUiComponents();
        if (detailsButton != null) detailsButton.interactable = true;

        ChipsetBattleStats.FinalizeBattle();
    }

    private void OnDetailsClicked()
    {
        EnsureDetailsUiComponents();
        if (damageDetailsPopup != null)
        {
            damageDetailsPopup.Show();
        }
    }

    public void EnsureDetailsUiComponents()
    {
        if (gameOverPanel != null)
        {
            Transform content = gameOverPanel.transform.Find("GameOverContent") ?? gameOverPanel.transform;

            // 1. Dịch chuyển DataChipReward và RedGemReward sang trái
            Transform dataRow = content.Find("DataChipReward");
            if (dataRow != null)
            {
                RectTransform dataRt = dataRow.GetComponent<RectTransform>();
                if (Mathf.Abs(dataRt.anchoredPosition.x) < 20f)
                {
                    dataRt.anchoredPosition = new Vector2(-110f, 135f);
                    dataRt.sizeDelta = new Vector2(340f, 118f);
                    Transform icon = dataRow.Find("Icon");
                    if (icon != null) icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-95f, 0f);
                    Transform val = dataRow.Find("Value");
                    if (val != null) val.GetComponent<RectTransform>().anchoredPosition = new Vector2(55f, 0f);
                }
            }

            Transform gemRow = content.Find("RedGemReward");
            if (gemRow != null)
            {
                RectTransform gemRt = gemRow.GetComponent<RectTransform>();
                if (Mathf.Abs(gemRt.anchoredPosition.x) < 20f)
                {
                    gemRt.anchoredPosition = new Vector2(-110f, 10f);
                    gemRt.sizeDelta = new Vector2(340f, 118f);
                    Transform icon = gemRow.Find("Icon");
                    if (icon != null) icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-95f, 0f);
                    Transform val = gemRow.Find("Value");
                    if (val != null) val.GetComponent<RectTransform>().anchoredPosition = new Vector2(55f, 0f);
                }
            }

            // 2. Tìm hoặc tạo nút DetailsButton
            if (detailsButton == null)
            {
                Transform existingBtn = content.Find("DetailsButton");
                if (existingBtn != null)
                {
                    detailsButton = existingBtn.GetComponent<Button>();
                }
                else
                {
                    detailsButton = CreateRuntimeDetailsButton(content);
                }
            }

            if (detailsButton != null)
            {
                detailsButton.onClick.RemoveListener(OnDetailsClicked);
                detailsButton.onClick.AddListener(OnDetailsClicked);
            }
        }

        // 3. Tìm hoặc tạo DamageDetailsPopup
        if (damageDetailsPopup == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                damageDetailsPopup = canvas.GetComponentInChildren<DamageDetailsPopup>(true);
                if (damageDetailsPopup == null)
                {
                    damageDetailsPopup = DamageDetailsPopup.CreateRuntimeModal(canvas.transform);
                }
            }
        }
    }

    private static Button CreateRuntimeDetailsButton(Transform parent)
    {
        TMP_FontAsset font = FindAnyObjectByType<TMP_Text>()?.font ?? TMP_Settings.defaultFontAsset;

        GameObject btnObj = new GameObject("DetailsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(175f, 72f);
        btnRt.sizeDelta = new Vector2(140f, 180f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = Color.clear;
        btnImg.raycastTarget = true;
        Button button = btnObj.GetComponent<Button>();

        // Icon biểu đồ (Chart icon)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, 28f);
        iconRt.sizeDelta = new Vector2(72f, 72f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        Sprite chartSprite = allSprites?.FirstOrDefault(s => s != null && (s.name == "icon-damage-details" || s.name.Contains("damage-details")));
        if (chartSprite != null)
        {
            iconImg.sprite = chartSprite;
        }
        else
        {
            iconImg.color = new Color32(255, 160, 32, 255);
        }

        // Chữ Details màu cam
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0f, -34f);
        labelRt.sizeDelta = new Vector2(140f, 40f);

        TMP_Text label = labelObj.GetComponent<TMP_Text>();
        if (font != null) label.font = font;
        label.text = "Details";
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color32(255, 160, 32, 255);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return button;
    }

    public void ClaimRewardAndReturnHome()
    {
        if (!TryGrantGameOverReward(1))
            return;

        ReturnHome();
    }

    public void ClaimTripleRewardAndReturnHome()
    {
        if (!PlayerDataService.IsVipOwned)
        {
            if (gameOverFeedbackText != null) gameOverFeedbackText.text = "VIP REQUIRED - BUY VIP IN SHOP";
            return;
        }

        if (!TryGrantGameOverReward(3))
            return;

        ReturnHome();
    }

    private bool TryGrantGameOverReward(int multiplier)
    {
        if (!resultResolved || rewardsGranted)
            return false;

        rewardsGranted = true;
        int safeMultiplier = Mathf.Max(1, multiplier);
        ChipManager.AddDataChips(pendingDataChipReward * safeMultiplier);
        ChipManager.AddRedGems(pendingRedGemReward * safeMultiplier);
        if (getRewardButton != null) getRewardButton.interactable = false;
        if (vipTripleButton != null) vipTripleButton.interactable = false;
        return true;
    }

    public static float CalculateStageProgress(int currentWaveIndex, float currentWaveProgress, int totalWaves)
    {
        int safeTotal = Mathf.Max(1, totalWaves);
        return Mathf.Clamp01((Mathf.Clamp(currentWaveIndex, 0, safeTotal) + Mathf.Clamp01(currentWaveProgress)) / safeTotal);
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

    public void ResolveWithVictory()
    {
        resultResolved = true;
        ownsGameplayPause = false;
        SetPanelActive(revivePanel, false);
        SetPanelActive(gameOverPanel, false);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
