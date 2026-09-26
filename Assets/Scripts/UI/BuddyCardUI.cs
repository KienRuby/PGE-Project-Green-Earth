using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum BuddySlotState
{
    Normal = 0,
    Empty = 1,
    Locked = 2
}

public class BuddyCardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image cardFrameImage;
    [SerializeField] private Image droneIconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject upgradeArrowGroup;
    [SerializeField] private GameObject equippedBadgeGroup;
    [SerializeField] private Sprite upgradeArrowSprite;
    [SerializeField] private GameObject normalContentGroup;
    [SerializeField] private GameObject emptySlotGroup;
    [SerializeField] private GameObject lockedSlotGroup;

    [Header("Equip Overlay")]
    [SerializeField] private GameObject equipOverlayGroup;
    [SerializeField] private Image equipDimOverlayImage;
    [SerializeField] private Image equipArrowImage;
    [SerializeField] private TMP_Text equipTextLabel;

    [Header("Inventory Selection")]
    [SerializeField] private CanvasGroup cardCanvasGroup;

    private bool isEquipTargetMode;
    private Action onTargetSlotClickedAction;

    [Header("Progress Bar Elements")]
    [SerializeField] private Image progressTrackImage;
    [SerializeField] private Image progressFillImage;

    private BuddyItemData boundData;
    private BuddySlotState slotState = BuddySlotState.Normal;
    private Action<BuddyItemData> onCardClicked;
    private Action<BuddyItemData> onUpgradeClicked;
    private Action onEmptySlotClicked;
    private Action onLockedSlotClicked;

    public const float StandardProgressFontSize = 26f;
    public const float StandardLevelFontSize = 22f;

    private static TMP_FontAsset cachedFont;
    private static Material cachedStrokeMaterial;

    public BuddyItemData BoundData => boundData;
    public BuddySlotState SlotState => slotState;
    public Image ProgressTrackImage => progressTrackImage;
    public Image ProgressFillImage => progressFillImage;
    public Image DroneIconImage => droneIconImage;
    public Image CardFrameImage => cardFrameImage;
    public GameObject UpgradeArrowGroup => upgradeArrowGroup;
    public GameObject EquippedBadgeGroup => equippedBadgeGroup;
    public TMP_Text LevelText => levelText;
    public TMP_Text ProgressText => progressText;

    public static void EnsureFontAndMaterial(TMP_Text text)
    {
        if (text == null) return;

        if (cachedFont == null)
        {
            if (text.font != null && text.font.name.IndexOf("Nunito", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cachedFont = text.font;
            }
#if UNITY_EDITOR
            if (cachedFont == null)
            {
                cachedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
            }
#endif
        }

        if (cachedStrokeMaterial == null)
        {
            if (text.fontSharedMaterial != null && text.fontSharedMaterial.name.IndexOf("Stroke", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cachedStrokeMaterial = text.fontSharedMaterial;
            }
#if UNITY_EDITOR
            if (cachedStrokeMaterial == null)
            {
                cachedStrokeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");
            }
#endif
        }

        if (cachedFont != null && text.font != cachedFont)
        {
            text.font = cachedFont;
        }

        if (cachedStrokeMaterial != null && text.fontSharedMaterial != cachedStrokeMaterial)
        {
            text.fontSharedMaterial = cachedStrokeMaterial;
        }
    }

    public static void ConfigureProgressText(TMP_Text text)
    {
        if (text == null) return;
        EnsureFontAndMaterial(text);
        text.color = Color.white;
        text.fontSize = StandardProgressFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.transform.localScale = Vector3.one;
    }

    public static void ConfigureLevelText(TMP_Text text)
    {
        if (text == null) return;
        EnsureFontAndMaterial(text);
        text.color = Color.white;
        text.fontSize = StandardLevelFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.transform.localScale = Vector3.one;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        if (slotState == BuddySlotState.Empty || slotState == BuddySlotState.Locked)
        {
            ApplyEmptyVisuals();
            return;
        }

        EnsureProgressBar();
        if (boundData != null)
        {
            float r = boundData.requiredCount > 0 ? (float)boundData.count / boundData.requiredCount : 1f;
            UpdateProgressBar(r);
        }
        else
        {
            UpdateProgressFromText();
        }
    }

    private void Update()
    {
        if (boundData != null && boundData.tier == BuddyTier.Holographic)
        {
            ChipsetFrameShimmerMaterial.UpdateUnscaledAnimationClock();
        }

        if (isEquipTargetMode && equipArrowImage != null)
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * 7.5f) * 6f;
            equipArrowImage.rectTransform.anchoredPosition = new Vector2(0f, 6f + offsetY);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                ResolveReferences();
                if (slotState == BuddySlotState.Empty || slotState == BuddySlotState.Locked)
                {
                    ApplyEmptyVisuals();
                    return;
                }
                EnsureProgressBar();
                if (boundData != null)
                {
                    float r = boundData.requiredCount > 0 ? (float)boundData.count / boundData.requiredCount : 1f;
                    UpdateProgressBar(r);
                }
                else
                {
                    UpdateProgressFromText();
                }
            };
        }
    }
