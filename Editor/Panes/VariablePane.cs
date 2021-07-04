using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
	public abstract class VariablePane : BasePane
	{
		static Dictionary<string, Rect> guiRects = new Dictionary<string, Rect>();

		public static object DrawVariable(Type fieldType, string fieldName, object fieldValue, string metaSuffix, bool allowExtensions, Type contextType)
		{
			GUIStyle expandButtonStyle = new GUIStyle(GUI.skin.button);
			RectOffset padding = expandButtonStyle.padding;
			padding.left = 0;
			padding.right = 1;
			expandButtonStyle.padding = padding;

			if(fieldValue == null)
			{
				fieldValue = TypeUtility.GetDefaultValue(fieldType);
			}

			fieldName = SidekickUtility.ParseDisplayString(fieldName);

			if(!string.IsNullOrEmpty(metaSuffix))
			{
				fieldName += " " + metaSuffix;
			}

			object newValue = fieldValue;

			bool isArray = fieldType.IsArray;
			bool isGenericList = TypeUtility.IsGenericList(fieldType);

			if(isArray || isGenericList)
			{
				Type elementType = TypeUtility.GetElementType(fieldType);

				string elementTypeName = TypeUtility.NameForType(elementType);
				if(isGenericList)
				{
					GUILayout.Label("List<" + elementTypeName + "> " + fieldName);
				}
				else
				{
					GUILayout.Label(elementTypeName + "[] " + fieldName);
				}

                IList list = null;
                int previousSize = 0;

                if (fieldValue != null)
                {
                    list = (IList)fieldValue;

                    previousSize = list.Count;
                }

				
				int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", previousSize));
				if(newSize != previousSize)
				{
                    if(list == null)
                    {
                        list = (IList)Activator.CreateInstance(fieldType);
                    }
					CollectionUtility.Resize(ref list, elementType, newSize);
				}

                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i] = DrawIndividualVariable("Element " + i, elementType, list[i]);
                    }
                }
			}
			else
			{
				// Not a collection
				EditorGUILayout.BeginHorizontal();

				newValue = DrawIndividualVariable(fieldName, fieldType, fieldValue);

				if(allowExtensions)
				{
                    GUI.enabled = true;
					//			GUI.SetNextControlName(
					if(GUILayout.Button("->", expandButtonStyle, GUILayout.Width(20)))
					{
						OldInspectorSidekick.Current.SetSelection(fieldValue, true);
					}
					bool expanded = GUILayout.Button("...", expandButtonStyle, GUILayout.Width(20));
					if(Event.current.type == EventType.Repaint)
					{
						string methodIdentifier = contextType.FullName + "." + fieldType.FullName;

						guiRects[methodIdentifier] = GUILayoutUtility.GetLastRect();
					}


					//			if (GUIUtility.hot
					{

						//				GUILayoutUtility
						//				gridRect = GUILayoutUtility.GetLastRect();
						//				gridRect.width = 100;
					}

					if(expanded)
					{
						string methodIdentifier = contextType.FullName + "." + fieldType.FullName;

						Rect gridRect = guiRects[methodIdentifier];
						GenericMenu menu = new GenericMenu ();
						menu.AddItem(new GUIContent("Placeholder"), false, null);
						//					if(fieldType == typeof(Texture))
						{
							menu.AddItem(new GUIContent("Export PNG"), false, ExportTexture, fieldValue);
						}
						menu.DropDown(gridRect);
					}
				}

				EditorGUILayout.EndHorizontal();
			}


			return newValue;
		}

        protected static object DrawIndividualVariable(string fieldName, Type fieldType, object fieldValue)
		{
			object newValue;
			if (fieldType == typeof(int)
                || (fieldType.IsSubclassOf(typeof(Enum)) && OldInspectorSidekick.Current.Settings.TreatEnumsAsInts))
			{
				newValue = EditorGUILayout.IntField(fieldName, (int)fieldValue);
			}
			else if (fieldType == typeof(string))
			{
				newValue = EditorGUILayout.TextField(fieldName, (string)fieldValue);
			}
			else if (fieldType == typeof(float))
			{
				newValue = EditorGUILayout.FloatField(fieldName, (float)fieldValue);
			}
			else if (fieldType == typeof(double))
			{
				newValue = EditorGUILayout.DoubleField(fieldName, (double)fieldValue);
			}
			else if (fieldType == typeof(bool))
			{
				newValue = EditorGUILayout.Toggle(fieldName, (bool)fieldValue);
			}
			else if (fieldType == typeof(Vector2))
			{
				newValue = EditorGUILayout.Vector2Field(fieldName, (Vector2)fieldValue);
			}
			else if (fieldType == typeof(Vector3))
			{
				newValue = EditorGUILayout.Vector3Field(fieldName, (Vector3)fieldValue);
			}
			else if (fieldType == typeof(Vector4))
			{
				newValue = EditorGUILayout.Vector4Field(fieldName, (Vector4)fieldValue);
			}
			else if (fieldType == typeof(Vector2Int))
			{
				newValue = EditorGUILayout.Vector2IntField(fieldName, (Vector2Int)fieldValue);
			}
			else if (fieldType == typeof(Vector3Int))
			{
				newValue = EditorGUILayout.Vector3IntField(fieldName, (Vector3Int)fieldValue);
			}
			else if (fieldType == typeof(Quaternion))
			{
				//if(InspectorSidekick.Current.Settings.RotationsAsEuler)
				//{
				//	Quaternion quaternion = (Quaternion)fieldValue;
				//	Vector3 eulerAngles = quaternion.eulerAngles;
				//	eulerAngles = EditorGUILayout.Vector3Field(fieldName, eulerAngles);
				//	newValue = Quaternion.Euler(eulerAngles);
				//}
				//else
				{
					Quaternion quaternion = (Quaternion)fieldValue;
					Vector4 vector = new Vector4(quaternion.x,quaternion.y,quaternion.z,quaternion.w);
					vector = EditorGUILayout.Vector4Field(fieldName, vector);
					newValue = new Quaternion(vector.x,vector.y,vector.z,vector.z);
				}
			}
			else if (fieldType == typeof(Bounds))
			{
				newValue = EditorGUILayout.BoundsField(fieldName, (Bounds)fieldValue);
			}
			else if (fieldType == typeof(BoundsInt))
			{
				newValue = EditorGUILayout.BoundsIntField(fieldName, (BoundsInt)fieldValue);
			}
			else if (fieldType == typeof(Color))
			{
				newValue = EditorGUILayout.ColorField(fieldName, (Color)fieldValue);
			}
            else if (fieldType == typeof(Color32))
            {
                newValue = (Color32)EditorGUILayout.ColorField(fieldName, (Color32)fieldValue);
            }
			else if (fieldType == typeof(Gradient))
			{
				newValue = EditorGUILayout.GradientField(new GUIContent(fieldName), (Gradient) fieldValue);
			}
			else if (fieldType == typeof(AnimationCurve))
			{
				newValue = EditorGUILayout.CurveField(fieldName, (AnimationCurve)fieldValue);
			}
			else if (fieldType.IsSubclassOf(typeof(Enum)))
			{
				newValue = EditorGUILayout.EnumPopup(fieldName, (Enum)fieldValue);
			}
			else if (fieldType == typeof(Rect))
			{
				newValue = EditorGUILayout.RectField(fieldName, (Rect)fieldValue);
			}
			else if (fieldType == typeof(RectInt))
			{
				newValue = EditorGUILayout.RectIntField(fieldName, (RectInt)fieldValue);
			}
			else if(fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				newValue = EditorGUILayout.ObjectField(fieldName, (UnityEngine.Object)fieldValue, fieldType, true);
			}
			else
			{
				GUILayout.Label(fieldType + " unsupported on " + fieldName);
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

		static void ExportTexture(object texture)
		{
			if(texture is Texture2D)
			{
				string path = EditorUtility.SaveFilePanel("Save Texture", "Assets", ((Texture2D)texture).name + ".png", "png");
				if(!string.IsNullOrEmpty(path))
				{
					byte[] bytes = ((Texture2D)texture).EncodeToPNG();
					System.IO.File.WriteAllBytes(path, bytes);
					AssetDatabase.Refresh();
				}
			}
		}
	}
}