using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
// ReSharper disable CoVariantArrayConversion

namespace VRRefAssist
{
    /// <summary>
    /// You can create your own AutosetAttribute by inheriting this class and overriding GetObjectsLogic to return the objects you want to set the field to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class AutosetAttribute : Attribute
    {
        public readonly bool dontOverride;
        public readonly bool suppressErrors;
        
        protected AutosetAttribute(bool dontOverride = false, bool suppressErrors = false)
        {
            this.dontOverride = dontOverride;
            this.suppressErrors = suppressErrors;
        }
        public abstract object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type);
    }
    
    /// <summary>
    /// This will run GetComponent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public GetComponent(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.GetComponents(type);
        }
    }

    public class GetComponents : GetComponent
    {
        public GetComponents(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }
    }

    /// <summary>
    /// This will run GetComponentInChildren(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInChildren : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public GetComponentInChildren(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.GetComponentsInChildren(type, true);
        }
    }

    public class GetComponentsInChildren : GetComponentInChildren
    {
        public GetComponentsInChildren(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }
    }

    /// <summary>
    /// This will run GetComponentInParent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInParent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public GetComponentInParent(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.GetComponentsInParent(type, true);
        }
    }

    public class GetComponentsInParent : GetComponentInParent
    {
        public GetComponentsInParent(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }
    }

    /// <summary>
    /// This is will run transform.parent.GetComponent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInDirectParent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public GetComponentInDirectParent(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.transform.parent == null ? Array.Empty<Component>() : uSharpBehaviour.transform.parent.GetComponents(type);
        }
    }

    public class GetComponentsInDirectParent : GetComponentInDirectParent
    {
        public GetComponentsInDirectParent(bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
        }
    }

    /// <summary>
    /// This will run FindObjectsOfType(type) and set the field to the result, if the field is not an array it will use the first value.
    /// </summary>
    public class FindObjectOfType : AutosetAttribute
    {
        public readonly bool includeDisabled;

        /// <param name="includeDisabled">Include components in disabled GameObjects?</param>
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public FindObjectOfType(bool includeDisabled = true, bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
            this.includeDisabled = includeDisabled;
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            #if UNITY_2020_1_OR_NEWER
            return UnityEngine.Object.FindObjectsOfType(type, includeDisabled);
            #else
            return includeDisabled ? FindObjectsOfTypeIncludeDisabled(type) : UnityEngine.Object.FindObjectsOfType(type);
            #endif
        }
        
        private static Component[] FindObjectsOfTypeIncludeDisabled(Type type)
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
    }

    /// <summary>
    /// Exactly the same as FindObjectOfType, as it already works with array fields.
    /// </summary>
    public class FindObjectsOfType : FindObjectOfType
    {
        public FindObjectsOfType(bool includeDisabled = true, bool dontOverride = false, bool suppressErrors = false) : base(includeDisabled, dontOverride, suppressErrors)
        {
        }
    }

    /// <summary>
    /// This will run Find(searchName) and set the field to the result, it also works for type of GameObject.
    /// </summary>
    public class Find : AutosetAttribute
    {
        public readonly string searchName;

        /// <param name="searchName">The name of the object to find</param>
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public Find(string searchName, bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
            this.searchName = searchName;
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            GameObject findGo = GameObject.Find(searchName);

            if (type == typeof(GameObject)) return new object[] {findGo};

            return findGo == null ? Array.Empty<Component>() : findGo.GetComponents(type);
        }
    }

    /// <summary>
    /// This will run transform.Find(searchName) and set the field to the result, it also works for type of GameObject.
    /// </summary>
    public class FindInChildren : AutosetAttribute
    {
        public readonly string searchName;

        /// <param name="searchName">The name of the object to find</param>
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public FindInChildren(string searchName, bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
            this.searchName = searchName;
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            GameObject findInChildrenGo = uSharpBehaviour.transform.Find(searchName).gameObject;

            if (type == typeof(GameObject)) return new object[] {findInChildrenGo};

            return findInChildrenGo == null ? Array.Empty<Component>() : findInChildrenGo.GetComponents(type);
        }
    }

    /// <summary>
    /// This will run GameObject.FindGameObjectsWithTag(tag) and GetComponents(type) on each result. Also works for GameObjects and Transforms.
    /// By default, this will include disabled gameObjects, but this can be changed with 'includeDisabledGameObjects'. Disabled *components* are always included.
    /// </summary>
    public class FindObjectWithTag : AutosetAttribute
    {
        public readonly string tag;
        public bool includeDisabledGameObjects;

        /// <param name="tag">The tag to search for</param>
        /// <param name="includeDisabledGameObjects">Include disabled GameObjects?</param>
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        /// <param name="suppressErrors">If the reference fails to be set, the console error will be suppressed.</param>
        public FindObjectWithTag(string tag, bool includeDisabledGameObjects = true, bool dontOverride = false, bool suppressErrors = false) : base(dontOverride, suppressErrors)
        {
            this.tag = tag;
            this.includeDisabledGameObjects = includeDisabledGameObjects;
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, System.Type type)
        {
            List<GameObject> results;
            
            if (includeDisabledGameObjects)
            {
                //Unity 2020 and newer has a method to find objects of type including disabled ones, and then we can filter by tag.
#if UNITY_2020_1_OR_NEWER
                results = UnityEngine.Object.FindObjectsOfType<GameObject>(true).Where(go => go.CompareTag(tag)).ToList();
#else
                //2019 and older doesn't, so we use a manual method.
                results = FindGameObjectsWithTagIncludeDisabled(tag).ToList();
#endif
            }
            else
            {
                //Just use the normal method if we don't want disabled gameObjects
                results = GameObject.FindGameObjectsWithTag(tag).ToList();
            }

            results = results.OrderBy(g => g.name).ToList();

            if (type == typeof(GameObject)) return results.ToArray();
            if (type == typeof(Transform)) return results.Select(g => g.transform).ToArray();

            List<Component> components = new List<Component>();
            
            foreach(GameObject go in results) {
                components.AddRange(go.GetComponents(type));
            }

            return components.ToArray();
        }

        //Iterate over all root gameObjects and get all gameObjects with the tag, including disabled ones.
        //This uses the same method in UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled but needs to be available outside of Editor since these attributes are runtime.
        private static GameObject[] FindGameObjectsWithTagIncludeDisabled(string tag)
        {
            GameObject[] rootGos = SceneManager.GetActiveScene().GetRootGameObjects();

            List<GameObject> objs = new List<GameObject>();

            foreach (GameObject root in rootGos)
            {
                objs.AddRange(root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).Where(go => go.CompareTag(tag)));
            }
            
            return objs.ToArray();
        }
    }
    
    /// <summary>
    /// This will run GameObject.FindGameObjectsWithTag(tag) and GetComponents(type) on each result. Also works for GameObjects and Transforms.
    /// By default, this will include disabled gameObjects, but this can be changed with 'includeDisabledGameObjects'. Disabled *components* are always included.
    /// </summary>
    public class FindObjectsWithTag : FindObjectWithTag {
        public FindObjectsWithTag(string tag, bool includeDisabledGameObjects = true, bool dontOverride = false, bool suppressErrors = false) : base(tag, includeDisabledGameObjects, dontOverride, suppressErrors)
        {
        }
    }
}