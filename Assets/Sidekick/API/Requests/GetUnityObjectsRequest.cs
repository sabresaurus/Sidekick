using UnityEngine;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    public class GetUnityObjectsRequest : BaseRequest
    {
        WrappedVariable variable;
        ComponentDescription componentDescription;

        public GetUnityObjectsRequest(WrappedVariable variable, ComponentDescription componentDescription)
        {
            this.variable = variable;
            this.componentDescription = componentDescription;
        }

        public GetUnityObjectsRequest(BinaryReader br)
        {
            this.variable = new WrappedVariable(br);
            this.componentDescription = new ComponentDescription(br);
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            variable.Write(bw);
            componentDescription.Write(bw);
        }

        public override BaseResponse GenerateResponse()
        {
            Type type = Assembly.Load(variable.AssemblyName).GetType(variable.TypeFullName);
            Object[] objects = Resources.FindObjectsOfTypeAll(type);

            return new GetUnityObjectsResponse(variable, componentDescription, objects);
        }
    }
}