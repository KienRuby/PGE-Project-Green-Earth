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
    public const int MaxRuntimeChipLevel = 5;
    private const int PrimaryChipsetCount = 10;

    [Header("Gameplay")]
    [SerializeField] private PlayerLevelController playerLevelController;
    [SerializeField, Min(1)] private int choicesPerLevel = 4;
    [SerializeField, Min(0)] private int rerollRedGemCost = 20;
    [SerializeField, Min(0)] private int maxRerollsPerLevel = 2;

    [Header("Popup References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private ChipsetChoiceCardUI[] choiceCards;
    [SerializeField] private UnityEngine.UI.Button rerollButton;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private UnityEngine.UI.Image rerollCurrencyIcon;
    [SerializeField] private RectTransform rerollTransform;
    [SerializeField] private CanvasGroup rerollCanvasGroup;

    [Header("MainMenu Chipset Assets")]
    [SerializeField] private ChipsetLevelVisualLibrary visualLibrary;
    [SerializeField] private Sprite[] chipIcons;
    [SerializeField] private Sprite[] frameSprites;
    [SerializeField] private Sprite[] levelPipSprites;
    [Tooltip("Thay sprite tại đây sau. Để trống sẽ dùng glyph placeholder.")]
    [SerializeField] private Sprite[] mechanicalParticleSprites;

    private readonly Queue<int> pendingLevels = new Queue<int>();
    private readonly Dictionary<int, int> runtimeChipLevels = new Dictionary<int, int>();
    private readonly List<ChipItemData> currentOffers = new List<ChipItemData>();
    private List<ChipItemData> catalog;
    private System.Random random;
    private Coroutine transitionRoutine;
    private int currentRerollCount;
    private bool isShowing;
    private bool acceptingInput;
    private bool ownsTimeScale;
    private bool victoryLocked;
    private float previousTimeScale = 1f;

    public static event Action<ChipItemData, int> OnRuntimeChipsetSelected;

    public bool IsShowing => isShowing;
    public bool IsVictoryLocked => victoryLocked;
    public IReadOnlyDictionary<int, int> RuntimeChipLevels => runtimeChipLevels;
    public IReadOnlyList<ChipItemData> CurrentOffers => currentOffers;
    public int MaxRerollsPerLevel
    {
        get => maxRerollsPerLevel;
        set => maxRerollsPerLevel = Mathf.Max(0, value);
    }
    public int CurrentRerollCount => currentRerollCount;
    public int RemainingRerolls => Mathf.Max(0, maxRerollsPerLevel - currentRerollCount);

    private void Awake()
    {
        random = new System.Random(Environment.TickCount);
        if (visualLibrary == null)
        {
            visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        }
        catalog = CreateRuntimeCatalog();

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
        if (victoryLocked) return;

        pendingLevels.Enqueue(newLevel);
        if (!isShowing) OpenNextLevelSelection();
    }

    /// <summary>
    /// Hủy mọi lựa chọn lên cấp đang mở/chờ khi trận đấu đã kết thúc.
    /// Không khôi phục timeScale vì VictoryPanel sẽ sở hữu trạng thái pause từ thời điểm này.
    /// </summary>
    public void CancelForVictory()
    {
        victoryLocked = true;
        pendingLevels.Clear();
        acceptingInput = false;
        isShowing = false;
        SetCardInteraction(false);

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (popupRoot != null) popupRoot.SetActive(false);
        ownsTimeScale = false;
    }

    private void OpenNextLevelSelection()
    {
        if (victoryLocked || pendingLevels.Count == 0 || popupRoot == null) return;

        // Đọc lại preset và tiến trình mới nhất trước mỗi lần lên cấp.
        catalog = CreateRuntimeCatalog();
        pendingLevels.Dequeue();
        if (!catalog.Any(item => item != null && GetRuntimeLevel(item.id) < MaxRuntimeChipLevel))
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            isShowing = false;
            acceptingInput = false;
            RestoreTimeScale();
            return;
        }
        currentRerollCount = 0;
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
        List<ChipItemData> eligibleCatalog = catalog
            .Where(item => item != null && GetRuntimeLevel(item.id) < MaxRuntimeChipLevel)
            .ToList();
        currentOffers.AddRange(SelectDistinctOffers(eligibleCatalog, Mathf.Min(choicesPerLevel, choiceCards?.Length ?? 0), random));

        for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
        {
            ChipsetChoiceCardUI card = choiceCards[i];
            if (card == null) continue;

            bool hasOffer = i < currentOffers.Count;
            card.gameObject.SetActive(hasOffer);
            if (!hasOffer) continue;

            ChipItemData offer = currentOffers[i].Clone();
            int currentRuntimeLevel = GetRuntimeLevel(offer.id);
            offer.level = Mathf.Clamp(currentRuntimeLevel + 1, 1, MaxRuntimeChipLevel);
            currentOffers[i] = offer;

            card.Setup(
                offer,
                GetIconSprite(offer.iconKey),
                GetGameplayLeverFrameSprite(offer.tier),
                GetLevelPipSprites(),
                currentRuntimeLevel,
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
        if (rerollTransform != null) rerollTransform.localScale = Vector3.one * 0.82f;
        if (rerollCanvasGroup != null) rerollCanvasGroup.alpha = 0f;

        for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
        {
            if (choiceCards[i] == null || !choiceCards[i].gameObject.activeSelf) continue;
            choiceCards[i].transform.localScale = Vector3.one * 0.92f;
            if (choiceCards[i].RootCanvasGroup != null) choiceCards[i].RootCanvasGroup.alpha = 0f;
        }

        const float duration = 0.46f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = Mathf.Clamp01(t / 0.45f);
            float titleT = Mathf.Clamp01(elapsed / 0.28f);
            if (titleTransform != null) titleTransform.localScale = Vector3.one * BackOut(titleT);

            for (int i = 0; i < (choiceCards?.Length ?? 0); i++)
            {
                ChipsetChoiceCardUI card = choiceCards[i];
                if (card == null || !card.gameObject.activeSelf) continue;
                float cardT = Mathf.Clamp01((elapsed - 0.045f * i) / 0.18f);
                card.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, EaseOutCubic(cardT));
                if (card.RootCanvasGroup != null) card.RootCanvasGroup.alpha = cardT;
            }

            float rerollT = Mathf.Clamp01((elapsed - 0.25f) / 0.18f);
            if (rerollTransform != null)
            {
                rerollTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, BackOut(rerollT));
            }
            if (rerollCanvasGroup != null) rerollCanvasGroup.alpha = rerollT;
            yield return null;
        }

        if (titleTransform != null) titleTransform.localScale = Vector3.one;
        if (rerollTransform != null) rerollTransform.localScale = Vector3.one;
        if (rerollCanvasGroup != null) rerollCanvasGroup.alpha = 1f;
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
        int newLevel = UpgradeRuntimeChipset(selected.id);
        selected.level = newLevel;
        OnRuntimeChipsetSelected?.Invoke(selected.Clone(), newLevel);

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        ChipsetChoiceCardUI selectedCard = choiceCards?.FirstOrDefault(card =>
            card != null && ReferenceEquals(card.BoundData, selected));
        transitionRoutine = StartCoroutine(PlaySelectionThenClose(selectedCard, newLevel));
    }

    private IEnumerator PlaySelectionThenClose(ChipsetChoiceCardUI selectedCard, int newLevel)
    {
        if (selectedCard != null)
        {
            yield return selectedCard.PlayLevelUpgradeFlash(newLevel);
        }

        yield return PlayCloseAnimation();
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

    public bool TryReroll()
    {
        if (currentRerollCount >= maxRerollsPerLevel) return false;
        if (rerollRedGemCost > 0 && !ChipManager.TrySpendRedGems(rerollRedGemCost))
        {
            RefreshRerollState();
            return false;
        }

        currentRerollCount++;
        GenerateOffers();
        RefreshRerollState();
        return true;
    }

    private void HandleRerollClicked()
    {
        if (!acceptingInput || rerollRedGemCost < 0) return;
        if (currentRerollCount >= maxRerollsPerLevel)
        {
            RefreshRerollState();
            return;
        }
        if (!ChipManager.TrySpendRedGems(rerollRedGemCost))
        {
            RefreshRerollState();
            return;
        }

        currentRerollCount++;
        acceptingInput = false;
        SetCardInteraction(false);
        GenerateOffers();
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(PlayOpenAnimation());
    }

    private void RefreshRerollState()
    {
        bool hasRerollsLeft = currentRerollCount < maxRerollsPerLevel;
        bool hasGems = ChipManager.HasEnoughRedGems(rerollRedGemCost);
        bool canReroll = acceptingInput && hasRerollsLeft && hasGems;

        if (rerollCostText != null)
        {
            rerollCostText.text = $"x{rerollRedGemCost}  Draw again";
        }
        if (rerollButton != null)
        {
            rerollButton.interactable = canReroll;
        }
        if (rerollCurrencyIcon != null)
        {
            rerollCurrencyIcon.color = (hasRerollsLeft && hasGems)
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

    public int GetRuntimeLevel(int chipId)
    {
        if (runtimeChipLevels.TryGetValue(chipId, out int level)) return level;
        return 0;
    }

    public int UpgradeRuntimeChipset(int chipId)
    {
        int newLevel = Mathf.Clamp(GetRuntimeLevel(chipId) + 1, 1, MaxRuntimeChipLevel);
        runtimeChipLevels[chipId] = newLevel;
        return newLevel;
    }

    private Sprite GetIconSprite(string key)
    {
        Sprite[] availableIcons = visualLibrary != null && visualLibrary.primaryChipIcons != null && visualLibrary.primaryChipIcons.Length > 0
            ? visualLibrary.primaryChipIcons
            : chipIcons;
        return FindMatchingIcon(availableIcons, key);
    }

    public static Sprite FindMatchingIcon(Sprite[] availableIcons, string key)
    {
        if (availableIcons == null || availableIcons.Length == 0) return null;
        if (string.IsNullOrWhiteSpace(key)) return availableIcons.FirstOrDefault(sprite => sprite != null);

        string cleanKey = NormalizeSpriteName(key);

        // The source atlas uses bilingual names such as "Rifle (Súng Trường)", while
        // gameplay keys are short slugs such as "rifle". Match the normalized prefix too.
        Sprite match = availableIcons.FirstOrDefault(sprite => sprite != null && (
            string.Equals(sprite.name, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizeSpriteName(sprite.name), cleanKey, StringComparison.Ordinal) ||
            NormalizeSpriteName(sprite.name).StartsWith(cleanKey, StringComparison.Ordinal)
        ));
        if (match != null) return match;

        // 2. Numeric sub-sprite mapping for icon chipset (1..10)
        string numKey = null;
        if (cleanKey.Contains("highexplosive") || cleanKey.Contains("mine") && !cleanKey.Contains("blackhole") && !cleanKey.Contains("biochemical")) numKey = "1";
        else if (cleanKey.Contains("energyjumper") || cleanKey.Contains("jumpercable")) numKey = "2";
        else if (cleanKey.Contains("shotgun")) numKey = "3";
        else if (cleanKey.Contains("spiky") || cleanKey.Contains("discus") || cleanKey.Contains("spicky")) numKey = "4";
        else if (cleanKey.Contains("gunturret") || cleanKey.Equals("turret")) numKey = "5";
        else if (cleanKey.Contains("multigun")) numKey = "6";
        else if (cleanKey.Contains("spinningblade") || cleanKey.Contains("blade")) numKey = "7";
        else if (cleanKey.Contains("rocketpunch") || cleanKey.Contains("punch")) numKey = "8";
        else if (cleanKey.Contains("standardgun") || cleanKey.Equals("gun") || cleanKey.Equals("pistol")) numKey = "9";
        else if (cleanKey.Contains("rifle") || cleanKey.Contains("assault")) numKey = "10";

        if (!string.IsNullOrEmpty(numKey))
        {
            match = availableIcons.FirstOrDefault(s => s != null && s.name == numKey);
            if (match != null) return match;
        }

        return availableIcons[0];
    }

    private static string NormalizeSpriteName(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .ToLowerInvariant();
    }

    private Sprite GetGameplayLeverFrameSprite(ChipTier tier)
    {
        Sprite[] availableFrames = visualLibrary != null && visualLibrary.tierLeverFrames != null && visualLibrary.tierLeverFrames.Length > 0
            ? visualLibrary.tierLeverFrames
            : frameSprites;
        return ResolveLeverFrameForTier(availableFrames, tier);
    }

    public static Sprite ResolveLeverFrameForTier(Sprite[] availableFrames, ChipTier tier)
    {
        if (availableFrames == null || availableFrames.Length == 0) return null;

        int frameIndex = ChipsetController.GetFrameIndex(tier);
        if (frameIndex < availableFrames.Length && availableFrames[frameIndex] != null)
        {
            return availableFrames[frameIndex];
        }

        return ResolveGreenLeverFrame(availableFrames);
    }

    public static Sprite ResolveGreenLeverFrame(Sprite[] availableFrames)
    {
        if (availableFrames == null || availableFrames.Length == 0) return null;
        return availableFrames.FirstOrDefault(frame => frame != null && frame.name == "ChipsetLeverGreen")
            ?? availableFrames.FirstOrDefault(frame => frame != null);
    }

    private Sprite[] GetLevelPipSprites()
    {
        return visualLibrary != null && visualLibrary.levelPipSprites != null && visualLibrary.levelPipSprites.Length > 0
            ? visualLibrary.levelPipSprites
            : levelPipSprites;
    }

    private static string GetOfferDescription(ChipItemData data)
    {
        if (data == null) return string.Empty;

        if (data.id == 1 || (data.iconKey != null && (data.iconKey.Contains("standard") || data.iconKey == "chipset_0")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Automatically fires support shots.";
                case 2: return "Increases damage and fire rate.";
                case 3: return "Shots gain a 10% chance to deal critical damage (2x).";
                case 4: return "Standard Gun gains 5% life steal.";
                case 5: return "Ultimate: Shots ricochet to one nearby enemy after the first hit.";
            }
        }

        if (data.id == 2 || (data.iconKey != null && (data.iconKey.Contains("rifle") || data.iconKey == "chipset_1")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Targets and fires at the nearest enemy.";
                case 2: return "Increases damage and fire rate.";
                case 3: return "Shots gain a 20% chance to pierce one enemy.";
                case 4: return "Shots always pierce one additional enemy.";
                case 5: return "Ultimate: Fires two parallel shots, doubling firepower.";
            }
        }

        if (data.id == 8 || (data.iconKey != null && (data.iconKey.Contains("shotgun") || data.iconKey == "chipset_7")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Fires a devastating close-range spread.";
                case 2: return "Improves reload speed and damage.";
                case 3: return "Narrows the spread and makes pellets pierce all targets.";
                case 4: return "Adds powerful knockback.";
                case 5: return "Ultimate: Fires two consecutive blasts without extra cooldown.";
            }
        }

        if (data.id == 5 || (data.iconKey != null && (data.iconKey.Contains("multigun") || data.iconKey == "chipset_4")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Unleashes a burst of shots in random forward directions.";
                case 2: return "Fires more shots with less time between bursts.";
                case 3: return "Shots lightly home in on targets.";
                case 4: return "Further increases the volume of fire.";
                case 5: return "Ultimate: Continuously fires in every direction around the player.";
            }
        }

        if (data.id == 10 || (data.iconKey != null && (data.iconKey.Contains("mine") || data.iconKey == "chipset_9")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Periodically drops mines along your path.";
                case 2: return "Increases explosion damage and reduces deployment time.";
                case 3: return "Mine hits slow enemies by 40% for 2 seconds.";
                case 4: return "Greatly increases the explosion radius.";
                case 5: return "Ultimate: The main mine releases three smaller mines when it explodes.";
            }
        }

        if (data.id == 9 || (data.iconKey != null && (data.iconKey.Contains("jumper") || data.iconKey.Contains("cable") || data.iconKey == "chipset_8")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Restores health when you damage enemies.";
                case 2: return "Increases life-steal effectiveness.";
                case 3: return "Applies life steal to every equipped weapon.";
                case 4: return "Excess healing becomes a shield worth up to 10% max HP.";
                case 5: return "Ultimate: Doubles life steal while HP is below 20%.";
            }
        }

        if (data.id == 7 || (data.iconKey != null && (data.iconKey.Contains("discus") || data.iconKey == "chipset_6")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "A spiked disc orbits the player.";
                case 2: return "Adds one disc and increases damage.";
                case 3: return "Discs inflict Bleed for 5 damage per second.";
                case 4: return "Adds a third disc to complete the defensive ring.";
                case 5: return "Ultimate: Discs double in size and can destroy enemy projectiles.";
            }
        }

        if (data.id == 6 || (data.iconKey != null && data.iconKey.Contains("turret")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Deploys a stationary gun turret.";
                case 2: return "Increases turret damage and duration.";
                case 3: return "Turret shots gain a 30% chance to explode for area damage.";
                case 4: return "The turret regenerates health after taking damage.";
                case 5: return "Ultimate: Deploy up to two gun turrets at once.";
            }
        }

        if (data.id == 3 || (data.iconKey != null && (data.iconKey.Contains("punch") || data.iconKey.Contains("rocket"))))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Launches a rocket-powered fist that explodes on impact.";
                case 2: return "Greatly increases direct-hit damage.";
                case 3: return "Increases the explosion radius by 40%.";
                case 4: return "The explosion stuns surviving enemies for 1 second.";
                case 5: return "Ultimate: The impact leaves a burning lava zone for 3 seconds.";
            }
        }

        if (data.id == 4 || (data.iconKey != null && data.iconKey.Contains("blade")))
        {
            switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
            {
                case 1: return "Throws a blade that automatically returns to the player.";
                case 2: return "The blade deals more damage and travels faster.";
                case 3: return "The blade pierces every target in its path.";
                case 4: return "Throws two blades in a V formation.";
                case 5: return "Ultimate: The blade spins at its destination for 2 seconds, dealing area damage before returning.";
            }
        }

        switch (Mathf.Clamp(data.level, 1, MaxRuntimeChipLevel))
        {
            case 2: return data.magicBonus;
            case 3: return data.rareBonus;
            case 4: return data.uniqueBonus;
            case 5: return data.epicBonus;
        }

        switch (data.iconKey)
        {
            case "spiky-discus": return "Spins a spiky Discus to attack an enemy.";
            case "energy-jumper-cables": return "Stealing life from the enemies.";
            case "big-battery": return "A part that increases Max HP.";
            case "shotgun": return "Deals significant damage to nearby enemies with many shells.";
            default: return data.description;
        }
    }

    public static List<ChipItemData> CreateRuntimeCatalog()
    {
        return ChipsetController.CreateSavedDatabase()
            .Where(chip => chip != null && chip.id >= 1 && chip.id <= PrimaryChipsetCount)
            .Select(chip => chip.Clone())
            .ToList();
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
        // Ten primary chipsets must all have the same chance to appear. Their persistent
        // MainMenu tier controls the Lever frame colour, not their draw probability.
        return 1d;
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
        Sprite[] levelPips,
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
        rerollTransform = drawAgainButton != null ? drawAgainButton.transform as RectTransform : null;
        rerollCanvasGroup = drawAgainButton != null ? drawAgainButton.GetComponent<CanvasGroup>() : null;
        chipIcons = icons;
        frameSprites = frames;
        levelPipSprites = levelPips;
        mechanicalParticleSprites = particleSprites;
    }
}
