using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị thanh tiến trình bảo hiểm cho từng bậc (Elite, Epic, Legend)
/// trong giao diện PityGuaranteePanel.
/// </summary>
public class PityProgressRow : MonoBehaviour
{
    [Header("UI Text References")]
    [Tooltip("Text hiển thị tên bậc (ví dụ: ELITE, EPIC, LEGEND).")]
    [SerializeField] private TMP_Text tierNameText;

    [Tooltip("Text hiển thị tỷ lệ tiến độ hiện tại (ví dụ: 'ELITE — 3 / 5').")]
    [SerializeField] private TMP_Text counterText;

    [Tooltip("Text hiển thị số lượt còn thiếu hoặc thông báo đã kích hoạt (ví dụ: 'Còn 2 lượt').")]
    [SerializeField] private TMP_Text remainingText;

    [Header("Progress Bar References")]
    [Tooltip("Slider hiển thị thanh tiến trình tích lũy.")]
    [SerializeField] private Slider progressBarSlider;

    [Tooltip("Image fill bên trong thanh tiến trình để đồng bộ màu sắc theo bậc.")]
    [SerializeField] private Image progressBarFillImage;

    [Header("Badge & Visuals")]
    [Tooltip("Khối màu hoặc huy hiệu đại diện cho màu của bậc.")]
    [SerializeField] private Image tierBadgeImage;

    [Tooltip("Icon biểu tượng của bậc (tùy chọn).")]
    [SerializeField] private Image tierIconImage;

    /// <summary>
    /// Cập nhật hiển thị dòng tiến độ bảo hiểm dựa trên dữ liệu thật từ LabUpgradeController.
    /// </summary>
    /// <param name="tierName">Tên bậc (ELITE, EPIC, LEGEND)</param>
    /// <param name="currentCount">Bộ đếm tích lũy hiện tại</param>
    /// <param name="threshold">Ngưỡng kích hoạt bảo hiểm (từ cấu hình Inspector)</param>
    /// <param name="tierColor">Màu sắc chủ đạo của bậc</param>
    /// <param name="tierIcon">Sprite icon đại diện (tùy chọn)</param>
    public void Setup(string tierName, int currentCount, int threshold, Color tierColor, Sprite tierIcon = null)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(tierColor);

        // 1. Tên bậc (nếu có Text riêng)
        if (tierNameText != null)
        {
            tierNameText.text = tierName;
            tierNameText.color = tierColor;
        }

        // 2. Icon hoặc Huy hiệu màu
        if (tierBadgeImage != null)
        {
            tierBadgeImage.color = tierColor;
        }

        if (tierIconImage != null)
        {
            if (tierIcon != null)
            {
                tierIconImage.sprite = tierIcon;
                tierIconImage.gameObject.SetActive(true);
            }
            else
            {
                tierIconImage.gameObject.SetActive(false);
            }
        }

        // 3. Xử lý trường hợp tắt bảo hiểm
        if (threshold <= 0)
        {
            if (counterText != null)
            {
                counterText.text = "TẮT";
                counterText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            if (remainingText != null)
            {
                remainingText.text = "<color=#888888>Chưa bật</color>";
            }
            if (progressBarSlider != null)
            {
                if (progressBarSlider.fillRect == null && progressBarFillImage != null)
                {
                    progressBarSlider.fillRect = progressBarFillImage.rectTransform;
                }
                progressBarSlider.minValue = 0;
                progressBarSlider.maxValue = 1;
                progressBarSlider.value = 0;
            }
            if (progressBarFillImage != null && progressBarFillImage.type == Image.Type.Filled)
            {
                progressBarFillImage.fillAmount = 0f;
            }
            return;
        }

        // 4. Tính toán số lượt còn lại theo đúng công thức: remaining = max(0, threshold - currentCount)
        int remaining = Mathf.Max(0, threshold - currentCount);
        int displayCount = Mathf.Min(currentCount, threshold);

        // 5. Hiển thị tiến trình dạng "0 / 5" đúng layout giao diện
        if (counterText != null)
        {
            counterText.text = $"{displayCount} / {threshold}";
            counterText.color = tierColor;
        }

        // 6. Cập nhật Slider tiến độ và tự động liên kết fillRect nếu thiếu
        if (progressBarSlider != null)
        {
            if (progressBarSlider.fillRect == null && progressBarFillImage != null)
            {
                progressBarSlider.fillRect = progressBarFillImage.rectTransform;
            }
            progressBarSlider.minValue = 0;
            progressBarSlider.maxValue = threshold;
            progressBarSlider.value = displayCount;
        }

        if (progressBarFillImage != null)
        {
            progressBarFillImage.color = tierColor;
            if (progressBarFillImage.type == Image.Type.Filled)
            {
                progressBarFillImage.fillAmount = (float)displayCount / threshold;
            }
        }

        // 7. Hiển thị trạng thái lượt còn lại
        if (remainingText != null)
        {
            remainingText.color = tierColor;
            if (remaining <= 0 || currentCount >= threshold)
            {
                remainingText.text = "<color=#FFD700>★ ĐÃ KÍCH HOẠT BẢO HIỂM ★</color>";
            }
            else
            {
                remainingText.text = $"Còn {remaining} lượt";
            }
        }
    }
}
