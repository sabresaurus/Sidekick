using UnityEngine;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetUnityObjectsResponse : BaseResponse
    {
        UnityObjectDescription[] objectDescriptions;

        public UnityObjectDescription[] ObjectDescriptions
        {
            get
            {
                return objectDescriptions;
            }
        }

        public GetUnityObjectsResponse(Object[] sourceObjects)
        {
            objectDescriptions = new UnityObjectDescription[sourceObjects.Length];
            for (int i = 0; i < sourceObjects.Length; i++)
            {
                objectDescriptions[i] = new UnityObjectDescription(sourceObjects[i]);
            }
        }

        public GetUnityObjectsResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            int count = br.ReadInt32();
            objectDescriptions = new UnityObjectDescription[count];
            for (int i = 0; i < count; i++)
            {
                objectDescriptions[i] = new UnityObjectDescription(br);
            }
        }


		public override void Write(BinaryWriter bw)
		{
            bw.Write(objectDescriptions.Length);
            for (int i = 0; i < objectDescriptions.Length; i++)
            {
                objectDescriptions[i].Write(bw);
            }
		}
	}
}
