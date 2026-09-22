#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;
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

    [Test]
    public void ChapterSelectorHeader_And_WaveBadge_SceneLayout_MatchesTargetVisuals()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var ctrl = Object.FindObjectOfType<ChapterScreenController>(true);
        Assert.IsNotNull(ctrl, "ChapterScreenController must exist in MainMenu.unity");

        Transform headerTr = ctrl.transform.Find("ChapterSelectorHeader");
        Assert.IsNotNull(headerTr, "ChapterSelectorHeader must exist under ChapterScreenController");

        Transform subTr = headerTr.Find("SubtitleText");
        Assert.IsNotNull(subTr, "SubtitleText must exist");
        RectTransform subRect = subTr.GetComponent<RectTransform>();
        Assert.AreEqual(30f, subRect.anchoredPosition.y, 0.01f, "SubtitleText Y must be 30");

        Transform titleTr = headerTr.Find("TitleText") ?? headerTr.Find("ChapterTitleText");
        Assert.IsNotNull(titleTr, "TitleText must exist");
        RectTransform titleRect = titleTr.GetComponent<RectTransform>();
        Assert.AreEqual(-25f, titleRect.anchoredPosition.y, 0.01f, "TitleText Y must be -25");

        Transform prevBtnTr = headerTr.Find("PreviousChapterButton") ?? headerTr.Find("PrevChapterButton");
        Assert.IsNotNull(prevBtnTr, "PreviousChapterButton must exist");
        RectTransform prevRect = prevBtnTr.GetComponent<RectTransform>();
        Assert.AreEqual(titleRect.anchoredPosition.y, prevRect.anchoredPosition.y, 0.01f,
            "PreviousChapterButton must be on the exact same vertical center line as TitleText");

        Transform nextBtnTr = headerTr.Find("NextChapterButton");
        Assert.IsNotNull(nextBtnTr, "NextChapterButton must exist");
        RectTransform nextRect = nextBtnTr.GetComponent<RectTransform>();
        Assert.AreEqual(titleRect.anchoredPosition.y, nextRect.anchoredPosition.y, 0.01f,
            "NextChapterButton must be on the exact same vertical center line as TitleText");

        // Verify TextMeshPro styling & CanvasRenderer
        var titleTmp = titleTr.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(titleTmp, "TitleText must have TextMeshProUGUI");
        Assert.IsTrue(titleTmp.font.name.Contains("Nunito"), "TitleText must use Nunito font");
        Assert.IsFalse(titleTmp.GetComponent<CanvasRenderer>().cullTransparentMesh, "TitleText mesh culling must be false");

        // Verify WaveBadge
        Transform previewTr = ctrl.transform.Find("StagePreviewWindow");
        Assert.IsNotNull(previewTr, "StagePreviewWindow must exist");
        Transform viewportTr = previewTr.Find("Viewport");
        Assert.IsNotNull(viewportTr, "Viewport must exist");
        Transform waveBadgeTr = viewportTr.Find("WaveBadge");
        Assert.IsNotNull(waveBadgeTr, "WaveBadge must exist");

        RectTransform waveRect = waveBadgeTr.GetComponent<RectTransform>();
        Assert.AreEqual(0f, waveRect.anchoredPosition.x, 0.01f, "WaveBadge must be horizontally centered at x=0");
        Assert.AreEqual(-25f, waveRect.anchoredPosition.y, 0.01f, "WaveBadge Y must be -25");
        Assert.AreEqual(300f, waveRect.sizeDelta.x, 0.01f, "WaveBadge width must be 300");
        Assert.AreEqual(52f, waveRect.sizeDelta.y, 0.01f, "WaveBadge height must be 52");

        Image badgeImg = waveBadgeTr.GetComponent<Image>();
        Assert.IsNotNull(badgeImg, "WaveBadge must have Image");
        Assert.Greater(badgeImg.color.a, 0.2f, "WaveBadge background alpha must be translucent (>0.2)");
        Assert.IsFalse(waveBadgeTr.GetComponent<CanvasRenderer>().cullTransparentMesh, "WaveBadge mesh culling must be false");
    }

    [Test]
    public void ChapterSelectorHeader_RefreshChapterView_UpdatesSubtitleAndTitle()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var ctrl = Object.FindObjectOfType<ChapterScreenController>(true);
        Assert.IsNotNull(ctrl, "ChapterScreenController must exist");

        ctrl.RefreshChapterView();

        Transform headerTr = ctrl.transform.Find("ChapterSelectorHeader");
        var subTmp = headerTr.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
        var titleTmp = (headerTr.Find("TitleText") ?? headerTr.Find("ChapterTitleText"))?.GetComponent<TextMeshProUGUI>();

        Assert.IsNotNull(subTmp, "SubtitleText TMP component must exist");
        Assert.IsNotNull(titleTmp, "TitleText TMP component must exist");

        Assert.IsTrue(subTmp.gameObject.activeSelf, "SubtitleText must be active");
        Assert.IsTrue(titleTmp.gameObject.activeSelf, "TitleText must be active");

        Assert.IsTrue(subTmp.text.StartsWith("Chapter."), $"SubtitleText must start with 'Chapter.', got '{subTmp.text}'");
        Assert.IsNotEmpty(titleTmp.text, "TitleText text must not be empty");

        // Verify stroke material
        Assert.IsNotNull(subTmp.fontSharedMaterial, "SubtitleText must have fontSharedMaterial");
        Assert.IsNotNull(titleTmp.fontSharedMaterial, "TitleText must have fontSharedMaterial");
        Assert.IsTrue(subTmp.fontSharedMaterial.name.Contains("Stroke"), "SubtitleText must use Stroke material");
        Assert.IsTrue(titleTmp.fontSharedMaterial.name.Contains("Stroke"), "TitleText must use Stroke material");
    }
}
#endif
