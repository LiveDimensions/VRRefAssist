using System;
using System.Diagnostics;
using UnityEditor;
using VRRefAssist.Editor.Exceptions;
using VRRefAssist.Editor.Extensions;

namespace VRRefAssist.Editor.Automation
{
    public static class RunOnBuildAutomation
    {
        [MenuItem("VR RefAssist/Tools/Run OnBuild Methods", priority = 200)]
        private static void ManuallyRunOnBuildMethods()
        {
            RunOnBuildMethods.CacheMonoInstances();

            RunOnBuildMethodsWithExecuteOrderType(ExecuteOrderType.PreFieldAutomation);
            RunOnBuildMethodsWithExecuteOrderType(ExecuteOrderType.PostFieldAutomation);
        }

        public static void RunOnBuildMethodsWithExecuteOrderType(ExecuteOrderType executeOrderType)
        {
            RunOnBuildMethodsWithExecuteOrderType(executeOrderType, out _);
        }

        public static void RunOnBuildMethodsWithExecuteOrderType(ExecuteOrderType executeOrderType, out bool cancelBuild)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            cancelBuild = false;

            var methods = executeOrderType == ExecuteOrderType.PreFieldAutomation ? RunOnBuildMethods.preFieldAutomationMethods : RunOnBuildMethods.postFieldAutomationMethods;

            string preOrPost = executeOrderType == ExecuteOrderType.PreFieldAutomation ? "pre" : "post";

            int count = 0;
            int total = methods.Count;

            foreach (var method in methods)
            {
                if (UnityEditorExtensions.DisplaySmartUpdatingCancellableProgressBar($"Running {preOrPost}-field automation OnBuild Methods...", count == total ? "Finishing..." : $"Progress: {count}/{total}.\tCurrent: {method.MethodInfo.Name}", count / (total - 1f)))
                {
                    EditorUtility.ClearProgressBar();

                    stopwatch.Stop();

                    cancelBuild = EditorUtility.DisplayDialog("Cancelled running instance OnBuild Methods", "You have canceled running instance OnBuild Methods\nDo you want to cancel the build as well?", "Cancel", "Continue");

                    if (!cancelBuild)
                    {
                        stopwatch.Start();
                        continue;
                    }

                    VRRADebugger.Log($"Ran {count}/{total} OnBuild Methods {preOrPost}-field automation in {stopwatch.Elapsed.TotalSeconds:F} seconds. Before it was cancelled");
                    EditorUtility.ClearProgressBar();
                    return;
                }

                if (!method.TryInvoke(out Exception e))
                {
                    stopwatch.Stop();

                    if (e is OnBuildException onBuildException)
                    {
                        if (onBuildException.logException)
                        {
                            UnityEngine.Debug.LogException(onBuildException);
                        }

                        if (onBuildException.showDialog)
                        {
                            switch (onBuildException.dialog.ShowDialog())
                            {
                                case Result.ContinueBuild:
                                    cancelBuild = false;
                                    break;
                                case Result.CancelBuild:
                                    cancelBuild = true;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    else
                    {
                        var exception = e.InnerException ?? e;
                        
                        cancelBuild = EditorUtility.DisplayDialog("Error running OnBuild Methods...",
                            $"An error occurred while running " + method.MethodInfo.Name + $".\n\n{exception.GetType()}: {exception.Message}\n\n{exception.StackTrace}\n\nDo you want to cancel the build?",
                            "Cancel", "Continue");
                    }

                    if (!cancelBuild)
                    {
                        stopwatch.Start();
                        continue;
                    }

                    VRRADebugger.Log($"Ran {count}/{total} OnBuild Methods {preOrPost}-field automation in {stopwatch.Elapsed.TotalSeconds:F} seconds. Before it was cancelled");
                    EditorUtility.ClearProgressBar();
                    return;
                }
                else
                {
                    count++;
                }
            }

            EditorUtility.ClearProgressBar();
            stopwatch.Stop();

            if (count > 0)
                VRRADebugger.Log($"Finished running {count}/{total} OnBuild Methods {preOrPost}-field automation in {stopwatch.Elapsed.TotalSeconds:F} seconds.");
        }

        public enum ExecuteOrderType
        {
            PreFieldAutomation,
            PostFieldAutomation
        }
    }
}