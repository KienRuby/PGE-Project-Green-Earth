using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Công cụ Editor tự động tạo thư mục và các ScriptableObject cho hệ thống Artifact:
/// - Data Disc (Artifact_CD)
/// - Modular Brick (Artifact_Lego)
/// - Kung Fu Data USB (Artifact_USB)
/// - Energy Butter (Artifact_Butter)
/// - Strong Cooler (Artifact_Cooler)
/// - Signal Rocket (Artifact_Rocket)
/// - Quantum Microchip (Artifact_Chip)
/// - ArtifactDatabase.asset
/// </summary>
public static class ArtifactSystemSetup
{
    private const string FolderPath = "Assets/Data/Artifacts";
    private const string ResourcesFolderPath = "Assets/Resources";
    private const string SpriteSheetPath = "Assets/Sprites/UI/nút artifact.png";

    [InitializeOnLoadMethod]
    [MenuItem("PGE/Setup Artifact Database & Defaults", false, 120)]
    [MenuItem("Tools/PGE/Setup Artifact Database & Defaults", false, 120)]
    public static void GenerateArtifactAssets()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        if (!Directory.Exists(ResourcesFolderPath))
        {
            Directory.CreateDirectory(ResourcesFolderPath);
        }

        // Tải toàn bộ sprite đã cắt từ sprite sheet nút artifact.png
        Dictionary<string, Sprite> sheetSprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .GroupBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Sprite GetSprite(string name)
        {
            sheetSprites.TryGetValue(name, out Sprite s);
            return s;
        }

        // 1. Data Disc (CD) - Crit Rate +10%
        ArtifactData disc = CreateOrUpdateArtifact(
            "data_disc",
            "Data Disc",
            "Shiny optical disc storing lost battle simulations and ancient data.",
            ArtifactStatType.CritRatePercent,
            10f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_CD")
        );

        // 2. Modular Brick (LEGO) - DEF +12
        ArtifactData brick = CreateOrUpdateArtifact(
            "modular_brick",
            "Modular Brick",
            "Interlocking plastic toy brick. Incredibly durable construction.",
            ArtifactStatType.DamageReduction,
            12f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_Lego")
        );

        // 3. Kung Fu Data USB (USB) - All Weapons' ATK +9%
        ArtifactData usb = CreateOrUpdateArtifact(
            "kung_fu_usb",
            "Kung Fu Data USB",
            "Does it actually have the Epic tome of Kung Fu in it?",
            ArtifactStatType.AllWeaponsDamagePercent,
            9f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_USB")
        );

        // 4. Energy Butter (Bơ / Phô mai) - HP +15%
        ArtifactData butter = CreateOrUpdateArtifact(
            "energy_butter",
            "Energy Butter",
            "High-calorie organic nutrient block that enhances biological vitality.",
            ArtifactStatType.MaxHealthPercent,
            15f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_Butter")
        );

        // 5. Strong Cooler (Quạt tản nhiệt) - Turret ATK Speed +20%
        ArtifactData cooler = CreateOrUpdateArtifact(
            "strong_cooler",
            "Strong Cooler",
            "Cools down Turrets when they overheat.",
            ArtifactStatType.TurretAttackSpeedPercent,
            20f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_Cooler")
        );

        // 6. Signal Rocket (Tên lửa / Pháo) - Move Speed +12%
        ArtifactData rocket = CreateOrUpdateArtifact(
            "signal_rocket",
            "Signal Rocket",
            "Miniature rocket propulsion unit. Boosts overall movement speed.",
            ArtifactStatType.MoveSpeedPercent,
            12f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_Rocket")
        );

