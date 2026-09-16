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

    public void SetupEmpty(Sprite frameSprite, Action onEmptyClick = null)
    {
        ResolveReferences();
        boundData = null;
        slotState = BuddySlotState.Empty;
        onEmptySlotClicked = onEmptyClick;
        SetEquippedBadge(false);

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(true);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(false);

        if (normalContentGroup == null)
        {
            if (droneIconImage != null) droneIconImage.gameObject.SetActive(false);
            if (levelText != null) levelText.gameObject.SetActive(false);
            if (progressFillImage != null) progressFillImage.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
        }

        if (upgradeArrowGroup != null) upgradeArrowGroup.SetActive(false);

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
        SetEquippedBadge(false);

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(true);

        if (normalContentGroup == null)
        {
            if (droneIconImage != null) droneIconImage.gameObject.SetActive(false);
            if (levelText != null) levelText.gameObject.SetActive(false);
            if (progressFillImage != null) progressFillImage.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
        }

        if (upgradeArrowGroup != null) upgradeArrowGroup.SetActive(false);

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
