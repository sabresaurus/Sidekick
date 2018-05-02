using UnityEngine;
using System.Collections;
using System;
using System.IO;

namespace Sabresaurus.Sidekick
{
    [System.Serializable]
    public class VariableMetaData
    {
        // Enum
        string[] enumNames;
        int[] enumValues;

        // Unity Object Reference
        Type localModeType;
        string typeFullName;
        string assemblyName;
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

        public Type LocalModeType
        {
            get
            {
                return localModeType;
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

        public VariableMetaData(BinaryReader br, DataType dataType)
        {
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

        public void Write(BinaryWriter bw, DataType dataType)
        {
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

        public static VariableMetaData Create(DataType dataType, Type type, object value, bool isArrayOrList)
        {
            if (dataType == DataType.Enum)
            {
                return CreateFromEnum(type);
            }
            else if (dataType == DataType.UnityObjectReference)
            {
                return CreateFromUnityObject(type, value as UnityEngine.Object, isArrayOrList);
            }
            else
            {
                return null;
            }
        }

        private static VariableMetaData CreateFromEnum(Type enumType)
        {
            VariableMetaData metaData = new VariableMetaData();
            metaData.enumNames = Enum.GetNames(enumType);
            metaData.enumValues = new int[metaData.enumNames.Length];
            Array enumValuesArray = Enum.GetValues(enumType);
            for (int i = 0; i < metaData.enumNames.Length; i++)
            {
                metaData.enumValues[i] = (int)enumValuesArray.GetValue(i);
            }
            return metaData;
        }

        private static VariableMetaData CreateFromUnityObject(Type elementType, UnityEngine.Object value, bool isArrayOrList)
        {
            VariableMetaData metaData = new VariableMetaData();
            metaData.typeFullName = elementType.FullName;
            metaData.assemblyName = elementType.Assembly.FullName;
            if (value != null)
            {
                if (isArrayOrList)
                    metaData.valueDisplayName = "Array/List";
                else
                    metaData.valueDisplayName = (value).name;
            }
            else
            {
                metaData.valueDisplayName = "null";
            }
            metaData.localModeType = elementType;
            return metaData;
        }
    }
}
