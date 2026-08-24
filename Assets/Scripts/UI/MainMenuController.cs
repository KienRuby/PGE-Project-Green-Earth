using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý chuyển cảnh từ MainMenu sang GamePlay thông qua nút Battle/ChapterButton.
/// Tự động tìm kiếm và gán sự kiện cho ChapterButton nếu chưa kéo thả trong Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [Tooltip("Nút bấm vào màn chơi chính (mặc định tìm ChapterButton).")]
    [SerializeField] private Button playButton;

    [Tooltip("Tên Scene Gameplay chính.")]
    [SerializeField] private string gameplaySceneName = "GamePlay";

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }
    }

    public void StartGame()
    {
        int selectedIndex = PlayerDataService.SelectedChapterIndex;
        ChapterDatabase db = null;
#if UNITY_EDITOR
        db = UnityEditor.AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
#endif
        if (db == null)
        {
            db = Resources.Load<ChapterDatabase>("ChapterDatabase");
        }

        int cost = 10;
        if (db != null)
        {
            ChapterData chapter = db.GetChapter(selectedIndex);
            if (chapter != null)
            {
                cost = chapter.energyCost;
            }
        }

        if (ChipManager.TrySpendEnergy(cost))
        {
            Debug.Log($"[MainMenuController] ⚡ Đã trừ {cost} Energy để bắt đầu Chapter {selectedIndex + 1}. Số dư còn lại: {ChipManager.Energy}");
            GameEvents.RaiseChapterPlayed(selectedIndex);
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogWarning($"[MainMenuController] ⚠️ Không đủ Energy ({ChipManager.Energy}/{cost}) để vào trận!");
        }
    }
}
