using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetGameObjectResponse : BaseResponse
    {
        string gameObjectName = "";
        List<ComponentDescription> components = new List<ComponentDescription>();

        public GetGameObjectResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            gameObjectName = br.ReadString();

            int componentCount = br.ReadInt32();
            for (int i = 0; i < componentCount; i++)
            {
                components.Add(new ComponentDescription(br));
            }
        }

        public GetGameObjectResponse()
        {

        }


        public override void Write(BinaryWriter bw)
        {
            bw.Write(gameObjectName);
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
                return gameObjectName;
            }

            set
            {
                gameObjectName = value;
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
