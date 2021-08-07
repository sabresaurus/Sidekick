using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
		
		public static object DrawVariable(Type fieldType, string fieldName, object fieldValue, string tooltip, VariableAttributes variableAttributes, bool allowExtensions, Type contextType)
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

			object newValue = fieldValue;

			bool isArray = fieldType.IsArray;
			bool isGenericList = TypeUtility.IsGenericList(fieldType);

			if(isArray || isGenericList)
			{
				Type elementType = TypeUtility.GetElementType(fieldType);
				
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
					newValue = CollectionUtility.Resize(list, isArray, fieldType, elementType, newSize);;
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

							list[i] = DrawIndividualVariable(new GUIContent("Element " + i), elementType, list[i], out var handled);

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

				newValue = DrawIndividualVariable(label, fieldType, fieldValue, out var handled);

				if(handled && allowExtensions)
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
					
					if(allowExtensions)
					{
						DrawExtensions(fieldValue, expandButtonStyle);
					}
					EditorGUILayout.EndHorizontal();
				
					EditorGUI.BeginDisabledGroup((variableAttributes & VariableAttributes.ReadOnly) != 0);
					
					if(expanded)
					{
						EditorGUI.indentLevel++;
						var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

						foreach (var fieldInfo in fields)
						{
							GUIContent subLabel = new GUIContent(fieldInfo.Name);
							EditorGUI.BeginChangeCheck();
							object newSubValue = DrawIndividualVariable(subLabel, fieldInfo.FieldType, fieldInfo.GetValue(fieldValue), out _);
							if (EditorGUI.EndChangeCheck())
							{
								fieldInfo.SetValue(fieldValue, newSubValue);
							}
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
			
			return newValue;
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
			if (fieldValue == null || (fieldValue is Object unityObject && unityObject == null))
			{
				GUI.enabled = false;
			}
			else
			{
				GUI.enabled = true;
			}

			if (GUILayout.Button(new GUIContent(SidekickEditorGUI.ForwardIcon, "Select This"), expandButtonStyle, GUILayout.Width(18), GUILayout.Height(18)))
			{
				SidekickWindow.Current.SetSelection(fieldValue);
			}

			Rect rect = GUILayoutUtility.GetRect(18, 18, expandButtonStyle, GUILayout.Width(18));

			if(GUI.Button(rect, new GUIContent(SidekickEditorGUI.MoreOptions, "More Options"), expandButtonStyle))
			{
				var menu = ClassUtilities.GetMenu(fieldValue);
				
				menu.DropDown(rect);
			}

			GUI.enabled = wasGUIEnabled;
		}

		private static object DrawIndividualVariable(GUIContent label, Type fieldType, object fieldValue, out bool handled)
		{
			handled = true;
			object newValue;
			if (fieldType == typeof(int)
                || (fieldType.IsSubclassOf(typeof(Enum)) && SidekickSettings.TreatEnumsAsInts))
			{
				newValue = EditorGUILayout.IntField(label, (int)fieldValue);
			}
			else if (fieldType == typeof(uint))
			{
				long newLong = EditorGUILayout.LongField(label, (uint)fieldValue);
				// Replicate Unity's built in behaviour
				newValue = (uint)Mathf.Clamp(newLong, uint.MinValue, uint.MaxValue);
			}
			else if (fieldType == typeof(long))
			{
				newValue = EditorGUILayout.LongField(label, (long)fieldValue);
			}
			else if (fieldType == typeof(ulong))
			{
				// Note that Unity doesn't have a built in way to handle larger values than long.MaxValue (its inspector
				// doesn't work correctly with ulong in fact), so display it as a validated text field
				string newString = EditorGUILayout.TextField(label, ((ulong)fieldValue).ToString());
				if(ulong.TryParse(newString, out ulong newULong))
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
				int newInt = EditorGUILayout.IntField(label, (byte)fieldValue);
				// Replicate Unity's built in behaviour
				newValue = (byte)Mathf.Clamp(newInt, byte.MinValue, byte.MaxValue);
			}
			else if (fieldType == typeof(sbyte))
			{
				int newInt = EditorGUILayout.IntField(label, (sbyte)fieldValue);
				// Replicate Unity's built in behaviour
				newValue = (sbyte)Mathf.Clamp(newInt, sbyte.MinValue, sbyte.MaxValue);
			}
			else if (fieldType == typeof(ushort))
			{
				int newInt = EditorGUILayout.IntField(label, (ushort)fieldValue);
				// Replicate Unity's built in behaviour
				newValue = (ushort)Mathf.Clamp(newInt, ushort.MinValue, ushort.MaxValue);
			}
			else if (fieldType == typeof(short))
			{
				int newInt = EditorGUILayout.IntField(label, (short)fieldValue);
				// Replicate Unity's built in behaviour
				newValue = (short)Mathf.Clamp(newInt, short.MinValue, short.MaxValue);
			}
			else if (fieldType == typeof(string))
			{
				newValue = EditorGUILayout.TextField(label, (string)fieldValue);
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
				newValue = EditorGUILayout.FloatField(label, (float)fieldValue);
			}
			else if (fieldType == typeof(double))
			{
				newValue = EditorGUILayout.DoubleField(label, (double)fieldValue);
			}
			else if (fieldType == typeof(bool))
			{
				newValue = EditorGUILayout.Toggle(label, (bool)fieldValue);
			}
			else if (fieldType == typeof(Vector2))
			{
				newValue = EditorGUILayout.Vector2Field(label, (Vector2)fieldValue);
			}
			else if (fieldType == typeof(Vector3))
			{
				newValue = EditorGUILayout.Vector3Field(label, (Vector3)fieldValue);
			}
			else if (fieldType == typeof(Vector4))
			{
				newValue = EditorGUILayout.Vector4Field(label, (Vector4)fieldValue);
			}
			else if (fieldType == typeof(Vector2Int))
			{
				newValue = EditorGUILayout.Vector2IntField(label, (Vector2Int)fieldValue);
			}
			else if (fieldType == typeof(Vector3Int))
			{
				newValue = EditorGUILayout.Vector3IntField(label, (Vector3Int)fieldValue);
			}
			else if (fieldType == typeof(Quaternion))
			{
				Quaternion quaternion = (Quaternion)fieldValue;
				Vector4 vector = new Vector4(quaternion.x,quaternion.y,quaternion.z,quaternion.w);
				vector = EditorGUILayout.Vector4Field(label, vector);
				newValue = new Quaternion(vector.x,vector.y,vector.z,vector.z);
			}
			else if (fieldType == typeof(Bounds))
			{
				newValue = EditorGUILayout.BoundsField(label, (Bounds)fieldValue);
			}
			else if (fieldType == typeof(BoundsInt))
			{
				newValue = EditorGUILayout.BoundsIntField(label, (BoundsInt)fieldValue);
			}
			else if (fieldType == typeof(Color))
			{
				newValue = EditorGUILayout.ColorField(label, (Color)fieldValue);
			}
            else if (fieldType == typeof(Color32))
            {
                newValue = (Color32)EditorGUILayout.ColorField(label, (Color32)fieldValue);
            }
			else if (fieldType == typeof(Gradient))
			{
				newValue = EditorGUILayout.GradientField(new GUIContent(label), (Gradient) fieldValue);
			}
			else if (fieldType == typeof(AnimationCurve))
			{
				newValue = EditorGUILayout.CurveField(label, (AnimationCurve)fieldValue);
			}
			else if (fieldType.IsSubclassOf(typeof(Enum)))
			{
				newValue = EditorGUILayout.EnumPopup(label, (Enum)fieldValue);
			}
			else if (fieldType == typeof(Rect))
			{
				newValue = EditorGUILayout.RectField(label, (Rect)fieldValue);
			}
			else if (fieldType == typeof(RectInt))
			{
				newValue = EditorGUILayout.RectIntField(label, (RectInt)fieldValue);
			}
			else if(fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				newValue = EditorGUILayout.ObjectField(label, (UnityEngine.Object)fieldValue, fieldType, true);
			}
			else
			{
				handled = false;
				newValue = fieldValue;
			}
			
			return newValue;
		}
	}
}