using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRRefAssist.Editor.Extensions;

namespace VRRefAssist.Editor.Automation
{
    public static class SingletonAutomation
    {
        private static bool cachedSingletons;
        private static Type[] cachedSingletonTypes;

        private static readonly Dictionary<Type, List<FieldInfo>> typesUsingSingletons = new Dictionary<Type, List<FieldInfo>>();

        static SingletonAutomation()
        {
            //Find all classes that inherit from MonoBehaviour
            List<Type> monoInheritors = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Where(t => !t.IsAbstract).ToList();

            List<Type> singletonTypes = TypeCache.GetTypesWithAttribute<Singleton>().ToList();

            //Find all fields that use Singletons
            foreach (Type monoInheritor in monoInheritors)
            {
                FieldInfo[] fields = FieldAutomation.GetAllFields(monoInheritor).ToArray();
                foreach (FieldInfo field in fields)
                {
                    if (!singletonTypes.Contains(field.FieldType)) continue;
                    if (!field.IsSerialized()) continue;

                    if (!typesUsingSingletons.ContainsKey(monoInheritor))
                    {
                        typesUsingSingletons.Add(monoInheritor, new List<FieldInfo> { field });
                    }
                    else
                    {
                        typesUsingSingletons[monoInheritor].Add(field);
                    }
                }
            }
        }

        /// <summary>
        /// Returns all singletons found in assemblies
        /// </summary>
        public static Type[] Singletons
        {
            get
            {
                if (!cachedSingletons)
                {
                    cachedSingletonTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.IsDefined(typeof(Singleton)))).ToArray();
                    cachedSingletons = true;
                }

                return cachedSingletonTypes;
            }
        }

        private static Dictionary<Type, MonoBehaviour> sceneSingletonsDict = new Dictionary<Type, MonoBehaviour>();

        private static void RefreshSingletonsInScene()
        {
            var types = TypeCache.GetTypesWithAttribute<Singleton>();
            
            List<MonoBehaviour> sceneMonoSingletons = new List<MonoBehaviour>();
            
            foreach (var type in types)
            {
                #if UNITY_2020_1_OR_NEWER
                MonoBehaviour mono = UnityEngine.Object.FindObjectOfType(type, true) as MonoBehaviour;
                #else
                MonoBehaviour mono = UnityEditorExtensions.FindObjectOfTypeIncludeDisabled(type) as MonoBehaviour;
                #endif
                
                if (mono != null)
                {
                    sceneMonoSingletons.Add(mono);
                }
            }
            
            /*
            #if UNITY_2020_1_OR_NEWER
            List<MonoBehaviour> sceneMonoSingletons = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true).Where(x => x != null && x.GetType().GetCustomAttribute<Singleton>() != null).ToList();
            #else
            List<MonoBehaviour> sceneMonoSingletons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<MonoBehaviour>().Where(x => x != null && x.GetType().GetCustomAttribute<Singleton>() != null).ToList();
            #endif
            */

            var repeatedSingletons = sceneMonoSingletons.GroupBy(u => u.GetType()).Where(r => r.Count() > 1);

            foreach (var repeatedSingleton in repeatedSingletons)
            {
                VRRADebugger.LogError($"There are multiple instances ({repeatedSingleton.Count()}) of the same singleton in the scene! (" + repeatedSingleton.Key + ")");
            }

            sceneSingletonsDict = sceneMonoSingletons.GroupBy(u => u.GetType()).Select(u => u.First()).ToDictionary(x => x.GetType(), x => x);

            if(sceneMonoSingletons.Count > 0)
                VRRADebugger.Log($"Singleton refresh found {sceneMonoSingletons.Count} singletons in the scene");
        }

        [MenuItem("VR RefAssist/Tools/Set Singleton References", priority = 201)]
        private static void ManuallySetAllSingletonReferences()
        {
            int count = SetAllSingletonReferences();

            if (count > 0) SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Successfully set ({count}) singleton references"));
        }

        public static int SetAllSingletonReferences()
        {
            return SetAllSingletonReferences(out _);
        }

        public static int SetAllSingletonReferences(out bool cancelBuild)
        {
            RefreshSingletonsInScene();

            bool showPopupWhenFieldAutomationFailed = VRRefAssistSettings.GetOrCreateSettings().showPopupWarnsForFailedFieldAutomation;

            cancelBuild = false;

            int count = 1;
            int total = typesUsingSingletons.Count;

            int resultCount = 0;

            foreach (var typeUsingSingleton in typesUsingSingletons)
            {
                Type typeToFind = typeUsingSingleton.Key;

                if (UnityEditorExtensions.DisplaySmartUpdatingCancellableProgressBar($"Setting Singleton References...", count == total ? "Finishing..." : $"Progress: {count}/{total}.\tCurrent U# Behaviour: {typeToFind.Name}", count / (total - 1f)))
                {
                    EditorUtility.ClearProgressBar();

                    cancelBuild = EditorUtility.DisplayDialog("Cancelled setting Singleton References", "You have canceled setting Singleton References\nDo you want to cancel the build as well?", "Cancel", "Continue");

                    return resultCount;
                }

                count++;

                //When getting the monos, check for the ones that specifically are of the type, otherwise we will repeat classes that are inherited.
#if UNITY_2020_1_OR_NEWER
                List<MonoBehaviour> monos = UnityEngine.Object.FindObjectsOfType(typeToFind, true).Where(x => x.GetType() == typeToFind).Select(x => (MonoBehaviour)x).ToList();
#else
                List<MonoBehaviour> monos = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(typeToFind).Where(x => x.GetType() == typeToFind).Select(x => (MonoBehaviour)x).ToList();
#endif

                FieldInfo[] fields = typeUsingSingleton.Value.ToArray();

                foreach (var sceneMono in monos)
                {
                    foreach (var field in fields)
                    {
                        if (!sceneSingletonsDict.ContainsKey(field.FieldType))
                        {
                            VRRADebugger.LogError($"Failed to set singleton \"{field.FieldType.Name}\" in {sceneMono.GetType().Name} ({sceneMono.name}), because the singleton was not found in the scene!", sceneMono.gameObject);

                            if (showPopupWhenFieldAutomationFailed)
                            {
                                bool cancel = EditorUtility.DisplayDialog("Failed to set Singleton reference", $"Failed to set singleton \"{field.FieldType.Name}\" in {sceneMono.GetType().Name} ({sceneMono.name}), because the singleton was not found in the scene!\nDo you want to cancel the build?", "Cancel", "Continue");
                                if (cancel)
                                {
                                    cancelBuild = true;
                                    return resultCount;
                                }
                            }

                            continue;
                        }

                        resultCount++;

                        field.SetValue(sceneMono, sceneSingletonsDict[field.FieldType]);
                    }

                    UnityEditorExtensions.FullSetDirty(sceneMono);
                }
            }

            if(resultCount > 0)
                VRRADebugger.Log($"Successfully set ({resultCount}) singleton references");
            EditorUtility.ClearProgressBar();

            return resultCount;
        }
    }
}