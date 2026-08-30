using System.Collections;
using UnityEngine;

/// <summary>
/// Hiệu ứng chớp đỏ/trắng khi nhân vật hoặc quái vật nhận sát thương (Damage Flash Reaction).
/// Hỗ trợ MaterialPropertyBlock với Shader Custom/2D/SpriteHitFlash (_FlashAmount, _FlashColor)
/// hoặc tự động fallback đổi màu SpriteRenderer trong thời gian ngắn (0.15s).
/// </summary>
[DisallowMultipleComponent]
public class SpriteHitFlash : MonoBehaviour
{
    [Header("Flash Configuration")]
    [Tooltip("Màu sắc của hiệu ứng chớp khi nhận sát thương.")]
    [SerializeField] private Color flashColor = Color.red;

    [Tooltip("Thời gian tồn tại của mỗi lần chớp (giây).")]
    [SerializeField] private float flashDuration = 0.15f;

    [Tooltip("Material sử dụng Shader Custom/2D/SpriteHitFlash (tùy chọn).")]
    [SerializeField] private Material hitFlashMaterial;

    private static readonly int FlashAmountPropId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorPropId = Shader.PropertyToID("_FlashColor");

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private MaterialPropertyBlock propBlock;
    private Coroutine flashRoutine;

    public Color FlashColor
    {
        get => flashColor;
        set => flashColor = value;
    }

    public float FlashDuration
    {
        get => flashDuration;
        set => flashDuration = Mathf.Max(0.01f, value);
    }

    public bool IsFlashing => flashRoutine != null;

    private void Awake()
    {
        CacheRenderers();
    }

    public void CacheRenderers()
    {
        if (propBlock == null) propBlock = new MaterialPropertyBlock();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalColors[i] = spriteRenderers[i].color;
                }
            }
        }
    }

    public void Flash()
    {
        Flash(flashColor, flashDuration);
    }

    public void Flash(Color color, float duration)
    {
        if (!gameObject.activeInHierarchy) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreOriginalColors();
        }

        flashRoutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetFloat(FlashAmountPropId, 1f);
                    propBlock.SetColor(FlashColorPropId, color);
                    spriteRenderers[i].SetPropertyBlock(propBlock);

                    Color orig = (originalColors != null && i < originalColors.Length) ? originalColors[i] : Color.white;
                    spriteRenderers[i].color = new Color(color.r, color.g, color.b, orig.a);
                }
            }
        }

        yield return new WaitForSeconds(duration);

        RestoreOriginalColors();
        flashRoutine = null;
    }

    public void RestoreOriginalColors()
    {
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (spriteRenderers != null && originalColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetFloat(FlashAmountPropId, 0f);
                    spriteRenderers[i].SetPropertyBlock(propBlock);

                    if (i < originalColors.Length)
                    {
                        spriteRenderers[i].color = originalColors[i];
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreOriginalColors();
    }
}
