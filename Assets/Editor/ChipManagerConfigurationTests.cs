using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ChipManagerConfigurationTests
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo InstanceField = typeof(ChipManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
    private ChipManager previousInstance;
    private GameObject testObject;
    private ChipManager manager;
    private readonly string[] keys = { PlayerDataService.DataChipsKey, PlayerDataService.RedGemsKey,
        PlayerDataService.EnergyKey, PlayerDataService.AdvanceStonesKey };
    private int[] savedValues;
    private bool[] savedKeys;

    [SetUp]
    public void SetUp()
    {
        previousInstance = (ChipManager)InstanceField.GetValue(null);
        savedValues = new int[keys.Length];
        savedKeys = new bool[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            savedKeys[i] = PlayerPrefs.HasKey(keys[i]);
            savedValues[i] = PlayerPrefs.GetInt(keys[i]);
        }
        testObject = new GameObject("ChipManager configuration test") { hideFlags = HideFlags.HideAndDontSave };
        testObject.SetActive(false);
        manager = testObject.AddComponent<ChipManager>();
        InstanceField.SetValue(null, manager);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
        InstanceField.SetValue(null, previousInstance);
        for (int i = 0; i < keys.Length; i++)
        {
            if (savedKeys[i]) PlayerPrefs.SetInt(keys[i], savedValues[i]);
            else PlayerPrefs.DeleteKey(keys[i]);
        }
    }

    private void Set(string field, object value) => typeof(ChipManager).GetField(field, PrivateInstance).SetValue(manager, value);

    [TestCase(false)]
    [TestCase(true)]
    public void Awake_PreservesConfiguredTestModeAndFiniteBalances(bool testMode)
    {
        Set("enableTestMode", testMode);
        Set("infiniteChipsInTestMode", false);
        Set("testDataChips", 100000);
        Set("testRedGems", 3456);
        typeof(ChipManager).GetMethod("Awake", PrivateInstance).Invoke(manager, null);

        Assert.That(ChipManager.IsTestMode, Is.EqualTo(testMode));
        Assert.That(ChipManager.IsInfiniteInTest, Is.False);
        if (testMode)
        {
            Assert.That(ChipManager.DataChips, Is.EqualTo(100000));
            Assert.That(ChipManager.RedGems, Is.EqualTo(3456));
        }
    }

    [Test]
    public void InfinityToggle_PreservesCustomBalances_ThenResumesFiniteSpending()
    {
        Set("enableTestMode", true);
        Set("infiniteChipsInTestMode", false);
        Set("testDataChips", 100000);
        Set("testRedGems", 2000);
        typeof(ChipManager).GetMethod("Awake", PrivateInstance).Invoke(manager, null);
        Assert.That(ChipManager.TrySpendDataChips(250), Is.True);
        Assert.That(ChipManager.DataChips, Is.EqualTo(99750));

        Set("infiniteChipsInTestMode", true);
        Assert.That(ChipManager.TrySpendDataChips(1000000), Is.True);
        Assert.That(ChipManager.TrySpendRedGems(1000000), Is.True);
        Set("infiniteChipsInTestMode", false);
        Assert.That(ChipManager.DataChips, Is.EqualTo(99750));
        Assert.That(ChipManager.RedGems, Is.EqualTo(2000));
        Assert.That(ChipManager.TrySpendDataChips(99751), Is.False);
        Assert.That(ChipManager.TrySpendRedGems(500), Is.True);
        Assert.That(ChipManager.RedGems, Is.EqualTo(1500));
    }
}
