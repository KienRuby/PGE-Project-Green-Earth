#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIDissolveController))]
public class UIDissolveControllerEditor : Editor
{
    private UIDissolveController controller;

    private void OnEnable()
    {
        controller = (UIDissolveController)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "UIDissolveController: Hệ thống hiệu ứng đóng UI dạng Dissolve / Disintegration Shader.\n" +
            "- Khóa click ngay lập tức khi Hide để chống click spam.\n" +
            "- Đồng bộ Screen-Space Noise toàn panel (không lệch pattern giữa các nút/chữ/icon).\n" +
            "- Tự động SetActive(false) sau khi shader tan rã 100%.",
            MessageType.Info);
        EditorGUILayout.Space(6);

        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Runtime / Editor Testing Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Show()", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                controller.Show();
            }
            else
            {
                controller.gameObject.SetActive(true);
                controller.ResetDissolve();
            }
        }

        if (GUILayout.Button("Test Hide()", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                controller.Hide();
            }
            else
            {
                Debug.Log("[UIDissolve] Để kiểm tra hoạt họa mượt mà đầy đủ Coroutine, vui lòng ấn Play Mode.");
            }
        }

        if (GUILayout.Button("Reset Dissolve", GUILayout.Height(30)))
        {
            controller.ResetDissolve();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Bake / Re-generate Noise Texture (512x512 Seamless)"))
        {
            UIDissolveNoiseGenerator.GenerateAndSaveTexture(512, 512, UIDissolveNoiseGenerator.NoiseTexturePath);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
