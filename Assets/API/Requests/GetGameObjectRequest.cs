using System;
using System.Collections.Generic;
using System.Reflection;
using Sabresaurus.Sidekick;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Requests
{
    [Flags]
    public enum InfoFlags
    {
        None = 0,
        Fields = 1,
        Properties = 2,
        Methods = 4,
	}

    public class GetGameObjectRequest : BaseRequest
    {
        public const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public GetGameObjectRequest(string gameObjectPath, InfoFlags flags)
        {
            GetGameObjectResponse getGOResponse = new GetGameObjectResponse();

            Transform foundTransform = TransformHelper.GetFromPath(gameObjectPath);
            getGOResponse.GameObjectName = foundTransform.name;

            List<Object> components = new List<Object>(foundTransform.GetComponents<Component>());
            // Not technically a component, but include the GameObject
            components.Insert(0, foundTransform.gameObject);
            getGOResponse.Components = new List<ComponentDescription>(components.Count);
            foreach (Object component in components)
            {
                InstanceIDMap.AddObject(component);

                ComponentDescription description = new ComponentDescription();
                Type componentType = component.GetType();
                description.TypeName = componentType.FullName;
                description.InstanceID = component.GetInstanceID();

                if((flags & InfoFlags.Fields) == InfoFlags.Fields)
                {
					FieldInfo[] fieldInfos = componentType.GetFields(BINDING_FLAGS);
                    foreach (FieldInfo fieldInfo in fieldInfos)
					{
                        if (TypeUtility.IsBackingField(fieldInfo, componentType))
                        {
                            // Skip backing fields for auto-implemented properties
                            continue;
                        }

						string fieldName = fieldInfo.Name;
						
						object objectValue = fieldInfo.GetValue(component);
                        VariableAttributes variableAttributes = VariableAttributes.None;
                        if(fieldInfo.IsInitOnly)
                        {
                            variableAttributes |= VariableAttributes.ReadOnly;
                        }
                        if (fieldInfo.IsStatic)
                        {
                            variableAttributes |= VariableAttributes.IsStatic;
                        }
                        if(fieldInfo.IsLiteral)
                        {
                            variableAttributes |= VariableAttributes.IsLiteral;
                        }
                        WrappedVariable wrappedVariable = new WrappedVariable(fieldName, objectValue, fieldInfo.FieldType, variableAttributes);
						description.Fields.Add(wrappedVariable);
					}
                }

                if ((flags & InfoFlags.Properties) == InfoFlags.Properties)
                {
					PropertyInfo[] properties = componentType.GetProperties(BINDING_FLAGS);
					foreach (PropertyInfo property in properties)
					{
						if (property.DeclaringType == typeof(Component)
						    || property.DeclaringType == typeof(UnityEngine.Object))
						{
							continue;
						}

                        object[] attributes = property.GetCustomAttributes(false);
                        bool isObsoleteWithError = AttributeHelper.IsObsoleteWithError(attributes);
                        if(isObsoleteWithError)
                        {
                            continue;
                        }

						string propertyName = property.Name;
						
						MethodInfo getMethod = property.GetGetMethod(true);
						MethodInfo setMethod = property.GetSetMethod(true);
						if(getMethod != null)
						{
							object objectValue = getMethod.Invoke(component, null);
                            // TODO consider moving these to the WrappedVariable ctor or a factory class
                            VariableAttributes variableAttributes = VariableAttributes.None;
                            if(setMethod == null)
                            {
                                variableAttributes |= VariableAttributes.ReadOnly;
                            }
                            if (getMethod.IsStatic)
                            {
                                variableAttributes |= VariableAttributes.IsStatic;
                            }
                            WrappedVariable wrappedVariable = new WrappedVariable(propertyName, objectValue, property.PropertyType, variableAttributes);
							description.Properties.Add(wrappedVariable);
						}
					}
                }

                if ((flags & InfoFlags.Methods) == InfoFlags.Methods)
                {
                    MethodInfo[] methodInfos = componentType.GetMethods(BINDING_FLAGS);
                    foreach (var methodInfo in methodInfos)
                    {
                        if (TypeUtility.IsPropertyMethod(methodInfo, componentType))
                        {
                            // Skip automatically generated getter/setter methods
                            continue;
                        }

                        WrappedMethod wrappedMethod = new WrappedMethod(methodInfo.Name, methodInfo.ReturnType, methodInfo.GetParameters().Length);
                        description.Methods.Add(wrappedMethod);
                    }
                }

                getGOResponse.Components.Add(description);
            }
            base.uncastResponse = getGOResponse;
        }
    }
}
