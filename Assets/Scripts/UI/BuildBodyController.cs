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

    [Header("Body Cards (5 Units: Slot 1 Default, Slots 2-5 AD Units)")]
    [SerializeField] private BodyCardView[] cardViews = new BodyCardView[5];

    [Header("Unit Build Costs")]
    [Tooltip("Giá Ngọc Đỏ để Build 5 Units (Slot 1 Default: 0, Unit 1: 1000, Unit 2: 1500, Unit 3: 2000, Unit 4: 3000).")]
    [SerializeField] private int[] unitBuildCosts = new int[5] { 0, 1000, 1500, 2000, 3000 };

    [Header("Toast Notification")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;

    // Public Properties and Events
    public static int EquippedSkinIndex
    {
        get
        {
            int index = PlayerPrefs.GetInt(EquippedSkinKey, 0);
            if (!IsBodyUnlocked(index)) return 0;
            return index;
        }
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

    public int GetBuildCost(int index)
    {
        if (unitBuildCosts != null && index >= 0 && index < unitBuildCosts.Length)
            return unitBuildCosts[index];
        return index switch
        {
            0 => 0,
            1 => 1000,
            2 => 1500,
            3 => 2000,
            4 => 3000,
            _ => 1000
        };
    }

    public static string GetUnitDisplayName(int index, bool vi)
    {
        if (index == 0) return vi ? "Trang phục Mặc định" : "Default Body";
        return $"AD Unit-{index}";
    }

    private void Awake()
    {
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
        if (index == 0) return true; // Skin mặc định (Slot 1) luôn mở khóa sẵn khi mới tải game
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
            bool vi = GameSettings.IsVietnamese;
            ShowToast(vi
                ? $"Cần vượt qua Chapter 3 để mở khóa Build Body! (Hiện tại: {currentCleared}/{RequiredChaptersToUnlock})"
                : $"Clear Chapter 3 to unlock Build Body! (Current: {currentCleared}/{RequiredChaptersToUnlock})");
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
        bool vi = GameSettings.IsVietnamese;
        string name = GetUnitDisplayName(index, vi);
        if (EquippedSkinIndex == index)
        {
            ShowToast(vi ? $"Đang sử dụng {name}!" : $"Currently using {name}!");
            return;
        }

        EquippedSkinIndex = index;
        RefreshAllCards();
        ShowToast(vi ? $"Đã đổi sang trang phục {name}!" : $"Equipped outfit {name}!");
    }

    private void OnBuildButtonClicked(int index)
    {
        if (index == 0 || IsBodyUnlocked(index))
        {
            EquipSkin(index);
            return;
        }

        int cost = GetBuildCost(index);
        int currentGems = ChipManager.RedGems;
        bool vi = GameSettings.IsVietnamese;
        string name = GetUnitDisplayName(index, vi);

        if (currentGems < cost)
        {
            ShowToast(vi
                ? $"Không đủ Ngọc Đỏ để Build {name}! Cần {cost:N0} Ngọc Đỏ (Hiện có: {currentGems:N0})"
                : $"Not enough Red Gems to build {name}! Need {cost:N0} Red Gems (Current: {currentGems:N0})");
            return;
        }

        if (ChipManager.TrySpendRedGems(cost))
        {
            PlayerPrefs.SetInt(GetBodyUnlockKey(index), 1);
            PlayerPrefs.Save();
            EquipSkin(index);
            ShowToast(vi
                ? $"★ Chúc mừng! Đã Build thành công {name}! ★"
                : $"★ Congratulations! Successfully built {name}! ★");
        }
    }

    public void RefreshAllCards()
    {
        int currentEquipped = EquippedSkinIndex;
        bool vi = GameSettings.IsVietnamese;

        for (int i = 0; i < cardViews.Length; i++)
        {
            BodyCardView card = cardViews[i];
            if (card == null || card.cardRoot == null) continue;

            bool isUnlocked = IsBodyUnlocked(i);
            bool isEquipped = isUnlocked && (i == currentEquipped);

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
                    card.statusText.text = vi ? "Phiên bản hiện tại" : "Current version";
                    card.statusText.gameObject.SetActive(true);
                }
                else if (isUnlocked)
                {
                    card.statusText.text = vi ? "Phiên bản trước" : "Previous version";
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

            // 4. Nhóm nút Build: Chỉ hiển thị trên thẻ chưa sở hữu (Skin mặc định index 0 luôn coi là đã sở hữu)
            if (card.buildGroup != null)
            {
                card.buildGroup.SetActive(!isUnlocked && i > 0);
            }

            if (card.buildCostText != null)
            {
                card.buildCostText.text = GetBuildCost(i).ToString("N0");
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
