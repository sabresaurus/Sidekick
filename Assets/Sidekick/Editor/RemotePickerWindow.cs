using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class RemotePickerWindow : EditorWindow
    {
        Vector2 scrollPosition = Vector2.zero;
        UnityObjectDescription[] objectDescriptions;
        GUIStyle lineStyle;

        int index = 0;

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
                if(displayDescriptions[i] != null)
                {
					content= new GUIContent(displayDescriptions[i].ObjectName);
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
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    e.Use();
                    index++;
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

        public static void Show(UnityObjectDescription[] objectDescriptions)
        {
            RemotePickerWindow window = new RemotePickerWindow();
            window.objectDescriptions = objectDescriptions;
            window.titleContent = new GUIContent("Select Object");
            window.ShowUtility();
        }
    }
}
