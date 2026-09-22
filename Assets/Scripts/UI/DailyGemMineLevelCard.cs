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

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
            startButton.onClick.AddListener(HandleStartClicked);
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

        SetLocked(isLocked, lockReason);
    }

    public void SetInteractable(bool interactable, bool hasEntrances)
    {
        if (startButton != null)
        {
            startButton.interactable = interactable && hasEntrances;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = (interactable && hasEntrances) ? 1f : 0.65f;
        }
    }

    public void SetLocked(bool isLocked, string reason = "")
    {
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(isLocked);
        }

        if (lockLabel != null && !string.IsNullOrEmpty(reason))
        {
            lockLabel.text = reason;
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(!isLocked);
        }
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
