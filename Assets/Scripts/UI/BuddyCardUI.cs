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

    public BuddyItemData BoundData => boundData;
    public BuddySlotState SlotState => slotState;
    public Image ProgressTrackImage => progressTrackImage;
    public Image ProgressFillImage => progressFillImage;

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
            Transform t = transform.Find("NormalContentGroup/DroneIcon") ?? transform.Find("DroneIcon") ?? transform.Find("Icon");
            if (t != null) droneIconImage = t.GetComponent<Image>();
        }
        if (levelText == null)
        {
            Transform t = transform.Find("NormalContentGroup/LevelText") ?? transform.Find("LevelText") ?? transform.Find("Level");
            if (t != null) levelText = t.GetComponent<TMP_Text>();
        }
        if (progressText == null)
        {
            Transform t = transform.Find("NormalContentGroup/BottomBar/ProgressText") ?? transform.Find("BottomBar/ProgressText") ?? transform.Find("ProgressText") ?? transform.Find("Quantity");
            if (t != null) progressText = t.GetComponent<TMP_Text>();
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

            // Remove any dark background track if previously generated
            Transform trackT = fillT.parent.Find(fillT.name + "_Track") ?? fillT.parent.Find("FillTrack") ?? fillT.parent.Find("Track");
            if (trackT != null)
            {
                if (Application.isPlaying) Destroy(trackT.gameObject);
                else DestroyImmediate(trackT.gameObject);
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

        if (progressFillImage != null)
        {
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

    public void UpdateProgressBar(float fillRatio)
    {
        EnsureProgressBar();
        fillRatio = Mathf.Clamp01(fillRatio);

        if (progressFillImage != null)
        {
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFillImage.fillClockwise = true;
            progressFillImage.fillAmount = fillRatio;
            progressFillImage.enabled = fillRatio > 0.001f;
        }

        if (progressText != null)
        {
            progressText.transform.SetAsLastSibling();
        }
    }

    public void UpdateProgressFromText()
    {
        if (progressText == null) return;
        string t = progressText.text?.Trim();
        if (string.IsNullOrEmpty(t)) return;

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
        }
    }

    public void SetQuantity(int current, int required)
    {
        ResolveReferences();
        if (progressText != null)
        {
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
        }

        if (droneIconImage != null)
        {
            if (iconSprite != null)
            {
                droneIconImage.sprite = iconSprite;
                droneIconImage.gameObject.SetActive(true);
            }
            else
            {
                droneIconImage.gameObject.SetActive(false);
            }
        }

        if (levelText != null && data != null)
        {
            levelText.text = $"LV.{data.level:00}";
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

        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(data != null && data.CanUpgrade);
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

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(true);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(false);

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
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

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(true);

        if (cardFrameImage != null)
        {
            cardFrameImage.raycastTarget = true;
            if (frameSprite != null) cardFrameImage.sprite = frameSprite;
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
            levelText.text = $"LV.{boundData.level:00}";
        }

        if (progressText != null)
        {
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

        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(boundData.CanUpgrade);
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
