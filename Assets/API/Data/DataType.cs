using System;
using System.IO;
using UnityEngine;

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
        Color32,
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

    public static class DataTypeHelper
    {
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
            else if(type == typeof(Color32))
                return DataType.Color32;
            else if (type.IsEnum)
				return DataType.Enum;
            else
            {
                return DataType.Unknown;
            }
        }

        public static object ReadFromBinary(DataType dataType, BinaryReader br)
        {
            object value = null;
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
            }
            else
            {
                Debug.LogWarning("Could not read " + dataType);
            }
            return value;
        }

        public static void WriteToBinary(DataType dataType, object value, BinaryWriter bw)
        {

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
            }
            else
            {
                Debug.LogWarning("Could not write " + dataType);
            }
        }
    }
}