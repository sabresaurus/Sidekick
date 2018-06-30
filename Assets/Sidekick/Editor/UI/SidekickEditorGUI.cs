using System;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public static class SidekickEditorGUI
    {
        public static bool DrawHeader(GUIContent content, ref bool? toggle, SerializedProperty activeField = null, bool active = true)
        {
            Rect contentRect = GUILayoutUtility.GetRect(1f, 17f);

            Rect iconRect = contentRect;
            iconRect.xMin += 14f;
            iconRect.width = 30;

            Rect checkboxRect = contentRect;
            checkboxRect.xMin += 32f;
            checkboxRect.width = 10;

            Rect labelRect = contentRect;
            labelRect.xMin += 46f;
            labelRect.xMax -= 20f;

            Rect toggleRect = contentRect;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            contentRect.xMin = 0.0f;
            contentRect.xMax = Screen.width / EditorGUIUtility.pixelsPerPoint;
            contentRect.width += 4f;

            DrawHeaderBackground(contentRect);

            if(toggle.HasValue)
            {
                toggle = EditorGUI.Toggle(checkboxRect, toggle.Value);
            }

            GUIContent label = new GUIContent(content);
            label.image = null;
            GUIContent image = new GUIContent(content);
            image.text = null;

            using (new EditorGUI.DisabledScope(!active))
                EditorGUI.LabelField(iconRect, image, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!active))
                EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

            if (activeField != null)
            {
                activeField.serializedObject.Update();
                activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, smallTickbox);
                activeField.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                labelRect.xMin = 0;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDown)
            {
                if (labelRect.Contains(current.mousePosition))
                {
                    if (current.button == 0)
                    {
                        current.Use();
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool DrawHeaderWithFoldout(GUIContent label, bool expanded, ref bool? toggle)
        {
            bool ret = DrawHeader(label, ref toggle);
            if (Event.current.type == EventType.Repaint)
            {
                // Only draw the Foldout - don't use it as a button or get focus
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.x += 3;
                rect.x += EditorGUI.indentLevel * 15;
                rect.y += 1.5f;
                GUI.enabled = false;
                EditorStyles.foldout.Draw(rect, GUIContent.none, -1, expanded);
                GUI.enabled = true;
            }
            return ret;
        }

        private static Color splitterColor { get { return EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.6f, 0.6f, 0.6f); } }

        public static void DrawHeader2(GUIContent label)
        {
            Rect contentRect = GUILayoutUtility.GetRect(1f, 17f);
            float xMax = contentRect.xMax;
            Rect labelRect = contentRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 8f;

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10
            };
            Vector2 textSize = style.CalcSize(label);
            contentRect.xMax = Mathf.Max(contentRect.xMin, labelRect.xMax - textSize.x - 2);
            contentRect.yMin = (contentRect.yMax + contentRect.yMin) / 2f;
            contentRect.yMax = contentRect.yMin + 1;
            Color tColour = GUI.color;
            Color color = splitterColor;
            EditorGUI.DrawRect(contentRect, color);

            contentRect.xMax = xMax + 2;
            contentRect.xMin = labelRect.xMax + 2;
            EditorGUI.DrawRect(contentRect, color);
            
            GUI.color = tColour;
            EditorGUI.LabelField(labelRect, label, style);
        }

        public static void DrawSplitter()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f);
            rect.xMin = 0.0f;
            rect.width += 4f;
            if (Event.current.type != EventType.Repaint)
                return;
            EditorGUI.DrawRect(rect, splitterColor);
        }
        
        public static Rect DrawVerticalLine(float height)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(false));
            rect.height = height;
            rect.yMin -= 8.0f;
            if (Event.current.type != EventType.Repaint)
                return rect;
            EditorGUI.DrawRect(rect, splitterColor);
            return rect;
        }

        private static Texture _thumb;
        public static Texture thumb
        {
            get
            {
                if (_thumb == null)
                    _thumb = EditorGUIUtility.IconContent("eventpin on").image;
                return _thumb;
            }
        }

        public static void DrawHeaderBackground(Rect rect)
        {
            float colorChannel = !EditorGUIUtility.isProSkin ? 1f : 0.1f;
            Color color = new Color(colorChannel, colorChannel, colorChannel, 0.2f);
            //color = Color.blue;
            EditorGUI.DrawRect(rect, color);
        }

        public static bool Toggle(bool value, string title, params GUILayoutOption[] options)
        {
            value = GUILayout.Toggle(value, title, EditorStyles.toolbarButton, options);
            return value;
        }

        public static T EnumFlagsToggle<T>(T value, T flag, string title, params GUILayoutOption[] options) where T : struct, IConvertible
        {
            bool present = ((Enum)(object)value).HasFlagByte((Enum)Enum.ToObject(value.GetType(), flag));

            bool newPresent = GUILayout.Toggle(present, title, EditorStyles.toolbarButton, options);
            if (newPresent != present)
            {
                value = (T)(IConvertible)(byte)((byte)(IConvertible)value ^ (byte)(IConvertible)flag);
            }
            return value;
        }

        private static GUIStyle _smallTickbox;
        public static GUIStyle smallTickbox
        {
            get { return _smallTickbox ?? (_smallTickbox = new GUIStyle("ShurikenCheckMark")); }
        }
    }
}
