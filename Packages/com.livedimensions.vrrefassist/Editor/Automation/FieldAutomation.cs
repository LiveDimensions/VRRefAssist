using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            List<Type> autosetAttributes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AutosetAttribute))).ToList();

            FieldAutomationTypeResults.Clear();

            foreach (var autosetAttribute in autosetAttributes)
            {
                FieldAutomationTypeResults.Add(autosetAttribute, 0);
            }
        }

        private static readonly Dictionary<Type, int> FieldAutomationTypeResults = new Dictionary<Type, int>();

        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool showPopupWhenFieldAutomationFailed;

        [MenuItem("VR RefAssist/Tools/Execute Field Automation", priority = 202)]
        public static void ExecuteAllFieldAutomation()
        {
            UdonSharpBehaviour[] sceneUdons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<UdonSharpBehaviour>();

            showPopupWhenFieldAutomationFailed = VRRefAssistSettings.GetOrCreateSettings().showPopupWarnsForFailedFieldAutomation;

            int count = 1;
            int total = sceneUdons.Length;
            foreach (var sceneUdon in sceneUdons)
            {
                if (UnityEditorExtensions.DisplaySmartUpdatingCancellableProgressBar($"Running Field Automation...", count == total ? "Finishing..." : $"Progress: {count}/{total}.\tCurrent U# Behaviour: {sceneUdon.name}", count / (total - 1f)))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                count++;

                foreach (var fieldAutomation in FieldAutomationTypeResults.Keys.ToList())
                {
                    int resultCount = sceneUdon.SetComponentFieldsWithAttribute(fieldAutomation);
                    FieldAutomationTypeResults[fieldAutomation] += resultCount;
                }
            }

            EditorUtility.ClearProgressBar();

            foreach (var result in FieldAutomationTypeResults)
            {
                if (result.Value > 0)
                    VRRADebugger.Log($"Successfully set ({result.Value}) {result.Key.Name} references");
            }
        }

        private static int SetComponentFieldsWithAttribute(this UdonSharpBehaviour sceneUdon, Type automationType)
        {
            FieldInfo[] fields = GetAllFields(sceneUdon.GetType()).ToArray();

            int count = 0;
            foreach (FieldInfo field in fields)
            {
                AutosetAttribute customAttribute = (AutosetAttribute)field.GetCustomAttribute(automationType);
                if (customAttribute == null) continue;
                if (!field.IsSerialized()) continue;

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
                    VRRADebugger.LogError($"Failed to set [{automationType}] ({field.DeclaringType}) {field.Name} on ({sceneUdon.GetType()}) {sceneUdon.name}", sceneUdon);

                    if (showPopupWhenFieldAutomationFailed)
                    {
                        bool result = EditorUtility.DisplayDialog("Field Automation...",
                            $"Failed to set [{automationType}] ({field.DeclaringType}) {field.Name} on ({sceneUdon.GetType()}) {sceneUdon.name}",
                            "Continue", "Abort");

                        if (result) continue;

                        throw new Exception("Field Automation Aborted");
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

                count++;
                UnityEditorExtensions.FullSetDirty(sceneUdon);
            }

            return count;
        }

        public static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null) return Enumerable.Empty<FieldInfo>();

            return t.GetFields(FieldFlags).Concat(GetAllFields(t.BaseType));
        }

        public static bool IsSerialized(this FieldInfo field) => !(field.GetCustomAttribute<NonSerializedAttribute>() != null || field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null);
    }
}