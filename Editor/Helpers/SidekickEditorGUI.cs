using System;
using System.Linq;
using System.Reflection;
#if ECS_EXISTS
using Unity.Transforms;
using Unity.Entities;
#endif
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
    public static class SidekickEditorGUI
    {
        private static MethodInfo beginLabelHighlight = typeof(EditorGUI).GetMethod("BeginLabelHighlight", BindingFlags.Static | BindingFlags.NonPublic);
        private static MethodInfo endLabelHighlight = typeof(EditorGUI).GetMethod("EndLabelHighlight", BindingFlags.Static | BindingFlags.NonPublic);
        
        private static Color splitterColor => EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.6f, 0.6f, 0.6f);

        public static readonly Texture BackIcon = EditorGUIUtility.TrIconContent("back").image; 
        public static readonly Texture ForwardIcon = EditorGUIUtility.TrIconContent("forward").image;
        public static readonly Texture MoreOptions = EditorGUIUtility.TrIconContent("Toolbar Plus").image;
        
        public static readonly Texture ErrorIcon = EditorGUIUtility.TrIconContent("console.erroricon").image;
        public static readonly Texture WarningIcon = EditorGUIUtility.TrIconContent("console.warnicon").image;
        public static readonly Texture InfoIcon = EditorGUIUtility.TrIconContent("console.infoicon").image;
        
        public static readonly Texture ErrorIconSmall = EditorGUIUtility.TrIconContent("console.erroricon.sml").image;
        public static readonly Texture WarningIconSmall = EditorGUIUtility.TrIconContent("console.warnicon.sml").image;
        public static readonly Texture InfoIconSmall = EditorGUIUtility.TrIconContent("console.infoicon.sml").image;
        
        public static readonly Texture BlueDotIcon = EditorGUIUtility.TrIconContent("sv_icon_dot1_pix16_gizmo").image;
        public static readonly Texture TransformIcon = EditorGUIUtility.TrIconContent("Transform Icon").image;
        
        public static Texture LockIconOff => new GUIStyle("IN LockButton").normal.scaledBackgrounds[0];
        public static Texture LockIconOn => new GUIStyle("IN LockButton").onNormal.scaledBackgrounds[0];

        private static Texture2D staticBackgroundLightSkin;
        private static Texture2D staticBackgroundDarkSkin;

        private static readonly Color staticBackgroundDarkTintColor = new Color32(227, 227, 227, 255);
        private static readonly Color staticBackgroundLightTintColor = new Color32(240, 240, 240, 255);
        
        public static Color StaticBackgroundTintColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return staticBackgroundDarkTintColor;
                }
                else
                {
                    return staticBackgroundLightTintColor;
                }
            }
        }

        public static Texture2D StaticBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    if (staticBackgroundDarkSkin == null)
                    {
                        staticBackgroundDarkSkin = new Texture2D(1, 1);
                        staticBackgroundDarkSkin.SetPixel(0,0, new Color32(50, 50, 50, 255));
                        staticBackgroundDarkSkin.Apply();
                    }

                    return staticBackgroundDarkSkin;
                }
                else
                {
                    if (staticBackgroundLightSkin == null)
                    {
                        staticBackgroundLightSkin = new Texture2D(1, 1);
                        staticBackgroundLightSkin.SetPixel(0,0, new Color32(186, 186, 186, 255));
                        staticBackgroundLightSkin.Apply();
                    }

                    return staticBackgroundLightSkin;
                }
            }
        }
        
        

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

        public static bool DetectClickInRect(Rect rect, int mouseButton = 0)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == mouseButton && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        public static void ButtonWithOptions(
            GUIContent content,
            out bool mainPressed,
            out bool optionsPressed)
        {
            mainPressed = false;
            optionsPressed = false;
            
            GUIStyle style = EditorStyles.toolbarDropDown;
            style.alignment = TextAnchor.MiddleCenter;
            
            Rect rect = GUILayoutUtility.GetRect(content, style);

            // Right click
            if (DetectClickInRect(rect, 1))
            {
                optionsPressed = true;
            }
            
            // Left click on the right side drop down icon
            if (EditorGUI.DropdownButton(new Rect(rect.xMax - style.padding.right, rect.y, style.padding.right, rect.height), GUIContent.none, FocusType.Passive, GUIStyle.none))
            {
                optionsPressed = true;
            }

            // Left click on main button
            if (GUI.Button(rect, content, style))
            {
                mainPressed = true;
            }

            Rect rightRect = rect;
            rightRect.xMin = rect.xMax - 18;
            rightRect.width = 1;
            rightRect.height = 12;
            rightRect.y = (rect.height - 12) / 2;
            GUI.DrawTexture(rightRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color32(0,0,0,38), Vector4.zero, Vector4.zero);
        }

        public static Texture GetIcon(object o, Type type)
        {
            GUIContent objectContent = EditorGUIUtility.ObjectContent(o as Object, type);
            if (objectContent.image != null)
            {
                return objectContent.image;
            }
#if ECS_EXISTS
            if (type == typeof(Translation)
            || type == typeof(Rotation)
            || type == typeof(Scale)
            || type == typeof(LocalToWorld))
            {
                return TransformIcon;
            }
            else if (type.GetInterfaces().Contains(typeof(IComponentData)))
            {
                return BlueDotIcon;
            }
#endif
            return null;
        }
    }
}
