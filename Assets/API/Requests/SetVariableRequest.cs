using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick.Requests
{
    public class SetVariableRequest : BaseRequest
    {
        public SetVariableRequest(int instanceID, WrappedVariable wrappedVariable)
        {
            Object targetObject = InstanceIDMap.GetObjectFromInstanceID(instanceID);

            if (targetObject != null)
            {
                FieldInfo fieldInfo = targetObject.GetType().GetField(wrappedVariable.VariableName, GetGameObjectRequest.BINDING_FLAGS);
                if(fieldInfo != null)
                {
                    fieldInfo.SetValue(targetObject, wrappedVariable.Value);
                }
                else
                {
                    PropertyInfo propertyInfo = targetObject.GetType().GetProperty(wrappedVariable.VariableName, GetGameObjectRequest.BINDING_FLAGS);
                    MethodInfo setMethod = propertyInfo.GetSetMethod();
                    setMethod.Invoke(targetObject, new object[] { wrappedVariable.Value });

                }
            }
            else
            {
                throw new System.NullReferenceException();
            }

            uncastResponse = new SetVariableResponse();
        }
    }

}