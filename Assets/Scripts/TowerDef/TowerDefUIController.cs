using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện người dùng (UI) trong chế độ Tower Def:
/// - Thanh trạng thái đỉnh màn hình: Nút Back, Số lượng Gold (🪙 50), Số lượng Energy (⚡ 0).
/// - Popup Xây dựng công trình (Build Modal).
/// - Popup Nâng cấp công trình / Cổng (Upgrade Modal).
/// - Thông báo nổi (Floating Text) khi thu hoạch vàng / năng lượng.
/// - Màn hình Thắng (Victory) & Thua (Defeat).
/// </summary>
public class TowerDefUIController : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text energyText;

    [Header("Build Popup")]
    [SerializeField] private GameObject buildModalRoot;
    [SerializeField] private Button buildTurretButton;
    [SerializeField] private Button buildGeneratorButton;
    [SerializeField] private Button closeBuildModalButton;

    [Header("Upgrade Popup")]
    [SerializeField] private GameObject upgradeModalRoot;
    [SerializeField] private TMP_Text upgradeTitleText;
    [SerializeField] private TMP_Text upgradeDescText;
    [SerializeField] private Button upgradeConfirmButton;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Button closeUpgradeModalButton;

    [Header("Gate Modal")]
    [SerializeField] private GameObject gateModalRoot;
    [SerializeField] private TMP_Text gateHpText;
    [SerializeField] private Button repairGateButton;
    [SerializeField] private Button upgradeGateButton;
    [SerializeField] private TMP_Text upgradeGateCostText;
    [SerializeField] private Button closeGateModalButton;

    [Header("Gate Upgrade Visuals (Image 3 Mockup)")]
    [SerializeField] private Sprite frameUpgradePopupSprite;
    [SerializeField] private Sprite panelUpgradeRowBarSprite;
    [SerializeField] private Sprite btnUpgradeGreySprite;
    [SerializeField] private Sprite btnUpgradeCyanSprite;
    [SerializeField] private Sprite btnUpgradeAdsSprite;
    [SerializeField] private Sprite coinIconSprite;
    [SerializeField] private Sprite[] gateLevelSprites;
    [SerializeField] private Sprite[] turretBaseLevelSprites;
    [SerializeField] private Sprite[] turretGunLevelSprites;
    [SerializeField] private TMP_FontAsset uiFont;

    [SerializeField] private Sprite[] bedLevelSprites;
    private bool hasUsedFreeAdUpgrade = false;
    private bool hasUsedFreeBedAdUpgrade = false;
    private Image gatePreviewImage1;
    private TextMeshProUGUI gateLevelText1;
    private TextMeshProUGUI row1HpText;
    private TextMeshProUGUI row1CostText;
    private Button goldUpgradeButton;

    private Image gatePreviewImage2;
    private TextMeshProUGUI gateLevelText2;
    private TextMeshProUGUI row2DescText;
    private Button adsUpgradeButton;
    private Image adsUpgradeBtnImg;

    [Header("Structure Modal Runtime Visuals")]
    private GameObject structureUpgradeModalRoot;
    private Image structPreviewImage1;
    private TextMeshProUGUI structLevelText1;
    private TextMeshProUGUI row1StructStatText;
    private TextMeshProUGUI row1StructCostText;
    private Button goldStructUpgradeButton;

    private Image structPreviewImage2;
    private TextMeshProUGUI structLevelText2;
    private TextMeshProUGUI row2StructDescText;
    private Button adsStructUpgradeButton;
    private Image adsStructUpgradeBtnImg;

    [Header("Floating Text & Overlays")]
    [SerializeField] private Transform floatingTextParent;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Button victoryHomeButton;
    [SerializeField] private Button defeatRetryButton;
    [SerializeField] private Button defeatHomeButton;

    private TowerDefGridCell currentSelectedCell;
    private TowerDefGate currentGate;

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (closeBuildModalButton != null) closeBuildModalButton.onClick.AddListener(CloseAllModals);
        if (closeUpgradeModalButton != null) closeUpgradeModalButton.onClick.AddListener(CloseAllModals);
        if (closeGateModalButton != null) closeGateModalButton.onClick.AddListener(CloseAllModals);

        if (buildTurretButton != null) buildTurretButton.onClick.AddListener(OnBuildTurretClicked);
        if (buildGeneratorButton != null) buildGeneratorButton.onClick.AddListener(OnBuildGeneratorClicked);
        if (upgradeConfirmButton != null) upgradeConfirmButton.onClick.AddListener(OnUpgradeConfirmClicked);

        if (repairGateButton != null) repairGateButton.onClick.AddListener(OnRepairGateClicked);
        if (upgradeGateButton != null) upgradeGateButton.onClick.AddListener(OnUpgradeGateClicked);

        if (victoryHomeButton != null) victoryHomeButton.onClick.AddListener(OnBackClicked);
        if (defeatHomeButton != null) defeatHomeButton.onClick.AddListener(OnBackClicked);
        if (defeatRetryButton != null) defeatRetryButton.onClick.AddListener(() => SceneManager.LoadScene("TowerDef"));

        CloseAllModals();
    }

    public void SetupTopBar(Button backBtn, TMP_Text coinTxt, TMP_Text energyTxt)
    {
        backButton = backBtn;
        coinText = coinTxt;
        energyText = energyTxt;

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    public void UpdateCurrencyDisplay(int gold, int energy)
    {
        if (coinText != null) coinText.text = gold.ToString();
        if (energyText != null) energyText.text = energy.ToString();
    }

    public void OpenBuildModal(TowerDefGridCell cell)
    {
        CloseAllModals();
        currentSelectedCell = cell;
        if (buildModalRoot != null) buildModalRoot.SetActive(true);
    }

    public void OpenUpgradeModal(TowerDefGridCell cell)
    {
        CloseAllModals();
        currentSelectedCell = cell;
        if (cell == null) return;

        EnsureSpritesAndFont();
        if (structureUpgradeModalRoot == null)
        {
            BuildStructureUpgradeModal();
        }

        RefreshStructureModalContent();
        if (structureUpgradeModalRoot != null)
        {
            structureUpgradeModalRoot.SetActive(true);
            structureUpgradeModalRoot.transform.SetAsLastSibling();
        }
    }

    public void SetupCustomSprites(
        Sprite framePopup,
        Sprite panelRowBar,
        Sprite btnGrey,
        Sprite btnCyan,
        Sprite btnAds,
        Sprite coinIcon,
        Sprite[] gateSprites,
        Sprite[] bedSprites = null,
        Sprite[] turretBases = null,
        Sprite[] turretGuns = null)
    {
        if (framePopup != null) frameUpgradePopupSprite = framePopup;
        if (panelRowBar != null) panelUpgradeRowBarSprite = panelRowBar;
        if (btnGrey != null) btnUpgradeGreySprite = btnGrey;
        if (btnCyan != null) btnUpgradeCyanSprite = btnCyan;
        if (btnAds != null) btnUpgradeAdsSprite = btnAds;
        if (coinIcon != null) coinIconSprite = coinIcon;
        if (gateSprites != null) gateLevelSprites = gateSprites;
        if (bedSprites != null) bedLevelSprites = bedSprites;
        if (turretBases != null) turretBaseLevelSprites = turretBases;
        if (turretGuns != null) turretGunLevelSprites = turretGuns;
    }

    private void EnsureSpritesAndFont()
    {
        if (uiFont == null)
        {
#if UNITY_EDITOR
            uiFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
#endif
            if (uiFont == null)
                uiFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Nunito SDF");
            if (uiFont == null)
                uiFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        if (frameUpgradePopupSprite == null) frameUpgradePopupSprite = Resources.Load<Sprite>("TowerDef/Frame_Upgrade_Popup");
        if (panelUpgradeRowBarSprite == null) panelUpgradeRowBarSprite = Resources.Load<Sprite>("TowerDef/Panel_Upgrade_Row_Bar");
        if (btnUpgradeGreySprite == null) btnUpgradeGreySprite = Resources.Load<Sprite>("TowerDef/Btn_Upgrade_Grey");
        if (btnUpgradeCyanSprite == null) btnUpgradeCyanSprite = Resources.Load<Sprite>("TowerDef/Btn_Upgrade_Cyan");
        if (btnUpgradeAdsSprite == null) btnUpgradeAdsSprite = Resources.Load<Sprite>("TowerDef/Btn_Upgrade_Ads");
        if (coinIconSprite == null) coinIconSprite = Resources.Load<Sprite>("TowerDef/Icon_Coin");

        if (gateLevelSprites == null || gateLevelSprites.Length < 4 || gateLevelSprites[0] == null)
        {
            gateLevelSprites = new Sprite[4];
            gateLevelSprites[0] = Resources.Load<Sprite>("TowerDef/Gate_Metal");
            gateLevelSprites[1] = Resources.Load<Sprite>("TowerDef/Gate_Cyan_Grid");
            gateLevelSprites[2] = Resources.Load<Sprite>("TowerDef/Gate_Green_Wood");
            gateLevelSprites[3] = Resources.Load<Sprite>("TowerDef/Gate_Blue_Wood");
        }

        if (turretBaseLevelSprites == null || turretBaseLevelSprites.Length < 5 || turretBaseLevelSprites[0] == null)
        {
            turretBaseLevelSprites = new Sprite[5];
            turretBaseLevelSprites[0] = Resources.Load<Sprite>("TowerDef/Turret_Base_01_Cyan");
            turretBaseLevelSprites[1] = Resources.Load<Sprite>("TowerDef/Turret_Base_02_Blue");
            turretBaseLevelSprites[2] = Resources.Load<Sprite>("TowerDef/Turret_Base_03_Gold");
            turretBaseLevelSprites[3] = Resources.Load<Sprite>("TowerDef/Turret_Base_04_Pink");
            turretBaseLevelSprites[4] = Resources.Load<Sprite>("TowerDef/Turret_Base_05_Red");
        }

        if (turretGunLevelSprites == null || turretGunLevelSprites.Length < 5 || turretGunLevelSprites[0] == null)
        {
            turretGunLevelSprites = new Sprite[5];
            turretGunLevelSprites[0] = Resources.Load<Sprite>("TowerDef/Turret_Gun_01_Cyan");
            turretGunLevelSprites[1] = Resources.Load<Sprite>("TowerDef/Turret_Gun_02_Blue");
            turretGunLevelSprites[2] = Resources.Load<Sprite>("TowerDef/Turret_Gun_03_Gold");
            turretGunLevelSprites[3] = Resources.Load<Sprite>("TowerDef/Turret_Gun_04_Purple");
            turretGunLevelSprites[4] = Resources.Load<Sprite>("TowerDef/Turret_Gun_05_Red");
        }

#if UNITY_EDITOR
        string uiDir = "Assets/Sprites/Mini game/Sliced/UI/";
        string tilesDir = "Assets/Sprites/Mini game/Sliced/Tiles/";
        string towersDir = "Assets/Sprites/Mini game/Sliced/Towers/";

        if (frameUpgradePopupSprite == null) frameUpgradePopupSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Frame_Upgrade_Popup.png");
        if (panelUpgradeRowBarSprite == null) panelUpgradeRowBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Panel_Upgrade_Row_Bar.png");
        if (btnUpgradeGreySprite == null) btnUpgradeGreySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Btn_Upgrade_Grey.png");
        if (btnUpgradeCyanSprite == null) btnUpgradeCyanSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Btn_Upgrade_Cyan.png");
        if (btnUpgradeAdsSprite == null) btnUpgradeAdsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Btn_Upgrade_Ads.png");
        if (coinIconSprite == null) coinIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Coin.png");

        if (gateLevelSprites[0] == null) gateLevelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Metal.png");
        if (gateLevelSprites[1] == null) gateLevelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Cyan_Grid.png");
        if (gateLevelSprites[2] == null) gateLevelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Green_Wood.png");
        if (gateLevelSprites[3] == null) gateLevelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Blue_Wood.png");

        if (turretBaseLevelSprites[0] == null) turretBaseLevelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_01_Cyan.png");
        if (turretBaseLevelSprites[1] == null) turretBaseLevelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_02_Blue.png");
        if (turretBaseLevelSprites[2] == null) turretBaseLevelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_03_Gold.png");
        if (turretBaseLevelSprites[3] == null) turretBaseLevelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_04_Pink.png");
        if (turretBaseLevelSprites[4] == null) turretBaseLevelSprites[4] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_05_Red.png");

        if (turretGunLevelSprites[0] == null) turretGunLevelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_01_Cyan.png");
        if (turretGunLevelSprites[1] == null) turretGunLevelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_02_Blue.png");
        if (turretGunLevelSprites[2] == null) turretGunLevelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_03_Gold.png");
        if (turretGunLevelSprites[3] == null) turretGunLevelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_04_Purple.png");
        if (turretGunLevelSprites[4] == null) turretGunLevelSprites[4] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_05_Red.png");

        if (bedLevelSprites == null || bedLevelSprites.Length < 4 || bedLevelSprites[0] == null)
        {
            bedLevelSprites = new Sprite[4];
            bedLevelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Core_Pod_Green.png");
            bedLevelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Core_Pod_Cyan.png");
            bedLevelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Core_Pod_Cyan.png");
            bedLevelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Core_Pod_Cyan.png");
        }
#endif
    }

    public void OpenGateModal(TowerDefGate gate)
    {
        CloseAllModals();
        currentGate = gate;
        if (gate == null) return;

        EnsureSpritesAndFont();
        if (gateModalRoot == null)
        {
            BuildGateUpgradeModal();
        }

        RefreshGateModalContent();
        if (gateModalRoot != null)
        {
            gateModalRoot.SetActive(true);
            gateModalRoot.transform.SetAsLastSibling();
        }
    }

    private void RefreshGateModalContent()
    {
        if (currentGate == null) return;

        bool isMax = currentGate.IsMaxLevel;
        int currentLvl = currentGate.GateLevel;
        int nextLvl = currentGate.NextLevel;
        float nextHp = currentGate.NextMaxHp;
        Sprite nextGateSpr = currentGate.GetLevelSprite(nextLvl);
        if (nextGateSpr == null && gateLevelSprites != null && gateLevelSprites.Length > 0)
        {
            int idx = Mathf.Clamp(nextLvl - 1, 0, gateLevelSprites.Length - 1);
            nextGateSpr = gateLevelSprites[idx];
        }

        if (gatePreviewImage1 != null && nextGateSpr != null)
        {
            gatePreviewImage1.sprite = nextGateSpr;
            gatePreviewImage1.color = Color.white;
        }
        if (gatePreviewImage2 != null && nextGateSpr != null)
        {
            gatePreviewImage2.sprite = nextGateSpr;
            gatePreviewImage2.color = Color.white;
        }

        if (isMax)
        {
            string currentName = TowerDefGate.GetGateName(currentLvl);
            if (gateLevelText1 != null) gateLevelText1.text = $"{currentName}\n<color=#FFD700>MAX</color>";
            if (gateLevelText2 != null) gateLevelText2.text = $"{currentName}\n<color=#FFD700>MAX</color>";
            if (row1HpText != null) row1HpText.text = $"Cổng đã đạt cấp tối đa!\n<size=22>Máu tối đa: {Mathf.RoundToInt(currentGate.MaxHp)}HP</size>";
            if (row2DescText != null) row2DescText.text = "Cổng đã đạt cấp tối đa!";
            if (goldUpgradeButton != null) goldUpgradeButton.interactable = false;
            if (adsUpgradeButton != null) adsUpgradeButton.interactable = false;
            if (row1CostText != null) row1CostText.text = "-";
            return;
        }

        string nextName = TowerDefGate.GetGateName(nextLvl);
        string lvlStr = $"{nextName}\n<color=#FFD700>LV.{nextLvl:D2}</color>";
        if (gateLevelText1 != null) gateLevelText1.text = lvlStr;
        if (gateLevelText2 != null) gateLevelText2.text = lvlStr;

        if (row1HpText != null) row1HpText.text = $"Thể tích máu: {Mathf.RoundToInt(nextHp)}HP";
        if (row1CostText != null) row1CostText.text = $"{currentGate.UpgradeCost}";

        if (goldUpgradeButton != null)
        {
            goldUpgradeButton.interactable = true;
        }

        if (row2DescText != null)
        {
            row2DescText.text = $"Thể tích máu: {Mathf.RoundToInt(nextHp)}HP\n<size=20><color=#D4D4D4>Chỉ có một cơ hội để sử dụng nó</color></size>";
        }

        if (adsUpgradeButton != null)
        {
            adsUpgradeButton.interactable = !hasUsedFreeAdUpgrade;
            if (adsUpgradeBtnImg != null)
            {
                adsUpgradeBtnImg.color = hasUsedFreeAdUpgrade ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : Color.white;
            }
        }
    }

    private void BuildGateUpgradeModal()
    {
        EnsureSpritesAndFont();

        // 1. Nền mờ toàn màn hình (Dim overlay)
        gateModalRoot = new GameObject("GateUpgradeModal", typeof(RectTransform), typeof(Image));
        gateModalRoot.transform.SetParent(transform, false);
        RectTransform rootRt = gateModalRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        Image dimImg = gateModalRoot.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.65f);
        dimImg.raycastTarget = true;

        Button dimBtn = gateModalRoot.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseAllModals);

        // 2. Khung viền Popup (Ảnh 2: Frame_Upgrade_Popup.png)
        GameObject frameObj = new GameObject("ModalFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(gateModalRoot.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = frameRt.anchorMax = frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = Vector2.zero;
        frameRt.sizeDelta = new Vector2(920f, 660f);

        Image frameImg = frameObj.GetComponent<Image>();
        if (frameUpgradePopupSprite != null) frameImg.sprite = frameUpgradePopupSprite;
        frameImg.color = Color.white;
        frameImg.raycastTarget = true;

        // 3. Nút Đóng "✕"
        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(frameObj.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-40f, -40f);
        closeRt.sizeDelta = new Vector2(60f, 60f);
        Image closeImg = closeObj.GetComponent<Image>();
        closeImg.color = new Color(0f, 0f, 0f, 0.01f);
        closeImg.raycastTarget = true;

        closeGateModalButton = closeObj.GetComponent<Button>();
        closeGateModalButton.onClick.AddListener(CloseAllModals);

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = closeTxtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTmp = closeTxtObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) closeTmp.font = uiFont;
        closeTmp.text = "✕";
        closeTmp.fontSize = 40f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.color = new Color(0.7f, 0.85f, 1f, 0.9f);
        closeTmp.alignment = TextAlignmentOptions.Center;

        // 4. Tiêu đề "Upgrade"
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(frameObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -70f);
        titleRt.sizeDelta = new Vector2(600f, 85f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) titleTmp.font = uiFont;
        titleTmp.text = "Upgrade";
        titleTmp.fontSize = 68f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.05f, 0.1f, 0.2f, 0.95f);
        titleOutline.effectDistance = new Vector2(3f, -3f);

        // 5. Hàng 1 (Nâng cấp bằng Vàng - Gold Upgrade Row)
        GameObject row1Obj = new GameObject("Row1_GoldUpgrade", typeof(RectTransform), typeof(Image));
        row1Obj.transform.SetParent(frameObj.transform, false);
        RectTransform row1Rt = row1Obj.GetComponent<RectTransform>();
        row1Rt.anchorMin = row1Rt.anchorMax = row1Rt.pivot = new Vector2(0.5f, 0.5f);
        row1Rt.anchoredPosition = new Vector2(0f, 45f);
        row1Rt.sizeDelta = new Vector2(760f, 180f);
        Image row1Img = row1Obj.GetComponent<Image>();
        if (panelUpgradeRowBarSprite != null) row1Img.sprite = panelUpgradeRowBarSprite;
        row1Img.color = Color.white;

        BuildRowContent(row1Obj.transform, false,
            out gatePreviewImage1, out gateLevelText1, out row1HpText, out row1CostText,
            out goldUpgradeButton, out _);

        // 6. Hàng 2 (Nâng cấp miễn phí bằng Ads - Free Ad Upgrade Row)
        GameObject row2Obj = new GameObject("Row2_AdsUpgrade", typeof(RectTransform), typeof(Image));
        row2Obj.transform.SetParent(frameObj.transform, false);
        RectTransform row2Rt = row2Obj.GetComponent<RectTransform>();
        row2Rt.anchorMin = row2Rt.anchorMax = row2Rt.pivot = new Vector2(0.5f, 0.5f);
        row2Rt.anchoredPosition = new Vector2(0f, -160f);
        row2Rt.sizeDelta = new Vector2(760f, 180f);
        Image row2Img = row2Obj.GetComponent<Image>();
        if (panelUpgradeRowBarSprite != null) row2Img.sprite = panelUpgradeRowBarSprite;
        row2Img.color = Color.white;

        BuildRowContent(row2Obj.transform, true,
            out gatePreviewImage2, out gateLevelText2, out row2DescText, out _,
            out _, out adsUpgradeButton);
        adsUpgradeBtnImg = adsUpgradeButton != null ? adsUpgradeButton.GetComponent<Image>() : null;
    }

    private void BuildRowContent(
        Transform parent,
        bool isFreeAdRow,
        out Image gatePreviewImg,
        out TextMeshProUGUI gateLevelTxt,
        out TextMeshProUGUI descTxt,
        out TextMeshProUGUI costTxt,
        out Button goldBtn,
        out Button adsBtn)
    {
        costTxt = null;
        goldBtn = null;
        adsBtn = null;

        // --- Cột trái: Khung icon Cổng + Chữ Cổng sắt LV.02 ---
        GameObject iconRoot = new GameObject("GateIconBox", typeof(RectTransform));
        iconRoot.transform.SetParent(parent, false);
        RectTransform iconRootRt = iconRoot.GetComponent<RectTransform>();
        iconRootRt.anchorMin = iconRootRt.anchorMax = iconRootRt.pivot = new Vector2(0f, 0.5f);
        iconRootRt.anchoredPosition = new Vector2(95f, 0f);
        iconRootRt.sizeDelta = new Vector2(130f, 150f);

        // Hình cổng
        GameObject sprObj = new GameObject("GateImage", typeof(RectTransform), typeof(Image));
        sprObj.transform.SetParent(iconRoot.transform, false);
        RectTransform sprRt = sprObj.GetComponent<RectTransform>();
        sprRt.anchorMin = sprRt.anchorMax = sprRt.pivot = new Vector2(0.5f, 0.5f);
        sprRt.anchoredPosition = new Vector2(0f, 15f);
        sprRt.sizeDelta = new Vector2(100f, 100f);
        gatePreviewImg = sprObj.GetComponent<Image>();
        gatePreviewImg.preserveAspect = true;
        gatePreviewImg.raycastTarget = false;

        // Chữ: "Cổng sắt\nLV.02"
        GameObject lvlTextObj = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlTextObj.transform.SetParent(iconRoot.transform, false);
        RectTransform lvlTextRt = lvlTextObj.GetComponent<RectTransform>();
        lvlTextRt.anchorMin = lvlTextRt.anchorMax = lvlTextRt.pivot = new Vector2(0.5f, 0f);
        lvlTextRt.anchoredPosition = new Vector2(0f, -5f);
        lvlTextRt.sizeDelta = new Vector2(125f, 50f);
        gateLevelTxt = lvlTextObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) gateLevelTxt.font = uiFont;
        gateLevelTxt.fontSize = 20f;
        gateLevelTxt.fontStyle = FontStyles.Bold;
        gateLevelTxt.alignment = TextAlignmentOptions.Center;
        gateLevelTxt.color = Color.white;
        gateLevelTxt.text = "Cổng sắt\n<color=#FFD700>LV.02</color>";

        // --- Cột giữa: Mô tả Thể tích máu / Giới hạn ---
        GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(parent, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = descRt.anchorMax = descRt.pivot = new Vector2(0.5f, 0.5f);
        descRt.anchoredPosition = new Vector2(-15f, 0f);
        descRt.sizeDelta = new Vector2(360f, 100f);
        descTxt = descObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) descTxt.font = uiFont;
        descTxt.fontSize = 28f;
        descTxt.fontStyle = FontStyles.Bold;
        descTxt.alignment = TextAlignmentOptions.MidlineLeft;
        descTxt.color = Color.white;

        if (!isFreeAdRow)
        {
            descTxt.text = "Thể tích máu: 70HP";
        }
        else
        {
            descTxt.text = "Thể tích máu: 70HP\n<size=20><color=#D4D4D4>Chỉ có một cơ hội để sử dụng nó</color></size>";
        }

        // --- Cột phải: Nút bấm ---
        if (!isFreeAdRow)
        {
            // Nút vàng: Btn_Upgrade_Grey.png
            GameObject btnObj = new GameObject("GoldUpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-105f, 0f);
            btnRt.sizeDelta = new Vector2(165f, 75f);

            Image btnImg = btnObj.GetComponent<Image>();
            if (btnUpgradeGreySprite != null) btnImg.sprite = btnUpgradeGreySprite;
            btnImg.color = Color.white;
            btnImg.raycastTarget = true;

            goldBtn = btnObj.GetComponent<Button>();
            goldBtn.onClick.AddListener(OnGoldUpgradeClicked);

            // Icon đồng vàng bên trong nút
            GameObject coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinObj.transform.SetParent(btnObj.transform, false);
            RectTransform coinRt = coinObj.GetComponent<RectTransform>();
            coinRt.anchorMin = coinRt.anchorMax = coinRt.pivot = new Vector2(0f, 0.5f);
            coinRt.anchoredPosition = new Vector2(32f, 0f);
            coinRt.sizeDelta = new Vector2(40f, 40f);
            Image coinImg = coinObj.GetComponent<Image>();
            if (coinIconSprite != null) coinImg.sprite = coinIconSprite;
            coinImg.preserveAspect = true;
            coinImg.raycastTarget = false;

            // Số tiền (giá nâng cấp)
            GameObject priceObj = new GameObject("PriceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceObj.transform.SetParent(btnObj.transform, false);
            RectTransform priceRt = priceObj.GetComponent<RectTransform>();
            priceRt.anchorMin = priceRt.anchorMax = priceRt.pivot = new Vector2(0.5f, 0.5f);
            priceRt.anchoredPosition = new Vector2(25f, 0f);
            priceRt.sizeDelta = new Vector2(85f, 50f);
            costTxt = priceObj.GetComponent<TextMeshProUGUI>();
            if (uiFont != null) costTxt.font = uiFont;
            costTxt.text = "12";
            costTxt.fontSize = 32f;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.color = new Color(0.12f, 0.12f, 0.12f);
            costTxt.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            // Nút Ads: Btn_Upgrade_Ads.png (có sẵn logo YouTube + badge Ads + Free)
            GameObject adsBtnObj = new GameObject("AdsUpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            adsBtnObj.transform.SetParent(parent, false);
            RectTransform adsBtnRt = adsBtnObj.GetComponent<RectTransform>();
            adsBtnRt.anchorMin = adsBtnRt.anchorMax = adsBtnRt.pivot = new Vector2(1f, 0.5f);
            adsBtnRt.anchoredPosition = new Vector2(-105f, 0f);
            adsBtnRt.sizeDelta = new Vector2(165f, 75f);

            Image adsImg = adsBtnObj.GetComponent<Image>();
            if (btnUpgradeAdsSprite != null) adsImg.sprite = btnUpgradeAdsSprite;
            adsImg.color = Color.white;
            adsImg.raycastTarget = true;

            adsBtn = adsBtnObj.GetComponent<Button>();
            adsBtn.onClick.AddListener(OnAdUpgradeClicked);
        }
    }

    private void OnGoldUpgradeClicked()
    {
        if (currentGate == null) return;
        bool ok = TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.TryUpgradeGate(currentGate);
        if (ok)
        {
            if (currentGate.IsMaxLevel)
            {
                CloseAllModals();
            }
            else
            {
                RefreshGateModalContent();
            }
        }
    }

    private void OnAdUpgradeClicked()
    {
        if (currentGate == null || hasUsedFreeAdUpgrade) return;
        bool ok = TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.TryUpgradeGateFree(currentGate);
        if (ok)
        {
            hasUsedFreeAdUpgrade = true;
            if (currentGate.IsMaxLevel)
            {
                CloseAllModals();
            }
            else
            {
                RefreshGateModalContent();
            }
        }
    }

    private void OnGoldStructureUpgradeClicked()
    {
        if (currentSelectedCell == null) return;
        bool ok = TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.TryUpgradeStructure(currentSelectedCell);
        if (ok)
        {
            bool isMax = false;
            if (currentSelectedCell.Generator != null) isMax = currentSelectedCell.Generator.IsMaxLevel;
            else if (currentSelectedCell.Turret != null) isMax = currentSelectedCell.Turret.IsMaxLevel;
            else isMax = currentSelectedCell.StructureLevel >= 4;

            if (isMax)
            {
                CloseAllModals();
            }
            else
            {
                RefreshStructureModalContent();
            }
        }
    }

    private void OnAdStructureUpgradeClicked()
    {
        if (currentSelectedCell == null || hasUsedFreeBedAdUpgrade) return;
        bool ok = TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.TryUpgradeStructureFree(currentSelectedCell);
        if (ok)
        {
            hasUsedFreeBedAdUpgrade = true;
            bool isMax = false;
            if (currentSelectedCell.Generator != null) isMax = currentSelectedCell.Generator.IsMaxLevel;
            else if (currentSelectedCell.Turret != null) isMax = currentSelectedCell.Turret.IsMaxLevel;
            else isMax = currentSelectedCell.StructureLevel >= 4;

            if (isMax)
            {
                CloseAllModals();
            }
            else
            {
                RefreshStructureModalContent();
            }
        }
    }

    private void RefreshStructureModalContent()
    {
        if (currentSelectedCell == null) return;

        bool isBed = currentSelectedCell.CurrentType == TowerDefStructureType.CoreBed;
        bool isGenerator = currentSelectedCell.CurrentType == TowerDefStructureType.EnergyGenerator;
        bool isTurret = currentSelectedCell.CurrentType == TowerDefStructureType.Turret;

        string structName = isBed ? "Giường ngủ" : (isGenerator ? "Máy phát điện" : "Tháp pháo");
        int currentLvl = 1;
        int nextLvl = 2;
        int cost = 50;
        bool isMax = false;
        Sprite nextSprite = null;
        string statStr = "";

        if (currentSelectedCell.Generator != null)
        {
            currentLvl = currentSelectedCell.Generator.StructureLevel;
            isMax = currentSelectedCell.Generator.IsMaxLevel;
            nextLvl = currentSelectedCell.Generator.NextLevel;
            cost = currentSelectedCell.Generator.UpgradeCost;
            int nextOutput = currentSelectedCell.Generator.NextOutput;
            string unit = isBed ? "Vàng" : "Năng lượng";
            statStr = $"Sản lượng: +{nextOutput} {unit}/giây";

            nextSprite = currentSelectedCell.Generator.GetLevelSprite(nextLvl);
            if (nextSprite == null && isBed && bedLevelSprites != null && bedLevelSprites.Length > 0)
            {
                int idx = Mathf.Clamp(nextLvl - 1, 0, bedLevelSprites.Length - 1);
                nextSprite = bedLevelSprites[idx];
            }
        }
        else if (currentSelectedCell.Turret != null)
        {
            TowerDefTurret turret = currentSelectedCell.Turret;
            currentLvl = turret.TurretLevel;
            isMax = turret.IsMaxLevel;
            nextLvl = turret.NextLevel;
            cost = turret.UpgradeCost;
            structName = TowerDefTurret.GetTurretName(isMax ? currentLvl : nextLvl);
            statStr = $"Sát thương: {Mathf.RoundToInt(turret.NextDamage)} DMG | Tốc bắn: {turret.NextFireRate:F1}/s";

            nextSprite = turret.GetGunSprite(isMax ? currentLvl : nextLvl);
            if (nextSprite == null && turretGunLevelSprites != null && turretGunLevelSprites.Length > 0)
            {
                int idx = Mathf.Clamp((isMax ? currentLvl : nextLvl) - 1, 0, turretGunLevelSprites.Length - 1);
                nextSprite = turretGunLevelSprites[idx];
            }
        }
        else
        {
            currentLvl = currentSelectedCell.StructureLevel;
            isMax = currentLvl >= 4;
            nextLvl = currentLvl + 1;
            cost = 50;
            statStr = $"Cấp độ: {nextLvl}";
        }

        if (structPreviewImage1 != null && nextSprite != null)
        {
            structPreviewImage1.sprite = nextSprite;
            structPreviewImage1.color = Color.white;
        }
        if (structPreviewImage2 != null && nextSprite != null)
        {
            structPreviewImage2.sprite = nextSprite;
            structPreviewImage2.color = Color.white;
        }

        if (isMax)
        {
            string maxLvlStr = $"{structName}\n<color=#FFD700>MAX</color>";
            if (structLevelText1 != null) structLevelText1.text = maxLvlStr;
            if (structLevelText2 != null) structLevelText2.text = maxLvlStr;
            if (row1StructStatText != null) row1StructStatText.text = $"{structName} đã đạt cấp tối đa!";
            if (row2StructDescText != null) row2StructDescText.text = $"{structName} đã đạt cấp tối đa!";
            if (row1StructCostText != null) row1StructCostText.text = "-";
            if (goldStructUpgradeButton != null) goldStructUpgradeButton.interactable = false;
            if (adsStructUpgradeButton != null) adsStructUpgradeButton.interactable = false;
            return;
        }

        string lvlStr = $"{structName}\n<color=#FFD700>LV.{nextLvl:D2}</color>";
        if (structLevelText1 != null) structLevelText1.text = lvlStr;
        if (structLevelText2 != null) structLevelText2.text = lvlStr;

        if (row1StructStatText != null) row1StructStatText.text = statStr;
        if (row1StructCostText != null) row1StructCostText.text = $"{cost}";

        if (goldStructUpgradeButton != null)
        {
            goldStructUpgradeButton.interactable = true;
        }

        if (row2StructDescText != null)
        {
            row2StructDescText.text = $"{statStr}\n<size=20><color=#D4D4D4>Chỉ có một cơ hội để sử dụng nó</color></size>";
        }

        if (adsStructUpgradeButton != null)
        {
            bool canUseAd = isBed ? !hasUsedFreeBedAdUpgrade : !hasUsedFreeAdUpgrade;
            adsStructUpgradeButton.interactable = canUseAd;
            if (adsStructUpgradeBtnImg != null)
            {
                adsStructUpgradeBtnImg.color = canUseAd ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }

    private void BuildStructureUpgradeModal()
    {
        EnsureSpritesAndFont();

        // 1. Nền mờ toàn màn hình (Dim overlay)
        structureUpgradeModalRoot = new GameObject("StructureUpgradeModal", typeof(RectTransform), typeof(Image));
        structureUpgradeModalRoot.transform.SetParent(transform, false);
        RectTransform rootRt = structureUpgradeModalRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        Image dimImg = structureUpgradeModalRoot.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.65f);
        dimImg.raycastTarget = true;

        Button dimBtn = structureUpgradeModalRoot.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseAllModals);

        // 2. Khung viền Popup (Frame_Upgrade_Popup.png)
        GameObject frameObj = new GameObject("ModalFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(structureUpgradeModalRoot.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = frameRt.anchorMax = frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = Vector2.zero;
        frameRt.sizeDelta = new Vector2(920f, 660f);

        Image frameImg = frameObj.GetComponent<Image>();
        if (frameUpgradePopupSprite != null) frameImg.sprite = frameUpgradePopupSprite;
        frameImg.color = Color.white;
        frameImg.raycastTarget = true;

        // 3. Nút Đóng "✕"
        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(frameObj.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-40f, -40f);
        closeRt.sizeDelta = new Vector2(60f, 60f);
        Image closeImg = closeObj.GetComponent<Image>();
        closeImg.color = new Color(0f, 0f, 0f, 0.01f);
        closeImg.raycastTarget = true;

        Button closeBtn = closeObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(CloseAllModals);

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = closeTxtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTmp = closeTxtObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) closeTmp.font = uiFont;
        closeTmp.text = "✕";
        closeTmp.fontSize = 40f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.color = new Color(0.7f, 0.85f, 1f, 0.9f);
        closeTmp.alignment = TextAlignmentOptions.Center;

        // 4. Tiêu đề "Upgrade"
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(frameObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -70f);
        titleRt.sizeDelta = new Vector2(600f, 85f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) titleTmp.font = uiFont;
        titleTmp.text = "Upgrade";
        titleTmp.fontSize = 68f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.05f, 0.1f, 0.2f, 0.95f);
        titleOutline.effectDistance = new Vector2(3f, -3f);

        // 5. Hàng 1 (Nâng cấp bằng Vàng - Gold Upgrade Row)
        GameObject row1Obj = new GameObject("Row1_GoldUpgrade", typeof(RectTransform), typeof(Image));
        row1Obj.transform.SetParent(frameObj.transform, false);
        RectTransform row1Rt = row1Obj.GetComponent<RectTransform>();
        row1Rt.anchorMin = row1Rt.anchorMax = row1Rt.pivot = new Vector2(0.5f, 0.5f);
        row1Rt.anchoredPosition = new Vector2(0f, 45f);
        row1Rt.sizeDelta = new Vector2(760f, 180f);
        Image row1Img = row1Obj.GetComponent<Image>();
        if (panelUpgradeRowBarSprite != null) row1Img.sprite = panelUpgradeRowBarSprite;
        row1Img.color = Color.white;

        BuildStructureRow(row1Obj.transform, false,
            out structPreviewImage1, out structLevelText1, out row1StructStatText, out row1StructCostText,
            out goldStructUpgradeButton, out _);

        // 6. Hàng 2 (Nâng cấp miễn phí bằng Ads - Free Ad Upgrade Row)
        GameObject row2Obj = new GameObject("Row2_AdsUpgrade", typeof(RectTransform), typeof(Image));
        row2Obj.transform.SetParent(frameObj.transform, false);
        RectTransform row2Rt = row2Obj.GetComponent<RectTransform>();
        row2Rt.anchorMin = row2Rt.anchorMax = row2Rt.pivot = new Vector2(0.5f, 0.5f);
        row2Rt.anchoredPosition = new Vector2(0f, -160f);
        row2Rt.sizeDelta = new Vector2(760f, 180f);
        Image row2Img = row2Obj.GetComponent<Image>();
        if (panelUpgradeRowBarSprite != null) row2Img.sprite = panelUpgradeRowBarSprite;
        row2Img.color = Color.white;

        BuildStructureRow(row2Obj.transform, true,
            out structPreviewImage2, out structLevelText2, out row2StructDescText, out _,
            out _, out adsStructUpgradeButton);
        adsStructUpgradeBtnImg = adsStructUpgradeButton != null ? adsStructUpgradeButton.GetComponent<Image>() : null;
    }

    private void BuildStructureRow(
        Transform parent,
        bool isFreeAdRow,
        out Image previewImg,
        out TextMeshProUGUI levelTxt,
        out TextMeshProUGUI descTxt,
        out TextMeshProUGUI costTxt,
        out Button goldBtn,
        out Button adsBtn)
    {
        costTxt = null;
        goldBtn = null;
        adsBtn = null;

        // Cột trái: Khung icon + Tên & Cấp độ
        GameObject iconRoot = new GameObject("IconBox", typeof(RectTransform));
        iconRoot.transform.SetParent(parent, false);
        RectTransform iconRootRt = iconRoot.GetComponent<RectTransform>();
        iconRootRt.anchorMin = iconRootRt.anchorMax = iconRootRt.pivot = new Vector2(0f, 0.5f);
        iconRootRt.anchoredPosition = new Vector2(95f, 0f);
        iconRootRt.sizeDelta = new Vector2(130f, 150f);

        GameObject sprObj = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
        sprObj.transform.SetParent(iconRoot.transform, false);
        RectTransform sprRt = sprObj.GetComponent<RectTransform>();
        sprRt.anchorMin = sprRt.anchorMax = sprRt.pivot = new Vector2(0.5f, 0.5f);
        sprRt.anchoredPosition = new Vector2(0f, 15f);
        sprRt.sizeDelta = new Vector2(100f, 100f);
        previewImg = sprObj.GetComponent<Image>();
        previewImg.preserveAspect = true;
        previewImg.raycastTarget = false;

        GameObject lvlTextObj = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlTextObj.transform.SetParent(iconRoot.transform, false);
        RectTransform lvlTextRt = lvlTextObj.GetComponent<RectTransform>();
        lvlTextRt.anchorMin = lvlTextRt.anchorMax = lvlTextRt.pivot = new Vector2(0.5f, 0f);
        lvlTextRt.anchoredPosition = new Vector2(0f, -5f);
        lvlTextRt.sizeDelta = new Vector2(125f, 50f);
        levelTxt = lvlTextObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) levelTxt.font = uiFont;
        levelTxt.fontSize = 20f;
        levelTxt.fontStyle = FontStyles.Bold;
        levelTxt.alignment = TextAlignmentOptions.Center;
        levelTxt.color = Color.white;
        levelTxt.text = "Giường ngủ\n<color=#FFD700>LV.02</color>";

        // Cột giữa: Mô tả Stat / Giới hạn
        GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(parent, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = descRt.anchorMax = descRt.pivot = new Vector2(0.5f, 0.5f);
        descRt.anchoredPosition = new Vector2(-15f, 0f);
        descRt.sizeDelta = new Vector2(360f, 100f);
        descTxt = descObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) descTxt.font = uiFont;
        descTxt.fontSize = 28f;
        descTxt.fontStyle = FontStyles.Bold;
        descTxt.alignment = TextAlignmentOptions.MidlineLeft;
        descTxt.color = Color.white;

        if (!isFreeAdRow)
        {
            descTxt.text = "Sản lượng: +2 Vàng/giây";
        }
        else
        {
            descTxt.text = "Sản lượng: +2 Vàng/giây\n<size=20><color=#D4D4D4>Chỉ có một cơ hội để sử dụng nó</color></size>";
        }

        // Cột phải: Nút bấm
        if (!isFreeAdRow)
        {
            GameObject btnObj = new GameObject("GoldUpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-105f, 0f);
            btnRt.sizeDelta = new Vector2(165f, 75f);

            Image btnImg = btnObj.GetComponent<Image>();
            if (btnUpgradeGreySprite != null) btnImg.sprite = btnUpgradeGreySprite;
            btnImg.color = Color.white;
            btnImg.raycastTarget = true;

            goldBtn = btnObj.GetComponent<Button>();
            goldBtn.onClick.AddListener(OnGoldStructureUpgradeClicked);

            GameObject coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinObj.transform.SetParent(btnObj.transform, false);
            RectTransform coinRt = coinObj.GetComponent<RectTransform>();
            coinRt.anchorMin = coinRt.anchorMax = coinRt.pivot = new Vector2(0f, 0.5f);
            coinRt.anchoredPosition = new Vector2(32f, 0f);
            coinRt.sizeDelta = new Vector2(40f, 40f);
            Image coinImg = coinObj.GetComponent<Image>();
            if (coinIconSprite != null) coinImg.sprite = coinIconSprite;
            coinImg.preserveAspect = true;
            coinImg.raycastTarget = false;

            GameObject priceObj = new GameObject("PriceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceObj.transform.SetParent(btnObj.transform, false);
            RectTransform priceRt = priceObj.GetComponent<RectTransform>();
            priceRt.anchorMin = priceRt.anchorMax = priceRt.pivot = new Vector2(0.5f, 0.5f);
            priceRt.anchoredPosition = new Vector2(25f, 0f);
            priceRt.sizeDelta = new Vector2(85f, 50f);
            costTxt = priceObj.GetComponent<TextMeshProUGUI>();
            if (uiFont != null) costTxt.font = uiFont;
            costTxt.text = "50";
            costTxt.fontSize = 32f;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.color = new Color(0.12f, 0.12f, 0.12f);
            costTxt.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            GameObject adsBtnObj = new GameObject("AdsUpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            adsBtnObj.transform.SetParent(parent, false);
            RectTransform adsBtnRt = adsBtnObj.GetComponent<RectTransform>();
            adsBtnRt.anchorMin = adsBtnRt.anchorMax = adsBtnRt.pivot = new Vector2(1f, 0.5f);
            adsBtnRt.anchoredPosition = new Vector2(-105f, 0f);
            adsBtnRt.sizeDelta = new Vector2(165f, 75f);

            Image adsImg = adsBtnObj.GetComponent<Image>();
            if (btnUpgradeAdsSprite != null) adsImg.sprite = btnUpgradeAdsSprite;
            adsImg.color = Color.white;
            adsImg.raycastTarget = true;

            adsBtn = adsBtnObj.GetComponent<Button>();
            adsBtn.onClick.AddListener(OnAdStructureUpgradeClicked);
        }
    }

    public void CloseAllModals()
    {
        if (buildModalRoot != null) buildModalRoot.SetActive(false);
        if (upgradeModalRoot != null) upgradeModalRoot.SetActive(false);
        if (gateModalRoot != null) gateModalRoot.SetActive(false);
        if (structureUpgradeModalRoot != null) structureUpgradeModalRoot.SetActive(false);
        currentSelectedCell = null;
        currentGate = null;
    }

    public void ShowVictory()
    {
        CloseAllModals();
        if (victoryPanel == null)
        {
            BuildVictoryPanel();
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            victoryPanel.transform.SetAsLastSibling();
        }
    }

    public void ShowDefeat()
    {
        CloseAllModals();
        if (defeatPanel == null)
        {
            BuildDefeatPanel();
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            defeatPanel.transform.SetAsLastSibling();
        }
    }

    private void BuildDefeatPanel()
    {
        defeatPanel = new GameObject("DefeatPanel", typeof(RectTransform), typeof(Image));
        defeatPanel.transform.SetParent(transform, false);
        RectTransform panelRt = defeatPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

        Image panelImg = defeatPanel.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.04f, 0.06f, 0.88f);
        panelImg.raycastTarget = true;

        // Modal Box
        GameObject box = new GameObject("DefeatBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(defeatPanel.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(760f, 580f);

        Image boxImg = box.GetComponent<Image>();
        boxImg.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        boxImg.raycastTarget = true;

        // Title: GAME OVER
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(700f, 90f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "GAME OVER";
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.28f, 0.28f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle: Thành trì đã bị quái vật phá hủy!
        GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(box.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = descRt.anchorMax = descRt.pivot = new Vector2(0.5f, 1f);
        descRt.anchoredPosition = new Vector2(0f, -145f);
        descRt.sizeDelta = new Vector2(680f, 90f);
        TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
        descTmp.text = "Thành trì đã bị quái vật phá hủy!\nHãy nâng cấp phòng thủ và thử lại.";
        descTmp.fontSize = 32f;
        descTmp.color = new Color(0.85f, 0.85f, 0.9f);
        descTmp.alignment = TextAlignmentOptions.Center;

        // Retry Button ("CHƠI LẠI")
        GameObject retryObj = CreateModalButton(box.transform, "RetryButton", new Vector2(0f, -40f), new Vector2(380f, 85f), new Color(0.15f, 0.65f, 0.35f, 1f), "CHƠI LẠI");
        defeatRetryButton = retryObj.GetComponent<Button>();
        defeatRetryButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

        // Home Button ("VỀ TRANG CHỦ")
        GameObject homeObj = CreateModalButton(box.transform, "HomeButton", new Vector2(0f, -150f), new Vector2(380f, 85f), new Color(0.35f, 0.38f, 0.45f, 1f), "VỀ TRANG CHỦ");
        defeatHomeButton = homeObj.GetComponent<Button>();
        defeatHomeButton.onClick.AddListener(OnBackClicked);
    }

    private void BuildVictoryPanel()
    {
        victoryPanel = new GameObject("VictoryPanel", typeof(RectTransform), typeof(Image));
        victoryPanel.transform.SetParent(transform, false);
        RectTransform panelRt = victoryPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

        Image panelImg = victoryPanel.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.04f, 0.06f, 0.88f);
        panelImg.raycastTarget = true;

        // Modal Box
        GameObject box = new GameObject("VictoryBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(victoryPanel.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(760f, 560f);

        Image boxImg = box.GetComponent<Image>();
        boxImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
        boxImg.raycastTarget = true;

        // Title: CHIẾN THẮNG!
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(700f, 90f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "CHIẾN THẮNG!";
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.25f, 0.92f, 0.45f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle: Bảo vệ thành công căn cứ!
        GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(box.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = descRt.anchorMax = descRt.pivot = new Vector2(0.5f, 1f);
        descRt.anchoredPosition = new Vector2(0f, -145f);
        descRt.sizeDelta = new Vector2(680f, 80f);
        TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
        descTmp.text = "Căn cứ phòng thủ đã được bảo vệ thành công!";
        descTmp.fontSize = 32f;
        descTmp.color = new Color(0.85f, 0.85f, 0.9f);
        descTmp.alignment = TextAlignmentOptions.Center;

        // Next / Home Button
        GameObject homeObj = CreateModalButton(box.transform, "HomeButton", new Vector2(0f, -60f), new Vector2(380f, 85f), new Color(0.15f, 0.65f, 0.35f, 1f), "VỀ TRANG CHỦ");
        victoryHomeButton = homeObj.GetComponent<Button>();
        victoryHomeButton.onClick.AddListener(OnBackClicked);
    }

    private GameObject CreateModalButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor, string label)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = tRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btnObj;
    }

    public void SpawnFloatingText(Vector3 worldPos, string content, Color color)
    {
        GameObject textObj = new GameObject("FloatingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(floatingTextParent != null ? floatingTextParent : transform, false);
        textObj.transform.position = worldPos;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        StartCoroutine(FloatAndFade(textObj, tmp));
    }

    private IEnumerator FloatAndFade(GameObject obj, TextMeshProUGUI tmp)
    {
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Canvas canvas = GetComponentInParent<Canvas>();
        float unitsPerUiPixel = canvas != null ? canvas.transform.lossyScale.y : 1f;
        Vector3 targetPos = startPos + new Vector3(0f, 60f * unitsPerUiPixel, 0f);
        Color startColor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        Destroy(obj);
    }

    private void OnBuildTurretClicked()
    {
        if (currentSelectedCell != null)
        {
            TowerDefGameManager.Instance?.TryBuildStructure(currentSelectedCell, TowerDefStructureType.Turret);
        }
        CloseAllModals();
    }

    private void OnBuildGeneratorClicked()
    {
        if (currentSelectedCell != null)
        {
            TowerDefGameManager.Instance?.TryBuildStructure(currentSelectedCell, TowerDefStructureType.EnergyGenerator);
        }
        CloseAllModals();
    }

    private void OnUpgradeConfirmClicked()
    {
        if (currentSelectedCell != null)
        {
            TowerDefGameManager.Instance?.TryUpgradeStructure(currentSelectedCell);
        }
        CloseAllModals();
    }

    private void OnRepairGateClicked()
    {
        if (currentGate != null)
        {
            TowerDefGameManager.Instance?.TryRepairGate(currentGate);
        }
        CloseAllModals();
    }

    private void OnUpgradeGateClicked()
    {
        if (currentGate != null)
        {
            TowerDefGameManager.Instance?.TryUpgradeGate(currentGate);
        }
        CloseAllModals();
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
