#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[TestFixture]
public sealed class BoxOpeningFlowTests
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        BoxOpeningSceneInstaller.InstallIntoMainMenu();
    }

    [Test]
    public void Scene_HasOneCanvasOneEventSystem_AndCompleteReusableHierarchy()
    {
        BoxOpeningController controller = Object.FindObjectOfType<BoxOpeningController>(true);
        Assert.That(controller, Is.Not.Null);
        Assert.That(Object.FindObjectsOfType<Canvas>(true).Length, Is.EqualTo(1));
        Assert.That(Object.FindObjectsOfType<EventSystem>(true).Length, Is.EqualTo(1));

        Transform root = controller.transform.Find("BoxOpeningCanvas");
        Assert.That(root, Is.Not.Null);
        Assert.That(root.Find("ChestRoot/ChestBody"), Is.Not.Null);
        Assert.That(root.Find("ChestRoot/ChestLid"), Is.Not.Null);
        Assert.That(root.Find("OpenVFXRoot").childCount, Is.EqualTo(8));
        Assert.That(root.Find("RewardRoot/RewardGlow"), Is.Not.Null);
        Assert.That(root.Find("RewardRoot/RewardBurst"), Is.Not.Null);
        Assert.That(root.Find("ResultPanel/RewardGrid").childCount, Is.EqualTo(10));
        Assert.That(Enumerable.Range(0, 10).All(index =>
            root.Find("ResultPanel/RewardGrid").GetChild(index).Find("QuantityEffect") != null), Is.True);
        Assert.That(root.Find("SkipButton").GetComponent<UnityEngine.UI.Button>(), Is.Not.Null);
    }

    [Test]
    public void VisualCatalog_HasAllChestVariants_AndEveryRewardIcon()
    {
        BoxOpeningController controller = Object.FindObjectOfType<BoxOpeningController>(true);
        Assert.That(controller.BoxVisualSets.Length, Is.EqualTo(4));
        Assert.That(controller.BoxVisualSets.All(set =>
            set != null && set.closedSprite != null && set.baseSprite != null &&
            set.lidSprite != null && set.openSprite != null), Is.True);

        Assert.That(controller.ChipsetRewardVisuals.Length, Is.EqualTo(10));
        Assert.That(controller.BuddyRewardVisuals.Length, Is.EqualTo(12));
        Assert.That(controller.ChipsetRewardVisuals.All(entry =>
            entry != null && entry.icon != null && entry.frame != null && !string.IsNullOrWhiteSpace(entry.displayName)), Is.True);
        Assert.That(controller.BuddyRewardVisuals.All(entry =>
            entry != null && entry.icon != null && entry.frame != null && !string.IsNullOrWhiteSpace(entry.displayName)), Is.True);
    }

    [Test]
    public void Shop_IsWiredToSinglePresentationController()
    {
        BoxOpeningController controller = Object.FindObjectOfType<BoxOpeningController>(true);
        ShopController[] shops = Object.FindObjectsOfType<ShopController>(true);
        Assert.That(shops, Is.Not.Empty);
        Assert.That(shops.All(shop =>
        {
            SerializedObject serializedShop = new SerializedObject(shop);
            return serializedShop.FindProperty("boxOpeningController").objectReferenceValue == controller;
        }), Is.True);
        Assert.That(controller.CurrentState, Is.EqualTo(BoxOpeningController.BoxOpeningState.Idle));
        Assert.That(controller.IsBusy, Is.False);
    }

    [Test]
    public void BoxAssets_AreImportedAsSprites_WithoutMissingReferences()
    {
        string[] names =
        {
            "Box_Chipset_1x", "Box_Chipset_10x", "Box_Drone_1x", "Box_Drone_10x"
        };
        string[] states = { "Closed", "Base", "Lid", "Open" };
        foreach (string name in names)
        foreach (string state in states)
        {
            string path = $"Assets/Sprites/UI/Shop/Boxes/{name}_{state}.png";
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
        }
    }
}
#endif
