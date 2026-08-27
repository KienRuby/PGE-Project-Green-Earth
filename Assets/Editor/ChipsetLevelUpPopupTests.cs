using System.Linq;
using NUnit.Framework;

public class ChipsetLevelUpPopupTests
{
    [Test]
    public void SharedCatalog_ContainsAllMainMenuChipsets_AndReturnsFreshCopies()
    {
        var first = ChipsetController.CreateDefaultDatabase();
        var second = ChipsetController.CreateDefaultDatabase();

        Assert.That(first.Count, Is.EqualTo(24));
        Assert.That(second.Count, Is.EqualTo(24));
        Assert.That(first.Select(item => item.id).Distinct().Count(), Is.EqualTo(24));
        Assert.That(first[0], Is.Not.SameAs(second[0]));

        first[0].level = 99;
        Assert.That(second[0].level, Is.Not.EqualTo(99));
    }

    [Test]
    public void SelectDistinctOffers_ReturnsFourUniqueClonedChipsets()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();
        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 4, new System.Random(1337));

        Assert.That(offers.Count, Is.EqualTo(4));
        Assert.That(offers.Select(item => item.id).Distinct().Count(), Is.EqualTo(4));
        Assert.That(offers.All(offer => catalog.All(source => !ReferenceEquals(source, offer))), Is.True);
    }

    [Test]
    public void SelectDistinctOffers_ClampsToAvailableCatalogSize()
    {
        var catalog = ChipsetController.CreateDefaultDatabase().Take(2).ToList();
        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 4, new System.Random(42));

        Assert.That(offers.Count, Is.EqualTo(2));
        Assert.That(offers.Select(item => item.id).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void SelectDistinctOffers_DoesNotMutateMainMenuCatalog()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();
        int[] originalLevels = catalog.Select(item => item.level).ToArray();

        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 4, new System.Random(7));
        offers[0].level = 77;

        CollectionAssert.AreEqual(originalLevels, catalog.Select(item => item.level).ToArray());
    }

    [Test]
    public void RuntimeCatalog_ContainsOnlyTheTenPrimaryChipsets()
    {
        var catalog = ChipsetLevelUpPopup.CreateRuntimeCatalog();

        Assert.That(catalog.Count, Is.EqualTo(10));
        CollectionAssert.AreEqual(Enumerable.Range(1, 10), catalog.Select(chip => chip.id));
    }

    [Test]
    public void UpgradeRuntimeChipset_IncreasesExactlyOneLevel_AndCapsAtFive()
    {
        UnityEngine.GameObject go = new UnityEngine.GameObject("RuntimeChipLevelTest");
        ChipsetLevelUpPopup popup = go.AddComponent<ChipsetLevelUpPopup>();

        try
        {
            Assert.That(popup.GetRuntimeLevel(1), Is.EqualTo(0));
            for (int expectedLevel = 1; expectedLevel <= ChipsetLevelUpPopup.MaxRuntimeChipLevel; expectedLevel++)
            {
                Assert.That(popup.UpgradeRuntimeChipset(1), Is.EqualTo(expectedLevel));
                Assert.That(popup.GetRuntimeLevel(1), Is.EqualTo(expectedLevel));
            }

            Assert.That(popup.UpgradeRuntimeChipset(1), Is.EqualTo(ChipsetLevelUpPopup.MaxRuntimeChipLevel));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void VisualLibrary_UsesFiveTierLeverFrames_AndFiveLevelPips()
    {
        ChipsetLevelVisualLibrary library = UnityEngine.Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");

        Assert.That(library, Is.Not.Null);
        CollectionAssert.AreEqual(
            new[] { "ChipsetLeverGreen", "ChipsetLeverBlue", "ChipsetLeverPurple", "ChipsetLeveYellow", "ChipsetLeverRed" },
            library.tierLeverFrames.Select(sprite => sprite.name));
        CollectionAssert.AreEqual(
            new[] { "cấp 1", "cấp 2", "cấp 3", "Cấp 4", "Cấp 5" },
            library.levelPipSprites.Select(sprite => sprite.name));
        Assert.That(library.primaryChipIcons.Length, Is.EqualTo(10));
        Assert.That(library.primaryChipIcons.All(sprite => sprite != null), Is.True);
        Assert.That(library.primaryChipIcons.Select(sprite => sprite.name).Distinct().Count(), Is.EqualTo(10));

        string[] iconKeys = ChipsetLevelUpPopup.CreateRuntimeCatalog()
            .Select(chip => chip.iconKey)
            .ToArray();
        UnityEngine.Sprite[] resolvedIcons = iconKeys
            .Select(key => ChipsetLevelUpPopup.FindMatchingIcon(library.primaryChipIcons, key))
            .ToArray();

        Assert.That(resolvedIcons.All(sprite => sprite != null), Is.True);
        Assert.That(
            resolvedIcons.Select(sprite => sprite.name).Distinct().Count(),
            Is.EqualTo(10),
            "Mỗi chipset chính phải resolve thành một icon atlas riêng, không được fallback về Standard Gun.");

        string[] expectedTierFrames =
        {
            "ChipsetLeverGreen",
            "ChipsetLeverBlue",
            "ChipsetLeverPurple",
            "ChipsetLeveYellow",
            "ChipsetLeverRed"
        };
        for (int tierIndex = 0; tierIndex < expectedTierFrames.Length; tierIndex++)
        {
            ChipTier tier = (ChipTier)(tierIndex + 1);
            UnityEngine.Sprite gameplayFrame = ChipsetLevelUpPopup.ResolveLeverFrameForTier(library.tierLeverFrames, tier);
            Assert.That(gameplayFrame, Is.Not.Null);
            Assert.That(
                gameplayFrame.name,
                Is.EqualTo(expectedTierFrames[tierIndex]),
                $"Popup gameplay phải dùng đúng khung đã mở khóa của tier {tier}.");
        }
    }

    [Test]
    public void SelectEquippedCatalog_UsesDeckOrder_AndReturnsFreshCopies()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();
        int[] equippedIds = { 10, 20, -1, 16, 10 };

        var gameplayCatalog = ChipsetController.SelectEquippedCatalog(catalog, equippedIds);

        CollectionAssert.AreEqual(new[] { 10, 20, 16 }, gameplayCatalog.Select(item => item.id).ToArray());
        Assert.That(gameplayCatalog.All(item => catalog.All(source => !ReferenceEquals(source, item))), Is.True);
    }

    [Test]
    public void SelectEquippedCatalog_FallsBackToFullCatalog_WhenDeckIsEmpty()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();

        var gameplayCatalog = ChipsetController.SelectEquippedCatalog(catalog, new[] { -1, -1 });

        Assert.That(gameplayCatalog.Count, Is.EqualTo(catalog.Count));
    }

    [Test]
    public void Reroll_MaxRerollsPerLevel_IsCappedAtTwo()
    {
        UnityEngine.GameObject go = new UnityEngine.GameObject("TestLevelUpPopup");
        ChipsetLevelUpPopup popup = go.AddComponent<ChipsetLevelUpPopup>();

        try
        {
            ChipManager.IsTestMode = true;
            PlayerDataService.RedGems = 1000;

            Assert.That(popup.MaxRerollsPerLevel, Is.EqualTo(2));
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(0));
            Assert.That(popup.RemainingRerolls, Is.EqualTo(2));

            // Reroll 1st time -> Thành công
            bool firstReroll = popup.TryReroll();
            Assert.That(firstReroll, Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(1));
            Assert.That(popup.RemainingRerolls, Is.EqualTo(1));

            // Reroll 2nd time -> Thành công
            bool secondReroll = popup.TryReroll();
            Assert.That(secondReroll, Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));
            Assert.That(popup.RemainingRerolls, Is.EqualTo(0));

            // Reroll 3rd time -> Phải thất bại do đã chạm tối đa 2 lần
            bool thirdReroll = popup.TryReroll();
            Assert.That(thirdReroll, Is.False, "Draw again lần thứ 3 phải bị chặn vì tối đa chỉ được 2 lần.");
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));
            Assert.That(popup.RemainingRerolls, Is.EqualTo(0));
        }
        finally
        {
            ChipManager.IsTestMode = false;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
