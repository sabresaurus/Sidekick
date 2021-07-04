using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System;

namespace Sabresaurus.Sidekick
{
	public class FieldPane : VariablePane 
	{
		public void DrawFields(Type componentType, object component, FieldInfo[] fields)
		{
			OldSettings settings = OldInspectorSidekick.Current.Settings; // Grab the active window's settings

			for (int j = 0; j < fields.Length; j++)
			{	
				string fieldName = fields[j].Name;

				if(!string.IsNullOrEmpty(settings.SearchTerm) && !fieldName.Contains(settings.SearchTerm, StringComparison.InvariantCultureIgnoreCase))
				{
					// Does not match search term, skip it
					continue;
				}

				string metaSuffix = "";
				object[] customAttributes = fields[j].GetCustomAttributes(false);
				if(fields[j].IsPublic || AttributeHelper.IsSerializable(customAttributes))
				{
					metaSuffix += "(SF)";
				}

				if(fields[j].IsStatic)
				{
					metaSuffix += "(Static)";
				}

				EditorGUI.BeginChangeCheck();
				object newValue = DrawVariable(fields[j].FieldType, fieldName, component != null ? fields[j].GetValue(component) : null, metaSuffix, true, componentType);
				if(EditorGUI.EndChangeCheck())
				{
					fields[j].SetValue(component, newValue);
				}
			}
		}


	}
}