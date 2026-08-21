using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Mục tiêu mà Camera sẽ bám theo (thường là Transform của Player).")]
    [SerializeField] private Transform target;

    [Header("Camera Follow")]
    [Tooltip("Tốc độ bám theo mục tiêu. 0 = bám ngay lập tức. Giá trị khoảng 8-15 thường rất mượt.")]
    [SerializeField, Range(0f, 30f)]
    private float followSpeed = 12f;

    [Header("Offset")]
    [Tooltip("Độ lệch vị trí giữa Camera và mục tiêu.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Anti Jitter")]
    [Tooltip("Khoảng cách cực nhỏ sẽ snap thẳng vào target để loại bỏ rung vi mô.")]
    [SerializeField] private float snapThreshold = 0.001f;

    private float cameraZ;
    private bool isCameraZInitialized;

    private void Awake()
    {
        InitializeCameraZ();
    }

    private void InitializeCameraZ()
    {
        cameraZ = transform.position.z;
        isCameraZInitialized = true;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
                SnapToTarget();
            }
        }
    }

    public float FollowSpeed
    {
        get => followSpeed;
        set => followSpeed = value;
    }

    public Vector2 Offset
    {
        get => offset;
        set => offset = value;
    }

    private Camera cachedCamera;

    private Camera GetCameraComponent()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }
        }
        return cachedCamera;
    }

    public void UpdateFollow(float deltaTime)
    {
        if (target == null)
            return;

        if (!isCameraZInitialized)
        {
            InitializeCameraZ();
        }

        Vector2 rawTarget2D = new Vector2(
            target.position.x + offset.x,
            target.position.y + offset.y
        );

        // 🔒 Clamp khung nhìn Camera không để lộ khoảng đen ngoài map
        if (MapBoundary.Instance != null)
        {
            rawTarget2D = MapBoundary.Instance.ClampCameraPosition(rawTarget2D, GetCameraComponent());
        }

        Vector3 desiredPosition = new Vector3(
            rawTarget2D.x,
            rawTarget2D.y,
            cameraZ
        );

        // followSpeed <= 0 = bám ngay lập tức
        // Nếu gần như trùng target -> snap thẳng để loại bỏ rung vi mô
        if (followSpeed <= 0f || (transform.position - desiredPosition).sqrMagnitude <= snapThreshold * snapThreshold)
        {
            transform.position = desiredPosition;
            return;
        }

        // Exponential smoothing: Frame-rate independent và ổn định hơn Lerp thông thường
        float t = 1f - Mathf.Exp(-followSpeed * deltaTime);

        Vector3 newPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            t
        );

        // Đảm bảo sau khi Lerp vẫn nằm trọn trong Map
        if (MapBoundary.Instance != null)
        {
            Vector2 clampedNew2D = MapBoundary.Instance.ClampCameraPosition(new Vector2(newPosition.x, newPosition.y), GetCameraComponent());
            newPosition.x = clampedNew2D.x;
            newPosition.y = clampedNew2D.y;
        }

        // Luôn cố định Z
        newPosition.z = cameraZ;
        transform.position = newPosition;
    }

    private void LateUpdate()
    {
        UpdateFollow(Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        if (!isCameraZInitialized)
        {
            InitializeCameraZ();
        }

        Vector2 target2D = new Vector2(
            target.position.x + offset.x,
            target.position.y + offset.y
        );

        if (MapBoundary.Instance != null)
        {
            target2D = MapBoundary.Instance.ClampCameraPosition(target2D, GetCameraComponent());
        }

        transform.position = new Vector3(
            target2D.x,
            target2D.y,
            cameraZ
        );
    }
}