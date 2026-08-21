using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Button noButton;
    [SerializeField] private Button vipReviveButton;
    [SerializeField] private TMP_Text reviveFeedbackText;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text wavesText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text dataChipRewardText;
    [SerializeField] private TMP_Text redGemRewardText;
    [SerializeField] private Button homeButton;

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
    private float timeScaleBeforePrompt = 1f;

    public bool IsRevivePromptVisible => revivePanel != null && revivePanel.activeSelf;
    public bool IsGameOverVisible => gameOverPanel != null && gameOverPanel.activeSelf;

    private void Awake()
    {
        ResolveGameplayReferences();

        SetPanelActive(revivePanel, false);
        SetPanelActive(gameOverPanel, false);
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveGameplayReferences();
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

        if (vipReviveButton != null)
        {
            vipReviveButton.onClick.RemoveListener(OnVipReviveClicked);
            vipReviveButton.onClick.AddListener(OnVipReviveClicked);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(ReturnHome);
            homeButton.onClick.AddListener(ReturnHome);
        }
    }

    private void UnbindButtons()
    {
        if (noButton != null) noButton.onClick.RemoveListener(ChooseNo);
        if (vipReviveButton != null) vipReviveButton.onClick.RemoveListener(OnVipReviveClicked);
        if (homeButton != null) homeButton.onClick.RemoveListener(ReturnHome);
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

        if (reviveFeedbackText != null)
        {
            reviveFeedbackText.text = PlayerDataService.IsVipOwned
                ? "VIP REVIVE READY"
                : "VIP REQUIRED - BUY VIP IN SHOP";
        }
    }

    public bool TryVipRevive()
    {
        if (resultResolved || playerHealth == null)
            return false;

        if (!PlayerDataService.IsVipOwned)
        {
            if (reviveFeedbackText != null)
                reviveFeedbackText.text = "VIP REQUIRED - BUY VIP IN SHOP";
            return false;
        }

        if (!playerHealth.Revive(0.5f, 2f))
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

    private void OnVipReviveClicked()
    {
        TryVipRevive();
    }

    public void ChooseNo()
    {
        if (resultResolved)
            return;

        resultResolved = true;
        SetPanelActive(revivePanel, false);
        PopulateAndGrantGameOverResult();
        SetPanelActive(gameOverPanel, true);
    }

    private void PopulateAndGrantGameOverResult()
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
        int dataChipReward = baseDataChipReward + completedWaves * dataChipsPerCompletedWave + kills * dataChipsPerKill;
        int redGemReward = completedWaves * redGemsPerCompletedWave;

        if (!rewardsGranted)
        {
            rewardsGranted = true;
            ChipManager.AddDataChips(dataChipReward);
            ChipManager.AddRedGems(redGemReward);
        }

        if (chapterText != null) chapterText.text = $"CHAPTER. {chapterNumber:00}";
        if (wavesText != null) wavesText.text = $"{Mathf.Clamp(currentWaveIndex + 1, 1, totalWaves):00} / {totalWaves:00} WAVES";
        if (progressText != null) progressText.text = $"STAGE PROGRESS  {Mathf.RoundToInt(stageProgress * 100f)}%";
        if (dataChipRewardText != null) dataChipRewardText.text = $"GET {dataChipReward:N0}";
        if (redGemRewardText != null) redGemRewardText.text = $"GET {redGemReward:N0}";
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

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
