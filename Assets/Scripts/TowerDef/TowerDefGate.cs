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
    [SerializeField] private float maxHp = 300f;
    [SerializeField] private float currentHp = 300f;
    [SerializeField] private int gateLevel = 1;
    [SerializeField] private int baseUpgradeCost = 50;

    [Header("Visual References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject upgradeIcon;
    [SerializeField] private Button gateButton;

    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public int GateLevel => gateLevel;
    public int UpgradeCost => baseUpgradeCost * gateLevel;
    public bool IsDestroyed => currentHp <= 0f;

    public event Action<float, float> OnHpChanged;
    public event Action OnGateDestroyed;
    public event Action<int> OnGateUpgraded;

    private void Awake()
    {
        currentHp = maxHp;
        UpdateHealthBarVisual();

        if (gateButton != null)
        {
            gateButton.onClick.RemoveListener(HandleGateClicked);
            gateButton.onClick.AddListener(HandleGateClicked);
        }
    }

    public void Setup(float initialMaxHp, Image hpFill, GameObject upIcon, Button btn)
    {
        maxHp = initialMaxHp;
        currentHp = maxHp;
        healthBarFill = hpFill;
        upgradeIcon = upIcon;
        gateButton = btn;

        if (gateButton != null)
        {
            gateButton.onClick.RemoveListener(HandleGateClicked);
            gateButton.onClick.AddListener(HandleGateClicked);
        }

        UpdateHealthBarVisual();
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        UpdateHealthBarVisual();
        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0f)
        {
            OnGateDestroyed?.Invoke();
        }
    }

    public void Repair(float amount)
    {
        if (IsDestroyed) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        UpdateHealthBarVisual();
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public bool TryUpgrade(ref int gold)
    {
        int cost = UpgradeCost;
        if (gold < cost) return false;

        gold -= cost;
        gateLevel++;
        maxHp += 150f;
        currentHp = maxHp; // Full heal upon upgrade
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
                // Fallback using rectTransform scale
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
