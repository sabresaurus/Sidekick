using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick.Requests
{
    public class InvokeMethodRequest : BaseRequest
    {
        public InvokeMethodRequest(int instanceID, string methodName, WrappedVariable[] wrappedParameters)
        {
            Object targetObject = InstanceIDMap.GetObjectFromInstanceID(instanceID);
            WrappedVariable returnedVariable = null;
            if (targetObject != null)
            {
                MethodInfo methodInfo = targetObject.GetType().GetMethod(methodName, GetGameObjectRequest.BINDING_FLAGS);
                object[] parameters = new object[wrappedParameters.Length];
                for (int i = 0; i < wrappedParameters.Length; i++)
                {
                    parameters[i] = wrappedParameters[i].Value;
                }

                object returnedValue = methodInfo.Invoke(targetObject, parameters);
                returnedVariable = new WrappedVariable("", returnedValue, methodInfo.ReturnType, VariableAttributes.None);
                //Debug.Log(returnedValue);
            }

            uncastResponse = new InvokeMethodResponse(methodName, returnedVariable);
        }
    }
}