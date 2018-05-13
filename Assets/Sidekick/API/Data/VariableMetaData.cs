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

        // Unity Object Reference
        string valueDisplayName;

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
            else if (dataType == DataType.UnityObjectReference)
            {
                valueDisplayName = br.ReadString();
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
            else if (dataType == DataType.UnityObjectReference)
            {

                bw.Write(valueDisplayName);
            }
        }

        public Type GetTypeFromMetaData()
        {
            Type type = Assembly.Load(AssemblyName).GetType(TypeFullName);
            return type;
        }

        public static VariableMetaData Create(DataType dataType, Type elementType, object value, VariableAttributes attributes)
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
                else if (dataType == DataType.UnityObjectReference)
                {
                    if ((value as UnityEngine.Object) != null || (value is UnityEngine.Object == false && value != null))
                    {
                        if (attributes.HasFlagByte(VariableAttributes.IsArray))
                            metaData.valueDisplayName = "Array Element";
                        else if (attributes.HasFlagByte(VariableAttributes.IsList))
                            metaData.valueDisplayName = "List Element";
                        else
                            metaData.valueDisplayName = ((UnityEngine.Object)value).name;
                    }
                    else
                    {
                        metaData.valueDisplayName = "null";
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
