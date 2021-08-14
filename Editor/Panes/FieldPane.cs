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

                Type fieldType = field.FieldType;

                VariableAttributes variableAttributes = VariableAttributes.None;

                // See https://stackoverflow.com/a/10261848
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    variableAttributes = VariableAttributes.Constant;
                    
                    // Prevent SetValue as it will result in a FieldAccessException
                    variableAttributes |= VariableAttributes.ReadOnly;
                }
                if (field.IsStatic)
                {
                    variableAttributes |= VariableAttributes.Static;
                }
                string tooltip = TypeUtility.GetTooltip(field, variableAttributes);

                if ((variableAttributes & VariableAttributes.ReadOnly) != 0)
                {
                    GUI.enabled = false;
                }
                
                DrawVariable(fieldType, fieldName, component != null ? field.GetValue(component) : null, tooltip, variableAttributes, true, componentType, newValue =>
                {
                    if ((variableAttributes & VariableAttributes.ReadOnly) == 0)
                    {
                        field.SetValue(component, newValue);
                    }
                });

                if ((variableAttributes & VariableAttributes.ReadOnly) != 0)
                {
                    GUI.enabled = true;
                }
            }
        }
    }
}