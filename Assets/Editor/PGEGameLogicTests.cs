using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PGEGameLogicTests
{
    [Test]
    public void LabUpgrade_KeyGeneration_IsConsistent()
    {
        string key1 = LabUpgradeController.GetItemLevelKey("ATK", 1);
        string key2 = LabUpgradeController.GetItemLevelKey("atk", 1);
        string key3 = LabUpgradeController.GetItemLevelKey("DEF", 0);

        Assert.That(key1, Is.EqualTo("PGE.Lab.ItemLevel.ATK"));
        Assert.That(key2, Is.EqualTo("PGE.Lab.ItemLevel.ATK"));
        Assert.That(key3, Is.EqualTo("PGE.Lab.ItemLevel.DEF"));
    }

    [Test]
    public void PlayerStatsManager_GetStatLevel_ReadsCorrectly()
    {
        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.HP", 5);
        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.SPD", 3);
        PlayerPrefs.Save();

        int hpLevel = PlayerStatsManager.GetStatLevel("HP");
        int spdLevel = PlayerStatsManager.GetStatLevel("SPD");

        Assert.That(hpLevel, Is.EqualTo(5));
        Assert.That(spdLevel, Is.EqualTo(3));
    }

    [Test]
    public void PlayerHealth_DamageReduction_ReducesDamageCorrectly()
    {
        GameObject go = new GameObject("PlayerHealthTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        health.SetDamageReduction(5);
        int initialHp = health.CurrentHealth;

        health.TakeDamage(12);
        // Effective damage should be 12 - 5 = 7
        Assert.That(health.CurrentHealth, Is.EqualTo(initialHp - 7));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PlayerHealth_LethalDamage_TriggersDeathAndDoesNotHealToFull()
    {
        GameObject go = new GameObject("PlayerLethalHealthTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        bool deathTriggered = false;
        health.OnPlayerDeath += () => deathTriggered = true;

        // Deal fatal damage
        health.TakeDamage(150);

        Assert.That(health.CurrentHealth, Is.EqualTo(0), "Máu sau khi nhận sát thương chí tử phải bằng 0.");
        Assert.That(health.IsDead, Is.True, "Player phải ở trạng thái IsDead = true.");
        Assert.That(deathTriggered, Is.True, "Sự kiện OnPlayerDeath phải được phát khi máu về 0.");

        // Subsequent damage or heals must not revive the player
        health.TakeDamage(50);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));

        health.Heal(50);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ObjectPool_DoubleReturn_DoesNotDuplicateInQueue()
    {
        GameObject prefab = new GameObject("PoolTestPrefab");
        GameObject container = new GameObject("PoolContainer");

        ObjectPool pool = new ObjectPool(prefab, 1, false, container.transform);
        pool.Initialize(container.transform);

        GameObject instance = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(instance, Is.Not.Null);

        // Return first time
        pool.Return(instance);

        // Return second time (attempt double enqueue)
        pool.Return(instance);

        // Get instance once
        GameObject firstGet = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(firstGet, Is.EqualTo(instance));

        // Get instance second time (should be null since pool has size 1 and canGrow = false)
        GameObject secondGet = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(secondGet, Is.Null);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(container);
    }

    [Test]
    public void PlayerDeathController_TriggersDeath_DisablesMovementAndInvokesEvents()
    {
        GameObject playerGo = new GameObject("PlayerTest");
        PlayerHealth health = playerGo.AddComponent<PlayerHealth>();
        PlayerMovement movement = playerGo.AddComponent<PlayerMovement>();
        PlayerDeathController deathCtrl = playerGo.AddComponent<PlayerDeathController>();

        bool deathStartedInvoked = false;
        deathCtrl.OnDeathStarted += () => { deathStartedInvoked = true; };

        deathCtrl.TriggerDeath();

        Assert.That(deathCtrl.IsDeathSequenceActive, Is.True);
        Assert.That(deathStartedInvoked, Is.True);
        Assert.That(movement.enabled, Is.False);

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void DieAnimation_HasNoRootPositionCurves_ToPreventAnimatorTransformLock()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/Player/Die.anim");
        Assert.That(clip, Is.Not.Null);

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (string.IsNullOrEmpty(binding.path) && binding.propertyName.StartsWith("m_LocalPosition"))
            {
                Assert.Fail($"Die.anim contains root position curve: {binding.propertyName}, which locks Player movement in Animator!");
            }
        }
    }

    [Test]
    public void TargetFrameRate_IsConfiguredForSmoothGameplay()
    {
        PlayerDataService.InitializeApplicationSettings();
        Assert.That(Application.targetFrameRate, Is.GreaterThanOrEqualTo(60));
    }

    [Test]
    public void AAAGoldenStarParticleDissolveShader_LoadsAndContainsRequiredProperties()
    {
        Shader shader = Shader.Find("Custom/2D/SpriteDissolve");
        Assert.That(shader, Is.Not.Null, "Custom/2D/SpriteDissolve shader must exist in the project.");

        Material mat = new Material(shader);
        Assert.That(mat.HasProperty("_DissolveAmount"), Is.True);
        Assert.That(mat.HasProperty("_DissolveDirectionMode"), Is.True);
        Assert.That(mat.HasProperty("_ParticleShapeMode"), Is.True);
        Assert.That(mat.HasProperty("_ParticleGridSize"), Is.True);
        Assert.That(mat.HasProperty("_DisperseSpeed"), Is.True);
        Assert.That(mat.HasProperty("_RadialBurstSpread"), Is.True);
        Assert.That(mat.HasProperty("_UpwardDrift"), Is.True);
        Assert.That(mat.HasProperty("_SwirlStrength"), Is.True);
        Assert.That(mat.HasProperty("_DisperseChaos"), Is.True);
        Assert.That(mat.HasProperty("_ParticleShrink"), Is.True);
        Assert.That(mat.HasProperty("_Gravity"), Is.True);
        Assert.That(mat.HasProperty("_EdgeColor"), Is.True);
        Assert.That(mat.HasProperty("_InnerEdgeColor"), Is.True);
        Assert.That(mat.HasProperty("_EdgeIntensity"), Is.True);
        Assert.That(mat.HasProperty("_SupernovaFlash"), Is.True);
        Assert.That(mat.HasProperty("_StarSparkleSpeed"), Is.True);
        Assert.That(mat.HasProperty("_PrismaticShimmer"), Is.True);
        Assert.That(mat.HasProperty("_HaloGlowIntensity"), Is.True);
        Assert.That(mat.HasProperty("_SpriteUVRect"), Is.True);
        Object.DestroyImmediate(mat);
    }

    [Test]
    public void PlayerDataService_CurrencyManagement_WorksCorrectly()
    {
        PlayerDataService.DataChips = 5000;
        PlayerDataService.RedGems = 1000;

        Assert.That(PlayerDataService.HasEnoughDataChips(3000), Is.True);
        Assert.That(PlayerDataService.TrySpendDataChips(2000), Is.True);
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(3000));

        PlayerDataService.AddDataChips(1500);
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(4500));

        Assert.That(PlayerDataService.HasEnoughRedGems(1500), Is.False);
        Assert.That(PlayerDataService.TrySpendRedGems(500), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(500));
    }

    [Test]
    public void ChipManager_TestMode_ProvidesUnlimitedChips()
    {
        ChipManager.IsTestMode = false;
        ChipManager.DataChips = 100;
        Assert.That(ChipManager.HasEnoughDataChips(200), Is.False);

        // Turn on Test Mode
        ChipManager.IsTestMode = true;
        Assert.That(ChipManager.IsTestMode, Is.True);
        Assert.That(ChipManager.HasEnoughDataChips(999999), Is.True);
        Assert.That(ChipManager.TrySpendDataChips(500000), Is.True);

        // Turn off Test Mode
        ChipManager.IsTestMode = false;
        Assert.That(ChipManager.DataChips, Is.EqualTo(100));
    }

    [Test]
    public void PlayerDataService_SelectedWeaponId_FallbackAndSet_WorksCorrectly()
    {
        string original = PlayerPrefs.GetString(PlayerDataService.SelectedWeaponIdKey, "blaster");
        string changedWeapon = null;
        System.Action<string> handler = id => changedWeapon = id;
        PlayerDataService.OnSelectedWeaponChanged += handler;

        try
        {
            // Null or whitespace fallback to "blaster"
            PlayerDataService.SelectedWeaponId = null;
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));
            Assert.That(changedWeapon, Is.EqualTo("blaster"));

            PlayerDataService.SelectedWeaponId = "   ";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));

            // Custom weapon ID
            PlayerDataService.SelectedWeaponId = "laser_blaster";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("laser_blaster"));
            Assert.That(changedWeapon, Is.EqualTo("laser_blaster"));
        }
        finally
        {
            PlayerDataService.OnSelectedWeaponChanged -= handler;
            PlayerDataService.SelectedWeaponId = original;
        }
    }

    [Test]
    public void CameraFollow_ZeroOrNegativeSpeed_SnapsImmediatelyToDesiredPosition()
    {
        GameObject cameraGo = new GameObject("CameraTest");
        GameObject targetGo = new GameObject("TargetTest");
        targetGo.transform.position = new Vector3(100f, 200f, 0f);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        CameraFollow follow = cameraGo.AddComponent<CameraFollow>();
        follow.SetTarget(targetGo.transform);
        follow.FollowSpeed = 0f;
        follow.Offset = new Vector2(5f, -5f);

        // Update follow with followSpeed = 0
        follow.UpdateFollow(0.016f);

        Assert.That(cameraGo.transform.position.x, Is.EqualTo(105f));
        Assert.That(cameraGo.transform.position.y, Is.EqualTo(195f));
        Assert.That(cameraGo.transform.position.z, Is.EqualTo(-10f));

        Object.DestroyImmediate(cameraGo);
        Object.DestroyImmediate(targetGo);
    }

    [Test]
    public void ShopController_VNDOffer_FailsClosedAndGrantsNoRewards()
    {
        GameObject shopGo = new GameObject("ShopTest");
        ShopController shop = shopGo.AddComponent<ShopController>();

        int initialGems = ChipManager.RedGems;
        ShopController.Offer vndOffer = new ShopController.Offer
        {
            id = "vnd-pack-1",
            displayName = "1000 RED GEMS (VND)",
            currency = ShopController.CurrencyType.VND,
            price = 50000,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 1000
        };

        shop.SetOffersForTesting(new[] { vndOffer });
        bool result = shop.TryPurchase(0);

        Assert.That(result, Is.False, "Purchases with CurrencyType.VND must fail-closed.");
        Assert.That(ChipManager.RedGems, Is.EqualTo(initialGems), "No rewards must be granted for VND offers without payment integration.");

        Object.DestroyImmediate(shopGo);
    }

    [Test]
    public void MainMenu_Architecture_SingleChapterPanelUnderCanvasContent()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Assert.That(File.Exists(scenePath), Is.True, "MainMenu.unity must exist.");

        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Assert.That(canvas, Is.Not.Null, "Canvas must exist in MainMenu.unity.");

        Transform contentTr = canvas.transform.Find("Content");
        Assert.That(contentTr, Is.Not.Null, "Canvas/Content must exist in MainMenu.unity.");

        Transform contentChapterPanel = contentTr.Find("ChapterPanel");
        Assert.That(contentChapterPanel, Is.Not.Null, "Canvas/Content/ChapterPanel must exist.");

        Transform rootChapterPanel = canvas.transform.Find("ChapterPanel");
        Assert.That(rootChapterPanel, Is.Null, "Duplicate Canvas/ChapterPanel must NOT exist.");

        // Count all ChapterPanel objects in scene
        int count = 0;
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            foreach (var transform in rootGo.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == "ChapterPanel")
                {
                    count++;
                }
            }
        }
        Assert.That(count, Is.EqualTo(1), "There must be exactly ONE ChapterPanel in MainMenu.unity.");
    }

    [Test]
    public void MainMenu_BottomNavigation_ChapterItem_PointsToContentChapterPanel()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Transform contentChapterPanel = canvas.transform.Find("Content/ChapterPanel");
        Assert.That(contentChapterPanel, Is.Not.Null);

        BottomNavigationController bottomNav = Object.FindObjectOfType<BottomNavigationController>();
        Assert.That(bottomNav, Is.Not.Null, "BottomNavigationController must exist.");

        SerializedObject navSO = new SerializedObject(bottomNav);
        SerializedProperty itemsProp = navSO.FindProperty("items");
        Assert.That(itemsProp.arraySize, Is.GreaterThanOrEqualTo(3));

        SerializedProperty chapterItem = itemsProp.GetArrayElementAtIndex(2);
        GameObject boundPanel = chapterItem.FindPropertyRelative("panel").objectReferenceValue as GameObject;
        Assert.That(boundPanel, Is.EqualTo(contentChapterPanel.gameObject), "BottomNavigation items[2].panel must point to Canvas/Content/ChapterPanel.");
    }

    [Test]
    public void MainMenu_ShopPanel_HasFunctionalShopControllerAndOffers()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Transform content = canvas.transform.Find("Content");
        Assert.That(content.GetComponent<ContentSizeFitter>(), Is.Null, "Canvas/Content must not resize full-screen tab panels.");
        Assert.That(content.GetComponent<GridLayoutGroup>(), Is.Null, "Canvas/Content must not lay out full-screen tab panels as grid cells.");

        Transform shopPanel = canvas.transform.Find("Content/ShopPanel");
        Assert.That(shopPanel, Is.Not.Null, "Canvas/Content/ShopPanel must exist.");
        Assert.That(shopPanel.GetComponent<ScrollRect>(), Is.Not.Null, "ShopPanel must be scrollable.");

        ShopController shop = shopPanel.GetComponent<ShopController>();
        Assert.That(shop, Is.Not.Null, "ShopPanel must use ShopController instead of a coming-soon placeholder.");

        SerializedObject shopSO = new SerializedObject(shop);
        Assert.That(shopSO.FindProperty("offers").arraySize, Is.EqualTo(7), "ShopPanel must expose all seven configured offers.");
    }

    [Test]
    public void MainMenu_DefaultNavigationItem_IsTheOnlyActiveContentPanel()
    {
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        BottomNavigationController bottomNav = Object.FindObjectOfType<BottomNavigationController>();
        Assert.That(bottomNav, Is.Not.Null);

        SerializedObject navSO = new SerializedObject(bottomNav);
        SerializedProperty items = navSO.FindProperty("items");
        int defaultSelectedIndex = navSO.FindProperty("defaultSelectedIndex").intValue;
        Color normalBackground = navSO.FindProperty("normalColor").colorValue;
        Color selectedBackground = navSO.FindProperty("selectedColor").colorValue;
        Color normalBorder = navSO.FindProperty("normalBorderColor").colorValue;
        Color selectedBorder = navSO.FindProperty("selectedBorderColor").colorValue;
        Color normalContent = navSO.FindProperty("normalContentColor").colorValue;
        Color selectedContent = navSO.FindProperty("selectedContentColor").colorValue;
        Assert.That(defaultSelectedIndex, Is.InRange(0, items.arraySize - 1));

        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            bool selected = i == defaultSelectedIndex;
            GameObject panel = item.FindPropertyRelative("panel").objectReferenceValue as GameObject;
            Assert.That(panel, Is.Not.Null, $"Navigation item {i} must reference a panel.");
            Assert.That(panel.activeSelf, Is.EqualTo(selected), "Exactly the configured default tab must be active in the saved scene.");

            Image background = item.FindPropertyRelative("background").objectReferenceValue as Image;
            Image icon = item.FindPropertyRelative("icon").objectReferenceValue as Image;
            TMP_Text label = item.FindPropertyRelative("label").objectReferenceValue as TMP_Text;
            Button button = item.FindPropertyRelative("button").objectReferenceValue as Button;
            Image border = item.FindPropertyRelative("border").objectReferenceValue as Image ?? button.GetComponent<Image>();

            Assert.That(background.color, Is.EqualTo(selected ? selectedBackground : normalBackground));
            Assert.That(border.color, Is.EqualTo(selected ? selectedBorder : normalBorder));
            Assert.That(icon.color, Is.EqualTo(selected ? selectedContent : normalContent));
            Assert.That(label.color, Is.EqualTo(selected ? selectedContent : normalContent));
        }
    }

    [TestCase(0f, 1f)]
    [TestCase(90f, 1f)]
    [TestCase(180f, -1f)]
    [TestCase(-90f, 1f)]
    public void CalculateAimScale_FlipsOnlyYAxis(float angle, float expectedYSign)
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateAimScale",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 3f, 4f);
        Vector3 result = (Vector3)method.Invoke(null, new object[] { angle, baseScale });

        Assert.That(result.x, Is.EqualTo(baseScale.x));
        Assert.That(result.y, Is.EqualTo(Mathf.Abs(baseScale.y) * expectedYSign));
        Assert.That(result.z, Is.EqualTo(baseScale.z));
    }

    [TestCase(false, 2f)]
    [TestCase(true, -2f)]
    public void CalculateBodyScale_MirrorsOnlyXAxis(
        bool isAimingLeft,
        float expectedX
    )
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateBodyScale",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 3f, 4f);
        Vector3 result = (Vector3)method.Invoke(
            null,
            new object[] { isAimingLeft, baseScale }
        );

        Assert.That(result.x, Is.EqualTo(expectedX));
        Assert.That(result.y, Is.EqualTo(baseScale.y));
        Assert.That(result.z, Is.EqualTo(baseScale.z));
    }

    [TestCase(0f, false, 0f)]
    [TestCase(90f, false, 90f)]
    [TestCase(180f, true, 0f)]
    [TestCase(135f, true, 45f)]
    [TestCase(-135f, true, -45f)]
    public void CalculateLocalAimAngle_CompensatesBodyMirror(
        float worldAngle,
        bool isAimingLeft,
        float expectedLocalAngle
    )
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateLocalAimAngle",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        float result = (float)method.Invoke(
            null,
            new object[] { worldAngle, isAimingLeft }
        );

        Assert.That(Mathf.DeltaAngle(expectedLocalAngle, result), Is.EqualTo(0f));
    }

    [Test]
    public void MainMenu_LabAtlasSpriteReferences_HaveValidLocalIds()
    {
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        const string atlasPath = "Assets/UI/Lab/Generated/lab-icon-atlas.png";
        string atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
        Assert.That(atlasGuid, Is.Not.Empty);

        var validLocalIds = new HashSet<long>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(atlasPath))
        {
            if (!(asset is Sprite sprite)) continue;

            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string spriteGuid, out long localId), Is.True);
            Assert.That(spriteGuid, Is.EqualTo(atlasGuid));
            validLocalIds.Add(localId);
        }

        MatchCollection references = Regex.Matches(
            File.ReadAllText(scenePath),
            @"fileID:\s*(-?\d+), guid:\s*" + atlasGuid);
        Assert.That(references.Count, Is.GreaterThan(0));

        var missingLocalIds = new HashSet<long>();
        foreach (Match reference in references)
        {
            long localId = long.Parse(reference.Groups[1].Value);
            if (!validLocalIds.Contains(localId)) missingLocalIds.Add(localId);
        }

        Assert.That(missingLocalIds, Is.Empty, "Every Lab atlas sprite reference in MainMenu must resolve to a current sprite local ID.");
    }

    [Test]
    public void MainMenu_ChapterPanel_ImagesHaveNonNullSprites()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Transform chapterPanel = canvas.transform.Find("Content/ChapterPanel");
        Assert.That(chapterPanel, Is.Not.Null);

        // Verify key sprites in ChapterPanel
        Transform stageBg = chapterPanel.Find("StagePreviewWindow/Viewport/StageBackground");
        Assert.That(stageBg, Is.Not.Null);
        Image stageBgImg = stageBg.GetComponent<Image>();
        Assert.That(stageBgImg.sprite, Is.Not.Null, "StageBackground must have a valid sprite assigned.");

        Transform boss = chapterPanel.Find("StagePreviewWindow/Viewport/BossSilhouette");
        Assert.That(boss, Is.Not.Null);
        Image bossImg = boss.GetComponent<Image>();
        Assert.That(bossImg, Is.Not.Null);
        Assert.That(bossImg.sprite, Is.Not.Null, "BossSilhouette must have a valid sprite assigned.");

        Transform rewardIcon = chapterPanel.Find("SubWidgetsContainer/QuestWidget/RewardBox/RewardIcon");
        Assert.That(rewardIcon, Is.Not.Null);
        Image rewardImg = rewardIcon.GetComponent<Image>();
        Assert.That(rewardImg, Is.Not.Null);
        Assert.That(rewardImg.sprite, Is.Not.Null, "RewardIcon must have a valid sprite assigned.");
    }

    [Test]
    public void ChapterDatabase_AllChapters_HaveValidPreviewAndBossSprites()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
        Assert.That(db, Is.Not.Null);
        Assert.That(db.Count, Is.GreaterThanOrEqualTo(4));

        for (int i = 0; i < db.Count; i++)
        {
            ChapterData chapter = db.GetChapter(i);
            Assert.That(chapter, Is.Not.Null, $"Chapter at index {i} must exist.");
            Assert.That(chapter.previewBackground, Is.Not.Null, $"Chapter {chapter.chapterNumber} must have previewBackground assigned.");
            Assert.That(chapter.bossSilhouette, Is.Not.Null, $"Chapter {chapter.chapterNumber} must have bossSilhouette assigned.");
        }
    }

    [Test]
    public void PauseModalController_OpenAndClose_TogglesTimeScaleAndActiveState()
    {
        GameObject modalRoot = new GameObject("PauseModalRoot");
        GameObject statsPnl = new GameObject("StatsPanel");
        GameObject chipPnl = new GameObject("ChipPanel");
        GameObject artPnl = new GameObject("ArtPanel");
        GameObject defPnl = new GameObject("DefPanel");
        GameObject atkPnl = new GameObject("AtkPanel");
        GameObject othPnl = new GameObject("OthPanel");

        GameObject resumeBtnGo = new GameObject("ResumeBtn");
        Button resumeBtn = resumeBtnGo.AddComponent<Button>();

        GameObject homeBtnGo = new GameObject("HomeBtn");
        Button homeBtn = homeBtnGo.AddComponent<Button>();

        GameObject hpTxtGo = new GameObject("HpText");
        TMP_Text hpTxt = hpTxtGo.AddComponent<TextMeshProUGUI>();

        GameObject defTxtGo = new GameObject("DefText");
        TMP_Text defTxt = defTxtGo.AddComponent<TextMeshProUGUI>();

        GameObject lvlTxtGo = new GameObject("LvlText");
        TMP_Text lvlTxt = lvlTxtGo.AddComponent<TextMeshProUGUI>();

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, resumeBtn, homeBtn,
            null, null, null,
            statsPnl, chipPnl, artPnl,
            null, null, null,
            defPnl, atkPnl, othPnl,
            hpTxt, defTxt, lvlTxt
        );

        float originalTimeScale = Time.timeScale;
        try
        {
            pauseCtrl.OpenPauseModal();
            Assert.That(pauseCtrl.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(modalRoot.activeSelf, Is.True);

            pauseCtrl.ResumeGame();
            Assert.That(pauseCtrl.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(modalRoot.activeSelf, Is.False);
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            Object.DestroyImmediate(modalRoot);
            Object.DestroyImmediate(statsPnl);
            Object.DestroyImmediate(chipPnl);
            Object.DestroyImmediate(artPnl);
            Object.DestroyImmediate(defPnl);
            Object.DestroyImmediate(atkPnl);
            Object.DestroyImmediate(othPnl);
            Object.DestroyImmediate(resumeBtnGo);
            Object.DestroyImmediate(homeBtnGo);
            Object.DestroyImmediate(hpTxtGo);
            Object.DestroyImmediate(defTxtGo);
            Object.DestroyImmediate(lvlTxtGo);
        }
    }

    [Test]
    public void PauseModalController_TabSwitching_UpdatesPanels()
    {
        GameObject modalRoot = new GameObject("PauseModalRoot");
        GameObject statsPnl = new GameObject("StatsPanel");
        GameObject chipPnl = new GameObject("ChipPanel");
        GameObject artPnl = new GameObject("ArtPanel");
        GameObject defPnl = new GameObject("DefPanel");
        GameObject atkPnl = new GameObject("AtkPanel");
        GameObject othPnl = new GameObject("OthPanel");

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, null, null,
            null, null, null,
            statsPnl, chipPnl, artPnl,
            null, null, null,
            defPnl, atkPnl, othPnl,
            null, null, null
        );

        pauseCtrl.SelectMainTab(1); // CHIPSET
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(1));
        Assert.That(statsPnl.activeSelf, Is.False);
        Assert.That(chipPnl.activeSelf, Is.True);
        Assert.That(artPnl.activeSelf, Is.False);

        pauseCtrl.SelectMainTab(2); // ARTIFACT
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(2));
        Assert.That(statsPnl.activeSelf, Is.False);
        Assert.That(chipPnl.activeSelf, Is.False);
        Assert.That(artPnl.activeSelf, Is.True);

        pauseCtrl.SelectMainTab(0); // STATS
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(0));
        Assert.That(statsPnl.activeSelf, Is.True);
        Assert.That(chipPnl.activeSelf, Is.False);
        Assert.That(artPnl.activeSelf, Is.False);

        // Sub-Tabs
        pauseCtrl.SelectSubTab(1); // Attack
        Assert.That(pauseCtrl.CurrentSubTab, Is.EqualTo(1));
        Assert.That(defPnl.activeSelf, Is.False);
        Assert.That(atkPnl.activeSelf, Is.True);
        Assert.That(othPnl.activeSelf, Is.False);

        pauseCtrl.SelectSubTab(2); // Other
        Assert.That(pauseCtrl.CurrentSubTab, Is.EqualTo(2));
        Assert.That(defPnl.activeSelf, Is.False);
        Assert.That(atkPnl.activeSelf, Is.False);
        Assert.That(othPnl.activeSelf, Is.True);

        Object.DestroyImmediate(modalRoot);
        Object.DestroyImmediate(statsPnl);
        Object.DestroyImmediate(chipPnl);
        Object.DestroyImmediate(artPnl);
        Object.DestroyImmediate(defPnl);
        Object.DestroyImmediate(atkPnl);
        Object.DestroyImmediate(othPnl);
    }

    [Test]
    public void PauseModalController_HomeButton_OpensQuitConfirmModal_AndNoCancels()
    {
        GameObject modalRoot = new GameObject("PauseModalRoot");
        GameObject confirmPnl = new GameObject("QuitConfirmDialog");

        GameObject homeBtnGo = new GameObject("HomeBtn");
        Button homeBtn = homeBtnGo.AddComponent<Button>();

        GameObject noBtnGo = new GameObject("NoBtn");
        Button noBtn = noBtnGo.AddComponent<Button>();

        GameObject okBtnGo = new GameObject("OkBtn");
        Button okBtn = okBtnGo.AddComponent<Button>();

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, null, homeBtn,
            null, null, null,
            null, null, null,
            null, null, null,
            null, null, null,
            null, null, null,
            confirmPnl, noBtn, okBtn
        );

        confirmPnl.SetActive(false);

        // Click Home -> Confirmation dialog opens
        pauseCtrl.OnHomeButtonClicked();
        Assert.That(confirmPnl.activeSelf, Is.True);

        // Click No -> Confirmation dialog closes
        pauseCtrl.OnConfirmNoClicked();
        Assert.That(confirmPnl.activeSelf, Is.False);

        Object.DestroyImmediate(modalRoot);
        Object.DestroyImmediate(confirmPnl);
        Object.DestroyImmediate(homeBtnGo);
        Object.DestroyImmediate(noBtnGo);
        Object.DestroyImmediate(okBtnGo);
    }

    [Test]
    public void Chipset_All24Chips_AreProperlyConfigured()
    {
        GameObject go = new GameObject("ChipsetControllerTest");
        ChipsetController controller = go.AddComponent<ChipsetController>();
        controller.InitializeDatabase();

        Assert.That(controller.AllChips.Count, Is.EqualTo(24), "Database phải chứa đúng 24 loại chip set.");

        string[] expectedNames = {
            "Standard Gun", "Rifle", "Rocket Punch", "Spinning Blade", "Multigun",
            "Gun Turret", "Spiky Discus", "Shotgun", "Energy Jumper Cables", "High-Explosive Mine",
            "Aiming Lens", "Plasma Field", "Laser Eye", "Biochemical Mine", "Tesla Coil",
            "ATK Module", "Black Hole Mine", "Sonic Boom", "Big Battery", "Turret Module",
            "Ice Turret", "Invincible Shield", "Healing Turret", "Flamethrower"
        };

        for (int i = 0; i < 24; i++)
        {
            var chip = controller.AllChips[i];
            Assert.That(chip.id, Is.EqualTo(i + 1));
            Assert.That(chip.chipName, Is.EqualTo(expectedNames[i]));
            Assert.That(string.IsNullOrWhiteSpace(chip.iconKey), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(chip.baseStatsSummary), Is.False, $"Base stats của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.magicBonus), Is.False, $"Magic bonus của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.rareBonus), Is.False, $"Rare bonus của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.uniqueBonus), Is.False, $"Unique bonus của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.epicBonus), Is.False, $"Epic bonus của {chip.chipName} không được rỗng.");
        }

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_TierLevelCaps_FollowProgressionRules()
    {
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Magic), Is.EqualTo(6), "Tier 1 (Magic) max level phải là 6.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Rare), Is.EqualTo(9), "Tier 2 (Rare) max level phải là 9.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Unique), Is.EqualTo(14), "Tier 3 (Unique) max level phải là 14.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Epic), Is.EqualTo(18), "Tier 4 (Epic) max level phải là 18.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Holographic), Is.EqualTo(24), "Advance Tier (Tier 5 / Holographic) max level phải là 24.");
    }

    [Test]
    public void Chipset_AdvanceTier_Requires10Stones_ToReachAdvanceTier()
    {
        ChipItemData epicChip = new ChipItemData
        {
            id = 1,
            chipName = "Standard Gun",
            tier = ChipTier.Epic,
            level = 18,
            count = 10,
            requiredCount = 0
        };

        Assert.That(epicChip.IsAtTierCap, Is.True);
        Assert.That(epicChip.CanAdvanceTier, Is.True);
        Assert.That(epicChip.NeedsAdvanceStones, Is.True);
        Assert.That(epicChip.AdvanceStoneCost, Is.EqualTo(10));

        // Test with 0 Advance Stones
        ChipManager.AdvanceStones = 0;
        bool advancedWithoutStones = epicChip.AdvanceTier();
        Assert.That(advancedWithoutStones, Is.False, "Không thể đột phá lên Advance Tier nếu không đủ 10 Đá Tiến Bậc.");
        Assert.That(epicChip.tier, Is.EqualTo(ChipTier.Epic));

        // Test with 15 Advance Stones
        ChipManager.AdvanceStones = 15;
        bool advancedWithStones = epicChip.AdvanceTier();
        Assert.That(advancedWithStones, Is.True, "Đột phá thành công khi có đủ 10 Đá Tiến Bậc.");
        Assert.That(epicChip.tier, Is.EqualTo(ChipTier.Holographic));
        Assert.That(epicChip.MaxLevel, Is.EqualTo(24));
        Assert.That(ChipManager.AdvanceStones, Is.EqualTo(5), "Phải trừ đúng 10 Đá Tiến Bậc.");
    }

    [Test]
    public void Chipset_Progression_FromTier1ToTier5()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 24,
            chipName = "Flamethrower",
            tier = ChipTier.Magic,
            level = 1,
            count = 500,
            requiredCount = 3
        };

        // Tier 1: upgrade to level 6
        while (chip.level < 6)
        {
            Assert.That(chip.CanUpgrade, Is.True);
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(6));
        Assert.That(chip.IsAtTierCap, Is.True);
        Assert.That(chip.CanUpgrade, Is.False);
        Assert.That(chip.CanAdvanceTier, Is.True);

        // Advance to Tier 2 (Rare, max 9)
        bool adv2 = chip.AdvanceTier();
        Assert.That(adv2, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Rare));
        Assert.That(chip.MaxLevel, Is.EqualTo(9));

        // Tier 2: upgrade to level 9
        while (chip.level < 9)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(9));
        Assert.That(chip.IsAtTierCap, Is.True);

        // Advance to Tier 3 (Unique, max 14)
        bool adv3 = chip.AdvanceTier();
        Assert.That(adv3, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Unique));
        Assert.That(chip.MaxLevel, Is.EqualTo(14));

        // Tier 3: upgrade to level 14
        while (chip.level < 14)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(14));
        Assert.That(chip.IsAtTierCap, Is.True);

        // Advance to Tier 4 (Epic, max 18)
        bool adv4 = chip.AdvanceTier();
        Assert.That(adv4, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Epic));
        Assert.That(chip.MaxLevel, Is.EqualTo(18));

        // Tier 4: upgrade to level 18
        while (chip.level < 18)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(18));
        Assert.That(chip.IsAtTierCap, Is.True);
        Assert.That(chip.NeedsAdvanceStones, Is.True);

        // Advance to Tier 5 (Holographic, max 24) with 10 stones
        ChipManager.AdvanceStones = 20;
        bool adv5 = chip.AdvanceTier();
        Assert.That(adv5, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
        Assert.That(chip.MaxLevel, Is.EqualTo(24));

        // Tier 5: upgrade to level 24
        while (chip.level < 24)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(24));
        Assert.That(chip.IsMaxOverall, Is.True);
        Assert.That(chip.CanUpgrade, Is.False);
        Assert.That(chip.CanAdvanceTier, Is.False);
    }

    [Test]
    public void Buddy_Database_InitializesWithAllDrones()
    {
        GameObject go = new GameObject("BuddyControllerTest");
        BuddyController controller = go.AddComponent<BuddyController>();
        controller.InitializeDatabase();

        Assert.That(controller.AllBuddies.Count, Is.GreaterThanOrEqualTo(10), "Buddy database must have all 10+ drones initialized.");

        BuddyItemData snowflake = null;
        BuddyItemData spider = null;
        foreach (var b in controller.AllBuddies)
        {
            if (b.iconKey == "drone-snowflake") snowflake = b;
            if (b.iconKey == "drone-spider") spider = b;
        }

        Assert.That(snowflake, Is.Not.Null, "Snowflake Drone must exist in database.");
        Assert.That(snowflake.count, Is.EqualTo(65), "Snowflake Drone count matches 65 in screenshot.");
        Assert.That(spider, Is.Not.Null, "Spider Drone must exist in database.");
        Assert.That(spider.count, Is.EqualTo(79), "Spider Drone count matches 79 in screenshot.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Buddy_EnhanceAndAdvanceTier_ConsumesResources_And_IncreasesProgression()
    {
        BuddyItemData drone = new BuddyItemData
        {
            id = 101,
            buddyName = "Test Drone",
            iconKey = "drone-spider",
            tier = BuddyTier.Common,
            level = 1,
            count = 10,
            requiredCount = 3,
            enhanceCost = 500
        };

        // Advance Tier with fragments
        Assert.That(drone.CanAdvanceTier, Is.True);
        bool adv = drone.AdvanceTier();
        Assert.That(adv, Is.True);
        Assert.That(drone.tier, Is.EqualTo(BuddyTier.Magic));
        Assert.That(drone.count, Is.EqualTo(7));
        Assert.That(drone.requiredCount, Is.GreaterThan(3));

        // Enhance level with Data Chips
        ChipManager.DataChips = 2000;
        Assert.That(drone.CanEnhance, Is.True);
        bool enh = drone.Enhance();
        Assert.That(enh, Is.True);
        Assert.That(drone.level, Is.EqualTo(2));
        Assert.That(drone.enhanceCost, Is.GreaterThan(500));
    }

    [Test]
    public void Buddy_CardUI_StateTransitions()
    {
        GameObject go = new GameObject("BuddyCardUITest");
        BuddyCardUI card = go.AddComponent<BuddyCardUI>();

        // Setup empty
        card.SetupEmpty(null);
        Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Empty));
        Assert.That(card.BoundData, Is.Null);

        // Setup locked
        card.SetupLocked(null);
        Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Locked));
        Assert.That(card.BoundData, Is.Null);

        // Setup normal
        BuddyItemData drone = new BuddyItemData { id = 1, buddyName = "Snowflake", level = 1, count = 65, requiredCount = 3 };
        card.Setup(drone, null, null);
        Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Normal));
        Assert.That(card.BoundData, Is.EqualTo(drone));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_Database_InitializesWithSonicBoomAndStats()
    {
        GameObject go = new GameObject("ChipsetControllerTest");
        ChipsetController controller = go.AddComponent<ChipsetController>();
        controller.InitializeDatabase();

        Assert.That(controller.AllChips.Count, Is.EqualTo(24), "Chipset database must have all 24 chips initialized.");

        ChipItemData sonicBoom = null;
        foreach (var c in controller.AllChips)
        {
            if (c.id == 18 || c.chipName == "Sonic Boom") sonicBoom = c;
        }

        Assert.That(sonicBoom, Is.Not.Null, "Sonic Boom must exist in database.");
        Assert.That(sonicBoom.count, Is.EqualTo(439), "Sonic Boom count must be 439 matching screenshot.");
        Assert.That(sonicBoom.requiredCount, Is.EqualTo(3), "Sonic Boom requiredCount must be 3.");
        Assert.That(sonicBoom.description, Does.Contain("Sonic attack"), "Description should describe Sonic attack.");
        Assert.That(sonicBoom.magicBonus, Does.Contain("ATK +15%"), "Magic bonus should be ATK +15%.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_Enhance_ConsumesDataChips_And_IncreasesLevel()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 200,
            chipName = "Test Chip",
            tier = ChipTier.Magic,
            level = 1,
            enhanceCost = 500
        };

        ChipManager.DataChips = 1000;
        Assert.That(chip.CanEnhance, Is.True);
        bool success = chip.Enhance();

        Assert.That(success, Is.True);
        Assert.That(chip.level, Is.EqualTo(2));
        Assert.That(ChipManager.DataChips, Is.EqualTo(500));
        Assert.That(chip.enhanceCost, Is.GreaterThan(500));
    }

    [Test]
    public void Chipset_CardUI_EmptyAndNormalStates()
    {
        GameObject go = new GameObject("ChipsetCardUITest");
        ChipsetCardUI card = go.AddComponent<ChipsetCardUI>();

        card.SetupEmpty(null);
        Assert.That(card.SlotState, Is.EqualTo(ChipSlotState.Empty));
        Assert.That(card.BoundData, Is.Null);

        ChipItemData chip = new ChipItemData { id = 1, chipName = "Standard Gun", level = 1, count = 22, requiredCount = 3 };
        card.Setup(chip, null, null);
        Assert.That(card.SlotState, Is.EqualTo(ChipSlotState.Normal));
        Assert.That(card.BoundData, Is.EqualTo(chip));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Buddy_PurifyingDrone_MatchesStats()
    {
        GameObject go = new GameObject("BuddyControllerTest");
        BuddyController controller = go.AddComponent<BuddyController>();
        controller.InitializeDatabase();

        BuddyItemData purifying = null;
        foreach (var b in controller.AllBuddies)
        {
            if (b.id == 10 || b.buddyName == "Purifying Drone") purifying = b;
        }

        Assert.That(purifying, Is.Not.Null, "Purifying Drone must exist in database.");
        Assert.That(purifying.count, Is.EqualTo(38), "Purifying Drone count must be 38.");
        Assert.That(purifying.requiredCount, Is.EqualTo(3), "Purifying Drone requiredCount must be 3.");
        Assert.That(purifying.description, Does.Contain("Ailment Resistance"), "Description matches screenshot.");

        Object.DestroyImmediate(go);
    }
}


