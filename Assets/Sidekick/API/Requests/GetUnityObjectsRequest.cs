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
        Guid componentGuid;

        public GetUnityObjectsRequest(WrappedVariable variable, Guid componentGuid)
        {
            this.variable = variable;
            this.componentGuid = componentGuid;
        }

        public GetUnityObjectsRequest(BinaryReader br)
        {
			this.variable = new WrappedVariable(br);
            this.componentGuid = new Guid(br.ReadString());
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            variable.Write(bw);
            bw.Write(componentGuid.ToString());
        }

        public override BaseResponse GenerateResponse()
        {
            Type type = Assembly.Load(variable.MetaData.AssemblyName).GetType(variable.MetaData.TypeFullName);
            Object[] objects = Resources.FindObjectsOfTypeAll(type);

            return new GetUnityObjectsResponse(variable, componentGuid, objects);
        }
    }
}