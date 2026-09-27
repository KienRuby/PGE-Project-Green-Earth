using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đại diện cho 1 thẻ Level trong danh sách cuộn Daily Gem Mine (Cấp 01 đến Cấp 05).
/// Quản lý hình ảnh xem trước, tiêu đề, số lượng Gem thưởng và nút bấm bắt đầu.
/// </summary>
public class DailyGemMineLevelCard : MonoBehaviour
{
    [Header("Level Configuration")]
    [Tooltip("Số thứ tự cấp độ (1 = Cấp 1, 2 = Cấp 2, ..., 5 = Cấp 5).")]
    [SerializeField] private int levelNumber = 1;

    [Header("UI Element References")]
    [Tooltip("Text hiển thị tên và cấp độ (ví dụ: 'Gem Mine LV.01').")]
    [SerializeField] private TMP_Text levelTitleText;

    [Tooltip("Text hiển thị số lượng kim cương thưởng (ví dụ: 'x120-220').")]
    [SerializeField] private TMP_Text rewardText;

    [Tooltip("Icon kim cương đỏ.")]
    [SerializeField] private Image rewardGemIcon;

    [Tooltip("Ảnh nền xem trước của màn chơi (bản đồ, quái vật, đá thiên thạch).")]
    [SerializeField] private Image previewImage;

    [Tooltip("Nút Start màu hồng để vào trận.")]
    [SerializeField] private Button startButton;

    [Tooltip("Text trên nút Start.")]
    [SerializeField] private TMP_Text startButtonLabel;

    [Tooltip("CanvasGroup để điều chỉnh độ mờ khi nút bị vô hiệu hóa/khóa.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Khung hiển thị khi Level bị khóa (tùy chọn).")]
    [SerializeField] private GameObject lockOverlay;

    [Tooltip("Text lý do khóa (ví dụ: 'Requires Chapter 3').")]
    [SerializeField] private TMP_Text lockLabel;

    public event Action<int> OnStartClicked;

