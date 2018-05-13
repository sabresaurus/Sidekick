using UnityEditor;
using UnityEngine;
using System.Collections;
using Sabresaurus.Sidekick;
using System;

public static class VariableDrawer
{
    public static object Draw(ComponentDescription componentDescription, WrappedVariable variable, Action<Guid, WrappedVariable> onObjectPicker)
    {
        string name = variable.VariableName;

        if ((variable.Attributes.HasFlagByte(VariableAttributes.IsLiteral)))
        {
            name += " [Const]";
        }
        if ((variable.Attributes.HasFlagByte(VariableAttributes.IsStatic)))
        {
            name += " [Static]";
        }

        GUI.enabled = (variable.Attributes.HasFlagByte(VariableAttributes.ReadOnly) == false
                              && variable.Attributes.HasFlagByte(VariableAttributes.IsLiteral) == false);

        object objectValue = variable.Value;
        object newValue = null;

        if (variable.DataType != DataType.Unknown)
        {
            if (variable.Attributes.HasFlagByte(VariableAttributes.IsArray)
                || variable.Attributes.HasFlagByte(VariableAttributes.IsList))
            {
                GUILayout.Label(name);
                EditorGUI.indentLevel++;
                IList list = null;
                int size = 0;
                if (variable.Value != null)
                {
                    list = (IList)variable.Value;
                    size = list.Count;
                }

                Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(variable.DataType, variable.MetaData);

                int newSize = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", size));
                if (newSize != size)
                {
                    if (list == null)
                    {
                        list = new ArrayList();
                        //list = (IList)Activator.CreateInstance(type);
                    }
                    CollectionUtility.Resize(ref list, type, newSize);
                }
                if(list != null)
                {
					for (int i = 0; i < list.Count; i++)
					{
						list[i] = DrawIndividualVariable(componentDescription, variable, "Element " + i, type, list[i], onObjectPicker);
					}
                }
                newValue = list;
                EditorGUI.indentLevel--;
            }
            else
            {
                Type type;
                if (variable.Value != null)
                {
                    type = variable.Value.GetType();
                }
                else
                {
                    type = variable.MetaData.GetTypeFromMetaData();
                }
                newValue = DrawIndividualVariable(componentDescription, variable, name, type, variable.Value, onObjectPicker);
            }
        }
        else
        {
            if (objectValue == null)
                EditorGUILayout.TextField(name, "{null}");
            else
                EditorGUILayout.TextField(name, "Unknown <" + variable.Value.ToString() + ">");

        }
        GUI.enabled = true;

        return newValue;

    }

    public static object DrawIndividualVariable(ComponentDescription componentDescription, WrappedVariable variable, string fieldName, Type fieldType, object fieldValue, Action<Guid, WrappedVariable> onObjectPicker)
    {
        object newValue;
        if (variable.DataType == DataType.Enum)
        {
            newValue = EditorGUILayout.IntPopup(fieldName, (int)fieldValue, variable.MetaData.EnumNames, variable.MetaData.EnumValues);
        }
        else if (variable.DataType == DataType.UnityObjectReference)
        {
            if (fieldValue is Guid)
            {

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(fieldName);
                if ((Guid)fieldValue != Guid.Empty)
                {
                    EditorGUILayout.TextField(variable.MetaData.ValueDisplayName);
                }
                else
                {
                    EditorGUILayout.TextField("None (" + variable.MetaData.TypeFullName + ")");
                }
                if (componentDescription != null)
                {
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        onObjectPicker(componentDescription.Guid, variable);
                    }
                }
                EditorGUILayout.EndHorizontal();

                newValue = fieldValue;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                newValue = EditorGUILayout.ObjectField(fieldName, (UnityEngine.Object)fieldValue, variable.MetaData.GetTypeFromMetaData(), true);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        else if (fieldType == typeof(int)
            || (fieldType.IsSubclassOf(typeof(Enum)) && OldInspectorSidekick.Current.Settings.TreatEnumsAsInts))
        {
            newValue = EditorGUILayout.IntField(fieldName, (int)fieldValue);
        }
        else if (fieldType == typeof(long))
        {
            newValue = EditorGUILayout.LongField(fieldName, (long)fieldValue);
        }
        else if (fieldType == typeof(string))
        {
            newValue = EditorGUILayout.TextField(fieldName, (string)fieldValue);
        }
        else if (fieldType == typeof(char))
        {
            string newString = EditorGUILayout.TextField(fieldName, new string((char)fieldValue, 1));
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
#if UNITY_2017_2_OR_NEWER
        else if (fieldType == typeof(Vector2Int))
        {
            newValue = EditorGUILayout.Vector2IntField(fieldName, (Vector2Int)fieldValue);
        }
        else if (fieldType == typeof(Vector3Int))
        {
            newValue = EditorGUILayout.Vector3IntField(fieldName, (Vector3Int)fieldValue);
        } 
#endif
        else if (fieldType == typeof(Quaternion))
        {
            //if(InspectorSidekick.Current.Settings.RotationsAsEuler)
            //{
            //  Quaternion quaternion = (Quaternion)fieldValue;
            //  Vector3 eulerAngles = quaternion.eulerAngles;
            //  eulerAngles = EditorGUILayout.Vector3Field(fieldName, eulerAngles);
            //  newValue = Quaternion.Euler(eulerAngles);
            //}
            //else
            {
                Quaternion quaternion = (Quaternion)fieldValue;
                Vector4 vector = new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
                vector = EditorGUILayout.Vector4Field(fieldName, vector);
                newValue = new Quaternion(vector.x, vector.y, vector.z, vector.z);
            }
        }
        else if (fieldType == typeof(Bounds))
        {
            newValue = EditorGUILayout.BoundsField(fieldName, (Bounds)fieldValue);
        }
#if UNITY_2017_2_OR_NEWER
        else if (fieldType == typeof(BoundsInt))
        {
            newValue = EditorGUILayout.BoundsIntField(fieldName, (BoundsInt)fieldValue);
        } 
#endif
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
            newValue = InternalEditorGUILayout.GradientField(new GUIContent(fieldName), (Gradient)fieldValue);
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
#if UNITY_2017_2_OR_NEWER
        else if (fieldType == typeof(RectInt))
        {
            newValue = EditorGUILayout.RectIntField(fieldName, (RectInt)fieldValue);
        } 
#endif
        else if (fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            newValue = EditorGUILayout.ObjectField(fieldName, (UnityEngine.Object)fieldValue, fieldType, true);
        }
        else
        {
            GUILayout.Label(fieldType + " " + fieldName);
            newValue = fieldValue;
        }



        //          EditorGUILayout.BoundsField()
        //          EditorGUILayout.ColorField
        //          EditorGUILayout.CurveField
        //          EditorGUILayout.EnumPopup
        //          EditorGUILayout.EnumMaskField
        //          EditorGUILayout.IntSlider // If there's a range attribute maybe?
        //          EditorGUILayout.LabelField // what's this?
        //          EditorGUILayout.ObjectField
        //          EditorGUILayout.RectField
        //          EditorGUILayout.TextArea
        //          EditorGUILayout.TextField

        // What's this? public static void HelpBox (string message, MessageType type, bool wide)
        // What's this?         public static bool InspectorTitlebar (bool foldout, Object targetObj)

        return newValue;
    }
}