using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChipSlotState
{
    Normal = 0,
    Empty = 1
}

public class ChipsetCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image cardFrameImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject upgradeArrowGroup;
    [SerializeField] private GameObject starObject;
    [SerializeField] private Image bottomProgressBar;

    [Header("UI Groups")]
    [SerializeField] private GameObject normalContentGroup;
    [SerializeField] private GameObject emptySlotGroup;

    private ChipItemData boundData;
    private ChipSlotState slotState = ChipSlotState.Normal;
    private Action<ChipItemData> onCardClicked;
    private Action<ChipItemData> onUpgradeClicked;
    private Action onEmptySlotClicked;

    public ChipItemData BoundData => boundData;
    public ChipSlotState SlotState => slotState;

    private void Awake()
    {
        ResolveReferences();
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
        }

        if (progressText != null && data != null)
        {
            if (data.IsMaxOverall)
            {
                progressText.text = $"{data.count}";
                if (bottomProgressBar != null) bottomProgressBar.color = new Color32(232, 121, 249, 255); // Pink
            }
            else
            {
                if (bottomProgressBar != null) bottomProgressBar.color = new Color32(74, 222, 128, 255); // Bright Green
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
        }

        if (progressText != null)
        {
            if (boundData.IsMaxOverall)
            {
                progressText.text = $"{boundData.count}";
                if (bottomProgressBar != null) bottomProgressBar.color = new Color32(232, 121, 249, 255); // Pink
            }
            else
            {
                if (bottomProgressBar != null) bottomProgressBar.color = new Color32(74, 222, 128, 255); // Bright Green
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
        GameObject emptyGroup = null)
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
    }

    public void SetDirectVisual(Sprite frame, Sprite icon, string level, string progress, bool star, bool arrow)
    {
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
    }
}
