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
				
				VariableAttributes variableAttributes = VariableAttributes.None;

				if (getMethod != null && getMethod.IsStatic || setMethod != null && setMethod.IsStatic)
				{
					variableAttributes |= VariableAttributes.Static;
				}
						
				if (setMethod == null)
				{
					variableAttributes |= VariableAttributes.ReadOnly;
				}
				
				if (getMethod == null)
				{
					variableAttributes |= VariableAttributes.WriteOnly;
				}
					
				string tooltip = TypeUtility.GetTooltip(property, variableAttributes);

				if (getMethod == null)
				{
					EditorGUILayout.LabelField(new GUIContent(property.Name, tooltip), new GUIContent("No get method", SidekickEditorGUI.ErrorIconSmall));
				}
				else if (InspectionExclusions.IsPropertyExcluded(componentType, property))
				{
					EditorGUILayout.LabelField(new GUIContent(property.Name, tooltip), new GUIContent("Excluded due to rule", SidekickEditorGUI.ErrorIconSmall, "See InspectionExclusions.cs"));
				}
				else if (AttributeHelper.IsObsoleteWithError(attributes))
				{
					// Don't try to get the value of properties that error on access
					EditorGUILayout.LabelField(new GUIContent(property.Name, tooltip), new GUIContent("[Obsolete] error", SidekickEditorGUI.ErrorIconSmall));
				}
				else
				{
					object oldValue = null;
					Exception error = null;
					try
					{
						oldValue = getMethod.Invoke(component, null);
					}
					catch (Exception e)
					{
						error = e;
					}

					if (error != null)
					{
						EditorGUILayout.LabelField(new GUIContent(property.Name, tooltip), new GUIContent(error.GetType().Name, SidekickEditorGUI.ErrorIconSmall));
					}
					else
					{
						EditorGUI.BeginChangeCheck();
						object newValue = DrawVariable(property.PropertyType, property.Name, oldValue, tooltip, variableAttributes, true, componentType);
						if (EditorGUI.EndChangeCheck() && setMethod != null)
						{
							setMethod.Invoke(component, new[] {newValue});
						}
					}
				}
			}
		}
	}
}