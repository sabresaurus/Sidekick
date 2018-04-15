using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    public class WrappedMethod
    {
        string methodName;
        DataType returnType;
        VariableAttributes returnTypeAttributes = VariableAttributes.None;
        List<WrappedParameter> parameters = new List<WrappedParameter>();

        public string MethodName
        {
            get
            {
                return methodName;
            }
        }

        public DataType ReturnType
        {
            get
            {
                return returnType;
            }
        }

        public VariableAttributes ReturnTypeAttributes
        {
            get
            {
                return returnTypeAttributes;
            }
        }

        public int ParameterCount
        {
            get
            {
                return parameters.Count;
            }
        }

        public List<WrappedParameter> Parameters
        {
            get
            {
                return parameters;
            }
        }

        public WrappedMethod(MethodInfo methodInfo)
        {
            this.methodName = methodInfo.Name;
            this.returnType = DataTypeHelper.GetWrappedDataTypeFromSystemType(methodInfo.ReturnType);

            parameters = new List<WrappedParameter>();
            foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
            {
                parameters.Add(new WrappedParameter(parameterInfo));
            }

            if(methodInfo.ReturnType.IsValueType)
            {
                returnTypeAttributes |= VariableAttributes.IsValueType;
            }
        }

        // Deserialisation constructor
        public WrappedMethod(BinaryReader br)
        {
            this.methodName = br.ReadString();
            this.returnType = (DataType)br.ReadByte();
            this.returnTypeAttributes = (VariableAttributes)br.ReadByte();
            int parameterCount = br.ReadInt32();
            parameters.Clear();
            for (int i = 0; i < parameterCount; i++)
            {
                parameters.Add(new WrappedParameter(br));
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(methodName);
            bw.Write((byte)returnType);
            bw.Write((byte)returnTypeAttributes);
            bw.Write(parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
            {
                parameters[i].Write(bw);
            }
        }
    }
}