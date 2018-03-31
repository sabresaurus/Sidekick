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

        public WrappedMethod(string methodName, Type returnType)
        {
            this.methodName = methodName;
            this.returnType = WrappedVariable.GetWrappedDataTypeFromSystemType(returnType);
        }

        public WrappedMethod(string methodName, DataType returnType)
        {
            this.methodName = methodName;
            this.returnType = returnType;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(methodName);
            bw.Write((byte)returnType);
        }

        public static WrappedMethod Read(BinaryReader br)
        {
            string methodName = br.ReadString();
            DataType returnType = (DataType)br.ReadByte();

            WrappedMethod wrappedMethod = new WrappedMethod(methodName, returnType);

            return wrappedMethod;
        }
    }
}