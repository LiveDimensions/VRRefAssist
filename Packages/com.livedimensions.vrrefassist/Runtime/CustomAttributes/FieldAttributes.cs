using System;

namespace VRRefAssist
{
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class AutosetAttribute : Attribute
    {
        public readonly bool dontOverride;
        
        protected AutosetAttribute(bool dontOverride = false)
        {
            this.dontOverride = dontOverride;
        }
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
    }
    
    /// <summary>
    /// This will run GetComponentInChildren(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInChildren : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponentInChildren(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This will run GetComponentInParent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInParent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponentInParent(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This is will run transform.parent.GetComponent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInDirectParent : AutosetAttribute
    {
        /// <param name="dontOverride">If the field value is not null, it won't be set again. You can use this to override references</param>
        public GetComponentInDirectParent(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This will run FindObjectOfType(type) and set the field to the result.
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
    }

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
    }
}