using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
    public class MethodPane : BasePane
    {
        Vector2 outputScrollPosition = Vector2.zero;
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

                labelStyle.fontSize = 10;

                var genericArguments = method.GetGenericArguments();
                
                GUIContent buttonLabel = new GUIContent("", "Click to fire with defaults");
                if (genericArguments.Length != 0)
                {
                    string genericArgumentsDisplay = string.Join(", ", genericArguments.Select(item => item.Name));
                    buttonLabel.text = $"{method.Name} <{genericArgumentsDisplay}> {parameters.Length}";
                }
                else
                {
                    buttonLabel.text = $"{method.Name} {parameters.Length}";
                }

                using (new EditorGUI.DisabledScope(method.IsGenericMethod))
                {
                    bool buttonClicked = GUILayout.Button(buttonLabel, normalButtonStyle);
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
                        var output = FireMethod(method, component, arguments, null);
                        outputObjects.AddRange(output);
                        opacity = 1f;
                    }
                }

                if (parameters.Length > 0 || genericArguments.Length > 0)
                {
                    string methodIdentifier = TypeUtility.GetMethodIdentifier(method);
                    
                    bool wasExpanded = expandedMethods.Any(item => item.MethodIdentifier == methodIdentifier);
                    string label = wasExpanded ? "▲" : "▼";
                    bool expanded = GUILayout.Toggle(wasExpanded, label, expandButtonStyle, GUILayout.Width(20));
                    if (expanded != wasExpanded)
                    {
                        if (expanded)
                        {
                            MethodSetup methodSetup = new MethodSetup()
                            {
                                MethodIdentifier = methodIdentifier,
                                Values = new object[parameters.Length],
                                GenericArguments = new Type[genericArguments.Length],
                            };
                            expandedMethods.Add(methodSetup);
                        }
                        else
                        {
                            expandedMethods.RemoveAll(item => item.MethodIdentifier == methodIdentifier);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    if (expanded)
                    {
                        MethodSetup methodSetup = expandedMethods.FirstOrDefault(item => item.MethodIdentifier == methodIdentifier);

                        if (methodSetup.Values.Length != parameters.Length)
                        {
                            methodSetup.Values = new object[parameters.Length];
                        }

                        EditorGUI.indentLevel++;

                        for (var i = 0; i < genericArguments.Length; i++)
                        {
                            Type genericArgument = genericArguments[i];
                            string displayLabel = genericArgument.Name;

                            Type[] constraints = genericArgument.GetGenericParameterConstraints();
                            if (constraints.Length != 0)
                            {
                                displayLabel += $" ({string.Join(", ", constraints.Select(item => item.Name))})";
                            }

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(displayLabel, TypeUtility.NameForType(methodSetup.GenericArguments[i]));
                            var popupRect = GUILayoutUtility.GetLastRect();
                            popupRect.width = EditorGUIUtility.currentViewWidth;

                            var selectTypeButtonLabel = new GUIContent("Select");
                            if (GUILayout.Button(selectTypeButtonLabel, EditorStyles.miniButton))
                            {
                                int index = i;
                                TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), type => methodSetup.GenericArguments[index] = type, constraints);
                                dropdown.Show(popupRect);
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            int index = i;
                            VariablePane.DrawVariable(parameters[i].ParameterType, parameters[i].Name, methodSetup.Values[i], "", VariablePane.VariableAttributes.None, null, false, null, newValue =>
                            {
                                methodSetup.Values[index] = newValue;
                            });
                        }

                        EditorGUI.indentLevel--;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(30);

                        bool anyGenericArgumentsMissing = methodSetup.GenericArguments.Any(item => item == null);

                        using (new EditorGUI.DisabledScope(anyGenericArgumentsMissing))
                        {
                            if (GUILayout.Button("Fire"))
                            {
                                var output = FireMethod(method, component, methodSetup.Values, methodSetup.GenericArguments);
                                outputObjects.AddRange(output);
                                opacity = 1f;
                            }
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

        List<object> FireMethod(MethodInfo method, object component, object[] parameters, Type[] genericTypes)
        {
            if (method.ReturnType == typeof(IEnumerator) && component is MonoBehaviour monoBehaviour)
            {
                return new List<object> {monoBehaviour.StartCoroutine(method.Name)};
            }
            
            if(method.IsGenericMethod)
            {
                method = method.MakeGenericMethod(genericTypes);
            }
            object result = method.Invoke(component, parameters);
            List<object> outputObjects = new List<object> {result};

            for (int i = 0; i < method.GetParameters().Length; i++)
            {
                if (method.GetParameters()[i].IsOut)
                {
                    outputObjects.Add(parameters[i]);
                }
            }

            return outputObjects;
        }

        public void PostDraw()
        {
            SidekickEditorGUI.DrawSplitter();
            GUILayout.Label("Output", EditorStyles.boldLabel);
            outputScrollPosition = EditorGUILayout.BeginScrollView(outputScrollPosition, GUILayout.MaxHeight(100));
            foreach (var outputObject in outputObjects)
            {
                if(TypeUtility.IsNotNull(outputObject))
                {
                    string name = outputObject switch
                    {
                        Object unityObject => $"{unityObject.name}",
                        _ => outputObject.ToString()
                    };

                    if (GUILayout.Button($"Select {name} ({TypeUtility.NameForType(outputObject.GetType())})"))
                    {
                        SidekickWindow.Current.SetSelection(outputObject);
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUILayout.Button("null");
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
            
            EditorGUILayout.EndScrollView();
        }
    }
}