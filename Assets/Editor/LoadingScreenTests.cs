#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenTests
{
    private const string PrefabPath = "Assets/Resources/UI/LoadingScreen.prefab";
    private const string ScenePath = "Assets/Scenes/Loading.unity";

    [Test]
    public void LoadingScreen_Prefab_ExistsAndHasRequiredComponents()
    {
        Assert.IsTrue(File.Exists(PrefabPath), $"Prefab không tồn tại tại {PrefabPath}");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, "Không thể load Prefab LoadingScreen");

        Canvas canvas = prefab.GetComponent<Canvas>();
        Assert.IsNotNull(canvas, "Prefab thiếu Canvas");
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "Canvas phải là ScreenSpaceOverlay");

        CanvasScaler scaler = prefab.GetComponent<CanvasScaler>();
        Assert.IsNotNull(scaler, "Prefab thiếu CanvasScaler");
        Assert.AreEqual(new Vector2(1080, 1920), scaler.referenceResolution, "CanvasScaler reference resolution phải là 1080x1920");

        CanvasGroup cg = prefab.GetComponent<CanvasGroup>();
        Assert.IsNotNull(cg, "Prefab thiếu CanvasGroup");

        LoadingScreenUI ui = prefab.GetComponent<LoadingScreenUI>();
        Assert.IsNotNull(ui, "Prefab thiếu LoadingScreenUI");
    }

    [Test]
    public void LoadingScreen_BackgroundAndMascot_SpritesAreAssigned()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);

        Transform bg = prefab.transform.Find("Background");
        Assert.IsNotNull(bg, "Không tìm thấy Background");
        Image bgImg = bg.GetComponent<Image>();
        Assert.IsNotNull(bgImg, "Background thiếu Image");
        Assert.IsNotNull(bgImg.sprite, "Background thiếu Sprite nền");
        Assert.IsTrue(bgImg.sprite.name.Contains("nền (4)"), $"Sprite nền sai: {bgImg.sprite.name}");

        Transform mascot = prefab.transform.Find("ProgressBar/Mascot");
        Assert.IsNotNull(mascot, "Không tìm thấy Mascot");

        // 1. Kiểm tra Creep 2
        Transform creep2Visual = mascot.Find("Creep2_Visual");
        Assert.IsNotNull(creep2Visual, "Không tìm thấy Creep2_Visual trong Mascot");
        Animator anim2 = creep2Visual.GetComponent<Animator>();
        Assert.IsNotNull(anim2, "Creep2_Visual thiếu Animator");
        Assert.AreEqual("Big creep 2", anim2.runtimeAnimatorController.name);

        string[] requiredLimbsCreep2 = new string[]
        {
            "big creep2- chân sau_1",
            "big creep2- chân sau_0",
            "big creep2- thaan",
            "big creep2- chân trc_1",
            "big creep2- chân trc_0"
        };
        foreach (string limbName in requiredLimbsCreep2)
        {
            Transform limb = creep2Visual.Find(limbName);
            Assert.IsNotNull(limb, $"Thiếu bộ phận trong Creep 2: {limbName}");
            Image img = limb.GetComponent<Image>();
            Assert.IsNotNull(img, $"{limbName} thiếu Image");
            Assert.IsNotNull(img.sprite, $"{limbName} thiếu Sprite");
        }

        // 2. Kiểm tra Creep 3
        Transform creep3Visual = mascot.Find("Creep3_Visual");
        Assert.IsNotNull(creep3Visual, "Không tìm thấy Creep3_Visual trong Mascot");
        Animator anim3 = creep3Visual.GetComponent<Animator>();
        Assert.IsNotNull(anim3, "Creep3_Visual thiếu Animator");
        Assert.AreEqual("Creep3", anim3.runtimeAnimatorController.name);

        Assert.IsNotNull(creep3Visual.Find("creep 3-thân (1)"));
        Assert.IsNotNull(creep3Visual.Find("creep 3-khớp trên_1/creep 3-khớp dưới_1"));
        Assert.IsNotNull(creep3Visual.Find("creep 3-khớp trên_0/creep 3-khớp dưới_0"));

        // 3. Kiểm tra Creep 1
        Transform creep1Visual = mascot.Find("Creep1_Visual");
        Assert.IsNotNull(creep1Visual, "Không tìm thấy Creep1_Visual trong Mascot");
        Animator anim1 = creep1Visual.GetComponent<Animator>();
        Assert.IsNotNull(anim1, "Creep1_Visual thiếu Animator");
        Assert.AreEqual("Creep", anim1.runtimeAnimatorController.name);

        string[] requiredLimbsCreep1 = new string[] { "chan1", "chan2", "Than", "chan truoc" };
        foreach (string limbName in requiredLimbsCreep1)
        {
            Transform limb = creep1Visual.Find(limbName);
            Assert.IsNotNull(limb, $"Thiếu bộ phận trong Creep 1: {limbName}");
            Image img = limb.GetComponent<Image>();
            Assert.IsNotNull(img, $"{limbName} thiếu Image");
            Assert.IsNotNull(img.sprite, $"{limbName} thiếu Sprite");
        }
    }

    [Test]
    public void LoadingScreen_CreepVariants_CanSwitchRandomly()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            LoadingScreenUI ui = instance.GetComponent<LoadingScreenUI>();
            Assert.IsNotNull(ui);
            Assert.IsNotNull(ui.CreepVariants);
            Assert.AreEqual(3, ui.CreepVariants.Length, "Phải có đúng 3 biến thể quái vật");

            // Kiểm tra chuyển đổi biến thể
            int firstIndex = ui.CurrentVariantIndex;
            bool switched = false;
            for (int i = 0; i < 20; i++)
            {
                ui.SelectRandomCreep();
                int newIndex = ui.CurrentVariantIndex;
                Assert.IsTrue(newIndex >= 0 && newIndex < 3);
                // Đảm bảo chỉ 1 biến thể được active
                int activeCount = 0;
                for (int v = 0; v < ui.CreepVariants.Length; v++)
                {
                    if (ui.CreepVariants[v].activeSelf) activeCount++;
                }
                Assert.AreEqual(1, activeCount, "Chỉ duy nhất 1 con quái được hiển thị tại một thời điểm");

                if (newIndex != firstIndex)
                    switched = true;
            }
            Assert.IsTrue(switched, "Hàm SelectRandomCreep phải chuyển đổi ngẫu nhiên sang quái khác");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void LoadingScreen_ProgressBar_FollowsMascot1to1()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        instance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            LoadingScreenUI ui = instance.GetComponent<LoadingScreenUI>();
            Assert.IsNotNull(ui);

            Transform fill = instance.transform.Find("ProgressBar/Fill");
            Image fillImg = fill.GetComponent<Image>();
            RectTransform barRt = instance.transform.Find("ProgressBar").GetComponent<RectTransform>();
            Transform mascot = instance.transform.Find("ProgressBar/Mascot");
            RectTransform mascotRt = mascot.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            ui.SendMessage("CalculateMascotBounds", SendMessageOptions.RequireReceiver);
            Vector3[] fillCorners = new Vector3[4];
            fillImg.rectTransform.GetWorldCorners(fillCorners);
            float offset = new SerializedObject(ui).FindProperty("mascotOffset").floatValue;
            float startX = barRt.InverseTransformPoint(fillCorners[0]).x + offset;
            float endX = barRt.InverseTransformPoint(fillCorners[3]).x + offset;
            Assert.AreEqual(startX, ui.MascotStartX, 0.01f);
            Assert.AreEqual(endX, ui.MascotEndX, 0.01f);

            // Test 0%, 50%, 100%
            float[] testPoints = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            foreach (float p in testPoints)
            {
                ui.SetProgress(p);
                Assert.AreEqual(p, fillImg.fillAmount, 0.001f, $"FillAmount phải bằng {p}");
                Assert.IsNotNull(fillImg.sprite, "Fill cần Sprite để Unity vẽ theo FillAmount");

                float expectedX = Mathf.Lerp(startX, endX, p);
                Assert.AreEqual(expectedX, mascotRt.anchoredPosition.x, 0.01f, $"Vị trí Mascot X tại {p * 100}% phải khớp tuyệt đối 1:1");
            }
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void LoadingScreen_ProgressBar_ConfiguredCorrectly()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);

        Transform fill = prefab.transform.Find("ProgressBar/Fill");
        Assert.IsNotNull(fill, "Không tìm thấy Fill");
        Image fillImg = fill.GetComponent<Image>();
        Assert.IsNotNull(fillImg, "Fill thiếu Image");
        Assert.AreEqual(Image.Type.Filled, fillImg.type, "Fill Image phải là Type.Filled");
        Assert.AreEqual(Image.FillMethod.Horizontal, fillImg.fillMethod, "Fill Method phải là Horizontal");
    }

    [Test]
    public void LoadingScreen_Scene_ExistsAndInBuildSettings()
    {
        Assert.IsTrue(File.Exists(ScenePath), $"Scene không tồn tại tại {ScenePath}");
        bool inBuild = EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled);
        Assert.IsTrue(inBuild, "Scene Loading.unity chưa được thêm hoặc enable trong EditorBuildSettings");
    }

    [MenuItem("PGE/Run Loading Screen Tests")]
    public static void RunTests()
    {
        try
            {
                if (!Directory.Exists("Reports")) Directory.CreateDirectory("Reports");

                var tests = new LoadingScreenTests();
                tests.LoadingScreen_Prefab_ExistsAndHasRequiredComponents();
                tests.LoadingScreen_BackgroundAndMascot_SpritesAreAssigned();
                tests.LoadingScreen_CreepVariants_CanSwitchRandomly();
                tests.LoadingScreen_ProgressBar_FollowsMascot1to1();
                tests.LoadingScreen_ProgressBar_ConfiguredCorrectly();
                tests.LoadingScreen_Scene_ExistsAndInBuildSettings();

                string report = "[LoadingScreenTests] ALL 6 TESTS PASSED SUCCESSFULLY!\n" +
                                "1. LoadingScreen_Prefab_ExistsAndHasRequiredComponents: PASSED\n" +
                                "2. LoadingScreen_BackgroundAndMascot_SpritesAreAssigned: PASSED (Creep 1, Creep 2, Creep 3 Animators & Limbs valid)\n" +
                                "3. LoadingScreen_CreepVariants_CanSwitchRandomly: PASSED (Randomly selects different creep each time)\n" +
                                "4. LoadingScreen_ProgressBar_FollowsMascot1to1: PASSED (Fill bar locked 1:1 to Creep without drift)\n" +
                                "5. LoadingScreen_ProgressBar_ConfiguredCorrectly: PASSED\n" +
                                "6. LoadingScreen_Scene_ExistsAndInBuildSettings: PASSED\n" +
                                $"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                File.WriteAllText("Reports/loading_screen_test_result.txt", report);
                Debug.Log("<color=green>[LoadingScreenTests] ALL 6 TESTS PASSED SUCCESSFULLY!</color>");
            }
            catch (System.Exception ex)
            {
                string err = $"[LoadingScreenTests] TEST FAILED: {ex.Message}\n{ex.StackTrace}";
                if (!Directory.Exists("Reports")) Directory.CreateDirectory("Reports");
                File.WriteAllText("Reports/loading_screen_test_result.txt", err);
            }
        }
}
#endif
