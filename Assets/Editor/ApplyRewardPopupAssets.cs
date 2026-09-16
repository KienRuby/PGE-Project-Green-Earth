using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PGE.EditorTools
{
    // Explicit reference-layout command. Reuses the existing hierarchy and sprites.
    public static class RewardPopupReferenceLayout
    {
        private const string RequestPath = "Temp/RewardPopupReference.request";
        private const float Scale = 1080f / 1152f;

        [InitializeOnLoadMethod]
        private static void RegisterRequest()
        {
            EditorApplication.update -= ProcessRequest;
            EditorApplication.update += ProcessRequest;
        }

        private static void ProcessRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                !System.IO.File.Exists(RequestPath)) return;
            string command = System.IO.File.ReadAllText(RequestPath).Trim();
            if (command == "apply" && EditorApplication.isPlayingOrWillChangePlaymode) return;
            System.IO.File.Delete(RequestPath);
            try
            {
                if (command == "apply") Apply();
                else if (command == "capture") CaptureBoth();
            }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }

        private static Transform Popup()
        {
            return Resources.FindObjectsOfTypeAll<RewardPopupController>()
                .First(x => x.gameObject.scene.IsValid() && x.gameObject.scene.name == "MainMenu").transform;
        }

        private static Sprite Sprite(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/" + name + ".png");

        private static void Rect(Transform t, float x, float y, float w, float h)
        {
            if (t == null) return;
            var r = (RectTransform)t;
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y) * Scale;
            r.sizeDelta = new Vector2(w, h) * Scale;
            r.localScale = Vector3.one;
        }

        private static void Image(Transform t, string sprite)
        {
            if (t == null) return;
            var img = t.GetComponent<UnityEngine.UI.Image>();
            if (img == null) return;
            img.sprite = Sprite(sprite);
            img.color = Color.white;
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = false;
        }

        private static void Hide(Transform t) { if (t != null) t.gameObject.SetActive(false); }

        [MenuItem("PGE/UI/Match Reward Popup Reference Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Apply layout in Edit Mode.");
            Transform popup = Popup();
            Undo.RegisterFullObjectHierarchyUndo(popup.gameObject, "Match reward popup references");
            Transform window = popup.Find("Window");
            Rect(window, 126, 430, 912, 1290);
            Image(window, "Frame_Daily_Login_Main");
            Hide(window.Find("Background"));
            Hide(window.Find("CloseButton"));
            foreach (var shadow in window.GetComponentsInChildren<UnityEngine.UI.Shadow>(true))
                if (!(shadow is UnityEngine.UI.Outline)) shadow.enabled = false;
            var dim = popup.Find("DimBackground").GetComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0, 0, 0, 0.78f);
            Transform tabs = window.Find("Tabs");
            Rect(tabs, 72, -118, 768, 118);
            tabs.SetAsFirstSibling();
            ConfigureTab(tabs.Find("DailyLoginTab"), true, true);
            ConfigureTab(tabs.Find("AchievementTab"), false, false);
            var controller = popup.GetComponent<RewardPopupController>();
            controller.SetTabSprites(Sprite("Tab_Daily_Login_Active"), Sprite("Tab_Daily_Login_Inactive"),
                Sprite("Tab_Achievements_Active"), Sprite("Tab_Achievements_Inactive"));
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("dailyTabBg").objectReferenceValue = tabs.Find("DailyLoginTab").GetComponent<UnityEngine.UI.Image>();
            controllerSO.FindProperty("achievementTabBg").objectReferenceValue = tabs.Find("AchievementTab").GetComponent<UnityEngine.UI.Image>();
            controllerSO.FindProperty("lockTabTransforms").boolValue = true;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            ConfigurePanel(window.Find("DailyLoginPanel"), true);
            ConfigurePanel(window.Find("AchievementPanel"), false);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");
            foreach (var text in window.GetComponentsInChildren<TMP_Text>(true))
            {
                if (font != null) text.font = font;
                if (material != null) text.fontSharedMaterial = material;
                text.enableAutoSizing = false;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;
            }
            window.Find("DailyLoginPanel").gameObject.SetActive(true);
            window.Find("AchievementPanel").gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
            EditorSceneManager.SaveScene(popup.gameObject.scene);
            Debug.Log("[RewardReference] Applied and saved reference layout.");
            CaptureBoth();
        }

        private static void ConfigureTab(Transform tab, bool left, bool active)
        {
            var rt = (RectTransform)tab;
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0 : 1, 0);
            rt.pivot = new Vector2(left ? 0 : 1, 0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(343.5f, active ? 116 : 101);
            Image(tab, "Tab_" + (left ? "Daily_Login_" : "Achievements_") + (active ? "Active" : "Inactive"));
            Hide(tab.Find("Background"));
            Hide(tab.Find("Label"));
            Hide(tab.Find("Badge"));
        }

        private static void ConfigurePanel(Transform panel, bool daily)
        {
            Rect(panel, 16, daily ? 41 : 22, 880, daily ? 1200 : 1236);
            Component panelUI = daily ? (Component)panel.GetComponent<DailyLoginPanelUI>() : panel.GetComponent<AchievementPanelUI>();
            var panelSO = new SerializedObject(panelUI);
            panelSO.FindProperty("energyIcon").objectReferenceValue = Sprite("Icon_Energy");
            panelSO.FindProperty("dataChipIcon").objectReferenceValue = Sprite("Icon_Data_Chip");
            panelSO.FindProperty("redGemIcon").objectReferenceValue = Sprite("Icon_Red_Gem");
            panelSO.ApplyModifiedPropertiesWithoutUndo();
            Transform content = panel.Find("Viewport/Content");
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = (daily ? 31 : 23) * Scale;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            foreach (Transform row in content)
            {
                var le = row.GetComponent<UnityEngine.UI.LayoutElement>() ?? row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                le.minHeight = le.preferredHeight = (daily ? 142 : 227) * Scale;
                le.flexibleHeight = 0;
                Rect(row.Find("Background"), 0, 0, 880, daily ? 142 : 227);
                Image(row.Find("Background"), daily ? "Row_Banner_Blue" : "Row_Banner_Achievement");
                if (row.TryGetComponent<UnityEngine.UI.Image>(out var border)) border.color = Color.clear;
                Transform rewards = row.Find("RewardsContainer");
                Rect(rewards, daily ? 229 : 29, daily ? 22 : 109, 355, 89);
                var rewardsLayout = rewards.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (rewardsLayout != null)
                {
                    rewardsLayout.spacing = 47 * Scale;
                    rewardsLayout.padding = new RectOffset();
                    rewardsLayout.childAlignment = TextAnchor.UpperLeft;
                    rewardsLayout.childControlWidth = rewardsLayout.childControlHeight = false;
                    rewardsLayout.childForceExpandWidth = rewardsLayout.childForceExpandHeight = false;
                }
                foreach (Transform badge in rewards)
                {
                    Rect(badge, 0, 0, 87, 89);
                    Rect(badge.Find("Icon"), 0, 0, 87, 89);
                    Rect(badge.Find("AmountText"), -3, 66, 93, 31);
                    var text = badge.Find("AmountText")?.GetComponent<TMP_Text>();
                    if (text != null) { text.fontSize = 27 * Scale; text.alignment = TextAlignmentOptions.Center; }
                    if (badge.TryGetComponent<UnityEngine.UI.Image>(out var badgeBackground)) badgeBackground.color = Color.clear;
                    if (daily)
                    {
                        int index = badge.GetSiblingIndex();
                        Image(badge.Find("Icon"), index == 0 ? "Icon_Energy" : index == 1 ? "Icon_Data_Chip" : "Icon_Red_Gem");
                    }
                }
                if (daily)
                {
                    Rect(row.Find("DayHeader"), 29, 24, 100, 88);
                    foreach (var txt in row.Find("DayHeader").GetComponentsInChildren<TMP_Text>(true))
                    {
                        bool number = txt.text.Trim() != "DAY";
                        Rect(txt.transform, 0, number ? 42 : 0, 80, 42);
                        txt.fontSize = 36 * Scale;
                        txt.color = number ? Color.yellow : Color.white;
                        txt.alignment = TextAlignmentOptions.Center;
                    }
                    Rect(row.Find("StateRight"), 661, 28, 185, 80);
                    Rect(row.Find("StateRight/ClaimButton"), 0, 0, 185.6f, 81.0667f);
                    var item = row.GetComponent<DailyLoginItemUI>();
                    item.SetButtonSprites(Sprite("Btn_Get"), Sprite("Btn_Claim_Again"), Sprite("Btn_Obtained"));
                    int day = row.GetSiblingIndex() + 1;
                    item.SetButtonVisual(day == 1 ? DailyButtonState.Obtained : day == 3 ? DailyButtonState.ClaimAgain : DailyButtonState.Get);
                }
                else
                {
                    Rect(row.Find("TitleText"), 29, 10, 790, 43);
                    var title = row.Find("TitleText").GetComponent<TMP_Text>();
                    title.fontSize = 36 * Scale;
                    Rect(row.Find("ProgressBarBg"), 27, 59, 539, 34);
                    Rect(row.Find("ProgressText"), 27, 59, 539, 34);
                    var progress = row.Find("ProgressText").GetComponent<TMP_Text>();
                    progress.fontSize = 24 * Scale;
                    progress.alignment = TextAlignmentOptions.Center;
                    Rect(row.Find("ActionButton"), 661, 66, 185.6f, 81.0667f);
                    Rect(row.Find("ActionButton/Background"), 0, 0, 185.6f, 81.0667f);
                }
            }
        }

        [MenuItem("PGE/UI/Capture Reward Popup Edit or Play")]
        public static void CaptureBoth()
        {
            Transform popup = Popup();
            bool wasActive = popup.gameObject.activeSelf;
            Transform window = popup.Find("Window");
            bool dailyActive = window.Find("DailyLoginPanel").gameObject.activeSelf;
            popup.gameObject.SetActive(true);
            for (int i = 0; i < 2; i++)
            {
                bool daily = i == 0;
                window.Find("DailyLoginPanel").gameObject.SetActive(daily);
                window.Find("AchievementPanel").gameObject.SetActive(!daily);
                ConfigureTab(window.Find("Tabs/DailyLoginTab"), true, daily);
                ConfigureTab(window.Find("Tabs/AchievementTab"), false, !daily);
                Render(popup.GetComponentInParent<Canvas>(), (daily ? "daily" : "achievements") +
                    (Application.isPlaying ? "-play" : "-edit"));
            }
            window.Find("DailyLoginPanel").gameObject.SetActive(dailyActive);
            window.Find("AchievementPanel").gameObject.SetActive(!dailyActive);
            ConfigureTab(window.Find("Tabs/DailyLoginTab"), true, dailyActive);
            ConfigureTab(window.Find("Tabs/AchievementTab"), false, !dailyActive);
            popup.gameObject.SetActive(wasActive);
        }

        private static void Render(Canvas canvas, string name)
        {
            var cameraObject = new GameObject("RewardReferenceCaptureCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 960;
            camera.transform.position = new Vector3(540, 960, -100);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.enabled = false;
            var texture = new RenderTexture(1080, 1920, 24);
            var mode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldDistance = canvas.planeDistance;
            var previousTarget = RenderTexture.active;
            Texture2D output = null;
            try
            {
                camera.targetTexture = texture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                output = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                output.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                output.Apply();
                System.IO.Directory.CreateDirectory("Temp/RewardReference");
                System.IO.File.WriteAllBytes("Temp/RewardReference/" + name + ".png", output.EncodeToPNG());
                Debug.Log("[RewardReference] Captured " + name);
            }
            finally
            {
                canvas.renderMode = mode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                RenderTexture.active = previousTarget;
                if (output != null) Object.DestroyImmediate(output);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(cameraObject);
                Canvas.ForceUpdateCanvases();
            }
        }
    }

    public static class ApplyRewardPopupAssets
    {
        [MenuItem("PGE/UI/Apply Real Sliced Assets to Popup")]
        public static void ApplyAssetsToScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ApplyAssetsToScene;
                return;
            }

            Scene currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.isDirty)
            {
                EditorSceneManager.SaveScene(currentScene);
            }

            Scene scene = currentScene;
            if (scene.path != "Assets/Scenes/MainMenu.unity")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            }

            Debug.Log($"[ApplyRewardPopupAssets] Target Scene: '{scene.name}' (Path: '{scene.path}', rootCount={scene.rootCount})");

            // 1. Load sliced sprites from sprite sheets and Extracted folder
            Sprite btnGetAch = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Btn_Get.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Btn_Get");
            Sprite btnNotAch = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Btn_Not_Achieved.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Btn_Not_Achieved");
            Sprite bannerAch = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Achievement.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Row_Banner_Achievement");
            Sprite barBgAch = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Progress_Bar_Bg.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Progress_Bar_Bg");
            Sprite barFillAch = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Progress_Bar_Fill.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Progress_Bar_Fill");
            Sprite iconGem = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Icon_Red_Gem.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Icon_Red_Gem");
            Sprite iconChip = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Icon_Data_Chip.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Icon_Data_Chip");
            Sprite iconEnergy = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Icon_Energy.png", "Assets/Sprites/UI/Reward/nút màn achievements.png", "Icon_Energy");

            Sprite btnObtained = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Btn_Obtained.png", "Assets/Sprites/UI/Reward/nút daily login.png", "Btn_Obtained");
            Sprite btnClaimAgain = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Btn_Claim_Again.png", "Assets/Sprites/UI/Reward/nút daily login.png", "Btn_Claim_Again");
            Sprite bannerDailyBlue = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Blue.png", "Assets/Sprites/UI/Reward/nút daily login.png", "Row_Banner_Blue");
            Sprite bannerDailyGrey = LoadSprite("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Grey.png", "Assets/Sprites/UI/Reward/nút daily login.png", "Row_Banner_Grey");

            // 2. Thu thập danh sách items qua Root GameObjects và fallback Resources
            var rootObjects = scene.GetRootGameObjects();
            var achItemsList = new System.Collections.Generic.List<AchievementItemUI>();
            var dailyItemsList = new System.Collections.Generic.List<DailyLoginItemUI>();
            foreach (var root in rootObjects)
            {
                achItemsList.AddRange(root.GetComponentsInChildren<AchievementItemUI>(true));
                dailyItemsList.AddRange(root.GetComponentsInChildren<DailyLoginItemUI>(true));
            }

            if (dailyItemsList.Count == 0)
            {
                dailyItemsList.AddRange(Resources.FindObjectsOfTypeAll<DailyLoginItemUI>()
                    .Where(x => !EditorUtility.IsPersistent(x.gameObject) && x.gameObject.scene == scene));
            }
            if (achItemsList.Count == 0)
            {
                achItemsList.AddRange(Resources.FindObjectsOfTypeAll<AchievementItemUI>()
                    .Where(x => !EditorUtility.IsPersistent(x.gameObject) && x.gameObject.scene == scene));
            }

            var achItems = achItemsList.ToArray();
            var dailyItems = dailyItemsList.OrderBy(d => d.name).ToArray();
            Debug.Log($"[ApplyRewardPopupAssets] Found {achItems.Length} Achievement items, {dailyItems.Length} Daily Login items.");
            foreach (var item in achItems)
            {
                SerializedObject so = new SerializedObject(item);
                so.FindProperty("btnGetSprite").objectReferenceValue = btnGetAch;
                so.FindProperty("btnNotAchievedSprite").objectReferenceValue = btnNotAch;
                so.FindProperty("btnObtainedSprite").objectReferenceValue = btnObtained;
                so.FindProperty("cardBannerSprite").objectReferenceValue = bannerAch;
                so.FindProperty("progressBarBgSprite").objectReferenceValue = barBgAch;
                so.FindProperty("progressBarFillSprite").objectReferenceValue = barFillAch;
                so.ApplyModifiedProperties();

                // Item row banner background
                Transform bgTr = item.transform.Find("Background");
                if (bgTr != null && bgTr.TryGetComponent<Image>(out var bgImg))
                {
                    if (bannerAch != null)
                    {
                        bgImg.sprite = bannerAch;
                        bgImg.color = Color.white;
                    }
                }

                // Progress Bar
                Transform pBgTr = item.transform.Find("ProgressBarBg");
                if (pBgTr != null && pBgTr.TryGetComponent<Image>(out var pBgImg))
                {
                    if (barBgAch != null)
                    {
                        pBgImg.sprite = barBgAch;
                        pBgImg.color = Color.white;
                    }

                    Transform pFillTr = pBgTr.Find("ProgressFill");
                    if (pFillTr != null && pFillTr.TryGetComponent<Image>(out var pFillImg))
                    {
                        if (barFillAch != null)
                        {
                            pFillImg.sprite = barFillAch;
                            pFillImg.color = Color.white;
                        }
                    }
                }

                // Action Button
                Transform btnTr = item.transform.Find("ActionButton");
                if (btnTr != null)
                {
                    // Border on ActionButton root
                    if (btnTr.TryGetComponent<Image>(out var borderImg))
                    {
                        borderImg.color = Color.clear;
                    }

                    // Background on ActionButton/Background
                    Transform btnBgTr = btnTr.Find("Background");
                    if (btnBgTr != null && btnBgTr.TryGetComponent<Image>(out var btnBgImg))
                    {
                        Transform labelTr = btnTr.Find("Label");
                        string labelText = labelTr != null && labelTr.TryGetComponent<TMP_Text>(out var tmp) ? tmp.text : "";

                        if (labelText.IndexOf("Obtain", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.name.EndsWith("_3") || item.name.EndsWith("_4"))
                        {
                            btnBgImg.sprite = btnObtained;
                        }
                        else if (labelText.IndexOf("Get", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                                 (item.name.EndsWith("_0") || item.name.EndsWith("_1") || item.name.EndsWith("_2")))
                        {
                            btnBgImg.sprite = btnNotAch != null ? btnNotAch : btnGetAch;
                        }
                        else
                        {
                            btnBgImg.sprite = btnGetAch;
                        }

                        btnBgImg.color = Color.white;
                        btnBgImg.preserveAspect = true;

                        if (labelTr != null)
                        {
                            labelTr.gameObject.SetActive(false);
                        }
                    }
                }

                // Reward badges icons
                Transform rewardsContainer = item.transform.Find("RewardsContainer");
                if (rewardsContainer != null)
                {
                    for (int i = 0; i < rewardsContainer.childCount; i++)
                    {
                        Transform badge = rewardsContainer.GetChild(i);
                        if (badge.TryGetComponent<Image>(out var badgeImg))
                        {
                            SerializedObject soImg = new SerializedObject(badgeImg);
                            SerializedProperty colProp = soImg.FindProperty("m_Color");
                            if (colProp != null)
                            {
                                Color c = colProp.colorValue;
                                colProp.colorValue = new Color(c.r, c.g, c.b, 0f);
                                soImg.ApplyModifiedProperties();
                            }
                            badgeImg.color = new Color(badgeImg.color.r, badgeImg.color.g, badgeImg.color.b, 0f);
                            EditorUtility.SetDirty(badgeImg);
                            EditorUtility.SetDirty(badge.gameObject);
                        }

                        Transform iconTr = badge.Find("Icon");
                        if (iconTr != null && iconTr.TryGetComponent<Image>(out var iconImg))
                        {
                            Transform amtTr = badge.Find("AmountText");
                            string amtText = amtTr != null && amtTr.TryGetComponent<TMP_Text>(out var atmp) ? atmp.text : "";

                            if (i == 0 && iconGem != null) iconImg.sprite = iconGem;
                            else if (i == 1 && iconChip != null) iconImg.sprite = iconChip;
                            else if (i == 2 && iconEnergy != null) iconImg.sprite = iconEnergy;
                            iconImg.preserveAspect = true;
                        }
                    }
                }

                EditorUtility.SetDirty(item.gameObject);
            }

            // 3. Process Daily Login Items
            for (int d = 0; d < dailyItems.Length; d++)
            {
                var item = dailyItems[d];
                int dayNumber = d + 1;
                if (item.name.StartsWith("Day") && int.TryParse(item.name.Substring(3), out int parsedDay))
                {
                    dayNumber = parsedDay;
                }

                Transform rewardsTr = item.transform.Find("RewardsContainer") ?? item.transform.Find("Rewards");

                // 3.1 Background Banner & Brightness
                Transform bgTr = item.transform.Find("Background");
                Image bgImg = null;
                if (bgTr != null && bgTr.TryGetComponent<Image>(out bgImg))
                {
                    // Nút sáng (Row_Banner_Blue) LUÔN LUÔN là background chính cho TẤT CẢ các ngày, KHÔNG BAO GIỜ bị thay thế
                    if (bannerDailyBlue != null)
                    {
                        bgImg.sprite = bannerDailyBlue;
                        bgImg.color = Color.white;
                    }
                }

                // Tạo hoặc tìm DarkOverlay đè lên Background (dùng sprite Row_Banner_Grey)
                Transform overlayTr = (bgTr != null ? bgTr.Find("DarkOverlay") : null) ?? item.transform.Find("DarkOverlay");
                if (overlayTr == null && bgTr != null)
                {
                    GameObject overlayObj = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
                    overlayObj.transform.SetParent(bgTr, false);
                    overlayTr = overlayObj.transform;
                }

                if (overlayTr != null)
                {
                    RectTransform overlayRt = overlayTr.GetComponent<RectTransform>();
                    overlayRt.anchorMin = Vector2.zero;
                    overlayRt.anchorMax = Vector2.one;
                    overlayRt.offsetMin = Vector2.zero;
                    overlayRt.offsetMax = Vector2.zero;
                    overlayRt.localScale = Vector3.one;
                    overlayRt.SetAsLastSibling();

                    if (overlayTr.TryGetComponent<Image>(out var overlayImg))
                    {
                        overlayImg.sprite = bannerDailyGrey;
                        overlayImg.color = Color.white;
                        overlayImg.raycastTarget = false;
                    }
                }

                // 3.2 Action Button (ClaimButton)
                Transform btnTr = item.transform.Find("StateRight/ClaimButton") ?? item.transform.Find("ClaimButton");
                Image btnImg = btnTr != null ? btnTr.GetComponent<Image>() : null;
                Button btn = btnTr != null ? btnTr.GetComponent<Button>() : null;

                // Xác định trạng thái Obtained:
                // Cả 2 ngày đã nhận (Day 01 và Day 02 theo ảnh thực tế) phải ở trạng thái Obtained
                bool isDayObtained = (dayNumber == 1 || dayNumber == 2);
                if (btnImg != null && btnImg.sprite != null && btnImg.sprite.name.IndexOf("Obtained", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isDayObtained = true;
                }

                if (overlayTr != null)
                {
                    // Chỉ ngày nào đã nhận rồi (Day 01, Day 02) thì mới TỐI (bật overlay đè lên nút sáng)
                    // Tất cả các ngày chưa nhận (Day 03..07) thì TẮT overlay (SÁNG)
                    overlayTr.gameObject.SetActive(isDayObtained);
                }

                if (btnTr != null && btnImg != null)
                {
                    btnTr.gameObject.SetActive(true);
                    btnImg.color = Color.white;
                    btnImg.preserveAspect = true;

                    Transform labelTr = btnTr.Find("Label");
                    if (labelTr != null) labelTr.gameObject.SetActive(false);

                    if (isDayObtained)
                    {
                        btnImg.sprite = btnObtained;
                        if (btn != null) btn.interactable = false;
                    }
                    else
                    {
                        btnImg.sprite = btnGetAch;
                        if (btn != null) btn.interactable = true;
                    }
                }

                // Cả ngày đã nhận và chưa nhận đều giữ alpha = 1.0f để chữ, icon, nút không bị xỉn màu
                if (item.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.alpha = 1.0f;
                }

                // Hide border on item root if present so banner is clean
                if (item.TryGetComponent<Image>(out var rootImg))
                {
                    rootImg.color = Color.clear;
                }

                SerializedObject so = new SerializedObject(item);
                so.FindProperty("btnGetSprite").objectReferenceValue = btnGetAch;
                so.FindProperty("btnClaimAgainSprite").objectReferenceValue = btnClaimAgain;
                so.FindProperty("btnObtainedSprite").objectReferenceValue = btnObtained;
                so.FindProperty("cardBannerBlue").objectReferenceValue = bannerDailyBlue;
                so.FindProperty("cardBannerGrey").objectReferenceValue = bannerDailyGrey;
                if (bgImg != null) so.FindProperty("cardBackground").objectReferenceValue = bgImg;
                if (overlayTr != null) so.FindProperty("darkOverlay").objectReferenceValue = overlayTr.gameObject;
                if (btnImg != null) so.FindProperty("claimButtonImage").objectReferenceValue = btnImg;
                if (btn != null) so.FindProperty("claimButton").objectReferenceValue = btn;
                if (rewardsTr != null)
                {
                    so.FindProperty("rewardsContainer").objectReferenceValue = rewardsTr;
                }
                so.ApplyModifiedProperties();

                // 3.3 Rewards Container & Badges
                if (rewardsTr != null)
                {
                    (Sprite sprite, string amount)[] dayRewards = GetDefaultDayRewards(dayNumber, iconEnergy, iconGem, iconChip);

                    HorizontalLayoutGroup rLayout = rewardsTr.GetComponent<HorizontalLayoutGroup>();
                    if (rLayout != null)
                    {
                        rLayout.spacing = 45f;
                        rLayout.childAlignment = TextAnchor.MiddleLeft;
                        rLayout.childControlWidth = false;
                        rLayout.childControlHeight = false;
                    }

                    // If no badges exist, create them
                    if (rewardsTr.childCount == 0)
                    {
                        for (int r = 0; r < dayRewards.Length; r++)
                        {
                            var rw = dayRewards[r];
                            GameObject badge = new GameObject($"RewardBadge_{r}", typeof(RectTransform), typeof(Image));
                            badge.transform.SetParent(rewardsTr, false);
                            RectTransform badgeRt = badge.GetComponent<RectTransform>();
                            badgeRt.sizeDelta = new Vector2(82f, 84f);
                            Image badgeBg = badge.GetComponent<Image>();
                            badgeBg.color = new Color32(11, 45, 60, 0); // alpha = 0 theo yêu cầu người dùng
                            badgeBg.raycastTarget = false;

                            // Icon
                            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                            iconObj.transform.SetParent(badge.transform, false);
                            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                            iconRt.pivot = new Vector2(0.5f, 0.5f);
                            iconRt.anchoredPosition = new Vector2(0f, 8f);
                            iconRt.sizeDelta = new Vector2(82f, 84f);
                            Image iconImg = iconObj.GetComponent<Image>();
                            iconImg.sprite = rw.sprite;
                            iconImg.preserveAspect = true;
                            iconImg.raycastTarget = false;

                            // Amount Text
                            GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
                            textObj.transform.SetParent(badge.transform, false);
                            RectTransform textRt = textObj.GetComponent<RectTransform>();
                            textRt.anchorMin = new Vector2(0f, 0f);
                            textRt.anchorMax = new Vector2(1f, 0f);
                            textRt.pivot = new Vector2(0.5f, 0f);
                            textRt.anchoredPosition = new Vector2(0f, -22f);
                            textRt.sizeDelta = new Vector2(90f, 24f);

                            TextMeshProUGUI amtTxt = textObj.GetComponent<TextMeshProUGUI>();
                            amtTxt.text = rw.amount;
                            amtTxt.fontSize = 20f;
                            amtTxt.fontStyle = FontStyles.Bold;
                            amtTxt.alignment = TextAlignmentOptions.Center;
                            amtTxt.color = Color.white;
                            amtTxt.raycastTarget = false;
                        }
                    }
                    else
                    {
                        // Badges exist -> update sprites, amounts, and set alpha = 0 on all RewardBadge backgrounds
                        for (int r = 0; r < rewardsTr.childCount; r++)
                        {
                            Transform badge = rewardsTr.GetChild(r);
                            if (badge.TryGetComponent<Image>(out var badgeImg))
                            {
                                SerializedObject soImg = new SerializedObject(badgeImg);
                                SerializedProperty colProp = soImg.FindProperty("m_Color");
                                if (colProp != null)
                                {
                                    Color c = colProp.colorValue;
                                    colProp.colorValue = new Color(c.r, c.g, c.b, 0f);
                                    soImg.ApplyModifiedProperties();
                                }
                                badgeImg.color = new Color(badgeImg.color.r, badgeImg.color.g, badgeImg.color.b, 0f); // alpha = 0
                                EditorUtility.SetDirty(badgeImg);
                                EditorUtility.SetDirty(badge.gameObject);
                            }

                            if (r < dayRewards.Length)
                            {
                                var rw = dayRewards[r];
                                Transform iconTr = badge.Find("Icon");
                                if (iconTr != null && iconTr.TryGetComponent<Image>(out var iconImg))
                                {
                                    iconImg.sprite = rw.sprite;
                                    iconImg.preserveAspect = true;
                                }
                                Transform amtTr = badge.Find("AmountText");
                                if (amtTr != null && amtTr.TryGetComponent<TMP_Text>(out var amtTxt))
                                {
                                    amtTxt.text = rw.amount;
                                }
                            }
                        }
                    }
                }

                EditorUtility.SetDirty(item.gameObject);
            }

            // 4. Quét toàn bộ GameObject RewardBadge trong toàn Scene: đảm bảo alpha = 0 trên Image component
            foreach (var root in rootObjects)
            {
                foreach (var badgeImg in root.GetComponentsInChildren<Image>(true))
                {
                    if (badgeImg.gameObject.name.IndexOf("RewardBadge", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SerializedObject soImg = new SerializedObject(badgeImg);
                        SerializedProperty colProp = soImg.FindProperty("m_Color");
                        if (colProp != null)
                        {
                            Color c = colProp.colorValue;
                            if (c.a != 0f)
                            {
                                colProp.colorValue = new Color(c.r, c.g, c.b, 0f);
                                soImg.ApplyModifiedProperties();
                            }
                        }
                        if (badgeImg.color.a != 0f)
                        {
                            badgeImg.color = new Color(badgeImg.color.r, badgeImg.color.g, badgeImg.color.b, 0f);
                        }
                        EditorUtility.SetDirty(badgeImg);
                        EditorUtility.SetDirty(badgeImg.gameObject);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ApplyRewardPopupAssets] Đã áp dụng toàn bộ Real Sliced Sprites & Badges vào {achItems.Length} Achievement items và {dailyItems.Length} Daily Login items trong MainMenu!");
        }

        private static (Sprite sprite, string amount)[] GetDefaultDayRewards(int day, Sprite energy, Sprite gem, Sprite chip)
        {
            // Thứ tự và số lượng chuẩn xác 100% theo ảnh mẫu:
            // 1. Pin Năng Lượng: x30
            // 2. Chip Dữ Liệu: x300
            // 3. Gem Đỏ: x1000
            return new[]
            {
                (energy, "x30"),
                (chip, "x300"),
                (gem, "x1000")
            };
        }

        private static Sprite LoadSprite(string extractedPath, string sheetPath, string spriteName)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(extractedPath);
            if (sp != null) return sp;

            var all = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            if (all != null)
            {
                foreach (var obj in all)
                {
                    if (obj is Sprite s && s.name == spriteName) return s;
                }
            }
            return null;
        }
    }
}
