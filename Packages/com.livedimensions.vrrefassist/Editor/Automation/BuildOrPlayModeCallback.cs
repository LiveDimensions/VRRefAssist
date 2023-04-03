using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;

namespace VRRefAssist.Editor.Automation
{
    internal class VRCBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => -1;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar) return true;

            return BuildOrPlayModeCallback.ExecuteAutomation(true);
        }
    }

    [InitializeOnLoad]
    public static class BuildOrPlayModeCallback
    {
        static BuildOrPlayModeCallback()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            ExecuteAutomation(false);
        }

        public static bool ExecuteAutomation(bool buildRequested)
        {
            if (buildRequested)
            {
                FieldAutomation.ExecuteAllFieldAutomation();
                SingletonAutomation.SetAllSingletonReferences();

                return RunOnBuildAutomation.RunOnBuildMethods();;
            }

            if (VRRefAssistSettings.GetOrCreateSettings().executeFieldAutomationWhenEnteringPlayMode)
            {
                FieldAutomation.ExecuteAllFieldAutomation();
                SingletonAutomation.SetAllSingletonReferences();
            }
            
            if (VRRefAssistSettings.GetOrCreateSettings().executeRunOnBuildMethodsWhenEnteringPlayMode)
            {
                RunOnBuildAutomation.RunOnBuildMethods();
            }
            
            return true; //There is no way of cancelling entering play mode
        }

        [MenuItem("VR RefAssist/Tools/Execute All Automation", priority = 100)]
        private static void ManuallyExecuteAllAutomation()
        {
            ExecuteAutomation(true);
        }
    }
}