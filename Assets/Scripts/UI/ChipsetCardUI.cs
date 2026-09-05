using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ChipSlotState
{
    Normal = 0,
    Empty = 1
}

public class ChipsetCardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image cardFrameImage;
    [SerializeField] private Image redEffectBackgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject upgradeArrowGroup;
    [SerializeField] private GameObject starObject;
    [SerializeField] private Image bottomProgressBar;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private RectTransform progressFillRect;

    [Header("UI Groups")]
    [SerializeField] private GameObject normalContentGroup;
    [SerializeField] private GameObject emptySlotGroup;

    private ChipItemData boundData;
    private ChipSlotState slotState = ChipSlotState.Normal;
    private Action<ChipItemData> onCardClicked;
    private Action<ChipItemData> onUpgradeClicked;
    private Action onEmptySlotClicked;
    private Material defaultFrameMaterial;
    private bool hasCapturedDefaultFrameMaterial;
    private Material defaultBackgroundMaterial;
    private Color defaultBackgroundColor;
    private bool hasCapturedDefaultBackgroundVisual;
    private bool toggleDedicatedRedBackground;
    private bool redShimmerEnabled;

    public ChipItemData BoundData => boundData;
    public ChipSlotState SlotState => slotState;
    public Image BottomProgressBar => bottomProgressBar;
    public Image ProgressFillImage => progressFillImage;
    public RectTransform ProgressFillRect => progressFillRect;

    private void Awake()
    {
        ResolveReferences();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotState == ChipSlotState.Normal && boundData != null)
        {
            onCardClicked?.Invoke(boundData);
        }
        else if (slotState == ChipSlotState.Empty)
        {
            onEmptySlotClicked?.Invoke();
        }
    }

    private void Update()
    {
        if (redShimmerEnabled)
        {
            ChipsetFrameShimmerMaterial.UpdateUnscaledAnimationClock();
        }
    }

    private void ResolveReferences()
    {
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (cardFrameImage == null) cardFrameImage = GetComponent<Image>();
        CaptureDefaultFrameMaterial();
        if (redEffectBackgroundImage == null)
        {
            Transform background = transform.Find("BackgroundRed") ?? transform.Find("Background");
            if (background != null)
            {
                redEffectBackgroundImage = background.GetComponent<Image>();
                toggleDedicatedRedBackground = string.Equals(
                    background.name,
                    "BackgroundRed",
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            toggleDedicatedRedBackground = string.Equals(
                redEffectBackgroundImage.name,
                "BackgroundRed",
                StringComparison.OrdinalIgnoreCase);
        }
        CaptureDefaultBackgroundVisual();
        if (normalContentGroup == null)
        {
            Transform t = transform.Find("NormalContentGroup");
            if (t != null) normalContentGroup = t.gameObject;
        }
        if (emptySlotGroup == null)
        {
            Transform t = transform.Find("EmptySlotGroup");
            if (t != null) emptySlotGroup = t.gameObject;
        }
        if (iconImage == null)
        {
            Transform t = transform.Find("NormalContentGroup/Icon") ?? transform.Find("Icon");
            if (t != null) iconImage = t.GetComponent<Image>();
        }
        if (levelText == null)
        {
            Transform t = transform.Find("NormalContentGroup/LevelText") ?? transform.Find("LevelText");
            if (t != null) levelText = t.GetComponent<TMP_Text>();
        }
        if (progressText == null)
        {
            Transform t = transform.Find("NormalContentGroup/BottomBar/ProgressText") ?? transform.Find("BottomBar/ProgressText") ?? transform.Find("ProgressText");
            if (t != null) progressText = t.GetComponent<TMP_Text>();
        }
        if (bottomProgressBar == null)
        {
            Transform t = transform.Find("NormalContentGroup/BottomBar") ?? transform.Find("BottomBar");
            if (t != null) bottomProgressBar = t.GetComponent<Image>();
        }
        if (progressFillRect == null && bottomProgressBar != null)
        {
            Transform fillT = bottomProgressBar.transform.Find("ProgressFill") ?? bottomProgressBar.transform.Find("Fill");
            if (fillT != null)
            {
                progressFillRect = fillT.GetComponent<RectTransform>();
                progressFillImage = fillT.GetComponent<Image>();
            }
        }
        EnsureProgressBar();
        if (upgradeArrowGroup == null)
        {
            Transform t = transform.Find("NormalContentGroup/UpgradeArrowGroup") ?? transform.Find("UpgradeArrowGroup");
            if (t != null) upgradeArrowGroup = t.gameObject;
        }
        if (upgradeButton == null && upgradeArrowGroup != null)
        {
            upgradeButton = upgradeArrowGroup.GetComponent<Button>();
        }
        if (starObject == null)
        {
            Transform t = transform.Find("NormalContentGroup/Star") ?? transform.Find("Star");
            if (t != null) starObject = t.gameObject;
        }
    }

    public void Setup(
        ChipItemData data,
        Sprite iconSprite,
        Sprite frameSprite,
        Action<ChipItemData> onCardClick = null,
        Action<ChipItemData> onUpgradeClick = null)
    {
        ResolveReferences();
        boundData = data;
        slotState = ChipSlotState.Normal;
        onCardClicked = onCardClick;
        onUpgradeClicked = onUpgradeClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(true);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
        }
        SetRedTierBackgroundEffect(data != null && data.tier == ChipTier.Holographic);

        if (iconImage != null)
        {
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (levelText != null && data != null)
        {
            if (data.IsMaxOverall)
            {
                levelText.text = $"LV.{data.level:00} MAX";
            }
            else if (data.IsTierUnlockReady)
            {
                levelText.text = $"LV.{data.level:00} CAP";
            }
            else
            {
                levelText.text = $"LV.{data.level:00}";
            }

            ConfigureLevelLabel(levelText, data.IsMaxOverall || data.IsTierUnlockReady);
        }

        EnsureProgressBar();

        ChipTier tier = data != null ? data.tier : ChipTier.Magic;
        if (bottomProgressBar != null)
        {
            bottomProgressBar.color = GetTierTrackColor(tier);
        }
        if (progressFillImage != null)
        {
            progressFillImage.color = GetTierProgressColor(tier);
        }

        if (progressText != null && data != null)
        {
            if (data.IsMaxOverall)
            {
                progressText.text = $"{data.count}";
            }
            else
            {
                if (data.requiredCount > 0)
                {
                    progressText.text = $"{data.count}/{data.requiredCount}";
                }
                else
                {
                    progressText.text = $"{data.count}";
                }
            }
        }

        float fillRatio = data != null ? CalculateFillRatio(data.count, data.requiredCount, data.IsMaxOverall) : 0f;
        UpdateProgressBar(fillRatio);

        bool hasAction = data != null && !data.IsMaxOverall && (data.CanUpgrade || data.CanAdvanceTier);
        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(hasAction);
        }

        if (starObject != null && data != null)
        {
            starObject.SetActive(data.hasStar);
        }

        if (cardButton != null)
        {
            cardButton.interactable = true;
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onCardClicked?.Invoke(boundData));
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = true;
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => onUpgradeClicked?.Invoke(boundData));
        }
    }

    public static void ConfigureLevelLabel(TMP_Text text, bool hasStatusSuffix)
    {
        if (text == null) return;

        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = hasStatusSuffix ? 18f : 22f;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = new Vector4(2f, 0f, 2f, 0f);
    }

    public static Color32 GetTierProgressColor(ChipTier tier)
    {
        switch (tier)
        {
            case ChipTier.Rare: return new Color32(56, 189, 248, 255);      // Blue
            case ChipTier.Unique: return new Color32(192, 132, 252, 255);  // Purple
            case ChipTier.Epic: return new Color32(250, 204, 21, 255);     // Yellow
            case ChipTier.Holographic: return new Color32(255, 77, 45, 255); // Red Tier
            default: return new Color32(74, 222, 128, 255);                // Green
        }
    }

    public static Color32 GetTierTrackColor(ChipTier tier)
    {
        switch (tier)
        {
            case ChipTier.Rare: return new Color32(12, 36, 56, 235);        // Dark Blue
            case ChipTier.Unique: return new Color32(36, 18, 54, 235);      // Dark Purple
            case ChipTier.Epic: return new Color32(48, 38, 12, 235);        // Dark Yellow/Brown
            case ChipTier.Holographic: return new Color32(48, 16, 16, 235); // Dark Red
            default: return new Color32(14, 38, 32, 235);                  // Dark Green
        }
    }

    public static void ConfigureProgressText(TMP_Text text)
    {
        if (text == null) return;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.25f;
        text.alignment = TextAlignmentOptions.Center;
    }

    public static float CalculateFillRatio(int count, int requiredCount, bool isMaxOverall = false)
    {
        if (isMaxOverall) return 1.0f;
        if (requiredCount <= 0) return count > 0 ? 1.0f : 0f;
        return Mathf.Clamp01((float)count / requiredCount);
    }

    public static float ParseFillRatioFromProgressText(string progress)
    {
        if (string.IsNullOrWhiteSpace(progress)) return 0f;
        if (string.Equals(progress.Trim(), "MAX", StringComparison.OrdinalIgnoreCase)) return 1.0f;

        string[] parts = progress.Split('/');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0].Trim(), out int count) && int.TryParse(parts[1].Trim(), out int required))
            {
                return CalculateFillRatio(count, required);
            }
        }
        else if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int countOnly))
        {
            return countOnly > 0 ? 1.0f : 0f;
        }

        return 0f;
    }

    public void EnsureProgressBar()
    {
        if (bottomProgressBar == null)
        {
            Transform barTransform = transform.Find("NormalContentGroup/BottomBar") ?? transform.Find("BottomBar");
            if (barTransform != null)
            {
                bottomProgressBar = barTransform.GetComponent<Image>();
            }
        }

        if (bottomProgressBar == null) return;

        ChipTier tier = boundData != null ? boundData.tier : ChipTier.Magic;
        bottomProgressBar.color = GetTierTrackColor(tier);

        if (progressFillRect == null || progressFillImage == null)
        {
            Transform fillT = bottomProgressBar.transform.Find("ProgressFill") ?? bottomProgressBar.transform.Find("Fill");
            if (fillT != null)
            {
                progressFillRect = fillT.GetComponent<RectTransform>();
                progressFillImage = fillT.GetComponent<Image>();
            }
            else
            {
                GameObject fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
                fillObj.transform.SetParent(bottomProgressBar.transform, false);
                progressFillRect = fillObj.GetComponent<RectTransform>();
                progressFillImage = fillObj.GetComponent<Image>();
                if (progressFillImage != null)
                {
                    progressFillImage.raycastTarget = false;
                }
            }
        }

        if (progressFillRect != null)
        {
            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(progressFillRect.anchorMax.x, 1f);
            progressFillRect.pivot = new Vector2(0f, 0.5f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
            progressFillRect.localScale = Vector3.one;
            progressFillRect.localRotation = Quaternion.identity;
        }

        if (progressFillImage != null)
        {
            progressFillImage.color = GetTierProgressColor(tier);
        }

        if (progressText == null)
        {
            Transform textT = bottomProgressBar.transform.Find("ProgressText") ?? transform.Find("NormalContentGroup/BottomBar/ProgressText") ?? transform.Find("ProgressText");
            if (textT != null) progressText = textT.GetComponent<TMP_Text>();
        }

        if (progressText != null)
        {
            progressText.transform.SetAsLastSibling();
            ConfigureProgressText(progressText);
        }
    }

    public void UpdateProgressBar(float fillRatio)
    {
        EnsureProgressBar();

        if (fillRatio > 1.0f)
        {
            fillRatio /= 100f;
        }
        fillRatio = Mathf.Clamp01(fillRatio);

        if (progressFillRect != null)
        {
            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(fillRatio, 1f);
            progressFillRect.pivot = new Vector2(0f, 0.5f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
        }

        if (progressFillImage != null)
        {
            progressFillImage.enabled = fillRatio > 0.001f;
        }
    }

    public void UseDetailBottomBarLayout()
    {
        ResolveReferences();
        if (bottomProgressBar != null)
        {
            ApplyDetailBottomBarLayout(bottomProgressBar.rectTransform);
        }
    }

    public static void ApplyDetailBottomBarLayout(RectTransform rect)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.08f, 0.10f);
        rect.anchorMax = new Vector2(0.92f, 0.25f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(0f, 28f);
        rect.offsetMax = new Vector2(0f, 28f);

        Vector3 anchoredPosition = rect.anchoredPosition3D;
        anchoredPosition.z = 0f;
        rect.anchoredPosition3D = anchoredPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = new Vector3(0.966f, 0.975f, 1f);
    }

    public void SetupEmpty(Sprite frameSprite, Action onEmptyClick = null)
    {
        ResolveReferences();
        boundData = null;
        slotState = ChipSlotState.Empty;
        onEmptySlotClicked = onEmptyClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(true);

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
        }
        SetRedTierBackgroundEffect(false);
        UpdateProgressBar(0f);

        if (cardButton != null)
        {
            cardButton.interactable = true;
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onEmptySlotClicked?.Invoke());
        }
    }

    public void Refresh()
    {
        if (boundData == null) return;

        if (levelText != null)
        {
            if (boundData.IsMaxOverall)
            {
                levelText.text = $"LV.{boundData.level:00} MAX";
            }
            else if (boundData.IsTierUnlockReady)
            {
                levelText.text = $"LV.{boundData.level:00} CAP";
            }
            else
            {
                levelText.text = $"LV.{boundData.level:00}";
            }

            ConfigureLevelLabel(levelText, boundData.IsMaxOverall || boundData.IsTierUnlockReady);
        }

        EnsureProgressBar();

        ChipTier tier = boundData.tier;
        if (bottomProgressBar != null)
        {
            bottomProgressBar.color = GetTierTrackColor(tier);
        }
        if (progressFillImage != null)
        {
            progressFillImage.color = GetTierProgressColor(tier);
        }

        SetRedTierBackgroundEffect(tier == ChipTier.Holographic);

        if (progressText != null)
        {
            if (boundData.IsMaxOverall)
            {
                progressText.text = $"{boundData.count}";
            }
            else
            {
                if (boundData.requiredCount > 0)
                {
                    progressText.text = $"{boundData.count}/{boundData.requiredCount}";
                }
                else
                {
                    progressText.text = $"{boundData.count}";
                }
            }
        }

        float fillRatio = CalculateFillRatio(boundData.count, boundData.requiredCount, boundData.IsMaxOverall);
        UpdateProgressBar(fillRatio);

        bool hasAction = !boundData.IsMaxOverall && (boundData.CanUpgrade || boundData.CanAdvanceTier);
        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(hasAction);
        }

        if (starObject != null)
        {
            starObject.SetActive(boundData.hasStar);
        }
    }

    public void InitializeReferences(
        Image frameImg,
        Image iconImg,
        TMP_Text lvlText,
        TMP_Text prgText,
        Button crdBtn,
        Button upgBtn,
        GameObject upgArrow,
        GameObject starObj,
        Image bottomBar,
        GameObject normalGroup = null,
        GameObject emptyGroup = null,
        Image fillImg = null,
        RectTransform fillRect = null)
    {
        this.cardFrameImage = frameImg;
        this.iconImage = iconImg;
        this.levelText = lvlText;
        this.progressText = prgText;
        this.cardButton = crdBtn;
        this.upgradeButton = upgBtn;
        this.upgradeArrowGroup = upgArrow;
        this.starObject = starObj;
        this.bottomProgressBar = bottomBar;
        this.normalContentGroup = normalGroup;
        this.emptySlotGroup = emptyGroup;
        this.progressFillImage = fillImg;
        this.progressFillRect = fillRect;
        hasCapturedDefaultFrameMaterial = false;
        CaptureDefaultFrameMaterial();
    }

    private Material defaultBottomBarMaterial;
    private bool hasCapturedDefaultBottomBarMaterial;

    private void CaptureDefaultFrameMaterial()
    {
        if (hasCapturedDefaultFrameMaterial || cardFrameImage == null) return;

        defaultFrameMaterial = cardFrameImage.material;
        hasCapturedDefaultFrameMaterial = true;
    }

    private void CaptureDefaultBottomBarMaterial()
    {
        if (hasCapturedDefaultBottomBarMaterial || bottomProgressBar == null) return;

        defaultBottomBarMaterial = bottomProgressBar.material;
        hasCapturedDefaultBottomBarMaterial = true;
    }

    private void CaptureDefaultBackgroundVisual()
    {
        if (hasCapturedDefaultBackgroundVisual || redEffectBackgroundImage == null) return;

        defaultBackgroundMaterial = redEffectBackgroundImage.material;
        defaultBackgroundColor = redEffectBackgroundImage.color;
        hasCapturedDefaultBackgroundVisual = true;
    }

    private void SetRedTierBackgroundEffect(bool enabled)
    {
        redShimmerEnabled = enabled;
        // BackGround1 luôn là nền tĩnh. Shader lá cờ chỉ áp dụng lên khung chipset đỏ.
        CaptureDefaultFrameMaterial();
        if (cardFrameImage != null)
        {
            cardFrameImage.material = enabled
                ? ChipsetFrameShimmerMaterial.Get(cardFrameImage.sprite) ?? defaultFrameMaterial
                : defaultFrameMaterial;
        }

        CaptureDefaultBottomBarMaterial();
        if (bottomProgressBar != null)
        {
            bottomProgressBar.material = defaultBottomBarMaterial;
        }

        if (redEffectBackgroundImage == null) return;

        CaptureDefaultBackgroundVisual();
        if (toggleDedicatedRedBackground)
        {
            redEffectBackgroundImage.gameObject.SetActive(enabled);
        }

        redEffectBackgroundImage.material = defaultBackgroundMaterial;
        redEffectBackgroundImage.color = defaultBackgroundColor;
    }

    public void SetDirectVisual(Sprite frame, Sprite icon, string level, string progress, bool star, bool arrow)
    {
        EnsureProgressBar();

        if (normalContentGroup != null) normalContentGroup.SetActive(true);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);

        if (cardFrameImage != null && frame != null) cardFrameImage.sprite = frame;
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(true);
        }
        if (levelText != null) levelText.text = level;
        if (progressText != null) progressText.text = progress;
        if (starObject != null) starObject.SetActive(star);
        if (upgradeArrowGroup != null) upgradeArrowGroup.SetActive(arrow);

        float fillRatio = ParseFillRatioFromProgressText(progress);
        UpdateProgressBar(fillRatio);
    }
}

