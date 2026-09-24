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
        if (upgradeModalRoot == null || cell == null) return;

        string name = cell.CurrentType == TowerDefStructureType.Turret ? "Turret Gun" :
                      cell.CurrentType == TowerDefStructureType.CoreBed ? "Core Bed" : "Energy Generator";
        int cost = cell.CurrentType == TowerDefStructureType.Turret ? (cell.Turret != null ? cell.Turret.UpgradeCost : 50) :
                   (cell.Generator != null ? cell.Generator.UpgradeCost : 50);

        if (upgradeTitleText != null) upgradeTitleText.text = $"{name} (Lv.{cell.StructureLevel})";
        if (upgradeDescText != null) upgradeDescText.text = $"Upgrade {name} to Level {cell.StructureLevel + 1} to increase performance.";
        if (upgradeCostText != null) upgradeCostText.text = $"{cost} Gold";

        upgradeModalRoot.SetActive(true);
    }

    public void OpenGateModal(TowerDefGate gate)
    {
        CloseAllModals();
        currentGate = gate;
        if (gateModalRoot == null || gate == null) return;

        if (gateHpText != null) gateHpText.text = $"Gate HP: {Mathf.CeilToInt(gate.CurrentHp)} / {gate.MaxHp} (Lv.{gate.GateLevel})";
        if (upgradeGateCostText != null) upgradeGateCostText.text = $"{gate.UpgradeCost} Gold";

        gateModalRoot.SetActive(true);
    }

    public void CloseAllModals()
    {
        if (buildModalRoot != null) buildModalRoot.SetActive(false);
        if (upgradeModalRoot != null) upgradeModalRoot.SetActive(false);
        if (gateModalRoot != null) gateModalRoot.SetActive(false);
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
        Vector3 targetPos = startPos + new Vector3(0f, 60f, 0f);
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
