using UnityEngine;
using System.IO;
using System;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Responses
{
    public class FindUnityObjectsResponse : BaseResponse
    {
		UnityObjectDescription[] objectDescriptions;
        WrappedVariable variable;
        ObjectPickerContext context;

        public UnityObjectDescription[] ObjectDescriptions
        {
            get
            {
                return objectDescriptions;
            }
        }

		public WrappedVariable Variable
		{
			get
			{
				return variable;
			}
		}
		
        public ObjectPickerContext Context
		{
			get
			{
                return context;
			}
		}

        public FindUnityObjectsResponse(WrappedVariable variable, ObjectPickerContext context, Object[] sourceObjects)
        {
            this.variable = variable;
            this.context = context;
            objectDescriptions = new UnityObjectDescription[sourceObjects.Length];
            for (int i = 0; i < sourceObjects.Length; i++)
            {
                objectDescriptions[i] = new UnityObjectDescription(sourceObjects[i]);
            }
        }

        public FindUnityObjectsResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            variable = new WrappedVariable(br);
            context = new ObjectPickerContext(br);
            int count = br.ReadInt32();
            objectDescriptions = new UnityObjectDescription[count];
            for (int i = 0; i < count; i++)
            {
                objectDescriptions[i] = new UnityObjectDescription(br);
            }
        }


		public override void Write(BinaryWriter bw)
		{
            variable.Write(bw);
            context.Write(bw);
            bw.Write(objectDescriptions.Length);
            for (int i = 0; i < objectDescriptions.Length; i++)
            {
                objectDescriptions[i].Write(bw);
            }
		}
	}
}
