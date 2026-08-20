using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BuddySlotState
{
    Normal = 0,
    Empty = 1,
    Locked = 2
}

public class BuddyCardUI : MonoBehaviour
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

    private BuddyItemData boundData;
    private BuddySlotState slotState = BuddySlotState.Normal;
    private Action<BuddyItemData> onCardClicked;
    private Action<BuddyItemData> onUpgradeClicked;
    private Action onEmptySlotClicked;
    private Action onLockedSlotClicked;

    public BuddyItemData BoundData => boundData;
    public BuddySlotState SlotState => slotState;

    public void Setup(
        BuddyItemData data,
        Sprite iconSprite,
        Sprite frameSprite,
        Action<BuddyItemData> onCardClick = null,
        Action<BuddyItemData> onUpgradeClick = null)
    {
        boundData = data;
        slotState = BuddySlotState.Normal;
        onCardClicked = onCardClick;
        onUpgradeClicked = onUpgradeClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(true);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(false);

        if (cardFrameImage != null && frameSprite != null)
        {
            cardFrameImage.sprite = frameSprite;
        }

        if (droneIconImage != null && iconSprite != null)
        {
            droneIconImage.sprite = iconSprite;
            droneIconImage.gameObject.SetActive(true);
        }

        if (levelText != null)
        {
            levelText.text = $"LV.{data.level:00}";
        }

        if (progressText != null)
        {
            if (data.requiredCount > 0)
            {
                progressText.text = $"{data.count}/{data.requiredCount}";
            }
            else
            {
                progressText.text = "MAX";
            }
        }

        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(data.CanUpgrade);
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
        slotState = BuddySlotState.Empty;
        onEmptySlotClicked = onEmptyClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(true);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(false);

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

    public void SetupLocked(Sprite frameSprite, Action onLockedClick = null)
    {
        boundData = null;
        slotState = BuddySlotState.Locked;
        onLockedSlotClicked = onLockedClick;

        if (normalContentGroup != null) normalContentGroup.SetActive(false);
        if (emptySlotGroup != null) emptySlotGroup.SetActive(false);
        if (lockedSlotGroup != null) lockedSlotGroup.SetActive(true);

        if (cardFrameImage != null && frameSprite != null)
        {
            cardFrameImage.sprite = frameSprite;
        }

        if (cardButton != null)
        {
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
