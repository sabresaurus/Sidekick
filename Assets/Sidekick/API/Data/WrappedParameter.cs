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
        readonly string variableName;
        VariableAttributes attributes = VariableAttributes.None;
        DataType dataType;

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

        public object DefaultValue
        {
            get
            {
                if(DataType == DataType.Enum)
                {
                    return 0;
                }
                else
                {
					Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(DataType);
					return TypeUtility.GetDefaultValue(type);
                }
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

            bool isArrayOrList = TypeUtility.IsArrayOrList(type);

            this.attributes = VariableAttributes.None;

            Type elementType = type;

            if (isArrayOrList)
            {
                this.attributes |= VariableAttributes.IsArrayOrList;
                elementType = TypeUtility.GetElementType(type);
                //Debug.Log(elementType);
                this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(elementType);
                //do something
            }

            if (generateMetadata)
            {
                metaData = VariableMetaData.Create(dataType, elementType, null, isArrayOrList);
            }
        }

        public WrappedParameter(BinaryReader br)
        {
            this.variableName = br.ReadString();
            this.attributes = (VariableAttributes)br.ReadByte();
            this.dataType = (DataType)br.ReadByte();

            bool hasMetaData = br.ReadBoolean();
            if(hasMetaData)
            {
				metaData = new VariableMetaData(br, dataType);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(variableName);
            bw.Write((byte)attributes);
            bw.Write((byte)dataType);

            bw.Write(metaData != null);
            if(metaData != null)
            {
				metaData.Write(bw, dataType);
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

		public override int GetHashCode()
		{
            return variableName.GetHashCode();
		}
	}
}