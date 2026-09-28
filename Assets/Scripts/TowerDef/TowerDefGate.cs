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
    [SerializeField] private Image gateImage;
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
        if (gateImage == null) gateImage = GetComponent<Image>();
        if (gateImage == null) gateImage = GetComponentInChildren<Image>(true);
        EnsureSpritesLoaded();
        currentHp = maxHp;
        UpdateGateVisual();
        UpdateHealthBarVisual();
        SetUpgradeBadgeActive(false);

        if (gateButton == null) gateButton = GetComponent<Button>();
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
        gateButton = btn != null ? btn : GetComponent<Button>();
        baseUpgradeCost = upgradeCost;
        hpIncrement = hpGainOnUpgrade;
        if (gateImage == null) gateImage = GetComponent<Image>();
        if (gateImage == null) gateImage = GetComponentInChildren<Image>(true);

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

    private Sprite LoadGateSprite(int level, string spriteName)
    {
        Sprite spr = Resources.Load<Sprite>("TowerDef/" + spriteName);
        if (spr != null) return spr;

        Sprite[] all = Resources.LoadAll<Sprite>("TowerDef/" + spriteName);
        if (all != null && all.Length > 0 && all[0] != null) return all[0];

#if UNITY_EDITOR
        string tilesDir = "Assets/Sprites/Mini game/Sliced/Tiles/";
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + spriteName + ".png");
        if (spr != null) return spr;

        string resDir = "Assets/Resources/TowerDef/";
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(resDir + spriteName + ".png");
        if (spr != null) return spr;
#endif

        if (TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.GateLevelSprites != null)
        {
            int idx = level - 1;
            if (idx >= 0 && idx < TowerDefGameManager.Instance.GateLevelSprites.Length)
            {
                if (TowerDefGameManager.Instance.GateLevelSprites[idx] != null)
                    return TowerDefGameManager.Instance.GateLevelSprites[idx];
            }
        }
        return null;
    }

    public void EnsureSpritesLoaded()
    {
        if (levelSprites == null || levelSprites.Length < 4)
        {
            Sprite[] newSprites = new Sprite[4];
            if (levelSprites != null)
            {
                for (int i = 0; i < Mathf.Min(levelSprites.Length, 4); i++)
                    newSprites[i] = levelSprites[i];
            }
            levelSprites = newSprites;
        }

        if (levelSprites[0] == null) levelSprites[0] = LoadGateSprite(1, "Gate_Metal");
        if (levelSprites[1] == null) levelSprites[1] = LoadGateSprite(2, "Gate_Cyan_Grid");
        if (levelSprites[2] == null) levelSprites[2] = LoadGateSprite(3, "Gate_Green_Wood");
        if (levelSprites[3] == null) levelSprites[3] = LoadGateSprite(4, "Gate_Blue_Wood");

        if (TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.GateLevelSprites != null)
        {
            Sprite[] gmSprites = TowerDefGameManager.Instance.GateLevelSprites;
            for (int i = 0; i < Mathf.Min(4, gmSprites.Length); i++)
            {
                if (levelSprites[i] == null && gmSprites[i] != null)
                    levelSprites[i] = gmSprites[i];
            }
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
            if (levelSprites == null || levelSprites.Length < 4)
            {
                levelSprites = new Sprite[4];
            }
            for (int i = 0; i < Mathf.Min(sprites.Length, 4); i++)
            {
                if (sprites[i] != null)
                {
                    levelSprites[i] = sprites[i];
                }
            }
        }
        EnsureSpritesLoaded();
        UpdateGateVisual();
    }

    public Sprite GetLevelSprite(int level)
    {
        EnsureSpritesLoaded();
        int idx = Mathf.Clamp(level - 1, 0, 3);
        if (levelSprites != null && idx < levelSprites.Length && levelSprites[idx] != null)
        {
            return levelSprites[idx];
        }

        string fallbackName;
        switch (level)
        {
            case 1: fallbackName = "Gate_Metal"; break;
            case 2: fallbackName = "Gate_Cyan_Grid"; break;
            case 3: fallbackName = "Gate_Green_Wood"; break;
            case 4: fallbackName = "Gate_Blue_Wood"; break;
            default: fallbackName = "Gate_Blue_Wood"; break;
        }
        Sprite spr = LoadGateSprite(level, fallbackName);
        if (spr != null)
        {
            if (levelSprites != null && idx < levelSprites.Length)
                levelSprites[idx] = spr;
            return spr;
        }

        if (gateImage == null) gateImage = GetComponent<Image>();
        return gateImage != null ? gateImage.sprite : null;
    }

    public void UpdateGateVisual()
    {
        EnsureSpritesLoaded();
        Sprite spr = GetLevelSprite(gateLevel);

        if (gateImage == null) gateImage = GetComponent<Image>();
        if (gateImage == null) gateImage = GetComponentInChildren<Image>(true);

        if (spr != null)
        {
            if (gateImage != null)
            {
                gateImage.sprite = spr;
                gateImage.overrideSprite = spr;
                gateImage.color = Color.white;
                gateImage.SetAllDirty();
            }
            Image direct = GetComponent<Image>();
            if (direct != null && direct != gateImage)
            {
                direct.sprite = spr;
                direct.overrideSprite = spr;
                direct.color = Color.white;
                direct.SetAllDirty();
            }
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spr;
                sr.color = Color.white;
            }
            Debug.Log($"[TowerDefGate] UpdateGateVisual: Level {gateLevel} -> Applied sprite '{spr.name}' on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[TowerDefGate] UpdateGateVisual: No sprite found for Gate Level {gateLevel} on {gameObject.name}!");
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
