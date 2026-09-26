#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RobotPetInfoSetupEditor
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("PGE/UI/Setup Robot Pet Info Button & Modal")]
    public static void SetupPetInfoMenuItem()
    {
        SetupPetInfoButtonAndModal(true);
    }

    [MenuItem("PGE/UI/Reset Lab Grid Position")]
    public static void ResetLabGridMenuItem()
    {
        FixLabGridPosition(true);
    }

    public static void FixLabGridPosition(bool forceLog)
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != MainMenuScenePath)
        {
            currentScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        Transform content = GameObject.Find("Canvas/Content")?.transform;
        if (content == null)
        {
            Canvas c = Object.FindObjectsOfType<Canvas>(true).FirstOrDefault(cnv => cnv.name == "Canvas");
            if (c != null) content = c.transform.Find("Content");
        }

        if (content != null)
        {
            Transform labPanel = content.Find("LabPanel");
            if (labPanel != null)
            {
                Transform statsPanel = labPanel.Find("StatsPanel");
                if (statsPanel != null)
                {
                    Transform upgradeGrid = statsPanel.Find("UpgradeGrid");
                    if (upgradeGrid != null)
                    {
                        RectTransform rt = upgradeGrid.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchorMin = new Vector2(0.5f, 1f);
                            rt.anchorMax = new Vector2(0.5f, 1f);
                            rt.pivot = new Vector2(0.5f, 1f);
                            rt.anchoredPosition = new Vector2(0f, -10f);
                            rt.sizeDelta = new Vector2(960f, 950f);
                            EditorUtility.SetDirty(rt);
                            EditorSceneManager.MarkSceneDirty(currentScene);
                            EditorSceneManager.SaveScene(currentScene);
                            if (forceLog) Debug.Log("[RobotPetInfoSetupEditor] Successfully restored UpgradeGrid position to (0, -10)!");
                        }
                    }
                }
            }
        }
    }

    public static void SetupPetInfoButtonAndModal(bool forceLog)
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != MainMenuScenePath)
        {
            currentScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = Object.FindObjectsOfType<Canvas>(true).FirstOrDefault(c => c.name == "Canvas");
        if (canvas == null)
        {
            if (forceLog) Debug.LogError("[RobotPetInfoSetupEditor] Cannot find Canvas in MainMenu scene!");
            return;
        }

        // 1. Ensure Modal in Canvas & Remove Button_X if present
        RobotPetInfoModalController modalCtrl = RobotPetInfoModalController.EnsureModalInCanvas(canvas);
        if (modalCtrl != null)
        {
            Transform bx = modalCtrl.transform.Find("ContentPanel/Button_X");
            if (bx != null)
            {
                Undo.DestroyObjectImmediate(bx.gameObject);
                if (forceLog) Debug.Log("[RobotPetInfoSetupEditor] Successfully deleted Button_X from RobotPetInfoModal!");
            }
        }

        // 2. Find RobotPetController & PetDetailPanel
        RobotPetController petCtrl = Object.FindObjectsOfType<RobotPetController>(true).FirstOrDefault();
        if (petCtrl == null)
        {
            if (forceLog) Debug.LogError("[RobotPetInfoSetupEditor] Cannot find RobotPetController in scene!");
            return;
        }

        Transform detailPanel = petCtrl.transform.Find("PetDetailPanel");
        if (detailPanel == null && petCtrl.detailPanel != null)
        {
            detailPanel = petCtrl.detailPanel.transform;
        }

        if (detailPanel == null)
        {
            if (forceLog) Debug.LogError("[RobotPetInfoSetupEditor] Cannot find PetDetailPanel under RobotPetController!");
            return;
        }

        // 3. Find or Create Button_Info
        Transform infoBtnTrans = detailPanel.Find("Button_Info");
        GameObject infoBtnObj;
        if (infoBtnTrans == null)
        {
            infoBtnObj = new GameObject("Button_Info", typeof(RectTransform), typeof(Image), typeof(Button));
            infoBtnObj.transform.SetParent(detailPanel, false);
            Undo.RegisterCreatedObjectUndo(infoBtnObj, "Create Pet Info Button");
        }
        else
        {
            infoBtnObj = infoBtnTrans.gameObject;
        }

        RectTransform rt = infoBtnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(43.25f, -40f);
        rt.sizeDelta = new Vector2(52f, 52f);

        Image img = infoBtnObj.GetComponent<Image>();
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Shop/icon_info_cyan.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/RobotPet_Sliced/Icon_Info.png");
        img.sprite = s;
        img.preserveAspect = true;
        img.raycastTarget = true;

        Button btn = infoBtnObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        // 4. Wire reference to RobotPetController
        SerializedObject so = new SerializedObject(petCtrl);
        SerializedProperty infoProp = so.FindProperty("infoButton");
        if (infoProp != null)
        {
            infoProp.objectReferenceValue = btn;
            so.ApplyModifiedProperties();
        }

        petCtrl.infoButton = btn;
        petCtrl.SetupButtonListeners();

        EditorUtility.SetDirty(infoBtnObj);
        EditorUtility.SetDirty(petCtrl.gameObject);
        if (modalCtrl != null) EditorUtility.SetDirty(modalCtrl.gameObject);
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        if (forceLog)
        {
            Debug.Log("[RobotPetInfoSetupEditor] Successfully created and wired Pet Button_Info & RobotPetInfoModal!");
        }
    }
}
#endif
