using NUnit.Framework;
using TMPro;
using UnityEngine;

[TestFixture]
public class LocalizationSystemTests
{
    private string originalLanguage;

    [SetUp]
    public void SetUp()
    {
        originalLanguage = GameSettings.Language;
    }

    [TearDown]
    public void TearDown()
    {
        GameSettings.Language = originalLanguage;
    }

    [Test]
    public void PGELocalization_Get_ReturnsCorrectLanguage()
    {
        GameSettings.Language = "English";
        Assert.That(PGELocalization.IsVietnamese, Is.False);
        Assert.That(PGELocalization.Get("common.confirm"), Is.EqualTo("Confirm"));
        Assert.That(PGELocalization.Get("settings.title"), Is.EqualTo("SETTINGS"));

        GameSettings.Language = "Tiếng Việt";
        Assert.That(PGELocalization.IsVietnamese, Is.True);
        Assert.That(PGELocalization.Get("common.confirm"), Is.EqualTo("Xác nhận"));
        Assert.That(PGELocalization.Get("settings.title"), Is.EqualTo("CÀI ĐẶT"));
    }

    [Test]
    public void PGELocalization_GetFormat_InterpolatesArgumentsCorrectly()
    {
        GameSettings.Language = "English";
        string formattedEn = PGELocalization.GetFormat("chapter.waves_count", 3, 10);
        Assert.That(formattedEn, Is.EqualTo("3 / 10 WAVES"));

        GameSettings.Language = "Tiếng Việt";
        string formattedVi = PGELocalization.GetFormat("chapter.waves_count", 3, 10);
        Assert.That(formattedVi, Is.EqualTo("3 / 10 ĐỢT"));
    }

    [Test]
    public void PGELocalization_Fallback_ReturnedWhenKeyNotFound()
    {
        string result = PGELocalization.Get("unknown.fake.key.123", "Fallback Value");
        Assert.That(result, Is.EqualTo("Fallback Value"));
    }

    [Test]
    public void PGELocalization_EventFires_WhenGameSettingsLanguageChanges()
    {
        int firedCount = 0;
        void Handler() => firedCount++;

        PGELocalization.OnLanguageChanged += Handler;
        try
        {
            GameSettings.Language = "English";
            GameSettings.Language = "Tiếng Việt";
            Assert.That(firedCount, Is.GreaterThanOrEqualTo(1));
        }
        finally
        {
            PGELocalization.OnLanguageChanged -= Handler;
        }
    }

