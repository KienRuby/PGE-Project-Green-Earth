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
        SceneManager.LoadScene(gameplaySceneName);
    }
}
