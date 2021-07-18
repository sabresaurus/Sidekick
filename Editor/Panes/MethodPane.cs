using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
        private List<object> outputObjects = new List<object>();

        float opacity = 0f;

        public void DrawMethods(Type componentType, object component, string searchTerm, MethodInfo[] methods)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleRight};
            GUIStyle normalButtonStyle = new GUIStyle(GUI.skin.button) {alignment = TextAnchor.MiddleLeft};
            normalButtonStyle.padding = normalButtonStyle.padding.SetLeft(100);

            List<MethodSetup> expandedMethods = SidekickWindow.Current.PersistentData.ExpandedMethods;

            GUIStyle expandButtonStyle = new GUIStyle(GUI.skin.button);
            RectOffset padding = expandButtonStyle.padding;
            padding.left = 0;
            padding.right = 1;
            expandButtonStyle.padding = padding;

            foreach (MethodInfo method in methods)
            {
                if(!SearchMatches(searchTerm, method.Name))
                {
                    // Does not match search term, skip it
                    continue;
                }

                //				object[] customAttributes = method.GetCustomAttributes(false);
                EditorGUILayout.BeginHorizontal();
                ParameterInfo[] parameters = method.GetParameters();


                if (method.ReturnType == typeof(void))
                    labelStyle.normal.textColor = Color.grey;
                else if (method.ReturnType.IsValueType)
                    labelStyle.normal.textColor = new Color(0, 0, 1);
                else
                    labelStyle.normal.textColor = new Color32(255, 130, 0, 255);


                bool buttonClicked = GUILayout.Button(method.Name + " " + parameters.Length, normalButtonStyle);
                Rect lastRect = GUILayoutUtility.GetLastRect();
                lastRect.xMax = normalButtonStyle.padding.left;
                GUI.Label(lastRect, TypeUtility.NameForType(method.ReturnType), labelStyle);

                if (buttonClicked)
                {
                    object[] arguments = null;
                    if (parameters.Length > 0)
                    {
                        arguments = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            arguments[i] = TypeUtility.GetDefaultValue(parameters[i].ParameterType);
                        }
                    }
                    object output = FireMethod(method, component, arguments);
                    outputObjects.Add(output);
                    opacity = 1f;
                }

                if (parameters.Length > 0)
                {
                    string methodIdentifier = componentType.FullName + "." + method.Name;

                    bool wasExpanded = expandedMethods.Any(item => item.MethodName == methodIdentifier);
                    bool expanded = GUILayout.Toggle(wasExpanded, "▼", expandButtonStyle, GUILayout.Width(20));
                    if (expanded != wasExpanded)
                    {
                        if (expanded)
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
                    if (expanded)
                    {
                        MethodSetup methodSetup = expandedMethods.FirstOrDefault(item => item.MethodName == methodIdentifier);

                        if (methodSetup.Values.Length != parameters.Length)
                        {
                            methodSetup.Values = new object[parameters.Length];
                        }

                        EditorGUI.indentLevel++;
                        for (int i = 0; i < parameters.Length; i++)
                        {
//							VariablePane.DrawVariable(parameters[i].ParameterType, parameters[i].Name, GetDefaultValue(parameters[i].ParameterType), "", false);
                            EditorGUI.BeginChangeCheck();
                            object newValue = VariablePane.DrawVariable(parameters[i].ParameterType, parameters[i].Name, methodSetup.Values[i], "", false, null);
                            if (EditorGUI.EndChangeCheck())
                            {
                                methodSetup.Values[i] = newValue;
                            }
                        }

                        EditorGUI.indentLevel--;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(30);

                        if (GUILayout.Button("Fire"))
                        {
                            object output = FireMethod(method, component, methodSetup.Values);
                            outputObjects.Add(output);
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

        object FireMethod(MethodInfo method, object component, object[] parameters)
        {
            if (method.ReturnType == typeof(IEnumerator) && component is MonoBehaviour monoBehaviour)
            {
                return monoBehaviour.StartCoroutine(method.Name);
            }

            return method.Invoke(component, parameters);
        }

        public void PostDraw()
        {
            GUILayout.Label("Output", EditorStyles.boldLabel);
            foreach (var outputObject in outputObjects)
            {
                if (outputObject != null)
                {
                    string name = outputObject switch
                    {
                        Object unityObject => $"{unityObject.name}({outputObject.GetType().FullName})",
                        _ => outputObject.GetType().FullName
                    };

                    if (GUILayout.Button("Select " + name))
                    {
                        SidekickWindow.Current.SetSelection(outputObject);
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUILayout.Button("Null");
                    }
                }
            }

            if (opacity > 0)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Color baseColor = new Color(0, 0, 1, 0.3f * opacity);
                GUI.color = baseColor;
                GUI.DrawTexture(lastRect, EditorGUIUtility.whiteTexture);

                baseColor.a = 0.8f * opacity;
                GUI.color = baseColor;
                float lineThickness = 2;

                GUI.DrawTexture(new Rect(lastRect.xMin, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lastRect.xMax - lineThickness, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);

                GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMin, lastRect.width - lineThickness * 2, lineThickness), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMax - lineThickness, lastRect.width - lineThickness * 2, lineThickness), EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
                opacity -= AnimationHelper.DeltaTime;

                AnimationHelper.SetAnimationActive();
            }
        }
    }
}