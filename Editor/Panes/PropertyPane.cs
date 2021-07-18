using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System;

namespace Sabresaurus.Sidekick
{
	public class PropertyPane : VariablePane 
	{
		public void DrawProperties(Type componentType, object component, string searchTerm, PropertyInfo[] properties)
		{
			foreach (var property in properties)
			{
				if(property.DeclaringType == typeof(Component)
				   || property.DeclaringType == typeof(UnityEngine.Object))
				{
					continue;
				}

				if(!SearchMatches(searchTerm, property.Name))
				{
					// Does not match search term, skip it
					continue;
				}


				MethodInfo getMethod = property.GetGetMethod(true);
				MethodInfo setMethod = property.GetSetMethod(true);

				string metaInformation = "";
				if(setMethod == null)
				{
					GUI.enabled = false;
				}

				object[] attributes = property.GetCustomAttributes(false);

				// Don't try to get the value of properties that error on access
				bool isObsoleteWithError = AttributeHelper.IsObsoleteWithError(attributes);

				if(getMethod != null 
				   && !isObsoleteWithError
				   && !(componentType == typeof(MeshFilter) && property.Name == "mesh") )
				{
					object oldValue = getMethod.Invoke(component, null);
					EditorGUI.BeginChangeCheck();
					object newValue = DrawVariable(property.PropertyType, property.Name, oldValue, metaInformation, true, componentType);
					if(EditorGUI.EndChangeCheck() && setMethod != null)
					{
						setMethod.Invoke(component, new object[] { newValue });
					}
				}
				else
				{
					GUILayout.Label(property.PropertyType + " " + property.Name);
				}

				if(setMethod == null)
				{
					GUI.enabled = true;
				}
			}
		}
	}
}