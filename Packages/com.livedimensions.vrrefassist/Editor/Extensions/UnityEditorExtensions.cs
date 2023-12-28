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
        
#if UNITY_2020_1_OR_NEWER
        [Obsolete("Use Object.FindObjectsOfType<T>(bool includeInactive) instead")]
#endif
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

#if UNITY_2020_1_OR_NEWER
        [Obsolete("Use Object.FindObjectsOfType(Type type,bool includeInactive) instead")]
#endif
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

#if UNITY_2020_1_OR_NEWER
        [Obsolete("Use Object.FindObjectOfType<T>(bool includeInactive) instead")]
#endif
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

#if UNITY_2020_1_OR_NEWER
        [Obsolete("Use Object.FindObjectOfType(Type type, bool includeInactive) instead")]
#endif
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
        
        private static System.Diagnostics.Stopwatch smartProgressBarWatch = System.Diagnostics.Stopwatch.StartNew();
        private static int smartProgressBarDisplaysSinceLastUpdate = 0;
        
        public static bool DisplaySmartUpdatingCancellableProgressBar(string title, string details, float progress, int updateIntervalByMS = 200, int updateIntervalByCall = 50)
        {
            bool updateProgressBar =
                smartProgressBarWatch.ElapsedMilliseconds >= updateIntervalByMS
                || ++smartProgressBarDisplaysSinceLastUpdate >= updateIntervalByCall;

            if (updateProgressBar)
            {
                smartProgressBarWatch.Stop();
                smartProgressBarWatch.Reset();
                smartProgressBarWatch.Start();

                smartProgressBarDisplaysSinceLastUpdate = 0;

                if (EditorUtility.DisplayCancelableProgressBar(title, details, progress))
                {
                    return true;
                }
            }

            return false;
        }
    }
}