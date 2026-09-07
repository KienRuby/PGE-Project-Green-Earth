using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PGE.EditorTools
{
    [InitializeOnLoad]
    public class TopHighlightCleaner : UnityEditor.AssetModificationProcessor
    {
        static TopHighlightCleaner()
        {
            EditorApplication.delayCall += CleanActiveSceneSilently;
        }

        [MenuItem("PGE/UI/Remove All TopHighlight Objects")]
        public static void RemoveAllTopHighlightsMenu()
        {
            int totalRemoved = CleanAllScenes();
            EditorUtility.DisplayDialog("TopHighlight Cleanup", $"Đã tìm và xóa thành công {totalRemoved} object TopHighlight trong toàn bộ dự án!", "OK");
        }

        public static int CleanAllScenes()
        {
            int total = 0;

            // 1. Dọn dẹp scene đang active
            Scene activeScene = SceneManager.GetActiveScene();
            total += CleanScene(activeScene);

            // 2. Dọn dẹp các scene khác nếu có trong Assets/Scenes
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == activeScene.path) continue;

                Scene s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                total += CleanScene(s);
                EditorSceneManager.CloseScene(s, true);
            }

            if (total > 0)
            {
                Debug.Log($"[TopHighlightCleaner] ✅ Đã dọn sạch {total} object TopHighlight trong toàn bộ scene!");
            }
            return total;
        }

        private static void CleanActiveSceneSilently()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                int removed = CleanScene(activeScene);
                if (removed > 0)
                {
                    Debug.Log($"[TopHighlightCleaner] ✅ Tự động dọn dẹp {removed} object TopHighlight trong scene {activeScene.name}!");
                }
            }
        }

        public static int CleanScene(Scene scene)
        {
            if (!scene.isLoaded) return 0;

            List<GameObject> toDestroy = new List<GameObject>();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                FindTopHighlightsRecursive(root.transform, toDestroy);
            }

            if (toDestroy.Count == 0) return 0;

            foreach (GameObject go in toDestroy)
            {
                if (go != null)
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return toDestroy.Count;
        }

        private static void FindTopHighlightsRecursive(Transform parent, List<GameObject> results)
        {
            if (parent == null) return;

            if (parent.name.StartsWith("TopHighlight", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(parent.gameObject);
                return; // Xóa cả nhánh con nếu có
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                FindTopHighlightsRecursive(parent.GetChild(i), results);
            }
        }

        // Quy tắc vĩnh viễn: Nghiêm cấm lưu bất kỳ GameObject TopHighlight nào vào Scene sau này
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths == null || paths.Length == 0) return paths;

            bool hasScene = false;
            foreach (string p in paths)
            {
                if (p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    hasScene = true;
                    break;
                }
            }

            if (hasScene)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    int removed = CleanScene(activeScene);
                    if (removed > 0)
                    {
                        Debug.LogWarning($"[TopHighlightEnforcer] ⚠️ Phát hiện và tự động loại bỏ {removed} object TopHighlight trước khi lưu scene theo quy chuẩn dự án!");
                    }
                }
            }

            return paths;
        }
    }
}
