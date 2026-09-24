using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý công trình sản sinh tài nguyên (Resource Generator):
/// - CorePod / Bed: Sản sinh Vàng (Gold / Coin) thụ động theo thời gian.
/// - PawnTower: Sản sinh Năng Lượng (Energy / Lightning) thụ động theo thời gian.
/// - Cho phép nâng cấp để tăng sản lượng thu hoạch.
/// </summary>
public class TowerDefGenerator : MonoBehaviour
{
    [Header("Generator Configuration")]
    [SerializeField] private TowerDefStructureType generatorType = TowerDefStructureType.CoreBed;
    [SerializeField] private int structureLevel = 1;
    [SerializeField] private int baseOutput = 1;
    [SerializeField] private float produceInterval = 1.0f; // Giây mỗi chu kỳ
    [SerializeField] private int baseUpgradeCost = 50;

    [Header("Visual References")]
    [SerializeField] private Image structureImage;
    [SerializeField] private GameObject upgradeIcon;

    private float timer;

    public TowerDefStructureType GeneratorType => generatorType;
    public int StructureLevel => structureLevel;
    public int CurrentOutput => baseOutput * structureLevel;
    public float ProduceInterval => produceInterval;
    public int UpgradeCost => baseUpgradeCost * structureLevel;

    public event Action<int> OnGeneratorUpgraded;

    public void Setup(TowerDefStructureType type, int level, Image img, GameObject upIcon, float interval = 1f)
    {
        generatorType = type;
        structureLevel = level;
        structureImage = img;
        upgradeIcon = upIcon;
        produceInterval = interval;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= produceInterval)
        {
            timer = 0f;
            ProduceResource();
        }
    }

    private void ProduceResource()
    {
        if (TowerDefGameManager.Instance == null) return;

        int amount = CurrentOutput;
        if (generatorType == TowerDefStructureType.CoreBed)
        {
            TowerDefGameManager.Instance.AddGold(amount);
            TowerDefGameManager.Instance.ShowFloatingText(transform.position, $"+{amount}", Color.yellow);
        }
        else if (generatorType == TowerDefStructureType.EnergyGenerator)
        {
            TowerDefGameManager.Instance.AddEnergy(amount);
            TowerDefGameManager.Instance.ShowFloatingText(transform.position, $"+{amount}", new Color(0.2f, 0.85f, 1f));
        }
    }

    public bool TryUpgrade(ref int gold)
    {
        int cost = UpgradeCost;
        if (gold < cost) return false;

        gold -= cost;
        structureLevel++;
        OnGeneratorUpgraded?.Invoke(structureLevel);
        return true;
    }

    public void SetUpgradeBadgeActive(bool active)
    {
        if (upgradeIcon != null)
        {
            upgradeIcon.SetActive(active);
        }
    }
}
