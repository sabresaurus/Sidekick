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
        string typeFullName;
        string assemblyName;

        // Enum
        string[] enumNames;
        int[] enumValues;

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

        public VariableMetaData()
        {

        }

        public VariableMetaData(BinaryReader br, DataType dataType, VariableAttributes attributes)
        {
            if (dataType == DataType.Enum || dataType == DataType.UnityObjectReference)
            {
                typeFullName = br.ReadString();
                assemblyName = br.ReadString();
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
        }

        public void Write(BinaryWriter bw, DataType dataType, VariableAttributes attributes)
        {
            if (dataType == DataType.Enum || dataType == DataType.UnityObjectReference)
            {
                bw.Write(typeFullName);
                bw.Write(assemblyName);
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
        }

        public Type GetTypeFromMetaData()
        {
            Type type = Assembly.Load(AssemblyName).GetType(TypeFullName);
            return type;
        }

        public static VariableMetaData Create(DataType dataType, Type elementType, VariableAttributes attributes)
        {
            if (dataType == DataType.Enum || dataType == DataType.UnityObjectReference)
            {
                VariableMetaData metaData = new VariableMetaData();

                metaData.typeFullName = elementType.FullName;
                metaData.assemblyName = elementType.Assembly.FullName;

                if (dataType == DataType.Enum)
                {
                    metaData.enumNames = Enum.GetNames(elementType);
                    metaData.enumValues = new int[metaData.enumNames.Length];
                    Array enumValuesArray = Enum.GetValues(elementType);
                    for (int i = 0; i < metaData.enumNames.Length; i++)
                    {
                        metaData.enumValues[i] = (int)enumValuesArray.GetValue(i);
                    }
                    return metaData;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
