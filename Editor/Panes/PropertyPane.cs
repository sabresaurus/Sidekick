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

				object[] attributes = property.GetCustomAttributes(false);

				if(getMethod != null && component != null)
				{
					VariableAttributes variableAttributes = VariableAttributes.None;

					if (getMethod.IsStatic)
					{
						variableAttributes |= VariableAttributes.Static;
					}
						
					if (setMethod == null)
					{
						variableAttributes |= VariableAttributes.ReadOnly;
					}
					
					string tooltip = TypeUtility.GetTooltip(property, variableAttributes);
					
					if (InspectionExclusions.IsPropertyExcluded(componentType, property))
					{
						GUILayout.Label(new GUIContent(property.Name + " excluded due to rule", tooltip));
					}
					else if (AttributeHelper.IsObsoleteWithError(attributes))
					{
						// Don't try to get the value of properties that error on access
						GUILayout.Label(new GUIContent(property.Name + " obsolete with error", tooltip));
					}
					else
					{
						object oldValue = getMethod.Invoke(component, null);
						EditorGUI.BeginChangeCheck();
						object newValue = DrawVariable(property.PropertyType, property.Name, oldValue, tooltip, variableAttributes, true, componentType);
						if (EditorGUI.EndChangeCheck() && setMethod != null)
						{
							setMethod.Invoke(component, new[] {newValue});
						}
					}
				}
				else
				{
					GUILayout.Label(property.PropertyType + " " + property.Name);
				}
			}
		}
	}
}