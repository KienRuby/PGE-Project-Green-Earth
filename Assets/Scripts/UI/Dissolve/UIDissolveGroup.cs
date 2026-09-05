using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý việc áp dụng Material Dissolve lên toàn bộ UI Graphics (Image, RawImage, TMP_Text) trong Panel.
/// - Đồng bộ một Shader/Material instance duy nhất cho toàn bộ panel (Zero GC alloc trong Update).
/// - Lưu trữ và khôi phục Material gốc khi hoàn tất.
/// - Đồng bộ Screen-Space hoặc Panel Bounds để toàn bộ Popup tan chảy như một thể thống nhất.
/// </summary>
[DisallowMultipleComponent]
public class UIDissolveGroup : MonoBehaviour
{
    [Header("Material Templates (Optional)")]
    [Tooltip("Material mẫu dùng shader Custom/UI/UIDissolve. Nếu để trống, script sẽ tự động tạo từ Shader.Find.")]
    [SerializeField] private Material baseGraphicDissolveMaterial;

    [Tooltip("Material mẫu dùng shader Custom/UI/UIDissolve_TMP cho TextMeshPro. Nếu để trống, script sẽ tự động tạo.")]
    [SerializeField] private Material baseTMPDissolveMaterial;

    [Header("Dissolve Noise Texture")]
    [Tooltip("Texture Noise. Nếu để trống, script sẽ tự động load từ Assets/Textures/Noise/dissolve_noise.png.")]
    [SerializeField] private Texture2D noiseTexture;

    // Cache Shader Property IDs
    private static readonly int PropDissolveAmount = Shader.PropertyToID("_DissolveAmount");
    private static readonly int PropDisintegrationWidth = Shader.PropertyToID("_DisintegrationWidth");
    private static readonly int PropGrainSize = Shader.PropertyToID("_GrainSize");
    private static readonly int PropDriftAmount = Shader.PropertyToID("_DriftAmount");
    private static readonly int PropSparkleIntensity = Shader.PropertyToID("_SparkleIntensity");
    private static readonly int PropUseUIColor = Shader.PropertyToID("_UseUIColor");
    private static readonly int PropNoiseTex = Shader.PropertyToID("_NoiseTex");
    private static readonly int PropNoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int PropNoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int PropNoiseOffset = Shader.PropertyToID("_NoiseOffset");
    private static readonly int PropUseScreenSpace = Shader.PropertyToID("_UseScreenSpace");
    private static readonly int PropPanelRect = Shader.PropertyToID("_PanelRect");
    private static readonly int PropDissolveDirection = Shader.PropertyToID("_DissolveDirection");
    private static readonly int PropDirectionInfluence = Shader.PropertyToID("_DirectionInfluence");
    private static readonly int PropDissolveSoftness = Shader.PropertyToID("_DissolveSoftness");
    private static readonly int PropEdgeWidth = Shader.PropertyToID("_EdgeWidth");
    private static readonly int PropEdgeColor = Shader.PropertyToID("_EdgeColor");
    private static readonly int PropInnerEdgeColor = Shader.PropertyToID("_InnerEdgeColor");
    private static readonly int PropEdgeIntensity = Shader.PropertyToID("_EdgeIntensity");

    // Single shared runtime material instances per group
    private Material sharedGraphicMaterial;
    private Material sharedTMPMaterial;

    private RectTransform panelRectTransform;

    private struct GraphicRecord
    {
        public Graphic graphic;
        public Material originalMaterial;
    }

    private struct TMPRecord
    {
        public TMP_Text text;
        public Material originalFontSharedMaterial;
    }

    private readonly List<GraphicRecord> trackedGraphics = new List<GraphicRecord>(32);
    private readonly List<TMPRecord> trackedTMPTexts = new List<TMPRecord>(16);

    private bool isMaterialApplied = false;
    private bool isInitialized = false;
    private static Texture2D generatedNoiseTexture;

    public Material SharedGraphicMaterial => sharedGraphicMaterial;
    public Material SharedTMPMaterial => sharedTMPMaterial;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();

        if (sharedGraphicMaterial != null)
        {
            Destroy(sharedGraphicMaterial);
            sharedGraphicMaterial = null;
        }

