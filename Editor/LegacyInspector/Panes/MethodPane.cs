using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
	[Serializable]
	public class MethodSetup
	{
		public string MethodName = "";

		public object[] Values = new object[0];
	}

	public class MethodPane : BasePane
	{
		string methodOutput = "";
		float opacity = 0f;
		public void DrawMethods(Type componentType, object component, MethodInfo[] methods)
		{
			OldSettings settings = OldInspectorSidekick.Current.Settings; // Grab the active window's settings

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.alignment = TextAnchor.MiddleRight;
			GUIStyle normalButtonStyle = new GUIStyle(GUI.skin.button);
			normalButtonStyle.padding = normalButtonStyle.padding.SetLeft(100);
			normalButtonStyle.alignment = TextAnchor.MiddleLeft;

			List<MethodSetup> expandedMethods = OldInspectorSidekick.Current.PersistentData.ExpandedMethods;

			GUIStyle expandButtonStyle = new GUIStyle(GUI.skin.button);
			RectOffset padding = expandButtonStyle.padding;
			padding.left = 0;
			padding.right = 1;
			expandButtonStyle.padding = padding;

			for (int j = 0; j < methods.Length; j++)
			{
				MethodInfo method = methods[j];

				if(!string.IsNullOrEmpty(settings.SearchTerm) && !method.Name.Contains(settings.SearchTerm, StringComparison.InvariantCultureIgnoreCase))
				{
					// Does not match search term, skip it
					continue;
				}

				//				object[] customAttributes = method.GetCustomAttributes(false);
				EditorGUILayout.BeginHorizontal();
				ParameterInfo[] parameters = method.GetParameters();


				if(method.ReturnType == typeof(void))
					labelStyle.normal.textColor = Color.grey;
				else if(method.ReturnType.IsValueType)
					labelStyle.normal.textColor = new Color(0,0,1);
				else
					labelStyle.normal.textColor = new Color32(255,130,0,255);


				bool buttonClicked = GUILayout.Button(method.Name + " " + parameters.Length, normalButtonStyle);
				Rect lastRect = GUILayoutUtility.GetLastRect();
				lastRect.xMax = normalButtonStyle.padding.left;
				GUI.Label(lastRect, TypeUtility.NameForType(method.ReturnType), labelStyle);

				if(buttonClicked)
				{
					object[] arguments = null;
					if(parameters.Length > 0)
					{
						arguments = new object[parameters.Length];
						for (int i = 0; i < parameters.Length; i++) 
						{
							arguments[i] = TypeUtility.GetDefaultValue(parameters[i].ParameterType);
						}
					}

					methodOutput = FireMethod(method, component, arguments);
					opacity = 1f;
				}

				if(parameters.Length > 0)
				{
					string methodIdentifier = componentType.FullName + "." + method.Name;

					bool wasExpanded = expandedMethods.Any(item => item.MethodName == methodIdentifier);
					bool expanded = GUILayout.Toggle(wasExpanded, "▼", expandButtonStyle, GUILayout.Width(20));
					if(expanded != wasExpanded)
					{
						if(expanded)
						{
							MethodSetup methodSetup = new MethodSetup()
							{
								MethodName = methodIdentifier,
								Values = new object[parameters.Length],
							};
							expandedMethods.Add(methodSetup);
						}
						else
						{
							expandedMethods.RemoveAll(item => item.MethodName == methodIdentifier);
						}
					}

					EditorGUILayout.EndHorizontal();
					if(expanded)
					{
						MethodSetup methodSetup = expandedMethods.FirstOrDefault(item => item.MethodName == methodIdentifier);
						
						if(methodSetup.Values.Length != parameters.Length)
						{
							methodSetup.Values = new object[parameters.Length];
						}

						EditorGUI.indentLevel++;
						for (int i = 0; i < parameters.Length; i++) 
						{
//							VariablePane.DrawVariable(parameters[i].ParameterType, parameters[i].Name, GetDefaultValue(parameters[i].ParameterType), "", false);
							EditorGUI.BeginChangeCheck();
							object newValue = VariablePane.DrawVariable(parameters[i].ParameterType, parameters[i].Name, methodSetup.Values[i], "", false, null);
							if(EditorGUI.EndChangeCheck())
							{
								methodSetup.Values[i] = newValue;
							}
						}
						EditorGUI.indentLevel--;

						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(30);

						if(GUILayout.Button("Fire"))
						{
							methodOutput = FireMethod(method, component, methodSetup.Values);
							opacity = 1f;
						}
						EditorGUILayout.EndHorizontal();
							
						GUILayout.Space(20);
					}
				}
				else
				{
					EditorGUILayout.EndHorizontal();
				}
			
			}
		}

		string FireMethod(MethodInfo method, object component, object[] parameters)
		{
            string methodOutput;

            if (method.ReturnType == typeof(IEnumerator) && component is MonoBehaviour)
            {
                MonoBehaviour monoBehaviour = (MonoBehaviour)component;
                monoBehaviour.StartCoroutine(method.Name);
                methodOutput = "Method started as coroutine";
            }
            else
            {
                DateTime startTime = DateTime.UtcNow;
                object returnedObject = method.Invoke(component, parameters);
                TimeSpan duration = DateTime.UtcNow - startTime;
                if (method.ReturnType == typeof(void))
                {
                    methodOutput = "Method fired, no return type";
                }
                else
                {
                    if (returnedObject != null)
                    {
                        methodOutput = $"{method.Name} returned:\n{returnedObject}";
                    }
                    else
                    {
                        methodOutput = $"{method.Name} returned:\nnull";
                    }
                }

                if (duration.TotalSeconds > 0.5f)
                {
                    methodOutput += "\nExecution took " + duration.TotalSeconds + " seconds";
                }
            }

            return methodOutput;
        }


		public void PostDraw()
		{
			GUILayout.TextArea(methodOutput, GUILayout.Height(50));

			if(opacity > 0)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
//				Color baseColor = new Color(1,0.9f,0);
				Color baseColor = new Color(0,0,1);
				baseColor.a = 0.3f * opacity;
				GUI.color = baseColor;
				GUI.DrawTexture(lastRect, EditorGUIUtility.whiteTexture);

				baseColor.a = 0.8f * opacity;
				GUI.color = baseColor;
				float lineThickness = 2;

				GUI.DrawTexture(new Rect(lastRect.xMin, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);
				GUI.DrawTexture(new Rect(lastRect.xMax-lineThickness, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);

				GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMin, lastRect.width-lineThickness*2, lineThickness), EditorGUIUtility.whiteTexture);
				GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMax-lineThickness, lastRect.width-lineThickness*2, lineThickness), EditorGUIUtility.whiteTexture);
				GUI.color = Color.white;
				opacity -= AnimationHelper.DeltaTime;

				AnimationHelper.SetAnimationActive();
			}
		}
	}
}