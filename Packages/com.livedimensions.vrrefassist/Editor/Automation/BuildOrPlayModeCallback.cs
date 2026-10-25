using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace VRRefAssist.Editor.Automation
{
    #if VRC_SDK_VRCSDK3
    internal class VRCBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => -1;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar) return true;

            //Some tools (e.g. VRCFury) invoke build requested callbacks after play mode has started, automation already ran on ExitingEditMode
            if (EditorApplication.isPlaying) return true;

            return BuildOrPlayModeCallback.ExecuteAutomation(true);
        }
    }
    #else
    internal class UnityBuildCallback : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!BuildOrPlayModeCallback.ExecuteAutomation(true))
            {
                throw new BuildFailedException("Stopping build as requested by VRRefAssist!");
            }
        }
    }
    #endif

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

            if(!ExecuteAutomation(false))
            {
                VRRADebugger.LogError("Automation failed, stopping play mode!");
                EditorApplication.isPlaying = false;
            }
        }

        public static bool ExecuteAutomation(bool buildRequested)
        {
            //Builds always run all automation, entering play mode respects the user's settings
            VRRefAssistSettings settings = VRRefAssistSettings.GetOrCreateSettings();
            bool executeRunOnBuildMethods = buildRequested || settings.executeRunOnBuildMethodsWhenEnteringPlayMode;
            bool executeFieldAutomation = buildRequested || settings.executeFieldAutomationWhenEnteringPlayMode;

            bool cancelBuild;

            if (executeRunOnBuildMethods)
            {
                RunOnBuildMethods.CacheMonoInstances();

                RunOnBuildAutomation.RunOnBuildMethodsWithExecuteOrderType(RunOnBuildAutomation.ExecuteOrderType.PreFieldAutomation, out cancelBuild);
                if (cancelBuild)
                {
                    return false;
                }
            }

            if (executeFieldAutomation)
            {
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
            }

            if (executeRunOnBuildMethods)
            {
                RunOnBuildAutomation.RunOnBuildMethodsWithExecuteOrderType(RunOnBuildAutomation.ExecuteOrderType.PostFieldAutomation, out cancelBuild);
                if (cancelBuild)
                {
                    return false;
                }
            }

            return true;
        }

        [MenuItem("VR RefAssist/Tools/Execute All Automation", priority = 100)]
        private static void ManuallyExecuteAllAutomation()
        {
            ExecuteAutomation(true);
        }
    }
}