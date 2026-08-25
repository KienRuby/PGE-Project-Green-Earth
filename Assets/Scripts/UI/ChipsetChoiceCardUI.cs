using System;
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

        ApplyTierColors(data != null ? data.tier : ChipTier.Magic);

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
