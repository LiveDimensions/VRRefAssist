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
            //
            //BUILD REQUESTED
            //
            if (buildRequested)
            {
                RunOnBuildMethods.CacheUSharpInstances();

                RunOnBuildAutomation.RunOnBuildMethodsWithExecuteOrderType(RunOnBuildAutomation.ExecuteOrderType.PreFieldAutomation, out bool cancelBuild);
                if (cancelBuild)
                {
                    return false;
                }

                FieldAutomation.ExecuteAllFieldAutomation(out cancelBuild);
                if (cancelBuild)
                {
                    return false;
                }

                SingletonAutomation.SetAllSingletonReferences(out cancelBuild);
                if (cancelBuild)
                {
                    return false;
                }


                RunOnBuildAutomation.RunOnBuildMethodsWithExecuteOrderType(RunOnBuildAutomation.ExecuteOrderType.PostFieldAutomation, out cancelBuild);
                if (cancelBuild)
                {
                    return false;
                }

                return true;
            }

            //
            //ENTERING PLAY MODE
            //
            bool executeRunOnBuildMethodsWhenEnteringPlayMode = VRRefAssistSettings.GetOrCreateSettings().executeRunOnBuildMethodsWhenEnteringPlayMode;

            if (executeRunOnBuildMethodsWhenEnteringPlayMode)
            {
                RunOnBuildMethods.CacheUSharpInstances();
                RunOnBuildAutomation.RunOnBuildMethodsWithExecuteOrderType(RunOnBuildAutomation.ExecuteOrderType.PreFieldAutomation);
            }

            if (VRRefAssistSettings.GetOrCreateSettings().executeFieldAutomationWhenEnteringPlayMode)
            {
                FieldAutomation.ExecuteAllFieldAutomation();
                SingletonAutomation.SetAllSingletonReferences();
            }

            if (executeRunOnBuildMethodsWhenEnteringPlayMode)
            {
                RunOnBuildAutomation.RunOnBuildMethodsWithExecuteOrderType(RunOnBuildAutomation.ExecuteOrderType.PostFieldAutomation);
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