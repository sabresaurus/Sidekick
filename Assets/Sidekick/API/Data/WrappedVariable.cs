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
        // TODO: Consider moving these elsewhere, maybe into an object
        string[] enumNames;
        int[] enumValues;

        string typeFullName;
        string assemblyName;
        string valueDisplayName;

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

        public string TypeFullName
        {
            get
            {
                return typeFullName;
            }
        }

        public string AssemblyName
        {
            get
            {
                return assemblyName;
            }
        }

        public string ValueDisplayName
        {
            get
            {
                return valueDisplayName;
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
        }

        public WrappedVariable(ParameterInfo parameterInfo, object objectValue)
            : this(parameterInfo.Name, objectValue, parameterInfo.ParameterType, true)
        {
            if (parameterInfo.ParameterType.IsValueType)
            {
                this.attributes |= VariableAttributes.IsValueType;
            }
        }

        public WrappedVariable(string variableName, object value, Type type, bool generateMetadata)
        {
            this.variableName = variableName;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(type);
        	this.value = value;

            bool isArray = type.IsArray;
            bool isGenericList = TypeUtility.IsGenericList(type);

            this.attributes = VariableAttributes.None;

            Type elementType = type;

            if (isArray || isGenericList)
            {
                this.attributes |= VariableAttributes.IsArrayOrList;
                elementType = TypeUtility.GetElementType(type);
                //Debug.Log(elementType);
                this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(elementType);
                //do something
            }

            // Root data type or element type of collection is unknown
            if (this.dataType == DataType.Unknown)
            {
                // Let's just use the type of the value to help us debug
                this.value = type.Name;
            }

            if (generateMetadata)
            {
                if (dataType == DataType.Enum)
                {
                    this.enumNames = Enum.GetNames(type);
                    this.enumValues = new int[this.enumNames.Length];
                    Array enumValuesArray = Enum.GetValues(type);
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        this.enumValues[i] = (int)enumValuesArray.GetValue(i);
                    }
                }
                else if (dataType == DataType.UnityObjectReference)
                {
                    typeFullName = elementType.FullName;
                    assemblyName = elementType.Assembly.FullName;
                    if (value != null && (isArray || isGenericList || ((UnityEngine.Object)value).GetInstanceID() != 0))
                    {
                        if (isArray || isGenericList)
                            valueDisplayName = "Array/List";
                        else
                            valueDisplayName = ((UnityEngine.Object)value).name;
                    }
                    else
                    {
                        valueDisplayName = "null";
                    }
                }
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


            if (dataType == DataType.Enum)
            {
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
            else if (dataType == DataType.UnityObjectReference)
            {
                typeFullName = br.ReadString();
                assemblyName = br.ReadString();
                valueDisplayName = br.ReadString();
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

            if (dataType == DataType.Enum)
            {
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
            else if (dataType == DataType.UnityObjectReference)
            {
                bw.Write(typeFullName);
                bw.Write(assemblyName);
                bw.Write(valueDisplayName);
            }
        }
    }
}