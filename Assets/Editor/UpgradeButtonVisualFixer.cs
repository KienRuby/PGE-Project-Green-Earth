#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UpgradeButtonVisualFixer
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string NunitoFontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string MaterialPath = "Assets/Fonts/Nunito/Nunito SDF - UpgradeButton.mat";
    private const string ResourceIconAtlasPath = "Assets/Sprites/UI/icon tài nguyên.png";
    private const string ResultImagePath = "C:/Users/ADMIN/.gemini/antigravity/brain/2fab4507-e897-4e8e-a0d6-873fe43bce5d/scratch/unity_upgrade_button_result.png";

    [MenuItem("PGE/UI/Fix Upgrade Button (Match Mockup)")]
    public static void ApplyFix()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        TMP_FontAsset nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NunitoFontPath);
        Material upgradeMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Sprite dataChipSprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(ResourceIconAtlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "data");

        if (nunitoFont == null)
        {
            Debug.LogError($"[UpgradeButtonVisualFixer] Nunito font not found at {NunitoFontPath}");
            return;
        }

        if (upgradeMat == null)
        {
            Debug.LogError($"[UpgradeButtonVisualFixer] Material not found at {MaterialPath}");
            return;
        }

        // Find UpgradeButton
        Button[] allButtons = UnityEngine.Object.FindObjectsOfType<Button>(true);
        Button upgradeBtn = allButtons.FirstOrDefault(b => b.name == "UpgradeButton" && b.transform.parent != null && (b.transform.parent.name == "StatsPanel" || b.transform.parent.name == "Panel_Stats"));

        if (upgradeBtn == null)
        {
            Debug.LogError("[UpgradeButtonVisualFixer] UpgradeButton not found in StatsPanel!");
            return;
        }

        RectTransform upgradeRect = upgradeBtn.GetComponent<RectTransform>();
        Undo.RecordObject(upgradeBtn.gameObject, "Fix Upgrade Button Layout");

        // 1. UpgradeText
        Transform upgradeTextTform = upgradeRect.Find("UpgradeText");
        if (upgradeTextTform != null)
        {
            Undo.RecordObject(upgradeTextTform.gameObject, "Fix UpgradeText");
            TextMeshProUGUI upgradeTmp = upgradeTextTform.GetComponent<TextMeshProUGUI>();
            if (upgradeTmp != null)
            {
                Undo.RecordObject(upgradeTmp, "Fix UpgradeText TMP");
                upgradeTmp.font = nunitoFont;
                upgradeTmp.fontSharedMaterial = upgradeMat;
                upgradeTmp.text = "Upgrade";
                upgradeTmp.fontSize = 48f;
                upgradeTmp.fontStyle = FontStyles.Bold;
                upgradeTmp.color = Color.white;
                upgradeTmp.alignment = TextAlignmentOptions.Center;
                upgradeTmp.enableWordWrapping = false;
                upgradeTmp.overflowMode = TextOverflowModes.Overflow;
                upgradeTmp.raycastTarget = false;
            }

            RectTransform textRt = upgradeTextTform.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0f, 0.46f);
                textRt.anchorMax = new Vector2(1f, 0.94f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = Vector2.zero;
                textRt.sizeDelta = Vector2.zero;
            }
        }

        // 2. PriceGroup container
        Transform priceGroupTform = upgradeRect.Find("PriceGroup");
        GameObject priceGroupObj;
        if (priceGroupTform == null)
        {
            priceGroupObj = new GameObject("PriceGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            priceGroupObj.transform.SetParent(upgradeRect, false);
            Undo.RegisterCreatedObjectUndo(priceGroupObj, "Create PriceGroup");
        }
        else
        {
            priceGroupObj = priceGroupTform.gameObject;
            Undo.RecordObject(priceGroupObj, "Configure PriceGroup");
        }

        RectTransform groupRt = priceGroupObj.GetComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(0.5f, 0.30f);
        groupRt.anchorMax = new Vector2(0.5f, 0.30f);
        groupRt.pivot = new Vector2(0.5f, 0.5f);
        groupRt.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup hlg = priceGroupObj.GetComponent<HorizontalLayoutGroup>() ?? priceGroupObj.AddComponent<HorizontalLayoutGroup>();
        Undo.RecordObject(hlg, "Configure HLG");
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 14f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = priceGroupObj.GetComponent<ContentSizeFitter>() ?? priceGroupObj.AddComponent<ContentSizeFitter>();
        Undo.RecordObject(csf, "Configure CSF");
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 3. PriceText inside PriceGroup
        Transform priceTextTform = upgradeRect.Find("PriceText") ?? priceGroupObj.transform.Find("PriceText");
        TextMeshProUGUI priceTmp = null;
        if (priceTextTform != null)
        {
            Undo.SetTransformParent(priceTextTform, groupRt, "Parent PriceText to PriceGroup");
            priceTextTform.SetSiblingIndex(0);

            priceTmp = priceTextTform.GetComponent<TextMeshProUGUI>();
            if (priceTmp != null)
            {
                Undo.RecordObject(priceTmp, "Fix PriceText TMP");
                priceTmp.font = nunitoFont;
                priceTmp.fontSharedMaterial = upgradeMat;
                if (string.Equals(priceTmp.text, "300", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(priceTmp.text))
                {
                    priceTmp.text = "17.700";
                }
                priceTmp.fontSize = 40f;
                priceTmp.fontStyle = FontStyles.Bold;
                priceTmp.color = Color.white;
                priceTmp.alignment = TextAlignmentOptions.Center;
                priceTmp.enableWordWrapping = false;
                priceTmp.overflowMode = TextOverflowModes.Overflow;
                priceTmp.raycastTarget = false;
                priceTmp.margin = Vector4.zero;
            }

            ContentSizeFitter priceCsf = priceTextTform.GetComponent<ContentSizeFitter>() ?? priceTextTform.gameObject.AddComponent<ContentSizeFitter>();
            priceCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            priceCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // 4. CurrencyIcon inside PriceGroup
        Transform iconTform = upgradeRect.Find("CurrencyIcon") ?? priceGroupObj.transform.Find("CurrencyIcon");
        if (iconTform != null)
        {
            Undo.SetTransformParent(iconTform, groupRt, "Parent CurrencyIcon to PriceGroup");
            iconTform.SetSiblingIndex(1);

            Image iconImg = iconTform.GetComponent<Image>();
            if (iconImg != null)
            {
                Undo.RecordObject(iconImg, "Fix CurrencyIcon Image");
                if (dataChipSprite != null) iconImg.sprite = dataChipSprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            RectTransform iconRt = iconTform.GetComponent<RectTransform>();
            if (iconRt != null)
            {
                iconRt.sizeDelta = new Vector2(46f, 46f);
            }
        }

        // 5. Connect controller references
        LabUpgradeController controller = UnityEngine.Object.FindObjectOfType<LabUpgradeController>(true);
        if (controller != null && priceTmp != null)
        {
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("priceText").objectReferenceValue = priceTmp;
            so.FindProperty("upgradeButton").objectReferenceValue = upgradeBtn;
            Image btnBg = upgradeBtn.GetComponent<Image>();
            if (btnBg != null) so.FindProperty("upgradeBackground").objectReferenceValue = btnBg;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[UpgradeButtonVisualFixer] Successfully applied visual fixes to UpgradeButton!");

        // Render preview image of the button to file
        RenderButtonPreview(upgradeRect);
    }

    private static void RenderButtonPreview(RectTransform buttonRect)
    {
        try
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);

            Canvas canvas = buttonRect.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            int width = 640;
            int height = 320;
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

            GameObject camObj = new GameObject("TempPreviewCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.08f, 0.2f, 1f);
            cam.orthographic = true;
            cam.targetTexture = rt;

            Vector3 worldPos = buttonRect.position;
            cam.transform.position = new Vector3(worldPos.x, worldPos.y, -10f);
            cam.orthographicSize = (buttonRect.rect.height * 1.3f * buttonRect.lossyScale.y) / 2f;

            cam.Render();

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camObj);

            string outDir = Path.GetDirectoryName(ResultImagePath);
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            File.WriteAllBytes(ResultImagePath, bytes);
            Debug.Log($"[UpgradeButtonVisualFixer] Successfully rendered preview to {ResultImagePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UpgradeButtonVisualFixer] Preview render exception: " + ex);
        }
    }
}
#endif
