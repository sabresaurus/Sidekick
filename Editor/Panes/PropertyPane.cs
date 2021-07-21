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

				if (property.GetIndexParameters().Length != 0)
				{
					// Indexer, show it in Methods instead as it takes parameters
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

				if(getMethod != null 
				   && component != null)
				{
					if (InspectionExclusions.IsPropertyExcluded(componentType, property))
					{
						GUILayout.Label(property.Name + " excluded due to rule");
					}
					else if (AttributeHelper.IsObsoleteWithError(attributes))
					{
						// Don't try to get the value of properties that error on access
						GUILayout.Label(property.Name + " obsolete with error");
					}
					else
					{
						object oldValue = getMethod.Invoke(component, null);
						EditorGUI.BeginChangeCheck();
						object newValue = DrawVariable(property.PropertyType, property.Name, oldValue, metaInformation, VariableAttributes.None, true, componentType);
						if (EditorGUI.EndChangeCheck() && setMethod != null)
						{
							setMethod.Invoke(component, new object[] {newValue});
						}
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