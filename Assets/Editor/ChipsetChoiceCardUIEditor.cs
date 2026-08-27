using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ChipsetChoiceCardUI)), CanEditMultipleObjects]
public class ChipsetChoiceCardUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Level Sprite Objects", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bật Use Manual Icon Transform để giữ RectTransform của ChipIcon. Tạo RuntimeLevelPip_1-5 để chỉnh riêng từng sprite cấp.",
            MessageType.Info);

        if (GUILayout.Button("Đặt lại RectTransform của ChipIcon"))
        {
            foreach (Object selectedObject in targets)
            {
                ChipsetChoiceCardUI card = selectedObject as ChipsetChoiceCardUI;
                if (card == null) continue;

                card.ResetEditableIconTransformInEditor();
                MarkSceneDirty(card);
            }
        }

        if (GUILayout.Button("Tạo / hiện Sprite Cấp 1-5"))
        {
            ChipsetLevelVisualLibrary library = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
            if (library == null)
            {
                Debug.LogError("Không tìm thấy Resources/ChipsetLevelVisualLibrary.asset.");
                return;
            }

            foreach (Object selectedObject in targets)
            {
                ChipsetChoiceCardUI card = selectedObject as ChipsetChoiceCardUI;
                if (card == null) continue;

                card.CreateEditableLevelPipsInEditor(library.levelPipSprites);
                MarkSceneDirty(card);
            }
        }

        if (GUILayout.Button("Đặt lại vị trí Sprite Cấp theo atlas"))
        {
            foreach (Object selectedObject in targets)
            {
                ChipsetChoiceCardUI card = selectedObject as ChipsetChoiceCardUI;
                if (card == null) continue;

                card.ResetEditableLevelPipTransformsInEditor();
                MarkSceneDirty(card);
            }
        }
    }

    private static void MarkSceneDirty(ChipsetChoiceCardUI card)
    {
        EditorUtility.SetDirty(card);
        if (!Application.isPlaying && card.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(card.gameObject.scene);
        }
    }
}