#endif

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardButton != null) return;

        if (isEquipTargetMode)
        {
            onTargetSlotClickedAction?.Invoke();
            return;
        }

        if (slotState == BuddySlotState.Normal && boundData != null)
        {
            onCardClicked?.Invoke(boundData);
        }
        else if (slotState == BuddySlotState.Empty)
        {
            onEmptySlotClicked?.Invoke();
        }
        else if (slotState == BuddySlotState.Locked)
        {
            onLockedSlotClicked?.Invoke();
        }
    }

    private void ResolveReferences()
    {
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (cardFrameImage == null) cardFrameImage = GetComponent<Image>();
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
        if (lockedSlotGroup == null)
        {
            Transform t = transform.Find("LockedSlotGroup");
            if (t != null) lockedSlotGroup = t.gameObject;
        }
        if (droneIconImage == null)
        {
            Transform t = transform.Find("NormalContentGroup/DroneIcon")
                ?? transform.Find("NormalContentGroup/Icon")
                ?? transform.Find("DroneIcon")
                ?? transform.Find("Icon");
            if (t != null) droneIconImage = t.GetComponent<Image>();
            if (droneIconImage == null)
            {
                foreach (var img in GetComponentsInChildren<Image>(true))
                {
                    if (img == cardFrameImage || img == progressTrackImage || img == progressFillImage) continue;
                    if (img.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        img.name.IndexOf("Drone", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        droneIconImage = img;
                        break;
                    }
                }
            }
        }
        if (levelText == null)
        {
            Transform t = transform.Find("NormalContentGroup/LevelText") ?? transform.Find("LevelText") ?? transform.Find("Level");
            if (t != null) levelText = t.GetComponent<TMP_Text>();
        }
        if (progressText == null)
        {
            Transform t = transform.Find("NormalContentGroup/BottomBar/ProgressText") ?? transform.Find("BottomBar/ProgressText") ?? transform.Find("ProgressText") ?? transform.Find("Quantity") ?? transform.Find("Quantiry");
            if (t != null) progressText = t.GetComponent<TMP_Text>();
            if (progressText == null)
            {
                foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp != levelText)
                    {
                        progressText = tmp;
                        break;
                    }
                }
            }
        }
        if (upgradeArrowGroup == null)
        {
            Transform t = transform.Find("NormalContentGroup/UpgradeArrowGroup") ?? transform.Find("UpgradeArrowGroup");
            if (t != null) upgradeArrowGroup = t.gameObject;
        }
        if (upgradeButton == null && upgradeArrowGroup != null)
        {
            upgradeButton = upgradeArrowGroup.GetComponent<Button>();
        }

        if (droneIconImage != null) droneIconImage.raycastTarget = false;
        if (progressFillImage != null) progressFillImage.raycastTarget = false;
        if (progressTrackImage != null) progressTrackImage.raycastTarget = false;
        if (levelText != null)
        {
            levelText.raycastTarget = false;
            ConfigureLevelText(levelText);
        }
        if (progressText != null)
        {
            progressText.raycastTarget = false;
            ConfigureProgressText(progressText);
        }
        EnsureProgressBar();
        EnsureEquipOverlay();
    }

    public void EnsureProgressBar()
    {
        // 1. Structure with "Fill" GameObject (e.g. SlotIconBuddy)
        Transform fillT = transform.Find("Fill") ?? transform.Find("NormalContentGroup/Fill");
        if (fillT != null)
        {
            if (progressFillImage == null)
            {
                progressFillImage = fillT.GetComponent<Image>();
            }

            progressTrackImage = null;
        }
        else
        {
            // 2. Structure with "BottomBar" GameObject (e.g. BottomBar in prefab/builder)
            Transform barT = transform.Find("NormalContentGroup/BottomBar") ?? transform.Find("BottomBar");
            if (barT != null)
            {
                Transform childFillT = barT.Find("ProgressFill") ?? barT.Find("Fill");
                if (childFillT != null)
                {
                    progressFillImage = childFillT.GetComponent<Image>();
                }
                else
                {
                    progressFillImage = barT.GetComponent<Image>();
                }
            }
        }

        if (progressFillImage == null)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img != cardFrameImage && img != droneIconImage && img != progressTrackImage)
                {
                    if (img.name.Equals("Fill", StringComparison.OrdinalIgnoreCase) || img.name.Equals("ProgressFill", StringComparison.OrdinalIgnoreCase))
                    {
                        progressFillImage = img;
                        break;
                    }
                }
            }
        }

        if (progressFillImage != null)
        {
            progressFillImage.color = Color.white;
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFillImage.fillClockwise = true;
            progressFillImage.raycastTarget = false;
        }

        if (progressText != null)
        {
            progressText.transform.SetAsLastSibling();
        }
    }

    public void EnsureUpgradeArrow(Sprite customArrowSprite = null)
    {
        if (customArrowSprite != null)
        {
            upgradeArrowSprite = customArrowSprite;
        }

        if (upgradeArrowGroup == null)
        {
            Transform t = transform.Find("NormalContentGroup/UpgradeArrowGroup")
                ?? transform.Find("UpgradeArrowGroup")
                ?? transform.Find("NormalContentGroup/UpgradeArrow")
                ?? transform.Find("UpgradeArrow");
            if (t != null)
            {
                upgradeArrowGroup = t.gameObject;
            }
            else
            {
                Transform parent = normalContentGroup != null ? normalContentGroup.transform : transform;
                GameObject arrowObj = new GameObject("UpgradeArrowGroup", typeof(RectTransform));
                RectTransform rt = arrowObj.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = new Vector2(0.85f, 0.16f);
                rt.anchorMax = new Vector2(0.85f, 0.16f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(46f, 46f);

                Image img = arrowObj.AddComponent<Image>();
                img.raycastTarget = true;
                Sprite arrowSprite = upgradeArrowSprite;
#if UNITY_EDITOR
                if (arrowSprite == null)
                {
                    arrowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Chipset/Frames/badge-upgrade.png");
                }
#endif
                if (arrowSprite != null)
                {
                    img.sprite = arrowSprite;
                    img.color = Color.white;
                }
                else
                {
                    img.color = new Color(0.18f, 0.92f, 0.45f, 1f);
                }

                Button btn = arrowObj.AddComponent<Button>();
                btn.targetGraphic = img;

                upgradeArrowGroup = arrowObj;
                upgradeButton = btn;
            }
        }

        if (upgradeButton == null && upgradeArrowGroup != null)
        {
            upgradeButton = upgradeArrowGroup.GetComponent<Button>();
            if (upgradeButton == null)
            {
                upgradeButton = upgradeArrowGroup.AddComponent<Button>();
            }
        }
    }

    public void SetEquippedBadge(bool isEquipped)
    {
        if (equippedBadgeGroup == null)
        {
            Transform t = transform.Find("NormalContentGroup/EquippedBadge")
                ?? transform.Find("EquippedBadge");
            if (t != null)
            {
                equippedBadgeGroup = t.gameObject;
            }
            else if (isEquipped)
            {
                Transform parent = normalContentGroup != null ? normalContentGroup.transform : transform;
                GameObject badgeObj = new GameObject("EquippedBadge", typeof(RectTransform));
                RectTransform rt = badgeObj.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = new Vector2(0.5f, 0.94f);
                rt.anchorMax = new Vector2(0.5f, 0.94f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(130f, 28f);

                Image bg = badgeObj.AddComponent<Image>();
                bg.color = new Color(0.04f, 0.28f, 0.32f, 0.92f);
                bg.raycastTarget = false;

                GameObject textObj = new GameObject("BadgeText", typeof(RectTransform));
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.SetParent(badgeObj.transform, false);
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                textRt.anchoredPosition = Vector2.zero;

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                if (levelText != null && levelText.font != null)
                {
                    tmp.font = levelText.font;
                    if (levelText.fontSharedMaterial != null) tmp.fontSharedMaterial = levelText.fontSharedMaterial;
                }
                tmp.text = "EQUIPPED";
                tmp.fontSize = 15f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.25f, 0.95f, 0.85f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                equippedBadgeGroup = badgeObj;
            }
        }

        if (equippedBadgeGroup != null)
        {
            equippedBadgeGroup.SetActive(isEquipped);
            if (isEquipped)
            {
                equippedBadgeGroup.transform.SetAsLastSibling();
            }
        }
    }

    public void UpdateProgressBar(float fillRatio)
    {
        EnsureProgressBar();
        fillRatio = Mathf.Clamp01(fillRatio);

        if (progressFillImage != null)
        {
            progressFillImage.color = Color.white;
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFillImage.fillClockwise = true;
            progressFillImage.fillAmount = fillRatio;
            bool shouldShow = fillRatio > 0.001f;
            progressFillImage.enabled = shouldShow;
            if (progressText == null || progressFillImage.gameObject != progressText.transform.parent.gameObject)
            {
                progressFillImage.gameObject.SetActive(shouldShow);
            }
            else
            {
                progressFillImage.gameObject.SetActive(true);
            }
        }

        if (progressText != null)
        {
            ConfigureProgressText(progressText);
            progressText.transform.SetAsLastSibling();
        }
    }

    public void UpdateProgressFromText()
    {
        if (progressText == null) return;
        string t = progressText.text?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            UpdateProgressBar(0f);
            return;
        }

        if (t.Equals("MAX", StringComparison.OrdinalIgnoreCase))
        {
            UpdateProgressBar(1f);
            return;
        }

        string[] parts = t.Split('/');
        if (parts.Length == 2)
        {
            if (float.TryParse(parts[0].Trim(), out float cur) &&
                float.TryParse(parts[1].Trim(), out float req) && req > 0)
            {
                UpdateProgressBar(cur / req);
            }
            else
            {
                UpdateProgressBar(0f);
            }
        }
        else
        {
            UpdateProgressBar(0f);
        }
    }

    public void SetQuantity(int current, int required)
    {
        ResolveReferences();
        if (progressText != null)
        {
            ConfigureProgressText(progressText);
            progressText.text = required > 0 ? $"{current}/{required}" : "MAX";
        }
        float ratio = required > 0 ? (float)current / required : 1f;
        UpdateProgressBar(ratio);
    }

    public void SetProgress(float ratio)
    {
        UpdateProgressBar(ratio);
    }

    public void Setup(
        BuddyItemData data,
        Sprite iconSprite,
        Sprite frameSprite,
        Action<BuddyItemData> onCardClick = null,
        Action<BuddyItemData> onUpgradeClick = null)
    {
        ResolveReferences();
        boundData = data;
        slotState = BuddySlotState.Normal;
        onCardClicked = onCardClick;
        onUpgradeClicked = onUpgradeClick;
        isEquipTargetMode = false;
        if (equipOverlayGroup != null) equipOverlayGroup.SetActive(false);
        SetInventoryEquipState(false, false);

        if (normalContentGroup != null) normalContentGroup.SetActive(true);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(false);

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
            if (data != null && data.tier == BuddyTier.Holographic)
            {
                cardFrameImage.material = ChipsetFrameShimmerMaterial.Get(cardFrameImage.sprite);
            }
            else
            {
                cardFrameImage.material = null;
            }
        }

        if (droneIconImage != null)
        {
            droneIconImage.raycastTarget = false;
            if (iconSprite != null)
            {
                droneIconImage.sprite = iconSprite;
                droneIconImage.color = Color.white;
                droneIconImage.enabled = true;
                droneIconImage.gameObject.SetActive(true);
            }
            else
            {
                droneIconImage.gameObject.SetActive(false);
            }
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(data != null);
            if (data != null)
            {
                levelText.text = $"LV.{data.level:00}";
            }
        }

        if (progressFillImage != null)
        {
            progressFillImage.color = Color.white;
            progressFillImage.gameObject.SetActive(data != null);
        }

        if (progressText != null)
        {
            progressText.gameObject.SetActive(data != null);
        }

        if (data != null)
        {
            float fillRatio = 0f;
            if (data.requiredCount > 0)
            {
                if (progressText != null) progressText.text = $"{data.count}/{data.requiredCount}";
                fillRatio = (float)data.count / data.requiredCount;
            }
            else
            {
                if (progressText != null) progressText.text = "MAX";
                fillRatio = 1f;
            }
            UpdateProgressBar(fillRatio);
        }
        else
        {
            UpdateProgressFromText();
        }

        EnsureUpgradeArrow(upgradeArrowSprite);
        bool canUpgrade = data != null && data.CanUpgrade;
        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(canUpgrade);
            if (canUpgrade)
            {
                upgradeArrowGroup.transform.SetAsLastSibling();
            }
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

    public void ApplyEmptyVisuals()
    {
        SetEquippedBadge(false);

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(slotState == BuddySlotState.Empty);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(slotState == BuddySlotState.Locked);

        if (droneIconImage != null)
        {
            droneIconImage.enabled = false;
            droneIconImage.gameObject.SetActive(false);
        }

        if (levelText != null)
        {
            levelText.text = "";
            levelText.gameObject.SetActive(false);
        }

        if (progressText != null)
        {
            progressText.text = "";
            progressText.gameObject.SetActive(false);
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
            progressFillImage.enabled = false;
            progressFillImage.gameObject.SetActive(false);
        }

        if (progressTrackImage != null)
        {
            progressTrackImage.enabled = false;
            progressTrackImage.gameObject.SetActive(false);
        }

        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(false);
        }
    }

    public void SetupEmpty(Sprite frameSprite, Action onEmptyClick = null)
    {
        ResolveReferences();
        boundData = null;
        slotState = BuddySlotState.Empty;
        onEmptySlotClicked = onEmptyClick;
        ApplyEmptyVisuals();

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
            cardFrameImage.material = null;
        }

        if (cardButton != null)
        {
            cardButton.interactable = true;
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onEmptySlotClicked?.Invoke());
        }
    }

    public void SetupLocked(Sprite frameSprite, Action onLockedClick = null)
    {
        ResolveReferences();
        boundData = null;
        slotState = BuddySlotState.Locked;
        onLockedSlotClicked = onLockedClick;
        ApplyEmptyVisuals();

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
            cardFrameImage.material = null;
        }

        if (cardButton != null)
        {
            cardButton.interactable = true;
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onLockedSlotClicked?.Invoke());
        }
    }

    public void Refresh()
    {
        if (boundData == null || slotState != BuddySlotState.Normal) return;

        if (levelText != null)
        {
            ConfigureLevelText(levelText);
            levelText.text = $"LV.{boundData.level:00}";
        }

        if (progressText != null)
        {
            ConfigureProgressText(progressText);
            if (boundData.requiredCount > 0)
            {
                progressText.text = $"{boundData.count}/{boundData.requiredCount}";
            }
            else
            {
                progressText.text = "MAX";
            }
        }

        float ratio = boundData.requiredCount > 0 ? (float)boundData.count / boundData.requiredCount : 1f;
        UpdateProgressBar(ratio);

        if (cardFrameImage != null)
        {
            if (boundData.tier == BuddyTier.Holographic)
            {
                cardFrameImage.material = ChipsetFrameShimmerMaterial.Get(cardFrameImage.sprite);
            }
            else
            {
                cardFrameImage.material = null;
            }
        }

        EnsureUpgradeArrow(upgradeArrowSprite);
        if (upgradeArrowGroup != null)
        {
            bool canUpgrade = boundData.CanUpgrade;
            upgradeArrowGroup.SetActive(canUpgrade);
            if (canUpgrade)
            {
                upgradeArrowGroup.transform.SetAsLastSibling();
            }
        }
    }

    private static Sprite cachedEquipBadgeSprite;

    public static Sprite GetEquipBadgeSprite()
    {
        if (cachedEquipBadgeSprite != null && !cachedEquipBadgeSprite.name.Equals("Equip", StringComparison.OrdinalIgnoreCase))
            return cachedEquipBadgeSprite;

        cachedEquipBadgeSprite = Resources.Load<Sprite>("UI/Chipset/badge-equip-arrow");
#if UNITY_EDITOR
        if (cachedEquipBadgeSprite == null)
        {
            cachedEquipBadgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Chipset/badge-equip-arrow.png");
        }
#endif
        return cachedEquipBadgeSprite;
    }

    public void EnsureEquipOverlay()
    {
        if (equipOverlayGroup == null)
        {
            Transform t = transform.Find("EquipOverlay") 
                       ?? transform.Find("NormalContentGroup/EquipOverlay");
            if (t != null)
            {
                equipOverlayGroup = t.gameObject;
            }
            else
            {
                GameObject overlayObj = new GameObject("EquipOverlay", typeof(RectTransform));
                RectTransform rt = overlayObj.GetComponent<RectTransform>();
                rt.SetParent(transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                equipOverlayGroup = overlayObj;
            }
        }

        if (equipOverlayGroup != null)
        {
            RectTransform rootRt = equipOverlayGroup.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.pivot = new Vector2(0.5f, 0.5f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
            }

            Image rootImg = equipOverlayGroup.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.enabled = false;
            }

            // Layer 1: Semi-transparent Dim Overlay over the card
            if (equipDimOverlayImage == null)
            {
                Transform dimT = equipOverlayGroup.transform.Find("DimOverlay");
                if (dimT != null)
                {
                    equipDimOverlayImage = dimT.GetComponent<Image>();
                }
                else
                {
                    GameObject dimObj = new GameObject("DimOverlay", typeof(RectTransform));
                    RectTransform dimRt = dimObj.GetComponent<RectTransform>();
                    dimRt.SetParent(equipOverlayGroup.transform, false);
                    dimRt.anchorMin = Vector2.zero;
                    dimRt.anchorMax = Vector2.one;
                    dimRt.pivot = new Vector2(0.5f, 0.5f);
                    dimRt.offsetMin = Vector2.zero;
                    dimRt.offsetMax = Vector2.zero;

                    Image dimImg = dimObj.AddComponent<Image>();
                    dimImg.raycastTarget = false;
                    equipDimOverlayImage = dimImg;
                }
            }

            if (equipDimOverlayImage != null)
            {
                equipDimOverlayImage.raycastTarget = false;
                if (cardFrameImage != null && cardFrameImage.sprite != null)
                {
                    equipDimOverlayImage.sprite = cardFrameImage.sprite;
                    equipDimOverlayImage.type = cardFrameImage.type;
                }
                equipDimOverlayImage.color = new Color(0f, 0f, 0f, 0.5f);
            }

            // Layer 2: Yellow Arrow Badge with Crisp White Rounded Stroke
            if (equipArrowImage == null)
            {
                Transform arrowT = equipOverlayGroup.transform.Find("EquipArrow");
                if (arrowT != null)
                {
                    equipArrowImage = arrowT.GetComponent<Image>();
                }
                else
                {
                    GameObject arrowObj = new GameObject("EquipArrow", typeof(RectTransform));
                    RectTransform arrowRt = arrowObj.GetComponent<RectTransform>();
                    arrowRt.SetParent(equipOverlayGroup.transform, false);
                    arrowRt.anchorMin = new Vector2(0.5f, 1f);
                    arrowRt.anchorMax = new Vector2(0.5f, 1f);
                    arrowRt.pivot = new Vector2(0.5f, 1f);
                    arrowRt.anchoredPosition = new Vector2(0f, 6f);
                    arrowRt.sizeDelta = new Vector2(50f, 54f);

                    Image arrowImg = arrowObj.AddComponent<Image>();
                    arrowImg.raycastTarget = false;
                    arrowImg.preserveAspect = true;
                    equipArrowImage = arrowImg;
                }
            }

            if (equipArrowImage != null)
            {
                equipArrowImage.raycastTarget = false;
                equipArrowImage.preserveAspect = true;
                if (equipArrowImage.sprite == null || equipArrowImage.sprite.name.Equals("Equip", StringComparison.OrdinalIgnoreCase))
                {
                    equipArrowImage.sprite = GetEquipBadgeSprite();
                }
                equipArrowImage.color = Color.white;
            }

            // Layer 3: Bold White "Equip" Text with Thick Outline
            if (equipTextLabel == null)
            {
                Transform txtT = equipOverlayGroup.transform.Find("EquipText");
                if (txtT != null)
                {
                    equipTextLabel = txtT.GetComponent<TMP_Text>();
                }
                else
                {
                    GameObject txtObj = new GameObject("EquipText", typeof(RectTransform));
                    RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                    txtRt.SetParent(equipOverlayGroup.transform, false);
                    txtRt.anchorMin = new Vector2(0.5f, 0.5f);
                    txtRt.anchorMax = new Vector2(0.5f, 0.5f);
                    txtRt.pivot = new Vector2(0.5f, 0.5f);
                    txtRt.anchoredPosition = new Vector2(0f, -12f);
                    txtRt.sizeDelta = new Vector2(140f, 44f);

                    TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
                    tmp.raycastTarget = false;
                    tmp.text = "Equip";
                    tmp.fontSize = 30f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                    EnsureFontAndMaterial(tmp);
                    equipTextLabel = tmp;
                }
            }

            if (equipTextLabel != null)
            {
                equipTextLabel.raycastTarget = false;
                equipTextLabel.text = "Equip";
                equipTextLabel.fontSize = 30f;
                equipTextLabel.fontStyle = FontStyles.Bold;
                equipTextLabel.alignment = TextAlignmentOptions.Center;
                equipTextLabel.color = Color.white;
                EnsureFontAndMaterial(equipTextLabel);
            }
        }
    }

    public void SetEquipTargetMode(bool isTargetMode, Sprite equipSprite = null, Action onTargetSlotClicked = null)
    {
        EnsureEquipOverlay();
        isEquipTargetMode = isTargetMode;
        onTargetSlotClickedAction = onTargetSlotClicked;

        if (isTargetMode)
        {
            if (equipOverlayGroup != null)
            {
                equipOverlayGroup.SetActive(true);
                equipOverlayGroup.transform.SetAsLastSibling();
            }

            if (equipArrowImage != null)
            {
                if (equipSprite != null && !equipSprite.name.Equals("Equip", StringComparison.OrdinalIgnoreCase))
                    equipArrowImage.sprite = equipSprite;
                else
                    equipArrowImage.sprite = GetEquipBadgeSprite();

                equipArrowImage.color = Color.white;
                equipArrowImage.enabled = true;
            }

            if (equipDimOverlayImage != null)
            {
                if (cardFrameImage != null && cardFrameImage.sprite != null)
                {
                    equipDimOverlayImage.sprite = cardFrameImage.sprite;
                    equipDimOverlayImage.type = cardFrameImage.type;
                }
                equipDimOverlayImage.color = new Color(0f, 0f, 0f, 0.5f);
                equipDimOverlayImage.enabled = true;
            }

            if (equipTextLabel != null)
            {
                equipTextLabel.text = "Equip";
                equipTextLabel.enabled = true;
                EnsureFontAndMaterial(equipTextLabel);
            }

            if (slotState == BuddySlotState.Empty)
            {
                if (normalContentGroup != null) normalContentGroup.SetActive(false);
                if (emptySlotGroup != null) emptySlotGroup.SetActive(true);
            }
            else
            {
                if (normalContentGroup != null) normalContentGroup.SetActive(true);
                if (emptySlotGroup != null) emptySlotGroup.SetActive(false);

                if (droneIconImage != null && boundData != null) droneIconImage.gameObject.SetActive(true);
                if (levelText != null && boundData != null) levelText.gameObject.SetActive(true);
                if (upgradeArrowGroup != null && boundData != null)
                {
                    upgradeArrowGroup.SetActive(boundData.CanUpgrade);
                }
                if (progressFillImage != null && boundData != null) progressFillImage.gameObject.SetActive(true);
            }

            if (cardButton != null)
            {
                cardButton.interactable = true;
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => onTargetSlotClickedAction?.Invoke());
            }
        }
        else
        {
            if (equipOverlayGroup != null) equipOverlayGroup.SetActive(false);
            if (equipArrowImage != null) equipArrowImage.rectTransform.anchoredPosition = new Vector2(0f, 6f);

            if (slotState == BuddySlotState.Empty)
            {
                if (normalContentGroup != null) normalContentGroup.SetActive(false);
                if (emptySlotGroup != null) emptySlotGroup.SetActive(true);

                if (cardButton != null)
                {
                    cardButton.interactable = true;
                    cardButton.onClick.RemoveAllListeners();
                    cardButton.onClick.AddListener(() => onEmptySlotClicked?.Invoke());
                }
            }
            else if (slotState == BuddySlotState.Locked)
            {
                if (normalContentGroup != null) normalContentGroup.SetActive(false);
                if (lockedSlotGroup != null) lockedSlotGroup.SetActive(true);

                if (cardButton != null)
                {
                    cardButton.interactable = true;
                    cardButton.onClick.RemoveAllListeners();
                    cardButton.onClick.AddListener(() => onLockedSlotClicked?.Invoke());
                }
            }
            else
            {
                if (normalContentGroup != null) normalContentGroup.SetActive(true);
                if (emptySlotGroup != null) emptySlotGroup.SetActive(false);
                if (lockedSlotGroup != null) lockedSlotGroup.SetActive(false);

                if (droneIconImage != null && boundData != null) droneIconImage.gameObject.SetActive(true);
                if (levelText != null && boundData != null) levelText.gameObject.SetActive(true);
                if (upgradeArrowGroup != null && boundData != null)
                {
                    upgradeArrowGroup.SetActive(boundData.CanUpgrade);
                }

                if (cardButton != null)
                {
                    cardButton.interactable = true;
                    cardButton.onClick.RemoveAllListeners();
                    cardButton.onClick.AddListener(() => onCardClicked?.Invoke(boundData));
                }
            }
        }
    }

    public void EnsureCanvasGroup()
    {
        if (cardCanvasGroup == null)
        {
            cardCanvasGroup = GetComponent<CanvasGroup>();
            if (cardCanvasGroup == null)
            {
                cardCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public void SetInventoryEquipState(bool isEquipModeActive, bool isThisCardSelected)
    {
        EnsureCanvasGroup();

        Transform legacyBorder = transform.Find("SelectionBorder");
        if (legacyBorder != null)
        {
            if (Application.isPlaying)
                Destroy(legacyBorder.gameObject);
            else
                DestroyImmediate(legacyBorder.gameObject);
        }

        if (!isEquipModeActive)
        {
            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 1.0f;
                cardCanvasGroup.interactable = true;
                cardCanvasGroup.blocksRaycasts = true;
            }
            if (cardButton != null)
            {
                cardButton.interactable = true;
            }
            return;
        }

        if (isThisCardSelected)
        {
            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 1.0f;
                cardCanvasGroup.interactable = true;
                cardCanvasGroup.blocksRaycasts = true;
            }
            if (cardButton != null)
            {
                cardButton.interactable = true;
            }
        }
        else
        {
            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 0.42f;
                cardCanvasGroup.interactable = false;
                cardCanvasGroup.blocksRaycasts = false;
            }
            if (cardButton != null)
            {
                cardButton.interactable = false;
            }
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
        GameObject normalGroup,
        GameObject emptyGroup,
        GameObject lockedGroup)
    {
        this.cardFrameImage = frameImg;
        this.droneIconImage = iconImg;
        this.levelText = lvlText;
        this.progressText = prgText;
        this.cardButton = crdBtn;
        this.upgradeButton = upgBtn;
        this.upgradeArrowGroup = upgArrow;
        this.normalContentGroup = normalGroup;
        this.emptySlotGroup = emptyGroup;
        this.lockedSlotGroup = lockedGroup;
    }
}
