using UnityEngine;
using System.Reflection;
using UnityEditor;
using System.Linq;
using System;

namespace Sabresaurus.Sidekick
{
    public class InternalEditorGUILayout
    {
        public static Gradient GradientField(GUIContent content, Gradient gradient, params GUILayoutOption[] options)
        {
            // Gradient support exists in EditorGUILayout but is internal
            MethodInfo methodInfo = typeof(EditorGUILayout).GetMethod("GradientField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(GUIContent), typeof(Gradient), typeof(GUILayoutOption[]) }, null);
            object newValue = methodInfo.Invoke(null, new object[] { content, gradient, options });
            return newValue as Gradient;
        }
    }
}