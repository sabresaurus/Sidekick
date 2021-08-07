using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Specifies types for Sidekick to ignore when inspecting objects
    /// </summary>
    public static class InspectionExclusions
    {
        public static List<Type> GetExcludedTypes()
        {
            return new List<Type>()
            {
                // Built in exclusions
                typeof(System.Object),
                typeof(UnityEngine.Object),
                typeof(UnityEngine.Component),
                typeof(UnityEngine.Behaviour),
                typeof(UnityEngine.MonoBehaviour),
                typeof(UnityEngine.ScriptableObject),
                // Add any custom exclusions here
            };
        }
        
        public static bool IsPropertyExcluded(Type componentType, PropertyInfo property)
        {
            // Will instantiate at edit time
            if (componentType == typeof(MeshFilter) && property.Name == "mesh") return true;
            if ((componentType == typeof(Renderer) || componentType.IsSubclassOf(typeof(Renderer))) && property.Name == "material") return true;
            if ((componentType == typeof(Renderer) || componentType.IsSubclassOf(typeof(Renderer))) && property.Name == "materials") return true;
			
            // Will result in assertions if the matrix fails ValidTRS
            if (componentType == typeof(Matrix4x4) && property.Name == "rotation") return true;
            if (componentType == typeof(Matrix4x4) && property.Name == "lossyScale") return true;
            return false;
        }
    }
}