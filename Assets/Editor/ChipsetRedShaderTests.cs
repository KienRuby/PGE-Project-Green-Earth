using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ChipsetRedShaderTests
{
    [Test]
    public void HolographicCard_AppliesFlagShaderOnlyToFrame_NotBackground()
    {
        GameObject cardObject = new GameObject(
            "ChipsetCard",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(ChipsetCardUI));
        GameObject backgroundObject = new GameObject(
            "BackgroundRed",
            typeof(RectTransform),
            typeof(Image));
        backgroundObject.transform.SetParent(cardObject.transform, false);

        try
        {
            Image frame = cardObject.GetComponent<Image>();
            Image background = backgroundObject.GetComponent<Image>();
            Material originalFrameMaterial = frame.material;
            Material originalBackgroundMaterial = background.material;
            ChipsetCardUI card = cardObject.GetComponent<ChipsetCardUI>();
            card.InitializeReferences(frame, null, null, null, cardObject.GetComponent<Button>(), null, null, null, null);

            ChipItemData data = new ChipItemData
            {
                id = 999,
                chipName = "Shader Test",
                tier = ChipTier.Holographic,
                level = 1
            };
            card.Setup(data, null, null);

            Assert.That(frame.material, Is.Not.SameAs(originalFrameMaterial));
            Assert.That(frame.material.shader.name, Is.EqualTo("PGE/UI/Chipset Red Shimmer"));
            Assert.That(background.gameObject.activeSelf, Is.True);
            Assert.That(background.material, Is.SameAs(originalBackgroundMaterial),
                "BackGround1 phải giữ material UI gốc và không nhận shader.");
        }
        finally
        {
            Object.DestroyImmediate(cardObject);
        }
    }
}
