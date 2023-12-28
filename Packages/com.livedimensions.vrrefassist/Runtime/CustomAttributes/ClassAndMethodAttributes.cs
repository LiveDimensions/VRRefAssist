using System;
using JetBrains.Annotations;

namespace VRRefAssist
{
    /// <summary>
    /// Any class that has a serialized reference (SerializedField or public without NonSerialized) to the class using this attribute will be automatically set on build.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Singleton : Attribute
    {
    }

    /// <summary>
    /// Any method with this attribute will be called on build.
    /// Static and instance methods are supported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)] [MeansImplicitUse]
    public class RunOnBuildAttribute : Attribute
    {
        public readonly int executionOrder;
        
        /// <param name="executionOrder">Execution order for RunOnBuild methods, lower values execute first. Values higher than 1000 will execute after field-automation</param>
        public RunOnBuildAttribute(int executionOrder = 0)
        {
            this.executionOrder = executionOrder;
        }
    }
}