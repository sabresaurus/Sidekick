using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Requests
{
    /// <summary>
    /// Fires a method with supplied arguments on the Unity object that instanceID maps to
    /// </summary>
    public class InvokeMethodRequest : BaseRequest
    {
        public InvokeMethodRequest(int instanceID, string methodName, WrappedVariable[] wrappedParameters)
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

            uncastResponse = new InvokeMethodResponse(methodName, returnedVariable);
        }
    }
}