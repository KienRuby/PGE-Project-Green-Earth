using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Pause Menu trong màn chơi GamePlay theo đúng mẫu thiết kế pixel:
/// - 3 Tab chính: STATS, CHIPSET, ARTIFACT
/// - Trong STATS: 3 Sub-tab (DEF, Attack, Other) + Thẻ nhân vật Bernard (Cấp độ, Exp %)
/// - Các chỉ số đọc trực tiếp từ PlayerHealth, PlayerStatsManager, PlayerLevelController
/// - 2 Nút hành động dưới cùng: Tiếp tục chơi (Resume ▶) và Trở về màn hình chính (Home 🏠)
/// </summary>
public class PauseModalController : MonoBehaviour
{
    [Header("1. Root Modal & Backdrop")]
    [Tooltip("Root GameObject của modal Pause (để bật/tắt khi tạm dừng).")]
    [SerializeField] private GameObject modalRoot;

    [Header("2. Main Tabs")]
    [SerializeField] private Button statsMainTabButton;
    [SerializeField] private Button chipsetMainTabButton;
    [SerializeField] private Button artifactMainTabButton;

    [SerializeField] private Image statsTabBg;
    [SerializeField] private Image chipsetTabBg;
    [SerializeField] private Image artifactTabBg;

    [SerializeField] private TMP_Text statsTabText;
    [SerializeField] private TMP_Text chipsetTabText;
    [SerializeField] private TMP_Text artifactTabText;

    [Header("3. Main Tab Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject chipsetPanel;
    [SerializeField] private GameObject artifactPanel;

    [Header("4. Stats Sub-Tabs")]
    [SerializeField] private Button defSubTabButton;
    [SerializeField] private Button attackSubTabButton;
    [SerializeField] private Button otherSubTabButton;

    [SerializeField] private Image defSubTabBg;
    [SerializeField] private Image attackSubTabBg;
    [SerializeField] private Image otherSubTabBg;

    [SerializeField] private TMP_Text defSubTabText;
    [SerializeField] private TMP_Text attackSubTabText;
    [SerializeField] private TMP_Text otherSubTabText;

    [Header("5. Stats Sub-Panels")]
    [SerializeField] private GameObject defStatsPanel;
    [SerializeField] private GameObject attackStatsPanel;
    [SerializeField] private GameObject otherStatsPanel;

    [Header("6. Character Card Info")]
    [SerializeField] private Image characterAvatarImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text characterLevelExpText;

    [Header("7. DEF Stat Value Texts")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text defValueText;
    [SerializeField] private TMP_Text rangedDefValueText;
    [SerializeField] private TMP_Text evasionRateValueText;
    [SerializeField] private TMP_Text kitRecoveryValueText;
    [SerializeField] private TMP_Text autoRecoveryValueText;
    [SerializeField] private TMP_Text ailmentResistValueText;

    [Header("8. Attack Stat Value Texts")]
    [SerializeField] private TMP_Text atkValueText;
    [SerializeField] private TMP_Text atkSpeedValueText;
    [SerializeField] private TMP_Text critAtkValueText;
    [SerializeField] private TMP_Text critRateValueText;
    [SerializeField] private TMP_Text lifeStealValueText;

    [Header("9. Other Stat Value Texts")]
    [SerializeField] private TMP_Text moveSpeedValueText;
    [SerializeField] private TMP_Text obtainedChipsValueText;
    [SerializeField] private TMP_Text chipsetSelectValueText;
    [SerializeField] private TMP_Text droneAtkValueText;
    [SerializeField] private TMP_Text turretAtkValueText;
    [SerializeField] private TMP_Text turretDurationValueText;

    [Header("10. Bottom Action Buttons")]
    [Tooltip("Nút tiếp tục trận đấu (Resume ▶).")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Nút thoát về Menu chính (Home 🏠).")]
    [SerializeField] private Button homeButton;

    [Header("11. Return to Main Menu Confirmation Dialog")]
    [Tooltip("Panel hộp thoại xác nhận khi bấm nút Home.")]
    [SerializeField] private GameObject quitConfirmPanel;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private Button confirmOkButton;
    [SerializeField] private TMP_Text confirmMessageText;

    [Header("12. Runtime References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStatsManager playerStats;
    [SerializeField] private PlayerLevelController playerLevel;

    // Styling Colors
    private static readonly Color32 ActiveTabBg = new Color32(88, 172, 178, 255);
    private static readonly Color32 InactiveTabBg = new Color32(36, 70, 86, 255);
    private static readonly Color32 ActiveSubTabBg = new Color32(88, 172, 178, 255);
    private static readonly Color32 InactiveSubTabBg = new Color32(48, 80, 96, 255);

    private static readonly Color32 ActiveTextCol = new Color32(14, 28, 36, 255);
    private static readonly Color32 InactiveTextCol = new Color32(160, 200, 205, 255);

    public bool IsPaused { get; private set; }
    public int CurrentMainTab { get; private set; } = 0; // 0 = Stats, 1 = Chipset, 2 = Artifact
    public int CurrentSubTab { get; private set; } = 0;  // 0 = DEF, 1 = Attack, 2 = Other

    private void Awake()
    {
        BindButtons();
        LocatePlayerReferences();
    }

    private void Start()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
    }

    private void BindButtons()
    {
        if (statsMainTabButton != null)
        {
            statsMainTabButton.onClick.RemoveListener(OnStatsMainTabClicked);
            statsMainTabButton.onClick.AddListener(OnStatsMainTabClicked);
        }
        if (chipsetMainTabButton != null)
        {
            chipsetMainTabButton.onClick.RemoveListener(OnChipsetMainTabClicked);
            chipsetMainTabButton.onClick.AddListener(OnChipsetMainTabClicked);
        }
        if (artifactMainTabButton != null)
        {
            artifactMainTabButton.onClick.RemoveListener(OnArtifactMainTabClicked);
            artifactMainTabButton.onClick.AddListener(OnArtifactMainTabClicked);
        }

        if (defSubTabButton != null)
        {
            defSubTabButton.onClick.RemoveListener(OnDefSubTabClicked);
            defSubTabButton.onClick.AddListener(OnDefSubTabClicked);
        }
        if (attackSubTabButton != null)
        {
            attackSubTabButton.onClick.RemoveListener(OnAttackSubTabClicked);
            attackSubTabButton.onClick.AddListener(OnAttackSubTabClicked);
        }
        if (otherSubTabButton != null)
        {
            otherSubTabButton.onClick.RemoveListener(OnOtherSubTabClicked);
            otherSubTabButton.onClick.AddListener(OnOtherSubTabClicked);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            homeButton.onClick.AddListener(OnHomeButtonClicked);
        }
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(OnConfirmNoClicked);
            confirmNoButton.onClick.AddListener(OnConfirmNoClicked);
        }
        if (confirmOkButton != null)
        {
            confirmOkButton.onClick.RemoveListener(OnConfirmOkClicked);
            confirmOkButton.onClick.AddListener(OnConfirmOkClicked);
        }
    }

    private void LocatePlayerReferences()
    {
        if (playerHealth == null || playerStats == null || playerLevel == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                if (playerHealth == null) playerHealth = playerGo.GetComponent<PlayerHealth>();
                if (playerStats == null) playerStats = playerGo.GetComponent<PlayerStatsManager>();
                if (playerLevel == null) playerLevel = playerGo.GetComponent<PlayerLevelController>();
            }
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            OpenPauseModal();
        }
    }

    public void OpenPauseModal()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        LocatePlayerReferences();
        RefreshAllStats();

        SelectMainTab(0);
        SelectSubTab(0);

        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    public void OnHomeButtonClicked()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
        }
        else
        {
            QuitToMainMenu();
        }
    }

    public void OnConfirmNoClicked()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
    }

