using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRRefAssist
{
    /// <summary>
    /// You can create your own AutosetAttribute by inheriting this class and overriding GetObjectsLogic to return the objects you want to set the field to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class AutosetAttribute : Attribute
    {
        public readonly bool dontOverride;
        
        protected AutosetAttribute(bool dontOverride = false)
        {
            this.dontOverride = dontOverride;
        }
        public abstract object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type);
    }
    
    /// <summary>
    /// This will run GetComponent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponent(bool dontOverride = false) : base(dontOverride)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.GetComponents(type);
        }
    }

    public class GetComponents : GetComponent { }

    /// <summary>
    /// This will run GetComponentInChildren(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInChildren : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponentInChildren(bool dontOverride = false) : base(dontOverride)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.GetComponentsInChildren(type, true);
        }
    }
    
    public class GetComponentsInChildren : GetComponentInChildren { }

    /// <summary>
    /// This will run GetComponentInParent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInParent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponentInParent(bool dontOverride = false) : base(dontOverride)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.GetComponentsInParent(type, true);
        }
    }
    
    public class GetComponentsInParent : GetComponentInParent { }

    /// <summary>
    /// This is will run transform.parent.GetComponent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInDirectParent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponentInDirectParent(bool dontOverride = false) : base(dontOverride)
        {
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return uSharpBehaviour.transform.parent == null ? Array.Empty<Component>() : uSharpBehaviour.transform.parent.GetComponents(type);
        }
    }
    
    public class GetComponentsInDirectParent : GetComponentInDirectParent { }

    /// <summary>
    /// This will run FindObjectsOfType(type) and set the field to the result, if the field is not an array it will use the first value.
    /// </summary>
    public class FindObjectOfType : AutosetAttribute
    {
        public readonly bool includeDisabled;

        /// <param name="includeDisabled">Include components in disabled GameObjects?</param>
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public FindObjectOfType(bool includeDisabled = true, bool dontOverride = false) : base(dontOverride)
        {
            this.includeDisabled = includeDisabled;
        }

        public override object[] GetObjectsLogic(UdonSharpBehaviour uSharpBehaviour, Type type)
        {
            return includeDisabled ? FindObjectsOfTypeIncludeDisabled(type) : UnityEngine.Object.FindObjectsOfType(type);        
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
    public class FindObjectsOfType : FindObjectOfType { }

    /// <summary>
    /// This will run Find(searchName) and set the field to the result, it also works for type of GameObject.
    /// </summary>
    public class Find : AutosetAttribute
    {
        public readonly string searchName;

        /// <param name="searchName">The name of the object to find</param>
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public Find(string searchName, bool dontOverride = false) : base(dontOverride)
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
        public FindInChildren(string searchName, bool dontOverride = false) : base(dontOverride)
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
}