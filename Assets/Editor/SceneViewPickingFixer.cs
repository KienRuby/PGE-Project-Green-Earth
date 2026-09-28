using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewPickingFixer
{
    static SceneViewPickingFixer()
    {
        EditorApplication.delayCall += UnlockAll;
    }

    [MenuItem("Tools/Mở khóa chọn Scene View (Unlock Picking)")]
    public static void UnlockAll()
    {
        // 1. Mở khóa toàn bộ GameObjects bị tắt Picking trong Scene view (Hierarchy finger icon)
        SceneVisibilityManager.instance.EnableAllPicking();
        SceneVisibilityManager.instance.ShowAll();

        // 2. Mở khóa toàn bộ Layers (Layers dropdown ở góc trên bên phải Unity)
        Tools.lockedLayers = 0;
        Tools.visibleLayers = -1;

        Debug.Log("<color=#22C55E><b>[SceneViewPickingFixer]</b> Đã mở khóa chọn (Picking) cho tất cả đối tượng và Layers trong Scene View thành công!</color>");
    }
}
