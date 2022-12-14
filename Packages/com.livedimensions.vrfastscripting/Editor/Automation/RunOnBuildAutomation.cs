using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace VRFastScripting.Editor.Automation
{
    [InitializeOnLoad]
    public static class RunOnBuildAutomation
    {
        private static List<MethodInfo> runOnBuildMethods;
        private const BindingFlags runOnBuildBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static bool refreshingOnBuildMethods;

        private static DateTime runOnBuildStartTime;

        static RunOnBuildAutomation()
        {
            refreshingOnBuildMethods = true;

            new Thread(SearchAssemblyForOnBuildMethods).Start();
        }

        private static void SearchAssemblyForOnBuildMethods()
        {
            runOnBuildMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass).SelectMany(x => x.GetMethods(runOnBuildBindingFlags))
                .Where(x => x.GetCustomAttributes<RunOnBuildAttribute>().FirstOrDefault() != null)
                .OrderBy(x => x.GetCustomAttribute<RunOnBuildAttribute>().executionOrder).ToList();

            foreach (var onBuildMethod in runOnBuildMethods)
            {
                if (!onBuildMethod.IsStatic)
                    VRFSDebugger.LogError($"<b>[RunOnBuild]</b> is not compatible with Non Static methods! (<b>{onBuildMethod.Name}</b> in {onBuildMethod.DeclaringType})");
            }

            refreshingOnBuildMethods = false;
        }


        [MenuItem("VRFastScripting/Tools/Run OnBuild Methods")]
        private static void ManuallyRunOnBuildMethods()
        {
            RunOnBuildMethods();
        }

        public static bool RunOnBuildMethods()
        {
            //This should only happen if you request a build within less than a second after recompiling
            if (refreshingOnBuildMethods)
            {
                VRFSDebugger.LogError("Still refreshing OnBuild Methods!");
                return false;
            }

            if (!runOnBuildMethods.Any()) return true;

            runOnBuildStartTime = DateTime.Now;
            VRFSDebugger.Log($"Running {runOnBuildMethods.Count()} OnBuild Methods...");

            int count = 1;
            int total = runOnBuildMethods.Count();
            foreach (var method in runOnBuildMethods)
            {
                if (EditorUtility.DisplayCancelableProgressBar($"Running OnBuild Methods...", count == total ? "Finishing..." : $"Progress: {count}/{total}.\tCurrent: {method.Name}", count / (total - 1f)))
                {
                    EditorUtility.ClearProgressBar();
                    bool cancel = EditorUtility.DisplayDialog("Cancelled running OnBuild Methods", "You have canceled running OnBuild Methods\nDo you want to cancel the build as well?", "Cancel", "Continue");
                    return !cancel;
                }

                count++;

                try
                {
                    method.Invoke(null, null);
                }
                catch (Exception e)
                {
                    VRFSDebugger.LogError($"Error running OnBuild Method <b>{method.Name}</b> in {method.DeclaringType}:\n{e}");
                    Debug.LogException(e);

                    bool result = EditorUtility.DisplayDialog("Running OnBuild Methods...",
                        "An error occured while running " + method.Name + ".\n\n" + e.Message + "\n\n" + e.StackTrace,
                        "Continue", "Abort");

                    if (result) continue;
                    VRFSDebugger.Log($"Ran {count} OnBuild Methods in {(DateTime.Now - runOnBuildStartTime).TotalSeconds} seconds. Before it was cancelled");
                    EditorUtility.ClearProgressBar();
                    return false;
                }
            }

            VRFSDebugger.Log($"Finished running {runOnBuildMethods.Count()} OnBuild Methods in {(DateTime.Now - runOnBuildStartTime).TotalSeconds:F} seconds.");
            EditorUtility.ClearProgressBar();
            return true;
        }
    }

    #if VRC_SDK_VRCSDK3
    public class VRFS_VRCBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => -1;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar) return true;

            return RunOnBuildAutomation.RunOnBuildMethods();
        }
    }
    #endif
}