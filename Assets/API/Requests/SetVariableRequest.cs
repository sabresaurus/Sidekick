using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;

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
                    // TODO: Investigate if this array copying could be simplified
					IList sourceList = (IList)wrappedVariable.Value;
					int count = sourceList.Count;

                    if(fieldInfo.FieldType.IsArray)
                    {
                        // Copying to an array
                        object newArray = Activator.CreateInstance(fieldInfo.FieldType, new object[] { count });
                        for (int i = 0; i < count; i++)
                        {
                            ((Array)newArray).SetValue(sourceList[i], i);
                        }
                        fieldInfo.SetValue(targetObject, newArray);
                    }
                    else
                    {
                        object newList = Activator.CreateInstance(fieldInfo.FieldType, new object[] { 0 });
                        for (int i = 0; i < count; i++)
                        {
                            ((IList)newList).Add(sourceList[i]);
                        }
                        fieldInfo.SetValue(targetObject, newList);
                    }
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