using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ChipsetShopSyncTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private readonly string[] suffixes = { "Level", "Tier", "Count", "ReqCount", "HasStar", "TierEnhanceCount", "EnhanceCost" };
    private readonly Dictionary<string, int?> backup = new Dictionary<string, int?>();
    private GameObject root;
    private ChipsetController controller;
    private ChipItemData chip;

    [SetUp]
    public void SetUp()
    {
        foreach (string suffix in suffixes)
        {
            string key = PlayerDataService.GetChipItemPrefix(1) + suffix;
            backup[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
            PlayerPrefs.DeleteKey(key);
        }
        root = new GameObject("Chipset sync test") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        controller = root.AddComponent<ChipsetController>();
        chip = new ChipItemData { id = 1, count = 2, level = 1, tier = ChipTier.Magic };
        typeof(ChipsetController).GetField("allChips", PrivateInstance)
            .SetValue(controller, new List<ChipItemData> { chip });
    }

    [TearDown]
    public void TearDown()
    {
        Invoke("OnDisable");
        Object.DestroyImmediate(root);
        foreach (var pair in backup)
        {
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
        }
        PlayerPrefs.Save();
        backup.Clear();
    }

    private void Invoke(string method) => typeof(ChipsetController).GetMethod(method, PrivateInstance).Invoke(controller, null);

    [Test]
    public void SerializedCatalog_LoadsSavedProgressWithoutReplacingCardData()
    {
        PlayerDataService.SaveChipsetItemData(1, 8, (int)ChipTier.Rare, 35, 10, true);
        controller.InitializeDatabase();
        Assert.That(controller.AllChips[0], Is.SameAs(chip));
        Assert.That(chip.count, Is.EqualTo(35));
        Assert.That(chip.level, Is.EqualTo(8));
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Rare));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ShopDrops_UpdateChipset_AfterReturningToTab(bool tabWasVisible)
    {
        PlayerDataService.SaveChipsetItemData(1, 8, (int)ChipTier.Rare, 35, 10, true);
        controller.InitializeDatabase();
        if (tabWasVisible) Invoke("OnEnable");
        var drops = new List<ShopBoxDropRoller.Drop>
        {
            new ShopBoxDropRoller.Drop(1, 3), new ShopBoxDropRoller.Drop(1, 7)
        };
        object snapshot = typeof(ShopController).GetMethod("CaptureBoxDropSnapshot", PrivateStatic)
            .Invoke(null, new object[] { ShopController.RewardType.ChipsetBox, drops });
        typeof(ShopController).GetMethod("ApplyBoxDrops", PrivateStatic).Invoke(null, new[] { snapshot });
        if (!tabWasVisible)
        {
            Assert.That(chip.count, Is.EqualTo(35), "Hidden tab missed the event");
            Invoke("OnEnable");
        }
        Assert.That(chip.count, Is.EqualTo(45));
        Assert.That(chip.level, Is.EqualTo(8));
        Invoke("OnDisable");
        Invoke("OnEnable");
        Assert.That(chip.count, Is.EqualTo(45), "Reopening must not grant rewards twice");
    }
}
