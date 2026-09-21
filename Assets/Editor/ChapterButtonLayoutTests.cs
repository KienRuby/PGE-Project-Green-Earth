#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
public class ChapterButtonLayoutTests
{
    private const string QuestBannerSpritePath = "Assets/Sprites/UI/Chapter/btn_quest_banner.png";
    private const string GrowthFundSpritePath = "Assets/Sprites/UI/Chapter/btn_growth_fund.png";
    private const string TowerDefSpritePath = "Assets/Sprites/UI/Chapter/btn_tower_def.png";
    private const string GemMineSpritePath = "Assets/Sprites/UI/Chapter/btn_gem_mine.png";

    [Test]
    public void ChapterButtonSprites_AllExistInAssetDatabase()
    {
        Sprite questSprite = AssetDatabase.LoadAssetAtPath<Sprite>(QuestBannerSpritePath);
        Sprite fundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrowthFundSpritePath);
        Sprite towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDefSpritePath);
        Sprite mineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GemMineSpritePath);

        Assert.IsNotNull(questSprite, "Quest banner sprite must exist");
        Assert.IsNotNull(fundSprite, "Growth fund sprite must exist");
        Assert.IsNotNull(towerSprite, "Tower Def button sprite must exist");
        Assert.IsNotNull(mineSprite, "Gem Mine button sprite must exist");
    }

    [Test]
    public void ChapterScreenController_SideButtons_CanBeRegisteredAndClicked()
    {
        GameObject go = new GameObject("ChapterPanelTest", typeof(RectTransform), typeof(ChapterScreenController));
        ChapterScreenController ctrl = go.GetComponent<ChapterScreenController>();

        GameObject towerBtnObj = new GameObject("TowerDefBtn", typeof(RectTransform), typeof(Button));
        GameObject gemMineBtnObj = new GameObject("GemMineBtn", typeof(RectTransform), typeof(Button));

        Button towerBtn = towerBtnObj.GetComponent<Button>();
        Button gemMineBtn = gemMineBtnObj.GetComponent<Button>();

        ctrl.SetSideModeButtonsForTesting(towerBtn, gemMineBtn);

        Assert.AreEqual(towerBtn, ctrl.TowerDefButton);
        Assert.AreEqual(gemMineBtn, ctrl.GemMineButton);

        // Click execution should not throw
        Assert.DoesNotThrow(() => ctrl.OnTowerDefClicked());
        Assert.DoesNotThrow(() => ctrl.OnGemMineClicked());

        Object.DestroyImmediate(towerBtnObj);
        Object.DestroyImmediate(gemMineBtnObj);
        Object.DestroyImmediate(go);
    }
}
#endif
