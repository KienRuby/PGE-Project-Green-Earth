using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện và logic màn hình Build Body trong Lab:
/// 1. Kiểm tra điều kiện mở khóa Tab: Vượt qua tối thiểu 3 Chapter (PlayerDataService.UnlockedChapterIndex >= 3).
///    - Khi chưa đủ điều kiện: Nút Tab hiển thị sprite có ổ khóa (Build body locked), bấm vào sẽ báo Toast.
///    - Khi đủ điều kiện: Nút Tab hiển thị sprite mở (Build body), cho phép chuyển đổi giữa tab Stats và Build body.
/// 2. Danh sách 4 Skin/Body Units (AD Unit-1, 2, 3, 4) đúng 100% theo ảnh:
///    - Skin đang chọn/trang bị (Current version): Nền đổi sang màu XANH LÁ (Panel_Bottom_Green), hiển thị nút Change skin.
///    - Các Skin khác: Nền màu XANH ĐẬM (Panel_Top_DarkBlue).
///    - Bấm vào nút Change skin hoặc bấm vào thẻ đã sở hữu sẽ trang bị Skin đó, thẻ đó chuyển sang nền XANH LÁ tức thì.
///    - AD Unit-4 có nút Build bằng Ngọc Đỏ (Red Gems) nếu chưa sở hữu.
/// </summary>
public class BuildBodyController : MonoBehaviour
{
    public const string EquippedSkinKey = "PGE.BuildBody.EquippedSkinIndex";
    public const string BodyUnlockedKeyPrefix = "PGE.BuildBody.Unlocked_";
    public const int RequiredChaptersToUnlock = 3;

    [System.Serializable]
    public class BodyCardView
    {
        [Header("Root and Background")]
        public GameObject cardRoot;
        public Image backgroundImage;
        public Button selectButton;

        [Header("Slot and Robot Visual")]
        public Image slotFrameImage;
        public Image robotImage;
        public TMP_Text unitNameText;

        [Header("Info Text")]
        public TMP_Text titleText;
        public TMP_Text statsDescriptionText;

        [Header("Status and Action Buttons")]
        public TMP_Text statusText;
        public Button changeSkinButton;
        public GameObject buildGroup;
        public Button buildButton;
        public TMP_Text buildCostText;
        public Image buildCostIcon;
    }

    [Header("Tab Navigation")]
    [Tooltip("Nút tab Stats ở trên cùng.")]
    [SerializeField] private Button statsTabButton;

    [Tooltip("Nút tab Build Body ở trên cùng.")]
    [SerializeField] private Button buildBodyTabButton;

    [Tooltip("Image hiển thị sprite của nút tab Build Body (để đổi giữa asset khóa và mở).")]
    [SerializeField] private Image buildBodyTabImage;

    [Tooltip("Sprite nút Build Body ở trạng thái mở khóa.")]
    [SerializeField] private Sprite buildBodyUnlockedSprite;

    [Tooltip("Sprite nút Build Body ở trạng thái bị khóa (có ổ khóa).")]
    [SerializeField] private Sprite buildBodyLockedSprite;

    [Tooltip("Màn hình Stats nâng cấp.")]
    [SerializeField] private GameObject statsPanel;

    [Tooltip("Màn hình danh sách Build Body.")]
    [SerializeField] private GameObject buildBodyPanel;

    [Header("Card Background Sprites")]
    [Tooltip("Ảnh nền màu XANH LÁ dùng cho Skin đang được trang bị (Current version).")]
    [SerializeField] private Sprite greenCardBackground;

    [Tooltip("Ảnh nền màu XANH ĐẬM dùng cho các Skin khác (Previous version / chưa trang bị).")]
    [SerializeField] private Sprite darkBlueCardBackground;

    [Header("Body Cards (4 Units)")]
    [SerializeField] private BodyCardView[] cardViews = new BodyCardView[4];

    [Header("Unit Build Costs")]
    [Tooltip("Giá Ngọc Đỏ để Build Unit 4.")]
    [SerializeField] private int unit4BuildCostRedGems = 500;

