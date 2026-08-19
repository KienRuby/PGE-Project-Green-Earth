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

    [Header("Debug Info (Inspector)")]
    [SerializeField] private Vector2 debugInput;
    [SerializeField] private float debugEffectiveSpeed;
    [SerializeField] private Vector2 debugCurrentPosition;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private float moveSpeedBonus;

    private Vector2 moveInput;
    private Vector2 keyboardInput;

    public Vector2 MoveDirection => moveInput;
    public float MoveSpeed => moveSpeed + moveSpeedBonus;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        if (joystick == null)
        {
            joystick = FindObjectOfType<VirtualJoystick>();
        }
    }

    public void SetMoveSpeedBonus(float bonus)
    {
        moveSpeedBonus = Mathf.Max(0f, bonus);
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            moveInput = Vector2.zero;
            debugInput = Vector2.zero;
            return;
        }

        ReadKeyboard();

        Vector2 joystickInput =
            joystick != null
            ? joystick.Direction
            : Vector2.zero;

        // Kết hợp cả Joystick lẫn Bàn phím
        if (joystickInput.sqrMagnitude > 0.001f)
        {
            moveInput = joystickInput;
        }
        else if (keyboardInput.sqrMagnitude > 0.001f)
        {
            moveInput = keyboardInput;
        }
        else
        {
            moveInput = Vector2.zero;
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        // Cập nhật thông số Debug để theo dõi trực tiếp trên Inspector
        debugInput = moveInput;
        debugEffectiveSpeed = moveSpeed + moveSpeedBonus;
        debugCurrentPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        float currentSpeed = moveSpeed + moveSpeedBonus;
        Vector2 movement = moveInput * currentSpeed;

        if (rb != null)
        {
            rb.velocity = movement;
        }
        else
        {
            transform.position += (Vector3)(movement * Time.fixedDeltaTime);
        }
    }

    private void ReadKeyboard()
    {
        keyboardInput = Vector2.zero;

        // 1. Đọc trực tiếp phím bấm vật lý (W, A, S, D, Mũi tên)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            keyboardInput.y += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            keyboardInput.y -= 1f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            keyboardInput.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            keyboardInput.x += 1f;
        }

        // 2. Fallback sang Axis
        if (keyboardInput == Vector2.zero)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                keyboardInput.x = h;
                keyboardInput.y = v;
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) keyboardInput.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) keyboardInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardInput.x += 1f;
        }
#endif

        keyboardInput = Vector2.ClampMagnitude(keyboardInput, 1f);
    }
}