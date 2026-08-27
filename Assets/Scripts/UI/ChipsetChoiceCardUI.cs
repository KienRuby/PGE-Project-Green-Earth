using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Card ngang dùng riêng cho popup chọn Chipset khi lên cấp.
/// Sprite icon/frame được lấy từ cùng atlas với Chipset MainMenu.
/// </summary>
public class ChipsetChoiceCardUI : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private UnityEngine.UI.Image borderImage;
    [SerializeField] private UnityEngine.UI.Image backgroundImage;
    [SerializeField] private UnityEngine.UI.Image iconFrameImage;
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private UnityEngine.UI.Button selectButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private ChipItemData boundData;
    private Action<ChipItemData> onSelected;
    private readonly UnityEngine.UI.Image[] levelPipImages = new UnityEngine.UI.Image[5];

    public ChipItemData BoundData => boundData;
    public CanvasGroup RootCanvasGroup => canvasGroup;

    private void Awake()
    {
        if (selectButton == null) selectButton = GetComponent<UnityEngine.UI.Button>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(
        ChipItemData data,
        Sprite iconSprite,
        Sprite frameSprite,
        Sprite[] levelPipSprites,
        int currentRuntimeLevel,
        string offerDescription,
        Action<ChipItemData> selectionCallback)
    {
        boundData = data;
        onSelected = selectionCallback;

        if (titleText != null)
        {
            titleText.text = data != null ? $"{data.chipName} LV.{data.level:00}" : "CHIPSET";
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(offerDescription)
                ? data?.description ?? string.Empty
                : offerDescription;
        }

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
            iconImage.preserveAspect = true;
        }

        if (iconFrameImage != null)
        {
            iconFrameImage.sprite = frameSprite;
            iconFrameImage.enabled = frameSprite != null;
            iconFrameImage.preserveAspect = true;
        }

        EnsureLevelPips(levelPipSprites);
        SetDisplayedRuntimeLevel(currentRuntimeLevel);

        // Runtime levels 1-5 do not unlock or recolour chipset frames. Tier-colour
        // unlocking belongs to the separate MainMenu chipset system.
        ApplyTierColors(ChipTier.Magic);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
            selectButton.onClick.AddListener(HandleClicked);
            selectButton.interactable = data != null;
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (selectButton != null) selectButton.interactable = enabled && boundData != null;
    }

    private void HandleClicked()
    {
        if (boundData != null) onSelected?.Invoke(boundData);
    }

    public IEnumerator PlayLevelUpgradeFlash(int newLevel)
    {
        int pipIndex = Mathf.Clamp(newLevel, 1, levelPipImages.Length) - 1;
        SetDisplayedRuntimeLevel(newLevel - 1);

        UnityEngine.UI.Image pip = levelPipImages[pipIndex];
        if (pip == null) yield break;

        pip.enabled = true;
        const float duration = 0.52f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 34f);
            Color color = pip.color;
            color.a = Mathf.Lerp(0.25f, 1f, pulse);
            pip.color = color;
            pip.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.86f, 1.22f, pulse);
            yield return null;
        }

        pip.color = Color.white;
        pip.rectTransform.localScale = Vector3.one;
        SetDisplayedRuntimeLevel(newLevel);
    }

    private void EnsureLevelPips(Sprite[] levelPipSprites)
    {
        if (iconFrameImage == null) return;

        for (int i = 0; i < levelPipImages.Length; i++)
        {
            string objectName = $"RuntimeLevelPip_{i + 1}";
            Transform existing = iconFrameImage.transform.Find(objectName);
            UnityEngine.UI.Image pip;
            if (existing != null)
            {
                pip = existing.GetComponent<UnityEngine.UI.Image>();
            }
            else
            {
                GameObject pipObject = new GameObject(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                pipObject.transform.SetParent(iconFrameImage.transform, false);
                pip = pipObject.GetComponent<UnityEngine.UI.Image>();
                pip.raycastTarget = false;
                pip.preserveAspect = true;
                RectTransform rect = pip.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2((i - 2) * 21f, 8f);
                rect.sizeDelta = new Vector2(20f, 23f);
            }

            pip.sprite = levelPipSprites != null && i < levelPipSprites.Length
                ? levelPipSprites[i]
                : null;
            pip.enabled = false;
            levelPipImages[i] = pip;
        }
    }

    private void SetDisplayedRuntimeLevel(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 0, levelPipImages.Length);
        for (int i = 0; i < levelPipImages.Length; i++)
        {
            if (levelPipImages[i] == null) continue;
            levelPipImages[i].enabled = i < clampedLevel && levelPipImages[i].sprite != null;
            levelPipImages[i].color = Color.white;
            levelPipImages[i].rectTransform.localScale = Vector3.one;
        }
    }

    private void ApplyTierColors(ChipTier tier)
    {
        Color32 border;
        Color32 background;

        switch (tier)
        {
            case ChipTier.Rare:
                border = new Color32(143, 0, 255, 255);
                background = new Color32(21, 0, 88, 245);
                break;
            case ChipTier.Unique:
                border = new Color32(255, 75, 195, 255);
                background = new Color32(72, 8, 78, 245);
                break;
            case ChipTier.Epic:
                border = new Color32(255, 154, 38, 255);
                background = new Color32(83, 38, 8, 245);
                break;
            case ChipTier.Holographic:
                border = new Color32(255, 224, 75, 255);
                background = new Color32(37, 67, 73, 245);
                break;
            default:
                border = new Color32(116, 244, 239, 255);
                background = new Color32(10, 58, 83, 245);
                break;
        }

        if (borderImage != null) borderImage.color = border;
        if (backgroundImage != null) backgroundImage.color = background;
        if (titleText != null) titleText.color = new Color32(255, 177, 31, 255);
    }

    public void InitializeReferences(
        UnityEngine.UI.Image border,
        UnityEngine.UI.Image background,
        UnityEngine.UI.Image iconFrame,
        UnityEngine.UI.Image icon,
        TMP_Text title,
        TMP_Text description,
        UnityEngine.UI.Button button,
        CanvasGroup group)
    {
        borderImage = border;
        backgroundImage = background;
        iconFrameImage = iconFrame;
        iconImage = icon;
        titleText = title;
        descriptionText = description;
        selectButton = button;
        canvasGroup = group;
    }
}
