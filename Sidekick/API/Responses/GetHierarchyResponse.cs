using System.Collections.Generic;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class GetHierarchyResponse : BaseResponse
    {
        List<SceneHierarchyDescription> scenes = new List<SceneHierarchyDescription>();
        public GetHierarchyResponse()
        {

        }
        public GetHierarchyResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            int sceneCount = br.ReadInt32();
            for (int i = 0; i < sceneCount; i++)
            {
                scenes.Add(new SceneHierarchyDescription(br));
            }
        }

        public List<SceneHierarchyDescription> Scenes
        {
            get
            {
                return scenes;
            }

            set
            {
                scenes = value;
            }
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write(scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                scenes[i].Write(bw);
            }
        }
    }
}
