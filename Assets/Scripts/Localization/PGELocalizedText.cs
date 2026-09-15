using TMPro;
using UnityEngine;

/// <summary>
/// Tự động cập nhật văn bản của TMP_Text dựa theo localizationKey mỗi khi ngôn ngữ thay đổi.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class PGELocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [SerializeField] private string defaultFallback;

    private TMP_Text targetText;

    public string LocalizationKey
    {
        get => localizationKey;
        set
        {
            localizationKey = value;
            Refresh();
        }
    }

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        PGELocalization.OnLanguageChanged -= Refresh;
        PGELocalization.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PGELocalization.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText == null || string.IsNullOrEmpty(localizationKey))
            return;

        string fallback = !string.IsNullOrEmpty(defaultFallback) ? defaultFallback : targetText.text;
        targetText.text = PGELocalization.Get(localizationKey, fallback);
    }
}
