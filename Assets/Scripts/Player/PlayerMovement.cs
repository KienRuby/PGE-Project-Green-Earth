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

    [Header("Crowd Escape / Anti-Trap")]
    [Tooltip("Bán kính phát hiện quái vật xung quanh để rẽ đám đông khi Player di chuyển.")]
    [SerializeField] private float crowdPushRadius = 1.1f;

    [Tooltip("Lực đẩy quái vật dạt sang 2 bên mở đường thoát cho Player.")]
    [SerializeField] private float crowdPushForce = 4.5f;

    [Tooltip("LayerMask của quái vật (mặc định tự tìm 'Enemy').")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Debug Info (Inspector)")]
    [SerializeField] private Vector2 debugInput;
    [SerializeField] private float debugEffectiveSpeed;
    [SerializeField] private Vector2 debugCurrentPosition;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private float moveSpeedBonus;

    private Vector2 moveInput;
    private Vector2 keyboardInput;

    private static readonly Collider2D[] nearbyEnemiesBuffer = new Collider2D[24];

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

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
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

        if (joystick == null)
        {
            joystick = FindObjectOfType<VirtualJoystick>(true);
        }

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
            if (moveInput.sqrMagnitude > 0.001f)
            {
                PushNearbyEnemies(moveInput);
            }

            Vector2 targetPos = rb.position + movement * Time.fixedDeltaTime;

            if (MapBoundary.Instance != null)
            {
                targetPos = MapBoundary.Instance.ClampPlayerPosition(targetPos);
                Vector2 diff = targetPos - rb.position;
                if (Time.fixedDeltaTime > 0.0001f)
                {
                    rb.velocity = diff / Time.fixedDeltaTime;
                }
                else
                {
                    rb.velocity = movement;
                }
            }
            else
            {
                rb.velocity = movement;
            }

            // Khi Player đang chủ động di chuyển, dùng MovePosition để thoát khỏi vòng vây của quái
            if (moveInput.sqrMagnitude > 0.001f)
            {
                rb.MovePosition(targetPos);
            }
        }
        else
        {
            Vector2 targetPos = (Vector2)transform.position + movement * Time.fixedDeltaTime;
            if (MapBoundary.Instance != null)
            {
                targetPos = MapBoundary.Instance.ClampPlayerPosition(targetPos);
            }
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }
    }

    /// <summary>
    /// Đẩy dạt các quái vật xung quanh sang hai bên hướng di chuyển để mở đường thoát khi bị vây.
    /// </summary>
    private void PushNearbyEnemies(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f) return;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            layerMask = enemyLayer,
            useLayerMask = enemyLayer.value != 0
        };

        int count = Physics2D.OverlapCircle(
            rb.position,
            crowdPushRadius,
            filter,
            nearbyEnemiesBuffer
        );

        Vector2 normMove = moveDir.normalized;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = nearbyEnemiesBuffer[i];
            if (col == null || col.gameObject == gameObject) continue;

            Rigidbody2D enemyRb = col.attachedRigidbody;
            if (enemyRb == null || enemyRb.bodyType == RigidbodyType2D.Static) continue;

            // Bỏ qua Boss nếu Boss có component riêng
            if (col.GetComponent<BossMovement>() != null) continue;

            Vector2 toEnemy = enemyRb.position - rb.position;
            float dist = toEnemy.magnitude;
            if (dist < 0.001f) toEnemy = Vector2.right;

            // Kiểm tra quái có nằm ở hướng di chuyển hoặc đang ép sát Player không
            float dot = Vector2.Dot(normMove, toEnemy.normalized);
            if (dot > -0.4f)
            {
                // Hướng đẩy vuông góc với hướng Player di chuyển (sang trái hoặc phải)
                Vector2 tangent = Vector2.Perpendicular(normMove);
                if (Vector2.Dot(tangent, toEnemy) < 0f)
                {
                    tangent = -tangent;
                }

                // Kết hợp đẩy dạt sang bên (75%) và đẩy nhẹ ra ngoài (25%)
                Vector2 pushDir = (tangent * 1.5f + toEnemy.normalized * 0.5f).normalized;
                float pushStrength = Mathf.Max(0.2f, 1f - (dist / crowdPushRadius)) * crowdPushForce;
                Vector2 enemyTarget = enemyRb.position + pushDir * (pushStrength * Time.fixedDeltaTime);
                enemyRb.MovePosition(enemyTarget);
            }
        }
    }

    private void LateUpdate()
    {
        // Lớp bảo vệ chắc chắn 100% Player không bị lực đẩy của quái văng ra ngoài biên
        if (MapBoundary.Instance != null && (playerHealth == null || !playerHealth.IsDead))
        {
            Vector2 clamped = MapBoundary.Instance.ClampPlayerPosition(transform.position);
            if ((Vector2)transform.position != clamped)
            {
                transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
                if (rb != null)
                {
                    rb.position = clamped;
                }
            }
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