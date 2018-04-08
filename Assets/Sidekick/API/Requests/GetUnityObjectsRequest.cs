using UnityEngine;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Requests
{
    public class GetUnityObjectsRequest : BaseRequest
    {
        public GetUnityObjectsRequest(string typeFullName, string assemblyName)
        {
            Type type = Assembly.Load(assemblyName).GetType(typeFullName);
            Object[] objects = Resources.FindObjectsOfTypeAll(type);

            uncastResponse = new GetUnityObjectsResponse(objects);
        }
    }
}