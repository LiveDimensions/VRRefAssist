using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRRefAssist.Editor.Extensions;

namespace VRRefAssist.Editor.Automation
{
    [InitializeOnLoad]
    public static class RunOnBuildMethods
    {
        private static readonly Dictionary<Type, List<UdonSharpBehaviour>> uSharpTypeInstances = new Dictionary<Type, List<UdonSharpBehaviour>>();

        public static List<RunOnBuildMethod> preFieldAutomationMethods = new List<RunOnBuildMethod>();
        public static List<RunOnBuildMethod> postFieldAutomationMethods = new List<RunOnBuildMethod>();

        public static bool refreshingOnBuildMethods;

        static RunOnBuildMethods()
        {
            refreshingOnBuildMethods = true;
            new Thread(RegisterRunOnBuildMethods).Start();
        }

        private static void RegisterRunOnBuildMethods()
        {
            var allMethods = TypeCache.GetMethodsWithAttribute<RunOnBuildAttribute>()
                .GroupBy(m => m.IsStatic)
                .ToDictionary(x => x.Key, z => z.ToList());

            List<StaticRunOnBuildMethod> staticMethods = new List<StaticRunOnBuildMethod>();
            List<InstanceRunOnBuildMethod> instanceMethods = new List<InstanceRunOnBuildMethod>();

            if (allMethods.ContainsKey(true))
                staticMethods = allMethods[true].Select(m => new StaticRunOnBuildMethod(m)).ToList();

            if (allMethods.ContainsKey(false))
                instanceMethods = allMethods[false].Select(m => new InstanceRunOnBuildMethod(m)).ToList();

            uSharpTypeInstances.Clear();

            foreach (var instanceMethod in instanceMethods)
            {
                if (!instanceMethod.declaringType.IsSubclassOf(typeof(UdonSharpBehaviour)))
                {
                    VRRADebugger.LogError($"RunOnBuild method {instanceMethod.MethodInfo.Name} in {instanceMethod.declaringType.Name} is not subclass of MonoBehaviour!");
                    continue;
                }

                uSharpTypeInstances.Add(instanceMethod.declaringType, new List<UdonSharpBehaviour>());
                
                var inheritedTypes = TypeCache.GetTypesDerivedFrom(instanceMethod.declaringType).Where(t => !t.IsAbstract).ToList();
                
                foreach (var inheritedType in inheritedTypes)
                {
                    if (uSharpTypeInstances.ContainsKey(inheritedType)) continue;
                    
                    uSharpTypeInstances.Add(inheritedType, new List<UdonSharpBehaviour>());
                }
            }

            var staticAndInstanceMethods = staticMethods.OfType<RunOnBuildMethod>().Concat(instanceMethods).ToList();

            //Split into pre and post field automation methods (execution order <= 1000 is pre, > 1000 is post)
            var preAndPostSplit = staticAndInstanceMethods.GroupBy(m => m.attribute.executionOrder <= 1000).ToDictionary(x => x.Key, z => z.ToList());

            if (preAndPostSplit.TryGetValue(true, out var preFieldMethods))
            {
                preFieldAutomationMethods = preFieldMethods.OrderBy(m => m.attribute.executionOrder).ToList();
            }

            if (preAndPostSplit.TryGetValue(false, out var postFieldMethods))
            {
                postFieldAutomationMethods = postFieldMethods.OrderBy(m => m.attribute.executionOrder).ToList();
            }

            refreshingOnBuildMethods = false;
        }

        public static void CacheUSharpInstances()
        {
            if (refreshingOnBuildMethods)
            {
                VRRADebugger.LogError("Cannot cache UdonSharp instances while refreshing RunOnBuild methods!");
                return;
            }

            foreach (var uSharpType in uSharpTypeInstances.Keys)
            {
#if UNITY_2020_1_OR_NEWER
                UdonSharpBehaviour[] uSharpInstances = UnityEngine.Object.FindObjectsOfType(uSharpType, true).Where(x => x.GetType() == uSharpType).Select(x => (UdonSharpBehaviour)x).ToArray();
#else
                UdonSharpBehaviour[] uSharpInstances = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(uSharpType).Where(x => x.GetType() == uSharpType).Select(x => (UdonSharpBehaviour)x).ToArray();
#endif

                uSharpTypeInstances[uSharpType].Clear();
                uSharpTypeInstances[uSharpType].AddRange(uSharpInstances);
            }
        }

        public abstract class RunOnBuildMethod
        {
            protected RunOnBuildMethod(MethodInfo methodInfo)
            {
                this.methodInfo = methodInfo;
                attribute = methodInfo.GetCustomAttribute<RunOnBuildAttribute>();
            }

            public MethodInfo MethodInfo => methodInfo;
            protected readonly MethodInfo methodInfo;
            public readonly RunOnBuildAttribute attribute;

            public bool TryInvoke(out Exception exception)
            {
                exception = null;

                try
                {
                    Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);

                    exception = e;

                    return false;
                }

                return true;
            }

            protected abstract void Invoke();
        }

        private class StaticRunOnBuildMethod : RunOnBuildMethod
        {
            protected override void Invoke()
            {
                methodInfo.Invoke(null, null);
            }

            public StaticRunOnBuildMethod(MethodInfo methodInfo) : base(methodInfo)
            {
            }
        }

        private class InstanceRunOnBuildMethod : RunOnBuildMethod
        {
            public readonly Type declaringType;

            protected override void Invoke()
            {
                var allTypesWithMethod = TypeCache.GetTypesDerivedFrom(declaringType).Where(t => !t.IsAbstract).ToList();
                
                allTypesWithMethod.Add(declaringType);

                foreach (var type in allTypesWithMethod)
                {
                    if (!uSharpTypeInstances.TryGetValue(type, out var instances))
                    {
                        return;
                    }

                    foreach (var instance in instances)
                    {
                        methodInfo.Invoke(instance, null);
                    }   
                }
            }

            public InstanceRunOnBuildMethod(MethodInfo methodInfo) : base(methodInfo)
            {
                declaringType = methodInfo.DeclaringType;
            }
        }
    }
}