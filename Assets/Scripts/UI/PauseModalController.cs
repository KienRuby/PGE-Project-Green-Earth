using System;
using System.Collections.Generic;
using System.Linq;
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

    [Header("2. Main Tabs (Legacy / Text-based)")]
    [SerializeField] private Button statsMainTabButton;
    [SerializeField] private Button chipsetMainTabButton;
    [SerializeField] private Button artifactMainTabButton;

    [SerializeField] private Image statsTabBg;
    [SerializeField] private Image chipsetTabBg;
    [SerializeField] private Image artifactTabBg;

    [SerializeField] private TMP_Text statsTabText;
    [SerializeField] private TMP_Text chipsetTabText;
    [SerializeField] private TMP_Text artifactTabText;

    [Header("2b. Main Tab On/Off Buttons (Design Pixel)")]
    [Tooltip("Nút / Đối tượng hiển thị trạng thái ON của tab Stats.")]
    [SerializeField] private GameObject statsOn;
    [Tooltip("Nút / Đối tượng hiển thị trạng thái OFF của tab Stats.")]
    [SerializeField] private GameObject statsOff;

    [Tooltip("Nút / Đối tượng hiển thị trạng thái ON của tab Chipset.")]
    [SerializeField] private GameObject chipsetOn;
    [Tooltip("Nút / Đối tượng hiển thị trạng thái OFF của tab Chipset.")]
    [SerializeField] private GameObject chipsetOff;

    [Tooltip("Nút / Đối tượng hiển thị trạng thái ON của tab Artifact.")]
    [SerializeField] private GameObject artifactOn;
    [Tooltip("Nút / Đối tượng hiển thị trạng thái OFF của tab Artifact.")]
    [SerializeField] private GameObject artifactOff;

    [Header("2c. Setting Button (Design Pixel)")]
    [Tooltip("Nút cài đặt ⚙️ (Settin) trong màn Pause.")]
    [SerializeField] private Button settingButton;

    [Header("2d. Tab Sprites (Pixel Design)")]
    [SerializeField] private Sprite statsOnSprite;
    [SerializeField] private Sprite statsOffSprite;
    [SerializeField] private Sprite chipsetOnSprite;
    [SerializeField] private Sprite chipsetOffSprite;
    [SerializeField] private Sprite artifactOnSprite;
    [SerializeField] private Sprite artifactOffSprite;

    [Header("3. Main Tab Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject chipsetPanel;
    [SerializeField] private GameObject artifactPanel;

    [Header("3b. Chipset Panel & Equipped Chips")]
    [Tooltip("Template ô card hiển thị Chipset đã trang bị.")]
    [SerializeField] private GameObject equippedChipCardTemplate;
    [Tooltip("Thư viện icon và khung chipset dùng chung từ MainMenu/Gameplay.")]
    [SerializeField] private ChipsetLevelVisualLibrary visualLibrary;

    [Serializable]
    public class RuntimeEquippedChipData
    {
        public int id;
        public string name;
        public string iconKey;
        public int level = 1;
        public ChipTier tier = ChipTier.Magic;
        public Sprite cachedIconSprite;
        public Sprite cachedFrameSprite;
    }

    private readonly List<RuntimeEquippedChipData> runtimeEquippedChips = new List<RuntimeEquippedChipData>();
    private readonly List<GameObject> spawnedChipCards = new List<GameObject>();

    public IReadOnlyList<RuntimeEquippedChipData> RuntimeEquippedChips => runtimeEquippedChips;
    public IReadOnlyList<GameObject> SpawnedChipCards => spawnedChipCards;

    [Header("3c. Artifact Panel & Icon Slots (Design Pixel)")]
    [Tooltip("Danh sách các ô icon Cổ Vật (Artifact) trong Scene. Vị trí và kích thước được giữ nguyên 100% khi sang Play Mode.")]
    [SerializeField] private List<GameObject> artifactIconSlots = new List<GameObject>();
    [Tooltip("Nếu bật, trong Play Mode các ô chưa trang bị Cổ Vật sẽ tạm thời ẩn đi.")]
    [SerializeField] private bool hideUnusedArtifactSlots = true;

    public IReadOnlyList<GameObject> ArtifactIconSlots => artifactIconSlots;
    public bool HideUnusedArtifactSlots { get => hideUnusedArtifactSlots; set => hideUnusedArtifactSlots = value; }

    [Header("3d. Artifact Detail Dialog (Popup Chi Tiết Cổ Vật)")]
    [Tooltip("Panel hộp thoại chi tiết Cổ Vật khi click vào icon.")]
    [SerializeField] private GameObject artifactDetailPanel;
    [SerializeField] private Image artifactDetailIcon;
    [SerializeField] private TMP_Text artifactDetailNameText;
    [SerializeField] private TMP_Text artifactDetailLoreText;
    [SerializeField] private TMP_Text artifactDetailStatText;
    [SerializeField] private Button artifactDetailOkButton;

    public GameObject ArtifactDetailPanel => artifactDetailPanel;
    public Image ArtifactDetailIcon => artifactDetailIcon;
    public TMP_Text ArtifactDetailNameText => artifactDetailNameText;
    public TMP_Text ArtifactDetailLoreText => artifactDetailLoreText;
    public TMP_Text ArtifactDetailStatText => artifactDetailStatText;
    public Button ArtifactDetailOkButton => artifactDetailOkButton;

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

    [Header("13. Combat Damage Details")]
    [Tooltip("Nút mở modal thống kê chi tiết sát thương (icon biểu đồ).")]
    [SerializeField] private Button damageDetailsButton;

    [Tooltip("Modal Thống Kê Sát Thương Chi Tiết.")]
    [SerializeField] private DamageDetailsPopup damageDetailsPopup;

    public Button DamageDetailsButton => damageDetailsButton;
    public DamageDetailsPopup DamageDetailsPopup => damageDetailsPopup;

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

    public GameObject StatsOn => statsOn;
    public GameObject StatsOff => statsOff;
    public GameObject ChipsetOn => chipsetOn;
    public GameObject ChipsetOff => chipsetOff;
    public GameObject ArtifactOn => artifactOn;
    public GameObject ArtifactOff => artifactOff;
    public Button SettingButton => settingButton;

    public Sprite StatsOnSprite { get => statsOnSprite; set => statsOnSprite = value; }
    public Sprite StatsOffSprite { get => statsOffSprite; set => statsOffSprite = value; }
    public Sprite ChipsetOnSprite { get => chipsetOnSprite; set => chipsetOnSprite = value; }
    public Sprite ChipsetOffSprite { get => chipsetOffSprite; set => chipsetOffSprite = value; }
    public Sprite ArtifactOnSprite { get => artifactOnSprite; set => artifactOnSprite = value; }
    public Sprite ArtifactOffSprite { get => artifactOffSprite; set => artifactOffSprite = value; }

    private void Awake()
    {
        AutoWireTabButtonsAndSettings();
        AutoWireArtifactDetailDialog();
        EnsureDamageDetailsComponents();
        BindButtons();
        LocatePlayerReferences();
        RefreshEquippedChips();
    }

    private void OnEnable()
    {
        AutoWireTabButtonsAndSettings();
        AutoWireArtifactDetailDialog();
        BindButtons();
        ChipsetLevelUpPopup.OnRuntimeChipsetSelected -= HandleChipsetSelected;
        ChipsetLevelUpPopup.OnRuntimeChipsetSelected += HandleChipsetSelected;
    }

    private void OnDisable()
    {
        ChipsetLevelUpPopup.OnRuntimeChipsetSelected -= HandleChipsetSelected;
    }

    private void OnDestroy()
    {
        ChipsetLevelUpPopup.OnRuntimeChipsetSelected -= HandleChipsetSelected;
    }

    private void Start()
    {
        // Chỉ ẩn modal nếu game không ở trạng thái pause
        if (!IsPaused && modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
        if (artifactDetailPanel != null)
        {
            artifactDetailPanel.SetActive(false);
        }
        RefreshEquippedChips();
    }

    private void BindButtons()
    {
        // 1. Tab buttons (Legacy / Main)
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

        // 2. Separate On/Off buttons (Pixel Design) if distinct from main tab buttons
        if (statsOn != null && statsOn != statsMainTabButton?.gameObject) EnsureButton(statsOn, OnStatsMainTabClicked);
        if (statsOff != null && statsOff != statsMainTabButton?.gameObject) EnsureButton(statsOff, OnStatsMainTabClicked);
        if (chipsetOn != null && chipsetOn != chipsetMainTabButton?.gameObject) EnsureButton(chipsetOn, OnChipsetMainTabClicked);
        if (chipsetOff != null && chipsetOff != chipsetMainTabButton?.gameObject) EnsureButton(chipsetOff, OnChipsetMainTabClicked);
        if (artifactOn != null && artifactOn != artifactMainTabButton?.gameObject) EnsureButton(artifactOn, OnArtifactMainTabClicked);
        if (artifactOff != null && artifactOff != artifactMainTabButton?.gameObject) EnsureButton(artifactOff, OnArtifactMainTabClicked);

        // 3. Stats Sub-tabs
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

        // 4. Bottom action buttons
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(OnSettingButtonClicked);
            settingButton.onClick.AddListener(OnSettingButtonClicked);
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
        if (damageDetailsButton != null)
        {
            damageDetailsButton.onClick.RemoveListener(OnDamageDetailsButtonClicked);
            damageDetailsButton.onClick.AddListener(OnDamageDetailsButtonClicked);
        }
        if (artifactDetailOkButton != null)
        {
            artifactDetailOkButton.onClick.RemoveListener(HideArtifactDetail);
            artifactDetailOkButton.onClick.AddListener(HideArtifactDetail);
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

        if (modalRoot != null)
        {
            UIDissolveController.ShowInstant(modalRoot);
        }

        EnsureDamageDetailsComponents();
        AutoWireTabButtonsAndSettings();
        BindButtons();
        LocatePlayerReferences();
        RefreshAllStats();
        RefreshEquippedChips();

        SelectMainTab(0);
        SelectSubTab(0);

        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
        HideArtifactDetail();
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (damageDetailsPopup != null && damageDetailsPopup.IsVisible)
        {
            damageDetailsPopup.Hide();
        }

        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }

        HideArtifactDetail();

        UIDissolveController dissolve = modalRoot != null ? modalRoot.GetComponent<UIDissolveController>() : null;
        if (dissolve != null && modalRoot != null && modalRoot.activeSelf)
        {
            dissolve.Hide();
        }
        else if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    public void OnHomeButtonClicked()
    {
        if (quitConfirmPanel != null)
        {
            UIDissolveController.ShowInstant(quitConfirmPanel);
        }
        else
        {
            QuitToMainMenu();
        }
    }

    public void OnSettingButtonClicked()
    {
        SettingsPanelController panel = SettingsPanelController.Instance;
        if (panel == null)
        {
            panel = FindObjectOfType<SettingsPanelController>(true);
        }
        if (panel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                panel = SettingsPanelController.CreateRuntimePanel(canvas.transform as RectTransform);
            }
        }

        if (panel != null)
        {
            panel.Toggle();
        }
        else
        {
            Debug.LogWarning("[PauseModalController] Không tìm thấy Canvas hoặc SettingsPanelController để hiển thị Settings.");
        }
    }

    public void OnConfirmNoClicked()
    {
        if (quitConfirmPanel != null)
        {
            UIDissolveController.HideWithEffect(quitConfirmPanel);
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
        HideArtifactDetail();

        // 1. Activate main content panels
        if (statsPanel != null)
        {
            EnsureParentChainActive(statsPanel);
            statsPanel.SetActive(index == 0);
        }
        if (chipsetPanel != null)
        {
            EnsureParentChainActive(chipsetPanel);
            chipsetPanel.SetActive(index == 1);
        }
        if (artifactPanel != null)
        {
            EnsureParentChainActive(artifactPanel);
            artifactPanel.SetActive(index == 2);
        }

        if (index == 1)
        {
            RefreshEquippedChips();
        }
        else if (index == 2)
        {
            RefreshEquippedArtifacts();
        }

        // 2. Ensure sprites are loaded
        LoadTabSpritesIfMissing();

        // 3. Update 3 Main Tab Buttons (MUST ALWAYS BE ACTIVE & VISIBLE!)
        UpdateMainTabVisual(statsMainTabButton, statsTabBg, statsTabText, index == 0, statsOnSprite, statsOffSprite);
        UpdateMainTabVisual(chipsetMainTabButton, chipsetTabBg, chipsetTabText, index == 1, chipsetOnSprite, chipsetOffSprite);
        UpdateMainTabVisual(artifactMainTabButton, artifactTabBg, artifactTabText, index == 2, artifactOnSprite, artifactOffSprite);

        // 4. If separate On/Off GameObjects exist, toggle them safely
        if (statsOn != null && statsOff != null && statsOn != statsOff)
        {
            SetTabOnOff(statsOn, statsOff, index == 0);
        }
        else if (statsOn != null && statsOff == null)
        {
            statsOn.SetActive(true);
        }

        if (chipsetOn != null && chipsetOff != null && chipsetOn != chipsetOff)
        {
            SetTabOnOff(chipsetOn, chipsetOff, index == 1);
        }
        else if (chipsetOn != null && chipsetOff == null)
        {
            chipsetOn.SetActive(true);
        }

        if (artifactOn != null && artifactOff != null && artifactOn != artifactOff)
        {
            SetTabOnOff(artifactOn, artifactOff, index == 2);
        }
        else if (artifactOn != null && artifactOff == null)
        {
            artifactOn.SetActive(true);
        }
    }

    private void UpdateMainTabVisual(Button btn, Image bg, TMP_Text text, bool isSelected, Sprite onSprite, Sprite offSprite)
    {
        if (btn != null)
        {
            btn.gameObject.SetActive(true);
            EnsureParentChainActive(btn.gameObject);
        }

        Image targetImg = bg;
        if (targetImg == null && btn != null)
        {
            targetImg = btn.GetComponent<Image>() ?? btn.GetComponentInChildren<Image>(true);
        }

        if (targetImg != null)
        {
            targetImg.gameObject.SetActive(true);
            Sprite targetSprite = isSelected ? onSprite : offSprite;
            if (targetSprite != null)
            {
                targetImg.sprite = targetSprite;
                targetImg.color = Color.white;
            }
            else
            {
                targetImg.color = isSelected ? ActiveTabBg : InactiveTabBg;
            }
        }

        if (text != null)
        {
            if (onSprite != null && offSprite != null)
            {
                text.gameObject.SetActive(false);
            }
            else
            {
                text.gameObject.SetActive(true);
                text.color = isSelected ? ActiveTextCol : InactiveTextCol;
            }
        }
    }

    private void SetTabOnOff(GameObject onObj, GameObject offObj, bool isOn)
    {
        if (onObj != null)
        {
            EnsureParentChainActive(onObj);
            onObj.SetActive(isOn);
        }
        if (offObj != null)
        {
            EnsureParentChainActive(offObj);
            offObj.SetActive(!isOn);
        }
    }

    private void EnsureParentChainActive(GameObject go)
    {
        if (go == null) return;
        Transform p = go.transform.parent;
        Transform topRoot = modalRoot != null ? modalRoot.transform : transform;
        while (p != null && p != topRoot && p != transform.root)
        {
            if (!p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(true);
            }
            p = p.parent;
        }
    }

    public void SelectSubTab(int index)
    {
        CurrentSubTab = index;

        if (defStatsPanel != null)
        {
            EnsureParentChainActive(defStatsPanel);
            defStatsPanel.SetActive(index == 0);
        }
        if (attackStatsPanel != null)
        {
            EnsureParentChainActive(attackStatsPanel);
            attackStatsPanel.SetActive(index == 1);
        }
        if (otherStatsPanel != null)
        {
            EnsureParentChainActive(otherStatsPanel);
            otherStatsPanel.SetActive(index == 2);
        }

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

    public void OnDamageDetailsButtonClicked()
    {
        EnsureDamageDetailsComponents();
        if (damageDetailsPopup != null)
        {
            damageDetailsPopup.Show();
        }
    }

    public void EnsureDamageDetailsComponents()
    {
        // Damage Details không thuộc thanh nút Pause. Nếu scene cũ vẫn còn nút này
        // thì giữ object/reference để không làm hỏng scene, nhưng vô hiệu hóa nó.
        if (damageDetailsButton == null && modalRoot != null)
        {
            Transform existingBtn = modalRoot.transform.Find("DamageDetailsButton")
                                 ?? modalRoot.transform.Find("DetailsButton");

            if (existingBtn != null)
            {
                damageDetailsButton = existingBtn.GetComponent<Button>();
            }
        }

        if (damageDetailsButton != null)
        {
            damageDetailsButton.onClick.RemoveListener(OnDamageDetailsButtonClicked);
            damageDetailsButton.gameObject.SetActive(false);
        }

        ApplyBottomActionLayout();
    }

    /// <summary>
    /// Giữ thanh nút Pause đúng mẫu: Resume bên trái, Settings ở giữa, Home bên phải.
    /// Damage Details không xuất hiện trên màn Pause.
    /// </summary>
    public void ApplyBottomActionLayout()
    {
        SetBottomButtonPosition(resumeButton, -246f);
        SetBottomButtonPosition(settingButton, 11f);
        SetBottomButtonPosition(homeButton, 259f);

        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(true);
        }

        if (damageDetailsButton != null && damageDetailsButton != resumeButton)
        {
            damageDetailsButton.gameObject.SetActive(false);
        }
    }

    private static void SetBottomButtonPosition(Button button, float x)
    {
        if (button == null) return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(x, -680f);
        }
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
        Button okBtn = null,
        Button dmgDetailsBtn = null,
        DamageDetailsPopup dmgDetailsPopup = null,
        GameObject sOn = null,
        GameObject sOff = null,
        GameObject cOn = null,
        GameObject cOff = null,
        GameObject aOn = null,
        GameObject aOff = null,
        Button setBtn = null)
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
        damageDetailsButton = dmgDetailsBtn;
        damageDetailsPopup = dmgDetailsPopup;
        statsOn = sOn;
        statsOff = sOff;
        chipsetOn = cOn;
        chipsetOff = cOff;
        artifactOn = aOn;
        artifactOff = aOff;
        settingButton = setBtn;
    }

    public void SetTabButtonsForTesting(
        GameObject sOn, GameObject sOff,
        GameObject cOn, GameObject cOff,
        GameObject aOn, GameObject aOff,
        Button setBtn = null)
    {
        statsOn = sOn;
        statsOff = sOff;
        chipsetOn = cOn;
        chipsetOff = cOff;
        artifactOn = aOn;
        artifactOff = aOff;
        settingButton = setBtn;
    }

    public void AutoWireTabButtonsAndSettings()
    {
        Transform searchRoot = modalRoot != null ? modalRoot.transform : transform;

        // Auto-wire main buttons if missing
        if (statsMainTabButton == null)
        {
            GameObject go = FindTabObject(searchRoot, "StatsTabButton", "StatsTab", "StatsButton", "StatsOn", "Stats");
            if (go != null) statsMainTabButton = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        }
        if (chipsetMainTabButton == null)
        {
            GameObject go = FindTabObject(searchRoot, "ChipsetTabButton", "ChipsetTab", "ChipsetButton", "ChipsetOn", "Chipset");
            if (go != null) chipsetMainTabButton = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        }
        if (artifactMainTabButton == null)
        {
            GameObject go = FindTabObject(searchRoot, "ArtifactTabButton", "ArtifactTab", "ArtifactButton", "ArtifactOn", "Artifact");
            if (go != null) artifactMainTabButton = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        }

        if (statsTabBg == null && statsMainTabButton != null)
        {
            statsTabBg = statsMainTabButton.GetComponent<Image>() ?? statsMainTabButton.GetComponentInChildren<Image>(true);
        }
        if (chipsetTabBg == null && chipsetMainTabButton != null)
        {
            chipsetTabBg = chipsetMainTabButton.GetComponent<Image>() ?? chipsetMainTabButton.GetComponentInChildren<Image>(true);
        }
        if (artifactTabBg == null && artifactMainTabButton != null)
        {
            artifactTabBg = artifactMainTabButton.GetComponent<Image>() ?? artifactMainTabButton.GetComponentInChildren<Image>(true);
        }

        // Auto-wire separate On/Off objects if present
        statsOn = WireIfNull(statsOn, searchRoot, "StatsOn", "Stats_On", "Stats On");
        statsOff = WireIfNull(statsOff, searchRoot, "StatsOff", "Stats_Off", "Stats Off");

        chipsetOn = WireIfNull(chipsetOn, searchRoot, "ChipsetOn", "Chipset_On", "Chipset On");
        chipsetOff = WireIfNull(chipsetOff, searchRoot, "ChipsetOff", "Chipset_Off", "Chipset Off");

        artifactOn = WireIfNull(artifactOn, searchRoot, "ArtifactOn", "Artifact_On", "Artifact On");
        artifactOff = WireIfNull(artifactOff, searchRoot, "ArtifactOff", "Artifact_Off", "Artifact Off");

        if (settingButton == null)
        {
            GameObject settingGo = FindTabObject(searchRoot, "Settin", "Setting", "Settings", "btnSetting", "ButtonSetting");
            if (settingGo != null)
            {
                settingButton = settingGo.GetComponent<Button>() ?? settingGo.AddComponent<Button>();
            }
        }

        if (resumeButton == null)
        {
            GameObject resumeGo = FindTabObject(searchRoot, "Tiep tu", "Tieptu", "Tiep_tu", "Resume", "Play");
            if (resumeGo != null)
            {
                resumeButton = resumeGo.GetComponent<Button>() ?? resumeGo.AddComponent<Button>();
            }
        }

        if (homeButton == null)
        {
            GameObject homeGo = FindTabObject(searchRoot, "MainMenu", "Main_Menu", "Home", "HomeButton", "btnHome");
            if (homeGo != null)
            {
                homeButton = homeGo.GetComponent<Button>() ?? homeGo.AddComponent<Button>();
            }
        }

        if (chipsetPanel == null)
        {
            GameObject chipPnl = FindTabObject(searchRoot, "ChipsetPanel", "Chipset_Panel");
            if (chipPnl != null) chipsetPanel = chipPnl;
        }

        if (artifactPanel == null)
        {
            GameObject artPnl = FindTabObject(searchRoot, "ArtifactPanel", "Artifact_Panel", "ArtifactContent", "ArtifactTabPanel");
            if (artPnl != null) artifactPanel = artPnl;
        }

        if (equippedChipCardTemplate == null && chipsetPanel != null)
        {
            Transform found = chipsetPanel.transform.Find("EquippedChipCard");
            if (found != null) equippedChipCardTemplate = found.gameObject;
        }

        LoadTabSpritesIfMissing();

        // Auto-wire sub-tab visuals if missing
        if (defSubTabButton != null)
        {
            if (defSubTabBg == null) defSubTabBg = defSubTabButton.GetComponent<Image>();
            if (defSubTabText == null) defSubTabText = defSubTabButton.GetComponentInChildren<TMP_Text>(true);
        }
        if (attackSubTabButton != null)
        {
            if (attackSubTabBg == null) attackSubTabBg = attackSubTabButton.GetComponent<Image>();
            if (attackSubTabText == null) attackSubTabText = attackSubTabButton.GetComponentInChildren<TMP_Text>(true);
        }
        if (otherSubTabButton != null)
        {
            if (otherSubTabBg == null) otherSubTabBg = otherSubTabButton.GetComponent<Image>();
            if (otherSubTabText == null) otherSubTabText = otherSubTabButton.GetComponentInChildren<TMP_Text>(true);
        }

        // Ensure StatsSubContainer is active
        Transform statsSub = searchRoot.Find("MainFrame/StatsPanel/StatsSubContainer")
                          ?? searchRoot.Find("StatsPanel/StatsSubContainer");
        if (statsSub != null && !statsSub.gameObject.activeSelf)
        {
            statsSub.gameObject.SetActive(true);
        }
        if (defStatsPanel != null && defStatsPanel.transform.parent != null && !defStatsPanel.transform.parent.gameObject.activeSelf)
        {
            defStatsPanel.transform.parent.gameObject.SetActive(true);
        }

        AlignTabPosition(statsOn, statsOff);
        AlignTabPosition(chipsetOn, chipsetOff);
        AlignTabPosition(artifactOn, artifactOff);

        AutoWireArtifactIconSlots();
    }

    public void AutoWireArtifactIconSlots()
    {
        if (artifactPanel == null) return;

        artifactIconSlots.RemoveAll(x => x == null);
        if (artifactIconSlots.Count > 0) return;

        // 1. Tìm container con nếu có (ví dụ "ArtifactIcons", "ArtifactSlots", "Icons", "Slots")
        Transform container = artifactPanel.transform.Find("ArtifactIcons")
                           ?? artifactPanel.transform.Find("ArtifactSlots")
                           ?? artifactPanel.transform.Find("Icons")
                           ?? artifactPanel.transform.Find("Slots");

        Transform searchParent = container != null ? container : artifactPanel.transform;

        for (int i = 0; i < searchParent.childCount; i++)
        {
            Transform child = searchParent.GetChild(i);
            if (child == null) continue;

            string childName = child.name;
            if (childName.Equals("ArtifactMessage", StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("ArtifactHint", StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("Viewport", StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("Scrollbar", StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("EmptyNotice", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (container != null ||
                childName.StartsWith("ArtifactIcon", StringComparison.OrdinalIgnoreCase) ||
                childName.StartsWith("ArtifactSlot", StringComparison.OrdinalIgnoreCase) ||
                childName.StartsWith("Slot", StringComparison.OrdinalIgnoreCase) ||
                childName.StartsWith("Icon", StringComparison.OrdinalIgnoreCase))
            {
                if (!artifactIconSlots.Contains(child.gameObject))
                {
                    artifactIconSlots.Add(child.gameObject);
                }
            }
        }
    }

    public void LoadTabSpritesIfMissing()
    {
        if (statsOnSprite == null) statsOnSprite = FindSpriteByName("StatsOn");
        if (statsOffSprite == null) statsOffSprite = FindSpriteByName("StatsOff");

        if (chipsetOnSprite == null) chipsetOnSprite = FindSpriteByName("ChipsetOn");
        if (chipsetOffSprite == null) chipsetOffSprite = FindSpriteByName("ChipsetOff");

        if (artifactOnSprite == null) artifactOnSprite = FindSpriteByName("ArtifactOn");
        if (artifactOffSprite == null) artifactOffSprite = FindSpriteByName("ArtifactOff");
    }

    private Sprite FindSpriteByName(string spriteName)
    {
#if UNITY_EDITOR
        string[] searchPaths = new string[]
        {
            "Assets/Sprites/UI/Pause/nút màn pause (1).png",
            "Assets/Sprites/UI/Pause/nut man pause (1).png"
        };
        foreach (string path in searchPaths)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null && assets.Length > 0)
            {
                foreach (var a in assets)
                {
                    if (a is Sprite s && s.name.Equals(spriteName, StringComparison.OrdinalIgnoreCase))
                    {
                        return s;
                    }
                }
            }
        }
#endif
        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in allSprites)
        {
            if (s != null && s.name.Equals(spriteName, StringComparison.OrdinalIgnoreCase))
            {
                return s;
            }
        }
        return null;
    }

    private GameObject WireIfNull(GameObject current, Transform searchRoot, params string[] names)
    {
        if (current != null) return current;
        GameObject found = FindTabObject(searchRoot, names);
        if (found == null && transform.parent != null)
        {
            found = FindTabObject(transform.parent, names);
        }
        return found;
    }

    public void AlignTabPosition(GameObject onObj, GameObject offObj)
    {
        if (onObj == null || offObj == null) return;
        RectTransform onRt = onObj.GetComponent<RectTransform>();
        RectTransform offRt = offObj.GetComponent<RectTransform>();
        if (onRt == null || offRt == null) return;

        if (onRt.parent == offRt.parent)
        {
            if (Mathf.Abs(offRt.anchoredPosition.y - onRt.anchoredPosition.y) > 2f ||
                Mathf.Abs(offRt.anchoredPosition.x - onRt.anchoredPosition.x) > 2f)
            {
                offRt.anchoredPosition = onRt.anchoredPosition;
                offRt.anchorMin = onRt.anchorMin;
                offRt.anchorMax = onRt.anchorMax;
                offRt.pivot = onRt.pivot;
                offRt.sizeDelta = onRt.sizeDelta;
            }
        }
    }

    public GameObject FindTabObject(Transform root, params string[] names)
    {
        if (root == null) return null;

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);

        // 1st pass: exact clean match
        foreach (Transform child in allChildren)
        {
            if (child == root) continue;

            string childNameClean = CleanName(child.name);
            foreach (string target in names)
            {
                string targetClean = CleanName(target);
                if (childNameClean.Equals(targetClean, StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }

            Image img = child.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                string spriteClean = CleanName(img.sprite.name);
                foreach (string target in names)
                {
                    string targetClean = CleanName(target);
                    if (spriteClean.Equals(targetClean, StringComparison.OrdinalIgnoreCase))
                    {
                        return child.gameObject;
                    }
                }
            }
        }

        // 2nd pass: contains match
        foreach (Transform child in allChildren)
        {
            if (child == root) continue;

            string childNameClean = CleanName(child.name);
            foreach (string target in names)
            {
                string targetClean = CleanName(target);
                if (childNameClean.IndexOf(targetClean, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child.gameObject;
                }
            }

            Image img = child.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                string spriteClean = CleanName(img.sprite.name);
                foreach (string target in names)
                {
                    string targetClean = CleanName(target);
                    if (spriteClean.IndexOf(targetClean, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return child.gameObject;
                    }
                }
            }
        }

        return null;
    }

    private static string CleanName(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        return str.Replace(" ", "").Replace("_", "").Replace("-", "").Replace("(", "").Replace(")", "");
    }

    private Button EnsureButton(GameObject go, UnityEngine.Events.UnityAction onClickAction)
    {
        if (go == null) return null;
        Button btn = go.GetComponent<Button>();
        if (btn == null)
        {
            btn = go.AddComponent<Button>();
        }
        Graphic g = go.GetComponent<Graphic>();
        if (g != null)
        {
            g.raycastTarget = true;
            if (btn.targetGraphic == null)
            {
                btn.targetGraphic = g;
            }
        }
        if (onClickAction != null)
        {
            btn.onClick.RemoveListener(onClickAction);
            btn.onClick.AddListener(onClickAction);
        }
        return btn;
    }

    // =========================================================================
    // EQUIPPED CHIPSET DISPLAY LOGIC (HIỂN THỊ ICON & KHUNG CHIPSET ĐÃ CHỌN)
    // =========================================================================

    private void HandleChipsetSelected(ChipItemData chip, int newLevel)
    {
        if (chip == null) return;
        Sprite icon = null;
        Sprite frame = null;
        if (ChipsetLevelUpPopup.EquippedRuntimeChips != null &&
            ChipsetLevelUpPopup.EquippedRuntimeChips.TryGetValue(chip.id, out var entry))
        {
            icon = entry.iconSprite;
            frame = entry.frameSprite;
        }
        if (icon == null) icon = GetChipsetIconSprite(chip.id, chip.iconKey);
        if (frame == null) frame = GetChipsetLeverFrame(chip.tier);

        RegisterOrUpdateRuntimeChip(chip.id, chip.chipName, chip.iconKey, newLevel, chip.tier, icon, frame);
        if (IsPaused && CurrentMainTab == 1)
        {
            RefreshEquippedChips();
        }
    }

    public void RegisterOrUpdateRuntimeChip(int id, string name, string iconKey, int level, ChipTier tier, Sprite iconSprite = null, Sprite frameSprite = null)
    {
        if (id <= 0) return;
        RuntimeEquippedChipData existing = runtimeEquippedChips.FirstOrDefault(c => c.id == id);
        if (existing != null)
        {
            existing.level = Mathf.Max(existing.level, level);
            if (!string.IsNullOrEmpty(name)) existing.name = name;
            if (!string.IsNullOrEmpty(iconKey)) existing.iconKey = iconKey;
            existing.tier = tier;
            if (iconSprite != null) existing.cachedIconSprite = iconSprite;
            if (frameSprite != null) existing.cachedFrameSprite = frameSprite;
        }
        else
        {
            runtimeEquippedChips.Add(new RuntimeEquippedChipData
            {
                id = id,
                name = string.IsNullOrEmpty(name) ? ChipsetBattleStats.GetChipsetName(id) : name,
                iconKey = string.IsNullOrEmpty(iconKey) ? ChipsetBattleStats.GetChipsetIconKey(id) : iconKey,
                level = Mathf.Max(1, level),
                tier = tier,
                cachedIconSprite = iconSprite,
                cachedFrameSprite = frameSprite
            });
        }
    }

    public void SyncRuntimeEquippedChips()
    {
        // 1. Đồng bộ từ ChipsetLevelUpPopup.EquippedRuntimeChips (chứa chính xác 100% icon và khung của thẻ level up đã chọn)
        var runtimeChips = ChipsetLevelUpPopup.EquippedRuntimeChips;
        if (runtimeChips != null && runtimeChips.Count > 0)
        {
            foreach (var kvp in runtimeChips)
            {
                var entry = kvp.Value;
                if (entry != null)
                {
                    RegisterOrUpdateRuntimeChip(
                        entry.id,
                        entry.name,
                        entry.iconKey,
                        entry.level,
                        entry.tier,
                        entry.iconSprite,
                        entry.frameSprite);
                }
            }
        }

        // 2. Luôn đảm bảo vũ khí mặc định (Standard Gun ID 1 hoặc súng đã trang bị)
        if (runtimeEquippedChips.Count == 0 || !runtimeEquippedChips.Any(c => c.id == 1))
        {
            Sprite defaultGunIcon = GetChipsetIconSprite(1, "standard-gun");
            Sprite defaultFrame = GetChipsetLeverFrame(ChipTier.Magic);
            RegisterOrUpdateRuntimeChip(1, "Standard Gun", "standard-gun", 1, ChipTier.Magic, defaultGunIcon, defaultFrame);
        }

        // 3. Đồng bộ từ ChipsetBattleStats (được cập nhật khi Player lên cấp và qua PlayerChipsetSkillManager)
        var battleEntries = ChipsetBattleStats.Entries;
        if (battleEntries != null)
        {
            for (int i = 0; i < battleEntries.Count; i++)
            {
                var entry = battleEntries[i];
                if (entry != null && entry.RuntimeLevel > 0)
                {
                    Sprite icon = GetChipsetIconSprite(entry.ChipsetId, entry.IconKey);
                    Sprite frame = GetChipsetLeverFrame(ChipTier.Magic);
                    RegisterOrUpdateRuntimeChip(entry.ChipsetId, entry.ChipsetName, entry.IconKey, entry.RuntimeLevel, ChipTier.Magic, icon, frame);
                }
            }
        }

        // 4. Đồng bộ từ ChipsetLevelUpPopup nếu có instance trong Scene (fallback runtimeChipLevels)
        ChipsetLevelUpPopup popup = FindObjectOfType<ChipsetLevelUpPopup>(true);
        if (popup != null && popup.RuntimeChipLevels != null)
        {
            foreach (var kvp in popup.RuntimeChipLevels)
            {
                int chipId = kvp.Key;
                int level = kvp.Value;
                if (level > 0)
                {
                    string iconKey = ChipsetBattleStats.GetChipsetIconKey(chipId);
                    Sprite icon = GetChipsetIconSprite(chipId, iconKey);
                    Sprite frame = GetChipsetLeverFrame(ChipTier.Magic);
                    RegisterOrUpdateRuntimeChip(chipId, ChipsetBattleStats.GetChipsetName(chipId), iconKey, level, ChipTier.Magic, icon, frame);
                }
            }
        }
    }

    public void RefreshEquippedChips()
    {
        if (chipsetPanel == null) return;

        SyncRuntimeEquippedChips();

        if (visualLibrary == null)
        {
            visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        }

        GameObject templateCard = FindChipCardTemplate();
        if (templateCard == null) return;

        templateCard.SetActive(runtimeEquippedChips.Count > 0);

        for (int i = 0; i < runtimeEquippedChips.Count; i++)
        {
            RuntimeEquippedChipData chip = runtimeEquippedChips[i];
            GameObject cardObj;

            if (i == 0)
            {
                cardObj = templateCard;
            }
            else
            {
                int poolIndex = i - 1;
                if (poolIndex < spawnedChipCards.Count && spawnedChipCards[poolIndex] != null)
                {
                    cardObj = spawnedChipCards[poolIndex];
                }
                else
                {
                    cardObj = Instantiate(templateCard, chipsetPanel.transform);
                    cardObj.name = $"EquippedChipCard_{i}";
                    if (poolIndex < spawnedChipCards.Count)
                    {
                        spawnedChipCards[poolIndex] = cardObj;
                    }
                    else
                    {
                        spawnedChipCards.Add(cardObj);
                    }
                }
            }

            cardObj.SetActive(true);

            // Căn vị trí hàng ngang: 5 khung chipset mỗi hàng, khoảng cách 160px.
            int col = i % 5;
            int row = i / 5;
            float posX = -320f + (col * 160f);
            float posY = 360f - (row * 220f);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.sizeDelta = new Vector2(150f, 190f);
            }

            UpdateChipCardVisual(cardObj, chip);
        }

        // Ẩn các card thừa trong pool
        for (int i = runtimeEquippedChips.Count - 1; i < spawnedChipCards.Count; i++)
        {
            if (i >= 0 && i < spawnedChipCards.Count && spawnedChipCards[i] != null)
            {
                spawnedChipCards[i].SetActive(false);
            }
        }
    }

    private void UpdateChipCardVisual(GameObject cardObj, RuntimeEquippedChipData chip)
    {
        if (cardObj == null || chip == null) return;

        // Card chỉ là container chỉnh vị trí. Bỏ khung chữ nhật lớn cũ để trong
        // Hierarchy mỗi slot chính là khung chipset lấy thẳng từ Level Up.
        Image legacyCardImage = cardObj.GetComponent<Image>();
        if (legacyCardImage != null) legacyCardImage.enabled = false;
        Transform legacyFill = cardObj.transform.Find("Fill");
        if (legacyFill != null)
        {
            Image fillImage = legacyFill.GetComponent<Image>();
            if (fillImage != null) fillImage.enabled = false;
        }

        // 1. Khung chipset dùng đúng sprite và cấu trúc của Level Up.
        Transform iconFrameTr = cardObj.transform.Find("IconFrameAssetSlot")
            ?? cardObj.transform.Find("IconFrame")
            ?? cardObj.transform.Find("ChipsetFrame");
        Image iconFrameImg = null;
        bool configureFrameLayout = false;
        if (iconFrameTr == null)
        {
            GameObject frameGo = new GameObject("IconFrameAssetSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameGo.transform.SetParent(cardObj.transform, false);
            iconFrameTr = frameGo.transform;
            iconFrameImg = frameGo.GetComponent<Image>();
            iconFrameTr.SetSiblingIndex(Mathf.Min(1, cardObj.transform.childCount - 1));
            configureFrameLayout = true;
        }
        else
        {
            iconFrameImg = iconFrameTr.GetComponent<Image>();
            if (!string.Equals(iconFrameTr.name, "IconFrameAssetSlot", StringComparison.Ordinal))
            {
                iconFrameTr.name = "IconFrameAssetSlot";
                configureFrameLayout = true;
            }
        }

        RectTransform frameRt = iconFrameTr.GetComponent<RectTransform>();
        if (frameRt != null && configureFrameLayout)
        {
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(120f, 150f);
        }

        Sprite leverFrameSprite = chip.cachedFrameSprite ?? GetChipsetLeverFrame(chip.tier);
        if (iconFrameImg != null)
        {
            iconFrameImg.sprite = leverFrameSprite;
            iconFrameImg.preserveAspect = true;
            iconFrameImg.color = Color.white;
            iconFrameImg.enabled = leverFrameSprite != null;
        }

        // 2. Icon Chipset: Image with weapon/skill icon
        Transform iconTr = iconFrameTr.Find("ChipIcon")
            ?? iconFrameTr.Find("GunIcon")
            ?? iconFrameTr.Find("Icon")
            ?? iconFrameTr.Find("ChipsetIcon")
            ?? cardObj.transform.Find("GunIcon")
            ?? cardObj.transform.Find("Icon")
            ?? cardObj.transform.Find("ChipsetIcon");

        Image iconImg = null;
        if (iconTr == null)
        {
            GameObject iconGo = new GameObject("ChipIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(iconFrameTr, false);
            iconTr = iconGo.transform;
            iconImg = iconGo.GetComponent<Image>();
        }
        else
        {
            iconImg = iconTr.GetComponent<Image>();
            if (iconTr.parent != iconFrameTr)
            {
                iconTr.SetParent(iconFrameTr, false);
            }
            if (!string.Equals(iconTr.name, "ChipIcon", StringComparison.Ordinal))
            {
                iconTr.name = "ChipIcon";
            }
        }

        RectTransform iconRt = iconTr.GetComponent<RectTransform>();
        if (iconRt != null)
        {
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            // Chuẩn hóa theo Transform thiết kế trong Inspector (media_1788528507150.png):
            // Pos X = -0.5, Pos Y = 20.1, Pos Z = 0, Width = 93.6, Height = 72, Scale = (1, 1, 1)
            iconRt.anchoredPosition = new Vector2(-0.5f, 20.1f);
            iconRt.sizeDelta = new Vector2(93.6f, 72f);
            iconRt.localScale = Vector3.one;
            iconRt.localEulerAngles = Vector3.zero;
        }

        Sprite chipIconSprite = chip.cachedIconSprite ?? GetChipsetIconSprite(chip.id, chip.iconKey);
        if (iconImg != null)
        {
            iconImg.sprite = chipIconSprite;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.enabled = chipIconSprite != null;
        }

        // 3. Dùng đúng năm sprite cấp của Level Up, đặt thành child của khung để
        // người thiết kế có thể chỉnh trực tiếp từng pip trong Hierarchy.
        Sprite[] levelPipSprites = visualLibrary != null ? visualLibrary.levelPipSprites : null;
        for (int i = 0; i < 5; i++)
        {
            string pipName = $"RuntimeLevelPip_{i + 1}";
            Transform pipTr = iconFrameTr.Find(pipName);
            Image pipImage;
            if (pipTr == null)
            {
                GameObject pipGo = new GameObject(pipName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pipGo.transform.SetParent(iconFrameTr, false);
                pipTr = pipGo.transform;
                pipImage = pipGo.GetComponent<Image>();

                RectTransform pipRt = pipGo.GetComponent<RectTransform>();
                pipRt.anchorMin = pipRt.anchorMax = new Vector2(0.5f, 0f);
                pipRt.pivot = new Vector2(0.5f, 0f);
                float normalizedCenter = Mathf.Lerp(0.174f, 0.858f, i / 4f);
                pipRt.anchoredPosition = new Vector2((normalizedCenter - 0.5f) * 120f, 32.4f);
                pipRt.sizeDelta = new Vector2(18.6f, 20.7f);
                pipRt.localScale = Vector3.one;
            }
            else
            {
                pipImage = pipTr.GetComponent<Image>();
            }

            if (pipImage == null) continue;
            pipImage.sprite = levelPipSprites != null && i < levelPipSprites.Length ? levelPipSprites[i] : null;
            pipImage.preserveAspect = true;
            pipImage.raycastTarget = false;
            pipImage.color = Color.white;
            pipImage.enabled = i < Mathf.Clamp(chip.level, 0, 5) && pipImage.sprite != null;
        }

        // Badge chữ cũ được giữ lại nhưng ẩn để không phá reference trong scene.
        Transform badgeTr = cardObj.transform.Find("LvlBadge") ?? cardObj.transform.Find("Badge");
        if (badgeTr != null)
        {
            badgeTr.gameObject.SetActive(false);
        }
    }

    private GameObject FindChipCardTemplate()
    {
        if (equippedChipCardTemplate != null) return equippedChipCardTemplate;

        if (chipsetPanel != null)
        {
            Transform found = chipsetPanel.transform.Find("EquippedChipCard");
            if (found != null)
            {
                equippedChipCardTemplate = found.gameObject;
                return equippedChipCardTemplate;
            }

            Transform[] allChildren = chipsetPanel.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t != null && t != chipsetPanel.transform && t.name.StartsWith("EquippedChipCard", StringComparison.OrdinalIgnoreCase))
                {
                    equippedChipCardTemplate = t.gameObject;
                    return equippedChipCardTemplate;
                }
            }
        }

        return null;
    }

    public Sprite GetChipsetLeverFrame(ChipTier tier)
    {
        Sprite[] availableFrames = visualLibrary != null && visualLibrary.tierLeverFrames != null && visualLibrary.tierLeverFrames.Length > 0
            ? visualLibrary.tierLeverFrames
            : null;
        if (availableFrames != null)
        {
            return ChipsetLevelUpPopup.ResolveLeverFrameForTier(availableFrames, tier);
        }

        if (visualLibrary == null)
        {
            visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
            if (visualLibrary != null && visualLibrary.tierLeverFrames != null && visualLibrary.tierLeverFrames.Length > 0)
            {
                return ChipsetLevelUpPopup.ResolveLeverFrameForTier(visualLibrary.tierLeverFrames, tier);
            }
        }

#if UNITY_EDITOR
        string framePath = "Assets/Sprites/UI/Chipset/khung chipset (1).png";
        var frames = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(framePath).OfType<Sprite>().ToArray();
        if (frames.Length > 0)
        {
            return ChipsetLevelUpPopup.ResolveLeverFrameForTier(frames, tier);
        }
#endif
        return null;
    }

    public Sprite GetChipsetIconSprite(int id, string iconKey)
    {
        if (visualLibrary == null)
        {
            visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        }

        Sprite[] availableIcons = visualLibrary != null && visualLibrary.primaryChipIcons != null && visualLibrary.primaryChipIcons.Length > 0
            ? visualLibrary.primaryChipIcons
            : null;

        if (availableIcons != null)
        {
            Sprite found = ChipsetLevelUpPopup.FindMatchingIcon(availableIcons, id, iconKey);
            if (found != null) return found;
        }

#if UNITY_EDITOR
        string iconAtlasPath = "Assets/Sprites/UI/Chipset/icon chipset.png";
        var icons = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(iconAtlasPath).OfType<Sprite>().ToArray();
        if (icons.Length > 0)
        {
            Sprite found = ChipsetLevelUpPopup.FindMatchingIcon(icons, id, iconKey);
            if (found != null) return found;
        }
#endif
        // Fallback cho ID 1: Standard Gun
        if (id == 1 || (iconKey != null && (iconKey.Contains("standard") || iconKey.Contains("gun"))))
        {
            Sprite gunSprite = Resources.Load<Sprite>("Gun")
#if UNITY_EDITOR
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Gun.png")
#endif
                ;
            if (gunSprite != null) return gunSprite;
        }

        return null;
    }

    public void ResetRuntimeEquippedChipsForTesting()
    {
        runtimeEquippedChips.Clear();
        foreach (var c in spawnedChipCards)
        {
            if (c != null) DestroyImmediate(c);
        }
        spawnedChipCards.Clear();
    }

    public void SetChipsetCardTemplateForTesting(GameObject template, ChipsetLevelVisualLibrary lib = null)
    {
        equippedChipCardTemplate = template;
        visualLibrary = lib;
    }

    private readonly List<GameObject> spawnedArtifactCards = new List<GameObject>();

    /// <summary>
    /// Hiển thị danh sách Cổ Vật (Artifact) đã nhặt trong run khi người chơi mở tab ARTIFACT ở Pause Menu.
    /// Ưu tiên hiển thị trên các ô icon Artifact đã được xếp và chỉnh kích thước trong Edit Mode,
    /// bảo toàn 100% vị trí (AnchoredPosition) và kích thước (SizeDelta) đã thiết kế.
    /// </summary>
    public void RefreshEquippedArtifacts()
    {
        if (artifactPanel == null) return;

        AutoWireArtifactIconSlots();

        Transform msgTr = artifactPanel.transform.Find("ArtifactMessage")
            ?? artifactPanel.transform.Find("ArtifactHint");

        bool hasArtifacts = PlayerArtifactInventory.Instance != null && PlayerArtifactInventory.Instance.EquippedArtifacts.Count > 0;
        var equippedArtifacts = PlayerArtifactInventory.Instance != null ? PlayerArtifactInventory.Instance.EquippedArtifacts : null;

        // Trường hợp 1: Có các ô ArtifactIcon trong Hierarchy (được xếp trong Edit Mode)
        if (artifactIconSlots != null && artifactIconSlots.Count > 0)
        {
            // Trong Play Mode: chỉ hiện thông báo hướng dẫn khi chưa có artifact nào
            if (msgTr != null)
            {
                if (Application.isPlaying)
                {
                    msgTr.gameObject.SetActive(!hasArtifacts);
                }
                else
                {
                    // Trong Edit Mode, ẩn message để không che khuất các icon đang chỉnh
                    msgTr.gameObject.SetActive(false);
                }
            }

            // Nếu số lượng artifact nhặt được nhiều hơn số slot dựng sẵn trong Hierarchy,
            // tự động nhân bản (spawn) thêm các slot tiếp theo theo lưới 4 cột / hàng
            int totalSlotsNeeded = hasArtifacts && equippedArtifacts != null ? equippedArtifacts.Count : artifactIconSlots.Count;
            while (artifactIconSlots.Count < totalSlotsNeeded && artifactPanel != null)
            {
                GameObject templateSlot = artifactIconSlots[0];
                GameObject newSlot = Instantiate(templateSlot, artifactPanel.transform);
                newSlot.name = $"ArtifactIcon_{artifactIconSlots.Count + 1}";

                int newIdx = artifactIconSlots.Count;
                int col = newIdx % 4;
                int row = newIdx / 4;
                RectTransform rt = newSlot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-307.5f + (col * 205f), 360f - (row * 200f));
                    rt.sizeDelta = new Vector2(150f, 175f);
                }
                artifactIconSlots.Add(newSlot);
            }

            for (int i = 0; i < artifactIconSlots.Count; i++)
            {
                GameObject slot = artifactIconSlots[i];
                if (slot == null) continue;

                // Trong Edit Mode: Luôn bật toàn bộ slot và gán preview để người dùng có thể căn chỉnh vị trí & kích thước
                if (!Application.isPlaying)
                {
                    slot.SetActive(true);
                    UpdateArtifactSlotVisualInEditor(slot, i);
                    WireSlotButtonClick(slot, i, null);
                    continue;
                }

                // Trong Play Mode:
                if (hasArtifacts && equippedArtifacts != null && i < equippedArtifacts.Count)
                {
                    ArtifactData art = equippedArtifacts[i];
                    slot.SetActive(true);
                    UpdateArtifactSlotVisual(slot, art);
                    WireSlotButtonClick(slot, i, art);
                }
                else
                {
                    if (hideUnusedArtifactSlots)
                    {
                        slot.SetActive(false);
                    }
                    else
                    {
                        slot.SetActive(true);
                        ClearArtifactSlotVisual(slot);
                    }
                }
            }
            return;
        }

        // Trường hợp 2: Fallback cho scene/test cũ không có sẵn icon slots (giữ tương thích với Test08)
        foreach (var c in spawnedArtifactCards)
        {
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
        spawnedArtifactCards.Clear();

        if (msgTr != null)
        {
            msgTr.gameObject.SetActive(!hasArtifacts);
        }

        Transform viewportTr = artifactPanel.transform.Find("Viewport");
        Transform contentTr = null;
        if (viewportTr != null)
        {
            contentTr = viewportTr.Find("Content");
        }
        else
        {
            contentTr = artifactPanel.transform.Find("Content");
        }

        if (contentTr == null)
        {
            ScrollRect scroll = artifactPanel.GetComponent<ScrollRect>() ?? artifactPanel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpObj.transform.SetParent(artifactPanel.transform, false);
            RectTransform vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(20f, 20f);
            vpRt.offsetMax = new Vector2(-20f, -20f);

            GameObject cntObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cntObj.transform.SetParent(vpObj.transform, false);
            RectTransform cntRt = cntObj.GetComponent<RectTransform>();
            cntRt.anchorMin = new Vector2(0f, 1f);
            cntRt.anchorMax = new Vector2(1f, 1f);
            cntRt.pivot = new Vector2(0.5f, 1f);
            cntRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = cntObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = cntObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = cntRt;

            contentTr = cntObj.transform;
        }

        if (!hasArtifacts)
        {
            if (msgTr == null)
            {
                GameObject emptyObj = new GameObject("EmptyNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
                emptyObj.transform.SetParent(contentTr, false);
                RectTransform ert = emptyObj.GetComponent<RectTransform>();
                ert.sizeDelta = new Vector2(600f, 100f);
                TextMeshProUGUI etxt = emptyObj.GetComponent<TextMeshProUGUI>();
                etxt.text = "You can get it from a\nshiny box in the field.";
                etxt.fontSize = 32f;
                etxt.alignment = TextAlignmentOptions.Center;
                etxt.color = new Color(1f, 1f, 1f, 0.6f);
                spawnedArtifactCards.Add(emptyObj);
            }
            return;
        }

        foreach (var art in PlayerArtifactInventory.Instance.EquippedArtifacts)
        {
            if (art == null) continue;

            GameObject card = new GameObject($"ArtifactCard_{art.id}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(contentTr, false);
            RectTransform crt = card.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(860f, 130f);

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color32(14, 48, 68, 235);

            GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
            iconFrame.transform.SetParent(card.transform, false);
            RectTransform ifrt = iconFrame.GetComponent<RectTransform>();
            ifrt.anchorMin = new Vector2(0f, 0.5f);
            ifrt.anchorMax = new Vector2(0f, 0.5f);
            ifrt.pivot = new Vector2(0f, 0.5f);
            ifrt.anchoredPosition = new Vector2(20f, 0f);
            ifrt.sizeDelta = new Vector2(96f, 96f);
            Image ifImg = iconFrame.GetComponent<Image>();
            ifImg.color = art.badgeBorderColor.a > 0.1f ? art.badgeBorderColor : new Color32(46, 229, 240, 255);

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(iconFrame.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(72f, 72f);
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

            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(card.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(0f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(135f, -14f);
            nameRt.sizeDelta = new Vector2(450f, 38f);
            TextMeshProUGUI nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
            nameTxt.text = art.artifactName;
            nameTxt.fontSize = 30f;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color = new Color32(255, 184, 28, 255);

            GameObject loreObj = new GameObject("Lore", typeof(RectTransform), typeof(TextMeshProUGUI));
            loreObj.transform.SetParent(card.transform, false);
            RectTransform loreRt = loreObj.GetComponent<RectTransform>();
            loreRt.anchorMin = new Vector2(0f, 0f);
            loreRt.anchorMax = new Vector2(0f, 0f);
            loreRt.pivot = new Vector2(0f, 0f);
            loreRt.anchoredPosition = new Vector2(135f, 14f);
            loreRt.sizeDelta = new Vector2(450f, 55f);
            TextMeshProUGUI loreTxt = loreObj.GetComponent<TextMeshProUGUI>();
            loreTxt.text = art.loreDescription;
            loreTxt.fontSize = 20f;
            loreTxt.color = new Color32(210, 210, 210, 255);

            GameObject statObj = new GameObject("Stat", typeof(RectTransform), typeof(TextMeshProUGUI));
            statObj.transform.SetParent(card.transform, false);
            RectTransform statRt = statObj.GetComponent<RectTransform>();
            statRt.anchorMin = new Vector2(1f, 0.5f);
            statRt.anchorMax = new Vector2(1f, 0.5f);
            statRt.pivot = new Vector2(1f, 0.5f);
            statRt.anchoredPosition = new Vector2(-25f, 0f);
            statRt.sizeDelta = new Vector2(250f, 60f);
            TextMeshProUGUI statTxt = statObj.GetComponent<TextMeshProUGUI>();
            statTxt.text = art.GetFormattedStatText();
            statTxt.fontSize = 28f;
            statTxt.fontStyle = FontStyles.Bold;
            statTxt.alignment = TextAlignmentOptions.Right;
            Button cardBtn = card.GetComponent<Button>() ?? card.AddComponent<Button>();
            ArtifactData currentArt = art;
            cardBtn.onClick.RemoveAllListeners();
            cardBtn.onClick.AddListener(() => ShowArtifactDetail(currentArt));

            spawnedArtifactCards.Add(card);
        }
    }

    private void UpdateArtifactSlotVisual(GameObject slot, ArtifactData art)
    {
        if (slot == null || art == null) return;

        Image iconImg = GetSlotImage(slot);
        if (iconImg != null)
        {
            iconImg.enabled = true;
            if (art.icon != null)
            {
                iconImg.sprite = art.icon;
                iconImg.color = Color.white;
            }
            else
            {
                Sprite fallback = GetArtifactFallbackSprite(art.statType);
                if (fallback != null)
                {
                    iconImg.sprite = fallback;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = art.badgeBorderColor.a > 0.1f ? art.badgeBorderColor : new Color32(46, 229, 240, 255);
                }
            }
            iconImg.preserveAspect = true;
        }

        TMP_Text nameTxt = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameTxt != null) nameTxt.text = art.artifactName;

        TMP_Text statTxt = slot.transform.Find("Stat")?.GetComponent<TMP_Text>();
        if (statTxt != null) statTxt.text = art.GetFormattedStatText();
    }

    private void UpdateArtifactSlotVisualInEditor(GameObject slot, int index)
    {
        if (slot == null) return;
        int maxSlots = ArtifactDatabase.Instance != null && ArtifactDatabase.Instance.artifacts != null && ArtifactDatabase.Instance.artifacts.Count > 0
            ? ArtifactDatabase.Instance.artifacts.Count
            : 7;

        if (index >= maxSlots)
        {
            slot.SetActive(false);
            return;
        }

        Image iconImg = GetSlotImage(slot);
        if (iconImg != null)
        {
            iconImg.enabled = true;
            Sprite fallback = GetArtifactFallbackByIndex(index);
            if (fallback != null)
            {
                iconImg.sprite = fallback;
            }
            if (iconImg.color.a < 0.1f)
            {
                iconImg.color = Color.white;
            }
            iconImg.preserveAspect = true;
        }
    }

    private void ClearArtifactSlotVisual(GameObject slot)
    {
        if (slot == null) return;
        Image iconImg = GetSlotImage(slot);
        if (iconImg != null)
        {
            iconImg.color = new Color(1f, 1f, 1f, 0.25f);
        }
    }

    private Image GetSlotImage(GameObject slot)
    {
        if (slot == null) return null;
        Transform iconChild = slot.transform.Find("Icon")
                           ?? slot.transform.Find("ArtifactIcon")
                           ?? slot.transform.Find("Image");
        if (iconChild != null)
        {
            Image img = iconChild.GetComponent<Image>();
            if (img != null) return img;
        }
        return slot.GetComponent<Image>() ?? slot.GetComponentInChildren<Image>(true);
    }

    private Sprite GetArtifactFallbackSprite(ArtifactStatType statType)
    {
        if (ArtifactDatabase.Instance != null && ArtifactDatabase.Instance.artifacts != null)
        {
            var matched = ArtifactDatabase.Instance.artifacts.Find(a => a != null && a.statType == statType);
            if (matched != null && matched.icon != null)
            {
                return matched.icon;
            }
        }
        return GetArtifactFallbackByIndex(0);
    }

    private Sprite GetArtifactFallbackByIndex(int index)
    {
        if (ArtifactDatabase.Instance != null && ArtifactDatabase.Instance.artifacts != null && ArtifactDatabase.Instance.artifacts.Count > 0)
        {
            int safeIdx = Mathf.Clamp(index, 0, ArtifactDatabase.Instance.artifacts.Count - 1);
            if (ArtifactDatabase.Instance.artifacts[safeIdx] != null && ArtifactDatabase.Instance.artifacts[safeIdx].icon != null)
            {
                return ArtifactDatabase.Instance.artifacts[safeIdx].icon;
            }
        }

        string[] names = new string[]
        {
            "Artifact_CD",
            "Artifact_Lego",
            "Artifact_USB",
            "Artifact_Butter",
            "Artifact_Cooler",
            "Artifact_Rocket",
            "Artifact_Chip"
        };
        int idx = Mathf.Clamp(index, 0, names.Length - 1);
        return LoadArtifactSpriteFromSheet(names[idx]);
    }

    private Sprite LoadArtifactSpriteFromSheet(string spriteName)
    {
#if UNITY_EDITOR
        string path = "Assets/Sprites/UI/nút artifact.png";
        var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in sprites)
        {
            if (obj is Sprite s && string.Equals(s.name, spriteName, StringComparison.OrdinalIgnoreCase))
            {
                return s;
            }
        }
#endif
        return null;
    }

    private Sprite LoadArtifactSprite(string spriteName)
    {
        return LoadArtifactSpriteFromSheet(spriteName);
    }

    public void SetArtifactIconSlotsForTesting(params GameObject[] slots)
    {
        artifactIconSlots.Clear();
        if (slots != null)
        {
            artifactIconSlots.AddRange(slots);
        }
    }

    public void AutoWireArtifactDetailDialog()
    {
        if (artifactDetailPanel == null && modalRoot != null)
        {
            Transform found = modalRoot.transform.Find("ArtifactDetailDialog")
                           ?? modalRoot.transform.Find("ArtifactDetailPopup")
                           ?? modalRoot.transform.Find("ArtifactDetailPanel")
                           ?? modalRoot.transform.Find("DetailDialog");
            if (found == null && transform.parent != null)
            {
                found = transform.parent.Find("ArtifactDetailDialog")
                     ?? transform.parent.Find("ArtifactDetailPopup");
            }
            if (found != null)
            {
                artifactDetailPanel = found.gameObject;
            }
        }

        if (artifactDetailPanel != null)
        {
            if (artifactDetailOkButton == null)
            {
                artifactDetailOkButton = artifactDetailPanel.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (artifactDetailIcon == null)
            {
                Transform iconTr = artifactDetailPanel.transform.Find("DetailCard/IconFrame/Icon")
                                ?? artifactDetailPanel.transform.Find("Card/IconFrame/Icon")
                                ?? artifactDetailPanel.transform.Find("IconFrame/Icon")
                                ?? artifactDetailPanel.transform.Find("Icon");
                if (iconTr != null)
                {
                    artifactDetailIcon = iconTr.GetComponent<Image>();
                }
                else
                {
                    artifactDetailIcon = artifactDetailPanel.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(img => img.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                               img.name.IndexOf("frame", StringComparison.OrdinalIgnoreCase) < 0 &&
                                               img.name.IndexOf("bg", StringComparison.OrdinalIgnoreCase) < 0 &&
                                               img.name.IndexOf("card", StringComparison.OrdinalIgnoreCase) < 0);
                }
            }

            if (artifactDetailNameText == null)
            {
                artifactDetailNameText = artifactDetailPanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.name.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         t.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (artifactDetailLoreText == null)
            {
                artifactDetailLoreText = artifactDetailPanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.name.IndexOf("lore", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         t.name.IndexOf("desc", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (artifactDetailStatText == null)
            {
                artifactDetailStatText = artifactDetailPanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.name.IndexOf("stat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         t.name.IndexOf("buff", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (artifactDetailOkButton != null)
            {
                artifactDetailOkButton.onClick.RemoveListener(HideArtifactDetail);
                artifactDetailOkButton.onClick.AddListener(HideArtifactDetail);
            }
        }
    }

    public void ShowArtifactDetail(ArtifactData data)
    {
        if (data == null) return;
        AutoWireArtifactDetailDialog();

        if (artifactDetailIcon != null)
        {
            artifactDetailIcon.enabled = true;
            Sprite spr = data.icon;
            if (spr == null)
            {
                spr = GetArtifactFallbackSprite(data.statType);
            }
            artifactDetailIcon.sprite = spr;
            artifactDetailIcon.preserveAspect = true;
            artifactDetailIcon.color = Color.white;
        }

        if (artifactDetailNameText != null)
        {
            artifactDetailNameText.text = data.artifactName;
            artifactDetailNameText.color = new Color32(255, 184, 28, 255);
        }

        if (artifactDetailLoreText != null)
        {
            artifactDetailLoreText.text = data.loreDescription;
        }

        if (artifactDetailStatText != null)
        {
            artifactDetailStatText.text = data.GetFormattedStatText();
        }

        if (artifactDetailPanel != null)
        {
            artifactDetailPanel.SetActive(true);
        }
    }

    public void HideArtifactDetail()
    {
        if (artifactDetailPanel != null)
        {
            artifactDetailPanel.SetActive(false);
        }
    }

    public void ArrangeArtifactSlotsGrid(int columns = 4)
    {
        AutoWireArtifactIconSlots();
        if (artifactIconSlots == null || artifactIconSlots.Count == 0) return;
        if (columns <= 0) columns = 4;

        float spacingX = 205f;
        float spacingY = 200f;
        float startX = -((columns - 1) * 0.5f) * spacingX;
        float startY = 360f;

        for (int i = 0; i < artifactIconSlots.Count; i++)
        {
            GameObject slot = artifactIconSlots[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt != null)
            {
                int col = i % columns;
                int row = i / columns;
                float posX = startX + (col * spacingX);
                float posY = startY - (row * spacingY);

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(posX, posY);
            }
        }
    }

    public void WireSlotButtonClick(GameObject slot, int index, ArtifactData art)
    {
        if (slot == null) return;
        Button btn = slot.GetComponent<Button>();
        if (btn == null)
        {
            btn = slot.AddComponent<Button>();
        }
        Graphic g = slot.GetComponent<Graphic>() ?? slot.GetComponentInChildren<Graphic>(true);
        if (g != null)
        {
            g.raycastTarget = true;
            if (btn.targetGraphic == null) btn.targetGraphic = g;
        }

        btn.onClick.RemoveAllListeners();
        if (art != null)
        {
            ArtifactData clickedArt = art;
            btn.onClick.AddListener(() => ShowArtifactDetail(clickedArt));
        }
        else
        {
            int slotIdx = index;
            btn.onClick.AddListener(() =>
            {
                ArtifactData previewArt = CreatePreviewArtifactData(slotIdx);
                ShowArtifactDetail(previewArt);
            });
        }
    }

    public ArtifactData CreatePreviewArtifactData(int index)
    {
        ArtifactData temp = ScriptableObject.CreateInstance<ArtifactData>();
        string[] names = { "Spare Battery", "Carbon Scales", "Strong Cooler", "Kung Fu Data USB", "Titanium Fabric", "Artifact Slot 6", "Artifact Slot 7" };
        string[] lores = {
            "Eco-friendly product you can recharge.",
            "Vinyl 1, it likes me.\nVinyl 2, it doesn't like me.",
            "Cools down Turrets when they overheat.",
            "Does it actually have the Epic tome of Kung Fu in it?",
            "Sturdy titanium. Covers the body.",
            "Mysterious high-tech component found in the ruins.",
            "Ancient core radiating pure elemental energy."
        };
        ArtifactStatType[] types = {
            ArtifactStatType.MaxHealthPercent,
            ArtifactStatType.RangedDefensePercent,
            ArtifactStatType.TurretAttackSpeedPercent,
            ArtifactStatType.AllWeaponsDamagePercent,
            ArtifactStatType.DamageReduction,
            ArtifactStatType.CritRatePercent,
            ArtifactStatType.MoveSpeedPercent
        };
        float[] vals = { 15f, 10f, 20f, 9f, 10f, 12f, 10f };

        int idx = Mathf.Clamp(index, 0, names.Length - 1);
        temp.id = $"preview_{idx}";
        temp.artifactName = names[idx];
        temp.loreDescription = lores[idx];
        temp.statType = types[idx];
        temp.statValue = vals[idx];
        temp.icon = GetArtifactFallbackByIndex(index);
        return temp;
    }

    public void SetArtifactDetailDialogForTesting(
        GameObject panel,
        Image icon,
        TMP_Text nameTxt,
        TMP_Text loreTxt,
        TMP_Text statTxt,
        Button okBtn)
    {
        artifactDetailPanel = panel;
        artifactDetailIcon = icon;
        artifactDetailNameText = nameTxt;
        artifactDetailLoreText = loreTxt;
        artifactDetailStatText = statTxt;
        artifactDetailOkButton = okBtn;
        if (artifactDetailOkButton != null)
        {
            artifactDetailOkButton.onClick.RemoveListener(HideArtifactDetail);
            artifactDetailOkButton.onClick.AddListener(HideArtifactDetail);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Chuyển card Pause cũ thành hierarchy khung chipset có thể chỉnh trực tiếp
    /// trong Scene. Không lưu scene tự động để không ghi đè công việc đang mở.
    /// </summary>
    public bool PrepareEditableChipsetTemplateInEditor()
    {
        if (Application.isPlaying) return false;

        AutoWireTabButtonsAndSettings();
        if (visualLibrary == null)
        {
            visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        }

        GameObject template = FindChipCardTemplate();
        if (template == null) return false;

        Sprite icon = GetChipsetIconSprite(1, "standard-gun");
        Sprite frame = GetChipsetLeverFrame(ChipTier.Magic);
        var previewChip = new RuntimeEquippedChipData
        {
            id = 1,
            name = "Standard Gun",
            iconKey = "standard-gun",
            level = 1,
            tier = ChipTier.Magic,
            cachedIconSprite = icon,
            cachedFrameSprite = frame
        };

        UpdateChipCardVisual(template, previewChip);
        UnityEditor.EditorUtility.SetDirty(template);
        UnityEditor.EditorUtility.SetDirty(this);
        return true;
    }
#endif
}
