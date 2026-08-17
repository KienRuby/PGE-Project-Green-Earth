using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabUpgradeController : MonoBehaviour
{
    [Serializable]
    public class ItemEntry
    {
        [Header("Item Settings")]
        public string itemName;
        public Sprite itemIcon;

        [Min(0f)]
        [Tooltip("Tỉ lệ tương đối. Ví dụ 10 sẽ dễ trúng gấp đôi 5; đặt 0 để không thể quay trúng.")]
        public float dropWeight = 1f;

        public bool startsUnlocked;

        [Min(1)]
        public int startingLevel = 1;

        [Header("Slot View")]
        public GameObject lockedGroup;
        public GameObject unlockedGroup;
        public Image iconImage;
        public TMP_Text levelText;
        public TMP_Text nameText;
        public Image slotBackground;

        [NonSerialized] public int level;
    }

    [Header("Items & Chances")]
    [SerializeField] private ItemEntry[] items;

    [Header("UI References")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text chipBalanceText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image upgradeBackground;

    [Header("Economy")]
    [SerializeField] private int startingChips = 700;
    [SerializeField] private int basePrice = 300;
    [SerializeField] private int priceStep = 150;

    [Header("Roll Animation")]
    [Min(0.1f)]
    [SerializeField] private float rollDuration = 1.35f;

    [Min(0.02f)]
    [SerializeField] private float rollStep = 0.075f;

    [SerializeField] private Color rollHighlightColor = new Color32(255, 203, 73, 255);

    private static readonly Color AffordableColor = new Color32(84, 180, 105, 255);
    private static readonly Color UnaffordableColor = new Color32(67, 105, 109, 255);
    private static readonly Color LockedSlotColor = new Color32(31, 87, 94, 245);
    private static readonly Color UnlockedSlotColor = new Color32(48, 94, 111, 255);

    private int currentChips;
    private int currentPrice;
    private int completedRolls;
    private int pendingItemIndex = -1;
    private bool isRolling;

    private void Start()
    {
        currentChips = startingChips;
        currentPrice = basePrice;

        for (int i = 0; i < items.Length; i++)
        {
            ItemEntry item = items[i];
            item.level = item.startsUnlocked ? Mathf.Max(1, item.startingLevel) : 0;
            RefreshItemView(item);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(StartRoll);
        }

        if (resultText != null)
        {
            resultText.text = "ROLL FOR A RANDOM UPGRADE";
        }

        RefreshMainView();
    }

    private void OnDisable()
    {
        if (!isRolling || pendingItemIndex < 0)
        {
            return;
        }

        StopAllCoroutines();
        ResolvePendingItem();
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(StartRoll);
        }
    }

    private void StartRoll()
    {
        if (isRolling || currentChips < currentPrice)
        {
            return;
        }

        pendingItemIndex = ChooseWeightedItemIndex();
        if (pendingItemIndex < 0)
        {
            return;
        }

        currentChips -= currentPrice;
        isRolling = true;
        RefreshMainView();
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        float elapsed = 0f;
        int previousVisualIndex = -1;

        while (elapsed < rollDuration)
        {
            RestoreSlotColor(previousVisualIndex);

            int visualIndex = UnityEngine.Random.Range(0, items.Length);
            SetSlotColor(visualIndex, rollHighlightColor);
            previousVisualIndex = visualIndex;

            float progress = Mathf.Clamp01(elapsed / rollDuration);
            float delay = Mathf.Lerp(rollStep * 0.6f, rollStep * 2f, progress);
            yield return new WaitForSecondsRealtime(delay);
            elapsed += delay;
        }

        RestoreSlotColor(previousVisualIndex);
        SetSlotColor(pendingItemIndex, rollHighlightColor);
        yield return new WaitForSecondsRealtime(0.3f);
        ResolvePendingItem();
    }

    private int ChooseWeightedItemIndex()
    {
        float totalWeight = GetTotalWeight();
        if (totalWeight <= 0f)
        {
            return -1;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        int fallbackIndex = -1;
        for (int i = 0; i < items.Length; i++)
        {
            float weight = Mathf.Max(0f, items[i].dropWeight);
            if (weight <= 0f)
            {
                continue;
            }

            fallbackIndex = i;
            if (roll < weight)
            {
                return i;
            }

            roll -= weight;
        }

        return fallbackIndex;
    }

    private void ResolvePendingItem()
    {
        if (pendingItemIndex < 0 || pendingItemIndex >= items.Length)
        {
            FinishRoll();
            return;
        }

        ItemEntry item = items[pendingItemIndex];
        bool wasLocked = item.level <= 0;
        item.level = wasLocked ? 1 : item.level + 1;
        RefreshItemView(item);

        if (resultText != null)
        {
            resultText.text = wasLocked
                ? $"UNLOCKED  {item.itemName}"
                : $"{item.itemName}  LEVEL {item.level:00}";
        }

        completedRolls++;
        currentPrice = basePrice + completedRolls * priceStep;
        FinishRoll();
    }

    private void FinishRoll()
    {
        RestoreSlotColor(pendingItemIndex);
        pendingItemIndex = -1;
        isRolling = false;
        RefreshMainView();
    }

    private void RefreshItemView(ItemEntry item)
    {
        bool unlocked = item.level > 0;

        if (item.lockedGroup != null)
        {
            item.lockedGroup.SetActive(!unlocked);
        }

        if (item.unlockedGroup != null)
        {
            item.unlockedGroup.SetActive(unlocked);
        }

        if (item.iconImage != null)
        {
            item.iconImage.sprite = item.itemIcon;
        }

        if (item.levelText != null)
        {
            item.levelText.text = $"LV.{Mathf.Max(1, item.level):00}";
        }

        if (item.nameText != null)
        {
            item.nameText.text = item.itemName;
        }

        if (item.slotBackground != null)
        {
            item.slotBackground.color = unlocked ? UnlockedSlotColor : LockedSlotColor;
        }
    }

    private void RefreshMainView()
    {
        if (chipBalanceText != null)
        {
            chipBalanceText.text = currentChips.ToString();
        }

        if (priceText != null)
        {
            priceText.text = currentPrice.ToString();
        }

        bool canRoll = !isRolling && currentChips >= currentPrice && GetTotalWeight() > 0f;
        if (upgradeButton != null)
        {
            upgradeButton.interactable = canRoll;
        }

        if (upgradeBackground != null)
        {
            upgradeBackground.color = canRoll ? AffordableColor : UnaffordableColor;
        }
    }

    private float GetTotalWeight()
    {
        float totalWeight = 0f;
        for (int i = 0; i < items.Length; i++)
        {
            totalWeight += Mathf.Max(0f, items[i].dropWeight);
        }

        return totalWeight;
    }

    private void SetSlotColor(int itemIndex, Color color)
    {
        if (itemIndex < 0 || itemIndex >= items.Length || items[itemIndex].slotBackground == null)
        {
            return;
        }

        items[itemIndex].slotBackground.color = color;
    }

    private void RestoreSlotColor(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= items.Length)
        {
            return;
        }

        RefreshItemView(items[itemIndex]);
    }
}
