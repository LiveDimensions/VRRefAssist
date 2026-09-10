
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace VRRefAssist.AttributeUtils
{
    public static class FieldInfoExtensions
    {
        public static object[] GetValues(this FieldInfo field, object obj)
        {
            if (field.FieldType.IsArray)
            {
                return (object[])field.GetValue(obj);
            }

            if (field.FieldType.IsIList())
            {
                return ((IList)field.GetValue(obj)).Cast<object>().ToArray();
            }

            return new[] { field.GetValue(obj) };
        }

        /// <summary>
        /// Test if a type implements IList of T.
        /// </summary>
        private static bool IsIList(this Type type)
        {
            var interfaceTest = new Func<Type, Type>(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)
                    ? i.GetGenericArguments().Single()
                    : null);
            
            if (interfaceTest(type) != null) return true;

            foreach (Type i in type.GetInterfaces())
            {
                if (interfaceTest(i) != null) return true;
            }

            return false;
        }
    }
}