    public void OnConfirmOkClicked()
    {
        QuitToMainMenu();
    }

    public void QuitToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnStatsMainTabClicked() => SelectMainTab(0);
    public void OnChipsetMainTabClicked() => SelectMainTab(1);
    public void OnArtifactMainTabClicked() => SelectMainTab(2);

    public void OnDefSubTabClicked() => SelectSubTab(0);
    public void OnAttackSubTabClicked() => SelectSubTab(1);
    public void OnOtherSubTabClicked() => SelectSubTab(2);

    public void SelectMainTab(int index)
    {
        CurrentMainTab = index;

        if (statsPanel != null) statsPanel.SetActive(index == 0);
        if (chipsetPanel != null) chipsetPanel.SetActive(index == 1);
        if (artifactPanel != null) artifactPanel.SetActive(index == 2);

        SetTabVisual(statsTabBg, statsTabText, index == 0);
        SetTabVisual(chipsetTabBg, chipsetTabText, index == 1);
        SetTabVisual(artifactTabBg, artifactTabText, index == 2);
    }

    public void SelectSubTab(int index)
    {
        CurrentSubTab = index;

        if (defStatsPanel != null) defStatsPanel.SetActive(index == 0);
        if (attackStatsPanel != null) attackStatsPanel.SetActive(index == 1);
        if (otherStatsPanel != null) otherStatsPanel.SetActive(index == 2);

        SetSubTabVisual(defSubTabBg, defSubTabText, index == 0);
        SetSubTabVisual(attackSubTabBg, attackSubTabText, index == 1);
        SetSubTabVisual(otherSubTabBg, otherSubTabText, index == 2);
    }

