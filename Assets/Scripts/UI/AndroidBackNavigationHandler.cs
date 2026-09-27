using UnityEngine;
using UnityEngine.SceneManagement;

namespace PGE.UI
{
    /// <summary>
    /// Xử lý phím Back phần cứng / Cử chỉ vuốt cạnh thoát trên Android (KeyCode.Escape):
    /// - Đóng các modal popup đang mở theo thứ tự ưu tiên (RewardPopup, DailyGemMine, Pity, v.v.).
    /// - Trong trận đánh (Gameplay): Tự động bật menu Tạm dừng (Pause Menu) hoặc tiếp tục chơi (Resume).
    /// - Tại màn hình chính (MainMenu): Nhắc nhở hoặc thoát ứng dụng an toàn.
    /// - Tự động khởi tạo một lần duy nhất qua RuntimeInitializeOnLoadMethod, không cần kéo thả vào Scene.
    /// </summary>
    public class AndroidBackNavigationHandler : MonoBehaviour
    {
        private static AndroidBackNavigationHandler instance;
        private float lastBackPressTime = -10f;
        private const float DoubleBackExitInterval = 2.0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance != null) return;

            GameObject go = new GameObject("[AndroidBackNavigationHandler]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AndroidBackNavigationHandler>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackPressed();
            }
        }

        private void HandleBackPressed()
        {
            // 1. Kiểm tra Reward Popup (Daily Login / Achievements)
            if (RewardPopupController.Instance != null && RewardPopupController.Instance.IsOpen)
            {
                RewardPopupController.Instance.ClosePopup();
                return;
            }

            // 2. Kiểm tra Daily Gem Mine Modal
            DailyGemMineModalController gemMine = FindObjectOfType<DailyGemMineModalController>(true);
            if (gemMine != null && gemMine.IsOpen)
            {
                gemMine.CloseModal();
                return;
            }

            // 3. Kiểm tra Pity Guarantee Panel
            PityGuaranteePanel pity = FindObjectOfType<PityGuaranteePanel>(true);
            if (pity != null && pity.IsOpen)
            {
                pity.Close();
                return;
            }

            // 4. Kiểm tra Pause Menu trong Gameplay
            PauseModalController pause = FindObjectOfType<PauseModalController>(true);
            if (pause != null)
            {
                if (pause.IsPaused)
                {
                    pause.ResumeGame();
                }
                else
                {
                    pause.OpenPauseModal();
                }
                return;
            }

            // 5. Nếu đang ở MainMenu và không có popup nào mở: Thoát khi bấm 2 lần
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "MainMenu" || currentScene.Contains("Menu"))
            {
                if (Time.unscaledTime - lastBackPressTime <= DoubleBackExitInterval)
                {
                    Application.Quit();
                }
                else
                {
                    lastBackPressTime = Time.unscaledTime;
                    Debug.Log("[AndroidBack] Bấm Back một lần nữa để thoát trò chơi.");
                }
            }
        }
    }
}
