using NUnit.Framework;
using System.Reflection;
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

    [Test]
    public void LevelUpChoiceCard_HolographicFrame_UsesAnimatedShaderWhileTimeScaleIsZero()
    {
        GameObject cardObject = new GameObject("LevelUpChoiceCard", typeof(RectTransform), typeof(Button), typeof(CanvasGroup));
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        GameObject frameObject = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(cardObject.transform, false);
        frameObject.transform.SetParent(cardObject.transform, false);
        Texture2D texture = new Texture2D(2, 2);
        Sprite frameSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        float previousTimeScale = Time.timeScale;

        try
        {
            ChipsetChoiceCardUI card = cardObject.AddComponent<ChipsetChoiceCardUI>();
            Image background = backgroundObject.GetComponent<Image>();
            Image frame = frameObject.GetComponent<Image>();
            Material originalBackgroundMaterial = background.material;
            card.InitializeReferences(
                null,
                background,
                frame,
                null,
                null,
                null,
                cardObject.GetComponent<Button>(),
                cardObject.GetComponent<CanvasGroup>());

            card.Setup(
                new ChipItemData { id = 1, chipName = "Red Choice", tier = ChipTier.Holographic },
                null,
                frameSprite,
                null,
                0,
                string.Empty,
                null);

            Time.timeScale = 0f;
            MethodInfo update = typeof(ChipsetChoiceCardUI).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(card, null);

            Assert.That(frame.material.shader.name, Is.EqualTo("PGE/UI/Chipset Red Shimmer"));
            Assert.That(Shader.GetGlobalFloat("_ChipsetUnscaledTime"), Is.GreaterThanOrEqualTo(0f));
            Assert.That(background.material, Is.SameAs(originalBackgroundMaterial));
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            Object.DestroyImmediate(frameSprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(cardObject);
        }
    }
}
