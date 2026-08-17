using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabUpgradeController : MonoBehaviour
{
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text chipBalanceText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image upgradeBackground;
    [SerializeField] private int startingChips = 700;
    [SerializeField] private int startingLevel = 1;
    [SerializeField] private int basePrice = 300;
    [SerializeField] private int priceStep = 150;

    private static readonly Color AffordableColor = new Color32(84, 180, 105, 255);
    private static readonly Color UnaffordableColor = new Color32(67, 105, 109, 255);

    private int currentChips;
    private int currentLevel;
    private int currentPrice;

    private void Start()
    {
        currentChips = startingChips;
        currentLevel = startingLevel;
        currentPrice = basePrice;

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeDefence);
        }

        RefreshView();
    }

    private void UpgradeDefence()
    {
        if (currentChips < currentPrice)
        {
            return;
        }

        currentChips -= currentPrice;
        currentLevel++;
        currentPrice = basePrice + (currentLevel - 1) * priceStep;
        RefreshView();
    }

    private void RefreshView()
    {
        if (chipBalanceText != null)
        {
            chipBalanceText.text = currentChips.ToString();
        }

        if (levelText != null)
        {
            levelText.text = $"LV.{currentLevel:00}";
        }

        if (priceText != null)
        {
            priceText.text = currentPrice.ToString();
        }

        bool canAfford = currentChips >= currentPrice;
        if (upgradeButton != null)
        {
            upgradeButton.interactable = canAfford;
        }

        if (upgradeBackground != null)
        {
            upgradeBackground.color = canAfford ? AffordableColor : UnaffordableColor;
        }
    }
}
