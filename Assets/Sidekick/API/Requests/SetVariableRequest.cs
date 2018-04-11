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
    /// Sets a field or property's value on the Unity object that instanceID maps to
    /// </summary>
    public class SetVariableRequest : BaseRequest
    {
        int instanceID;
        WrappedVariable wrappedVariable;

        public SetVariableRequest(int instanceID, WrappedVariable wrappedVariable)
        {
            this.instanceID = instanceID;
            this.wrappedVariable = wrappedVariable;
        }

        public SetVariableRequest(BinaryReader br)
        {
            this.instanceID = br.ReadInt32();
            this.wrappedVariable = new WrappedVariable(br);
        }

		public override void Write(BinaryWriter bw)
		{
            base.Write(bw);

            bw.Write(instanceID);
            wrappedVariable.Write(bw);
		}

		public override BaseResponse GenerateResponse()
		{
            Object targetObject = InstanceIDMap.GetObjectFromInstanceID(instanceID);

            if (targetObject != null)
            {
                FieldInfo fieldInfo = targetObject.GetType().GetField(wrappedVariable.VariableName, GetGameObjectRequest.BINDING_FLAGS);
                if (fieldInfo != null)
                {
                    if (wrappedVariable.Attributes.HasFlagByte(VariableAttributes.IsArrayOrList))
                    {
                        fieldInfo.SetValue(targetObject, ConvertArrayOrList(wrappedVariable, fieldInfo.FieldType));
                    }
                    else
                    {
                        fieldInfo.SetValue(targetObject, wrappedVariable.Value);
                    }
                }
                else
                {
                    PropertyInfo propertyInfo = targetObject.GetType().GetProperty(wrappedVariable.VariableName, GetGameObjectRequest.BINDING_FLAGS);
                    MethodInfo setMethod = propertyInfo.GetSetMethod();
                    if (wrappedVariable.Attributes.HasFlagByte(VariableAttributes.IsArrayOrList))
                    {
                        setMethod.Invoke(targetObject, new object[] { ConvertArrayOrList(wrappedVariable, propertyInfo.PropertyType) });
                    }
                    else
                    {
                        object value = wrappedVariable.Value;
                        if (wrappedVariable.DataType == DataType.UnityObjectReference)
                        {
                            value = InstanceIDMap.GetObjectFromInstanceID((int)value);
                        }
                        setMethod.Invoke(targetObject, new object[] { value });
                    }
                }
            }
            else
            {
                throw new System.NullReferenceException();
            }

            return new SetVariableResponse();
		}

		object ConvertArrayOrList(WrappedVariable wrappedVariable, Type type)
        {
            // TODO: Investigate if this array copying could be simplified
            IList sourceList = (IList)wrappedVariable.Value;
            int count = sourceList.Count;
            if (type.IsArray)
            {
                // Copying to an array
                object newArray = Activator.CreateInstance(type, new object[] { count });
                for (int i = 0; i < count; i++)
                {
                    ((Array)newArray).SetValue(sourceList[i], i);
                }
                return newArray;
            }
            else
            {
                object newList = Activator.CreateInstance(type, new object[] { 0 });
                for (int i = 0; i < count; i++)
                {
                    ((IList)newList).Add(sourceList[i]);
                }
                return newList;
            }
            
        }





    }

}