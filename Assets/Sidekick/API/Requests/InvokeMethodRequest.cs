using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    /// <summary>
    /// Fires a method with supplied arguments on the object that Guid maps to
    /// </summary>
    public class InvokeMethodRequest : BaseRequest
    {
        Guid guid;
        string methodName;
        WrappedVariable[] wrappedParameters;

        public InvokeMethodRequest(Guid guid, string methodName, WrappedVariable[] wrappedParameters)
        {
            this.guid = guid;
            this.methodName = methodName;
            this.wrappedParameters = wrappedParameters;
        }

        public InvokeMethodRequest(BinaryReader br)
        {
            this.guid = new Guid(br.ReadString());
            this.methodName = br.ReadString();
            int parameterCount = br.ReadInt32();

            this.wrappedParameters = new WrappedVariable[parameterCount];
            for (int i = 0; i < parameterCount; i++)
            {
                this.wrappedParameters[i] = new WrappedVariable(br);
            }
        }

		public override void Write(BinaryWriter bw)
		{
            bw.Write(this.guid.ToString());
            bw.Write(this.methodName);
            int parameterCount = this.wrappedParameters.Length;

            bw.Write(parameterCount);
            for (int i = 0; i < parameterCount; i++)
            {
                wrappedParameters[i].Write(bw);
            }
		}


		public override BaseResponse GenerateResponse()
		{
            object targetObject = ObjectMap.GetObjectFromGUID(guid);
            WrappedVariable returnedVariable = null;
            if (targetObject != null)
            {
                Type[] parameterTypes = new Type[wrappedParameters.Length];
                for (int i = 0; i < wrappedParameters.Length; i++)
                {
                    parameterTypes[i] = DataTypeHelper.GetSystemTypeFromWrappedDataType(wrappedParameters[i].DataType, wrappedParameters[i].MetaData, wrappedParameters[i].Attributes);
                }
                MethodInfo methodInfo = targetObject.GetType().GetMethodAll(methodName, parameterTypes);
				Debug.Assert(methodInfo != null, "Couldn't find a matching method for signature");
                object[] parameters = new object[wrappedParameters.Length];
                for (int i = 0; i < wrappedParameters.Length; i++)
                {
                    parameters[i] = wrappedParameters[i].ValueNative;
                }
                object returnedValue = methodInfo.Invoke(targetObject, parameters);
                if (methodInfo.ReturnType == typeof(IEnumerator) && targetObject is MonoBehaviour)
                {
                    // Run it as a coroutine
                    MonoBehaviour monoBehaviour = (MonoBehaviour)targetObject;
                    monoBehaviour.StartCoroutine((IEnumerator)returnedValue);
                }
                returnedVariable = new WrappedVariable("", returnedValue, methodInfo.ReturnType, false);

                //Debug.Log(returnedValue);
            }

            return new InvokeMethodResponse(methodName, returnedVariable);
		}
	}
}