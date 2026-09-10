using System.Linq;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BuddyEquipSystemTests
{
    private const int PresetCount = 3;
    private const int SlotCount = 3;

    [SetUp]
    public void SetUp()
    {
        // Clear PlayerPrefs keys related to Buddy decks for clean deterministic tests
        for (int i = 0; i < PresetCount; i++)
        {
            PlayerPrefs.DeleteKey(PlayerDataService.GetBuddyDeckKey(i));
        }
        PlayerPrefs.DeleteKey(PlayerDataService.BuddyActiveDeckKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < PresetCount; i++)
        {
            PlayerPrefs.DeleteKey(PlayerDataService.GetBuddyDeckKey(i));
        }
        PlayerPrefs.DeleteKey(PlayerDataService.BuddyActiveDeckKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void BuddyDeck_DefaultState_AllSlotsAreEmptyMinusOne()
    {
        int[] loaded = PlayerDataService.LoadBuddyDeck(0, new int[] { -1, -1, -1 });
        Assert.IsNotNull(loaded);
        Assert.AreEqual(SlotCount, loaded.Length);
        Assert.AreEqual(-1, loaded[0]);
        Assert.AreEqual(-1, loaded[1]);
        Assert.AreEqual(-1, loaded[2]);
    }

    [Test]
    public void BuddyEquip_EmptySlot_AssignsToFirstAvailableSlot()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            // Set deck 0 to all empty
            int[] currentDeck = new int[] { -1, -1, -1 };
            PlayerDataService.SaveBuddyDeck(0, currentDeck);
            ctrl.InitializeDatabase();

            // Find Drone 2 (Turret Buffer)
            var turretBuffer = ctrl.AllBuddies.FirstOrDefault(b => b.id == 2);
            Assert.IsNotNull(turretBuffer, "Turret Buffer (id=2) must exist in AllBuddies");

            ctrl.OpenDetailModal(turretBuffer);

            // Trigger equip via private method invocation
            var toggleMethod = typeof(BuddyController).GetMethod("ToggleEquipSelectedBuddy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(toggleMethod, "ToggleEquipSelectedBuddy method must exist");

            toggleMethod.Invoke(ctrl, null);

            int[] saved = PlayerDataService.LoadBuddyDeck(0);
            Assert.AreEqual(2, saved[0], "Slot 0 should now be equipped with Turret Buffer (id=2)");
            Assert.AreEqual(-1, saved[1], "Slot 1 should remain empty");
            Assert.AreEqual(-1, saved[2], "Slot 2 should remain empty");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyEquip_SequentialEquip_FillsAll3Slots()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            var toggleMethod = typeof(BuddyController).GetMethod("ToggleEquipSelectedBuddy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Equip drone 1
            ctrl.OpenDetailModal(ctrl.AllBuddies[0]);
            toggleMethod.Invoke(ctrl, null);

            // Equip drone 2
            ctrl.OpenDetailModal(ctrl.AllBuddies[1]);
            toggleMethod.Invoke(ctrl, null);

            // Equip drone 3
            ctrl.OpenDetailModal(ctrl.AllBuddies[2]);
            toggleMethod.Invoke(ctrl, null);

            int[] saved = PlayerDataService.LoadBuddyDeck(0);
            Assert.AreEqual(ctrl.AllBuddies[0].id, saved[0], "Slot 0 should have Drone 1");
            Assert.AreEqual(ctrl.AllBuddies[1].id, saved[1], "Slot 1 should have Drone 2");
            Assert.AreEqual(ctrl.AllBuddies[2].id, saved[2], "Slot 2 should have Drone 3");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyEquip_WhenFull_ReplacesSlot0()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            // Pre-fill deck 0 with [10, 20, 30]
            int[] fullDeck = new int[] { 10, 20, 30 };
            PlayerDataService.SaveBuddyDeck(0, fullDeck);
            ctrl.InitializeDatabase();

            var toggleMethod = typeof(BuddyController).GetMethod("ToggleEquipSelectedBuddy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Equip Drone 2 (Turret Buffer)
            var turretBuffer = ctrl.AllBuddies.FirstOrDefault(b => b.id == 2);
            Assert.IsNotNull(turretBuffer);

            ctrl.OpenDetailModal(turretBuffer);
            toggleMethod.Invoke(ctrl, null);

            int[] saved = PlayerDataService.LoadBuddyDeck(0);
            Assert.AreEqual(2, saved[0], "Slot 0 should be replaced with Turret Buffer (id=2)");
            Assert.AreEqual(20, saved[1], "Slot 1 should remain 20");
            Assert.AreEqual(30, saved[2], "Slot 2 should remain 30");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyUnequip_ClearsSpecificSlotToMinusOne()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            // Drone 2 is in slot 1: [1, 2, 3]
            int[] initialDeck = new int[] { 1, 2, 3 };
            PlayerDataService.SaveBuddyDeck(0, initialDeck);
            ctrl.InitializeDatabase();

            var toggleMethod = typeof(BuddyController).GetMethod("ToggleEquipSelectedBuddy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Select Drone 2 (already equipped)
            var turretBuffer = ctrl.AllBuddies.FirstOrDefault(b => b.id == 2);
            ctrl.OpenDetailModal(turretBuffer);

            // Toggling an already equipped buddy should unequip it
            toggleMethod.Invoke(ctrl, null);

            int[] saved = PlayerDataService.LoadBuddyDeck(0);
            Assert.AreEqual(1, saved[0], "Slot 0 should still be 1");
            Assert.AreEqual(-1, saved[1], "Slot 1 should now be unequipped to -1");
            Assert.AreEqual(3, saved[2], "Slot 2 should still be 3");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyEquip_Persistence_SavesAndLoadsAcrossPresetDecks()
    {
        int[] deck0 = new int[] { 1, 2, -1 };
        int[] deck1 = new int[] { 3, 4, 5 };
        int[] deck2 = new int[] { -1, -1, -1 };

        PlayerDataService.SaveBuddyDeck(0, deck0);
        PlayerDataService.SaveBuddyDeck(1, deck1);
        PlayerDataService.SaveBuddyDeck(2, deck2);

        int[] loaded0 = PlayerDataService.LoadBuddyDeck(0);
        int[] loaded1 = PlayerDataService.LoadBuddyDeck(1);
        int[] loaded2 = PlayerDataService.LoadBuddyDeck(2);

        CollectionAssert.AreEqual(deck0, loaded0);
        CollectionAssert.AreEqual(deck1, loaded1);
        CollectionAssert.AreEqual(deck2, loaded2);
    }

    [Test]
    public void BuddyUnequip_FromEquippedSlot_ClearsOnlyTargetSlot()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            // Set deck 0: [1, 2, 3]
            PlayerDataService.SaveBuddyDeck(0, new int[] { 1, 2, 3 });
            ctrl.InitializeDatabase();

            // Open from equipped slot 1 (drone 2)
            var drone2 = ctrl.AllBuddies.FirstOrDefault(b => b.id == 2);
            ctrl.OpenDetailModalFromEquippedSlot(drone2, 1);

            var toggleMethod = typeof(BuddyController).GetMethod("ToggleEquipSelectedBuddy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            toggleMethod.Invoke(ctrl, null);

            int[] saved = PlayerDataService.LoadBuddyDeck(0);
            Assert.AreEqual(1, saved[0], "Slot 0 should remain 1");
            Assert.AreEqual(-1, saved[1], "Slot 1 should now be unequipped to -1");
            Assert.AreEqual(3, saved[2], "Slot 2 should remain 3");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
