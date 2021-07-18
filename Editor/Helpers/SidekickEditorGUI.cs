using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public static class SidekickEditorGUI
    {
        private static MethodInfo beginLabelHighlight = typeof(EditorGUI).GetMethod("BeginLabelHighlight", BindingFlags.Static | BindingFlags.NonPublic);
        private static MethodInfo endLabelHighlight = typeof(EditorGUI).GetMethod("EndLabelHighlight", BindingFlags.Static | BindingFlags.NonPublic);
        
        private static Color splitterColor => EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.6f, 0.6f, 0.6f);

        public static readonly Texture BackIcon = EditorGUIUtility.IconContent("back").image; 
        public static readonly Texture ForwardIcon = EditorGUIUtility.IconContent("forward").image;
        public static readonly Texture LockIconOff = EditorGUIUtility.IconContent("LockIcon").image;
        public static readonly Texture LockIconOn = EditorGUIUtility.IconContent("LockIcon-On").image;
        public static readonly Texture MoreOptions = EditorGUIUtility.IconContent("Toolbar Plus").image;

        public static void DrawTypeChainHeader(GUIContent label)
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

        public static void DrawSplitter(float alpha = 1)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f);
            rect.xMin = 0.0f;
            rect.width += 4f;
            if (Event.current.type != EventType.Repaint)
                return;
            Color drawColor = splitterColor;
            drawColor.a = alpha;
            EditorGUI.DrawRect(rect, drawColor);
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
        
        public static void BeginLabelHighlight(string searchContext)
        {
            BeginLabelHighlight(searchContext, (Color)new Color32(49, 105,172,255), Color.white);
        }

        public static void BeginLabelHighlight(string searchContext, Color searchHighlightSelectionColor, Color searchHighlightColor)
        {
            beginLabelHighlight.Invoke(null, new object[]
            {
                searchContext, searchHighlightSelectionColor, searchHighlightColor
            });
        }
        
        public static void EndLabelHighlight()
        {
            endLabelHighlight.Invoke(null, null);
        }
    }
}
