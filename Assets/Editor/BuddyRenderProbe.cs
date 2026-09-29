using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class BuddyRenderProbe
{
    private static bool loggedEmptyScene;

    static BuddyRenderProbe()
    {
        Debug.Log($"[BuddyRenderProbe] installed, isPlaying={EditorApplication.isPlaying}");
        EditorApplication.update += InspectOnceInPlayMode;
    }

    private static void InspectOnceInPlayMode()
    {
        if (!EditorApplication.isPlaying) return;
        BuddyCombatDrone[] drones = Object.FindObjectsOfType<BuddyCombatDrone>();
        if (drones.Length == 0)
        {
            if (!loggedEmptyScene)
            {
                Debug.Log($"[BuddyRenderProbe] play scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, droneCount=0");
                loggedEmptyScene = true;
            }
            return;
        }
        Debug.Log($"[BuddyRenderProbe] play scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, droneCount={drones.Length}");

        foreach (BuddyCombatDrone drone in drones)
        {
            SpriteRenderer renderer = drone.GetComponent<SpriteRenderer>();
            Material material = renderer != null ? renderer.sharedMaterial : null;
            Sprite sprite = renderer != null ? renderer.sprite : null;
            Debug.Log($"[BuddyRenderProbe] {drone.name}: sprite={(sprite != null ? sprite.name : "null")}, texture={(sprite != null && sprite.texture != null ? sprite.texture.name : "null")}, material={(material != null ? material.name : "null")}, shader={(material != null && material.shader != null ? material.shader.name : "null")}, supported={material != null && material.shader != null && material.shader.isSupported}, color={(renderer != null ? renderer.color.ToString() : "null")}, visible={renderer != null && renderer.isVisible}");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        SpriteRenderer playerRenderer = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        Material playerMaterial = playerRenderer != null ? playerRenderer.sharedMaterial : null;
        Debug.Log($"[BuddyRenderProbe] player renderer: sprite={(playerRenderer != null && playerRenderer.sprite != null ? playerRenderer.sprite.name : "null")}, material={(playerMaterial != null ? playerMaterial.name : "null")}, shader={(playerMaterial != null && playerMaterial.shader != null ? playerMaterial.shader.name : "null")}");
        EditorApplication.update -= InspectOnceInPlayMode;
    }
}
