using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace Sabresaurus.Sidekick
{
    public class WrappedMethod
    {
        string methodName;
        DataType returnType;
        int parameterCount;

        public string MethodName
        {
            get
            {
                return methodName;
            }

            set
            {
                methodName = value;
            }
        }

        public DataType ReturnType
        {
            get
            {
                return returnType;
            }

            set
            {
                returnType = value;
            }
        }

        public int ParameterCount
        {
            get
            {
                return parameterCount;
            }

            set
            {
                parameterCount = value;
            }
        }

        public WrappedMethod(string methodName, Type returnType, int parameterCount)
        {
            this.methodName = methodName;
            this.returnType = DataTypeHelper.GetWrappedDataTypeFromSystemType(returnType);
            this.parameterCount = parameterCount;
        }

        public WrappedMethod(BinaryReader br)
        {
            this.methodName = br.ReadString();
            this.returnType = (DataType)br.ReadByte();
            this.parameterCount = br.ReadInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(methodName);
            bw.Write((byte)returnType);
            bw.Write(parameterCount);
        }
    }
}