using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Presentation-only sequence for Shop box rewards. ShopController owns reward rolling, persistence,
/// and currency; this component only reads the already committed drop list.
/// </summary>
public sealed class BoxOpeningController : MonoBehaviour
{
    public enum BoxOpeningState
    {
        Idle,
        Spawning,
        Opening,
        RevealingReward,
        Result,
        Closing
    }

    [Serializable]
    public sealed class BoxVisualSet
    {
        public ShopController.RewardType rewardType;
        public bool multiOpen;
        public Sprite closedSprite;
        public Sprite baseSprite;
        public Sprite lidSprite;
        public Sprite openSprite;
    }

    [Serializable]
    public sealed class RewardVisualEntry
    {
        public int itemId;
        public string displayName;
        public string description;
        public Sprite icon;
        public Sprite frame;
    }

    [Serializable]
    public sealed class ResultItemView
    {
        public GameObject root;
        public UnityEngine.UI.Image quantityEffect;
        public UnityEngine.UI.Image frame;
        public UnityEngine.UI.Image icon;
        public TMP_Text nameText;
        public TMP_Text amountText;
    }

    [Header("Screens")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private UnityEngine.UI.Button skipButton;
    [SerializeField] private UnityEngine.UI.Button closeResultButton;

    [Header("Chest")]
    [SerializeField] private RectTransform chestRoot;
    [SerializeField] private UnityEngine.UI.Image chestClosedImage;
    [SerializeField] private UnityEngine.UI.Image chestBaseImage;
    [SerializeField] private UnityEngine.UI.Image chestLidImage;
    [SerializeField] private UnityEngine.UI.Image chestOpenImage;

    [Header("Reusable Smoke")]
    [SerializeField] private RectTransform smokeRoot;
    [SerializeField] private UnityEngine.UI.Image[] smokePuffs = Array.Empty<UnityEngine.UI.Image>();

    [Header("Reward Reveal")]
    [SerializeField] private RectTransform rewardRoot;
    [SerializeField] private CanvasGroup rewardCanvasGroup;
    [SerializeField] private UnityEngine.UI.Image rewardGlowImage;
    [SerializeField] private UnityEngine.UI.Image rewardBurstImage;
    [SerializeField] private UnityEngine.UI.Image rewardFrameImage;
    [SerializeField] private UnityEngine.UI.Image rewardIconImage;
    [SerializeField] private TMP_Text rewardNameText;
    [SerializeField] private TMP_Text rewardDescText;
    [SerializeField] private TMP_Text rewardAmountText;
    [SerializeField] private TMP_Text tapToContinueText;
    [SerializeField] private UnityEngine.UI.Button fullScreenTapButton;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private CanvasGroup resultCanvasGroup;
    [SerializeField] private ResultItemView[] resultItems = Array.Empty<ResultItemView>();

    [Header("Existing Asset References")]
    [SerializeField] private BoxVisualSet[] boxVisualSets = Array.Empty<BoxVisualSet>();
    [SerializeField] private RewardVisualEntry[] chipsetRewardVisuals = Array.Empty<RewardVisualEntry>();
    [SerializeField] private RewardVisualEntry[] buddyRewardVisuals = Array.Empty<RewardVisualEntry>();

    [Header("Timing (unscaled seconds)")]
    [SerializeField, Min(0.01f)] private float spawnDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float anticipationDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float lidOpenDuration = 0.20f;
    [SerializeField, Min(0.01f)] private float rewardRevealDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.13f;

    private static readonly Vector2 RewardStartPosition = new Vector2(0f, -505f);
    private static readonly Vector2 RewardDisplayPosition = new Vector2(0f, 44f);
    private static readonly Vector2[] SmokeDirections =
    {
        new Vector2(-230f, 40f), new Vector2(-170f, 135f), new Vector2(-85f, 205f),
        new Vector2(15f, 235f), new Vector2(105f, 205f), new Vector2(185f, 125f),
        new Vector2(240f, 35f), new Vector2(0f, 145f)
    };

    private readonly List<ShopBoxDropRoller.Drop> currentDrops = new List<ShopBoxDropRoller.Drop>(10);
    private readonly Dictionary<int, RewardVisualEntry> chipsetVisualById = new Dictionary<int, RewardVisualEntry>();
    private readonly Dictionary<int, RewardVisualEntry> buddyVisualById = new Dictionary<int, RewardVisualEntry>();

    private Coroutine sequenceRoutine;
    private Coroutine smokeRoutine;
    private BoxOpeningState state = BoxOpeningState.Idle;
    private ShopController.RewardType currentRewardType;
    private BoxVisualSet currentVisualSet;
    private bool skipRequested;
    private bool tapRequested;
    private bool shopWasActive;
    private GameObject activeShopPanel;

    public bool IsBusy => state != BoxOpeningState.Idle;
    public BoxOpeningState CurrentState => state;
    public IReadOnlyList<ShopBoxDropRoller.Drop> CurrentDrops => currentDrops;
    public BoxVisualSet[] BoxVisualSets => boxVisualSets;
    public RewardVisualEntry[] ChipsetRewardVisuals => chipsetRewardVisuals;
    public RewardVisualEntry[] BuddyRewardVisuals => buddyRewardVisuals;

    private void Awake()
    {
        RebuildVisualLookups();
        if (skipButton != null) skipButton.onClick.AddListener(Skip);
        if (closeResultButton != null) closeResultButton.onClick.AddListener(CloseResult);
        if (fullScreenTapButton != null) fullScreenTapButton.onClick.AddListener(OnTapContinue);
        ResetAllVisuals();
    }

    private void OnDestroy()
    {
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
        if (closeResultButton != null) closeResultButton.onClick.RemoveListener(CloseResult);
        if (fullScreenTapButton != null) fullScreenTapButton.onClick.RemoveListener(OnTapContinue);
    }

    public void OnTapContinue()
    {
        if (state == BoxOpeningState.RevealingReward)
        {
            tapRequested = true;
        }
    }

    public bool TryBegin(
        ShopController.RewardType rewardType,
        int requestedBoxCount,
        IReadOnlyList<ShopBoxDropRoller.Drop> committedDrops,
        GameObject sourceShopPanel = null)
    {
        if (IsBusy || committedDrops == null || committedDrops.Count == 0)
            return false;

        currentVisualSet = FindVisualSet(rewardType, requestedBoxCount > 1);
        if (currentVisualSet == null || !HasCompleteChestVisuals(currentVisualSet))
        {
            Debug.LogError($"[BoxOpening] Missing chest visual set for {rewardType}, multi={requestedBoxCount > 1}.");
            return false;
        }

        currentDrops.Clear();
        for (int i = 0; i < committedDrops.Count; i++) currentDrops.Add(committedDrops[i]);

        currentRewardType = rewardType;
        skipRequested = false;
        state = BoxOpeningState.Spawning;
        activeShopPanel = sourceShopPanel != null ? sourceShopPanel : shopPanel;
        shopWasActive = activeShopPanel != null && activeShopPanel.activeSelf;

        if (activeShopPanel != null) activeShopPanel.SetActive(false);
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        ConfigureChest(currentVisualSet);
        ResetAllVisuals(keepOverlayOpen: true);
        ResetChestClosed();
        if (skipButton != null) skipButton.interactable = true;
        sequenceRoutine = StartCoroutine(PlaySequence());
        return true;
    }

    public void Skip()
    {
        if (state == BoxOpeningState.Idle || state == BoxOpeningState.Result || skipRequested)
            return;

        skipRequested = true;
    }

    public void CloseResult()
    {
        if (state != BoxOpeningState.Result) return;

        state = BoxOpeningState.Closing;
        StopRunningRoutines();
        ResetAllVisuals();
        if (overlayRoot != null) overlayRoot.SetActive(false);
        if (activeShopPanel != null && shopWasActive) activeShopPanel.SetActive(true);
        activeShopPanel = null;
        currentDrops.Clear();
        currentVisualSet = null;
        state = BoxOpeningState.Idle;
    }

    private IEnumerator PlaySequence()
    {
        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 1f;

        chestRoot.localScale = Vector3.one * 0.78f;
        yield return Tween(spawnDuration, value =>
        {
            float scale = value < 0.72f
                ? Mathf.LerpUnclamped(0.78f, 1.08f, EaseOutBack(value / 0.72f))
                : Mathf.Lerp(1.08f, 1f, EaseOutQuad((value - 0.72f) / 0.28f));
            chestRoot.localScale = Vector3.one * scale;
        });
        if (TryCompleteSkip()) yield break;

        for (int i = 0; i < currentDrops.Count; i++)
        {
            ResetChestClosed();
            ResetRewardVisual();

            yield return PlayAnticipation();
            if (TryCompleteSkip()) yield break;

            state = BoxOpeningState.Opening;
            yield return PlayLidOpen();
            if (TryCompleteSkip()) yield break;

            if (smokeRoutine != null) StopCoroutine(smokeRoutine);
            smokeRoutine = StartCoroutine(PlaySmoke());

            state = BoxOpeningState.RevealingReward;
            ApplyReward(currentDrops[i]);
            yield return PlayRewardReveal();
            if (TryCompleteSkip()) yield break;

            tapRequested = false;
            if (tapToContinueText != null)
            {
                tapToContinueText.gameObject.SetActive(true);
                tapToContinueText.text = GameSettings.IsVietnamese ? "Chạm để tiếp tục" : "Tap to continue";
            }

            while (!tapRequested && !skipRequested)
            {
                if (rewardBurstImage != null && rewardBurstImage.gameObject.activeSelf)
                    rewardBurstImage.rectTransform.Rotate(0f, 0f, 16f * Time.unscaledDeltaTime);
                if (rewardGlowImage != null && rewardGlowImage.gameObject.activeSelf)
                    rewardGlowImage.rectTransform.Rotate(0f, 0f, -10f * Time.unscaledDeltaTime);
                yield return null;
            }
            if (TryCompleteSkip()) yield break;

            if (i < currentDrops.Count - 1)
            {
                yield return HideCurrentReward();
                if (TryCompleteSkip()) yield break;
            }
        }

        ShowResultInstant();
    }

    private IEnumerator PlayAnticipation()
    {
        state = BoxOpeningState.Spawning;
        Vector3 start = Vector3.one;
        yield return Tween(anticipationDuration, value =>
        {
            Vector3 scale;
            float angle;
            if (value < 0.33f)
            {
                float t = EaseInOutSine(value / 0.33f);
                scale = Vector3.LerpUnclamped(start, new Vector3(1.05f, 0.92f, 1f), t);
                angle = Mathf.Lerp(0f, -2f, t);
            }
            else if (value < 0.66f)
            {
                float t = EaseInOutSine((value - 0.33f) / 0.33f);
                scale = Vector3.LerpUnclamped(new Vector3(1.05f, 0.92f, 1f), new Vector3(0.97f, 1.05f, 1f), t);
                angle = Mathf.Lerp(-2f, 2f, t);
            }
            else
            {
                float t = EaseOutQuad((value - 0.66f) / 0.34f);
                scale = Vector3.LerpUnclamped(new Vector3(0.97f, 1.05f, 1f), Vector3.one, t);
                angle = Mathf.Lerp(2f, 0f, t);
            }
            chestRoot.localScale = scale;
            chestRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
        });
        chestRoot.localScale = Vector3.one;
        chestRoot.localRotation = Quaternion.identity;
    }

    private IEnumerator PlayLidOpen()
    {
        chestClosedImage.gameObject.SetActive(false);
        chestOpenImage.gameObject.SetActive(false);
        chestBaseImage.gameObject.SetActive(true);
        chestLidImage.gameObject.SetActive(true);

        Vector2 lidStartPosition = chestLidImage.rectTransform.anchoredPosition;
        yield return Tween(lidOpenDuration, value =>
        {
            float eased = EaseOutBack(value);
            chestLidImage.rectTransform.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -95f, eased), 0f, Mathf.Lerp(0f, -4f, value));
            chestLidImage.rectTransform.anchoredPosition = lidStartPosition + new Vector2(0f, Mathf.Lerp(0f, 95f, EaseOutQuad(value)));
        });

