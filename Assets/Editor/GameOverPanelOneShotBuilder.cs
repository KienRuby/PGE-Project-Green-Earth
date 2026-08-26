#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameOverPanelOneShotBuilder
{
    static GameOverPanelOneShotBuilder()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Run;
            return;
        }

        PlayerRunEndSceneBuilder.BuildGameOverOnly();
        Debug.Log("[GameOverPanelOneShotBuilder] COMPLETE");
    }
}
#endif
