using UnityEngine;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    public class FindUnityObjectsRequest : BaseRequest
    {
        WrappedVariable variable;
        ObjectPickerContext context;

        public FindUnityObjectsRequest(WrappedVariable variable, ObjectPickerContext context)
        {
            this.variable = variable;
            this.context = context;
        }

        public FindUnityObjectsRequest(BinaryReader br)
        {
			this.variable = new WrappedVariable(br);
            this.context = new ObjectPickerContext(br);
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            variable.Write(bw);
            context.Write(bw);
        }

        public override BaseResponse GenerateResponse()
        {
            Type type = variable.MetaData.GetTypeFromMetaData();
            Object[] objects = Resources.FindObjectsOfTypeAll(type);

            return new FindUnityObjectsResponse(variable, context, objects);
        }
    }
}