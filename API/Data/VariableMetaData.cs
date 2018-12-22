using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    [System.Serializable]
    public class VariableMetaData
    {
        // UnityObjectReference
        string typeFullName;
        string assemblyName;

        // Enum
        string enumUnderlyingType; // an enum is backed by an int by default, but can be changed
        string[] enumNames;
        object[] enumValues;

        public string EnumUnderlyingType
        {
            get
            {
                return enumUnderlyingType;
            }
        }

        public string[] EnumNames
        {
            get
            {
                return enumNames;
            }
        }

        public object[] EnumValues
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

        public VariableMetaData()
        {

        }

        public VariableMetaData(BinaryReader br, DataType dataType, VariableAttributes attributes)
        {
            if (dataType == DataType.UnityObjectReference)
            {
                typeFullName = br.ReadString();
                assemblyName = br.ReadString();
            }

            if (dataType == DataType.Enum)
            {
                enumUnderlyingType = br.ReadString();
                int enumNameCount = br.ReadInt32();
                enumNames = new string[enumNameCount];
                enumValues = new object[enumNameCount];
                for (int i = 0; i < enumNameCount; i++)
                {
                    enumNames[i] = br.ReadString();
                }
                Type enumBackingType = GetTypeFromMetaData();
                for (int i = 0; i < enumNameCount; i++)
                {
                    enumValues[i] = DataTypeHelper.ReadIntegerFromBinary(enumBackingType, br);
                }
            }
        }

        public void Write(BinaryWriter bw, DataType dataType, VariableAttributes attributes)
        {
            if (dataType == DataType.UnityObjectReference)
            {
                bw.Write(typeFullName);
                bw.Write(assemblyName);
            }

            if (dataType == DataType.Enum)
            {
                bw.Write(enumUnderlyingType);
                bw.Write(enumNames.Length);
                for (int i = 0; i < enumNames.Length; i++)
                {
                    bw.Write(enumNames[i]);
                }
                for (int i = 0; i < enumNames.Length; i++)
                {
                    DataTypeHelper.WriteIntegerToBinary(enumValues[i], bw);
                }
            }
        }

        public Type GetTypeFromMetaData()
        {
            if(!string.IsNullOrEmpty(AssemblyName) && !string.IsNullOrEmpty(TypeFullName))
            {
                return Assembly.Load(AssemblyName).GetType(TypeFullName);
            }
            else if(!string.IsNullOrEmpty(EnumUnderlyingType))
            {
                return typeof(int).Assembly.GetType(EnumUnderlyingType);
            }
            else
            {
                return null;
            }
        }

        public static VariableMetaData Create(DataType dataType, Type elementType, VariableAttributes attributes)
        {
            if (dataType == DataType.Enum || dataType == DataType.UnityObjectReference)
            {
                VariableMetaData metaData = new VariableMetaData();

                if (dataType == DataType.UnityObjectReference)
                {
                    metaData.typeFullName = elementType.FullName;
                    metaData.assemblyName = elementType.Assembly.FullName;
                }
                else if (dataType == DataType.Enum)
                {
                    Type underlyingType = Enum.GetUnderlyingType(elementType);

                    metaData.enumUnderlyingType = underlyingType.FullName;
                    metaData.enumNames = Enum.GetNames(elementType);
                    metaData.enumValues = new object[metaData.enumNames.Length];
                    Array enumValuesArray = Enum.GetValues(elementType);

                    for (int i = 0; i < metaData.enumNames.Length; i++)
                    {
                        metaData.enumValues[i] = Convert.ChangeType(enumValuesArray.GetValue(i), underlyingType);
                    }
                }

				return metaData;
            }
            else
            {
                return null;
            }
        }
    }
}
