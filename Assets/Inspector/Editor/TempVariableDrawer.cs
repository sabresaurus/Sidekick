using UnityEditor;
using UnityEngine;
using System.Collections;
using Sabresaurus.Sidekick;

public static class TempVariableDrawer
{
    public static object Draw(WrappedVariable variable)
    {
        object objectValue = variable.Value;
        if(variable.DataType != DataType.Unknown)
        {
            return VariablePane.DrawIndividualVariable(variable.VariableName, variable.Value.GetType(), variable.Value);
        }
        else
        {
			if (objectValue == null)
				EditorGUILayout.TextField(variable.VariableName, "{null}");
			else
				EditorGUILayout.TextField(variable.VariableName, variable.Value.ToString());
            
			return null;
        }
    }
}