        chestBaseImage.gameObject.SetActive(false);
        chestLidImage.gameObject.SetActive(false);
        chestOpenImage.gameObject.SetActive(true);
    }

    private IEnumerator PlaySmoke()
    {
        if (smokeRoot == null || smokePuffs == null) yield break;
        smokeRoot.gameObject.SetActive(true);

        int count = Mathf.Min(smokePuffs.Length, SmokeDirections.Length);
        for (int i = 0; i < count; i++)
        {
            UnityEngine.UI.Image puff = smokePuffs[i];
            if (puff == null) continue;
            puff.gameObject.SetActive(true);
            puff.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            puff.rectTransform.localScale = Vector3.one * (0.45f + (i % 3) * 0.08f);
            Color color = puff.color;
            color.a = 0.92f;
            puff.color = color;
        }

        float duration = 0.52f;
        float elapsed = 0f;
        while (elapsed < duration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < count; i++)
            {
                UnityEngine.UI.Image puff = smokePuffs[i];
                if (puff == null) continue;
                float delay = (i % 3) * 0.035f;
                float localT = Mathf.Clamp01((t - delay) / Mathf.Max(0.01f, 1f - delay));
                puff.rectTransform.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, -10f), SmokeDirections[i], EaseOutQuad(localT));
                puff.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.35f, EaseOutQuad(localT));
                Color color = puff.color;
                color.a = 0.92f * (1f - localT * localT);
                puff.color = color;
            }
            yield return null;
        }

        ClearSmoke();
        smokeRoutine = null;
    }

    private IEnumerator PlayRewardReveal()
    {
        rewardRoot.gameObject.SetActive(true);
        rewardCanvasGroup.alpha = 0f;
        rewardRoot.anchoredPosition = RewardStartPosition;
        rewardRoot.localScale = Vector3.one * 0.3f;
        rewardBurstImage.gameObject.SetActive(true);
        rewardBurstImage.rectTransform.localRotation = Quaternion.identity;
        rewardGlowImage.gameObject.SetActive(true);
        rewardGlowImage.rectTransform.localRotation = Quaternion.identity;
        if (rewardNameText != null)
        {
            rewardNameText.gameObject.SetActive(true);
            rewardNameText.alpha = 0f;
        }
        if (rewardDescText != null && !string.IsNullOrEmpty(rewardDescText.text))
        {
            rewardDescText.gameObject.SetActive(true);
            rewardDescText.alpha = 0f;
        }
        if (rewardAmountText != null)
        {
            rewardAmountText.gameObject.SetActive(true);
            rewardAmountText.alpha = 0f;
        }

        yield return Tween(rewardRevealDuration, value =>
        {
            float easedPosition = EaseOutBack(value);
            rewardRoot.anchoredPosition = Vector2.LerpUnclamped(RewardStartPosition, RewardDisplayPosition, easedPosition);
            rewardCanvasGroup.alpha = Mathf.Clamp01(value * 5f);
            if (rewardNameText != null) rewardNameText.alpha = Mathf.Clamp01(value * 3f);
            if (rewardDescText != null && rewardDescText.gameObject.activeSelf) rewardDescText.alpha = Mathf.Clamp01(value * 3f);
            if (rewardAmountText != null) rewardAmountText.alpha = Mathf.Clamp01(value * 3f);

            float scale;
            if (value < 0.58f) scale = Mathf.Lerp(0.3f, 1.15f, EaseOutQuad(value / 0.58f));
            else if (value < 0.82f) scale = Mathf.Lerp(1.15f, 0.95f, EaseInOutSine((value - 0.58f) / 0.24f));
            else scale = Mathf.Lerp(0.95f, 1f, EaseOutQuad((value - 0.82f) / 0.18f));
            rewardRoot.localScale = Vector3.one * scale;

            rewardBurstImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.05f, EaseOutBack(value));
            rewardBurstImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, value * 18f);
            rewardGlowImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.2f, EaseOutQuad(value));
            rewardGlowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -value * 12f);
        });

        rewardRoot.anchoredPosition = RewardDisplayPosition;
        rewardRoot.localScale = Vector3.one;
        rewardCanvasGroup.alpha = 1f;
        if (rewardNameText != null) rewardNameText.alpha = 1f;
        if (rewardDescText != null && rewardDescText.gameObject.activeSelf) rewardDescText.alpha = 1f;
        if (rewardAmountText != null) rewardAmountText.alpha = 1f;
    }

    private IEnumerator HideCurrentReward()
    {
        Vector3 startScale = rewardRoot.localScale;
        yield return Tween(transitionDuration, value =>
        {
            float eased = EaseInOutSine(value);
            rewardRoot.localScale = Vector3.Lerp(startScale, Vector3.one * 0.55f, eased);
            rewardCanvasGroup.alpha = 1f - eased;
            if (rewardNameText != null) rewardNameText.alpha = 1f - eased;
            if (rewardDescText != null) rewardDescText.alpha = 1f - eased;
            if (rewardAmountText != null) rewardAmountText.alpha = 1f - eased;
            if (tapToContinueText != null) tapToContinueText.alpha = 1f - eased;
        });
        ResetRewardVisual();
        ClearSmoke();
    }

    private IEnumerator Tween(float duration, Action<float> update)
    {
        float elapsed = 0f;
        update(0f);
        while (elapsed < duration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            update(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (!skipRequested) update(1f);
    }

    private bool TryCompleteSkip()
    {
        if (!skipRequested) return false;
        ShowResultInstant();
        return true;
    }

    private void ShowResultInstant()
    {
        StopSmokeOnly();
        ResetRewardVisual();
        ClearSmoke();
        chestRoot.gameObject.SetActive(false);
        state = BoxOpeningState.Result;
        skipRequested = false;
        if (skipButton != null) skipButton.interactable = false;

        PopulateResultItems();
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.alpha = 1f;
            resultCanvasGroup.interactable = true;
            resultCanvasGroup.blocksRaycasts = true;
        }
        sequenceRoutine = null;
    }

    private void PopulateResultItems()
    {
        for (int i = 0; i < resultItems.Length; i++)
        {
            ResultItemView view = resultItems[i];
            bool visible = i < currentDrops.Count;
            if (view?.root != null) view.root.SetActive(visible);
            if (!visible || view == null) continue;

            RewardVisualEntry visual = ResolveRewardVisual(currentDrops[i].ItemId);
            ApplyQuantityEffect(view.quantityEffect, currentDrops[i].Pieces, false);
            if (view.icon != null) view.icon.sprite = visual?.icon;
            if (view.frame != null)
            {
                view.frame.sprite = visual?.frame;
                view.frame.enabled = view.frame.sprite != null;
            }
            if (view.nameText != null) view.nameText.text = visual?.displayName ?? $"ITEM {currentDrops[i].ItemId}";
            if (view.amountText != null) view.amountText.text = $"x{currentDrops[i].Pieces:N0}";
        }
    }

    private void ApplyReward(ShopBoxDropRoller.Drop drop)
    {
        RewardVisualEntry visual = ResolveRewardVisual(drop.ItemId);
        rewardIconImage.sprite = visual?.icon;
        rewardIconImage.enabled = rewardIconImage.sprite != null;
        rewardFrameImage.sprite = visual?.frame;
        rewardFrameImage.enabled = rewardFrameImage.sprite != null;
        ApplyQuantityEffect(rewardGlowImage, drop.Pieces, true);
        ApplyQuantityEffect(rewardBurstImage, drop.Pieces, true);
        if (rewardNameText != null)
        {
            rewardNameText.text = visual?.displayName ?? $"ITEM {drop.ItemId}";
            rewardNameText.color = Color.white;
            rewardNameText.gameObject.SetActive(true);
        }
        if (rewardDescText != null)
        {
            string desc = GetLocalizedDescription(visual);
            rewardDescText.text = desc;
            rewardDescText.color = Color.white;
            rewardDescText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
        }
        if (rewardAmountText != null)
        {
            rewardAmountText.text = $"x{drop.Pieces:N0}";
            rewardAmountText.color = new Color32(255, 205, 67, 255);
            rewardAmountText.gameObject.SetActive(true);
        }
    }

    private string GetLocalizedDescription(RewardVisualEntry visual)
    {
        if (visual == null || string.IsNullOrEmpty(visual.description)) return string.Empty;
        if (!GameSettings.IsVietnamese) return visual.description;

        string name = visual.displayName ?? string.Empty;
        if (name.IndexOf("Rifle", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Vũ khí xả loạt đạn tốc độ cao";
        if (name.IndexOf("Standard Gun", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Luôn được trang bị, kể cả khi không có trong bộ bài";
        if (name.IndexOf("Rocket Punch", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Phóng nắm đấm tên lửa gây sát thương diện rộng";
        if (name.IndexOf("Spinning Blade", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Ném phi đao xoay xuyên qua kẻ địch";
        if (name.IndexOf("Multigun", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Bắn mưa đạn ra nhiều hướng cùng lúc";
        if (name.IndexOf("Gun Turret", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Triển khai trụ súng tự động tấn công";
        if (name.IndexOf("Spiky Discus", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Đĩa gai xoay quanh bản thân gây sát thương";
        if (name.IndexOf("Shotgun", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Bắn loạt mảnh đạn uy lực mạnh ở tầm gần";
        if (name.IndexOf("Energy Jumper Cables", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Hút sinh lực từ kẻ địch để hồi phục HP";
        if (name.IndexOf("High-Explosive Mine", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Rải mìn phát nổ cực mạnh trên mặt đất";

        return visual.description;
    }

    private RewardVisualEntry ResolveRewardVisual(int itemId)
    {
        Dictionary<int, RewardVisualEntry> lookup = currentRewardType == ShopController.RewardType.ChipsetBox
            ? chipsetVisualById
            : buddyVisualById;
        lookup.TryGetValue(itemId, out RewardVisualEntry result);
        return result;
    }

    private static void ApplyQuantityEffect(UnityEngine.UI.Image effect, int pieces, bool keepCommonEffect)
    {
        if (effect == null) return;

        if (pieces >= 7)
        {
            effect.gameObject.SetActive(true);
            effect.color = new Color32(255, 215, 60, keepCommonEffect ? (byte)235 : (byte)195);
        }
        else if (pieces >= 3)
        {
            effect.gameObject.SetActive(true);
            effect.color = new Color32(220, 100, 255, keepCommonEffect ? (byte)235 : (byte)185);
        }
        else
        {
            effect.gameObject.SetActive(keepCommonEffect);
            effect.color = new Color32(255, 255, 255, keepCommonEffect ? (byte)230 : (byte)0);
        }
    }

    private void RebuildVisualLookups()
    {
        chipsetVisualById.Clear();
        buddyVisualById.Clear();
        AddVisuals(chipsetRewardVisuals, chipsetVisualById);
        AddVisuals(buddyRewardVisuals, buddyVisualById);
    }

    private static void AddVisuals(RewardVisualEntry[] source, Dictionary<int, RewardVisualEntry> target)
    {
        if (source == null) return;
        for (int i = 0; i < source.Length; i++)
        {
            RewardVisualEntry entry = source[i];
            if (entry != null && entry.itemId > 0) target[entry.itemId] = entry;
        }
    }

    private BoxVisualSet FindVisualSet(ShopController.RewardType rewardType, bool multiOpen)
    {
        for (int i = 0; i < boxVisualSets.Length; i++)
        {
            BoxVisualSet candidate = boxVisualSets[i];
            if (candidate != null && candidate.rewardType == rewardType && candidate.multiOpen == multiOpen)
                return candidate;
        }
        return null;
    }

    private static bool HasCompleteChestVisuals(BoxVisualSet set)
    {
        return set.closedSprite != null && set.baseSprite != null && set.lidSprite != null && set.openSprite != null;
    }

    private void ConfigureChest(BoxVisualSet set)
    {
        chestClosedImage.sprite = set.closedSprite;
        chestBaseImage.sprite = set.baseSprite;
        chestLidImage.sprite = set.lidSprite;
        chestOpenImage.sprite = set.openSprite;

        float width = set.multiOpen ? 450f : 325f;
        SetImageWidth(chestClosedImage, width);
        SetImageWidth(chestBaseImage, width);
        SetImageWidth(chestLidImage, width * 0.985f);
        SetImageWidth(chestOpenImage, width);

        float openHeight = chestOpenImage.rectTransform.sizeDelta.y;
        float baseHeight = chestBaseImage.rectTransform.sizeDelta.y;
        float lidHeight = chestLidImage.rectTransform.sizeDelta.y;
        chestLidImage.rectTransform.pivot = new Vector2(0.5f, 0.08f);
        chestBaseImage.rectTransform.anchoredPosition = new Vector2(0f, -openHeight * 0.5f + baseHeight * 0.5f);
        chestLidImage.rectTransform.anchoredPosition = new Vector2(0f, openHeight * 0.5f - lidHeight * 0.5f);
    }

    private static void SetImageWidth(UnityEngine.UI.Image image, float width)
    {
        if (image == null || image.sprite == null) return;
        float aspect = image.sprite.rect.height / Mathf.Max(1f, image.sprite.rect.width);
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = new Vector2(width, width * aspect);
    }

    private void ResetChestClosed()
    {
        chestRoot.gameObject.SetActive(true);
        chestRoot.localScale = Vector3.one;
        chestRoot.localRotation = Quaternion.identity;
        chestClosedImage.gameObject.SetActive(true);
        chestBaseImage.gameObject.SetActive(false);
        chestLidImage.gameObject.SetActive(false);
        chestOpenImage.gameObject.SetActive(false);
        chestLidImage.rectTransform.localRotation = Quaternion.identity;
        ConfigureChest(currentVisualSet);
    }

    private void ResetRewardVisual()
    {
        if (rewardRoot == null) return;
        rewardRoot.gameObject.SetActive(false);
        rewardRoot.anchoredPosition = RewardStartPosition;
        rewardRoot.localScale = Vector3.one * 0.3f;
        rewardRoot.localRotation = Quaternion.identity;
        if (rewardCanvasGroup != null) rewardCanvasGroup.alpha = 0f;
        if (rewardIconImage != null) rewardIconImage.sprite = null;
        if (rewardFrameImage != null) rewardFrameImage.sprite = null;
        if (rewardNameText != null)
        {
            rewardNameText.text = string.Empty;
            rewardNameText.gameObject.SetActive(false);
        }
        if (rewardDescText != null)
        {
            rewardDescText.text = string.Empty;
            rewardDescText.gameObject.SetActive(false);
        }
        if (rewardAmountText != null)
        {
            rewardAmountText.text = string.Empty;
            rewardAmountText.gameObject.SetActive(false);
        }
        if (tapToContinueText != null) tapToContinueText.gameObject.SetActive(false);
        if (rewardGlowImage != null) rewardGlowImage.gameObject.SetActive(false);
        if (rewardBurstImage != null) rewardBurstImage.gameObject.SetActive(false);
    }

    private void ResetAllVisuals(bool keepOverlayOpen = false)
    {
        StopSmokeOnly();
        ClearSmoke();
        ResetRewardVisual();
        if (resultPanel != null) resultPanel.SetActive(false);
        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.alpha = 0f;
            resultCanvasGroup.interactable = false;
            resultCanvasGroup.blocksRaycasts = false;
        }
        if (chestRoot != null) chestRoot.gameObject.SetActive(keepOverlayOpen);
        if (!keepOverlayOpen && overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void ClearSmoke()
    {
        if (smokePuffs != null)
        {
            for (int i = 0; i < smokePuffs.Length; i++)
            {
                UnityEngine.UI.Image puff = smokePuffs[i];
                if (puff == null) continue;
                Color color = puff.color;
                color.a = 0f;
                puff.color = color;
                puff.gameObject.SetActive(false);
            }
        }
        if (smokeRoot != null) smokeRoot.gameObject.SetActive(false);
    }

    private void StopRunningRoutines()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
        StopSmokeOnly();
    }

    private void StopSmokeOnly()
    {
        if (smokeRoutine != null)
        {
            StopCoroutine(smokeRoutine);
            smokeRoutine = null;
        }
    }

    private static float EaseOutQuad(float value)
    {
        value = Mathf.Clamp01(value);
        return 1f - (1f - value) * (1f - value);
    }

    private static float EaseInOutSine(float value)
    {
        value = Mathf.Clamp01(value);
        return -(Mathf.Cos(Mathf.PI * value) - 1f) * 0.5f;
    }

    private static float EaseOutBack(float value)
    {
        value = Mathf.Clamp01(value);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t = value - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }
}
