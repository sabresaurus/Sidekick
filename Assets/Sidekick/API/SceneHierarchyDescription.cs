using System.Collections.Generic;
using System.IO;

namespace Sabresaurus.Sidekick
{
    public class SceneHierarchyDescription : APIData
    {
        string sceneName;

        List<HierarchyNode> hierarchyNodes = new List<HierarchyNode>();

        public string SceneName
        {
            get
            {
                return sceneName;
            }

            set
            {
                sceneName = value;
            }
        }

        public List<HierarchyNode> HierarchyNodes
        {
            get
            {
                return hierarchyNodes;
            }

            set
            {
                hierarchyNodes = value;
            }
        }

        public SceneHierarchyDescription()
        {

        }

        public SceneHierarchyDescription(BinaryReader br)
            : base(br)
        {
            sceneName = br.ReadString();

            int nodeCount = br.ReadInt32();
            for (int i = 0; i < nodeCount; i++)
            {
                hierarchyNodes.Add(new HierarchyNode()
                {
                    ObjectName = br.ReadString(),
                    Depth = br.ReadInt32(),
                    ActiveInHierarchy = br.ReadBoolean(),
                });
            }
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write(sceneName);

            bw.Write(hierarchyNodes.Count);
            for (int i = 0; i < hierarchyNodes.Count; i++)
            {
                bw.Write(hierarchyNodes[i].ObjectName);
                bw.Write(hierarchyNodes[i].Depth);
                bw.Write(hierarchyNodes[i].ActiveInHierarchy);
            }
        }
    }
}