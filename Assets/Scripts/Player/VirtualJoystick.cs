using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Joystick")]
    [Tooltip("RectTransform của vòng tròn nền Joystick.")]
    [SerializeField] private RectTransform background;

    [Tooltip("RectTransform của núm gạt điều khiển bên trong Joystick.")]
    [SerializeField] private RectTransform handle;

    [Tooltip("Phạm vi di chuyển tối đa của núm gạt so với bán kính của nền.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float handleRange = 0.65f;

    [Header("Dynamic Joystick")]
    [Tooltip("Tự động ép vị trí hiển thị Joystick nằm trọn bên trong vùng cảm ứng, không bị khuất mép màn hình.")]
    [SerializeField] private bool keepInsideScreen = true;

    private RectTransform touchArea;

    private Vector2 input;
    private bool isPressed;

    public Vector2 Direction => input;

    private void Awake()
    {
        touchArea = transform as RectTransform;

        HideJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        // Không cho joystick bị tràn ra ngoài màn hình.
        if (keepInsideScreen)
            localPoint = ClampJoystickPosition(localPoint);

        background.anchoredPosition = localPoint;

        background.gameObject.SetActive(true);

        handle.anchoredPosition = Vector2.zero;

        input = Vector2.zero;
        isPressed = true;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed || background == null || handle == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float radius =
            Mathf.Min(
                background.rect.width,
                background.rect.height
            ) * 0.5f;

        if (radius <= 0f)
            return;

        input = Vector2.ClampMagnitude(
            localPoint / radius,
            1f
        );

        handle.anchoredPosition =
            input *
            radius *
            handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HideJoystick();
    }

    private Vector2 ClampJoystickPosition(Vector2 position)
    {
        if (touchArea == null || background == null)
            return position;

        Rect areaRect = touchArea.rect;

        float halfWidth =
            background.rect.width * 0.5f;

        float halfHeight =
            background.rect.height * 0.5f;

        position.x = Mathf.Clamp(
            position.x,
            areaRect.xMin + halfWidth,
            areaRect.xMax - halfWidth
        );

        position.y = Mathf.Clamp(
            position.y,
            areaRect.yMin + halfHeight,
            areaRect.yMax - halfHeight
        );

        return position;
    }

    private void HideJoystick()
    {
        input = Vector2.zero;
        isPressed = false;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (background != null)
            background.gameObject.SetActive(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            HideJoystick();
        }
    }

    private void OnDisable()
    {
        HideJoystick();
    }
}