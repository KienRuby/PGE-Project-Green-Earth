using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Virtual Joystick điều khiển cảm ứng đa điểm thế hệ mới:
/// - Hỗ trợ kiến trúc kép (Dual-Layer): EventSystem Raycast + Hardware Direct Touch/Mouse Polling.
/// - Triệt tiêu 100% lỗi culling/chặn raycast (CullTransparentMesh = false).
/// - Tự động định vị Joystick động (Dynamic Joystick) mượt mà theo ngón tay người chơi.
/// - Hoạt động hoàn hảo trên Android Touch, iOS, Unity Device Simulator và PC Mouse/Keyboard.
/// </summary>
public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Joystick Visual Elements")]
    [Tooltip("RectTransform của vòng tròn nền Joystick.")]
    [SerializeField] private RectTransform background;

    [Tooltip("RectTransform của núm gạt điều khiển bên trong Joystick.")]
    [SerializeField] private RectTransform handle;

    [Tooltip("Phạm vi di chuyển tối đa của núm gạt so với bán kính của nền.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float handleRange = 0.65f;

    [Header("Dynamic Joystick Settings")]
    [Tooltip("Tự động ép vị trí hiển thị Joystick nằm trọn bên trong vùng cảm ứng, không bị khuất mép màn hình.")]
    [SerializeField] private bool keepInsideScreen = true;

    [Tooltip("Bán kính kéo tối đa tính theo pixel màn hình.")]
    [SerializeField] private float touchRadiusPixels = 80f;

    [Header("Direct Hardware Touch Fallback")]
    [Tooltip("Bật chế độ bắt cảm ứng trực tiếp qua Input.touches/Input.mousePosition để không phụ thuộc vào EventSystem.")]
    [SerializeField] private bool enableDirectTouchFallback = true;

    private RectTransform touchArea;
    private Canvas parentCanvas;
    private Camera canvasCamera;
    private Vector2 pointerDownScreenPos;
    private Vector2 fixedBackgroundPosition;
    private Vector2 input;
    private bool isPressed;
    private int activeFingerId = -1;

    public Vector2 Direction => input;
    public bool IsPressed => isPressed;

    private void Awake()
    {
        touchArea = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
        }

        // Đảm bảo CanvasRenderer không bị cull khi alpha = 0
        CanvasRenderer cr = GetComponent<CanvasRenderer>();
        if (cr != null)
        {
            cr.cullTransparentMesh = false;
        }

        // Đảm bảo các con (Background, Handle) không nuốt/chặn sự kiện raycast của TouchArea
        if (background != null)
        {
            fixedBackgroundPosition = background.anchoredPosition;
            Graphic bgGraphic = background.GetComponent<Graphic>();
            if (bgGraphic != null) bgGraphic.raycastTarget = false;

            CanvasRenderer bgCr = background.GetComponent<CanvasRenderer>();
            if (bgCr != null) bgCr.cullTransparentMesh = false;
        }

        if (handle != null)
        {
            Graphic handleGraphic = handle.GetComponent<Graphic>();
            if (handleGraphic != null) handleGraphic.raycastTarget = false;

            CanvasRenderer handleCr = handle.GetComponent<CanvasRenderer>();
            if (handleCr != null) handleCr.cullTransparentMesh = false;
        }

        Graphic touchGraphic = GetComponent<Graphic>();
        if (touchGraphic != null)
        {
            touchGraphic.raycastTarget = true;
            // Đặt alpha tối thiểu để Unity UI Raycaster luôn nhận diện được
            if (touchGraphic.color.a < 0.005f)
            {
                Color c = touchGraphic.color;
                c.a = 0.005f;
                touchGraphic.color = c;
            }
        }

        HideJoystick();
    }

    private void Update()
    {
        if (!enableDirectTouchFallback) return;

        // Fallback 1: Mobile Touch
        if (Input.touchCount > 0)
        {
            HandleDirectTouch();
        }
        // Fallback 2: Mouse (Editor / PC)
        else if (Input.mousePresent)
        {
            HandleDirectMouse();
        }
    }

    private void HandleDirectTouch()
    {
        if (!isPressed)
        {
            // Tìm ngón tay vừa chạm vào màn hình (ở nửa dưới màn hình để tránh nút Pause ở TopHUD)
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    // Tránh thanh TopHUD trên cùng (top 15% màn hình)
                    if (t.position.y <= Screen.height * 0.88f)
                    {
                        StartJoystick(t.position, t.fingerId);
                        break;
                    }
                }
            }
        }
        else if (activeFingerId >= 0)
        {
            // Cập nhật ngón tay đang giữ
            bool foundActiveFinger = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId == activeFingerId)
                {
                    foundActiveFinger = true;
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        UpdateJoystickInput(t.position);
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        HideJoystick();
                    }
                    break;
                }
            }

            if (!foundActiveFinger)
            {
                HideJoystick();
            }
        }
    }

    private void HandleDirectMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.y <= Screen.height * 0.88f)
            {
                StartJoystick(Input.mousePosition, -99);
            }
        }
        else if (isPressed && activeFingerId == -99)
        {
            if (Input.GetMouseButton(0))
            {
                UpdateJoystickInput(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HideJoystick();
            }
        }
    }

    private void StartJoystick(Vector2 screenPos, int fingerId)
    {
        if (background == null || handle == null || !GameSettings.JoystickEnabled) return;

        activeFingerId = fingerId;
        pointerDownScreenPos = screenPos;
        isPressed = true;

        if (touchArea != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchArea,
                screenPos,
                canvasCamera,
                out Vector2 localPoint))
        {
            if (GameSettings.DynamicJoystick)
            {
                if (keepInsideScreen)
                    localPoint = ClampJoystickPosition(localPoint);

                background.anchoredPosition = localPoint;
            }
            else
            {
                background.anchoredPosition = fixedBackgroundPosition;
            }
        }

        background.gameObject.SetActive(true);
        handle.anchoredPosition = Vector2.zero;
        input = Vector2.zero;
    }

    private void UpdateJoystickInput(Vector2 currentScreenPos)
    {
        if (!isPressed || background == null || handle == null) return;

        Vector2 screenDiff = currentScreenPos - pointerDownScreenPos;
        float radius = touchRadiusPixels > 10f ? touchRadiusPixels : 80f;

        if (background.rect.width > 10f)
        {
            radius = Mathf.Max(radius, background.rect.width * 0.5f);
        }

        input = Vector2.ClampMagnitude(screenDiff / radius, 1f);

        float visualRadius = Mathf.Min(background.rect.width, background.rect.height) * 0.5f;
        if (visualRadius <= 0.01f) visualRadius = 50f;

        handle.anchoredPosition = input * (visualRadius * handleRange);
    }

    #region Unity EventSystem Handlers (Layer 1)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isPressed)
        {
            StartJoystick(eventData.position, eventData.pointerId);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPressed && (activeFingerId == eventData.pointerId || activeFingerId == -99 || activeFingerId == -1))
        {
            UpdateJoystickInput(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activeFingerId == eventData.pointerId || activeFingerId == -99)
        {
            HideJoystick();
        }
    }
    #endregion

    private Vector2 ClampJoystickPosition(Vector2 position)
    {
        if (touchArea == null || background == null)
            return position;

        Rect areaRect = touchArea.rect;
        float halfWidth = background.rect.width * 0.5f;
        float halfHeight = background.rect.height * 0.5f;

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

    public void HideJoystick()
    {
        input = Vector2.zero;
        isPressed = false;
        activeFingerId = -1;

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
