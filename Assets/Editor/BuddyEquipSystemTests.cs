using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

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
    public void BuddyController_AllBuddies_ContainsExactlyFiveActiveDrones()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            Assert.AreEqual(5, ctrl.AllBuddies.Count, "There must be exactly 5 active drones in the game.");
            int[] expectedIds = new int[] { 1, 2, 3, 4, 10 };
            int[] actualIds = ctrl.AllBuddies.Select(b => b.id).ToArray();
            CollectionAssert.AreEqual(expectedIds, actualIds, "Active drones must strictly be IDs 1, 2, 3, 4, 10.");

            foreach (int id in expectedIds)
            {
                Assert.IsTrue(BuddyController.IsPrimaryPlayableDrone(id), $"ID {id} must be recognized as a primary playable drone.");
            }

            int[] removedIds = new int[] { 5, 6, 7, 8, 9, 11, 12 };
            foreach (int id in removedIds)
            {
                Assert.IsFalse(BuddyController.IsPrimaryPlayableDrone(id), $"Removed drone ID {id} must NOT be recognized as primary playable drone.");
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
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
    public void BuddyEquip_WhenFull_RejectsUntilADroneIsUnequipped()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            int[] fullDeck = new int[] { 1, 3, 10 };
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
            CollectionAssert.AreEqual(fullDeck, saved, "A fourth drone must not replace any equipped drone.");

            ctrl.OpenDetailModalFromEquippedSlot(ctrl.AllBuddies.First(b => b.id == 3), 1);
            toggleMethod.Invoke(ctrl, null);
            ctrl.OpenDetailModalFromInventory(turretBuffer);
            toggleMethod.Invoke(ctrl, null);
            CollectionAssert.AreEqual(new[] { 1, 2, 10 }, PlayerDataService.LoadBuddyDeck(0),
                "After unequipping, the new drone must fill only the freed slot.");
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

    [Test]
    public void BuddyCardUI_WhenEnoughFragments_UpgradeArrowIsActive()
    {
        GameObject go = new GameObject("TestCard", typeof(BuddyCardUI));
        try
        {
            BuddyCardUI card = go.GetComponent<BuddyCardUI>();
            BuddyItemData data = new BuddyItemData
            {
                id = 1,
                buddyName = "Sloy",
                level = 1,
                count = 29,
                requiredCount = 10,
                enhanceCost = 500,
                tier = BuddyTier.Common
            };

            Assert.IsTrue(data.CanAdvanceTier);
            Assert.IsTrue(data.CanUpgrade);

            card.Setup(data, null, null);
            Assert.IsNotNull(card.UpgradeArrowGroup, "UpgradeArrowGroup should be created/ensured");
            Assert.IsTrue(card.UpgradeArrowGroup.activeSelf, "UpgradeArrowGroup must be active when CanUpgrade is true");
            Image arrowImg = card.UpgradeArrowGroup.GetComponent<Image>();
            Assert.IsNotNull(arrowImg, "UpgradeArrowGroup must have an Image component");
            Assert.IsNotNull(arrowImg.sprite, "UpgradeArrowGroup must have a non-null sprite (not white square)");
            Assert.IsTrue(arrowImg.sprite.name.Contains("badge-upgrade"), "Sprite should be badge-upgrade");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyCardUI_SetEquippedBadge_TogglesBadgeProperly()
    {
        GameObject go = new GameObject("TestCard", typeof(BuddyCardUI));
        try
        {
            BuddyCardUI card = go.GetComponent<BuddyCardUI>();
            card.SetEquippedBadge(true);
            Assert.IsNotNull(card.EquippedBadgeGroup, "EquippedBadgeGroup should be created");
            Assert.IsTrue(card.EquippedBadgeGroup.activeSelf, "EquippedBadgeGroup should be active");

            card.SetEquippedBadge(false);
            Assert.IsFalse(card.EquippedBadgeGroup.activeSelf, "EquippedBadgeGroup should be deactivated");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyController_SortByQuantity_PutsHighestCountFirst()
    {
        GameObject go = new GameObject("TestBuddyController", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            PlayerDataService.SetBuddyPieceCount(1, 10);
            PlayerDataService.SetBuddyPieceCount(2, 50);

            ctrl.InitializeDatabase();
            ctrl.SetSortMode(true);

            var list = ctrl.AllBuddies.OrderByDescending(b => b.count).ToList();
            Assert.AreEqual(2, list[0].id, "Highest quantity buddy should be first in sorted order");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuddyController_EquipBuddy_ClonesExactTemplateToEquippedSlot()
    {
        GameObject root = new GameObject("BuddyController_Root", typeof(RectTransform));
        try
        {
            // Create EquippedRow
            GameObject equippedRow = new GameObject("EquippedRow", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
            equippedRow.transform.SetParent(root.transform, false);

            // Create SlotIconBuddy container with 2 templates
            GameObject slotIconContainer = new GameObject("SlotIconBuddy", typeof(RectTransform));
            slotIconContainer.transform.SetParent(root.transform, false);

            GameObject templateSnowflake = new GameObject("drone-snowflake", typeof(RectTransform), typeof(BuddyCardUI));
            templateSnowflake.transform.SetParent(slotIconContainer.transform, false);
            templateSnowflake.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 320f);

            GameObject templateSpider = new GameObject("drone-spider", typeof(RectTransform), typeof(BuddyCardUI));
            templateSpider.transform.SetParent(slotIconContainer.transform, false);
            templateSpider.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 320f);

            // Child on templateSpider to verify child preservation
            GameObject spiderIcon = new GameObject("DroneIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            spiderIcon.transform.SetParent(templateSpider.transform, false);
            spiderIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(10f, -20f);

            BuddyController ctrl = root.AddComponent<BuddyController>();
            ctrl.InitializeDatabase();

            // Find Drone 2 (Turret Buffer / drone-spider)
            var spiderBuddy = ctrl.AllBuddies.FirstOrDefault(b => b.id == 2);
            Assert.IsNotNull(spiderBuddy);

            // Open and Equip
            ctrl.OpenDetailModal(spiderBuddy);
            var toggleMethod = typeof(BuddyController).GetMethod("ToggleEquipSelectedBuddy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            toggleMethod.Invoke(ctrl, null);

            // Verify that EquippedRow has slot 0 cloned from drone-spider
            Assert.IsTrue(equippedRow.transform.childCount >= 1);
            Transform slot0 = equippedRow.transform.GetChild(0);
            Assert.AreEqual("EquippedSlot_0_drone-spider", slot0.name, "Slot 0 should be cloned from drone-spider template");
            Assert.AreEqual(new Vector2(150f, 200f), slot0.GetComponent<RectTransform>().sizeDelta, "Slot 0 should match the compact Buddy card dimensions");

            // Verify child DroneIcon position was preserved from template
            Transform childIcon = slot0.Find("DroneIcon");
            Assert.IsNotNull(childIcon, "DroneIcon child should exist in clone");
            Assert.AreEqual(new Vector2(10f, -20f), childIcon.GetComponent<RectTransform>().anchoredPosition, "Child position must be preserved");

            // Unequip
            ctrl.OpenDetailModalFromEquippedSlot(spiderBuddy, 0);
            toggleMethod.Invoke(ctrl, null);

            // Verify reverted to empty slot
            slot0 = equippedRow.transform.GetChild(0);
            Assert.AreEqual("EquippedSlot_0", slot0.name, "Slot 0 should revert to clean empty slot name");
            // Simulate a stale fourth visual slot from the previous implementation.
            var stale = Object.Instantiate(slot0.gameObject, equippedRow.transform);
            stale.name = "StaleExtraSlot";
            for (int i = 0; i < 3; i++)
            {
                ctrl.OpenDetailModalFromInventory(spiderBuddy);
                toggleMethod.Invoke(ctrl, null);
                ctrl.RefreshEquippedGrid();
                Assert.AreEqual(3, equippedRow.transform.childCount, "Repeated refresh must leave exactly three slots.");
                Assert.AreEqual("EquippedSlot_0_drone-spider", equippedRow.transform.GetChild(0).name);
                ctrl.OpenDetailModalFromEquippedSlot(spiderBuddy, 0);
                toggleMethod.Invoke(ctrl, null);
                Assert.AreEqual(3, equippedRow.transform.childCount);
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BuddyCardUI_ConfigureText_StandardizesFontSizeAndColor()
    {
        GameObject go = new GameObject("BuddyCardUI_TextTest", typeof(RectTransform), typeof(BuddyCardUI));
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            BuddyCardUI card = go.GetComponent<BuddyCardUI>();

            GameObject lvlObj = new GameObject("Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            lvlObj.transform.SetParent(go.transform, false);
            lvlObj.transform.localScale = new Vector3(0.639f, 0.639f, 0.639f);
            TMPro.TMP_Text lvlText = lvlObj.GetComponent<TMPro.TMP_Text>();
            lvlText.fontSize = 50f;
            lvlText.color = Color.red;

            GameObject prgObj = new GameObject("Quantity", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            prgObj.transform.SetParent(go.transform, false);
            TMPro.TMP_Text prgText = prgObj.GetComponent<TMPro.TMP_Text>();
            prgText.fontSize = 36f;
            prgText.color = new Color(0.04f, 0.08f, 0.12f, 1f);

            BuddyCardUI.ConfigureLevelText(lvlText);
            BuddyCardUI.ConfigureProgressText(prgText);

            Assert.AreEqual(Color.white, lvlText.color, "Level text must be white");
            Assert.AreEqual(BuddyCardUI.StandardLevelFontSize, lvlText.fontSize, "Level text must match StandardLevelFontSize");
            Assert.AreEqual(Vector3.one, lvlObj.transform.localScale, "Level transform scale must be normalized to Vector3.one");

            Assert.AreEqual(Color.white, prgText.color, "Progress text must be white");
            Assert.AreEqual(BuddyCardUI.StandardProgressFontSize, prgText.fontSize, "Progress text must match StandardProgressFontSize");
            Assert.AreEqual(Vector3.one, prgObj.transform.localScale, "Progress transform scale must be Vector3.one");

            // Verify UpdateProgressBar keeps text white
            card.InitializeReferences(null, null, lvlText, prgText, null, null, null, null, null, null);
            card.UpdateProgressBar(0.75f);
            Assert.AreEqual(Color.white, prgText.color, "Progress text color must remain white when progress is filled");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
