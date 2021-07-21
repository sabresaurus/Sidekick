using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
	public abstract class VariablePane : BasePane
	{
		public enum VariableAttributes
		{
			None,
			Static,
			Constant
		}
		public static object DrawVariable(Type fieldType, string fieldName, object fieldValue, string tooltip, VariableAttributes variableAttributes, bool allowExtensions, Type contextType)
		{
			GUIStyle expandButtonStyle = new GUIStyle(GUI.skin.button);
			RectOffset padding = expandButtonStyle.padding;
			padding.left = 0;
			padding.right = 1;
			expandButtonStyle.padding = padding;

			fieldValue ??= TypeUtility.GetDefaultValue(fieldType);

			string displayName = SidekickUtility.NicifyIdentifier(fieldName);
			if (variableAttributes == VariableAttributes.Static)
			{
				displayName += " [S]";
			}
			GUIContent label = new GUIContent(displayName, tooltip);

			object newValue = fieldValue;

			bool isArray = fieldType.IsArray;
			bool isGenericList = TypeUtility.IsGenericList(fieldType);

			if(isArray || isGenericList)
			{
				Type elementType = TypeUtility.GetElementType(fieldType);

				string elementTypeName = TypeUtility.NameForType(elementType);
				if(isGenericList)
				{
					label.tooltip += " List<" + elementTypeName + "> ";
				}
				else
				{
					label.tooltip += " []";
				}

				string expandedID = fieldType.FullName + fieldName;
				bool expanded = SidekickWindow.Current.PersistentData.ExpandedFields.Contains(expandedID);
				EditorGUI.BeginChangeCheck();
				expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, label);
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
				
				Rect sizeRect = GUILayoutUtility.GetLastRect();
				sizeRect.xMin = sizeRect.xMax - 80;

				if (expanded)
				{
					EditorGUI.indentLevel++;

					IList list = null;
					int previousSize = 0;

					if (fieldValue != null)
					{
						list = (IList) fieldValue;

						previousSize = list.Count;
					}

					int newSize = Mathf.Max(0, EditorGUI.IntField(sizeRect, previousSize));
					if (newSize != previousSize)
					{
						list ??= (IList) Activator.CreateInstance(fieldType);
						CollectionUtility.Resize(ref list, elementType, newSize);
					}

					if (list != null)
					{
						for (int i = 0; i < list.Count; i++)
						{
							EditorGUILayout.BeginHorizontal();

							list[i] = DrawIndividualVariable(new GUIContent("Element " + i), elementType, list[i]);

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
				// Not a collection
				EditorGUILayout.BeginHorizontal();

				newValue = DrawIndividualVariable(label, fieldType, fieldValue);

				if(allowExtensions)
				{
					DrawExtensions(fieldValue, expandButtonStyle);
				}

				EditorGUILayout.EndHorizontal();
			}


			return newValue;
		}

		private static void DrawExtensions(object fieldValue, GUIStyle expandButtonStyle)
		{
			GUI.enabled = true;
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
		}

		private static object DrawIndividualVariable(GUIContent label, Type fieldType, object fieldValue)
		{
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
				GUILayout.Label(new GUIContent($"{label.text} (unsupported)", label.tooltip));
				newValue = fieldValue;
			}

			//			EditorGUILayout.BoundsField()
			//			EditorGUILayout.ColorField
			//			EditorGUILayout.CurveField
			//			EditorGUILayout.EnumPopup
			//			EditorGUILayout.EnumMaskField
			//			EditorGUILayout.IntSlider // If there's a range attribute maybe?
			//			EditorGUILayout.LabelField // what's this?
			//			EditorGUILayout.ObjectField
			//			EditorGUILayout.RectField
			//			EditorGUILayout.TextArea
			//			EditorGUILayout.TextField

			// What's this? public static void HelpBox (string message, MessageType type, bool wide)
			// What's this? 		public static bool InspectorTitlebar (bool foldout, Object targetObj)

			return newValue;
		}
	}
}