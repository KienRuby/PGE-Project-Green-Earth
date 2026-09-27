using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý màn hình Loading cho game (Async Scene Loading):
/// - Hiển thị nền tím hoa văn silhouette (Assets/Sprites/Backround/nền (4).png).
/// - Thanh tiến trình viền cyan, ruột hồng kẹo ngọt.
/// - Linh vật quái vật 2 đầu (quai_08) nhấp nhô và di chuyển dọc theo đầu thanh nạp.
/// - Hoạt động độc lập (Standalone Scene) hoặc dạng Overlay Singleton (DontDestroyOnLoad).
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    private static LoadingScreenUI _instance;
    private static Sprite _whiteFillSprite;
    public static LoadingScreenUI Instance => _instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform progressBarRect;
    [SerializeField] private RectTransform mascotRect;
    [SerializeField] private Animator mascotAnimator;
    [SerializeField] private TMP_Text progressText;

    [Header("Creep Mascot Variants (Mỗi lần nạp ngẫu nhiên 1 con khác nhau)")]
    [SerializeField] private GameObject[] creepVariants;
    [SerializeField] private float mascotOffset = 35f;

    [Header("Mascot Animation Settings")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobAmplitude = 4f;
    [SerializeField] private float squashAmount = 0.08f;

    [Header("Transition Settings")]
    [SerializeField] private float minDisplayDuration = 1.0f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float progressSpeed = 2.0f;

    [Header("Standalone Testing (Khi mở trực tiếp Scene Loading)")]
    [SerializeField] private bool testInStandaloneMode = false;
    [SerializeField] private string testTargetScene = "GamePlay";

    private Vector3 _mascotBaseScale = Vector3.one;
    private float _mascotBaseY = 0f;
    private float _mascotStartX;
    private float _mascotEndX;
    private float _currentProgress = 0f;
    private bool _isLoading = false;
    private int _currentVariantIndex = -1;

    public int CurrentVariantIndex => _currentVariantIndex;
    public GameObject[] CreepVariants => creepVariants;
    public float MascotStartX => _mascotStartX;
    public float MascotEndX => _mascotEndX;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (mascotRect != null)
        {
            _mascotBaseScale = mascotRect.localScale;
            _mascotBaseY = mascotRect.anchoredPosition.y;
        }

        if (creepVariants != null && creepVariants.Length > 0)
        {
            SelectRandomCreep();
        }
        else if (mascotRect != null && mascotAnimator == null)
        {
            mascotAnimator = mascotRect.GetComponentInChildren<Animator>();
        }

        if (mascotAnimator != null)
        {
            mascotAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            mascotAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        Canvas.ForceUpdateCanvases();
        CalculateMascotBounds();
        SetProgress(0f);
    }

    /// <summary>
    /// Chọn ngẫu nhiên một con quái khác với con đang hiển thị trước đó.
    /// Kích hoạt visual của con được chọn, tắt các con còn lại và cập nhật Animator.
    /// </summary>
    public void SelectRandomCreep()
    {
        if (creepVariants == null || creepVariants.Length == 0) return;

        int newIndex;
        if (creepVariants.Length == 1)
        {
            newIndex = 0;
        }
        else
        {
            do
            {
                newIndex = Random.Range(0, creepVariants.Length);
            } while (newIndex == _currentVariantIndex);
        }

        _currentVariantIndex = newIndex;
        for (int i = 0; i < creepVariants.Length; i++)
        {
            if (creepVariants[i] != null)
            {
                bool active = (i == _currentVariantIndex);
                creepVariants[i].SetActive(active);
                if (active)
                {
                    mascotAnimator = creepVariants[i].GetComponentInChildren<Animator>();
                    if (mascotAnimator != null)
                    {
                        mascotAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                        mascotAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    }
                }
            }
        }
    }

    private void Start()
    {
        // Nếu người dùng mở trực tiếp Scene Loading để test
        if (testInStandaloneMode && !string.IsNullOrEmpty(testTargetScene))
        {
            LoadScene(testTargetScene, minDisplayDuration);
        }
    }

    private void CalculateMascotBounds()
    {
        if (progressBarRect == null) return;

        if (fillImage != null && fillImage.rectTransform.rect.width > 0f)
        {
            Vector3[] corners = new Vector3[4];
            fillImage.rectTransform.GetWorldCorners(corners);
            _mascotStartX = progressBarRect.InverseTransformPoint(corners[0]).x + mascotOffset;
            _mascotEndX = progressBarRect.InverseTransformPoint(corners[3]).x + mascotOffset;
            return;
        }

        float barWidth = progressBarRect.rect.width > 0f ? progressBarRect.rect.width : 744f;
        _mascotStartX = -barWidth * 0.5f + mascotOffset;
        _mascotEndX = barWidth * 0.5f + mascotOffset;
    }

    private void Update()
    {
        if (!_isLoading) return;

        AnimateMascot();
    }

    private void AnimateMascot()
    {
        if (mascotRect == null) return;

        float posX = Mathf.Lerp(_mascotStartX, _mascotEndX, _currentProgress);

        if (mascotAnimator != null)
        {
            // Quái vật có Animator diễn hoạt bước đi (Walk):
            // Animator diễn hoạt chuyển động chân và dáng đi, giữ nguyên scale chuẩn
            float time = Time.unscaledTime * bobFrequency;
            float bob = (bobAmplitude > 0f) ? Mathf.Abs(Mathf.Sin(time)) * bobAmplitude : 0f;
            mascotRect.anchoredPosition = new Vector2(posX, _mascotBaseY + bob);
            mascotRect.localScale = _mascotBaseScale;
        }
        else
        {
            // Fallback khi mascot là ảnh tĩnh: dùng nhấp nhô squash/stretch
            float time = Time.unscaledTime * bobFrequency;
            float bob = Mathf.Abs(Mathf.Sin(time)) * bobAmplitude;
            float squash = 1f + Mathf.Sin(time * 2f) * squashAmount;

            mascotRect.anchoredPosition = new Vector2(posX, _mascotBaseY + bob);
            mascotRect.localScale = new Vector3(_mascotBaseScale.x * (2f - squash), _mascotBaseScale.y * squash, _mascotBaseScale.z);
        }
    }

    public void SetProgress(float progress)
    {
        _currentProgress = Mathf.Clamp01(progress);

        if (fillImage != null)
        {
            if (fillImage.sprite == null)
            {
                if (_whiteFillSprite == null)
                    _whiteFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
                fillImage.sprite = _whiteFillSprite;
            }
            fillImage.fillAmount = _currentProgress;
        }

        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(_currentProgress * 100f)}%";

        if (mascotRect != null && progressBarRect != null)
        {
            float posX = Mathf.Lerp(_mascotStartX, _mascotEndX, _currentProgress);
            mascotRect.anchoredPosition = new Vector2(posX, mascotRect.anchoredPosition.y);
        }
    }

    /// <summary>
    /// API toàn cục để tải bất kỳ scene nào kèm màn hình loading.
    /// </summary>
    public static void Load(string sceneName, float minDuration = 1.0f)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[LoadingScreenUI] Tên Scene rỗng, không thể tải.");
            return;
        }

        if (_instance == null)
        {
            // Thử nạp từ Resources
            GameObject prefab = Resources.Load<GameObject>("UI/LoadingScreen");
            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab);
                _instance = obj.GetComponent<LoadingScreenUI>();
            }
            else
            {
                Debug.LogWarning("[LoadingScreenUI] Không tìm thấy prefab Resources/UI/LoadingScreen, fallback sang SceneManager.LoadScene.");
                SceneManager.LoadScene(sceneName);
                return;
            }
        }

        if (_instance != null)
        {
            _instance.LoadScene(sceneName, minDuration);
        }
        else
        {
            Debug.LogError("[LoadingScreenUI] Không tìm thấy component LoadingScreenUI trên Prefab, fallback sang SceneManager.LoadScene.");
            SceneManager.LoadScene(sceneName);
        }
    }

    public void LoadScene(string sceneName, float minDuration = 1.0f)
    {
        if (_isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName, minDuration));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float minDuration)
    {
        _isLoading = true;
        SelectRandomCreep();
        Canvas.ForceUpdateCanvases();
        CalculateMascotBounds();
        SetProgress(0f);

        // 1. Fade In màn hình loading
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // 2. Tải cảnh bất đồng bộ ngầm
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[LoadingScreenUI] Không thể nạp Scene '{sceneName}'. Kiểm tra lại Build Settings.");
            _isLoading = false;
            yield break;
        }

        op.allowSceneActivation = false;

        float startTime = Time.unscaledTime;
        float targetProgress = 0f;

        while (!op.isDone)
        {
            // AsyncOperation.progress chạy từ 0 -> 0.9 khi đang tải
            float rawProgress = Mathf.Clamp01(op.progress / 0.9f);

            // Đảm bảo thanh loading chạy mượt mà và tôn trọng minDuration
            float timeRatio = Mathf.Clamp01((Time.unscaledTime - startTime) / Mathf.Max(0.1f, minDuration));
            targetProgress = Mathf.Min(rawProgress, timeRatio);

            _currentProgress = Mathf.MoveTowards(_currentProgress, targetProgress, progressSpeed * Time.unscaledDeltaTime);
            SetProgress(_currentProgress);

            // Khi cả cảnh đã tải xong ngầm và đã đủ thời gian tối thiểu
            if (op.progress >= 0.9f && (Time.unscaledTime - startTime) >= minDuration && _currentProgress >= 0.99f)
            {
                SetProgress(1f);
                yield return new WaitForSecondsRealtime(0.15f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        // 3. Đợi cảnh mới khởi tạo Awake/Start xong
        yield return null;

        // 4. Fade Out biến mất
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        _isLoading = false;
    }
}
