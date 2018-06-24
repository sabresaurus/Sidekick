using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetObjectResponse : BaseResponse
    {
        string objectName = "";
        List<ComponentDescription> components = new List<ComponentDescription>();

        public GetObjectResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            objectName = br.ReadString();

            int componentCount = br.ReadInt32();
            for (int i = 0; i < componentCount; i++)
            {
                components.Add(new ComponentDescription(br));
            }
        }

        public GetObjectResponse()
        {

        }


        public override void Write(BinaryWriter bw)
        {
            bw.Write(objectName);
            bw.Write(components.Count);

            foreach (ComponentDescription item in components)
            {
                item.Write(bw);
            }
        }

        public string GameObjectName
        {
            get
            {
                return objectName;
            }

            set
            {
                objectName = value;
            }
        }

        public List<ComponentDescription> Components
        {
            get
            {
                return components;
            }

            set
            {
                components = value;
            }
        }
    }
}
