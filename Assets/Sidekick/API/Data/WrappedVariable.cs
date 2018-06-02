using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Wraps a field or property so that it can be sent over the network
    /// </summary>
    public class WrappedVariable : WrappedBaseObject
    {
        object value;

        // Unity Object Reference
        string[] valueDisplayNames;

        #region Properties

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

        public object ValueNative
        {
            get
            {
                if (dataType == DataType.UnityObjectReference)
                {
                    // TODO support array of Unity Objects

                    if (value is UnityEngine.Object || value == null)
                    {
                        UnityEngine.Object unityObject = value as UnityEngine.Object;
                        return unityObject;
                    }
                    else
                    {
                        return ObjectMap.GetObjectFromGUID((Guid)value);
                    }
                }

                if (attributes.HasFlagByte(VariableAttributes.IsArray)
                        || attributes.HasFlagByte(VariableAttributes.IsList))
                {
                    return CollectionUtility.ConvertArrayOrList(this, DataTypeHelper.GetSystemTypeFromWrappedDataType(dataType, metaData, attributes));
                }
                else
                {
                    return Value;
                }
            }
        }

        public string[] ValueDisplayNames
        {
            get
            {
                return valueDisplayNames;
            }
        }
        #endregion

        public WrappedVariable(FieldInfo fieldInfo, object objectValue)
            : this(fieldInfo.Name, objectValue, fieldInfo.FieldType, true)
        {
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
            if (fieldInfo.FieldType.IsValueType)
            {
                this.attributes |= VariableAttributes.IsValueType;
            }
            if(AttributeHelper.IsObsolete(fieldInfo.GetCustomAttributes(false)))
            {
                this.attributes |= VariableAttributes.Obsolete;
            }
        }

        public WrappedVariable(PropertyInfo propertyInfo, object objectValue)
            : this(propertyInfo.Name, objectValue, propertyInfo.PropertyType, true)
        {
            MethodInfo getMethod = propertyInfo.GetGetMethod(true);
            MethodInfo setMethod = propertyInfo.GetSetMethod(true);

            if (setMethod == null)
            {
                this.attributes |= VariableAttributes.ReadOnly;
            }
            if (getMethod.IsStatic)
            {
                this.attributes |= VariableAttributes.IsStatic;
            }
            if(propertyInfo.PropertyType.IsValueType)
            {
                this.attributes |= VariableAttributes.IsValueType;
            }
            if (AttributeHelper.IsObsolete(propertyInfo.GetCustomAttributes(false)))
            {
                this.attributes |= VariableAttributes.Obsolete;
            }
        }

        public WrappedVariable(WrappedParameter parameter)
            : base(parameter.VariableName, parameter.DataType, parameter.Attributes, parameter.MetaData)
        {
            this.value = parameter.DefaultValue;
        }

        public WrappedVariable(string variableName, object value, Type type, bool generateMetadata)
            : base(variableName, type, generateMetadata)
        {
            this.value = value;

            bool isArray = type.IsArray;
            bool isGenericList = TypeUtility.IsGenericList(type);

            // Root data type or element type of collection is unknown
            if (this.dataType == DataType.Unknown)
            {
                if (isArray || isGenericList)
                {
                    IList list = (IList)value;
                    int count = list.Count;

                    List<string> unknownList = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        unknownList.Add(type.Name);
                    }

                    this.value = unknownList;
                }
                else
                {
                    // Let's just use the type of the value to help us debug
                    this.value = type.Name;
                }
            }

            if (dataType == DataType.UnityObjectReference)
            {
                if ((value as UnityEngine.Object) != null || (value is UnityEngine.Object == false && value != null))
                {
                    if (attributes.HasFlagByte(VariableAttributes.IsArray)
                        || attributes.HasFlagByte(VariableAttributes.IsList))
                    {
                        IList list = (IList)value;
                        valueDisplayNames = new string[list.Count];
                        for (int i = 0; i < list.Count; i++)
                        {
                            UnityEngine.Object castObject = ((UnityEngine.Object)list[i]);
                            if (castObject != null)
                                valueDisplayNames[i] = castObject.name;
                            else
                                valueDisplayNames[i] = "null";
                        }
                    }
                    else
                    {
                        valueDisplayNames = new[] { ((UnityEngine.Object)value).name };
                    }
                }
                else
                {
                    valueDisplayNames = new[] { "null" };
                }
            }
        }

        public WrappedVariable(BinaryReader br)
            : base(br)
        {
            if (this.attributes.HasFlagByte(VariableAttributes.IsArray))
            {
                int count = br.ReadInt32();
                object[] array = new object[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = DataTypeHelper.ReadFromBinary(dataType, br);
                }
                this.value = array;
            }
            else if (this.attributes.HasFlagByte(VariableAttributes.IsList))
            {
                int count = br.ReadInt32();

                List<object> list = new List<object>(count);
                for (int i = 0; i < count; i++)
                {
                    list.Add(DataTypeHelper.ReadFromBinary(dataType, br));
                }
                this.value = list;
            }
            else
            {
                this.value = DataTypeHelper.ReadFromBinary(dataType, br);
            }

            if (dataType == DataType.UnityObjectReference)
            {
                valueDisplayNames = new string[br.ReadInt32()];
                for (int i = 0; i < valueDisplayNames.Length; i++)
                {
                    valueDisplayNames[i] = br.ReadString();
                }
            }
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            if (this.attributes.HasFlagByte(VariableAttributes.IsArray)
                || this.attributes.HasFlagByte(VariableAttributes.IsList))
            {
                if (value is IList)
                {
                    IList list = (IList)value;
                    int count = list.Count;
                    bw.Write(count);
                    for (int i = 0; i < count; i++)
                    {
                        DataTypeHelper.WriteToBinary(dataType, list[i], bw);
                    }
                }
                else
                {
                    throw new NotImplementedException("Array serialisation has not been implemented for this array type: " + value.GetType());
                }
            }
            else
            {
                DataTypeHelper.WriteToBinary(dataType, value, bw);
            }

            if (dataType == DataType.UnityObjectReference)
            {
                bw.Write(valueDisplayNames.Length);
                for (int i = 0; i < valueDisplayNames.Length; i++)
                {
                    bw.Write(valueDisplayNames[i]);
                }
            }
        }
    }
}