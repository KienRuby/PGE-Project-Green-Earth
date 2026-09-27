using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Cổng / Cửa sắt phòng thủ của căn cứ (Gate_Metal):
/// - Có thanh máu xanh lá (Bar_Green).
/// - Nhận sát thương khi quái tiếp cận và tấn công.
/// - Cho phép sửa chữa (Repair) và nâng cấp (Upgrade) tăng Max HP và giáp.
/// - Phát sự kiện khi cổng bị phá hủy để xử lý thua trận.
/// </summary>
public class TowerDefGate : MonoBehaviour
{
    [Header("Gate Stats")]
    [SerializeField] private float maxHp = 50f;
    [SerializeField] private float currentHp = 50f;
    [SerializeField] private int gateLevel = 1;
    [SerializeField] private int baseUpgradeCost = 12;
    [SerializeField] private float hpIncrement = 20f;
    [SerializeField] private Sprite[] levelSprites;

    [Header("Visual References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject upgradeIcon;
    [SerializeField] private Button gateButton;

    public const int MAX_GATE_LEVEL = 4;
    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public int GateLevel => gateLevel;
    public int MaxLevel => MAX_GATE_LEVEL;
    public bool IsMaxLevel => gateLevel >= MAX_GATE_LEVEL;
    public int NextLevel => Mathf.Min(MAX_GATE_LEVEL, gateLevel + 1);
    public float NextMaxHp => maxHp + hpIncrement;
    public int UpgradeCost => IsMaxLevel ? 0 : baseUpgradeCost * gateLevel;
    public bool IsDestroyed => currentHp <= 0f;

    public event Action<float, float> OnHpChanged;
    public event Action OnGateDestroyed;
    public event Action<int> OnGateUpgraded;

    private void Awake()
    {
        EnsureSpritesLoaded();
        currentHp = maxHp;
        UpdateGateVisual();
        UpdateHealthBarVisual();
        SetUpgradeBadgeActive(false);

        if (gateButton != null)
        {
            gateButton.onClick.RemoveListener(HandleGateClicked);
            gateButton.onClick.AddListener(HandleGateClicked);
        }
    }

    public void Setup(float initialMaxHp, Image hpFill, GameObject upIcon, Button btn, int upgradeCost = 50, float hpGainOnUpgrade = 150f)
    {
        maxHp = initialMaxHp;
        currentHp = maxHp;
        healthBarFill = hpFill;
        upgradeIcon = upIcon;
        gateButton = btn;
        baseUpgradeCost = upgradeCost;
        hpIncrement = hpGainOnUpgrade;

        if (gateButton != null)
        {
            gateButton.onClick.RemoveListener(HandleGateClicked);
            gateButton.onClick.AddListener(HandleGateClicked);
        }

        EnsureSpritesLoaded();
        UpdateGateVisual();
        SetUpgradeBadgeActive(false);
        UpdateHealthBarVisual();
    }

    public void EnsureSpritesLoaded()
    {
        if (levelSprites == null || levelSprites.Length < 4 || levelSprites[0] == null)
        {
            levelSprites = new Sprite[4];
            levelSprites[0] = Resources.Load<Sprite>("TowerDef/Gate_Metal");
            levelSprites[1] = Resources.Load<Sprite>("TowerDef/Gate_Cyan_Grid");
            levelSprites[2] = Resources.Load<Sprite>("TowerDef/Gate_Green_Wood");
            levelSprites[3] = Resources.Load<Sprite>("TowerDef/Gate_Blue_Wood");

#if UNITY_EDITOR
            string tilesDir = "Assets/Sprites/Mini game/Sliced/Tiles/";
            if (levelSprites[0] == null) levelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Metal.png");
            if (levelSprites[1] == null) levelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Cyan_Grid.png");
            if (levelSprites[2] == null) levelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Green_Wood.png");
            if (levelSprites[3] == null) levelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Blue_Wood.png");
#endif
        }
    }

    public static string GetGateName(int level)
    {
        switch (level)
        {
            case 1: return "Cổng sắt";
            case 2: return "Cổng lưới lam";
            case 3: return "Cổng mạ lục";
            case 4: return "Cổng hợp kim";
            default: return $"Cổng cấp {level:D2}";
        }
    }

    public void SetLevelSprites(Sprite[] sprites)
    {
        if (sprites != null && sprites.Length > 0)
        {
            levelSprites = sprites;
        }
        EnsureSpritesLoaded();
        UpdateGateVisual();
    }

    public Sprite GetLevelSprite(int level)
    {
        EnsureSpritesLoaded();
        if (levelSprites != null && levelSprites.Length > 0)
        {
            int idx = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);
            if (idx < levelSprites.Length && levelSprites[idx] != null)
                return levelSprites[idx];
        }
        Image gateImg = GetComponent<Image>();
        return gateImg != null ? gateImg.sprite : null;
    }

    public void UpdateGateVisual()
    {
        EnsureSpritesLoaded();
        Image gateImg = GetComponent<Image>();
        if (gateImg != null)
        {
            Sprite spr = GetLevelSprite(gateLevel);
            if (spr != null)
            {
                gateImg.sprite = spr;
                gateImg.color = Color.white;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        UpdateHealthBarVisual();
        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0f)
        {
            Image gateImg = GetComponent<Image>();
            if (gateImg != null)
            {
                gateImg.color = new Color(0.6f, 0.2f, 0.2f, 1f);
            }
            OnGateDestroyed?.Invoke();
        }
    }

    public void Repair(float amount)
    {
        if (IsDestroyed) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        Image gateImg = GetComponent<Image>();
        if (gateImg != null) gateImg.color = Color.white;
        UpdateHealthBarVisual();
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public bool TryUpgrade(ref int gold)
    {
        if (IsMaxLevel) return false;
        int cost = UpgradeCost;
        if (gold < cost) return false;

        gold -= cost;
        gateLevel++;
        maxHp += hpIncrement;
        currentHp = maxHp; // Hồi đầy máu khi nâng cấp
        Image gateImg = GetComponent<Image>();
        if (gateImg != null) gateImg.color = Color.white;
        UpdateGateVisual();
        UpdateHealthBarVisual();
        OnHpChanged?.Invoke(currentHp, maxHp);
        OnGateUpgraded?.Invoke(gateLevel);
        return true;
    }

    public bool TryUpgradeFree()
    {
        if (IsMaxLevel) return false;

        gateLevel++;
        maxHp += hpIncrement;
        currentHp = maxHp; // Hồi đầy máu khi nâng cấp miễn phí
        Image gateImg = GetComponent<Image>();
        if (gateImg != null) gateImg.color = Color.white;
        UpdateGateVisual();
        UpdateHealthBarVisual();
        OnHpChanged?.Invoke(currentHp, maxHp);
        OnGateUpgraded?.Invoke(gateLevel);
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

    private void UpdateHealthBarVisual()
    {
        if (healthBarFill != null)
        {
            float ratio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            if (healthBarFill.type == Image.Type.Filled)
            {
                healthBarFill.fillAmount = ratio;
            }
            else
            {
                RectTransform rt = healthBarFill.rectTransform;
                if (rt != null)
                {
                    rt.localScale = new Vector3(ratio, 1f, 1f);
                }
            }
        }
    }

    private void HandleGateClicked()
    {
        TowerDefGameManager.Instance?.OpenGateUpgradePopup(this);
    }
}
