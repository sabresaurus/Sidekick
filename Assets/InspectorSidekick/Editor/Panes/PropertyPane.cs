using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System;

namespace Sabresaurus.Sidekick
{
	public class PropertyPane : VariablePane 
	{
		public void DrawProperties(Type componentType, object component, PropertyInfo[] properties)
		{
			Settings settings = InspectorSidekick.Current.Settings; // Grab the active window's settings

			for (int j = 0; j < properties.Length; j++)
			{
				if(properties[j].DeclaringType == typeof(Component)
					|| properties[j].DeclaringType == typeof(UnityEngine.Object))
				{
					continue;
				}

				if(!string.IsNullOrEmpty(settings.SearchTerm) && !properties[j].Name.Contains(settings.SearchTerm, StringComparison.InvariantCultureIgnoreCase))
				{
					// Does not match search term, skip it
					continue;
				}


				MethodInfo getMethod = properties[j].GetGetMethod(true);
				MethodInfo setMethod = properties[j].GetSetMethod(true);

				string metaSuffix = "";
				if(setMethod == null)
				{
					GUI.enabled = false;
				}

				object[] attributes = properties[j].GetCustomAttributes(false);

				// Don't try to get the value of properties that error on access
				bool isObsoleteWithError = AttributeHelper.IsObsoleteWithError(attributes);

				if(getMethod != null 
					&& !isObsoleteWithError
					&& !(componentType == typeof(MeshFilter) && properties[j].Name == "mesh") )
				{
					object oldValue = getMethod.Invoke(component, null);
					EditorGUI.BeginChangeCheck();
					object newValue = DrawVariable(properties[j].PropertyType, properties[j].Name, oldValue, metaSuffix, true, componentType);
					if(EditorGUI.EndChangeCheck() && setMethod != null)
					{
						setMethod.Invoke(component, new object[] { newValue });
					}
				}
				else
				{
					GUILayout.Label(properties[j].PropertyType + " " + properties[j].Name);
				}

				if(setMethod == null)
				{
					GUI.enabled = true;
				}
			}
		}
	}
}