using UnityEngine;
using System.IO;
using System;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetUnityObjectsResponse : BaseResponse
    {
		UnityObjectDescription[] objectDescriptions;
        WrappedVariable variable;
        Guid componentGuid;

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
		
        public Guid ComponentGuid
		{
			get
			{
                return componentGuid;
			}
		}

        public GetUnityObjectsResponse(WrappedVariable variable, Guid componentGuid, Object[] sourceObjects)
        {
            this.variable = variable;
            this.componentGuid = componentGuid;
            objectDescriptions = new UnityObjectDescription[sourceObjects.Length];
            for (int i = 0; i < sourceObjects.Length; i++)
            {
                objectDescriptions[i] = new UnityObjectDescription(sourceObjects[i]);
            }
        }

        public GetUnityObjectsResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            variable = new WrappedVariable(br);
            componentGuid = new Guid(br.ReadString());
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
            bw.Write(componentGuid.ToString());
            bw.Write(objectDescriptions.Length);
            for (int i = 0; i < objectDescriptions.Length; i++)
            {
                objectDescriptions[i].Write(bw);
            }
		}
	}
}
