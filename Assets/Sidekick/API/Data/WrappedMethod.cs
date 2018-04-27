using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    [Flags]
    public enum MethodAttributes : byte
    {
        None = 0,
        Static = 1,
        Obsolete = 2,

    }

    public class WrappedMethod
    {
        string methodName;
        DataType returnType;
        VariableAttributes returnTypeAttributes = VariableAttributes.None;
		MethodAttributes methodAttributes = MethodAttributes.None;
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

        public MethodAttributes MethodAttributes
        {
            get
            {
                return methodAttributes;
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
            if(methodInfo.IsStatic)
            {
                this.methodAttributes |= MethodAttributes.Static;
            }
            if (AttributeHelper.IsObsolete(methodInfo.GetCustomAttributes(false)))
            {
                this.methodAttributes |= MethodAttributes.Obsolete;
            }
        }

        // Deserialisation constructor
        public WrappedMethod(BinaryReader br)
        {
            this.methodName = br.ReadString();
            this.returnType = (DataType)br.ReadByte();
            this.returnTypeAttributes = (VariableAttributes)br.ReadByte();
            this.methodAttributes = (MethodAttributes)br.ReadByte();
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
            bw.Write((byte)methodAttributes);
            bw.Write(parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
            {
                parameters[i].Write(bw);
            }
        }

		public override bool Equals(object obj)
		{
            if(obj is WrappedMethod)
            {
                WrappedMethod otherMethod = (WrappedMethod)obj;

                if(this.methodName != otherMethod.methodName
                   || this.returnType != otherMethod.returnType
                   || this.returnTypeAttributes != otherMethod.returnTypeAttributes
                   || this.methodAttributes != otherMethod.methodAttributes
                   || this.ParameterCount != otherMethod.ParameterCount
                  )
                {
                    return false;
                }

                // Check all parameters match
                for (int i = 0; i < this.ParameterCount; i++)
                {
                    if(!this.parameters[i].Equals(otherMethod.parameters[i]))
                    {
                        return false;
                    }
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