using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VRRefAssist.Editor.Extensions
{
    public static class UnityEditorExtensions
    {
        public static void FullSetDirty(Object obj)
        {
            if (obj == null)
            {
                return;
            }
            
            EditorUtility.SetDirty(obj);
            if (PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            }
        }
        
        public static T[] FindObjectsOfTypeIncludeDisabled<T>() where T : Object
        {
            //If T is a GameObject, get all Transforms and cast them to GameObjects
            if (typeof(T) == typeof(GameObject))
            {
                return FindObjectsOfTypeIncludeDisabled<Transform>().Select(t => t.gameObject).Cast<T>().ToArray();
            }
            
            GameObject[] rootGos = SceneManager.GetActiveScene().GetRootGameObjects();

            List<T> objs = new List<T>();

            foreach (GameObject root in rootGos)
            {
                objs.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return objs.ToArray();
        }

        public static Component[] FindObjectsOfTypeIncludeDisabled(Type type)
        {
            if (type == null) return Array.Empty<Component>();

            GameObject[] rootGos = SceneManager.GetActiveScene().GetRootGameObjects();

            List<Component> objs = new List<Component>();

            foreach (GameObject root in rootGos)
            {
                objs.AddRange(root.GetComponentsInChildren(type, true));
            }

            return objs.ToArray();
        }

        public static T FindObjectOfTypeIncludeDisabled<T>() where T : Object
        {
            //If T is a GameObject, get all Transforms and cast them to GameObjects
            if (typeof(T) == typeof(GameObject))
            {
                return FindObjectOfTypeIncludeDisabled<Transform>().gameObject as T;
            }
            
            GameObject[] rootGos = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject root in rootGos)
            {
                T obj = root.GetComponentInChildren<T>(true);
                if (obj != null)
                {
                    return obj;
                }
            }

            return null;
        }

        public static Component FindObjectOfTypeIncludeDisabled(Type type)
        {
            if (type == null) return null;
            GameObject[] rootGos = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject root in rootGos)
            {
                Component obj = root.GetComponentInChildren(type, true);
                if (obj != null)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}