using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace Sabresaurus.Sidekick
{
    public enum DataType : byte
    {
        // Array
        Void, // i.e. void
        String,
        Boolean,
        Integer, // Signed 32 bit
        Long, // Signed 64 bit
        Float, // 32 bit Single
        Double, // 32 bit Single
        Vector2,
        Vector3,
        Vector4,
		Quaternion,
        Rect,
        Color,
        ObjectReference,
        LayerMask,
        Enum,
        Character,
        AnimationCurve,
        Bounds,
        Gradient,
        ExposedReference,
        FixedBufferSize,
        Vector2Int,
        Vector3Int,
        RectInt,
        BoundsInt,

        Unknown = 255
    }

    public class WrappedVariable
    {
        string variableName;
        DataType dataType;
        bool readOnly = false;
        object value;

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

        public bool ReadOnly
        {
            get
            {
                return readOnly;
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

        public WrappedVariable(string variableName, object value, Type type)
        {
            this.variableName = variableName;
            this.value = value;
            this.dataType = GetWrappedDataTypeFromSystemType(type);
        }

        public WrappedVariable(BinaryReader br)
        {
            this.variableName = br.ReadString();
            this.readOnly = (br.ReadByte() != 0);
            this.dataType = (DataType)br.ReadByte();

            object uncastValue = null;

            if (dataType == DataType.String)
            {
                uncastValue = br.ReadString();
            }
            else if (dataType == DataType.Boolean)
            {
                byte byteValue = br.ReadByte();
                uncastValue = (byteValue != 0);
            }
            else if (dataType == DataType.Integer)
            {
                uncastValue = br.ReadInt32();
            }
            else if (dataType == DataType.Long)
            {
                uncastValue = br.ReadInt64();
            }
            else if (dataType == DataType.Float)
            {
                uncastValue = br.ReadSingle();
            }
            else if (dataType == DataType.Double)
            {
                uncastValue = br.ReadDouble();
            }
            else if (dataType == DataType.Vector2)
            {
                uncastValue = new Vector2(br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Vector3)
            {
                uncastValue = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Vector4)
            {
                uncastValue = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Quaternion)
            {
                uncastValue = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Rect)
            {
                uncastValue = new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Color)
            {
                uncastValue = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else
            {
                Debug.LogWarning("Could not read " + dataType);
            }

            this.value = uncastValue;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(variableName);
            bw.Write(readOnly ? (byte)1 : (byte)0);
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
            else
            {
                Debug.LogWarning("Could not write " + dataType);
            }
        }

        public static DataType GetWrappedDataTypeFromSystemType(Type type)
        {
            //Debug.Log(type.Name);
            if (type == typeof(void))
                return DataType.Void;
            else if (type == typeof(string))
                return DataType.String;
            else if (type == typeof(bool))
                return DataType.Boolean;
            else if (type == typeof(int))
                return DataType.Integer;
            else if (type == typeof(long))
                return DataType.Long;
            else if (type == typeof(float))
                return DataType.Float;
            else if (type == typeof(double))
                return DataType.Double;
            else if (type == typeof(Vector2))
                return DataType.Vector2;
            else if (type == typeof(Vector3))
                return DataType.Vector3;
            else if (type == typeof(Vector4))
                return DataType.Vector4;
            else if (type == typeof(Quaternion))
                return DataType.Quaternion;
            else if (type == typeof(Rect))
                return DataType.Rect;
            else if (type == typeof(Color))
                return DataType.Color;
            else
            {
                return DataType.Unknown;
            }
        }
    }
}