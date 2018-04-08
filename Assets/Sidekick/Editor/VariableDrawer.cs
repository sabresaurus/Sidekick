using UnityEditor;
using UnityEngine;
using System.Collections;
using Sabresaurus.Sidekick;
using System;

public static class VariableDrawer
{
    public static object Draw(WrappedVariable variable)
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
            if (variable.Value == null)
            {
                Debug.LogError(name + " is null");
            }

            if (variable.Attributes.HasFlagByte(VariableAttributes.IsArrayOrList))
            {
                GUILayout.Label(name);
                EditorGUI.indentLevel++;
                IList list = (IList)variable.Value;

                Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(variable.DataType);

                int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", list.Count));
                if (newSize != list.Count)
                {
                    if (list == null)
                    {
                        list = (IList)Activator.CreateInstance(type);
                    }
                    CollectionUtility.Resize(ref list, type, newSize);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = DrawIndividualVariable(variable, name, type, list[i]);
                }
                newValue = list;
                EditorGUI.indentLevel--;
            }
            else
            {
                newValue = DrawIndividualVariable(variable, name, variable.Value.GetType(), variable.Value);
            }
        }
        else
        {
            if (objectValue == null)
                EditorGUILayout.TextField(name, "{null}");
            else
                EditorGUILayout.TextField(name, variable.Value.ToString());

        }
        GUI.enabled = true;

        return newValue;

    }

    public static object DrawIndividualVariable(WrappedVariable variable, string fieldName, Type fieldType, object fieldValue)
    {
        object newValue;
        if (variable.DataType == DataType.Enum)
        {
            newValue = EditorGUILayout.IntPopup(fieldName, (int)variable.Value, variable.EnumNames, variable.EnumValues);
        }
        else if (variable.DataType == DataType.UnityObjectReference)
        {
            GUILayout.Label(fieldName + " " + variable.TypeFullName + " " + variable.ValueDisplayName + " " + (int)fieldValue);
            newValue = fieldValue;
            //newValue = EditorGUILayout.IntField(fieldName, (int)fieldValue);
        }
        else if (fieldType == typeof(int)
            || (fieldType.IsSubclassOf(typeof(Enum)) && InspectorSidekick.Current.Settings.TreatEnumsAsInts))
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
            string newString= EditorGUILayout.TextField(fieldName, new string((char)fieldValue, 1));
            if(newString.Length == 1)
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
        else if (fieldType == typeof(Color))
        {
            newValue = EditorGUILayout.ColorField(fieldName, (Color)fieldValue);
        }
        else if (fieldType == typeof(Color32))
        {
            newValue = (Color32)EditorGUILayout.ColorField(fieldName, (Color32)fieldValue);
        }
        //else if (fieldType == typeof(Gradient))
        //{
        //    //newValue = EditorGUILayout.grad(fieldName, (AnimationCurve)fieldValue);
        //}
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