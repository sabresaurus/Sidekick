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
        int componentInstanceID;

        public GetUnityObjectsRequest(WrappedVariable variable, int componentInstanceID)
        {
            this.variable = variable;
            this.componentInstanceID = componentInstanceID;
        }

        public GetUnityObjectsRequest(BinaryReader br)
        {
            this.variable = new WrappedVariable(br);
            this.componentInstanceID = br.ReadInt32();
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            variable.Write(bw);
            bw.Write(componentInstanceID);
        }

        public override BaseResponse GenerateResponse()
        {
            Type type = Assembly.Load(variable.AssemblyName).GetType(variable.TypeFullName);
            Object[] objects = Resources.FindObjectsOfTypeAll(type);

            return new GetUnityObjectsResponse(variable, componentInstanceID, objects);
        }
    }
}