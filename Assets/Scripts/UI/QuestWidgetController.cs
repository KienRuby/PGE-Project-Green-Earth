using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Widget Nhiệm Vụ (Quest Banner) hiển thị ở góc trái màn hình Chapter.
/// </summary>
public class QuestWidgetController : MonoBehaviour
{
    [Header("Quest Data")]
    [Tooltip("ScriptableObject chứa dữ liệu cấu hình nhiệm vụ hiện tại.")]
    [SerializeField] private QuestData currentQuest;

    [Header("UI References")]
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private TMP_Text rewardAmountText;
    [SerializeField] private Button getButton;
    [SerializeField] private GameObject notificationDot;

    public const string QuestClaimedKeyPrefix = "PGE.Quest.Claimed.";

    public static string GetQuestClaimedKey(string questId)
    {
        string normalized = string.IsNullOrWhiteSpace(questId) ? "default" : questId.Trim();
        return $"{QuestClaimedKeyPrefix}{normalized}";
    }

    public static bool IsQuestClaimed(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return false;
        return PlayerPrefs.GetInt(GetQuestClaimedKey(questId), 0) == 1;
    }

    public static void SetQuestClaimed(string questId, bool claimed = true)
    {
        if (string.IsNullOrWhiteSpace(questId)) return;
        PlayerPrefs.SetInt(GetQuestClaimedKey(questId), claimed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        if (getButton != null)
        {
            getButton.onClick.RemoveListener(OnGetButtonClicked);
            getButton.onClick.AddListener(OnGetButtonClicked);
        }

        RefreshQuestView();
    }

    private void OnDestroy()
    {
        if (getButton != null)
        {
            getButton.onClick.RemoveListener(OnGetButtonClicked);
        }
    }

    public void SetQuest(QuestData quest)
    {
        currentQuest = quest;
        RefreshQuestView();
    }

    public bool IsCurrentQuestClaimed()
    {
        return currentQuest != null && IsQuestClaimed(currentQuest.questId);
    }

    public void RefreshQuestView()
    {
        bool isClaimed = IsCurrentQuestClaimed();

        if (currentQuest == null)
        {
            if (questTitleText != null) questTitleText.text = "Quest";
            if (questDescriptionText != null) questDescriptionText.text = "Upgrade stats\nat the lab";
            if (rewardAmountText != null) rewardAmountText.text = "X200";
            if (getButton != null) getButton.interactable = true;
            if (notificationDot != null) notificationDot.SetActive(true);
            return;
        }

        if (questTitleText != null) questTitleText.text = currentQuest.questTitle;
        if (questDescriptionText != null) questDescriptionText.text = currentQuest.questDescription;
        if (rewardAmountText != null) rewardAmountText.text = $"X{currentQuest.rewardAmount}";
        if (rewardIconImage != null && currentQuest.rewardIcon != null)
        {
            rewardIconImage.sprite = currentQuest.rewardIcon;
        }

        if (getButton != null)
        {
            getButton.interactable = !isClaimed;
            TMP_Text btnText = getButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = isClaimed ? "CLAIMED" : "GET";
            }
        }

        if (notificationDot != null)
        {
            notificationDot.SetActive(!isClaimed);
        }
    }

    public bool TryClaimReward()
    {
        if (currentQuest == null) return false;
        if (IsCurrentQuestClaimed())
        {
            Debug.LogWarning($"[QuestWidget] Nhiệm vụ {currentQuest.questId} đã được nhận thưởng trước đó!");
            return false;
        }

        // Trao thưởng
        switch (currentQuest.rewardType)
        {
            case QuestData.RewardType.RedGem:
                ChipManager.AddRedGems(currentQuest.rewardAmount);
                break;
            case QuestData.RewardType.DataChip:
                ChipManager.AddDataChips(currentQuest.rewardAmount);
                break;
            case QuestData.RewardType.Energy:
                ChipManager.AddEnergy(currentQuest.rewardAmount);
                break;
        }

        SetQuestClaimed(currentQuest.questId, true);
        RefreshQuestView();

        Debug.Log($"[QuestWidget] Đã nhận thưởng Quest thành công: {currentQuest.rewardAmount} {currentQuest.rewardType}");
        return true;
    }

    private void OnGetButtonClicked()
    {
        TryClaimReward();
    }
}
