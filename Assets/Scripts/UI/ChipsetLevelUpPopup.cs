using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Popup chọn Chipset khi Player lên cấp trong Gameplay.
/// Dùng catalog/icon của MainMenu, dừng simulation và chạy UI bằng unscaled time.
/// </summary>
public class ChipsetLevelUpPopup : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private PlayerLevelController playerLevelController;
    [SerializeField, Min(1)] private int choicesPerLevel = 4;
    [SerializeField, Min(0)] private int rerollRedGemCost = 20;

    [Header("Popup References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private ChipsetChoiceCardUI[] choiceCards;
    [SerializeField] private UnityEngine.UI.Button rerollButton;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private UnityEngine.UI.Image rerollCurrencyIcon;

    [Header("MainMenu Chipset Assets")]
    [SerializeField] private Sprite[] chipIcons;
    [SerializeField] private Sprite[] frameSprites;
    [Tooltip("Thay sprite tại đây sau. Để trống sẽ dùng glyph placeholder.")]
    [SerializeField] private Sprite[] mechanicalParticleSprites;

    private readonly Queue<int> pendingLevels = new Queue<int>();
    private readonly Dictionary<int, int> runtimeChipLevels = new Dictionary<int, int>();
    private readonly List<ChipItemData> currentOffers = new List<ChipItemData>();
    private List<ChipItemData> catalog;
    private System.Random random;
    private Coroutine transitionRoutine;
    private bool isShowing;
    private bool acceptingInput;
    private bool ownsTimeScale;
    private float previousTimeScale = 1f;

    public static event Action<ChipItemData, int> OnRuntimeChipsetSelected;

    public bool IsShowing => isShowing;
    public IReadOnlyDictionary<int, int> RuntimeChipLevels => runtimeChipLevels;
    public IReadOnlyList<ChipItemData> CurrentOffers => currentOffers;

    private void Awake()
    {
        random = new System.Random(Environment.TickCount);
        catalog = ChipsetController.CreateDefaultDatabase();

        if (popupRoot != null && mechanicalParticleSprites != null && mechanicalParticleSprites.Length > 0)
        {
            ChipsetLevelUpParticleField particleField = popupRoot.GetComponentInChildren<ChipsetLevelUpParticleField>(true);
            if (particleField != null) particleField.SetParticleSprites(mechanicalParticleSprites);
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(HandleRerollClicked);
            rerollButton.onClick.AddListener(HandleRerollClicked);
        }

        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void Start()
    {
        ResolveAndSubscribeLevelController();
    }

    private void OnDestroy()
    {
        if (playerLevelController != null)
        {
            playerLevelController.OnLevelUp -= HandleLevelUp;
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(HandleRerollClicked);
        }

        RestoreTimeScale();
    }

    private void ResolveAndSubscribeLevelController()
    {
        if (playerLevelController == null)
        {
            playerLevelController = PlayerLevelController.Instance != null
                ? PlayerLevelController.Instance
                : FindObjectOfType<PlayerLevelController>();
        }

        if (playerLevelController != null)
        {
            playerLevelController.OnLevelUp -= HandleLevelUp;
            playerLevelController.OnLevelUp += HandleLevelUp;
        }
        else
        {
            Debug.LogWarning("[ChipsetLevelUpPopup] Không tìm thấy PlayerLevelController.");
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevels.Enqueue(newLevel);
        if (!isShowing) OpenNextLevelSelection();
    }

    private void OpenNextLevelSelection()
    {
        if (pendingLevels.Count == 0 || popupRoot == null) return;

        pendingLevels.Dequeue();
        isShowing = true;
        acceptingInput = false;

        if (!ownsTimeScale)
        {
            previousTimeScale = Time.timeScale;
            ownsTimeScale = true;
        }
        Time.timeScale = 0f;

        popupRoot.SetActive(true);
        GenerateOffers();

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(PlayOpenAnimation());
    }

    private void GenerateOffers()
    {
        currentOffers.Clear();
        currentOffers.AddRange(SelectDistinctOffers(catalog, Mathf.Min(choicesPerLevel, choiceCards?.Length ?? 0), random));

        for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
        {
            ChipsetChoiceCardUI card = choiceCards[i];
            if (card == null) continue;

            bool hasOffer = i < currentOffers.Count;
            card.gameObject.SetActive(hasOffer);
            if (!hasOffer) continue;

            ChipItemData offer = currentOffers[i].Clone();
            offer.level = GetRuntimeLevel(offer.id) + 1;
            currentOffers[i] = offer;

            card.Setup(
                offer,
                GetIconSprite(offer.iconKey),
                GetFrameSprite(offer.tier),
                GetOfferDescription(offer),
                HandleChipSelected);
            card.SetInteractionEnabled(false);
        }

        RefreshRerollState();
    }

    private IEnumerator PlayOpenAnimation()
    {
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.interactable = true;
        }
        if (titleTransform != null) titleTransform.localScale = Vector3.one * 0.78f;

        for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
        {
            if (choiceCards[i] == null || !choiceCards[i].gameObject.activeSelf) continue;
            choiceCards[i].transform.localScale = Vector3.one * 0.92f;
            if (choiceCards[i].RootCanvasGroup != null) choiceCards[i].RootCanvasGroup.alpha = 0f;
        }

        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = Mathf.Clamp01(t / 0.45f);
            if (titleTransform != null) titleTransform.localScale = Vector3.one * BackOut(t);

            for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
            {
                ChipsetChoiceCardUI card = choiceCards[i];
                if (card == null || !card.gameObject.activeSelf) continue;
                float cardT = Mathf.Clamp01((elapsed - 0.045f * i) / 0.18f);
                card.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, EaseOutCubic(cardT));
                if (card.RootCanvasGroup != null) card.RootCanvasGroup.alpha = cardT;
            }
            yield return null;
        }

        if (titleTransform != null) titleTransform.localScale = Vector3.one;
        if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;
        SetCardInteraction(true);
        acceptingInput = true;
        RefreshRerollState();

        if (EventSystem.current != null && choiceCards != null && choiceCards.Length > 0 && choiceCards[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(choiceCards[0].gameObject);
        }
        transitionRoutine = null;
    }

    private void HandleChipSelected(ChipItemData selected)
    {
        if (!acceptingInput || selected == null) return;

        acceptingInput = false;
        SetCardInteraction(false);
        runtimeChipLevels[selected.id] = selected.level;
        OnRuntimeChipsetSelected?.Invoke(selected.Clone(), selected.level);

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(PlayCloseAnimation());
    }

    private IEnumerator PlayCloseAnimation()
    {
        const float duration = 0.16f;
        float elapsed = 0f;
        float startAlpha = popupCanvasGroup != null ? popupCanvasGroup.alpha : 1f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        if (pendingLevels.Count > 0)
        {
            OpenNextLevelSelection();
            yield break;
        }

        if (popupRoot != null) popupRoot.SetActive(false);
        isShowing = false;
        transitionRoutine = null;
        RestoreTimeScale();
    }

    private void HandleRerollClicked()
    {
        if (!acceptingInput || rerollRedGemCost < 0) return;
        if (!ChipManager.TrySpendRedGems(rerollRedGemCost))
        {
            RefreshRerollState();
            return;
        }

        acceptingInput = false;
        SetCardInteraction(false);
        GenerateOffers();
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(PlayOpenAnimation());
    }

    private void RefreshRerollState()
    {
        if (rerollCostText != null) rerollCostText.text = $"x{rerollRedGemCost}  Draw again";
        if (rerollButton != null)
        {
            rerollButton.interactable = acceptingInput && ChipManager.HasEnoughRedGems(rerollRedGemCost);
        }
        if (rerollCurrencyIcon != null)
        {
            rerollCurrencyIcon.color = ChipManager.HasEnoughRedGems(rerollRedGemCost)
                ? new Color32(210, 48, 55, 255)
                : new Color32(95, 95, 95, 255);
        }
    }

    private void SetCardInteraction(bool enabled)
    {
        for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
        {
            if (choiceCards[i] != null) choiceCards[i].SetInteractionEnabled(enabled);
        }
    }

    private int GetRuntimeLevel(int chipId)
    {
        return runtimeChipLevels.TryGetValue(chipId, out int level) ? level : 0;
    }

    private Sprite GetIconSprite(string key)
    {
        if (chipIcons == null || chipIcons.Length == 0) return null;
        return chipIcons.FirstOrDefault(sprite => sprite != null && string.Equals(sprite.name, key, StringComparison.OrdinalIgnoreCase));
    }

    private Sprite GetFrameSprite(ChipTier tier)
    {
        if (frameSprites == null || frameSprites.Length == 0) return null;
        int index;
        switch (tier)
        {
            case ChipTier.Rare: index = 1; break;
            case ChipTier.Unique:
            case ChipTier.Epic: index = 2; break;
            case ChipTier.Holographic: index = 3; break;
            default: index = 0; break;
        }
        return frameSprites[Mathf.Clamp(index, 0, frameSprites.Length - 1)];
    }

    private static string GetOfferDescription(ChipItemData data)
    {
        switch (data.iconKey)
        {
            case "spiky-discus": return "Spins a spiky Discus to attack an enemy.";
            case "energy-jumper-cables": return "Stealing life from the enemies.";
            case "big-battery": return "A part that increases Max HP.";
            case "shotgun": return "Deals significant damage to nearby enemies with many shells.";
            default: return data.description;
        }
    }

    public static List<ChipItemData> SelectDistinctOffers(
        IReadOnlyList<ChipItemData> source,
        int count,
        System.Random rng)
    {
        var pool = source?.Where(item => item != null).Select(item => item.Clone()).ToList()
            ?? new List<ChipItemData>();
        var result = new List<ChipItemData>();
        if (rng == null) rng = new System.Random();

        while (pool.Count > 0 && result.Count < Mathf.Max(0, count))
        {
            double totalWeight = pool.Sum(GetOfferWeight);
            double roll = rng.NextDouble() * totalWeight;
            int selectedIndex = pool.Count - 1;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= GetOfferWeight(pool[i]);
                if (roll <= 0d)
                {
                    selectedIndex = i;
                    break;
                }
            }

            result.Add(pool[selectedIndex]);
            pool.RemoveAt(selectedIndex);
        }

        return result;
    }

    private static double GetOfferWeight(ChipItemData item)
    {
        switch (item.tier)
        {
            case ChipTier.Rare: return 0.55d;
            case ChipTier.Unique: return 0.28d;
            case ChipTier.Epic: return 0.14d;
            case ChipTier.Holographic: return 0.07d;
            default: return 1d;
        }
    }

    private void RestoreTimeScale()
    {
        if (!ownsTimeScale) return;
        Time.timeScale = previousTimeScale;
        ownsTimeScale = false;
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }

    private static float BackOut(float value)
    {
        float t = Mathf.Clamp01(value) - 1f;
        const float overshoot = 1.70158f;
        return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
    }

    public void InitializeReferences(
        PlayerLevelController levelController,
        GameObject root,
        CanvasGroup group,
        RectTransform title,
        ChipsetChoiceCardUI[] cards,
        UnityEngine.UI.Button drawAgainButton,
        TMP_Text drawAgainText,
        UnityEngine.UI.Image currencyIcon,
        Sprite[] icons,
        Sprite[] frames,
        Sprite[] particleSprites)
    {
        playerLevelController = levelController;
        popupRoot = root;
        popupCanvasGroup = group;
        titleTransform = title;
        choiceCards = cards;
        rerollButton = drawAgainButton;
        rerollCostText = drawAgainText;
        rerollCurrencyIcon = currencyIcon;
        chipIcons = icons;
        frameSprites = frames;
        mechanicalParticleSprites = particleSprites;
    }
}
