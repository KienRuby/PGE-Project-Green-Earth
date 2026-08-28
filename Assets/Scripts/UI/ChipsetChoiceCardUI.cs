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

    [Header("Frame Content Layout")]
    [Tooltip("Bật để giữ nguyên RectTransform của ChipIcon mà bạn chỉnh trực tiếp trong Hierarchy.")]
    [SerializeField] private bool useManualIconTransform = true;
    [Tooltip("Kích thước icon theo tỉ lệ vùng khung đang hiển thị. X = rộng, Y = cao.")]
    [SerializeField] private Vector2 iconNormalizedSize = new Vector2(0.78f, 0.48f);
    [Tooltip("Vị trí icon theo tỉ lệ vùng khung. X = trái/phải, Y = xuống/lên.")]
    [SerializeField] private Vector2 iconNormalizedOffset = new Vector2(-0.12254377f, 0.11511628f);
    [Tooltip("Tâm ngang của sprite cấp 1 và cấp 5. Các cấp giữa được chia đều.")]
    [SerializeField] private Vector2 levelCenterRange = new Vector2(0.174f, 0.858f);
    [Tooltip("Kích thước mỗi sprite cấp theo tỉ lệ vùng khung.")]
    [SerializeField] private Vector2 levelNormalizedSize = new Vector2(0.155f, 0.138f);
    [Tooltip("Khoảng cách từ đáy khung tới hàng sprite cấp, tính theo tỉ lệ chiều cao khung.")]
    [SerializeField] private float levelBottomNormalized = 0.216f;
    [Tooltip("Bật để giữ nguyên RectTransform của RuntimeLevelPip_1-5 mà bạn chỉnh trực tiếp trong Hierarchy.")]
    [SerializeField] private bool useManualLevelPipTransforms = true;

    private ChipItemData boundData;
    private Action<ChipItemData> onSelected;
    private readonly UnityEngine.UI.Image[] levelPipImages = new UnityEngine.UI.Image[5];
    private Material defaultFrameMaterial;
    private bool hasCapturedDefaultFrameMaterial;
    private Material defaultBackgroundMaterial;
    private Color defaultBackgroundColor;
    private bool hasCapturedDefaultBackgroundVisual;
    private bool redShimmerEnabled;

    public ChipItemData BoundData => boundData;
    public CanvasGroup RootCanvasGroup => canvasGroup;

    private void Awake()
    {
        if (selectButton == null) selectButton = GetComponent<UnityEngine.UI.Button>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        CaptureDefaultFrameMaterial();
        CaptureDefaultBackgroundVisual();
    }

    private void Update()
    {
        if (redShimmerEnabled)
        {
            ChipsetFrameShimmerMaterial.UpdateUnscaledAnimationClock();
        }
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

        ApplyFrameContentLayout();
        EnsureLevelPips(levelPipSprites);
        SetDisplayedRuntimeLevel(currentRuntimeLevel);

        // Runtime levels 1-5 do not unlock or recolour chipset frames. Tier-colour
        // unlocking belongs to the separate MainMenu chipset system.
        ApplyTierColors(ChipTier.Magic);
        SetRedTierBackgroundEffect(data != null && data.tier == ChipTier.Holographic);

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
            bool wasCreated = false;
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
                wasCreated = true;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(pipObject, $"Create {objectName}");
                }
#endif
            }

            if (wasCreated || !useManualLevelPipTransforms)
            {
                ConfigureLevelPipRect(pip.rectTransform, i);
            }
            pip.sprite = levelPipSprites != null && i < levelPipSprites.Length
                ? levelPipSprites[i]
                : null;
            pip.enabled = false;
            levelPipImages[i] = pip;
        }
    }

    /// <summary>
    /// Căn icon theo phần sprite khung thực sự được vẽ. IconFrame dùng
    /// preserveAspect nên vùng hiển thị có thể hẹp hơn RectTransform của nó.
    /// </summary>
    private void ApplyFrameContentLayout(bool force = false)
    {
        if ((!force && useManualIconTransform) || iconFrameImage == null || iconImage == null) return;

        Vector2 renderedFrameSize = GetRenderedFrameSize();
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(
            renderedFrameSize.x * iconNormalizedOffset.x,
            renderedFrameSize.y * iconNormalizedOffset.y);
        iconRect.sizeDelta = new Vector2(
            renderedFrameSize.x * iconNormalizedSize.x,
            renderedFrameSize.y * iconNormalizedSize.y);
        iconRect.localScale = Vector3.one;
    }

    private void ConfigureLevelPipRect(RectTransform pipRect, int pipIndex)
    {
        if (pipRect == null) return;

        Vector2 renderedFrameSize = GetRenderedFrameSize();
        float normalizedIndex = levelPipImages.Length <= 1
            ? 0.5f
            : pipIndex / (float)(levelPipImages.Length - 1);
        // Các slice "cấp 1" - "cấp 5" được vẽ để phủ đúng 5 ô ở đáy
        // khung chipset: tâm ô đầu/cuối lần lượt là 17.4% và 85.8%.
        float normalizedCenterX = Mathf.Lerp(levelCenterRange.x, levelCenterRange.y, normalizedIndex);
        float x = (normalizedCenterX - 0.5f) * renderedFrameSize.x;
        float renderedBottomInset = (iconFrameImage.rectTransform.rect.height - renderedFrameSize.y) * 0.5f;

        pipRect.anchorMin = pipRect.anchorMax = new Vector2(0.5f, 0f);
        pipRect.pivot = new Vector2(0.5f, 0f);
        pipRect.anchoredPosition = new Vector2(
            x,
            renderedBottomInset + renderedFrameSize.y * levelBottomNormalized);
        pipRect.sizeDelta = new Vector2(
            renderedFrameSize.x * levelNormalizedSize.x,
            renderedFrameSize.y * levelNormalizedSize.y);
        pipRect.localScale = Vector3.one;
    }

    private void OnValidate()
    {
        iconNormalizedSize.x = Mathf.Max(0.01f, iconNormalizedSize.x);
        iconNormalizedSize.y = Mathf.Max(0.01f, iconNormalizedSize.y);
        levelNormalizedSize.x = Mathf.Max(0.01f, levelNormalizedSize.x);
        levelNormalizedSize.y = Mathf.Max(0.01f, levelNormalizedSize.y);

        ApplyFrameContentLayout();
        if (iconFrameImage == null) return;

        for (int i = 0; i < levelPipImages.Length; i++)
        {
            Transform existing = iconFrameImage.transform.Find($"RuntimeLevelPip_{i + 1}");
            if (existing == null) continue;

            levelPipImages[i] = existing.GetComponent<UnityEngine.UI.Image>();
            if (!useManualLevelPipTransforms)
            {
                ConfigureLevelPipRect(existing as RectTransform, i);
            }
        }
    }

