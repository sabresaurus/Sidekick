using System.Reflection;
using UnityEditor;
using System;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class FieldPane : VariablePane
    {
        public void DrawFields(Type componentType, object component, FieldInfo[] fields)
        {
            SidekickSettings settings = SidekickWindow.Current.Settings; // Grab the active window's settings

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;

                if (!string.IsNullOrEmpty(settings.SearchTerm) && !fieldName.Contains(settings.SearchTerm, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Does not match search term, skip it
                    continue;
                }

                bool isReadonly = false;
                string metaInformation = "";
                object[] customAttributes = field.GetCustomAttributes(false);
                foreach (var customAttribute in customAttributes)
                {
                    metaInformation += $"[{customAttribute.GetType().Name.RemoveEnd("Attribute")}]";
                }

                // See https://stackoverflow.com/a/10261848
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    metaInformation += "Constant";

                    // Prevent SetValue as it will result in a FieldAccessException
                    isReadonly = true;
                }
                else if (field.IsStatic)
                {
                    metaInformation += "Static";
                }
            
                Type fieldType = field.FieldType;

                if (!string.IsNullOrEmpty(metaInformation))
                {
                    metaInformation += "\n";
                }
                
                metaInformation += $"{TypeUtility.GetVisibilityName(field)} {TypeUtility.NameForType(fieldType)} {fieldName}";

                if (isReadonly)
                {
                    GUI.enabled = false;
                }
                
                EditorGUI.BeginChangeCheck();
                object newValue = DrawVariable(fieldType, fieldName, component != null ? field.GetValue(component) : null, metaInformation, true, componentType);
                if (EditorGUI.EndChangeCheck() && !isReadonly)
                {
                    field.SetValue(component, newValue);
                }
                
                if (isReadonly)
                {
                    GUI.enabled = true;
                }
            }
        }
    }
}