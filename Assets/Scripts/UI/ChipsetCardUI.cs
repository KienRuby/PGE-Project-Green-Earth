using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private ChipItemData boundData;
    private Action<ChipItemData> onCardClicked;
    private Action<ChipItemData> onUpgradeClicked;

    public ChipItemData BoundData => boundData;

    public void Setup(
        ChipItemData data,
        Sprite iconSprite,
        Sprite frameSprite,
        Action<ChipItemData> onCardClick = null,
        Action<ChipItemData> onUpgradeClick = null)
    {
        boundData = data;
        onCardClicked = onCardClick;
        onUpgradeClicked = onUpgradeClick;

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
                progressText.text = $"{data.count}";
            }
        }

        bool canUpgrade = data.CanUpgrade;
        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(canUpgrade);
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

    public void Refresh()
    {
        if (boundData == null) return;

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
                progressText.text = $"{boundData.count}";
            }
        }

        if (upgradeArrowGroup != null)
        {
            upgradeArrowGroup.SetActive(boundData.CanUpgrade);
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
        Image bottomBar)
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
    }

    public void SetDirectVisual(Sprite frame, Sprite icon, string level, string progress, bool star, bool arrow)
    {
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
