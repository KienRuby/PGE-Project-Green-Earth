using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển của Player.")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Mobile")]
    [Tooltip("Tham chiếu tới VirtualJoystick trên UI điều khiển cho điện thoại/cảm ứng.")]
    [SerializeField] private VirtualJoystick joystick;

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 keyboardInput;

    public Vector2 MoveDirection => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        ReadKeyboard();

        Vector2 joystickInput =
            joystick != null
            ? joystick.Direction
            : Vector2.zero;

        // Ưu tiên joystick nếu người chơi đang chạm joystick
        if (joystickInput.sqrMagnitude > 0.01f)
        {
            moveInput = joystickInput;
        }
        else
        {
            moveInput = keyboardInput;
        }

        // Không cho đi chéo nhanh hơn
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        Vector2 targetPosition =
            rb.position +
            moveInput * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);
    }

    private void ReadKeyboard()
    {
        keyboardInput = Vector2.zero;

#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            keyboardInput.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            keyboardInput.y -= 1f;
        }

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            keyboardInput.x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            keyboardInput.x += 1f;
        }

#else

        keyboardInput.x =
            Input.GetAxisRaw("Horizontal");

        keyboardInput.y =
            Input.GetAxisRaw("Vertical");

#endif

        keyboardInput =
            Vector2.ClampMagnitude(
                keyboardInput,
                1f
            );
    }
}