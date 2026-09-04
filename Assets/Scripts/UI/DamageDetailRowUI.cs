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
    [SerializeField] private RectTransform progressBarTrack;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dpsText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text timeText;

    private static readonly Color DefaultBarColor = new Color32(78, 206, 196, 255); // Vibrant Cyan / Teal fill
    private static readonly Color DefaultTrackColor = new Color32(10, 24, 34, 255); // Dark track
    private static readonly Color DefaultCardColor = new Color32(13, 34, 46, 255);  // Card background

    private void Awake()
    {
        EnsureLayout();
    }

    public void EnsureLayout()
    {
        if (rowRect == null) rowRect = GetComponent<RectTransform>();
        if (rowRect != null)
        {
            rowRect.sizeDelta = new Vector2(768f, 74f);
        }

        Image rowImg = GetComponent<Image>();
        if (rowImg != null)
        {
            rowImg.color = DefaultCardColor;
        }

        // 1. Icon Frame & Image
        if (iconImage != null)
        {
            RectTransform iconRt = iconImage.rectTransform;
            RectTransform frameRt = iconRt.parent as RectTransform;
            if (frameRt != null && frameRt != transform)
            {
                frameRt.anchorMin = new Vector2(0.5f, 0.5f);
                frameRt.anchorMax = new Vector2(0.5f, 0.5f);
                frameRt.pivot = new Vector2(0.5f, 0.5f);
                frameRt.anchoredPosition = new Vector2(-325f, 0f);
                frameRt.sizeDelta = new Vector2(52f, 52f);

                Image frameImg = frameRt.GetComponent<Image>();
                if (frameImg != null)
                {
                    frameImg.color = new Color32(24, 64, 76, 255);
                }
            }
            iconImage.preserveAspect = true;
        }

        // 2. Name Text
        if (nameText != null)
        {
            RectTransform nRt = nameText.rectTransform;
            nRt.anchorMin = new Vector2(0.5f, 0.5f);
            nRt.anchorMax = new Vector2(0.5f, 0.5f);
            nRt.pivot = new Vector2(0.5f, 0.5f);
            nRt.anchoredPosition = new Vector2(-185f, 10f);
            nRt.sizeDelta = new Vector2(210f, 26f);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.fontSize = 19f;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 13f;
            nameText.fontSizeMax = 19f;
            nameText.color = Color.white;
            nameText.fontStyle = FontStyles.Bold;
        }

        // 3. Progress Bar Track & Fill
        if (progressBarTrack == null)
        {
            Transform existingTrack = transform.Find("ProgressBarTrack");
            if (existingTrack != null)
            {
                progressBarTrack = existingTrack.GetComponent<RectTransform>();
            }
            else
            {
                GameObject trackObj = new GameObject("ProgressBarTrack", typeof(RectTransform), typeof(Image));
                trackObj.transform.SetParent(transform, false);
                progressBarTrack = trackObj.GetComponent<RectTransform>();
            }
        }

        if (progressBarTrack != null)
        {
            progressBarTrack.anchorMin = new Vector2(0.5f, 0.5f);
            progressBarTrack.anchorMax = new Vector2(0.5f, 0.5f);
            progressBarTrack.pivot = new Vector2(0.5f, 0.5f);
            progressBarTrack.anchoredPosition = new Vector2(-185f, -14f);
            progressBarTrack.sizeDelta = new Vector2(210f, 8f);

            Image trackImg = progressBarTrack.GetComponent<Image>();
            if (trackImg != null)
            {
                trackImg.color = DefaultTrackColor;
            }

            if (progressFillRect != null && progressFillRect.parent != progressBarTrack)
            {
                progressFillRect.SetParent(progressBarTrack, false);
                progressFillRect.anchorMin = Vector2.zero;
                progressFillRect.anchorMax = new Vector2(0f, 1f);
                progressFillRect.offsetMin = Vector2.zero;
                progressFillRect.offsetMax = Vector2.zero;
            }
        }

        if (progressFillImage != null)
        {
            progressFillImage.color = DefaultBarColor;
        }

        // 4. Stats Columns
        FormatStatText(dpsText, new Vector2(0f, 0f), new Vector2(80f, 50f), 22f);
        FormatStatText(percentText, new Vector2(95f, 0f), new Vector2(80f, 50f), 22f);
        FormatStatText(damageText, new Vector2(205f, 0f), new Vector2(110f, 50f), 22f);
        FormatStatText(timeText, new Vector2(310f, 0f), new Vector2(80f, 50f), 22f);
    }

    private static void FormatStatText(TMP_Text text, Vector2 pos, Vector2 size, float fontSize)
    {
        if (text == null) return;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
    }

    public void Setup(
        Sprite icon,
        string chipName,
        int dps,
        float percent,
        long damage,
        string time)
    {
        EnsureLayout();

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

        // Cập nhật độ rộng thanh tiến trình Cyan (% sát thương)
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

        if (progressFillImage != null)
        {
            progressFillImage.color = DefaultBarColor;
        }
    }
}
