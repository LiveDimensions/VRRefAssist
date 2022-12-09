using System;

namespace VRFastScripting
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
        public GetComponent(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This will run GetComponentInChildren(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInChildren : AutosetAttribute
    {
        public GetComponentInChildren(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This will run GetComponentInParent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInParent : AutosetAttribute
    {
        public GetComponentInParent(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This is will run transform.parent.GetComponent(type) on the object this is attached to and set the field to the result.
    /// </summary>
    public class GetComponentInDirectParent : AutosetAttribute
    {
        public GetComponentInDirectParent(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This will run FindObjectOfType(type) and set the field to the result.
    /// </summary>
    public class FindObjectOfType : AutosetAttribute
    {
        public FindObjectOfType(bool dontOverride = false) : base(dontOverride)
        {
        }
    }

    /// <summary>
    /// This will run Find(searchName) and set the field to the result, it also works for type of GameObject.
    /// </summary>
    public class Find : AutosetAttribute
    {
        public readonly string searchName;

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

        public FindInChildren(string searchName, bool dontOverride = false) : base(dontOverride)
        {
            this.searchName = searchName;
        }
    }
}