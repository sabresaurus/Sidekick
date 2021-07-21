using System.Reflection;
using UnityEditor;
using System;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class FieldPane : VariablePane
    {
        public void DrawFields(Type componentType, object component, string searchTerm, FieldInfo[] fields)
        {
            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;

                if(!SearchMatches(searchTerm, fieldName))
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
                if (!string.IsNullOrEmpty(metaInformation))
                {
                    metaInformation += "\n";
                }
            
                Type fieldType = field.FieldType;

                VariableAttributes variableAttributes = VariableAttributes.None;

                // See https://stackoverflow.com/a/10261848
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    metaInformation += $"{TypeUtility.GetVisibilityName(field)} const {TypeUtility.NameForType(fieldType)} {fieldName}";
                    
                    variableAttributes = VariableAttributes.Constant;
                    
                    // Prevent SetValue as it will result in a FieldAccessException
                    isReadonly = true;
                }
                else if (field.IsStatic)
                {
                    metaInformation += $"{TypeUtility.GetVisibilityName(field)} static {TypeUtility.NameForType(fieldType)} {fieldName}";

                    variableAttributes = VariableAttributes.Static;
                }
                else
                {
                    metaInformation += $"{TypeUtility.GetVisibilityName(field)} {TypeUtility.NameForType(fieldType)} {fieldName}";    
                }

                if (isReadonly)
                {
                    GUI.enabled = false;
                }
                
                EditorGUI.BeginChangeCheck();
                object newValue = DrawVariable(fieldType, fieldName, component != null ? field.GetValue(component) : null, metaInformation, variableAttributes, true, componentType);
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