#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckAndSyncPlayerSortingLayers
{
    private const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private static readonly string[] ScenePaths = new string[]
    {
        "Assets/Scenes/GamePlay.unity",
        "Assets/Scenes/GenMine.unity"
    };

    private const string ReportPath = "Reports/player_sorting_layers_report.txt";

    public struct SpriteRendererInfo
    {
        public string relativePath;
        public string sortingLayerName;
        public int sortingLayerID;
        public int sortingOrder;
        public bool enabled;
        public string spriteName;
    }

    [MenuItem("PGE/Player/Check and Sync Player Sorting Layers")]
    public static void RunCheckAndSync()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("================================================================================");
        report.AppendLine("             PLAYER SORTING LAYERS CHECK AND SYNCHRONIZATION REPORT             ");
        report.AppendLine($"                     Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}                     ");
        report.AppendLine("================================================================================\n");

        // 1. Inspect Prefab (Source of Truth)
        GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabObj == null)
        {
            report.AppendLine($"[ERROR] Cannot load Player prefab at {PrefabPath}!");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.LogError(report.ToString());
            return;
        }

        Dictionary<string, SpriteRendererInfo> prefabRenderers = CollectSpriteRenderers(prefabObj.transform);
        report.AppendLine($"--- 1. PLAYER PREFAB (Source of Truth: {PrefabPath}) ---");
        report.AppendLine($"Total SpriteRenderers found: {prefabRenderers.Count}\n");
        report.AppendLine(string.Format("{0,-40} | {1,-15} | {2,-12} | {3,-6} | {4}", "Path", "SortingLayer", "LayerID", "Order", "Sprite"));
        report.AppendLine(new string('-', 95));

        foreach (var kvp in prefabRenderers)
        {
            var info = kvp.Value;
            report.AppendLine(string.Format("{0,-40} | {1,-15} | {2,-12} | {3,-6} | {4}",
                info.relativePath, info.sortingLayerName, info.sortingLayerID, info.sortingOrder, info.spriteName));
        }
        report.AppendLine();

        // 2. Check and Sync each Scene
        foreach (string scenePath in ScenePaths)
        {
            report.AppendLine("--------------------------------------------------------------------------------");
            report.AppendLine($"--- 2. SCENE: {scenePath} ---");
            report.AppendLine("--------------------------------------------------------------------------------");

            Scene activeScene = SceneManager.GetActiveScene();
            bool wasActive = string.Equals(activeScene.path, scenePath, System.StringComparison.OrdinalIgnoreCase);
            Scene scene = wasActive ? activeScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                GameObject scenePlayer = FindPlayerInScene(scene);
                if (scenePlayer == null)
                {
                    report.AppendLine($"[WARNING] No Player GameObject found in scene: {scenePath}");
                    continue;
                }

                Dictionary<string, SpriteRendererInfo> sceneRenderers = CollectSpriteRenderers(scenePlayer.transform);
                report.AppendLine($"Total SpriteRenderers on Player in scene: {sceneRenderers.Count}\n");

                bool sceneModified = false;
                List<string> syncedList = new List<string>();
                List<string> matchedList = new List<string>();
                List<string> missingInPrefab = new List<string>();

                // Get all SpriteRenderers on scene Player as components
                SpriteRenderer[] allSceneSrs = scenePlayer.GetComponentsInChildren<SpriteRenderer>(true);

                foreach (SpriteRenderer sr in allSceneSrs)
                {
                    string path = GetRelativePath(scenePlayer.transform, sr.transform);
                    if (prefabRenderers.TryGetValue(path, out SpriteRendererInfo targetInfo))
                    {
                        bool layerMismatch = sr.sortingLayerID != targetInfo.sortingLayerID;
                        bool orderMismatch = sr.sortingOrder != targetInfo.sortingOrder;

                        if (layerMismatch || orderMismatch)
                        {
                            report.AppendLine($"[SYNCING] {path}:");
                            report.AppendLine($"    BEFORE: Layer={sr.sortingLayerName} (ID={sr.sortingLayerID}), Order={sr.sortingOrder}");
                            report.AppendLine($"    AFTER:  Layer={targetInfo.sortingLayerName} (ID={targetInfo.sortingLayerID}), Order={targetInfo.sortingOrder}");

                            Undo.RecordObject(sr, "Sync Sorting Layer");
                            sr.sortingLayerID = targetInfo.sortingLayerID;
                            sr.sortingOrder = targetInfo.sortingOrder;
                            EditorUtility.SetDirty(sr);
                            sceneModified = true;
                            syncedList.Add(path);
                        }
                        else
                        {
                            matchedList.Add(path);
                        }
                    }
                    else
                    {
                        missingInPrefab.Add(path);
                    }
                }

                // Check if any prefab renderer is missing on scene Player
                List<string> missingInScene = new List<string>();
                foreach (var kvp in prefabRenderers)
                {
                    if (!sceneRenderers.ContainsKey(kvp.Key))
                    {
                        missingInScene.Add(kvp.Key);
                    }
                }

                report.AppendLine($"Summary for {System.IO.Path.GetFileName(scenePath)}:");
                report.AppendLine($"  - Matched & In Sync: {matchedList.Count}");
                report.AppendLine($"  - Synchronized (Changed): {syncedList.Count}");
                if (missingInPrefab.Count > 0)
                {
                    report.AppendLine($"  - In Scene but not in Prefab ({missingInPrefab.Count}): {string.Join(", ", missingInPrefab)}");
                }
                if (missingInScene.Count > 0)
                {
                    report.AppendLine($"  - In Prefab but missing in Scene ({missingInScene.Count}): {string.Join(", ", missingInScene)}");
                }

                if (sceneModified)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine($"  -> Saved scene changes to {scenePath}.");
                }
                else
                {
                    report.AppendLine($"  -> Scene was already in sync.");
                }
                report.AppendLine();
            }
            finally
            {
                if (!wasActive && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        report.AppendLine("================================================================================");
        report.AppendLine("                             SYNCHRONIZATION COMPLETE                           ");
        report.AppendLine("================================================================================");

        Directory.CreateDirectory("Reports");
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log($"<color=green>[CheckAndSyncPlayerSortingLayers] Completed! Report saved to {ReportPath}</color>");
    }

    private static GameObject FindPlayerInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Player") return root;
            PlayerChipsetSkillManager manager = root.GetComponentInChildren<PlayerChipsetSkillManager>(true);
            if (manager != null) return manager.gameObject;
        }
        return null;
    }

    private static Dictionary<string, SpriteRendererInfo> CollectSpriteRenderers(Transform root)
    {
        var dict = new Dictionary<string, SpriteRendererInfo>();
        SpriteRenderer[] srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in srs)
        {
            string relPath = GetRelativePath(root, sr.transform);
            dict[relPath] = new SpriteRendererInfo
            {
                relativePath = relPath,
                sortingLayerName = sr.sortingLayerName,
                sortingLayerID = sr.sortingLayerID,
                sortingOrder = sr.sortingOrder,
                enabled = sr.enabled,
                spriteName = sr.sprite != null ? sr.sprite.name : "null"
            };
        }
        return dict;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == target) return target.name;
        List<string> parts = new List<string>();
        Transform curr = target;
        while (curr != null && curr != root)
        {
            parts.Add(curr.name);
            curr = curr.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            RunCheckAndSync();
        };
    }
}
#endif