    [Header("Toast Notification")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    // Public Properties and Events
    public static int EquippedSkinIndex
    {
        get => PlayerPrefs.GetInt(EquippedSkinKey, 2); // Mặc định Unit-3 (index 2) giống trong ảnh mẫu
        set
        {
            PlayerPrefs.SetInt(EquippedSkinKey, value);
            PlayerPrefs.Save();
            OnEquippedSkinChanged?.Invoke(value);
        }
    }

    public static event Action<int> OnEquippedSkinChanged;

    public bool IsBuildBodyUnlocked
    {
        get
        {
            // Kiểm tra xem người chơi đã vượt qua ít nhất 3 chapter chưa (UnlockedChapterIndex >= 3)
            int unlockedChapter = PlayerDataService.UnlockedChapterIndex;
            return unlockedChapter >= RequiredChaptersToUnlock || ChipManager.IsTestMode;
        }
    }

    private void Awake()
    {
        // Khởi tạo trạng thái mở khóa mặc định cho Unit 1, 2, 3 (đã sở hữu)
        if (!PlayerPrefs.HasKey(GetBodyUnlockKey(0))) PlayerPrefs.SetInt(GetBodyUnlockKey(0), 1);
        if (!PlayerPrefs.HasKey(GetBodyUnlockKey(1))) PlayerPrefs.SetInt(GetBodyUnlockKey(1), 1);
        if (!PlayerPrefs.HasKey(GetBodyUnlockKey(2))) PlayerPrefs.SetInt(GetBodyUnlockKey(2), 1);
        PlayerPrefs.Save();

        SetupTabListeners();
        SetupCardListeners();
        RefreshTabLockState();
    }

    private void Start()
    {
        SetupTabListeners();
        SetupCardListeners();
        RefreshTabLockState();
        RefreshAllCards();
    }

    private void OnEnable()
    {
        SetupTabListeners();
        SetupCardListeners();
        RefreshTabLockState();
        RefreshAllCards();
    }

    public static string GetBodyUnlockKey(int index) => $"{BodyUnlockedKeyPrefix}{index}";

    public static bool IsBodyUnlocked(int index)
    {
        if (index <= 2) return true; // Unit 1, 2, 3 mặc định đã mở sẵn
        return PlayerPrefs.GetInt(GetBodyUnlockKey(index), 0) == 1;
    }

    private void SetupTabListeners()
    {
        if (statsTabButton != null)
        {
            statsTabButton.onClick.RemoveAllListeners();
            statsTabButton.onClick.AddListener(OnStatsTabClicked);
        }

        if (buildBodyTabButton != null)
        {
            buildBodyTabButton.onClick.RemoveAllListeners();
            buildBodyTabButton.onClick.AddListener(OnBuildBodyTabClicked);
        }
    }

    public void OnStatsTabClicked()
    {
        if (statsPanel != null) statsPanel.SetActive(true);
        if (buildBodyPanel != null) buildBodyPanel.SetActive(false);
        RefreshTabVisuals(isBuildBodyActive: false);
    }

    public void OnBuildBodyTabClicked()
    {
        if (!IsBuildBodyUnlocked)
        {
            int currentCleared = PlayerDataService.UnlockedChapterIndex;
            ShowToast($"Cần vượt qua Chapter 3 để mở khóa Build Body! (Hiện tại: {currentCleared}/{RequiredChaptersToUnlock})");
            return;
        }

        if (statsPanel != null) statsPanel.SetActive(false);
        if (buildBodyPanel != null) buildBodyPanel.SetActive(true);
        RefreshTabVisuals(isBuildBodyActive: true);
        RefreshAllCards();
    }

    private void RefreshTabVisuals(bool isBuildBodyActive)
    {
        if (statsTabButton != null && statsTabButton.targetGraphic != null)
        {
            statsTabButton.targetGraphic.color = !isBuildBodyActive ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        if (buildBodyTabButton != null && buildBodyTabButton.targetGraphic != null)
        {
            buildBodyTabButton.targetGraphic.color = isBuildBodyActive ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    public void RefreshTabLockState()
    {
        bool unlocked = IsBuildBodyUnlocked;

        if (buildBodyTabImage != null)
        {
            if (unlocked && buildBodyUnlockedSprite != null)
            {
                buildBodyTabImage.sprite = buildBodyUnlockedSprite;
            }
            else if (!unlocked && buildBodyLockedSprite != null)
            {
                buildBodyTabImage.sprite = buildBodyLockedSprite;
            }
        }

        // Nếu đang khóa mà buildBodyPanel đang bật thì quay lại StatsPanel
        if (!unlocked && buildBodyPanel != null && buildBodyPanel.activeSelf)
        {
            OnStatsTabClicked();
        }
    }

    private void SetupCardListeners()
    {
        for (int i = 0; i < cardViews.Length; i++)
        {
            int index = i;
            BodyCardView card = cardViews[i];
            if (card == null) continue;

            if (card.selectButton != null)
            {
                card.selectButton.onClick.RemoveAllListeners();
                card.selectButton.onClick.AddListener(() => OnCardClicked(index));
            }

            if (card.changeSkinButton != null)
            {
                card.changeSkinButton.onClick.RemoveAllListeners();
                card.changeSkinButton.onClick.AddListener(() => OnChangeSkinClicked(index));
            }

            if (card.buildButton != null)
            {
                card.buildButton.onClick.RemoveAllListeners();
                card.buildButton.onClick.AddListener(() => OnBuildButtonClicked(index));
            }
        }
    }

    private void OnCardClicked(int index)
    {
        if (IsBodyUnlocked(index))
        {
            EquipSkin(index);
        }
    }

    private void OnChangeSkinClicked(int index)
    {
        if (IsBodyUnlocked(index))
        {
            EquipSkin(index);
        }
    }

    private void EquipSkin(int index)
    {
        if (EquippedSkinIndex == index)
        {
            ShowToast($"Đang sử dụng AD Unit-{index + 1}!");
            return;
        }

        EquippedSkinIndex = index;
        RefreshAllCards();
        ShowToast($"Đã đổi sang trang phục AD Unit-{index + 1}!");
    }

    private void OnBuildButtonClicked(int index)
    {
        if (IsBodyUnlocked(index))
        {
            EquipSkin(index);
            return;
        }

        int cost = unit4BuildCostRedGems;
        int currentGems = ChipManager.RedGems;

        if (currentGems < cost)
        {
            ShowToast($"Không đủ Ngọc Đỏ để Build Unit {index + 1}! Cần {cost} Ngọc Đỏ (Hiện có: {currentGems:N0})");
            return;
        }

        if (ChipManager.TrySpendRedGems(cost))
        {
            PlayerPrefs.SetInt(GetBodyUnlockKey(index), 1);
            PlayerPrefs.Save();
            EquipSkin(index);
            ShowToast($"★ Chúc mừng! Đã Build thành công AD Unit-{index + 1}! ★");
        }
    }

    public void RefreshAllCards()
    {
        int currentEquipped = EquippedSkinIndex;

        for (int i = 0; i < cardViews.Length; i++)
        {
            BodyCardView card = cardViews[i];
            if (card == null || card.cardRoot == null) continue;

            bool isEquipped = (i == currentEquipped);
            bool isUnlocked = IsBodyUnlocked(i);

            // 1. Đổi màu nền: Nền xanh lá (Panel_Bottom_Green) nếu đang chọn/trang bị, xanh đậm (Panel_Top_DarkBlue) nếu không
            if (card.backgroundImage != null)
            {
                card.backgroundImage.sprite = isEquipped ? greenCardBackground : darkBlueCardBackground;
            }

            // 2. Trạng thái StatusText và Buttons
            if (card.statusText != null)
            {
                if (isEquipped)
                {
                    card.statusText.text = "Current version";
                    card.statusText.gameObject.SetActive(true);
                }
                else if (isUnlocked)
                {
                    card.statusText.text = "Previous version";
                    card.statusText.gameObject.SetActive(true);
                }
                else
                {
                    card.statusText.gameObject.SetActive(false);
                }
            }

            // 3. Nút Change skin: Chỉ hiển thị trên thẻ đang trang bị
            if (card.changeSkinButton != null)
            {
                card.changeSkinButton.gameObject.SetActive(isEquipped);
            }

            // 4. Nhóm nút Build: Chỉ hiển thị trên thẻ chưa sở hữu (AD Unit-4)
            if (card.buildGroup != null)
            {
                card.buildGroup.SetActive(!isUnlocked);
            }

            if (card.buildCostText != null)
            {
                card.buildCostText.text = unit4BuildCostRedGems.ToString();
            }
        }
    }

    public void ShowToast(string message)
    {
        if (toastRoot == null || toastText == null) return;
        toastText.text = message;
        toastRoot.SetActive(false);
        toastRoot.SetActive(true);
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), 2.5f);
    }

    private void HideToast()
    {
        if (toastRoot != null) toastRoot.SetActive(false);
    }
}
