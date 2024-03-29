﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
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
            //Find all classes that inherit from UdonSharpBehaviour
            List<Type> uSharpInheritors = TypeCache.GetTypesDerivedFrom<UdonSharpBehaviour>().Where(t => !t.IsAbstract).ToList();

            List<Type> singletonTypes = TypeCache.GetTypesWithAttribute<Singleton>().ToList();

            //Find all fields that use Singletons
            foreach (Type uSharpInheritor in uSharpInheritors)
            {
                FieldInfo[] fields = FieldAutomation.GetAllFields(uSharpInheritor).ToArray();
                foreach (FieldInfo field in fields)
                {
                    if (!singletonTypes.Contains(field.FieldType)) continue;
                    if (!field.IsSerialized()) continue;

                    if (!typesUsingSingletons.ContainsKey(uSharpInheritor))
                    {
                        typesUsingSingletons.Add(uSharpInheritor, new List<FieldInfo> { field });
                    }
                    else
                    {
                        typesUsingSingletons[uSharpInheritor].Add(field);
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

        private static Dictionary<Type, UdonSharpBehaviour> sceneSingletonsDict = new Dictionary<Type, UdonSharpBehaviour>();

        private static void RefreshSingletonsInScene()
        {
            #if UNITY_2020_1_OR_NEWER
            List<UdonSharpBehaviour> sceneUdonSingletons = UnityEngine.Object.FindObjectsOfType<UdonSharpBehaviour>(true).Where(x => x.GetType().GetCustomAttribute<Singleton>() != null).ToList();
            #else
            List<UdonSharpBehaviour> sceneUdonSingletons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<UdonSharpBehaviour>().Where(x => x.GetType().GetCustomAttribute<Singleton>() != null).ToList();
            #endif

            var repeatedSingletons = sceneUdonSingletons.GroupBy(u => u.GetType()).Where(r => r.Count() > 1);

            foreach (var repeatedSingleton in repeatedSingletons)
            {
                VRRADebugger.LogError($"There are multiple instances ({repeatedSingleton.Count()}) of the same singleton in the scene! (" + repeatedSingleton.Key + ")");
            }

            sceneSingletonsDict = sceneUdonSingletons.GroupBy(u => u.GetType()).Select(u => u.First()).ToDictionary(x => x.GetType(), x => x);

            VRRADebugger.Log($"Singleton refresh found {sceneUdonSingletons.Count} singletons in the scene");
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

                //When getting the udons, check for the ones that specifically are of the type, otherwise we will repeat classes that are inherited.
#if UNITY_2020_1_OR_NEWER
                List<UdonSharpBehaviour> udons = UnityEngine.Object.FindObjectsOfType(typeToFind, true).Where(x => x.GetType() == typeToFind).Select(x => (UdonSharpBehaviour)x).ToList();
#else
                List<UdonSharpBehaviour> udons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(typeToFind).Where(x => x.GetType() == typeToFind).Select(x => (UdonSharpBehaviour)x).ToList();
#endif

                FieldInfo[] fields = typeUsingSingleton.Value.ToArray();

                foreach (var sceneUdon in udons)
                {
                    foreach (var field in fields)
                    {
                        if (!sceneSingletonsDict.ContainsKey(field.FieldType))
                        {
                            VRRADebugger.LogError($"Failed to set singleton \"{field.FieldType.Name}\" in {sceneUdon.GetType().Name} ({sceneUdon.name}), because the singleton was not found in the scene!", sceneUdon.gameObject);

                            if (showPopupWhenFieldAutomationFailed)
                            {
                                bool cancel = EditorUtility.DisplayDialog("Failed to set Singleton reference", $"Failed to set singleton \"{field.FieldType.Name}\" in {sceneUdon.GetType().Name} ({sceneUdon.name}), because the singleton was not found in the scene!\nDo you want to cancel the build?", "Cancel", "Continue");
                                if (cancel)
                                {
                                    cancelBuild = true;
                                    return resultCount;
                                }
                            }

                            continue;
                        }

                        resultCount++;

                        field.SetValue(sceneUdon, sceneSingletonsDict[field.FieldType]);
                    }

                    UnityEditorExtensions.FullSetDirty(sceneUdon);
                }
            }

            VRRADebugger.Log($"Successfully set ({resultCount}) singleton references");
            EditorUtility.ClearProgressBar();

            return resultCount;
        }
    }
}