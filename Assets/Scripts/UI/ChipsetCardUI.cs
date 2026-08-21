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

    public void Setup(
        ChipItemData data,
        Sprite iconSprite,
        Sprite frameSprite,
        Action<ChipItemData> onCardClick = null,
        Action<ChipItemData> onUpgradeClick = null)
    {
        boundData = data;
        slotState = ChipSlotState.Normal;
        onCardClicked = onCardClick;
        onUpgradeClicked = onUpgradeClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(true);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);

        if (cardFrameImage != null && frameSprite != null)
        {
            cardFrameImage.sprite = frameSprite;
        }

        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.gameObject.SetActive(true);
        }

        if (levelText != null)
        {
            if (data.IsMaxOverall)
            {
                levelText.text = $"LV.{data.level:00} MAX";
            }
            else if (data.IsAtTierCap)
            {
                levelText.text = $"LV.{data.level:00} CAP";
            }
            else
            {
                levelText.text = $"LV.{data.level:00}";
            }
        }

        if (progressText != null)
        {
            if (data.IsMaxOverall)
            {
                progressText.text = "MAX";
            }
            else if (data.CanAdvanceTier)
            {
                progressText.text = data.NeedsAdvanceStones ? "10 STONES" : "ADVANCE";
            }
            else if (data.requiredCount > 0)
            {
                progressText.text = $"{data.count}/{data.requiredCount}";
            }
            else
            {
                progressText.text = $"{data.count}";
            }
        }

        bool hasAction = data.CanUpgrade || data.CanAdvanceTier;
        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(hasAction);
        }

        if (starObject != null)
        {
            starObject.SetActive(data.hasStar);
        }

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onCardClicked?.Invoke(boundData));
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => onUpgradeClicked?.Invoke(boundData));
        }
    }

    public void SetupEmpty(Sprite frameSprite, Action onEmptyClick = null)
    {
        boundData = null;
        slotState = ChipSlotState.Empty;
        onEmptySlotClicked = onEmptyClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(true);

        if (cardFrameImage != null && frameSprite != null)
        {
            cardFrameImage.sprite = frameSprite;
        }

        if (cardButton != null)
        {
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
            else if (boundData.IsAtTierCap)
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
                progressText.text = "MAX";
            }
            else if (boundData.CanAdvanceTier)
            {
                progressText.text = boundData.NeedsAdvanceStones ? "10 STONES" : "ADVANCE";
            }
            else if (boundData.requiredCount > 0)
            {
                progressText.text = $"{boundData.count}/{boundData.requiredCount}";
            }
            else
            {
                progressText.text = $"{boundData.count}";
            }
        }

        bool hasAction = boundData.CanUpgrade || boundData.CanAdvanceTier;
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