#if UNITY_EDITOR
    public void CreateEditableLevelPipsInEditor(Sprite[] sprites)
    {
        useManualLevelPipTransforms = true;
        EnsureLevelPips(sprites);
        for (int i = 0; i < levelPipImages.Length; i++)
        {
            UnityEngine.UI.Image pip = levelPipImages[i];
            if (pip == null) continue;

            pip.enabled = true;
            pip.color = Color.white;
            pip.rectTransform.localScale = Vector3.one;
            UnityEditor.EditorUtility.SetDirty(pip.gameObject);
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void ResetEditableLevelPipTransformsInEditor()
    {
        for (int i = 0; i < levelPipImages.Length; i++)
        {
            Transform existing = iconFrameImage != null
                ? iconFrameImage.transform.Find($"RuntimeLevelPip_{i + 1}")
                : null;
            if (existing == null) continue;

            UnityEditor.Undo.RecordObject(existing, "Reset level sprite RectTransform");
            ConfigureLevelPipRect(existing as RectTransform, i);
            UnityEditor.EditorUtility.SetDirty(existing);
        }
    }

    public void ResetEditableIconTransformInEditor()
    {
        if (iconImage == null) return;

        UnityEditor.Undo.RecordObject(iconImage.rectTransform, "Reset ChipIcon RectTransform");
        ApplyFrameContentLayout(true);
        UnityEditor.EditorUtility.SetDirty(iconImage.rectTransform);
    }
#endif

    private Vector2 GetRenderedFrameSize()
    {
        RectTransform frameRect = iconFrameImage.rectTransform;
        float rectWidth = Mathf.Max(1f, frameRect.rect.width);
        float rectHeight = Mathf.Max(1f, frameRect.rect.height);
        Sprite frameSprite = iconFrameImage.sprite;
        if (frameSprite == null || frameSprite.rect.height <= 0f)
        {
            return new Vector2(rectWidth, rectHeight);
        }

        float spriteAspect = frameSprite.rect.width / frameSprite.rect.height;
        float rectAspect = rectWidth / rectHeight;
        return rectAspect > spriteAspect
            ? new Vector2(rectHeight * spriteAspect, rectHeight)
            : new Vector2(rectWidth, rectWidth / spriteAspect);
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
        hasCapturedDefaultFrameMaterial = false;
        CaptureDefaultFrameMaterial();
        hasCapturedDefaultBackgroundVisual = false;
        CaptureDefaultBackgroundVisual();
    }

    private void CaptureDefaultFrameMaterial()
    {
        if (hasCapturedDefaultFrameMaterial || iconFrameImage == null) return;

        defaultFrameMaterial = iconFrameImage.material;
        hasCapturedDefaultFrameMaterial = true;
    }

    private void CaptureDefaultBackgroundVisual()
    {
        if (hasCapturedDefaultBackgroundVisual || backgroundImage == null) return;

        defaultBackgroundMaterial = backgroundImage.material;
        defaultBackgroundColor = backgroundImage.color;
        hasCapturedDefaultBackgroundVisual = true;
    }

    private void SetRedTierBackgroundEffect(bool enabled)
    {
        redShimmerEnabled = enabled;
        // Nền thẻ luôn tĩnh; chỉ khung icon chipset đỏ nhận shader lá cờ.
        CaptureDefaultFrameMaterial();
        if (iconFrameImage != null)
        {
            iconFrameImage.material = enabled
                ? ChipsetFrameShimmerMaterial.Get(iconFrameImage.sprite) ?? defaultFrameMaterial
                : defaultFrameMaterial;
        }

        if (backgroundImage == null) return;

        CaptureDefaultBackgroundVisual();
        backgroundImage.material = defaultBackgroundMaterial;
        backgroundImage.color = defaultBackgroundColor;
    }
}
