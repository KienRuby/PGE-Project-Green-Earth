#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace PGE.EditorTools
{
    internal static class PGEAppleBuildPostprocessor
    {
        [PostProcessBuild(500)]
        private static void ConfigureAppleSignIn(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;
            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(frameworkTarget, "AuthenticationServices.framework", false);
            project.WriteToFile(projectPath);

            string entitlementPath = Path.Combine(buildPath, "Unity-iPhone/PGE.entitlements");
            var entitlements = new PlistDocument();
            if (File.Exists(entitlementPath)) entitlements.ReadFromFile(entitlementPath);
            PlistElementArray array = entitlements.root.CreateArray("com.apple.developer.applesignin");
            array.AddString("Default");
            entitlements.WriteToFile(entitlementPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            project.AddFile(entitlementPath, "PGE.entitlements");
            project.SetBuildProperty(mainTarget, "CODE_SIGN_ENTITLEMENTS", "Unity-iPhone/PGE.entitlements");
            project.WriteToFile(projectPath);
        }
    }
}
#endif
