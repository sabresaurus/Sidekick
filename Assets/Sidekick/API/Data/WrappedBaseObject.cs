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
        ReadOnly = 1 << 0,
        IsStatic = 1 << 1,
        IsLiteral = 1 << 2, // e.g. const
        IsArray = 1 << 3,
        IsList = 1 << 4,
        IsValueType = 1 << 5,
        Obsolete = 1 << 6,
    }

    /// <summary>
    /// Base class for WrappedVariable and WrappedParameter
    /// </summary>
    public abstract class WrappedBaseObject
    {
        protected string variableName;
        protected VariableAttributes attributes = VariableAttributes.None;
        protected DataType dataType;

        // Meta data
        protected VariableMetaData metaData;

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

#if UNITY_EDITOR
        public object DefaultValue
        {
            get
            {
                if (attributes.HasFlagByte(VariableAttributes.IsArray)
                    || attributes.HasFlagByte(VariableAttributes.IsList))
                {
                    Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(DataType, metaData, attributes);
                    return TypeUtility.GetDefaultValue(type);
                }
                else
                {
                    return DefaultElementValue;
                }
            }
        }

        public object DefaultElementValue
        {
            get
            {
                if (DataType == DataType.Enum)
                {
                    return 0;
                }
                else if (DataType == DataType.UnityObjectReference && BridgingContext.Instance.container.Settings.InspectionConnection == InspectionConnection.RemotePlayer)
                {
                    return Guid.Empty;
                }
                else
                {
                    Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(DataType, metaData);
                    return TypeUtility.GetDefaultValue(type);
                }
            }
        }
#endif

        #endregion

        public WrappedBaseObject(string variableName, Type type, VariableMetaData metaData)
            : this(variableName, type, false)
        {
            this.metaData = metaData;
        }

        public WrappedBaseObject(string variableName, Type type, bool generateMetadata)
        {
            this.variableName = variableName;
            this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(type);

            bool isArray = type.IsArray;
            bool isGenericList = TypeUtility.IsGenericList(type);

            this.attributes = VariableAttributes.None;

            Type elementType = type;

            if (isArray || isGenericList)
            {
                if (isArray)
                {
                    this.attributes |= VariableAttributes.IsArray;
                }
                else if (isGenericList)
                {
                    this.attributes |= VariableAttributes.IsList;
                }
                elementType = TypeUtility.GetElementType(type);
                //Debug.Log(elementType);
                this.dataType = DataTypeHelper.GetWrappedDataTypeFromSystemType(elementType);
            }

            if (generateMetadata)
            {
                metaData = VariableMetaData.Create(dataType, elementType, attributes);
            }
        }

        protected WrappedBaseObject(string variableName, DataType dataType, VariableAttributes attributes, VariableMetaData metaData)
        {
            this.variableName = variableName;
            this.dataType = dataType;
            this.attributes = attributes;
            this.metaData = metaData;
        }

        public WrappedBaseObject(BinaryReader br)
        {
            this.variableName = br.ReadString();
            this.attributes = (VariableAttributes)br.ReadByte();
            this.dataType = (DataType)br.ReadByte();

            bool hasMetaData = br.ReadBoolean();
            if (hasMetaData)
            {
                metaData = new VariableMetaData(br, dataType, attributes);
            }
        }

        public virtual void Write(BinaryWriter bw)
        {
            bw.Write(variableName);
            bw.Write((byte)attributes);
            bw.Write((byte)dataType);

            bw.Write(metaData != null);
            if (metaData != null)
            {
                metaData.Write(bw, dataType, attributes);
            }
        }
    }
}