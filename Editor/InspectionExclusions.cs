using System;
using System.Collections.Generic;

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
    }
}