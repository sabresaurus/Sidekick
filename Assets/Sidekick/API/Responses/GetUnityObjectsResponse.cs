using UnityEngine;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetUnityObjectsResponse : BaseResponse
    {
		UnityObjectDescription[] objectDescriptions;
        WrappedVariable variable;
        int componentInstanceID;


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
		
        public int ComponentInstanceID
		{
			get
			{
                return componentInstanceID;
			}
		}

        public GetUnityObjectsResponse(WrappedVariable variable, int componentInstanceID, Object[] sourceObjects)
        {
            this.variable = variable;
            this.componentInstanceID = componentInstanceID;
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
            componentInstanceID = br.ReadInt32();
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
            bw.Write(componentInstanceID);
            bw.Write(objectDescriptions.Length);
            for (int i = 0; i < objectDescriptions.Length; i++)
            {
                objectDescriptions[i].Write(bw);
            }
		}
	}
}
