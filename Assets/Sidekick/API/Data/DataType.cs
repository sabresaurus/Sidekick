using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Enum matching types that Sidekick can understand
    /// </summary>
    public enum DataType : byte
    {
        // Array
        Void, // Method return type
        String,
        Char,
        Boolean,
        Integer, // Signed 32 bit
        Long, // Signed 64 bit
        Float, // 32 bit Single
        Double, // 32 bit Single
        Vector2,
        Vector3,
        Vector4,
        Vector2Int,
        Vector3Int,
        Bounds,
        BoundsInt,
        Quaternion,
        Rect,
        RectInt,
        Color,
        Color32,
        UnityObjectReference,
        LayerMask,
        Enum,
        AnimationCurve,
        Gradient,
        ExposedReference,
        FixedBufferSize,


        Unknown = 255
    }

    public static class DataTypeHelper
    {
        static Dictionary<Type, DataType> mappings = new Dictionary<Type, DataType>()
        {
            {typeof(void), DataType.Void},
            {typeof(string), DataType.String},
            {typeof(Char), DataType.Char},
            {typeof(bool), DataType.Boolean},
            {typeof(int), DataType.Integer},
            {typeof(long), DataType.Long},
            {typeof(float), DataType.Float},
            {typeof(double), DataType.Double},
            {typeof(Vector2), DataType.Vector2},
            {typeof(Vector3), DataType.Vector3},
            {typeof(Vector4), DataType.Vector4},
#if UNITY_2017_2_OR_NEWER
		    {typeof(Vector2Int), DataType.Vector2Int},
            {typeof(Vector3Int), DataType.Vector3Int},  
#endif
            {typeof(Quaternion), DataType.Quaternion},
            {typeof(Rect), DataType.Rect},
#if UNITY_2017_2_OR_NEWER
		    {typeof(RectInt), DataType.RectInt},
#endif
            {typeof(Bounds), DataType.Bounds},
#if UNITY_2017_2_OR_NEWER
            {typeof(BoundsInt), DataType.BoundsInt},
#endif
            {typeof(Gradient), DataType.Gradient},
            {typeof(AnimationCurve), DataType.AnimationCurve},
            {typeof(Color), DataType.Color},
            {typeof(Color32), DataType.Color32},
            //else if (type.IsEnum)), DataType.Enum},
        };

        public static DataType GetWrappedDataTypeFromSystemType(Type type)
        {
            if (mappings.ContainsKey(type))
            {
                return mappings[type];
            }
            //else if (type.IsAssignableFrom(typeof(UnityEngine.Object)))
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return DataType.UnityObjectReference;
            }
            else if (type.IsEnum || type == typeof(Enum))
            {
                return DataType.Enum;
            }
            else
            {
                return DataType.Unknown;
            }
        }

        public static Type GetSystemTypeFromWrappedDataType(DataType dataType, VariableMetaData metaData)
        {
            Type elementType = null;
            if (dataType == DataType.UnityObjectReference || dataType == DataType.Enum)
            {
                elementType = metaData.GetTypeFromMetaData();
            }

            foreach (KeyValuePair<Type, DataType> mapping in mappings)
            {
                if (mapping.Value == dataType)
                {
                    elementType = mapping.Key;
                    break;
                }
            }

            if (elementType != null)
            {
                return elementType;
            }
            else
            {
                // None matched
                return null;
            }
        }

        public static Type GetSystemTypeFromWrappedDataType(DataType dataType, VariableMetaData metaData, VariableAttributes attributes)
        {
            Type elementType = GetSystemTypeFromWrappedDataType(dataType, metaData);

            if (elementType != null)
            {
                if (attributes.HasFlagByte(VariableAttributes.IsArray))
                {
                    return elementType.MakeArrayType();
                }
                else if (attributes.HasFlagByte(VariableAttributes.IsList))
                {
                    Type listType = typeof(List<>);
                    return listType.MakeGenericType(elementType);
                }
                else
                {
                    return elementType;
                }
            }
            else
            {
                // None matched
                return null;
            }
        }

        public static object ReadFromBinary(DataType dataType, BinaryReader br)
        {
            object value = null;
            if (dataType == DataType.String)
            {
                value = br.ReadString();
            }
            else if (dataType == DataType.Char)
            {
                value = br.ReadChar();
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
#if UNITY_2017_2_OR_NEWER
            else if (dataType == DataType.Vector2Int)
            {
                value = new Vector2Int(br.ReadInt32(), br.ReadInt32());
            }
            else if (dataType == DataType.Vector3Int)
            {
                value = new Vector3Int(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
            } 
#endif
            else if (dataType == DataType.Bounds)
            {
                value = new Bounds(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()), new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
            }
#if UNITY_2017_2_OR_NEWER
            else if (dataType == DataType.BoundsInt)
            {
                value = new BoundsInt(new Vector3Int(br.ReadInt32(), br.ReadInt32(), br.ReadInt32()), new Vector3Int(br.ReadInt32(), br.ReadInt32(), br.ReadInt32()));
            } 
#endif
            else if (dataType == DataType.Quaternion)
            {
                value = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Rect)
            {
                value = new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
#if UNITY_2017_2_OR_NEWER
            else if (dataType == DataType.RectInt)
            {
                value = new RectInt(br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
            }
#endif
            else if (dataType == DataType.Color)
            {
                value = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            else if (dataType == DataType.Color32)
            {
                value = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());
            }
            else if (dataType == DataType.AnimationCurve)
            {
                int keyframeCount = br.ReadInt32();
                Keyframe[] keyframes = new Keyframe[keyframeCount];
                for (int i = 0; i < keyframeCount; i++)
                {
                    keyframes[i] = new Keyframe(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                }
                value = new AnimationCurve(keyframes);
            }
            else if (dataType == DataType.Gradient)
            {
                GradientMode gradientMode = (GradientMode)br.ReadByte();
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[br.ReadInt32()];
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i] = new GradientAlphaKey(br.ReadSingle(), br.ReadSingle());
                }
                GradientColorKey[] colorKeys = new GradientColorKey[br.ReadInt32()];

                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i] = new GradientColorKey(new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()), br.ReadSingle());
                }
                value = new Gradient() { mode = gradientMode, alphaKeys = alphaKeys, colorKeys = colorKeys };
            }
            else if (dataType == DataType.Enum)
            {
                value = br.ReadInt32();
            }
            else if (dataType == DataType.UnityObjectReference)
            {
                string guidString = br.ReadString();
                if (string.IsNullOrEmpty(guidString))
                    value = Guid.Empty;
                else
                    value = new Guid(guidString); // Read guid
            }
            else if (dataType == DataType.Unknown)
            {
                value = br.ReadString(); // Read Type name
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
            else if (dataType == DataType.Char)
            {
                bw.Write((char)value);
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
#if UNITY_2017_2_OR_NEWER
            else if (dataType == DataType.Vector2Int)
            {
                Vector2Int vector = (Vector2Int)value;
                bw.Write(vector.x);
                bw.Write(vector.y);
            }
            else if (dataType == DataType.Vector3Int)
            {
                Vector3Int vector = (Vector3Int)value;
                bw.Write(vector.x);
                bw.Write(vector.y);
                bw.Write(vector.z);
            } 
#endif
            else if (dataType == DataType.Bounds)
            {
                Bounds bounds = (Bounds)value;
                Vector3 center = bounds.center;
                bw.Write(center.x);
                bw.Write(center.y);
                bw.Write(center.z);

                Vector3 size = bounds.size;
                bw.Write(size.x);
                bw.Write(size.y);
                bw.Write(size.z);
            }
#if UNITY_2017_2_OR_NEWER
            else if (dataType == DataType.BoundsInt)
            {
                BoundsInt bounds = (BoundsInt)value;

                Vector3Int position = bounds.position;
                bw.Write(position.x);
                bw.Write(position.y);
                bw.Write(position.z);

                Vector3Int size = bounds.size;
                bw.Write(size.x);
                bw.Write(size.y);
                bw.Write(size.z);
            } 
#endif
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
#if UNITY_2017_2_OR_NEWER
            else if (dataType == DataType.RectInt)
            {
                RectInt rect = (RectInt)value;
                bw.Write(rect.x);
                bw.Write(rect.y);
                bw.Write(rect.width);
                bw.Write(rect.height);
            } 
#endif
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
            else if (dataType == DataType.AnimationCurve)
            {
                AnimationCurve curve = (AnimationCurve)value;
                bw.Write(curve.keys.Length);
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    Keyframe key = curve.keys[i];
                    bw.Write(key.time);
                    bw.Write(key.value);
                    bw.Write(key.inTangent);
                    bw.Write(key.outTangent);
                }
            }
            else if (dataType == DataType.Gradient)
            {
                Gradient gradient = (Gradient)value;
                bw.Write((byte)gradient.mode);
                bw.Write(gradient.alphaKeys.Length);
                for (int i = 0; i < gradient.alphaKeys.Length; i++)
                {
                    var key = gradient.alphaKeys[i];
                    bw.Write(key.alpha);
                    bw.Write(key.time);
                }
                bw.Write(gradient.colorKeys.Length);
                for (int i = 0; i < gradient.colorKeys.Length; i++)
                {
                    var key = gradient.colorKeys[i];
                    bw.Write(key.color.r);
                    bw.Write(key.color.g);
                    bw.Write(key.color.b);
                    bw.Write(key.color.a);
                    bw.Write(key.time);
                }
            }
            else if (dataType == DataType.Enum)
            {
                bw.Write((int)value);
            }
            else if (dataType == DataType.UnityObjectReference)
            {
                if (value is UnityEngine.Object || value == null)
                {
                    UnityEngine.Object unityObject = value as UnityEngine.Object;
                    if (unityObject != null)
                        bw.Write(ObjectMap.AddOrGetObject(unityObject).ToString());
                    else
                        bw.Write(Guid.Empty.ToString());
                }
                else
                {
                    bw.Write(((Guid)value).ToString());
                }
            }
            else if (dataType == DataType.Unknown)
            {
                bw.Write((string)value); // Write Type name
            }
            else
            {
                Debug.LogWarning("Could not write " + dataType);
            }
        }
    }
}