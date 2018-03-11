using System;
using System.Collections.Generic;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;

namespace Sabresaurus.Sidekick.Requests
{
    public class GetGameObjectRequest : BaseRequest
    {
        public GetGameObjectRequest(string gameObjectPath)
        {
            GetGameObjectResponse getGOResponse = new GetGameObjectResponse();

            Transform foundTransform = TransformHelper.GetFromPath(gameObjectPath);
            getGOResponse.GameObjectName = foundTransform.name;

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Component[] components = foundTransform.GetComponents<Component>();
            getGOResponse.Components = new List<ComponentDescription>(components.Length);
            foreach (Component component in components)
            {
                ComponentDescription description = new ComponentDescription();
                Type componentType = component.GetType();
                description.TypeName = componentType.Name;

                FieldInfo[] fields = componentType.GetFields(bindingFlags);
                foreach (FieldInfo field in fields)
                {
                    string fieldName = field.Name;
                    object objectValue = field.GetValue(component);
                    WrappedVariable wrappedVariable = new WrappedVariable(fieldName, objectValue, field.FieldType);
                    description.Fields.Add(wrappedVariable);
                }

                getGOResponse.Components.Add(description);
            }
            base.uncastResponse = getGOResponse;
        }
    }
}
