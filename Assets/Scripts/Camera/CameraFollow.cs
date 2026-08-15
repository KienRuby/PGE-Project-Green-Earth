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

    private void Awake()
    {
        cameraZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            cameraZ
        );

        // Nếu gần như trùng target -> snap thẳng
        // tránh camera dao động cực nhỏ.
        if ((transform.position - desiredPosition).sqrMagnitude
            <= snapThreshold * snapThreshold)
        {
            transform.position = desiredPosition;
            return;
        }

        // Exponential smoothing.
        // Frame-rate independent và ổn định hơn Lerp thông thường.
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        Vector3 newPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            t
        );

        // Luôn cố định Z
        newPosition.z = cameraZ;

        transform.position = newPosition;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            cameraZ
        );
    }
}