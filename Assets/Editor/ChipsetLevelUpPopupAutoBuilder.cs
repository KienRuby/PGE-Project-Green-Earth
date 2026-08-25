#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class ChipsetLevelUpPopupAutoBuilder
{
    static ChipsetLevelUpPopupAutoBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterReload;
    }

    private static void BuildOnceAfterReload()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += BuildOnceAfterReload;
            return;
        }

        GamePlayHUDSceneBuilder.BuildChipsetLevelUpPopupOnly();
        Debug.Log("[ChipsetLevelUpPopupAutoBuilder] One-shot popup build completed.");
    }
}
#endif
