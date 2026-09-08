using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PGE.EditorTools
{
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
                        rLayout.spacing = 18f;
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
                            badgeRt.sizeDelta = new Vector2(75f, 75f);
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
                            iconRt.sizeDelta = new Vector2(45f, 45f);
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
