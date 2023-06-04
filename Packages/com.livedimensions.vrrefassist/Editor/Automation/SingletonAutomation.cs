using System;
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
        private const BindingFlags FieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static void RefreshSingletonsInScene()
        {
            List<UdonSharpBehaviour> sceneUdonSingletons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<UdonSharpBehaviour>().Where(x => x.GetType().GetCustomAttribute<Singleton>() != null).ToList();

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
            RefreshSingletonsInScene();

            List<UdonSharpBehaviour> udons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<UdonSharpBehaviour>().ToList();

            int count = 0;

            foreach (UdonSharpBehaviour udon in udons)
            {
                count += udon.SetSingletonReferences();
            }

            VRRADebugger.Log($"Successfully set ({count}) singleton references");

            return count;
        }

        private static int SetSingletonReferences(this UdonSharpBehaviour udon)
        {
            int count = 0;
            FieldInfo[] fields = FieldAutomation.GetAllFields(udon.GetType()).ToArray();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.GetCustomAttribute<Singleton>() == null) continue;
                if (!field.IsSerialized()) continue;

                if (!sceneSingletonsDict.ContainsKey(field.FieldType))
                {
                    VRRADebugger.LogError($"Failed to set singleton \"{field.FieldType.Name}\" in {udon.GetType().Name} ({udon.name}), because the singleton was not found in the scene!", udon.gameObject);
                    continue;
                }

                field.SetValue(udon, sceneSingletonsDict[field.FieldType]);

                UnityEditorExtensions.FullSetDirty(udon);

                count++;
            }

            return count;
        }
    }
}