using UnityEngine;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Requests
{
    public class GetUnityObjectsRequest : BaseRequest
    {
        public GetUnityObjectsRequest(WrappedVariable variable, ComponentDescription componentDescription)
        {
            Type type = Assembly.Load(variable.AssemblyName).GetType(variable.TypeFullName);
            Object[] objects = Resources.FindObjectsOfTypeAll(type);

            uncastResponse = new GetUnityObjectsResponse(variable, componentDescription, objects);
        }
    }
}