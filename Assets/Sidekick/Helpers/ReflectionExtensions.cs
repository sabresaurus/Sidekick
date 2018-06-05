using UnityEngine;
using System.Collections;
using System;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    public static class ReflectionExtensions
    {
        public const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static FieldInfo GetFieldAll(this Type type, string name)
        {
            while (type != null)
            {
                FieldInfo fieldInfo = type.GetField(name, BINDING_FLAGS);

                if (fieldInfo != null)
                {
                    return fieldInfo;
                }
                type = type.BaseType;
            }
            // None matched
            return null;
        }

        public static PropertyInfo GetPropertyAll(this Type type, string name)
        {
            while (type != null)
            {
                PropertyInfo propertyInfo = type.GetProperty(name, BINDING_FLAGS);

                if (propertyInfo != null)
                {
                    return propertyInfo;
                }
                type = type.BaseType;
            }
            // None matched
            return null;
        }

        public static MethodInfo GetMethodAll(this Type type, string name)
        {
            while (type != null)
            {
                MethodInfo methodInfo = type.GetMethod(name, BINDING_FLAGS);

                if (methodInfo != null)
                {
                    return methodInfo;
                }
                type = type.BaseType;
            }
            // None matched
            return null;
        }

        public static MethodInfo GetMethodAll(this Type type, string name, Type[] parameterTypes)
        {
            while (type != null)
            {
                MethodInfo methodInfo = type.GetMethod(name, BINDING_FLAGS, null, parameterTypes, null);

                if (methodInfo != null)
                {
                    return methodInfo;
                }
                type = type.BaseType;
            }
            // None matched
            return null;
        }
    }
}