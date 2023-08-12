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
            int count = 0;
            foreach (var field in GetAllFields(sceneUdon.GetType()))
            {
                var attributeType = FieldAutomationTypeResults[automationType];

                AutosetAttribute customAttribute = (AutosetAttribute)field.GetCustomAttribute(automationType);
                if (customAttribute == null) continue;
                if (!field.IsSerialized()) continue;


                if (customAttribute.dontOverride && field.GetValue(sceneUdon) != null)
                {
                    continue;
                }

                object[] components = sceneUdon.GetFromFieldAutomation(field.FieldType.IsArray ? field.FieldType.GetElementType() : field.FieldType, customAttribute);

                bool failToSet;

                if (field.FieldType.IsArray)
                {
                    failToSet = components.Length == 0;
                }
                else
                {
                    failToSet = components.FirstOrDefault() == null;
                }

                if (failToSet)
                {
                    VRRADebugger.LogError($"Failed to set [{attributeType}] ({field.DeclaringType}) {field.Name} on ({sceneUdon.GetType()}) {sceneUdon.name}", sceneUdon);

                    if (showPopupWhenFieldAutomationFailed)
                    {
                        bool result = EditorUtility.DisplayDialog("Field Automation...",
                            $"Failed to set [{attributeType}] ({field.DeclaringType}) {field.Name} on ({sceneUdon.GetType()}) {sceneUdon.name}",
                            "Continue", "Abort");

                        if (result) continue;
                        
                        throw new Exception("Field Automation Aborted");
                    }
                    
                    continue;
                }

                object obj;
                if (field.FieldType.IsArray)
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


        //TODO: Add support for GameObject arrays with [Find] attributes
        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        private static object[] GetFromFieldAutomation(this UdonSharpBehaviour component, Type type, AutosetAttribute attribute)
        {
            return attribute.GetObjectsLogic(component, type);
            
            /*
            switch (automationType)
            {
                case FieldAutomationType.GetComponent:
                    return component.GetComponents(type);
                case FieldAutomationType.GetComponentInChildren:
                    return component.GetComponentsInChildren(type, true);
                case FieldAutomationType.GetComponentInParent:
                    return component.GetComponentsInParent(type, true);
                case FieldAutomationType.GetComponentInDirectParent:
                    return component.transform.parent == null ? Array.Empty<Component>() : component.transform.parent.GetComponents(type);
                case FieldAutomationType.FindObjectOfType:
                    return ((FindObjectOfType) attribute).includeDisabled ? UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(type) : UnityEngine.Object.FindObjectsOfType(type);
                case FieldAutomationType.Find:
                    GameObject findGo = GameObject.Find(((Find) attribute).searchName);

                    if (type == typeof(GameObject)) return new object[] {findGo};

                    return findGo == null ? Array.Empty<Component>() : findGo.GetComponents(type);
                case FieldAutomationType.FindInChildren:
                    GameObject findInChildrenGo = component.transform.Find(((FindInChildren) attribute).searchName).gameObject;

                    if (type == typeof(GameObject)) return new object[] {findInChildrenGo};

                    return findInChildrenGo == null ? Array.Empty<Component>() : findInChildrenGo.GetComponents(type);
                default:
                    throw new ArgumentOutOfRangeException(nameof(automationType), automationType, null);
            }*/
        }

        /*
        private static readonly Dictionary<FieldAutomationType, int> FieldAutomationResults = new Dictionary<FieldAutomationType, int>
        {
            {FieldAutomationType.GetComponent, 0},
            {FieldAutomationType.GetComponentInChildren, 0},
            {FieldAutomationType.GetComponentInParent, 0},
            {FieldAutomationType.GetComponentInDirectParent, 0},
            {FieldAutomationType.FindObjectOfType, 0},
            {FieldAutomationType.Find, 0},
            {FieldAutomationType.FindInChildren, 0},
        };

        private enum FieldAutomationType
        {
            GetComponent,
            GetComponentInChildren,
            GetComponentInParent,
            GetComponentInDirectParent,
            FindObjectOfType,
            Find,
            FindInChildren
        }*/

        public static bool IsSerialized(this FieldInfo field) => !(field.GetCustomAttribute<NonSerializedAttribute>() != null || field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null);
    }
}