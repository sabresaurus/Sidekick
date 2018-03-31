using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick.Requests
{
    public class InvokeMethodRequest : BaseRequest
    {
        public InvokeMethodRequest(int instanceID, string methodName)
        {
            Object targetObject = InstanceIDMap.GetObjectFromInstanceID(instanceID);

            if (targetObject != null)
            {
                MethodInfo methodInfo = targetObject.GetType().GetMethod(methodName, GetGameObjectRequest.BINDING_FLAGS);
                object returnedValue = methodInfo.Invoke(targetObject, null);
                Debug.Log(returnedValue);
            }

            uncastResponse = new InvokeMethodResponse();
        }
    }
}