#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
public class ShopPanelVisualTests
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
    }

    [Test]
    public void BuildShopPanel_BuildsAll28Sprites_AndMatchesTargetDesign()
    {
        // 1. Trigger Builder
        ShopPanelBuilder.BuildFullShopPanel();

        // 2. Verify ShopPanel
        GameObject shopPanel = GameObject.Find("ShopPanel") ?? GameObject.Find("ShopPanel (Scrollable)");
        Assert.That(shopPanel, Is.Not.Null, "ShopPanel must exist in MainMenu.");

        ScrollRect scroll = shopPanel.GetComponent<ScrollRect>();
        Assert.That(scroll, Is.Not.Null, "ShopPanel must have ScrollRect.");
        Assert.That(scroll.vertical, Is.True, "ScrollRect must be vertically scrollable.");

        Transform content = scroll.content;
        Assert.That(content, Is.Not.Null, "ScrollRect content must be assigned.");

        // 3. Verify Section 1: VIP Package
        Transform vip = content.Find("Card_VIP_Package");
        Assert.That(vip, Is.Not.Null, "VIP Package card must exist.");
        Image vipImg = vip.GetComponent<Image>();
        Assert.That(vipImg.sprite, Is.Not.Null);
        Assert.That(vipImg.sprite.name, Is.EqualTo("Card_VIP_Package"));

        // 4. Verify Section 2 & 3: Special Item Header & Carousel
        Transform headerSpecial = content.Find("Header_Special_Item");
        Assert.That(headerSpecial, Is.Not.Null, "Header_Special_Item must exist.");

        Transform carousel = content.Find("Special_Item_Carousel");
        Assert.That(carousel, Is.Not.Null, "Special_Item_Carousel must exist.");
        ShopCarouselUI carouselUI = carousel.GetComponent<ShopCarouselUI>();
        Assert.That(carouselUI, Is.Not.Null, "Carousel must have ShopCarouselUI component.");
        Assert.That(carouselUI.Pages.Length, Is.EqualTo(3), "Carousel must have 3 package cards.");
        Assert.That(carouselUI.Dots.Length, Is.EqualTo(3), "Carousel must have 3 pagination dots.");

        Transform cardsCont = carousel.Find("CardsContainer");
        Assert.That(cardsCont.Find("Card_Welcome_Package"), Is.Not.Null);
        Assert.That(cardsCont.Find("Card_Intermediate_Pack"), Is.Not.Null);
        Assert.That(cardsCont.Find("Card_Advanced_Pack"), Is.Not.Null);

        // 5. Verify Section 4 & 5: Daily Shop
        Transform headerDaily = content.Find("Header_Daily_Shop");
        Assert.That(headerDaily, Is.Not.Null);
        Transform dailyRow = content.Find("Daily_Shop_Row");
        Assert.That(dailyRow, Is.Not.Null);
        Assert.That(dailyRow.Find("Item_Daily_Gem_Free"), Is.Not.Null);
        Assert.That(dailyRow.Find("Item_Daily_Drone_Box_1"), Is.Not.Null);
        Assert.That(dailyRow.Find("Item_Daily_Drone_Box_2"), Is.Not.Null);

        // 6. Verify Section 6 & 7: Box Gacha
        Transform headerBox = content.Find("Header_Box");
        Assert.That(headerBox, Is.Not.Null);
        Transform boxSec = content.Find("Box_Gacha_Section");
        Assert.That(boxSec, Is.Not.Null);
        Assert.That(boxSec.Find("Chipset_Box_Row/Box_Chipset_1x"), Is.Not.Null);
        Assert.That(boxSec.Find("Chipset_Box_Row/Box_Chipset_10x"), Is.Not.Null);
        Assert.That(boxSec.Find("Drone_Box_Row/Box_Drone_1x"), Is.Not.Null);
        Assert.That(boxSec.Find("Drone_Box_Row/Box_Drone_10x"), Is.Not.Null);

        // 7. Verify Section 8 & 9: Meta Shop Carousel
        Transform headerMeta = content.Find("Header_Meta_Shop");
        Assert.That(headerMeta, Is.Not.Null);
        Transform metaCarousel = content.Find("Meta_Shop_Carousel");
        Assert.That(metaCarousel, Is.Not.Null, "Meta_Shop_Carousel must exist.");
        ShopCarouselUI metaCarouselUI = metaCarousel.GetComponent<ShopCarouselUI>();
        Assert.That(metaCarouselUI, Is.Not.Null, "Meta_Shop_Carousel must have ShopCarouselUI component.");
        Assert.That(metaCarouselUI.Pages.Length, Is.EqualTo(2), "Meta_Shop_Carousel must have 2 package cards.");
        Assert.That(metaCarouselUI.Dots.Length, Is.EqualTo(2), "Meta_Shop_Carousel must have 2 pagination dots.");
        Transform metaCardsCont = metaCarousel.Find("CardsContainer");
        Assert.That(metaCardsCont.Find("Card_Gun_Pack"), Is.Not.Null);
        Assert.That(metaCardsCont.Find("Card_Drone_Pack"), Is.Not.Null);

        // 8. Verify Section 10 & 11: Data Chip
        Transform headerData = content.Find("Header_Data_Chip");
        Assert.That(headerData, Is.Not.Null);
        Transform dataRow = content.Find("Data_Chip_Row");
        Assert.That(dataRow, Is.Not.Null);
        Assert.That(dataRow.Find("Item_Data_Chip_1"), Is.Not.Null);
        Assert.That(dataRow.Find("Item_Data_Chip_2"), Is.Not.Null);
        Assert.That(dataRow.Find("Item_Data_Chip_3"), Is.Not.Null);

        // 9. Verify Section 12 & 13: Gem Event
        Transform headerGem = content.Find("Header_Gem_Event");
        Assert.That(headerGem, Is.Not.Null);
        Transform gemSec = content.Find("Gem_Event_Section");
        Assert.That(gemSec, Is.Not.Null);
        Assert.That(gemSec.Find("Gem_Row_1/Item_Gem_1"), Is.Not.Null);
        Assert.That(gemSec.Find("Gem_Row_1/Item_Gem_2"), Is.Not.Null);
        Assert.That(gemSec.Find("Gem_Row_1/Item_Gem_3"), Is.Not.Null);
        Assert.That(gemSec.Find("Gem_Row_2/Item_Gem_4"), Is.Not.Null);
        Assert.That(gemSec.Find("Gem_Row_2/Item_Gem_5"), Is.Not.Null);
        Assert.That(gemSec.Find("Gem_Row_2/Item_Gem_6"), Is.Not.Null);

        // 10. Verify ShopController
        ShopController controller = shopPanel.GetComponent<ShopController>();
        Assert.That(controller, Is.Not.Null);
    }
}
#endif
