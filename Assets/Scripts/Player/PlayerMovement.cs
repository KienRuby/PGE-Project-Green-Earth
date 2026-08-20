using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển cơ bản của Player.")]
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
    public float MoveSpeed => EffectiveSpeed;
    public float EffectiveSpeed => Mathf.Max(3.5f, moveSpeed) + moveSpeedBonus;

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
            joystick = FindObjectOfType<VirtualJoystick>(true);
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

        Vector2 joystickInput = (joystick != null && joystick.gameObject.activeInHierarchy)
            ? joystick.Direction
            : Vector2.zero;

        // Ưu tiên Joystick nếu đang gạt, hoặc dùng Bàn phím
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

        debugInput = moveInput;
        debugEffectiveSpeed = EffectiveSpeed;
        debugCurrentPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        float currentSpeed = EffectiveSpeed;
        Vector2 movement = moveInput * currentSpeed;

        if (rb != null)
        {
            rb.velocity = movement;

            // Fallback nếu Rigidbody kinematic hoặc bị kẹt
            if (rb.bodyType == RigidbodyType2D.Kinematic)
            {
                rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
            }
        }
        else
        {
            transform.position += (Vector3)(movement * Time.fixedDeltaTime);
        }
    }

    private void ReadKeyboard()
    {
        keyboardInput = Vector2.zero;

        // 1. Phím vật lý W, A, S, D và Mũi tên
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

        // 2. Fallback sang Input.GetAxisRaw ("Horizontal", "Vertical")
        if (keyboardInput == Vector2.zero)
        {
            try
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                {
                    keyboardInput.x = h;
                    keyboardInput.y = v;
                }
            }
            catch
            {
                // Bỏ qua nếu chưa định nghĩa Axis
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