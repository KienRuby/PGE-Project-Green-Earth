using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Component gắn vào các thành phần UI (Button, Toggle, Tab, Popup...)
/// để tự động phát âm thanh khi Click hoặc Hover mà không cần viết code thủ công.
/// </summary>
public class UIAudioTrigger : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, ISubmitHandler, IPointerEnterHandler
{
    [Header("Click / Select Sound")]
    [Tooltip("Mã định danh SoundId khi Click vào UI (ví dụ: UI_ButtonClick, UI_TabSwitch)")]
    [SerializeField] private string clickSoundId = SoundIdConst.UI_BUTTON_CLICK;

    [Tooltip("Âm lượng nhân thêm cho âm thanh Click này")]
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    [Header("Hover Sound")]
    [Tooltip("Có phát âm thanh khi rê chuột/con trỏ vào UI không")]
    [SerializeField] private bool playOnHover = false;

    [Tooltip("Mã định danh SoundId khi Hover vào UI")]
    [SerializeField] private string hoverSoundId = "UI_ButtonHover";

    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.7f;

    private Button button;
    private Toggle toggle;

    private void Awake()
    {
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();

    }

    // Input handlers survive UI controllers rebuilding Button.onClick listeners.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (button != null && button.IsInteractable() || toggle != null && toggle.IsInteractable())
            PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (button != null && button.IsInteractable() || toggle != null && toggle.IsInteractable())
            PlayClick();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Nếu không có Button hoặc Toggle (chẳng hạn UI Image, Panel) thì tự kích hoạt
        if (button == null && toggle == null)
        {
            PlayClick();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playOnHover && !string.IsNullOrEmpty(hoverSoundId) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(hoverSoundId, hoverVolume);
        }
    }

    public void PlayClick()
    {
        if (!string.IsNullOrEmpty(clickSoundId) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(clickSoundId, clickVolume);
        }
    }
}
