using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị của 1 dòng thống kê Power-up / Chipset trong popup Damage Details.
/// Bao gồm thanh bar màu Cyan/Teal thể hiện tỷ lệ % sát thương, Icon, Tên, DPS, % Dmg, Tổng Dmg, Thời gian.
/// </summary>
public sealed class DamageDetailRowUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform rowRect;
    [SerializeField] private RectTransform progressFillRect;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dpsText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text timeText;

    private static readonly Color DefaultBarColor = new Color32(46, 125, 122, 235); // Teal / Cyan fill

    private void Awake()
    {
        if (rowRect == null) rowRect = GetComponent<RectTransform>();
    }

    public void Setup(
        Sprite icon,
        string chipName,
        int dps,
        float percent,
        long damage,
        string time)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
        {
            nameText.text = chipName;
        }

        if (dpsText != null)
        {
            dpsText.text = dps.ToString("N0");
        }

        if (percentText != null)
        {
            percentText.text = $"{percent:F1}%";
        }

        if (damageText != null)
        {
            damageText.text = damage.ToString("N0");
        }

        if (timeText != null)
        {
            timeText.text = time;
        }

        // Cập nhật độ rộng thanh tiến trình Teal (% sát thương)
        UpdateProgressBar(percent);
    }

    public void UpdateProgressBar(float percent)
    {
        float normalized = Mathf.Clamp01(percent / 100f);

        if (progressFillRect != null)
        {
            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(normalized, 1f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
        }

        if (progressFillImage != null && progressFillImage.color.a < 0.05f)
        {
            progressFillImage.color = DefaultBarColor;
        }
    }
}
