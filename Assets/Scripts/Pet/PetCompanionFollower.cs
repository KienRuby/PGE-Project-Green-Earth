using System;
using UnityEngine;

/// <summary>
/// Điều khiển Pet Companion di chuyển bay/đi theo Player trong trận đấu (GamePlay).
/// Đặc tính:
/// - Hoàn toàn bị động: Không có Collider, không nhận sát thương, không tấn công quái ("không làm gì cả").
/// - LUÔN LUÔN ĐI SAU LƯNG PLAYER: Tự động tính toán vị trí sau lưng theo hướng di chuyển/nhìn của Player.
/// - Lật Sprite (flipX) chuẩn xác: Toàn bộ 7 Sprite Pet gốc đều quay mặt sang phải (Face Right).
///   Khi di chuyển sang phải: flipX = false (quay phải).
///   Khi di chuyển sang trái: flipX = true (quay trái).
///   Khi đứng yên: Nhìn cùng hướng với Player.
/// - Bay theo sau Player mượt mà bằng Vector3.SmoothDamp.
/// - Hiệu ứng bồng bềnh (bobbing) nhẹ nhàng theo hàm Sin.
/// - Giữ khoảng cách an toàn (minDistance) không bao giờ đè lên Player.
/// </summary>
public class PetCompanionFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Transform của Player cần theo sau.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Khoảng cách giữ cự ly sau lưng người chơi (mét).")]
    [SerializeField] private float followDistance = 0.7f;

    [Tooltip("Khoảng cách tối thiểu với tâm Player để chống đè hình ảnh (mét).")]
    [SerializeField] private float minPlayerDistance = 0.35f;

    [Tooltip("Tốc độ theo sau tối đa (m/s).")]
    [SerializeField] private float maxFollowSpeed = 16.0f;

    [Tooltip("Thời gian làm mượt (SmoothDamp smoothTime).")]
    [SerializeField] private float smoothTime = 0.12f;

    [Tooltip("Tốc độ xoay vòng quanh Player để đổi vị trí ra sau lưng (m/s).")]
    [SerializeField] private float offsetTransitionSpeed = 5.0f;

    [Header("Bobbing Effect")]
    [Tooltip("Biên độ bồng bềnh lên xuống.")]
    [SerializeField] private float bobbingAmplitude = 0.04f;

    [Tooltip("Tần số bồng bềnh (chu kỳ/giây).")]
    [SerializeField] private float bobbingFrequency = 3.0f;

    [Header("Visual & Scale")]
    [Tooltip("Kích cỡ hiển thị trong trận đấu (giảm 1 nửa từ 0.22f xuống 0.11f theo yêu cầu).")]
    [SerializeField] private float petScale = 0.11f;

    [Tooltip("Góc nghiêng tối đa khi di chuyển (độ).")]
    [SerializeField] private float tiltAmount = 8f;

    [SerializeField] private SpriteRenderer spriteRenderer;

    // Runtime state
    private Vector3 currentVelocity;
    private Vector2 currentFollowOffset;
    private Vector2 playerFacingDirection = Vector2.right;
    private Vector3 lastPlayerPos;
    private PlayerMovement playerMovement;
    private float bobbingTimer;
    private int petId = -1;
    private bool currentFlipX = false;

    public int PetId => petId;
    public float PetScale
    {
        get => petScale;
        set
        {
            petScale = Mathf.Max(0.01f, value);
            transform.localScale = new Vector3(petScale, petScale, 1f);
        }
    }

    public Transform PlayerTransform
    {
        get => playerTransform;
        set
        {
            playerTransform = value;
            if (playerTransform != null)
            {
                playerMovement = playerTransform.GetComponent<PlayerMovement>();
                lastPlayerPos = playerTransform.position;
            }
        }
    }

    public SpriteRenderer PetSpriteRenderer => spriteRenderer;
    public Vector2 CurrentFollowOffset => currentFollowOffset;
    public Vector2 PlayerFacingDirection => playerFacingDirection;

    public void Initialize(Transform targetPlayer, int id, Sprite sprite = null)
    {
        playerTransform = targetPlayer;
        petId = id;

        if (playerTransform != null)
        {
            playerMovement = playerTransform.GetComponent<PlayerMovement>();
            lastPlayerPos = playerTransform.position;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }

        // Toàn bộ Sprite Pet gốc quay mặt sang PHẢI, mặc định không flip
        currentFlipX = false;
        spriteRenderer.flipX = false;

        // Cấu hình sorting order hiển thị đẹp, nằm ngay sau Player
        spriteRenderer.sortingOrder = 14;

        transform.localScale = new Vector3(petScale, petScale, 1f);

        // Mặc định xuất phát sau lưng Player (bên trái khi Player nhìn sang phải)
        currentFollowOffset = new Vector2(-followDistance, 0.15f);
        if (playerTransform != null)
        {
            transform.position = CalculateTargetPosition(0f);
        }
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        transform.localScale = new Vector3(petScale, petScale, 1f);
        currentFollowOffset = new Vector2(-followDistance, 0.15f);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            FindPlayerIfMissing();
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayerIfMissing();
            if (playerTransform == null) return;
        }

        UpdateMovement(Time.deltaTime);
    }

    private void FindPlayerIfMissing()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMovement = playerTransform.GetComponent<PlayerMovement>();
            lastPlayerPos = playerTransform.position;
            if (transform.position == Vector3.zero)
            {
                transform.position = CalculateTargetPosition(0f);
            }
        }
    }

    /// <summary>
    /// Tính toán vị trí mục tiêu luôn nằm SAU LƯNG người chơi theo hướng di chuyển/mặt nhìn.
    /// </summary>
    public Vector3 CalculateTargetPosition(float deltaTime)
    {
        if (playerTransform == null) return transform.position;

        // 1. Cập nhật hướng di chuyển / hướng nhìn của Player
        Vector3 playerPos = playerTransform.position;
        Vector2 delta = (Vector2)(playerPos - lastPlayerPos);

        if (playerMovement != null && playerMovement.MoveDirection.sqrMagnitude > 0.01f)
        {
            playerFacingDirection = playerMovement.MoveDirection.normalized;
        }
        else if (delta.sqrMagnitude > 0.0001f)
        {
            playerFacingDirection = delta.normalized;
        }
        lastPlayerPos = playerPos;

        // 2. Vị trí SAU LƯNG Player: Vector ngược hướng nhìn + nhích nhẹ trục Y cho góc nhìn 2.5D
        Vector2 targetBehindOffset = -playerFacingDirection * followDistance + new Vector2(0f, 0.15f);

        // 3. Chuyển đổi mượt mà góc bay ra sau lưng khi Player đổi hướng
        if (deltaTime > 0f)
        {
            currentFollowOffset = Vector2.Lerp(currentFollowOffset, targetBehindOffset, deltaTime * offsetTransitionSpeed);
        }
        else
        {
            currentFollowOffset = targetBehindOffset;
        }

        // Vị trí tâm Player
        Vector3 playerCenter = playerPos + new Vector3(0f, 0.2f, 0f);

        // Hiệu ứng dao động bồng bềnh Sinusoidal Bobbing
        bobbingTimer += deltaTime * bobbingFrequency;
        float bobOffset = Mathf.Sin(bobbingTimer) * bobbingAmplitude;

        return playerCenter + (Vector3)currentFollowOffset + new Vector3(0f, bobOffset, 0f);
    }

    private void UpdateMovement(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        Vector3 targetPos = CalculateTargetPosition(deltaTime);
        Vector3 prevPos = transform.position;

        // 1. Di chuyển mượt mà tới vị trí mục tiêu sau lưng
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref currentVelocity,
            smoothTime,
            maxFollowSpeed,
            deltaTime
        );

        // 2. Chống đè lấn lên Player (Anti-Overlap Guard)
        if (playerTransform != null)
        {
            Vector3 playerCenter = playerTransform.position + new Vector3(0f, 0.2f, 0f);
            Vector3 toPet = transform.position - playerCenter;
            toPet.z = 0f;
            float dist = toPet.magnitude;
            if (dist < minPlayerDistance)
            {
                Vector3 pushDir = dist > 0.001f ? toPet.normalized : new Vector3(-playerFacingDirection.x, -playerFacingDirection.y, 0f);
                if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector3.left;
                transform.position = playerCenter + pushDir * minPlayerDistance;
                if (Vector3.Dot(currentVelocity, pushDir) < 0f)
                {
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, pushDir);
                }
            }
        }

        // 3. Hiệu ứng nghiêng nhẹ khi di chuyển
        float horizVel = (transform.position.x - prevPos.x) / Mathf.Max(0.0001f, deltaTime);
        float targetTilt = Mathf.Clamp(-horizVel * 1.5f, -tiltAmount, tiltAmount);
        Quaternion desiredRot = Quaternion.Euler(0f, 0f, targetTilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, deltaTime * 8f);

        // 4. LẬT MẶT SPRITE (FLIPX) CHUẨN XÁC:
        // QUY TẮC: Toàn bộ 7 Sprite gốc (Bat, Slime, Spider, Snake, Dog, Turtle, Snail) đều quay mặt sang PHẢI.
        // - Khi di chuyển sang PHẢI (horizVel > 0.05f) => flipX = false (quay mặt sang phải).
        // - Khi di chuyển sang TRÁI (horizVel < -0.05f) => flipX = true (quay mặt sang trái).
        // - Khi đứng yên / di chuyển thẳng đứng: Nhìn theo hướng mặt của Player.
        if (spriteRenderer != null)
        {
            if (horizVel > 0.05f)
            {
                currentFlipX = false; // Quay phải
            }
            else if (horizVel < -0.05f)
            {
                currentFlipX = true; // Quay trái
            }
            else
            {
                // Khi đứng yên, nhìn cùng hướng ngang với Player
                if (playerFacingDirection.x > 0.05f)
                {
                    currentFlipX = false; // Player nhìn phải => Pet nhìn phải
                }
                else if (playerFacingDirection.x < -0.05f)
                {
                    currentFlipX = true; // Player nhìn trái => Pet nhìn trái
                }
            }

            spriteRenderer.flipX = currentFlipX;
        }
    }
}
