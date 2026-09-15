using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adapter tương thích cho các scene cũ. ChapterScreenController là nơi duy nhất
/// kiểm tra, trừ năng lượng và chuyển sang gameplay.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [Tooltip("Nút Start cũ. Nếu ChapterScreenController đã sở hữu nút này thì adapter sẽ không gắn listener lần hai.")]
    [SerializeField] private Button playButton;

    [SerializeField] private ChapterScreenController chapterScreenController;

    private void Start()
    {
        ResolveChapterScreen();
        if (playButton != null &&
            (chapterScreenController == null || !chapterScreenController.OwnsStartButton(playButton)))
        {
            playButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(StartGame);
    }

    public void StartGame()
    {
        ResolveChapterScreen();
        if (chapterScreenController == null)
        {
            Debug.LogError("[MainMenuController] Không tìm thấy ChapterScreenController; không thể bắt đầu Chapter.");
            return;
        }

        chapterScreenController.OnStartButtonClicked();
    }

    private void ResolveChapterScreen()
    {
        if (chapterScreenController == null)
        {
            chapterScreenController = FindObjectOfType<ChapterScreenController>();
        }
    }
}
