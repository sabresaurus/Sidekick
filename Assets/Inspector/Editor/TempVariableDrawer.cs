using UnityEditor;
using UnityEngine;
using System.Collections;
using Sabresaurus.Sidekick;

public static class TempVariableDrawer
{
    public static object Draw(WrappedVariable variable)
    {
        string name = variable.VariableName;

        if ((variable.Attributes & VariableAttributes.IsLiteral) == VariableAttributes.IsLiteral)
        {
            name += " [Const]";
        }
        else if ((variable.Attributes & VariableAttributes.IsStatic) == VariableAttributes.IsStatic)
        {
            name += " [Static]";
        }

        bool enableEditing = (variable.Attributes & (VariableAttributes.ReadOnly | VariableAttributes.IsLiteral)) == VariableAttributes.None;

        object objectValue = variable.Value;
        if(variable.DataType == DataType.Enum)
        {
            GUI.enabled = enableEditing;
            object newValue = VariablePane.DrawIndividualVariable(name, variable.Value.GetType(), variable.Value);
            GUI.enabled = true;
            return newValue;
        }
        else if (variable.DataType != DataType.Unknown)
        {
            GUI.enabled = enableEditing;
            object newValue = VariablePane.DrawIndividualVariable(name, variable.Value.GetType(), variable.Value);
            GUI.enabled = true;
            return newValue;
        }
        else
        {
            if (objectValue == null)
                EditorGUILayout.TextField(name, "{null}");
            else
                EditorGUILayout.TextField(name, variable.Value.ToString());

            return null;
        }
    }
}