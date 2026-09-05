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

    [Test]
    public void ChipsetDetailModal_PerkRowRedLock_AppliesHolographicShimmerShader()
    {
        GameObject controllerGo = new GameObject("ChipsetControllerTest");
        GameObject modalGo = new GameObject("ChipsetDetailModal");
        GameObject[] rowObjects = new GameObject[4];
        Image[] rowIcons = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            rowObjects[i] = new GameObject($"PerkRow_{i}");
            rowObjects[i].transform.SetParent(modalGo.transform, false);
            GameObject iconGo = new GameObject("LockIcon");
            iconGo.transform.SetParent(rowObjects[i].transform, false);
            rowIcons[i] = iconGo.AddComponent<Image>();
        }

        try
        {
            ChipsetController controller = controllerGo.AddComponent<ChipsetController>();
            var type = typeof(ChipsetController);
            type.GetField("detailModal", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, modalGo);
            type.GetField("perkRowIcons", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, rowIcons);

            Texture2D dummyTex = new Texture2D(2, 2);
            Sprite checkSprite = Sprite.Create(dummyTex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            type.GetField("unlockedCheckSprite", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, checkSprite);

            controller.EnsureLockTierSprites();

            ChipItemData epicChip = new ChipItemData
            {
                id = 101,
                chipName = "Epic Test Chip",
                tier = ChipTier.Epic,
                level = 15,
                magicBonus = "Bonus 1",
                rareBonus = "Bonus 2",
                uniqueBonus = "Bonus 3",
                epicBonus = "Bonus 4"
            };

            controller.OpenDetailModal(epicChip);

            // Row 0..2 should be unlocked with Open Locks, Row 3 should be locked with Red Closed Lock & Holographic shader
            Assert.That(rowIcons[0].material, Is.Null);
            Assert.That(rowIcons[0].sprite.name, Does.Contain("Open"));
            Assert.That(rowIcons[1].material, Is.Null);
            Assert.That(rowIcons[1].sprite.name, Does.Contain("Open"));
            Assert.That(rowIcons[2].material, Is.Null);
            Assert.That(rowIcons[2].sprite.name, Does.Contain("Open"));
            Assert.That(rowIcons[3].material, Is.Not.Null, "Row 3 Red Lock must have a material assigned.");
            Assert.That(rowIcons[3].material.shader.name, Is.EqualTo("PGE/UI/Chipset Red Shimmer"), "Row 3 Red Lock must use the PGE/UI/Chipset Red Shimmer shader.");
            Assert.That(rowIcons[3].sprite.name, Is.EqualTo("Lock_Red"));

            // Advance chip to Holographic: Row 3 should now unlock with Lock_Red_Open and keep the Holographic shader
            ChipItemData holoChip = new ChipItemData
            {
                id = 101,
                chipName = "Holo Test Chip",
                tier = ChipTier.Holographic,
                level = 20,
                magicBonus = "Bonus 1",
                rareBonus = "Bonus 2",
                uniqueBonus = "Bonus 3",
                epicBonus = "Bonus 4"
            };

            controller.OpenDetailModal(holoChip);
            Assert.That(rowIcons[3].sprite.name, Does.Contain("Lock_Red_Open"), "Row 3 unlocked must use Lock_Red_Open sprite.");
            Assert.That(rowIcons[3].material, Is.Not.Null, "Row 3 Red Open Lock still features the holographic shimmer effect.");
            Assert.That(rowIcons[3].material.shader.name, Is.EqualTo("PGE/UI/Chipset Red Shimmer"));

            Object.DestroyImmediate(dummyTex);
            Object.DestroyImmediate(checkSprite);
        }
        finally
        {
            Object.DestroyImmediate(modalGo);
            Object.DestroyImmediate(controllerGo);
        }
    }

    [Test]
    public void ChipsetDetailModal_NotEnoughDataChips_ShowsNotice2_AndDimmedButton()
    {
        GameObject controllerGo = new GameObject("ChipsetController", typeof(ChipsetController));
        GameObject modalGo = new GameObject("DetailModal");
        GameObject boxGo = new GameObject("ModalBox");
        boxGo.transform.SetParent(modalGo.transform, false);

        GameObject enhBtnGo = new GameObject("EnhanceBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        enhBtnGo.transform.SetParent(boxGo.transform, false);

        GameObject advBtnGo = new GameObject("AdvanceTierBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        advBtnGo.transform.SetParent(boxGo.transform, false);

        try
        {
            ChipsetController controller = controllerGo.GetComponent<ChipsetController>();
            typeof(ChipsetController).GetField("detailModal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, modalGo);
            typeof(ChipsetController).GetField("detailEnhanceBtn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, enhBtnGo.GetComponent<Button>());
            typeof(ChipsetController).GetField("enhanceBtnCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, enhBtnGo.GetComponent<CanvasGroup>());
            typeof(ChipsetController).GetField("detailAdvanceTierBtn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, advBtnGo.GetComponent<Button>());
            typeof(ChipsetController).GetField("advanceTierBtnCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, advBtnGo.GetComponent<CanvasGroup>());

            ChipItemData chip = new ChipItemData
            {
                id = 201,
                chipName = "Rocket Punch",
                tier = ChipTier.Rare,
                level = 8,
                enhanceCost = 3500,
                count = 4
            };
            chip.ConfigureTierUnlockRules(10, 5, 7, 15, 20);

            // Set player data chips lower than enhance cost
            ChipManager.DataChips = 2496;

            controller.OpenDetailModal(chip);

            // Button should be dimmed
            CanvasGroup enhCg = controller.EnhanceBtnCanvasGroup;
            Assert.That(enhCg.alpha, Is.LessThan(1.0f), "Nút Enhance phải tối đi khi thiếu Data Chips.");

            // Click Enhance -> should trigger Notice 2 (Not enough Data Chips)
            MethodInfo enhanceMethod = typeof(ChipsetController).GetMethod("EnhanceSelectedChip", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(enhanceMethod, Is.Not.Null);
            enhanceMethod.Invoke(controller, null);

            GameObject notice2 = controller.NotEnoughChipsNotice;
            Assert.That(notice2, Is.Not.Null);
            Assert.That(notice2.activeSelf, Is.True, "Bảng 2 (Not enough Data Chips) phải hiện lên khi thiếu Data Chips.");
            Assert.That(notice2.GetComponent<UIDissolveController>(), Is.Not.Null, "Bảng 2 phải có UIDissolveController để tan biến sau 2s.");

            GameObject notice1 = controller.NotEnoughFragmentsNotice;
            if (notice1 != null)
            {
                Assert.That(notice1.activeSelf, Is.False, "Bảng 1 không được bật khi bấm Enhance thiếu Data Chips.");
            }
        }
        finally
        {
            Object.DestroyImmediate(modalGo);
            Object.DestroyImmediate(controllerGo);
        }
    }

    [Test]
    public void ChipsetDetailModal_MaxEnhanceForFrame_ShowsNothing_AndButtonIsDimmed()
    {
        GameObject controllerGo = new GameObject("ChipsetController", typeof(ChipsetController));
        GameObject modalGo = new GameObject("DetailModal");
        GameObject boxGo = new GameObject("ModalBox");
        boxGo.transform.SetParent(modalGo.transform, false);

        GameObject enhBtnGo = new GameObject("EnhanceBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        enhBtnGo.transform.SetParent(boxGo.transform, false);

        try
        {
            ChipsetController controller = controllerGo.GetComponent<ChipsetController>();
            typeof(ChipsetController).GetField("detailModal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, modalGo);
            typeof(ChipsetController).GetField("detailEnhanceBtn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, enhBtnGo.GetComponent<Button>());
            typeof(ChipsetController).GetField("enhanceBtnCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, enhBtnGo.GetComponent<CanvasGroup>());

            ChipItemData chip = new ChipItemData
            {
                id = 202,
                chipName = "Maxed Frame Chip",
                tier = ChipTier.Rare,
                level = 9, // Rare max level is 9
                enhanceCost = 1000
            };
            chip.ConfigureTierUnlockRules(3, 5, 7, 15, 20);
            // Simulate 3 enhances already done (max of current frame)
            for (int i = 0; i < 3; i++) chip.tierEnhanceCount++;

            Assert.That(chip.IsMaxEnhanceForCurrentFrame, Is.True, "Chipset phải được nhận diện đã đạt max enhance của khung hiện tại.");

            // Even if player has infinite data chips
            ChipManager.DataChips = 999999;

            controller.OpenDetailModal(chip);

            // Enhance button must be dimmed
            CanvasGroup enhCg = controller.EnhanceBtnCanvasGroup;
            Assert.That(enhCg.alpha, Is.LessThan(1.0f), "Nút Enhance phải tối đi khi đã đạt max enhance của khung hiện tại.");

            // Click Enhance -> MUST SHOW NOTHING ("nếu enchan tối đa của khung hiện tại thì sẽ không hiện gì")
            MethodInfo enhanceMethod = typeof(ChipsetController).GetMethod("EnhanceSelectedChip", BindingFlags.Instance | BindingFlags.NonPublic);
            enhanceMethod.Invoke(controller, null);

            GameObject notice2 = controller.NotEnoughChipsNotice;
            if (notice2 != null)
            {
                Assert.That(notice2.activeSelf, Is.False, "Khi đã max enhance của khung hiện tại thì không được hiện bất kỳ bảng thông báo nào.");
            }
            GameObject notice1 = controller.NotEnoughFragmentsNotice;
            if (notice1 != null)
            {
                Assert.That(notice1.activeSelf, Is.False);
            }
        }
        finally
        {
            Object.DestroyImmediate(modalGo);
            Object.DestroyImmediate(controllerGo);
        }
    }

    [Test]
    public void ChipsetDetailModal_NotEnoughFragments_ShowsNotice1_AndDimmedButton()
    {
        GameObject controllerGo = new GameObject("ChipsetController", typeof(ChipsetController));
        GameObject modalGo = new GameObject("DetailModal");
        GameObject boxGo = new GameObject("ModalBox");
        boxGo.transform.SetParent(modalGo.transform, false);

        GameObject advBtnGo = new GameObject("AdvanceTierBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        advBtnGo.transform.SetParent(boxGo.transform, false);

        try
        {
            ChipsetController controller = controllerGo.GetComponent<ChipsetController>();
            typeof(ChipsetController).GetField("detailModal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, modalGo);
            typeof(ChipsetController).GetField("detailAdvanceTierBtn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, advBtnGo.GetComponent<Button>());
            typeof(ChipsetController).GetField("advanceTierBtnCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, advBtnGo.GetComponent<CanvasGroup>());

            ChipItemData chip = new ChipItemData
            {
                id = 203,
                chipName = "Rocket Punch",
                tier = ChipTier.Rare,
                level = 8,
                count = 4 // Only 4 fragments, needs 7
            };
            chip.ConfigureTierUnlockRules(10, 5, 7, 15, 20);

            controller.OpenDetailModal(chip);

            // Advance button should be dimmed
            CanvasGroup advCg = controller.AdvanceTierBtnCanvasGroup;
            Assert.That(advCg.alpha, Is.LessThan(1.0f), "Nút Advance Tier phải tối đi khi thiếu mảnh.");

            // Click Advance Tier -> should trigger Notice 1 (You need to collect more Chipsets)
            MethodInfo advMethod = typeof(ChipsetController).GetMethod("AdvanceTierSelectedChip", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(advMethod, Is.Not.Null);
            advMethod.Invoke(controller, null);

            GameObject notice1 = controller.NotEnoughFragmentsNotice;
            Assert.That(notice1, Is.Not.Null);
            Assert.That(notice1.activeSelf, Is.True, "Bảng 1 (You need to collect more Chipsets) phải hiện lên khi thiếu mảnh.");
            Assert.That(notice1.GetComponent<UIDissolveController>(), Is.Not.Null, "Bảng 1 phải có UIDissolveController để tan biến sau 2s.");
        }
        finally
        {
            Object.DestroyImmediate(modalGo);
            Object.DestroyImmediate(controllerGo);
        }
    }

    [Test]
    public void ChipsetDetailModal_EnoughResources_ButtonsAreBright()
    {
        GameObject controllerGo = new GameObject("ChipsetController", typeof(ChipsetController));
        GameObject modalGo = new GameObject("DetailModal");
        GameObject boxGo = new GameObject("ModalBox");
        boxGo.transform.SetParent(modalGo.transform, false);

        GameObject enhBtnGo = new GameObject("EnhanceBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        enhBtnGo.transform.SetParent(boxGo.transform, false);

        GameObject advBtnGo = new GameObject("AdvanceTierBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        advBtnGo.transform.SetParent(boxGo.transform, false);

        try
        {
            ChipsetController controller = controllerGo.GetComponent<ChipsetController>();
            typeof(ChipsetController).GetField("detailModal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, modalGo);
            typeof(ChipsetController).GetField("detailEnhanceBtn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, enhBtnGo.GetComponent<Button>());
            typeof(ChipsetController).GetField("enhanceBtnCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, enhBtnGo.GetComponent<CanvasGroup>());
            typeof(ChipsetController).GetField("detailAdvanceTierBtn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, advBtnGo.GetComponent<Button>());
            typeof(ChipsetController).GetField("advanceTierBtnCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, advBtnGo.GetComponent<CanvasGroup>());

            ChipItemData chip = new ChipItemData
            {
                id = 204,
                chipName = "Rich Chip",
                tier = ChipTier.Rare,
                level = 7,
                enhanceCost = 500,
                count = 10 // Needs 7
            };
            chip.ConfigureTierUnlockRules(10, 5, 7, 15, 20);

            ChipManager.DataChips = 10000;

            controller.OpenDetailModal(chip);

            // Both buttons should be bright
            Assert.That(controller.EnhanceBtnCanvasGroup.alpha, Is.EqualTo(1.0f), "Nút Enhance phải SÁNG khi đủ Data Chips và chưa max khung.");
            Assert.That(controller.AdvanceTierBtnCanvasGroup.alpha, Is.EqualTo(1.0f), "Nút Advance Tier phải SÁNG khi đủ mảnh.");
        }
        finally
        {
            Object.DestroyImmediate(modalGo);
            Object.DestroyImmediate(controllerGo);
        }
    }
}
