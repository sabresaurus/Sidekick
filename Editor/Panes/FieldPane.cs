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

            for (int j = 0; j < fields.Length; j++)
            {
                string fieldName = fields[j].Name;

                if (!string.IsNullOrEmpty(settings.SearchTerm) && !fieldName.Contains(settings.SearchTerm, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Does not match search term, skip it
                    continue;
                }

                bool isReadonly = false;
                string metaInformation = "";
                object[] customAttributes = fields[j].GetCustomAttributes(false);
                foreach (var customAttribute in customAttributes)
                {
                    metaInformation += $"[{customAttribute.GetType().Name.RemoveEnd("Attribute")}]";
                }
                
                // See https://stackoverflow.com/a/10261848
                if (fields[j].IsLiteral && !fields[j].IsInitOnly)
                {
                    metaInformation += "Constant";
                    
                    // Prevent SetValue as it will result in a FieldAccessException
                    isReadonly = true;
                }
                else if (fields[j].IsStatic)
                {
                    metaInformation += "Static";
                }

                Type fieldType = fields[j].FieldType;
                metaInformation += $"\n{TypeUtility.NameForType(fieldType)} {fieldName}";

                if (isReadonly)
                {
                    GUI.enabled = false;
                }
                
                EditorGUI.BeginChangeCheck();
                object newValue = DrawVariable(fieldType, fieldName, component != null ? fields[j].GetValue(component) : null, metaInformation, true, componentType);
                if (EditorGUI.EndChangeCheck() && !isReadonly)
                {
                    fields[j].SetValue(component, newValue);
                }
                
                if (isReadonly)
                {
                    GUI.enabled = true;
                }
            }
        }
    }
}