using UnityEngine;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetUnityObjectsResponse : BaseResponse
    {
		UnityObjectDescription[] objectDescriptions;
        WrappedVariable variable;
        ComponentDescription componentDescription;


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
		
		public ComponentDescription ComponentDescription
		{
			get
			{
				return componentDescription;
			}
		}

        public GetUnityObjectsResponse(WrappedVariable variable, ComponentDescription componentDescription, Object[] sourceObjects)
        {
            this.variable = variable;
            this.componentDescription = componentDescription;
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
            componentDescription = new ComponentDescription(br);
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
            componentDescription.Write(bw);
            bw.Write(objectDescriptions.Length);
            for (int i = 0; i < objectDescriptions.Length; i++)
            {
                objectDescriptions[i].Write(bw);
            }
		}
	}
}
