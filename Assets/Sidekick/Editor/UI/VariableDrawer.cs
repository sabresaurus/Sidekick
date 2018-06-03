using UnityEditor;
using UnityEngine;
using System.Collections;
using Sabresaurus.Sidekick;
using System;

public static class VariableDrawer
{
    public delegate void OpenPickerCallback(ObjectPickerContext context, WrappedVariable variable);

    public static object Draw(ObjectPickerContext objectPickerContext, WrappedVariable variable, OpenPickerCallback onObjectPicker)
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

                int newSize = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", size));
                if (newSize != size)
                {
                    if (list == null)
                    {
                        list = new ArrayList();
                        //list = (IList)Activator.CreateInstance(type);
                    }
                    CollectionUtility.Resize(ref list, variable.DefaultElementValue, newSize);
                }
                if(list != null)
                {
					for (int i = 0; i < list.Count; i++)
					{
                        list[i] = DrawIndividualVariable(objectPickerContext, variable, "Element " + i, list[i], onObjectPicker, i);
					}
                }
                newValue = list;
                EditorGUI.indentLevel--;
            }
            else
            {
                newValue = DrawIndividualVariable(objectPickerContext, variable, name, variable.Value, onObjectPicker);
            }
        }
        else
        {
            EditorGUILayout.LabelField(name, "Unknown <" + variable.Value.ToString() + "> ");
        }
        GUI.enabled = true;

        return newValue;

    }

    public static object DrawIndividualVariable(ObjectPickerContext objectPickerContext, WrappedVariable variable, string fieldName, object fieldValue, OpenPickerCallback onObjectPicker, int index = 0)
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
                if ((Guid)fieldValue != Guid.Empty && variable.ValueDisplayNames.Length > index)
                {
                    EditorGUILayout.TextField(fieldName, variable.ValueDisplayNames[index]);
                }
                else
                {
                    EditorGUILayout.TextField(fieldName, "None (" + variable.MetaData.TypeFullName + ")");
                }
                if (objectPickerContext != null)
                {
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        onObjectPicker(objectPickerContext, variable);
                    }
                }
                EditorGUILayout.EndHorizontal();

                newValue = fieldValue;
            }
            else
            {
                newValue = EditorGUILayout.ObjectField(fieldName, (UnityEngine.Object)fieldValue, variable.MetaData.GetTypeFromMetaData(), true);
            }
        }
        else if (variable.DataType == DataType.Integer)
        {
            newValue = EditorGUILayout.IntField(fieldName, (int)fieldValue);
        }
        else if (variable.DataType == DataType.Long)
        {
            newValue = EditorGUILayout.LongField(fieldName, (long)fieldValue);
        }
        else if (variable.DataType == DataType.String)
        {
            newValue = EditorGUILayout.TextField(fieldName, (string)fieldValue);
        }
        else if (variable.DataType == DataType.Char)
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
        else if (variable.DataType == DataType.Float)
        {
            newValue = EditorGUILayout.FloatField(fieldName, (float)fieldValue);
        }
        else if (variable.DataType == DataType.Double)
        {
            newValue = EditorGUILayout.DoubleField(fieldName, (double)fieldValue);
        }
        else if (variable.DataType == DataType.Boolean)
        {
            newValue = EditorGUILayout.Toggle(fieldName, (bool)fieldValue);
        }
        else if (variable.DataType == DataType.Vector2)
        {
            newValue = EditorGUILayout.Vector2Field(fieldName, (Vector2)fieldValue);
        }
        else if (variable.DataType == DataType.Vector3)
        {
            newValue = EditorGUILayout.Vector3Field(fieldName, (Vector3)fieldValue);
        }
        else if (variable.DataType == DataType.Vector4)
        {
            newValue = EditorGUILayout.Vector4Field(fieldName, (Vector4)fieldValue);
        }
#if UNITY_2017_2_OR_NEWER
        else if (variable.DataType == DataType.Vector2Int)
        {
            newValue = EditorGUILayout.Vector2IntField(fieldName, (Vector2Int)fieldValue);
        }
        else if (variable.DataType == DataType.Vector3Int)
        {
            newValue = EditorGUILayout.Vector3IntField(fieldName, (Vector3Int)fieldValue);
        } 
#endif
        else if (variable.DataType == DataType.Quaternion)
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
        else if (variable.DataType == DataType.Bounds)
        {
            newValue = EditorGUILayout.BoundsField(fieldName, (Bounds)fieldValue);
        }
#if UNITY_2017_2_OR_NEWER
        else if (variable.DataType == DataType.BoundsInt)
        {
            newValue = EditorGUILayout.BoundsIntField(fieldName, (BoundsInt)fieldValue);
        } 
#endif
        else if (variable.DataType == DataType.Color)
        {
            newValue = EditorGUILayout.ColorField(fieldName, (Color)fieldValue);
        }
        else if (variable.DataType == DataType.Color32)
        {
            newValue = (Color32)EditorGUILayout.ColorField(fieldName, (Color32)fieldValue);
        }
        else if (variable.DataType == DataType.Gradient)
        {
            newValue = InternalEditorGUILayout.GradientField(new GUIContent(fieldName), (Gradient)fieldValue);
        }
        else if (variable.DataType == DataType.AnimationCurve)
        {
            newValue = EditorGUILayout.CurveField(fieldName, (AnimationCurve)fieldValue);
        }
        else if (variable.DataType == DataType.Rect)
        {
            newValue = EditorGUILayout.RectField(fieldName, (Rect)fieldValue);
        }
#if UNITY_2017_2_OR_NEWER
        else if (variable.DataType == DataType.RectInt)
        {
            newValue = EditorGUILayout.RectIntField(fieldName, (RectInt)fieldValue);
        } 
#endif
        else
        {
            GUILayout.Label(fieldName);
            newValue = fieldValue;
        }

        return newValue;
    }
}