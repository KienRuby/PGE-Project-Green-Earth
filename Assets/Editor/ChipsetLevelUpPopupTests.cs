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
