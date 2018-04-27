using System.IO;
using System;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Wraps a method parameter so that it can be sent over the network.
    /// </summary>
    public class WrappedParameter
    {
        string variableName;
        VariableAttributes attributes = VariableAttributes.None;
        DataType dataType;

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

        #endregion

        public WrappedParameter(ParameterInfo parameterInfo)
            : this(parameterInfo.Name, parameterInfo.ParameterType, true)
        {
        }

        public WrappedParameter(string variableName, Type type, bool generateMetadata)
        {
            this.variableName = variableName;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(type);

            bool isArray = type.IsArray;
            bool isGenericList = TypeUtility.IsGenericList(type);

            this.attributes = VariableAttributes.None;

            if (isArray || isGenericList)
            {
                this.attributes |= VariableAttributes.IsArrayOrList;
                Type elementType = TypeUtility.GetElementType(type);
                //Debug.Log(elementType);
                this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(elementType);
                //do something
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
            }
        }

        public WrappedParameter(BinaryReader br)
        {
            this.variableName = br.ReadString();
            this.attributes = (VariableAttributes)br.ReadByte();
            this.dataType = (DataType)br.ReadByte();

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
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(variableName);
            bw.Write((byte)attributes);
            bw.Write((byte)dataType);

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
        }
        public override bool Equals(object obj)
        {
            if (obj is WrappedParameter)
            {
                WrappedParameter otherParameter = (WrappedParameter)obj;

                if (this.variableName != otherParameter.variableName
                    || this.attributes != otherParameter.attributes
                    || this.dataType != otherParameter.dataType
                  )
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}