        if (sharedTMPMaterial != null)
        {
            Destroy(sharedTMPMaterial);
            sharedTMPMaterial = null;
        }
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized && sharedGraphicMaterial != null && sharedTMPMaterial != null) return;

        panelRectTransform = GetComponent<RectTransform>();

        if (noiseTexture == null)
        {
            noiseTexture = Resources.Load<Texture2D>("dissolve_noise");
#if UNITY_EDITOR
            if (noiseTexture == null)
            {
                noiseTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Noise/dissolve_noise.png");
            }
#endif
            if (noiseTexture == null)
            {
                noiseTexture = GetOrCreateGeneratedNoise();
            }
        }

        // Tạo 1 instance duy nhất cho Graphic (Image, RawImage)
        if (sharedGraphicMaterial == null)
        {
            Shader graphicShader = Shader.Find("Custom/UI/UIDissolve");
            if (baseGraphicDissolveMaterial != null)
            {
                sharedGraphicMaterial = new Material(baseGraphicDissolveMaterial);
            }
            else if (graphicShader != null)
            {
                sharedGraphicMaterial = new Material(graphicShader);
            }
            else
            {
                Debug.LogWarning("[UIDissolveGroup] Không tìm thấy shader Custom/UI/UIDissolve!");
            }

            if (sharedGraphicMaterial != null)
            {
                sharedGraphicMaterial.name = $"{gameObject.name}_UIDissolve_Instance";
                if (noiseTexture != null) sharedGraphicMaterial.SetTexture(PropNoiseTex, noiseTexture);
            }
        }

        // Tạo 1 instance duy nhất cho TMP_Text
        if (sharedTMPMaterial == null)
        {
            Shader tmpShader = Shader.Find("Custom/UI/UIDissolve_TMP");
            if (baseTMPDissolveMaterial != null)
            {
                sharedTMPMaterial = new Material(baseTMPDissolveMaterial);
            }
            else if (tmpShader != null)
            {
                sharedTMPMaterial = new Material(tmpShader);
            }

            if (sharedTMPMaterial != null)
            {
                sharedTMPMaterial.name = $"{gameObject.name}_UIDissolve_TMP_Instance";
                if (noiseTexture != null) sharedTMPMaterial.SetTexture(PropNoiseTex, noiseTexture);
            }
        }

        // Chỉ cache khi cả hai shader đã sẵn sàng; nếu Unity vừa reimport shader,
        // lần gọi sau có thể tự phục hồi thay vì kẹt vĩnh viễn ở material null.
        isInitialized = sharedGraphicMaterial != null && sharedTMPMaterial != null;
    }

    private static Texture2D GetOrCreateGeneratedNoise()
    {
        if (generatedNoiseTexture != null) return generatedNoiseTexture;

        const int size = 128;
        generatedNoiseTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "UIDissolve_RuntimeNoise",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        var random = new System.Random(0x504745);
        for (int i = 0; i < pixels.Length; i++)
        {
            byte value = (byte)random.Next(0, 256);
            pixels[i] = new Color32(value, value, value, value);
        }

        generatedNoiseTexture.SetPixels32(pixels);
        generatedNoiseTexture.Apply(false, true);
        return generatedNoiseTexture;
    }

    /// <summary>
    /// Quét toàn bộ Graphic và TMP_Text trong Panel và áp dụng Material Dissolve dùng chung.
    /// </summary>
    public void CollectAndApplyMaterials()
    {
        InitializeIfNeeded();

        if (isMaterialApplied) return;

        trackedGraphics.Clear();
        trackedTMPTexts.Clear();

        // 1. Quét TMP_Text trước (vì TMP_Text kế thừa MaskableGraphic)
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        HashSet<Graphic> tmpsAsGraphics = new HashSet<Graphic>();

        if (tmps != null && tmps.Length > 0 && sharedTMPMaterial != null)
        {
            for (int i = 0; i < tmps.Length; i++)
            {
                TMP_Text t = tmps[i];
                if (t == null) continue;

                tmpsAsGraphics.Add(t);
                trackedTMPTexts.Add(new TMPRecord
                {
                    text = t,
                    originalFontSharedMaterial = t.fontSharedMaterial
                });

                // Gán font texture atlas của font asset hiện tại vào shared TMP dissolve material
                if (t.font != null && t.font.material != null && t.font.material.mainTexture != null)
                {
                    sharedTMPMaterial.mainTexture = t.font.material.mainTexture;
                }

                t.fontSharedMaterial = sharedTMPMaterial;
            }
        }

        // 2. Quét toàn bộ Graphic còn lại (Image, RawImage, Button graphic)
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        if (graphics != null && graphics.Length > 0 && sharedGraphicMaterial != null)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic g = graphics[i];
                if (g == null) continue;
                if (tmpsAsGraphics.Contains(g)) continue; // Đã xử lý bằng TMP shader

                trackedGraphics.Add(new GraphicRecord
                {
                    graphic = g,
                    originalMaterial = g.material
                });

                g.material = sharedGraphicMaterial;
            }
        }

        UpdatePanelBounds();
        isMaterialApplied = true;
    }

    /// <summary>
    /// Khôi phục lại toàn bộ Material ban đầu của tất cả phần tử con.
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        if (!isMaterialApplied) return;

        for (int i = 0; i < trackedGraphics.Count; i++)
        {
            var rec = trackedGraphics[i];
            if (rec.graphic != null)
            {
                rec.graphic.material = rec.originalMaterial;
            }
        }
        trackedGraphics.Clear();

        for (int i = 0; i < trackedTMPTexts.Count; i++)
        {
            var rec = trackedTMPTexts[i];
            if (rec.text != null)
            {
                rec.text.fontSharedMaterial = rec.originalFontSharedMaterial;
            }
        }
        trackedTMPTexts.Clear();

        isMaterialApplied = false;
    }

    /// <summary>
    /// Cập nhật tiến độ phân rã (0 = bình thường, 1 = tan biến hoàn toàn).
    /// </summary>
    public void SetDissolveProgress(float progress)
    {
        if (sharedGraphicMaterial != null)
        {
            sharedGraphicMaterial.SetFloat(PropDissolveAmount, progress);
        }

        if (sharedTMPMaterial != null)
        {
            sharedTMPMaterial.SetFloat(PropDissolveAmount, progress);
        }
    }

    /// <summary>
    /// Cấu hình toàn bộ tham số hiệu ứng lên Material một lần trước khi bắt đầu hoạt họa.
    /// </summary>
    public void ConfigureMaterialSettings(
        int directionMode,
        float directionInfluence,
        float edgeWidth,
        Color edgeColor,
        Color innerEdgeColor,
        float edgeIntensity,
        float noiseScale,
        float noiseSpeed,
        Vector2 noiseOffset,
        bool useScreenSpace,
        float softness,
        float disintegrationWidth = 0.22f,
        float grainSize = 1.8f,
        float driftAmount = 0.85f,
        float sparkleIntensity = 2.2f,
        bool useUIColor = true)
    {
        InitializeIfNeeded();
        UpdatePanelBounds();

        ApplyToMaterial(sharedGraphicMaterial);
        ApplyToMaterial(sharedTMPMaterial);

        void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetFloat(PropDissolveDirection, directionMode);
            mat.SetFloat(PropDirectionInfluence, directionInfluence);
            mat.SetFloat(PropEdgeWidth, edgeWidth);
            mat.SetColor(PropEdgeColor, edgeColor);
            mat.SetColor(PropInnerEdgeColor, innerEdgeColor);
            mat.SetFloat(PropEdgeIntensity, edgeIntensity);
            mat.SetFloat(PropNoiseScale, noiseScale);
            mat.SetFloat(PropNoiseSpeed, noiseSpeed);
            mat.SetVector(PropNoiseOffset, noiseOffset);
            mat.SetFloat(PropUseScreenSpace, useScreenSpace ? 1f : 0f);
            mat.SetFloat(PropDissolveSoftness, softness);
            mat.SetFloat(PropDisintegrationWidth, disintegrationWidth);
            mat.SetFloat(PropGrainSize, grainSize);
            mat.SetFloat(PropDriftAmount, driftAmount);
            mat.SetFloat(PropSparkleIntensity, sparkleIntensity);
            mat.SetFloat(PropUseUIColor, useUIColor ? 1f : 0f);
        }
    }

    private void UpdatePanelBounds()
    {
        if (panelRectTransform == null) panelRectTransform = GetComponent<RectTransform>();
        if (panelRectTransform == null) return;

        Vector3[] corners = new Vector3[4];
        panelRectTransform.GetWorldCorners(corners);
        Vector4 bounds = new Vector4(corners[0].x, corners[0].y, corners[2].x, corners[2].y);

        if (sharedGraphicMaterial != null) sharedGraphicMaterial.SetVector(PropPanelRect, bounds);
        if (sharedTMPMaterial != null) sharedTMPMaterial.SetVector(PropPanelRect, bounds);
    }
}
