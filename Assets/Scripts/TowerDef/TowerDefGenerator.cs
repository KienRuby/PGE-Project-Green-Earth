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
    [SerializeField] private Sprite[] levelSprites;

    [Header("Visual References")]
    [SerializeField] private Image structureImage;
    [SerializeField] private GameObject upgradeIcon;

    public const int MAX_GENERATOR_LEVEL = 4;
    private float timer;

    public TowerDefStructureType GeneratorType => generatorType;
    public int StructureLevel => structureLevel;
    public int MaxLevel => MAX_GENERATOR_LEVEL;
    public bool IsMaxLevel => structureLevel >= MAX_GENERATOR_LEVEL;
    public int NextLevel => Mathf.Min(MAX_GENERATOR_LEVEL, structureLevel + 1);
    public int CurrentOutput => baseOutput * structureLevel;
    public int NextOutput => baseOutput * NextLevel;
    public float ProduceInterval => produceInterval;
    public int UpgradeCost => IsMaxLevel ? 0 : baseUpgradeCost * structureLevel;

    public event Action<int> OnGeneratorUpgraded;

    public void Setup(TowerDefStructureType type, int level, Image img, GameObject upIcon, float interval = 1f)
    {
        generatorType = type;
        structureLevel = level;
        structureImage = img;
        upgradeIcon = upIcon;
        produceInterval = interval;
        timer = 0f;
        SetUpgradeBadgeActive(false);
        UpdateVisual();
    }

    public void SetLevelSprites(Sprite[] sprites)
    {
        levelSprites = sprites;
        UpdateVisual();
    }

    public Sprite GetLevelSprite(int level)
    {
        if (levelSprites != null && levelSprites.Length > 0)
        {
            int idx = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);
            if (idx < levelSprites.Length && levelSprites[idx] != null)
                return levelSprites[idx];
        }

        if (generatorType == TowerDefStructureType.EnergyGenerator)
        {
            string[] names = { "Pawn_Tower_01_Cyan", "Pawn_Tower_02_Blue", "Pawn_Tower_03_Purple", "Pawn_Tower_04_Red", "Pawn_Tower_05_Gold" };
            int nameIdx = Mathf.Clamp(level - 1, 0, names.Length - 1);
            Sprite spr = Resources.Load<Sprite>($"TowerDef/{names[nameIdx]}");
            if (spr != null) return spr;
            if (structureImage != null && structureImage.sprite != null) return structureImage.sprite;
            return Resources.Load<Sprite>("TowerDef/Pawn_Tower_03_Purple");
        }
        if (generatorType == TowerDefStructureType.CoreBed)
        {
            if (level > 1)
            {
                Sprite cyan = Resources.Load<Sprite>("TowerDef/Core_Pod_Cyan");
                if (cyan != null) return cyan;
            }
            if (structureImage != null && structureImage.sprite != null) return structureImage.sprite;
            return Resources.Load<Sprite>("TowerDef/Core_Pod_Green");
        }

        if (structureImage != null && structureImage.sprite != null)
            return structureImage.sprite;

        return null;
    }

    public void UpdateVisual()
    {
        if (structureImage != null)
        {
            Sprite spr = GetLevelSprite(structureLevel);
            if (spr != null) structureImage.sprite = spr;
        }
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
        if (IsMaxLevel) return false;
        int cost = UpgradeCost;
        if (gold < cost) return false;

        gold -= cost;
        structureLevel++;
        UpdateVisual();
        OnGeneratorUpgraded?.Invoke(structureLevel);
        return true;
    }

    public bool TryUpgradeFree()
    {
        if (IsMaxLevel) return false;

        structureLevel++;
        UpdateVisual();
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

    public void RefreshUpgradeBadge(int currentGold)
    {
        bool canUpgrade = (currentGold >= UpgradeCost) && !IsMaxLevel;
        SetUpgradeBadgeActive(canUpgrade);
    }
}
