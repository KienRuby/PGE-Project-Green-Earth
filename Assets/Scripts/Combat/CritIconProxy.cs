using UnityEngine;

/// <summary>
/// Cho phép điều chỉnh kích thước và vị trí của Icon Chí Mạng trực tiếp ngay khi chọn GameObject CritIcon trong Inspector.
/// Tự động đồng bộ hai chiều với component DamageNumber trên đối tượng cha.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class CritIconProxy : MonoBehaviour
{
    private DamageNumber parentDamageNumber;

    public DamageNumber ParentDamageNumber
    {
        get
        {
            if (parentDamageNumber == null)
            {
                parentDamageNumber = GetComponentInParent<DamageNumber>();
            }
            return parentDamageNumber;
        }
    }

    [Header("Tùy Chỉnh Icon Chí Mạng (Inspector Controls)")]
    [Tooltip("Kích thước icon (chỉnh ~0.38 - 0.45 để nhỏ bằng với chiều cao con số sát thương).")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float iconSize = 0.42f;

    [Tooltip("Khoảng cách tới con số sát thương đầu tiên (càng nhỏ càng dịch sát vào số).")]
    [Range(-0.1f, 0.4f)]
    [SerializeField] private float iconSpacing = 0.03f;

    [Tooltip("Độ lệch vị trí (X, Y). Dùng để nâng cao/hạ thấp hoặc dịch sang trái/phải.")]
    [SerializeField] private Vector2 iconOffset = new Vector2(0f, 0.01f);

    public float IconSize
    {
        get => iconSize;
        set
        {
            iconSize = value;
            ApplyToParent();
        }
    }

    public float IconSpacing
    {
        get => iconSpacing;
        set
        {
            iconSpacing = value;
            ApplyToParent();
        }
    }

    public Vector2 IconOffset
    {
        get => iconOffset;
        set
        {
            iconOffset = value;
            ApplyToParent();
        }
    }

    private Vector3 lastLocalScale = Vector3.zero;
    private bool isInternalUpdating;

    private void OnEnable()
    {
        SyncFromParent();
        lastLocalScale = transform.localScale;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CheckTransformChanges();
        }
#endif
    }

    /// <summary>
    /// Nhận diện khi người dùng kéo tay bằng công cụ Scale Tool (R) hoặc chỉnh Transform Scale trong Inspector.
    /// Tự động quy đổi Transform.localScale thành IconSize và lưu đồng bộ sang đối tượng cha.
    /// </summary>
    public void CheckTransformChanges()
    {
        if (isInternalUpdating) return;

        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteW = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                float spriteH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                float maxDim = Mathf.Max(spriteW, spriteH);

                if (maxDim > 0f && lastLocalScale != Vector3.zero && transform.localScale != lastLocalScale)
                {
                    float newCalculatedSize = transform.localScale.x * maxDim;
                    if (Mathf.Abs(newCalculatedSize - iconSize) > 0.003f)
                    {
                        iconSize = Mathf.Max(0.05f, newCalculatedSize);
                        lastLocalScale = transform.localScale;
                        ApplyToParent();
                        return;
                    }
                }
            }
            lastLocalScale = transform.localScale;
        }
    }

    public void SyncFromParent()
    {
        if (ParentDamageNumber != null)
        {
            iconSize = ParentDamageNumber.CritIconSize;
            iconSpacing = ParentDamageNumber.CritIconSpacing;
            iconOffset = ParentDamageNumber.CritIconOffset;
            lastLocalScale = transform.localScale;
        }
    }

    private void ApplyToParent()
    {
        if (ParentDamageNumber != null)
        {
            isInternalUpdating = true;
            try
            {
                ParentDamageNumber.CritIconSize = iconSize;
                ParentDamageNumber.CritIconSpacing = iconSpacing;
                ParentDamageNumber.CritIconOffset = iconOffset;
                ParentDamageNumber.UpdateCritLayout();
                lastLocalScale = transform.localScale;
            }
            finally
            {
                isInternalUpdating = false;
            }
        }
    }

    private void OnValidate()
    {
        ApplyToParent();
    }

    [ContextMenu("Khôi Phục Kích Thước Chuẩn (Bằng Chiều Cao Chữ Số)")]
    public void ResetToMatchedHeight()
    {
        iconSize = 0.42f;
        iconSpacing = 0.03f;
        iconOffset = new Vector2(0f, 0.01f);
        ApplyToParent();
    }
}
