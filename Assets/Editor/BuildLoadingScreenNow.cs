#if UNITY_EDITOR
using UnityEditor;

public static class BuildLoadingScreenNow
{
    [InitializeOnLoadMethod]
    private static void Run()
    {
        LoadingScreenBuilder.BuildLoadingScreen();
    }
}
#endif
