using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UdonSharp;
using UnityEditor;
using VRRefAssist.Editor.Extensions;

namespace VRRefAssist.Editor.Automation
{
    [InitializeOnLoad]
    public static class FieldAutomation
    {
        static FieldAutomation()
        {
            //Make list of all AutosetAttributes in the project by searching all assemblies and checking if is subclass of AutosetAttribute
            List<Type> autosetAttributes = TypeCache.GetTypesDerivedFrom<AutosetAttribute>().Where(t => !t.IsAbstract).ToList();

            FieldAutomationTypeResults.Clear();

            foreach (var autosetAttribute in autosetAttributes)
            {
                FieldAutomationTypeResults.Add(autosetAttribute, 0);
            }

            //Find all classes that inherit from UdonSharpBehaviour
            List<Type> uSharpInheritors = TypeCache.GetTypesDerivedFrom<UdonSharpBehaviour>().Where(t => !t.IsAbstract).ToList();

            //Find all fields in all UdonSharpBehaviours that have an AutosetAttribute and cache them
            foreach (var uSharpInheritor in uSharpInheritors)
            {
                FieldInfo[] fields = GetAllFields(uSharpInheritor).ToArray();

                foreach (FieldInfo field in fields)
                {
                    AutosetAttribute customAttribute = (AutosetAttribute)field.GetCustomAttribute(typeof(AutosetAttribute));
                    if (customAttribute == null) continue;
                    if (!field.IsSerialized()) continue;

                    if (cachedAutosetFields.ContainsKey(uSharpInheritor))
                        cachedAutosetFields[uSharpInheritor].Add(field);
                    else
                        cachedAutosetFields.Add(uSharpInheritor, new List<FieldInfo> { field });
                }
            }
        }

        private static readonly Dictionary<Type, List<FieldInfo>> cachedAutosetFields = new Dictionary<Type, List<FieldInfo>>();

        private static readonly Dictionary<Type, int> FieldAutomationTypeResults = new Dictionary<Type, int>();

        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool showPopupWhenFieldAutomationFailed;

        [MenuItem("VR RefAssist/Tools/Execute Field Automation", priority = 202)]
        public static void ManuallyExecuteFieldAutomation()
        {
            ExecuteAllFieldAutomation(out _);
        }

        public static void ExecuteAllFieldAutomation()
        {
            ExecuteAllFieldAutomation(out _);
        }
        
        public static void ExecuteAllFieldAutomation(out bool cancelBuild)
        {
            FieldAutomationTypeResults.Clear();
            
            showPopupWhenFieldAutomationFailed = VRRefAssistSettings.GetOrCreateSettings().showPopupWarnsForFailedFieldAutomation;

            cancelBuild = false;
            
            int count = 1;
            int total = cachedAutosetFields.Count;

            foreach (var cachedValuePair in cachedAutosetFields)
            {
                Type typeToFind = cachedValuePair.Key;

                if (UnityEditorExtensions.DisplaySmartUpdatingCancellableProgressBar("Running Field Automation...", count == total ? "Finishing..." : $"Progress: {count}/{total}.\tCurrent U# Behaviour: {typeToFind.Name}", count / (total - 1f)))
                {
                    EditorUtility.ClearProgressBar();
                    
                    cancelBuild = EditorUtility.DisplayDialog("Cancelled running Field Automation", "You have canceled field automation (Autoset fields)\nDo you want to cancel the build as well?", "Cancel", "Continue");
                    return;
                }

                count++;

                //When getting the udons, check for the ones that specifically are of the type, otherwise we will repeat classes that are inherited.
#if UNITY_2020_1_OR_NEWER
                List<UdonSharpBehaviour> udons = UnityEngine.Object.FindObjectsOfType(typeToFind, true).Where(x => x.GetType() == typeToFind).Select(x => (UdonSharpBehaviour)x).ToList();          
#else
                List<UdonSharpBehaviour> udons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(typeToFind).Where(x => x.GetType() == typeToFind).Select(x => (UdonSharpBehaviour)x).ToList();
#endif

                FieldInfo[] fields = cachedValuePair.Value.ToArray();

                foreach (var sceneUdon in udons)
                {
                    foreach (var field in fields)
                    {
                        AutosetAttribute customAttribute = field.GetCustomAttribute<AutosetAttribute>();

                        if (customAttribute.dontOverride && field.GetValue(sceneUdon) != null)
                        {
                            continue;
                        }

                        bool isArray = field.FieldType.IsArray;

                        object[] components = customAttribute.GetObjectsLogic(sceneUdon, isArray ? field.FieldType.GetElementType() : field.FieldType);

                        bool failToSet;

                        if (isArray)
                        {
                            failToSet = components.Length == 0;
                        }
                        else
                        {
                            failToSet = components.FirstOrDefault() == null;
                        }

                        if (failToSet)
                        {
                            //Don't log error if suppressErrors is true
                            if (!customAttribute.suppressErrors)
                            {
                                VRRADebugger.LogError($"Failed to set \"[{customAttribute}] {field.Name}\" on ({sceneUdon.GetType()}) {sceneUdon.name}", sceneUdon);
                            }

                            if (showPopupWhenFieldAutomationFailed)
                            {
                                bool cancel = EditorUtility.DisplayDialog("Failed Field Automation...",
                                    $"Failed to set \"[{customAttribute}] {field.Name}\" on ({sceneUdon.GetType()}) {sceneUdon.name}\nDo you want to cancel the build?",
                                    "Cancel", "Continue");

                                if (cancel)
                                {
                                    cancelBuild = true;
                                    return;
                                }
                            }

                            continue;
                        }

                        object obj;
                        if (isArray)
                        {
                            var elementType = field.FieldType.GetElementType();

                            var actualValues = Array.CreateInstance(elementType, components.Length);

                            Array.Copy(components, actualValues, components.Length);

                            obj = actualValues;
                        }
                        else
                        {
                            obj = components.FirstOrDefault();
                        }

                        field.SetValue(sceneUdon, obj);

                        Type customAttributeType = customAttribute.GetType();

                        if (FieldAutomationTypeResults.ContainsKey(customAttributeType))
                            FieldAutomationTypeResults[customAttributeType]++;
                        else
                            FieldAutomationTypeResults.Add(customAttributeType, 1);
                    }

                    UnityEditorExtensions.FullSetDirty(sceneUdon);
                }
            }

            EditorUtility.ClearProgressBar();

            foreach (var result in FieldAutomationTypeResults)
            {
                if (result.Value > 0)
                    VRRADebugger.Log($"Successfully set ({result.Value}) {result.Key.Name} references");
            }
        }

        public static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null) return Enumerable.Empty<FieldInfo>();

            return t.GetFields(FieldFlags).Concat(GetAllFields(t.BaseType));
        }

        public static bool IsSerialized(this FieldInfo field) => !(field.GetCustomAttribute<NonSerializedAttribute>() != null || field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null);
    }
}