using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UnityEngine;
using VRFastScripting.Editor.Extensions;

#if UDONSHARP

namespace VRFastScripting.Editor.Automation
{
    public static class FieldAutomation
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static UdonSharpBehaviour[] sceneUdons;

        [RunOnBuild(-1)]
        private static void ExecuteAllFieldAutomation()
        {
            sceneUdons = UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<UdonSharpBehaviour>();

            foreach (var sceneUdon in sceneUdons)
            {
                foreach (var fieldAutomation in FieldAutomationDict.Keys)
                {
                    int count = sceneUdon.SetComponentFieldsWithAttribute(fieldAutomation);
                    FieldAutomationResults[fieldAutomation] += count;
                }
            }

            foreach (var result in FieldAutomationResults)
            {
                if (result.Value > 0)
                    VRFSDebugger.Log($"Successfully set ({result.Value}) {result.Key.ToString()}s references");
            }
        }

        private static int SetComponentFieldsWithAttribute(this UdonSharpBehaviour sceneUdon, FieldAutomationType automationType)
        {
            int count = 0;
            foreach (var field in sceneUdon.GetType().GetFields(FieldFlags))
            {
                var attributeType = FieldAutomationDict[automationType];

                var customAttribute = field.GetCustomAttribute(FieldAutomationDict[automationType]);
                if (customAttribute == null) continue;
                if (!field.IsSerialized()) continue;


                if (((AutosetAttribute) customAttribute).dontOverride && field.GetValue(sceneUdon) != null)
                {
                    continue;
                }

                var components = sceneUdon.GetFromFieldAutomation(field.FieldType.IsArray ? field.FieldType.GetElementType() : field.FieldType, automationType, customAttribute);

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
                    VRFSDebugger.LogError($"Failed to set [{attributeType.Name}] ({field.DeclaringType}) {field.Name} on ({sceneUdon.GetType()}) {sceneUdon.name}", sceneUdon);
                    continue;
                }

                object obj;
                if (field.FieldType.IsArray)
                {
                    var elementType = field.FieldType.GetElementType();

                    var actualValues = Array.CreateInstance(elementType, components.Length);
                    for (int i = 0; i < components.Length; i++)
                    {
                        actualValues.SetValue(Convert.ChangeType(components[i], elementType), i);
                    }

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


        //TODO: Add support for GameObject arrays with [Find] attributes
        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        private static object[] GetFromFieldAutomation(this UdonSharpBehaviour component, Type type, FieldAutomationType automationType, Attribute attribute)
        {
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
                    return UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(type);
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
            }
        }

        private static readonly Dictionary<FieldAutomationType, Type> FieldAutomationDict = new Dictionary<FieldAutomationType, Type>
        {
            {FieldAutomationType.GetComponent, typeof(GetComponent)},
            {FieldAutomationType.GetComponentInChildren, typeof(GetComponentInChildren)},
            {FieldAutomationType.GetComponentInParent, typeof(GetComponentInParent)},
            {FieldAutomationType.GetComponentInDirectParent, typeof(GetComponentInDirectParent)},
            {FieldAutomationType.FindObjectOfType, typeof(FindObjectOfType)},
            {FieldAutomationType.Find, typeof(Find)},
            {FieldAutomationType.FindInChildren, typeof(FindInChildren)},
        };

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
        }

        public static bool IsSerialized(this FieldInfo field) => !(field.GetCustomAttribute<NonSerializedAttribute>() != null || field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null);
    }
}

#endif