public static class ChipsetFrameShimmerMaterial
{
    private static readonly Dictionary<int, Material> MaterialsBySprite = new Dictionary<int, Material>();
    private static readonly int UnscaledTimeId = Shader.PropertyToID("_ChipsetUnscaledTime");

    public static void UpdateUnscaledAnimationClock()
    {
        Shader.SetGlobalFloat(UnscaledTimeId, Time.unscaledTime);
    }

    public static Material Get(Sprite sprite)
    {
        int spriteId = sprite != null ? sprite.GetInstanceID() : 0;
        if (MaterialsBySprite.TryGetValue(spriteId, out Material existing) && existing != null)
        {
            return existing;
        }

        Shader shader = Resources.Load<Shader>("Shaders/ChipsetRedShimmer");
        if (shader == null) shader = Shader.Find("PGE/UI/Chipset Red Shimmer");
        if (shader == null) return null;

        Material material = new Material(shader)
        {
            name = "Chipset Red Shimmer (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };

        Vector4 uvRect = new Vector4(0f, 0f, 1f, 1f);
        if (sprite != null && sprite.uv != null && sprite.uv.Length > 0)
        {
            Vector2 min = sprite.uv[0];
            Vector2 max = sprite.uv[0];
            for (int i = 1; i < sprite.uv.Length; i++)
            {
                min = Vector2.Min(min, sprite.uv[i]);
                max = Vector2.Max(max, sprite.uv[i]);
            }

            Vector2 size = max - min;
            uvRect = new Vector4(min.x, min.y, Mathf.Max(0.0001f, size.x), Mathf.Max(0.0001f, size.y));
        }

        material.SetVector("_SpriteUVRect", uvRect);
        MaterialsBySprite[spriteId] = material;
        return material;
    }
}
