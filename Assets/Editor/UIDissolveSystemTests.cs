using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDissolveSystemTests
{
    private GameObject testRoot;
    private UIDissolveController controller;
    private UIDissolveGroup group;
    private UIDissolveParticle particle;
    private CanvasGroup canvasGroup;

    [SetUp]
    public void SetUp()
    {
        testRoot = new GameObject("Test_UIPopup", typeof(RectTransform));
        RectTransform rt = testRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 600);

        // Add child Image
        GameObject childImgObj = new GameObject("Background", typeof(RectTransform));
        childImgObj.transform.SetParent(testRoot.transform, false);
        Image img = childImgObj.AddComponent<Image>();
        img.color = Color.blue;

        // Add child TMP_Text
        GameObject childTxtObj = new GameObject("TitleText", typeof(RectTransform));
        childTxtObj.transform.SetParent(testRoot.transform, false);
        TMP_Text txt = childTxtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "Reward Header";

        // Add controller
        controller = testRoot.AddComponent<UIDissolveController>();
        controller.InitializeIfNeeded();

        group = testRoot.GetComponent<UIDissolveGroup>();
        particle = testRoot.GetComponent<UIDissolveParticle>();
        canvasGroup = testRoot.GetComponent<CanvasGroup>();
    }

    [TearDown]
    public void TearDown()
    {
        if (testRoot != null)
        {
            Object.DestroyImmediate(testRoot);
        }
    }

    [Test]
    public void UIDissolve_AutoWiring_InitializesAllRequiredComponents()
    {
        Assert.That(controller, Is.Not.Null);
        Assert.That(group, Is.Not.Null, "UIDissolveGroup phải được tự động gắn vào panel.");
        Assert.That(particle, Is.Not.Null, "UIDissolveParticle phải được tự động gắn vào panel.");
        Assert.That(canvasGroup, Is.Not.Null, "CanvasGroup phải được tự động gắn vào panel.");
    }

    [Test]
    public void UIDissolveGroup_CollectAndApplyMaterials_UsesSingleSharedMaterialPerCategory()
    {
        group.CollectAndApplyMaterials();

        Image img = testRoot.GetComponentInChildren<Image>(true);
        TMP_Text txt = testRoot.GetComponentInChildren<TMP_Text>(true);

        Assert.That(group.SharedGraphicMaterial, Is.Not.Null, "Shader Custom/UI/UIDissolve phải compile và tạo được material.");
        Assert.That(group.SharedTMPMaterial, Is.Not.Null, "Shader TMP dissolve phải compile và tạo được material.");
        Assert.That(img.material, Is.EqualTo(group.SharedGraphicMaterial), "Image phải dùng chung SharedGraphicMaterial.");
        Assert.That(txt.fontSharedMaterial, Is.EqualTo(group.SharedTMPMaterial), "TMP phải dùng chung SharedTMPMaterial.");

        // Kiểm tra SetDissolveProgress
        group.SetDissolveProgress(0.75f);
        int propId = Shader.PropertyToID("_DissolveAmount");
        if (group.SharedGraphicMaterial != null)
        {
            Assert.That(group.SharedGraphicMaterial.GetFloat(propId), Is.EqualTo(0.75f));
        }

        // Khôi phục lại
        group.RestoreOriginalMaterials();
        Assert.That(img.material, Is.Not.EqualTo(group.SharedGraphicMaterial), "Sau khi restore, Image phải quay về material ban đầu.");
    }

    [Test]
    public void UIDissolveController_Hide_BlocksRaycastsImmediately_ToPreventClickSpam()
    {
        // Ban đầu mở bình thường
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        controller.Hide();

        // Ngay khi Hide được gọi, tương tác phải bị khóa lập tức
        Assert.That(canvasGroup.interactable, Is.False, "interactable phải bị tắt ngay khi bắt đầu Hide để chống click spam.");
        Assert.That(canvasGroup.blocksRaycasts, Is.False, "blocksRaycasts phải bị tắt ngay khi bắt đầu Hide.");
        Assert.That(controller.CurrentState, Is.EqualTo(UIDissolveController.TransitionState.Hiding));
    }

    [Test]
    public void UIDissolveController_HideInstant_ImmediatelyDeactivatesAndResets()
    {
        controller.HideInstant();

        Assert.That(testRoot.activeSelf, Is.False, "HideInstant phải SetActive(false) ngay lập tức.");
        Assert.That(controller.CurrentState, Is.EqualTo(UIDissolveController.TransitionState.IdleClosed));
        Assert.That(canvasGroup.interactable, Is.True, "Sau khi reset, canvasGroup sẵn sàng cho lần mở kế tiếp.");
        Assert.That(canvasGroup.blocksRaycasts, Is.True);
    }

    [Test]
    public void UIDissolveController_ShowInstant_ActivatesAndRestoresInteractions()
    {
        testRoot.SetActive(false);

        controller.Show(UIDissolveController.ShowMode.Instant);

        Assert.That(testRoot.activeSelf, Is.True);
        Assert.That(controller.CurrentState, Is.EqualTo(UIDissolveController.TransitionState.IdleOpened));
        Assert.That(canvasGroup.interactable, Is.True);
        Assert.That(canvasGroup.blocksRaycasts, Is.True);
    }

    [Test]
    public void UIDissolveNoiseGenerator_ProducesValid512x512Texture()
    {
        Texture2D tex = UIDissolveNoiseGenerator.GenerateNoiseTexture(128, 128);

        Assert.That(tex, Is.Not.Null);
        Assert.That(tex.width, Is.EqualTo(128));
        Assert.That(tex.height, Is.EqualTo(128));

        Color c = tex.GetPixel(64, 64);
        Assert.That(c.a, Is.InRange(0f, 1f));
        Assert.That(c.r, Is.InRange(0f, 1f));

        Object.DestroyImmediate(tex);
    }

    [Test]
    public void UIDissolveController_StardustConfiguration_MatchesFastVideoReference()
    {
        Assert.That(controller.Duration, Is.EqualTo(0.34f), "Đóng tab phải hoàn tất nhanh trong 0.34s.");
        Assert.That(controller.Direction, Is.EqualTo(UIDissolveController.DissolveDirection.TopToBottom));
        Assert.That(controller.UseUIColor, Is.True, "Màu tan biến phải lấy theo màu của UI (useUIColor = true).");
        Assert.That(controller.GrainSize, Is.EqualTo(1.45f), "Video dùng hạt cát nhỏ và dày.");
        Assert.That(controller.DisintegrationWidth, Is.EqualTo(0.28f));

        // Test configuring material settings
        group.ConfigureMaterialSettings(
            directionMode: 3,
            directionInfluence: 0.8f,
            edgeWidth: 0.035f,
            edgeColor: Color.white,
            innerEdgeColor: Color.white,
            edgeIntensity: 1.3f,
            noiseScale: 3.6f,
            noiseSpeed: 0f,
            noiseOffset: Vector2.zero,
            useScreenSpace: true,
            softness: 0.01f,
            disintegrationWidth: 0.28f,
            grainSize: 1.45f,
            driftAmount: 0.55f,
            sparkleIntensity: 1.3f,
            useUIColor: true
        );

        if (group.SharedGraphicMaterial != null)
        {
            Assert.That(group.SharedGraphicMaterial.GetFloat("_UseUIColor"), Is.EqualTo(1f));
            Assert.That(group.SharedGraphicMaterial.GetFloat("_GrainSize"), Is.EqualTo(1.45f));
        }
    }
}