    private void SetTabVisual(Image bg, TMP_Text text, bool isActive)
    {
        if (bg != null) bg.color = isActive ? ActiveTabBg : InactiveTabBg;
        if (text != null) text.color = isActive ? ActiveTextCol : InactiveTextCol;
    }

    private void SetSubTabVisual(Image bg, TMP_Text text, bool isActive)
    {
        if (bg != null) bg.color = isActive ? ActiveSubTabBg : InactiveSubTabBg;
        if (text != null) text.color = isActive ? ActiveTextCol : InactiveTextCol;
    }

    public void RefreshAllStats()
    {
        // 1. Character Name & Level
        if (characterNameText != null)
        {
            characterNameText.text = "Bernard";
        }

        if (characterLevelExpText != null)
        {
            if (playerLevel != null)
            {
                float expPercent = playerLevel.EXPProgress * 100f;
                characterLevelExpText.text = $"LV.{playerLevel.CurrentLevel:D2} ({expPercent:F2}%)";
            }
            else
            {
                characterLevelExpText.text = "LV.01 (0,00%)";
            }
        }

        // 2. DEF Stats
        int curHp = playerHealth != null ? playerHealth.CurrentHealth : 260;
        int maxHp = playerHealth != null ? playerHealth.MaxHealth : 260;
        int def = playerStats != null ? playerStats.DamageReduction + 9 : 9;
        float regen = playerStats != null ? playerStats.HealthRegenPerSecond + 1.1f : 1.1f;

        if (hpValueText != null) hpValueText.text = $"{curHp}/{maxHp}";
        if (defValueText != null) defValueText.text = $"{def}";
        if (rangedDefValueText != null) rangedDefValueText.text = "0%";
        if (evasionRateValueText != null) evasionRateValueText.text = "3%";
        if (kitRecoveryValueText != null) kitRecoveryValueText.text = "30%";
        if (autoRecoveryValueText != null) autoRecoveryValueText.text = $"{regen:F1}/sec";
        if (ailmentResistValueText != null) ailmentResistValueText.text = "0%";

        // 3. Attack Stats
        float atkBonus = playerStats != null ? 3.5f + (playerStats.BonusDamage * 0.5f) : 3.5f;
        float critRate = playerStats != null ? (playerStats.CritChance * 100f) + 3.5f : 3.5f;

        if (atkValueText != null) atkValueText.text = $"{atkBonus:F1}%";
        if (atkSpeedValueText != null) atkSpeedValueText.text = "0%";
        if (critAtkValueText != null) critAtkValueText.text = "150%";
        if (critRateValueText != null) critRateValueText.text = $"{critRate:F1}%";
        if (lifeStealValueText != null) lifeStealValueText.text = "0%";

        // 4. Other Stats
        float moveSpd = playerStats != null ? 2.0f + (playerStats.BonusSpeed * 2.0f) : 2.0f;
        if (moveSpeedValueText != null) moveSpeedValueText.text = $"{moveSpd:F0}%";
        if (obtainedChipsValueText != null) obtainedChipsValueText.text = "2%";
        if (chipsetSelectValueText != null) chipsetSelectValueText.text = "3%";
        if (droneAtkValueText != null) droneAtkValueText.text = "0%";
        if (turretAtkValueText != null) turretAtkValueText.text = "0%";
        if (turretDurationValueText != null) turretDurationValueText.text = "0%";
    }

    public void SetReferencesForTesting(
        GameObject root,
        Button resumeBtn,
        Button homeBtn,
        Button statsTab,
        Button chipTab,
        Button artTab,
        GameObject statsPnl,
        GameObject chipPnl,
        GameObject artPnl,
        Button defSubTab,
        Button atkSubTab,
        Button othSubTab,
        GameObject defPnl,
        GameObject atkPnl,
        GameObject othPnl,
        TMP_Text hpTxt,
        TMP_Text defTxt,
        TMP_Text lvlExpTxt,
        GameObject quitConfirmPnl = null,
        Button noBtn = null,
        Button okBtn = null)
    {
        modalRoot = root;
        resumeButton = resumeBtn;
        homeButton = homeBtn;
        statsMainTabButton = statsTab;
        chipsetMainTabButton = chipTab;
        artifactMainTabButton = artTab;
        statsPanel = statsPnl;
        chipsetPanel = chipPnl;
        artifactPanel = artPnl;
        defSubTabButton = defSubTab;
        attackSubTabButton = atkSubTab;
        otherSubTabButton = othSubTab;
        defStatsPanel = defPnl;
        attackStatsPanel = atkPnl;
        otherStatsPanel = othPnl;
        hpValueText = hpTxt;
        defValueText = defTxt;
        characterLevelExpText = lvlExpTxt;
        quitConfirmPanel = quitConfirmPnl;
        confirmNoButton = noBtn;
        confirmOkButton = okBtn;
    }
}
