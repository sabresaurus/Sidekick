﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_MATH_EXISTS
using Unity.Mathematics;
#endif
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public abstract class VariablePane : BasePane
    {
        [Flags]
        public enum VariableAttributes
        {
            None = 0,
            Static = 1 << 0,
            Constant = 1 << 1,
            ReadOnly = 1 << 2,
            WriteOnly = 1 << 3,
        }

        public static void DrawVariable(Type fieldType, string fieldName, object fieldValue, string tooltip, VariableAttributes variableAttributes, IEnumerable<Attribute> customAttributes, bool allowExtensions, Type contextType, Action<object> changeCallback)
        {
            if ((variableAttributes & VariableAttributes.Static) != 0)
            {
                var style = new GUIStyle {normal = {background = SidekickEditorGUI.StaticBackground}};
                EditorGUILayout.BeginVertical(style);
            }

            GUIStyle expandButtonStyle = new GUIStyle(GUI.skin.button);
            RectOffset padding = expandButtonStyle.padding;
            padding.left = 0;
            padding.right = 1;
            expandButtonStyle.padding = padding;

            fieldValue ??= TypeUtility.GetDefaultValue(fieldType);

            string displayName = SidekickUtility.NicifyIdentifier(fieldName);

            GUIContent label = new GUIContent(displayName, tooltip);

            bool isArray = fieldType.IsArray;
            bool isGenericList = TypeUtility.IsGenericList(fieldType);
            bool isGenericDictionary = TypeUtility.IsGenericDictionary(fieldType);

            if (isGenericDictionary)
            {
                EditorGUILayout.BeginHorizontal();

                string expandedID = fieldType.FullName + fieldName;
                bool expanded = DrawHeader(expandedID, label, (variableAttributes & VariableAttributes.Static) != 0);

                int count = 0;
                if (fieldValue != null)
                {
                    EditorGUI.BeginDisabledGroup(true);

                    count = (int) fieldType.GetProperty("Count").GetValue(fieldValue);

                    EditorGUILayout.IntField(count, GUILayout.Width(80));
                    EditorGUI.EndDisabledGroup();
                }

                if (allowExtensions)
                {
                    DrawExtensions(fieldValue, expandButtonStyle);
                }

                EditorGUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUI.indentLevel++;

                    if (fieldValue != null)
                    {
                        FieldInfo entriesArrayField = fieldType.GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
                        IList entriesArray = (IList) entriesArrayField.GetValue(fieldValue);
                        Type elementType = TypeUtility.GetFirstElementType(entriesArrayField.FieldType);
                        FieldInfo elementKeyFieldInfo = elementType.GetField("key", BindingFlags.Public | BindingFlags.Instance);
                        FieldInfo elementValueFieldInfo = elementType.GetField("value", BindingFlags.Public | BindingFlags.Instance);
                        int oldIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        for (int i = 0; i < count; i++)
                        {
                            object entry = entriesArray[i];

                            EditorGUILayout.BeginHorizontal();

                            object key = elementKeyFieldInfo.GetValue(entry);
                            object value = elementValueFieldInfo.GetValue(entry);

                            using (new EditorGUI.DisabledScope(true))
                            {
                                DrawIndividualVariable(GUIContent.none, key.GetType(), key, null, out _, newValue =>
                                {
                                    /*list[index] = newValue;*/
                                });
                            }

                            DrawIndividualVariable(GUIContent.none, value.GetType(), value, null, out var handled, newValue =>
                            {
                                PropertyInfo indexer = fieldType.GetProperties().First(x => x.GetIndexParameters().Length > 0);
                                indexer.SetValue(fieldValue, newValue, new[] {key});
                            });

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUI.indentLevel = oldIndent;
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else if (isArray || isGenericList)
            {
                Type elementType = TypeUtility.GetFirstElementType(fieldType);

                EditorGUILayout.BeginHorizontal();

                string expandedID = fieldType.FullName + fieldName;
                bool expanded = DrawHeader(expandedID, label, (variableAttributes & VariableAttributes.Static) != 0);

                EditorGUI.BeginDisabledGroup((variableAttributes & VariableAttributes.ReadOnly) != 0);

                IList list = null;
                int previousSize = 0;

                if (fieldValue != null)
                {
                    list = (IList) fieldValue;

                    previousSize = list.Count;
                }

                int newSize = Mathf.Max(0, EditorGUILayout.IntField(previousSize, GUILayout.Width(80)));
                if (newSize != previousSize)
                {
                    var newValue = CollectionUtility.Resize(list, isArray, fieldType, elementType, newSize);
                    changeCallback(newValue);
                }

                if (allowExtensions)
                {
                    DrawExtensions(fieldValue, expandButtonStyle);
                }

                EditorGUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUI.indentLevel++;

                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();

                            int index = i;
                            DrawIndividualVariable(new GUIContent("Element " + i), elementType, list[i], null, out var handled, newValue => { list[index] = newValue; });

                            if (allowExtensions)
                            {
                                DrawExtensions(list[i], expandButtonStyle);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup((variableAttributes & VariableAttributes.ReadOnly) != 0);

                // Not a collection
                EditorGUILayout.BeginHorizontal();

                DrawIndividualVariable(label, fieldType, fieldValue, customAttributes, out var handled, changeCallback);

                if (handled && allowExtensions)
                {
                    DrawExtensions(fieldValue, expandButtonStyle);
                }

                EditorGUILayout.EndHorizontal();

                if (!handled)
                {
                    EditorGUI.EndDisabledGroup();

                    string expandedID = fieldType.FullName + fieldName;
                    EditorGUILayout.BeginHorizontal();
                    bool expanded = DrawHeader(expandedID, label, (variableAttributes & VariableAttributes.Static) != 0);

                    if (allowExtensions)
                    {
                        DrawExtensions(fieldValue, expandButtonStyle);
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.BeginDisabledGroup((variableAttributes & VariableAttributes.ReadOnly) != 0);

                    if (expanded)
                    {
                        EditorGUI.indentLevel++;
                        if (fieldValue != null)
                        {
                            var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                            foreach (var fieldInfo in fields)
                            {
                                GUIContent subLabel = new GUIContent(fieldInfo.Name);
                                DrawIndividualVariable(subLabel, fieldInfo.FieldType, fieldInfo.GetValue(fieldValue), null, out _, newValue => { fieldInfo.SetValue(fieldValue, newValue); });
                            }
                        }
                        else
                        {
                            GUILayout.Label("Null");
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }

            EditorGUI.EndDisabledGroup();

            if ((variableAttributes & VariableAttributes.Static) != 0)
            {
                EditorGUILayout.EndVertical();
            }
        }

        private static bool DrawHeader(string expandedID, GUIContent label, bool isStatic)
        {
            bool expanded = SidekickWindow.Current.PersistentData.ExpandedFields.Contains(expandedID);
            EditorGUI.BeginChangeCheck();

            GUIStyle style = new GUIStyle(EditorStyles.foldoutHeader);

            Color oldColor = GUI.backgroundColor;
            if (isStatic)
            {
                GUI.backgroundColor = SidekickEditorGUI.StaticBackgroundTintColor;
            }

            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, label, style);
            if (EditorGUI.EndChangeCheck())
            {
                if (expanded)
                {
                    SidekickWindow.Current.PersistentData.ExpandedFields.Add(expandedID);
                }
                else
                {
                    SidekickWindow.Current.PersistentData.ExpandedFields.Remove(expandedID);
                }
            }

            if (isStatic)
            {
                style.normal.background = SidekickEditorGUI.StaticBackground;
            }

            GUI.backgroundColor = oldColor;
            return expanded;
        }

        private static void DrawExtensions(object fieldValue, GUIStyle expandButtonStyle)
        {
            bool wasGUIEnabled = GUI.enabled;
            GUI.enabled = TypeUtility.IsNotNull(fieldValue);

            if (GUILayout.Button(new GUIContent(SidekickEditorGUI.ForwardIcon, "Select This"), expandButtonStyle, GUILayout.Width(18), GUILayout.Height(18)))
            {
                SidekickWindow.Current.SetSelection(fieldValue);
            }

            Rect rect = GUILayoutUtility.GetRect(18, 18, expandButtonStyle, GUILayout.Width(18));

            if (GUI.Button(rect, new GUIContent(SidekickEditorGUI.MoreOptions, "More Options"), expandButtonStyle))
            {
                var menu = ClassUtilities.GetMenu(fieldValue, null);

                menu.DropDown(rect);
            }

            GUI.enabled = wasGUIEnabled;
        }

        private static void DrawIndividualVariable(GUIContent label, Type fieldType, object fieldValue, IEnumerable<Attribute> customAttributes, out bool handled, Action<object> changeCallback)
        {
            EditorGUI.BeginChangeCheck();
            handled = true;
            object newValue;

            RangeAttribute rangeAttribute = null;

            if (customAttributes != null)
            {
                foreach (var customAttribute in customAttributes)
                {
                    if (customAttribute is RangeAttribute attribute)
                    {
                        rangeAttribute = attribute;
                    }
                }
            }

            if (fieldType == typeof(int))
            {
                if (SidekickSettings.PreferUnityAttributes && rangeAttribute != null)
                {
                    newValue = EditorGUILayout.IntSlider(label, (int) fieldValue, (int) rangeAttribute.min, (int) rangeAttribute.max);
                }
                else
                {
                    newValue = EditorGUILayout.IntField(label, (int) fieldValue);
                }
            }
            else if (fieldType == typeof(uint))
            {
                long newLong = EditorGUILayout.LongField(label, (uint) fieldValue);
                // Replicate Unity's built in behaviour
                newValue = (uint) Mathf.Clamp(newLong, uint.MinValue, uint.MaxValue);
            }
            else if (fieldType == typeof(long))
            {
                newValue = EditorGUILayout.LongField(label, (long) fieldValue);
            }
            else if (fieldType == typeof(ulong))
            {
                // Note that Unity doesn't have a built in way to handle larger values than long.MaxValue (its inspector
                // doesn't work correctly with ulong in fact), so display it as a validated text field
                string newString = EditorGUILayout.TextField(label, ((ulong) fieldValue).ToString());
                if (ulong.TryParse(newString, out ulong newULong))
                {
                    newValue = newULong;
                }
                else
                {
                    newValue = fieldValue;
                }
            }
            else if (fieldType == typeof(byte))
            {
                int newInt = EditorGUILayout.IntField(label, (byte) fieldValue);
                // Replicate Unity's built in behaviour
                newValue = (byte) Mathf.Clamp(newInt, byte.MinValue, byte.MaxValue);
            }
            else if (fieldType == typeof(sbyte))
            {
                int newInt = EditorGUILayout.IntField(label, (sbyte) fieldValue);
                // Replicate Unity's built in behaviour
                newValue = (sbyte) Mathf.Clamp(newInt, sbyte.MinValue, sbyte.MaxValue);
            }
            else if (fieldType == typeof(ushort))
            {
                int newInt = EditorGUILayout.IntField(label, (ushort) fieldValue);
                // Replicate Unity's built in behaviour
                newValue = (ushort) Mathf.Clamp(newInt, ushort.MinValue, ushort.MaxValue);
            }
            else if (fieldType == typeof(short))
            {
                int newInt = EditorGUILayout.IntField(label, (short) fieldValue);
                // Replicate Unity's built in behaviour
                newValue = (short) Mathf.Clamp(newInt, short.MinValue, short.MaxValue);
            }
            else if (fieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(label, (string) fieldValue);
            }
            else if (fieldType == typeof(char))
            {
                string newString = EditorGUILayout.TextField(label, new string((char) fieldValue, 1));
                // Replicate Unity's built in behaviour
                if (newString.Length == 1)
                {
                    newValue = newString[0];
                }
                else
                {
                    newValue = fieldValue;
                }
            }
            else if (fieldType == typeof(float))
            {
                if (SidekickSettings.PreferUnityAttributes && rangeAttribute != null)
                {
                    newValue = EditorGUILayout.Slider(label, (float) fieldValue, rangeAttribute.min, rangeAttribute.max);
                }
                else
                {
                    newValue = EditorGUILayout.FloatField(label, (float) fieldValue);
                }
            }
            else if (fieldType == typeof(double))
            {
                newValue = EditorGUILayout.DoubleField(label, (double) fieldValue);
            }
            else if (fieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(label, (bool) fieldValue);
            }
            else if (fieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(label, (Vector2) fieldValue);
            }
            else if (fieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(label, (Vector3) fieldValue);
            }
            else if (fieldType == typeof(Vector4))
            {
                newValue = EditorGUILayout.Vector4Field(label, (Vector4) fieldValue);
            }
            else if (fieldType == typeof(Vector2Int))
            {
                newValue = EditorGUILayout.Vector2IntField(label, (Vector2Int) fieldValue);
            }
            else if (fieldType == typeof(Vector3Int))
            {
                newValue = EditorGUILayout.Vector3IntField(label, (Vector3Int) fieldValue);
            }
            else if (fieldType == typeof(Quaternion))
            {
                Quaternion quaternion = (Quaternion) fieldValue;
                Vector4 vector = new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
                vector = EditorGUILayout.Vector4Field(label, vector);
                newValue = new Quaternion(vector.x, vector.y, vector.z, vector.z);
            }
#if UNITY_MATH_EXISTS
            else if (fieldType == typeof(float2))
            {
                newValue = (float2) EditorGUILayout.Vector2Field(label, (float2) fieldValue);
            }
            else if (fieldType == typeof(float3))
            {
                newValue = (float3) EditorGUILayout.Vector3Field(label, (float3) fieldValue);
            }
            else if (fieldType == typeof(float4))
            {
                newValue = (float4) EditorGUILayout.Vector4Field(label, (float4) fieldValue);
            }
#endif
            else if (fieldType == typeof(Bounds))
            {
                newValue = EditorGUILayout.BoundsField(label, (Bounds) fieldValue);
            }
            else if (fieldType == typeof(BoundsInt))
            {
                newValue = EditorGUILayout.BoundsIntField(label, (BoundsInt) fieldValue);
            }
            else if (fieldType == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField(label, (Color) fieldValue);
            }
            else if (fieldType == typeof(Color32))
            {
                newValue = (Color32) EditorGUILayout.ColorField(label, (Color32) fieldValue);
            }
            else if (fieldType == typeof(Gradient))
            {
                newValue = EditorGUILayout.GradientField(new GUIContent(label), (Gradient) fieldValue);
            }
            else if (fieldType == typeof(AnimationCurve))
            {
                newValue = EditorGUILayout.CurveField(label, (AnimationCurve) fieldValue);
            }
            else if (fieldType.IsSubclassOf(typeof(Enum)))
            {
                newValue = EditorGUILayout.EnumPopup(label, (Enum) fieldValue);
                Type underlyingType = Enum.GetUnderlyingType(fieldValue.GetType());
                // Cast from the enum to the underlying type (e.g. byte) then to int
                object cast = Convert.ChangeType(newValue, underlyingType);
                cast = Convert.ChangeType(cast, typeof(int));
                // Allow them to edit as an int then cast back
                newValue = Convert.ChangeType(EditorGUILayout.IntField((int) cast), underlyingType);
            }
            else if (fieldType == typeof(Rect))
            {
                newValue = EditorGUILayout.RectField(label, (Rect) fieldValue);
            }
            else if (fieldType == typeof(RectInt))
            {
                newValue = EditorGUILayout.RectIntField(label, (RectInt) fieldValue);
            }
            else if (fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                newValue = EditorGUILayout.ObjectField(label, (UnityEngine.Object) fieldValue, fieldType, true);
            }
            else if (fieldType == typeof(Type))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, new GUIContent(TypeUtility.NameForType((Type) fieldValue)));
                var popupRect = GUILayoutUtility.GetLastRect();
                popupRect.width = EditorGUIUtility.currentViewWidth;

                var selectTypeButtonLabel = new GUIContent("Select");
                if (GUILayout.Button(selectTypeButtonLabel, EditorStyles.miniButton))
                {
                    TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), type =>
                    {
                        // Async apply
                        changeCallback?.Invoke(type);
                    });
                    dropdown.Show(popupRect);
                }

                EditorGUILayout.EndHorizontal();
                newValue = fieldValue;
            }
            else
            {
                handled = false;
                newValue = fieldValue;
            }

            if (EditorGUI.EndChangeCheck())
            {
                changeCallback?.Invoke(newValue);
            }
        }
    }
}