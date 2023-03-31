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

                return RunOnBuildAutomation.RunOnBuildMethods();;
            }

            if (VRRefAssistSettings.GetOrCreateSettings().executeFieldAutomationWhenEnteringPlayMode)
            {
                FieldAutomation.ExecuteAllFieldAutomation();
            }
            
            if (VRRefAssistSettings.GetOrCreateSettings().executeRunOnBuildMethodsWhenEnteringPlayMode)
            {
                RunOnBuildAutomation.RunOnBuildMethods();
            }
            
            return true; //There is no way of cancelling entering play mode
        }
    }
}