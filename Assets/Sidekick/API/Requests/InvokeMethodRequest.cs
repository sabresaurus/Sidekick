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
    /// Fires a method with supplied arguments on the Unity object that instanceID maps to
    /// </summary>
    public class InvokeMethodRequest : BaseRequest
    {
        int instanceID;
        string methodName;
        WrappedVariable[] wrappedParameters;

        public InvokeMethodRequest(int instanceID, string methodName, WrappedVariable[] wrappedParameters)
        {
            this.instanceID = instanceID;
            this.methodName = methodName;
            this.wrappedParameters = wrappedParameters;
        }

        public InvokeMethodRequest(BinaryReader br)
        {
            this.instanceID = br.ReadInt32();
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
            base.Write(bw);
		}


		public override BaseResponse GenerateResponse()
		{
            Object targetObject = InstanceIDMap.GetObjectFromInstanceID(instanceID);
            WrappedVariable returnedVariable = null;
            if (targetObject != null)
            {
                Type[] parameterTypes = new Type[wrappedParameters.Length];
                for (int i = 0; i < wrappedParameters.Length; i++)
                {
                    parameterTypes[i] = DataTypeHelper.GetSystemTypeFromWrappedDataType(wrappedParameters[i].DataType);
                }
                MethodInfo methodInfo = targetObject.GetType().GetMethod(methodName, GetGameObjectRequest.BINDING_FLAGS, null, parameterTypes, null);
                object[] parameters = new object[wrappedParameters.Length];
                for (int i = 0; i < wrappedParameters.Length; i++)
                {
                    parameters[i] = wrappedParameters[i].Value;
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