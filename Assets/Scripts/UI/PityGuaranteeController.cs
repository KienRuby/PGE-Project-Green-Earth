using System;
using UnityEngine;

/// <summary>
/// Controller điều phối hệ thống Triple Pity Guarantee (Bảo hiểm 3 bậc: Elite 10, Epic 25, Legend 50):
/// - Đảm bảo quy tắc độc lập: Quay trúng bậc nào chỉ reset bảo hiểm của bậc đó về 0.
/// - Các bậc khác tiếp tục tăng tiến độ bình thường.
/// - Kết nối trực tiếp với PlayerDataService và LabUpgradeController.
/// </summary>
public class PityGuaranteeController : MonoBehaviour
{
    [Header("Threshold Settings")]
    [SerializeField] private int eliteThreshold = 10;
    [SerializeField] private int epicThreshold = 25;
    [SerializeField] private int legendThreshold = 50;

    [SerializeField] private LabUpgradeController labUpgradeController;
    [SerializeField] private PityGuaranteePanel pityGuaranteePanel;

    public int EliteThreshold => eliteThreshold;
    public int EpicThreshold => epicThreshold;
    public int LegendThreshold => legendThreshold;

    public int EliteCounter => PlayerDataService.LabElitePityCounter;
    public int EpicCounter => PlayerDataService.LabEpicPityCounter;
    public int LegendCounter => PlayerDataService.LabLegendPityCounter;

    public event Action OnPityCountersChanged;

    private void Awake()
    {
        if (labUpgradeController == null) labUpgradeController = GetComponent<LabUpgradeController>() ?? FindObjectOfType<LabUpgradeController>();
        if (pityGuaranteePanel == null) pityGuaranteePanel = GetComponent<PityGuaranteePanel>() ?? FindObjectOfType<PityGuaranteePanel>();
    }

    public int GetPityCount(LabUpgradeController.ItemRarity rarity)
    {
        switch (rarity)
        {
            case LabUpgradeController.ItemRarity.Elite: return EliteCounter;
            case LabUpgradeController.ItemRarity.Epic: return EpicCounter;
            case LabUpgradeController.ItemRarity.Legend: return LegendCounter;
            default: return 0;
        }
    }

    public int GetPityThreshold(LabUpgradeController.ItemRarity rarity)
    {
        switch (rarity)
        {
            case LabUpgradeController.ItemRarity.Elite: return eliteThreshold;
            case LabUpgradeController.ItemRarity.Epic: return epicThreshold;
            case LabUpgradeController.ItemRarity.Legend: return legendThreshold;
            default: return 0;
        }
    }

    public bool IsGuaranteed(LabUpgradeController.ItemRarity rarity)
    {
        int threshold = GetPityThreshold(rarity);
        if (threshold <= 0) return false;
        return GetPityCount(rarity) + 1 >= threshold;
    }

    public LabUpgradeController.ItemRarity? GetNextGuaranteedRarity()
    {
        if (legendThreshold > 0 && LegendCounter + 1 >= legendThreshold) return LabUpgradeController.ItemRarity.Legend;
        if (epicThreshold > 0 && EpicCounter + 1 >= epicThreshold) return LabUpgradeController.ItemRarity.Epic;
        if (eliteThreshold > 0 && EliteCounter + 1 >= eliteThreshold) return LabUpgradeController.ItemRarity.Elite;
        return null;
    }

    /// <summary>
    /// Ghi nhận kết quả của 1 lượt roll và cập nhật bộ đếm độc lập:
    /// Trúng Legend -> reset Legend, tăng Elite & Epic.
    /// Trúng Epic -> reset Epic, tăng Elite & Legend.
    /// Trúng Elite -> reset Elite, tăng Epic & Legend.
    /// Trúng Common -> tăng cả 3.
    /// </summary>
    public void RecordRollResult(LabUpgradeController.ItemRarity wonRarity)
    {
        int elite = EliteCounter;
        int epic = EpicCounter;
        int legend = LegendCounter;

        if (wonRarity == LabUpgradeController.ItemRarity.Legend)
        {
            legend = 0;
            elite++;
            epic++;
        }
        else if (wonRarity == LabUpgradeController.ItemRarity.Epic)
        {
            epic = 0;
            elite++;
            legend++;
        }
        else if (wonRarity == LabUpgradeController.ItemRarity.Elite)
        {
            elite = 0;
            epic++;
            legend++;
        }
        else
        {
            elite++;
            epic++;
            legend++;
        }

        PlayerDataService.LabElitePityCounter = elite;
        PlayerDataService.LabEpicPityCounter = epic;
        PlayerDataService.LabLegendPityCounter = legend;
        PlayerDataService.LabPityCounter = elite;

        OnPityCountersChanged?.Invoke();
        if (pityGuaranteePanel != null) pityGuaranteePanel.Refresh();
    }

    public void ResetAllPity()
    {
        PlayerDataService.LabElitePityCounter = 0;
        PlayerDataService.LabEpicPityCounter = 0;
        PlayerDataService.LabLegendPityCounter = 0;
        PlayerDataService.LabPityCounter = 0;

        OnPityCountersChanged?.Invoke();
        if (pityGuaranteePanel != null) pityGuaranteePanel.Refresh();
    }
}
