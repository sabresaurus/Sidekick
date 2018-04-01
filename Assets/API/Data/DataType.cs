using System;
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
    }
}