        // 7. Quantum Microchip (Chip vi mạch) - Ranged DEF +15%
        ArtifactData chip = CreateOrUpdateArtifact(
            "quantum_chip",
            "Quantum Microchip",
            "Advanced silicon processor that calculates incoming ranged projectile vectors.",
            ArtifactStatType.RangedDefensePercent,
            15f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255),
            GetSprite("Artifact_Chip")
        );

        // Giữ lại Spare Battery để tương thích ngược cho các unit test cũ nếu có
        ArtifactData legacyBattery = CreateOrUpdateArtifact(
            "spare_battery",
            "Spare Battery",
            "Eco-friendly product you can recharge.",
            ArtifactStatType.MaxHealthPercent,
            15f,
            new Color32(46, 229, 240, 255),
            new Color32(11, 45, 60, 255)
        );

        // 8. Cập nhật ArtifactDatabase trong Data & Resources với 7 Artifact mới
        CreateOrUpdateDatabase("Assets/Data/Artifacts/ArtifactDatabase.asset", disc, brick, usb, butter, cooler, rocket, chip, legacyBattery);
        CreateOrUpdateDatabase("Assets/Resources/ArtifactDatabase.asset", disc, brick, usb, butter, cooler, rocket, chip, legacyBattery);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ArtifactSystemSetup] ✅ Đã cập nhật hoàn tất 7 Cổ vật mới với Sprite cắt từ sheet và ArtifactDatabase!");
    }

    private static ArtifactData CreateOrUpdateArtifact(
        string id,
        string name,
        string lore,
        ArtifactStatType statType,
        float statVal,
        Color borderColor,
        Color bgColor,
        Sprite sprite = null)
    {
        string path = $"{FolderPath}/{name.Replace(" ", "_").Replace("'", "")}.asset";
        ArtifactData data = AssetDatabase.LoadAssetAtPath<ArtifactData>(path);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ArtifactData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.id = id;
        data.artifactName = name;
        data.loreDescription = lore;
        data.statType = statType;
        data.statValue = statVal;
        data.badgeBorderColor = borderColor;
        data.badgeBgColor = bgColor;

        if (sprite != null)
        {
            data.icon = sprite;
        }
        else
        {
            string iconPath = $"Assets/Sprites/UI/Artifact/{id}.png";
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (spr != null)
            {
                data.icon = spr;
            }
        }

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void CreateOrUpdateDatabase(string path, params ArtifactData[] items)
    {
        ArtifactDatabase db = AssetDatabase.LoadAssetAtPath<ArtifactDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ArtifactDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }

        db.artifacts.Clear();
        foreach (var it in items)
        {
            if (it != null && !db.artifacts.Contains(it))
            {
                db.artifacts.Add(it);
            }
        }

        EditorUtility.SetDirty(db);
    }

    [MenuItem("PGE/Setup Artifact Icons in Pause Modal", false, 121)]
    public static void SetupArtifactIconsMenu()
    {
        PauseModalController pauseCtrl = Object.FindObjectOfType<PauseModalController>(true);
        if (pauseCtrl == null)
        {
            Debug.LogWarning("[ArtifactSystemSetup] Không tìm thấy PauseModalController trong Scene!");
            return;
        }

        Transform artPanel = pauseCtrl.transform.Find("MainFrame/ArtifactPanel")
                          ?? pauseCtrl.transform.Find("ArtifactPanel");

        if (artPanel == null)
        {
            Debug.LogWarning("[ArtifactSystemSetup] Không tìm thấy ArtifactPanel!");
            return;
        }

        SetupArtifactIconsInPanel(artPanel.gameObject, pauseCtrl);
    }

    public static void SetupArtifactIconsInPanel(GameObject artifactPanel, PauseModalController pauseCtrl = null)
    {
        if (artifactPanel == null) return;

        string[] spriteNames = new string[]
        {
            "titanium_fabric",
            "spare_battery",
            "carbon_scales",
            "strong_cooler",
            "kung_fu_usb",
            "artifact_slot_5",
            "artifact_slot_6",
            "artifact_slot_7"
        };

        const int columns = 4;
        const int totalSlots = 8;
        const float spacingX = 205f;
        const float spacingY = 200f;
        const float startX = -307.5f;
        const float startY = 360f;

        for (int i = 0; i < totalSlots; i++)
        {
            string slotName = $"ArtifactIcon_{i + 1}";
            Transform existing = artifactPanel.transform.Find(slotName);
            GameObject slotObj;
            int col = i % columns;
            int row = i / columns;
            Vector2 pos = new Vector2(startX + (col * spacingX), startY - (row * spacingY));

            if (existing == null)
            {
                slotObj = new GameObject(slotName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                slotObj.transform.SetParent(artifactPanel.transform, false);
                Undo.RegisterCreatedObjectUndo(slotObj, $"Create {slotName}");

                RectTransform rt = slotObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(150f, 175f);
            }
            else
            {
                slotObj = existing.gameObject;
                RectTransform rt = slotObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = pos;
                    rt.sizeDelta = new Vector2(150f, 175f);
                }
            }

            slotObj.SetActive(true);
            var btn = slotObj.GetComponent<UnityEngine.UI.Button>();
            if (btn == null)
            {
                btn = slotObj.AddComponent<UnityEngine.UI.Button>();
            }

            UnityEngine.UI.Image img = slotObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                if (i < spriteNames.Length)
                {
                    string path = $"Assets/Sprites/UI/Artifact/{spriteNames[i]}.png";
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null) img.sprite = s;
                }
                img.color = Color.white;
                img.preserveAspect = true;
                btn.targetGraphic = img;
                EditorUtility.SetDirty(img);
            }
            EditorUtility.SetDirty(slotObj);
        }

        if (pauseCtrl != null)
        {
            SetupArtifactDetailDialogInScene(pauseCtrl);
            pauseCtrl.AutoWireArtifactIconSlots();
            EditorUtility.SetDirty(pauseCtrl);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[ArtifactSystemSetup] ✅ Đã thiết lập lưới 4 icon/hàng cho Artifact trong Pause Modal thành công!");
    }

    public static void SetupArtifactDetailDialogInScene(PauseModalController pauseCtrl)
    {
        if (pauseCtrl == null) return;

        Transform parentTr = pauseCtrl.transform;
        Transform existing = parentTr.Find("ArtifactDetailDialog");
        GameObject dialogGo;
        if (existing == null)
        {
            dialogGo = new GameObject("ArtifactDetailDialog", typeof(RectTransform));
            dialogGo.transform.SetParent(parentTr, false);
            Undo.RegisterCreatedObjectUndo(dialogGo, "Create ArtifactDetailDialog");

            RectTransform drt = dialogGo.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;

            // 1. Backdrop
            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            backdrop.transform.SetParent(dialogGo.transform, false);
            RectTransform brt = backdrop.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            UnityEngine.UI.Image bImg = backdrop.GetComponent<UnityEngine.UI.Image>();
            bImg.color = new Color(0f, 0f, 0f, 0.65f);

            // 2. DetailCard
            GameObject card = new GameObject("DetailCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            card.transform.SetParent(dialogGo.transform, false);
            RectTransform crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(880f, 1180f);
            UnityEngine.UI.Image cImg = card.GetComponent<UnityEngine.UI.Image>();
            cImg.color = new Color32(88, 172, 178, 255);

            // Fill
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            fill.transform.SetParent(card.transform, false);
            RectTransform frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(8f, 8f);
            frt.offsetMax = new Vector2(-8f, -8f);
            UnityEngine.UI.Image fImg = fill.GetComponent<UnityEngine.UI.Image>();
            fImg.color = new Color32(14, 48, 68, 255);

            // 3. IconFrame
            GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            iconFrame.transform.SetParent(card.transform, false);
            RectTransform ifrt = iconFrame.GetComponent<RectTransform>();
            ifrt.anchorMin = new Vector2(0.5f, 0.5f);
            ifrt.anchorMax = new Vector2(0.5f, 0.5f);
            ifrt.pivot = new Vector2(0.5f, 0.5f);
            ifrt.anchoredPosition = new Vector2(0f, 260f);
            ifrt.sizeDelta = new Vector2(260f, 300f);
            UnityEngine.UI.Image ifImg = iconFrame.GetComponent<UnityEngine.UI.Image>();
            ifImg.color = new Color32(88, 172, 178, 255);

            GameObject ifFill = new GameObject("IconFrameFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            ifFill.transform.SetParent(iconFrame.transform, false);
            RectTransform iffrt = ifFill.GetComponent<RectTransform>();
            iffrt.anchorMin = Vector2.zero;
            iffrt.anchorMax = Vector2.one;
            iffrt.offsetMin = new Vector2(6f, 6f);
            iffrt.offsetMax = new Vector2(-6f, -6f);
            UnityEngine.UI.Image iffImg = ifFill.GetComponent<UnityEngine.UI.Image>();
            iffImg.color = new Color32(11, 45, 60, 255);

            // Icon Image
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            iconObj.transform.SetParent(iconFrame.transform, false);
            RectTransform icrt = iconObj.GetComponent<RectTransform>();
            icrt.anchorMin = new Vector2(0.5f, 0.5f);
            icrt.anchorMax = new Vector2(0.5f, 0.5f);
            icrt.pivot = new Vector2(0.5f, 0.5f);
            icrt.anchoredPosition = Vector2.zero;
            icrt.sizeDelta = new Vector2(180f, 180f);
            UnityEngine.UI.Image icImg = iconObj.GetComponent<UnityEngine.UI.Image>();
            icImg.preserveAspect = true;
            icImg.color = Color.white;
            string iconPath = "Assets/Sprites/UI/Artifact/titanium_fabric.png";
            Sprite icSpr = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icSpr != null) icImg.sprite = icSpr;

            // 4. NameText
            GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            nameObj.transform.SetParent(card.transform, false);
            RectTransform nmrt = nameObj.GetComponent<RectTransform>();
            nmrt.anchorMin = new Vector2(0.5f, 0.5f);
            nmrt.anchorMax = new Vector2(0.5f, 0.5f);
            nmrt.pivot = new Vector2(0.5f, 0.5f);
            nmrt.anchoredPosition = new Vector2(0f, 50f);
            nmrt.sizeDelta = new Vector2(750f, 60f);
            TMPro.TextMeshProUGUI nmTxt = nameObj.GetComponent<TMPro.TextMeshProUGUI>();
            nmTxt.text = "Titanium Fabric";
            nmTxt.fontSize = 36f;
            nmTxt.fontStyle = TMPro.FontStyles.Bold;
            nmTxt.alignment = TMPro.TextAlignmentOptions.Center;
            nmTxt.color = new Color32(255, 184, 28, 255);

            // 5. LoreText
            GameObject loreObj = new GameObject("LoreText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            loreObj.transform.SetParent(card.transform, false);
            RectTransform lrrt = loreObj.GetComponent<RectTransform>();
            lrrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrrt.pivot = new Vector2(0.5f, 0.5f);
            lrrt.anchoredPosition = new Vector2(0f, -40f);
            lrrt.sizeDelta = new Vector2(750f, 80f);
            TMPro.TextMeshProUGUI lrTxt = loreObj.GetComponent<TMPro.TextMeshProUGUI>();
            lrTxt.text = "Sturdy titanium. Covers the body.";
            lrTxt.fontSize = 24f;
            lrTxt.alignment = TMPro.TextAlignmentOptions.Center;
            lrTxt.color = Color.white;

            // 6. StatText
            GameObject statObj = new GameObject("StatText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            statObj.transform.SetParent(card.transform, false);
            RectTransform strt = statObj.GetComponent<RectTransform>();
            strt.anchorMin = new Vector2(0.5f, 0.5f);
            strt.anchorMax = new Vector2(0.5f, 0.5f);
            strt.pivot = new Vector2(0.5f, 0.5f);
            strt.anchoredPosition = new Vector2(0f, -150f);
            strt.sizeDelta = new Vector2(750f, 60f);
            TMPro.TextMeshProUGUI stTxt = statObj.GetComponent<TMPro.TextMeshProUGUI>();
            stTxt.text = "DEF +10";
            stTxt.fontSize = 32f;
            stTxt.fontStyle = TMPro.FontStyles.Bold;
            stTxt.alignment = TMPro.TextAlignmentOptions.Center;
            stTxt.color = Color.white;

            // 7. OkButton
            GameObject okObj = new GameObject("OkButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            okObj.transform.SetParent(card.transform, false);
            RectTransform okrt = okObj.GetComponent<RectTransform>();
            okrt.anchorMin = new Vector2(0.5f, 0.5f);
            okrt.anchorMax = new Vector2(0.5f, 0.5f);
            okrt.pivot = new Vector2(0.5f, 0.5f);
            okrt.anchoredPosition = new Vector2(0f, -350f);
            okrt.sizeDelta = new Vector2(320f, 85f);
            UnityEngine.UI.Image okImg = okObj.GetComponent<UnityEngine.UI.Image>();
            okImg.color = new Color32(88, 172, 178, 255);

            GameObject okTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            okTxtObj.transform.SetParent(okObj.transform, false);
            RectTransform oktxtrt = okTxtObj.GetComponent<RectTransform>();
            oktxtrt.anchorMin = Vector2.zero;
            oktxtrt.anchorMax = Vector2.one;
            oktxtrt.offsetMin = Vector2.zero;
            oktxtrt.offsetMax = Vector2.zero;
            TMPro.TextMeshProUGUI oktxt = okTxtObj.GetComponent<TMPro.TextMeshProUGUI>();
            oktxt.text = "OK";
            oktxt.fontSize = 32f;
            oktxt.fontStyle = TMPro.FontStyles.Bold;
            oktxt.alignment = TMPro.TextAlignmentOptions.Center;
            oktxt.color = new Color32(14, 28, 36, 255);
        }
        else
        {
            dialogGo = existing.gameObject;
        }

        dialogGo.SetActive(false);
        pauseCtrl.AutoWireArtifactDetailDialog();
        EditorUtility.SetDirty(dialogGo);
        EditorUtility.SetDirty(pauseCtrl);
    }
}