    public int LevelNumber => levelNumber;
    public Button StartButton => startButton;
    public TMP_Text LevelTitleText => levelTitleText;
    public TMP_Text RewardText => rewardText;
    public Image PreviewImage => previewImage;
    public GameObject LockOverlay => lockOverlay;
    public bool IsLocked => lockOverlay != null && lockOverlay.activeSelf;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        RemoveRedundantStartLabel();
        EnsureLockBadge();
        OptimizeRaycastTargetsForSwiping();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
            startButton.onClick.AddListener(HandleStartClicked);
        }
    }

    /// <summary>
    /// Xóa bỏ text "Start" bị thừa trên StartButton vì ảnh nút btn_pink_start.png đã chứa sẵn chữ Start.
    /// </summary>
    public void RemoveRedundantStartLabel()
    {
        if (startButton != null)
        {
            Transform startLbl = startButton.transform.Find("StartLabel");
            if (startLbl != null)
            {
                if (Application.isPlaying) Destroy(startLbl.gameObject);
                else DestroyImmediate(startLbl.gameObject);
            }

            TMP_Text[] tmps = startButton.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                if (t == null) continue;
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
        }
    }

    /// <summary>
    /// Đảm bảo thẻ luôn có huy hiệu LockedBadge khi màn chơi bị khóa.
    /// Huy hiệu này bắt buộc phải có raycastTarget = true và Button để chặn đứng các thao tác click,
    /// tuyệt đối không cho tia raycast xuyên thủng xuống giao diện Chapter phía dưới.
    /// </summary>
    public void EnsureLockBadge()
    {
        if (lockOverlay != null)
        {
            Image existingImg = lockOverlay.GetComponent<Image>();
            if (existingImg != null) existingImg.raycastTarget = true;
            Button existingBtn = lockOverlay.GetComponent<Button>() ?? lockOverlay.AddComponent<Button>();
            existingBtn.targetGraphic = existingImg;
            existingBtn.onClick.RemoveListener(HandleLockedBadgeClicked);
            existingBtn.onClick.AddListener(HandleLockedBadgeClicked);
            return;
        }

        Transform existing = transform.Find("LockedBadge") ?? transform.Find("LockOverlay");
        if (existing != null)
        {
            lockOverlay = existing.gameObject;
            lockLabel = existing.GetComponentInChildren<TMP_Text>(true);
            Image existingImg = existing.GetComponent<Image>();
            if (existingImg != null) existingImg.raycastTarget = true;
            Button existingBtn = existing.GetComponent<Button>() ?? existing.gameObject.AddComponent<Button>();
            existingBtn.targetGraphic = existingImg;
            existingBtn.onClick.RemoveListener(HandleLockedBadgeClicked);
            existingBtn.onClick.AddListener(HandleLockedBadgeClicked);
            return;
        }

        GameObject badgeObj = new GameObject("LockedBadge", typeof(RectTransform), typeof(Image), typeof(Button));
        badgeObj.layer = gameObject.layer;
        badgeObj.transform.SetParent(transform, false);
        RectTransform rt = badgeObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 18f);
        rt.sizeDelta = new Vector2(190f, 72f);
        rt.localScale = Vector3.one;

        Image img = badgeObj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
        img.raycastTarget = true; // Bắt buộc nhận raycast để hấp thụ click

        Button btn = badgeObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.RemoveListener(HandleLockedBadgeClicked);
        btn.onClick.AddListener(HandleLockedBadgeClicked);

        GameObject txtObj = new GameObject("LockedText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.layer = gameObject.layer;
        txtObj.transform.SetParent(badgeObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.pivot = new Vector2(0.5f, 0.5f);
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
        txt.text = "LOCKED";
        txt.fontSize = 30f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color32(220, 220, 220, 255);
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        if (levelTitleText != null && levelTitleText.font != null)
        {
            txt.font = levelTitleText.font;
        }

        lockOverlay = badgeObj;
        lockLabel = txt;
        badgeObj.SetActive(false);
    }

    private void HandleLockedBadgeClicked()
    {
        Debug.Log($"[DailyGemMineLevelCard] Màn {levelNumber} đang bị khóa: {lockLabel?.text}");
    }

    /// <summary>
    /// Tối ưu hóa raycast target cho các thành phần trên thẻ:
    /// - Ảnh nền, tiêu đề, icon kim cương đặt raycastTarget = false để cử chỉ lướt (Swipe)
    ///   truyền thẳng về ScrollView tương tự Tower Def.
    /// - Nút Start giữ nguyên raycastTarget = true để nhận click bắt đầu trận.
    /// - Huy hiệu LockedBadge giữ raycastTarget = true để chặn click xuyên thấu khi màn bị khóa.
    /// </summary>
    public void OptimizeRaycastTargetsForSwiping()
    {
        if (previewImage != null) previewImage.raycastTarget = false;
        if (rewardGemIcon != null) rewardGemIcon.raycastTarget = false;
        if (levelTitleText != null) levelTitleText.raycastTarget = false;
        if (rewardText != null) rewardText.raycastTarget = false;
        if (lockLabel != null) lockLabel.raycastTarget = false;

        if (startButton != null)
        {
            var targetGraphic = startButton.targetGraphic as Image ?? startButton.GetComponent<Image>();
            if (targetGraphic != null)
            {
                targetGraphic.raycastTarget = true;
            }
        }

        if (lockOverlay != null)
        {
            Image lockImg = lockOverlay.GetComponent<Image>();
            if (lockImg != null)
            {
                lockImg.raycastTarget = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
        }
    }

    public void Setup(
        int level,
        string title,
        string reward,
        Sprite previewSprite = null,
        bool isLocked = false,
        string lockReason = "")
    {
        levelNumber = level;

        if (levelTitleText != null)
        {
            levelTitleText.text = title;
        }

        if (rewardText != null)
        {
            rewardText.text = reward;
        }

        if (previewImage != null && previewSprite != null)
        {
            previewImage.sprite = previewSprite;
        }

        RemoveRedundantStartLabel();
        EnsureLockBadge();
        SetLocked(isLocked, lockReason);
    }

    public void SetInteractable(bool interactable, bool hasEntrances)
    {
        bool isUnlocked = DailyGemMineProgress.IsLevelUnlocked(levelNumber);
        if (!isUnlocked)
        {
            SetLocked(true, $"Clear LV.{levelNumber - 1:D2} to Unlock");
            return;
        }

        SetLocked(false);

        if (startButton != null)
        {
            startButton.interactable = interactable && hasEntrances;
            var targetGraphic = startButton.targetGraphic as Image ?? startButton.GetComponent<Image>();
            if (targetGraphic != null)
            {
                targetGraphic.color = Color.white;
                targetGraphic.raycastTarget = true;
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = (interactable && hasEntrances) ? 1f : 0.65f;
        }
    }

    public void SetLocked(bool isLocked, string reason = "")
    {
        EnsureLockBadge();

        if (startButton != null)
        {
            startButton.gameObject.SetActive(!isLocked);
        }

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(isLocked);
            if (isLocked)
            {
                Image lockImg = lockOverlay.GetComponent<Image>();
                if (lockImg != null) lockImg.raycastTarget = true;
                Button lockBtn = lockOverlay.GetComponent<Button>() ?? lockOverlay.AddComponent<Button>();
                lockBtn.interactable = true;
                lockBtn.targetGraphic = lockImg;
            }
        }

        if (lockLabel != null && !string.IsNullOrEmpty(reason))
        {
            lockLabel.text = reason;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isLocked ? 0.7f : 1f;
        }

        if (previewImage != null)
        {
            previewImage.color = isLocked ? new Color(0.65f, 0.65f, 0.65f, 1f) : Color.white;
        }
    }

    public void SetLockOverlay(GameObject overlay, TMP_Text label = null)
    {
        lockOverlay = overlay;
        lockLabel = label;
    }

    private void HandleStartClicked()
    {
        OnStartClicked?.Invoke(levelNumber);
    }

    public void SetReferencesForTesting(
        int level,
        Button btn,
        TMP_Text titleTxt,
        TMP_Text rwdTxt)
    {
        levelNumber = level;
        startButton = btn;
        levelTitleText = titleTxt;
        rewardText = rwdTxt;

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
            startButton.onClick.AddListener(HandleStartClicked);
        }
    }
}
