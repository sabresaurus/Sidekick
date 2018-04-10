using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class RemotePickerWindow : EditorWindow
    {
        ComponentDescription componentDescription;
        Action<ComponentDescription, WrappedVariable, UnityObjectDescription> onValueChanged;
        Vector2 scrollPosition = Vector2.zero;
        WrappedVariable variable;
        UnityObjectDescription[] objectDescriptions;
        GUIStyle lineStyle;

        int index = 0;

        public UnityObjectDescription ActiveObjectDescription
        {
            get
            {
                index = Mathf.Clamp(index, 0, objectDescriptions.Length);
                if(index == 0)
                {
                    return null;
                }
                else
                {
					return objectDescriptions[index-1];
                }
            }
        }

        private void OnEnable()
        {
            lineStyle = new GUIStyle("PR Label");
        }

        void OnGUI()
        {
            Event e = Event.current;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            List<UnityObjectDescription> displayDescriptions = new List<UnityObjectDescription>();
            displayDescriptions.Add(null);
            displayDescriptions.AddRange(objectDescriptions);

            for (int i = 0; i < displayDescriptions.Count; i++)
            {
                GUIContent content = new GUIContent("None");
                if (displayDescriptions[i] != null)
                {
                    content = new GUIContent(displayDescriptions[i].ObjectName);
                }

                Rect rect = GUILayoutUtility.GetRect(50, 16);
                if (e.type == EventType.Repaint)
                {
                    lineStyle.Draw(rect, content, false, true, (index == i), true);
                }
                else if (e.type == EventType.MouseDown)
                {
                    if (rect.Contains(e.mousePosition))
                    {
                        if (e.button == 0)
                        {
                            index = i;
                            onValueChanged(componentDescription, variable, ActiveObjectDescription);

                            Repaint();
                        }
                    }
                }
            }

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.UpArrow)
                {
                    e.Use();
                    index--;
                    onValueChanged(componentDescription, variable, ActiveObjectDescription);
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    e.Use();
                    index++;
                    onValueChanged(componentDescription, variable, ActiveObjectDescription);
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                }
            }


            EditorGUILayout.EndScrollView();
        }

        void OnInspectorUpdate()
        {
            //Repaint();
        }

        public static void Show(ComponentDescription componentDescription, UnityObjectDescription[] objectDescriptions, WrappedVariable variable, Action<ComponentDescription, WrappedVariable, UnityObjectDescription> onValueChanged)
        {
            RemotePickerWindow window = new RemotePickerWindow();
            window.componentDescription = componentDescription;
            window.objectDescriptions = objectDescriptions;
            window.variable = variable;
            window.onValueChanged = onValueChanged;
            if (variable != null)
            {
                window.index = objectDescriptions.Select(item => item.InstanceID).ToList().IndexOf((int)variable.Value) + 1;
            }
            window.titleContent = new GUIContent("Select Object");
            window.ShowUtility();
        }
    }
}
