using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    [Flags]
    public enum VariableAttributes : byte
    {
        None = 0,
        ReadOnly = 1,
        IsStatic = 2,
        IsLiteral = 4, // e.g. const
        IsArray = 8,
    }

    public class WrappedVariable
    {
        string variableName;
        VariableAttributes attributes = VariableAttributes.None;
        DataType dataType;
        object value;

        // TODO: Consider moving these elsewhere, maybe into an object
        string[] enumNames;
        int[] enumValues;

        #region Properties
        public string VariableName
        {
            get
            {
                return variableName;
            }
        }

        public DataType DataType
        {
            get
            {
                return dataType;
            }
        }

        public VariableAttributes Attributes
        {
            get
            {
                return attributes;
            }
        }

        public string[] EnumNames
        {
            get
            {
                return enumNames;
            }
        }

        public int[] EnumValues
        {
            get
            {
                return enumValues;
            }
        }

        public object Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }
        #endregion

        public WrappedVariable(FieldInfo fieldInfo, object objectValue)
        {
            this.variableName = fieldInfo.Name;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(fieldInfo.FieldType);

            if (dataType == DataType.Enum)
            {
                FetchEnumMetadata(fieldInfo.FieldType);
            }

            this.attributes = VariableAttributes.None;
            if (fieldInfo.IsInitOnly)
            {
                this.attributes |= VariableAttributes.ReadOnly;
            }
            if (fieldInfo.IsStatic)
            {
                this.attributes |= VariableAttributes.IsStatic;
            }
            if (fieldInfo.IsLiteral)
            {
                this.attributes |= VariableAttributes.IsLiteral;
            }

            this.value = objectValue;
        }

        public WrappedVariable(PropertyInfo propertyInfo, object objectValue)
        {
            this.variableName = propertyInfo.Name;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(propertyInfo.PropertyType);

            if (dataType == DataType.Enum)
            {
                FetchEnumMetadata(propertyInfo.PropertyType);
            }

            MethodInfo getMethod = propertyInfo.GetGetMethod(true);
            MethodInfo setMethod = propertyInfo.GetSetMethod(true);

            this.attributes = VariableAttributes.None;
            if (setMethod == null)
            {
                this.attributes |= VariableAttributes.ReadOnly;
            }
            if (getMethod.IsStatic)
            {
                this.attributes |= VariableAttributes.IsStatic;
            }

            this.value = objectValue;
        }

        public void FetchEnumMetadata(Type type)
        {
            this.enumNames = Enum.GetNames(type);
            this.enumValues = new int[this.enumNames.Length];
            Array enumValuesArray = Enum.GetValues(type);
            for (int i = 0; i < enumNames.Length; i++)
            {
                this.enumValues[i] = (int)enumValuesArray.GetValue(i);
            }
        }

        public WrappedVariable(string variableName, object value, Type type, VariableAttributes attributes)
        {
            this.variableName = variableName;
            this.attributes = attributes;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(type);
            this.value = value;
        }

        public WrappedVariable(BinaryReader br)
        {
            this.variableName = br.ReadString();
            this.attributes = (VariableAttributes)br.ReadByte();
            this.dataType = (DataType)br.ReadByte();

            if (dataType == DataType.String)
            {
                value = br.ReadString();
            }
            else if (dataType == DataType.Boolean)
            {
                byte byteValue = br.ReadByte();
                value = (byteValue != 0);
            }
            else if (dataType == DataType.Integer)
            {
                value = br.ReadInt32();
            }
            else if (dataType == DataType.Long)
            {
                value = br.ReadInt64();
            }
            else if (dataType == DataType.Float)
            {
                value = br.ReadSingle();
            }
            else if (dataType == DataType.Double)
            {
                value = br.ReadDouble();
            }
            else if (dataType == DataType.Vector2)
            {
                value = new Vector2(br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Vector3)
            {
                value = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Vector4)
            {
                value = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Quaternion)
            {
                value = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Rect)
            {
                value = new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Color)
            {
                value = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Color32)
            {
                value = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());
            }
            else if (dataType == DataType.Enum)
            {
                value = br.ReadInt32();
                int enumNameCount = br.ReadInt32();
                enumNames = new string[enumNameCount];
                enumValues = new int[enumNameCount];
                for (int i = 0; i < enumNameCount; i++)
                {
                    enumNames[i] = br.ReadString();
                }
                for (int i = 0; i < enumNameCount; i++)
                {
                    enumValues[i] = br.ReadInt32();
                }
            }
            else
            {
                Debug.LogWarning("Could not read " + dataType);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(variableName);
            bw.Write((byte)attributes);
            bw.Write((byte)dataType);

            if (dataType == DataType.String)
            {
                bw.Write((string)value);
            }
            else if (dataType == DataType.Boolean)
            {
                bool boolValue = (bool)value;
                bw.Write(boolValue ? (byte)1 : (byte)0);
            }
            else if (dataType == DataType.Integer)
            {
                bw.Write((int)value);
            }
            else if (dataType == DataType.Long)
            {
                bw.Write((long)value);
            }
            else if (dataType == DataType.Float)
            {
                bw.Write((float)value);
            }
            else if (dataType == DataType.Double)
            {
                bw.Write((double)value);
            }
            else if (dataType == DataType.Vector2)
            {
                Vector2 vector = (Vector2)value;
                bw.Write(vector.x);
                bw.Write(vector.y);
            }
            else if (dataType == DataType.Vector3)
            {
                Vector3 vector = (Vector3)value;
                bw.Write(vector.x);
                bw.Write(vector.y);
                bw.Write(vector.z);
            }
            else if (dataType == DataType.Vector4)
            {
                Vector4 vector = (Vector4)value;
                bw.Write(vector.x);
                bw.Write(vector.y);
                bw.Write(vector.z);
                bw.Write(vector.w);
            }
            else if (dataType == DataType.Quaternion)
            {
                Quaternion rotation = (Quaternion)value;
                bw.Write(rotation.x);
                bw.Write(rotation.y);
                bw.Write(rotation.z);
                bw.Write(rotation.w);
            }
            else if (dataType == DataType.Rect)
            {
                Rect rect = (Rect)value;
                bw.Write(rect.x);
                bw.Write(rect.y);
                bw.Write(rect.width);
                bw.Write(rect.height);
            }
            else if (dataType == DataType.Color)
            {
                Color color = (Color)value;
                bw.Write(color.r);
                bw.Write(color.g);
                bw.Write(color.b);
                bw.Write(color.a);
            }
            else if (dataType == DataType.Color32)
            {
                Color32 color = (Color32)value;
                bw.Write(color.r);
                bw.Write(color.g);
                bw.Write(color.b);
                bw.Write(color.a);
            }
            else if (dataType == DataType.Enum)
            {
                bw.Write((int)value);

                bw.Write(enumNames.Length);
                for (int i = 0; i < enumNames.Length; i++)
                {
                    bw.Write(enumNames[i]);
                }
                for (int i = 0; i < enumNames.Length; i++)
                {
                    bw.Write(enumValues[i]);
                }
            }
            else
            {
                Debug.LogWarning("Could not write " + dataType);
            }
        }
    }
}