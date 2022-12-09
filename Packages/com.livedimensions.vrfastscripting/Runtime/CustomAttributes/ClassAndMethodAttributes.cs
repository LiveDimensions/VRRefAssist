using System;
using JetBrains.Annotations;

namespace VRFastScripting
{
    /// <summary>
    /// Any class that has a serialized reference (SerializedField or public without NonSerialized) to the class using this attribute will be automatically set on build.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Singleton : Attribute
    {
    }

    /// <summary>
    /// Any static method with this attribute will be called on build.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)] [MeansImplicitUse]
    public class RunOnBuildAttribute : Attribute
    {
        public readonly int executionOrder;

        public RunOnBuildAttribute(int executionOrder = 0)
        {
            this.executionOrder = executionOrder;
        }
    }
}