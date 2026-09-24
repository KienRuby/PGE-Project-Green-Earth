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
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    public void ShowDefeat()
    {
        CloseAllModals();
        if (defeatPanel != null) defeatPanel.SetActive(true);
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
