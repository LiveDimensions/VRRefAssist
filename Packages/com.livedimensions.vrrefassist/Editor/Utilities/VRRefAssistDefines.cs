
using UnityEditor;
using UnityEditor.Compilation;

namespace VRRefAssist.Editor.Utilities
{
    internal static class VRRefAssistDefines
    {
        private const string _PACKAGE_DEFINE_SYMBOL = "VR_REF_ASSIST";

        [InitializeOnLoadMethod]
        internal static void UpdatePackageDefines()
        {
            bool definesChanged = false;
            definesChanged |= TryAddDefine(_PACKAGE_DEFINE_SYMBOL);

            if (!definesChanged) return;
            
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }

        private static bool TryAddDefine(string define)
        {
            BuildTargetGroup platform = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(platform);
            
            if (defines.Contains(define)) return false;
            if (defines.Length > 0) defines += ";";
            defines += define;
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(platform, defines);
            
            return true;
        }

        private static bool TryRemoveDefine(string define)
        {
            BuildTargetGroup platform = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(platform);
            
            if (!defines.Contains(define)) return false;
            defines = defines.Replace(define + ";", "");
            defines = defines.Replace(define, "");
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(platform, defines);
            
            return true;
        }
    }
}
