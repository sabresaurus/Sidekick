using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    [Flags]
    public enum VariableAttributes : byte
    {
        None = 0,
        ReadOnly = 1,
        IsStatic = 2,
        IsLiteral = 4, // e.g. const
        IsArrayOrList = 8,
        IsValueType = 16,
        Obsolete = 32,
    }

    /// <summary>
    /// Wraps a field or property so that it can be sent over the network
    /// </summary>
    public class WrappedVariable
    {
        string variableName;
        VariableAttributes attributes = VariableAttributes.None;
        DataType dataType;
        object value;

        // Meta data
        VariableMetaData metaData;

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

        public VariableMetaData MetaData
        {
            get
            {
                return metaData;
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
            : this(fieldInfo.Name, objectValue, fieldInfo.FieldType, true)
        {
            this.variableName = fieldInfo.Name;

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
            : this(parameter.VariableName, parameter.DefaultValue, DataTypeHelper.GetSystemTypeFromWrappedDataType(parameter.DataType), parameter.MetaData)
        {
            this.attributes = parameter.Attributes;
            //arguments.Add(new WrappedVariable(parameter.VariableName, parameter.DefaultValue, type, parameter.MetaData));
        }

        public WrappedVariable(string variableName, object value, Type type, VariableMetaData metaData)
            : this(variableName, value, type, false)
        {
            this.metaData = metaData;
        }

        public WrappedVariable(string variableName, object value, Type type, bool generateMetadata)
        {
            this.variableName = variableName;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(type);
            this.value = value;

            bool isArrayOrList = TypeUtility.IsArrayOrList(type);

            this.attributes = VariableAttributes.None;

            Type elementType = type;

            if (isArrayOrList)
            {
                this.attributes |= VariableAttributes.IsArrayOrList;
                elementType = TypeUtility.GetElementType(type);
                //Debug.Log(elementType);
                this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(elementType);
            }

            // Root data type or element type of collection is unknown
            if (this.dataType == DataType.Unknown)
            {
                if (isArrayOrList)
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

            if (generateMetadata)
            {
                metaData = VariableMetaData.Create(dataType, elementType, value, isArrayOrList);
            }
        }

        public WrappedVariable(BinaryReader br)
        {
            this.variableName = br.ReadString();
            this.attributes = (VariableAttributes)br.ReadByte();
            this.dataType = (DataType)br.ReadByte();

            if (this.attributes.HasFlagByte(VariableAttributes.IsArrayOrList))
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

            bool hasMetaData = br.ReadBoolean();
            if (hasMetaData)
            {
                metaData = new VariableMetaData(br, dataType);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(variableName);
            bw.Write((byte)attributes);
            bw.Write((byte)dataType);

            if (this.attributes.HasFlagByte(VariableAttributes.IsArrayOrList))
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
                //else if(value is Array)
                //{
                //    Debug.Log(variableName + " is Array");
                //}
                else
                {
                    throw new NotImplementedException("Array serialisation has not been implemented for this array type: " + value.GetType());
                }
            }
            else
            {
                DataTypeHelper.WriteToBinary(dataType, value, bw);
            }

            bw.Write(metaData != null);
            if (metaData != null)
            {
                metaData.Write(bw, dataType);
            }
        }
    }
}