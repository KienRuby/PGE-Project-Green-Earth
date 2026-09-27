#if UNITY_EDITOR
public static class BuildLoadingScreenNow
{
    public static void Run()
    {
        LoadingScreenBuilder.BuildLoadingScreen();
    }
}
#endif