    [Test]
    public void PGELocalizedText_AutoUpdates_WhenLanguageSwitches()
    {
        GameObject go = new GameObject("LocalizedLabelTest", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(PGELocalizedText));
        try
        {
            GameSettings.Language = "English";
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            PGELocalizedText locText = go.GetComponent<PGELocalizedText>();
            locText.LocalizationKey = "settings.title";
            locText.Refresh();

            Assert.That(tmp.text, Is.EqualTo("SETTINGS"));

            GameSettings.Language = "Tiếng Việt";
            Assert.That(tmp.text, Is.EqualTo("CÀI ĐẶT"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GameSettings_NormalizeLanguage_HandlesVariants()
    {
        Assert.That(GameSettings.NormalizeLanguage("Tiếng Việt"), Is.EqualTo("Tiếng Việt"));
        Assert.That(GameSettings.NormalizeLanguage("Vietnamese"), Is.EqualTo("Tiếng Việt"));
        Assert.That(GameSettings.NormalizeLanguage("vi"), Is.EqualTo("Tiếng Việt"));
        Assert.That(GameSettings.NormalizeLanguage("English"), Is.EqualTo("English"));
        Assert.That(GameSettings.NormalizeLanguage("Russian"), Is.EqualTo("Russian"));
        Assert.That(GameSettings.NormalizeLanguage("Chinese"), Is.EqualTo("Chinese"));
        Assert.That(GameSettings.NormalizeLanguage("UnknownLang"), Is.EqualTo("English"));
    }

    [Test]
    public void GameSettings_GetLanguageDisplayName_ReturnsNativeNames()
    {
        Assert.That(GameSettings.GetLanguageDisplayName("English"), Is.EqualTo("English"));
        Assert.That(GameSettings.GetLanguageDisplayName("Tiếng Việt"), Is.EqualTo("Tiếng Việt"));
        Assert.That(GameSettings.GetLanguageDisplayName("Vietnamese"), Is.EqualTo("Tiếng Việt"));
        Assert.That(GameSettings.GetLanguageDisplayName("Chinese"), Is.EqualTo("Chinese"));
        Assert.That(GameSettings.GetLanguageDisplayName("Russian"), Does.Contain("Русский"));
    }

    [Test]
    public void SettingsPanel_LanguageOptionsPanel_CreatesHeaderAndHighlightsActive()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            GameSettings.Language = "Tiếng Việt";
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());
            panel.Open();

            panel.ToggleLanguageOptions();

            Transform options = panel.transform.Find("SafeContent/LanguageOptionsPanel");
            Assert.That(options, Is.Not.Null);
            Assert.That(options.gameObject.activeSelf, Is.True);

            Transform viBtn = options.Find("VietnameseButton");
            Assert.That(viBtn, Is.Not.Null);
            TMP_Text viText = viBtn.GetComponentInChildren<TMP_Text>(true);
            Assert.That(viText.text, Does.Contain("•"));
            Assert.That(viText.text, Does.Contain("Tiếng Việt"));

            // Switch to English via EnglishButton
            Transform enBtn = options.Find("EnglishButton");
            Assert.That(enBtn, Is.Not.Null);
            enBtn.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

            Assert.That(GameSettings.Language, Is.EqualTo("English"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Chipset_NoticeBox_TranslatesToVietnameseAndReverts()
    {
        GameSettings.Language = "English";
        Assert.That(PGELocalization.Get("chipset.not_enough_chips"), Is.EqualTo("Not enough Data Chips"));
        Assert.That(PGELocalization.GetChipName("Standard Gun"), Is.EqualTo("Standard Gun"));
        Assert.That(PGELocalization.Get("chipset.mod_badge"), Does.Contain("MOD • UP TO"));

        GameSettings.Language = "Tiếng Việt";
        Assert.That(PGELocalization.Get("chipset.not_enough_chips"), Is.EqualTo("Không đủ Chip Dữ Liệu"));
        Assert.That(PGELocalization.GetChipName("Standard Gun"), Is.EqualTo("Súng Tiêu Chuẩn"));
        Assert.That(PGELocalization.Get("chipset.mod_badge"), Does.Contain("MOD • TỐI ĐA"));

        // Revert back to English
        GameSettings.Language = "English";
        Assert.That(PGELocalization.Get("chipset.not_enough_chips"), Is.EqualTo("Not enough Data Chips"));
        Assert.That(PGELocalization.GetChipName("Standard Gun"), Is.EqualTo("Standard Gun"));
    }

    [Test]
    public void Shop_GetShopMessage_TranslatesAccurately()
    {
        GameSettings.Language = "English";
        Assert.That(PGELocalization.GetShopMessage("DAILY SHOP READY"), Is.EqualTo("DAILY SHOP READY"));
        Assert.That(PGELocalization.GetShopMessage("NOT ENOUGH RED GEMS"), Is.EqualTo("NOT ENOUGH RED GEMS"));
        Assert.That(PGELocalization.GetShopMessage("VIP UNLOCKED • 10,000 GEMS RECEIVED"), Is.EqualTo("VIP UNLOCKED • 10,000 GEMS RECEIVED"));

        GameSettings.Language = "Tiếng Việt";
        Assert.That(PGELocalization.GetShopMessage("DAILY SHOP READY"), Is.EqualTo("CỬA HÀNG ĐÃ SẴN SÀNG"));
        Assert.That(PGELocalization.GetShopMessage("NOT ENOUGH RED GEMS"), Is.EqualTo("KHÔNG ĐỦ NGỌC"));
        Assert.That(PGELocalization.GetShopMessage("VIP UNLOCKED • 10,000 GEMS RECEIVED"), Is.EqualTo("ĐÃ MỞ KHÓA VIP • 10,000 NGỌC ĐÃ NHẬN"));

        GameSettings.Language = "English";
        Assert.That(PGELocalization.GetShopMessage("DAILY SHOP READY"), Is.EqualTo("DAILY SHOP READY"));
    }

    [Test]
    public void BottomNav_EnsureLocalizedLabels_TogglesVisibility()
    {
        GameObject navObj = new GameObject("BottomNavTest", typeof(RectTransform), typeof(BottomNavigationController));
        try
        {
            BottomNavigationController nav = navObj.GetComponent<BottomNavigationController>();
            GameObject btnObj = new GameObject("BtnShop", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            btnObj.transform.SetParent(navObj.transform, false);

            nav.SetItemsForTesting(new[]
            {
                new BottomNavigationController.NavigationItem
                {
                    name = "Shop",
                    button = btnObj.GetComponent<UnityEngine.UI.Button>()
                }
            });

            GameSettings.Language = "Tiếng Việt";
            nav.EnsureLocalizedLabels();
            Assert.That(nav.Items[0].label, Is.Not.Null);
            Assert.That(nav.Items[0].label.gameObject.activeSelf, Is.True);
            Assert.That(nav.Items[0].label.text, Is.EqualTo("Cửa hàng"));

            GameSettings.Language = "English";
            nav.EnsureLocalizedLabels();
            Assert.That(nav.Items[0].label.gameObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(navObj);
        }
    }
}

