using UnityEngine;
using UnityEngine.SceneManagement;
using Sabresaurus.Sidekick.Responses;
using System.Collections.Generic;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    /// <summary>
    /// Gets a complete hierarchy of all loaded scenes, only including named paths
    /// </summary>
    public class GetHierarchyRequest : BaseRequest
    {
        public GetHierarchyRequest()
        {
            
        }

        public GetHierarchyRequest(BinaryReader br)
        {

        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);
        }

		public override BaseResponse GenerateResponse()
		{
            GetHierarchyResponse response = new GetHierarchyResponse();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                SceneHierarchyDescription sceneHierarchyDescription = new SceneHierarchyDescription();

                sceneHierarchyDescription.SceneName = scene.name;
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    RecurseHierarchy(sceneHierarchyDescription.HierarchyNodes, rootObject.transform, 0);
                }
                response.Scenes.Add(sceneHierarchyDescription);
            }

            return response;
		}

		private static void RecurseHierarchy(List<SceneHierarchyDescription.HierarchyNode> nodes, Transform transform, int depth)
        {
            ObjectMap.AddOrGetObject(transform);
            ObjectMap.AddOrGetObject(transform.gameObject);

            nodes.Add(new SceneHierarchyDescription.HierarchyNode()
            {
                ObjectName = transform.name,
                Depth = depth
            });

            foreach (Transform childTransform in transform)
            {
                RecurseHierarchy(nodes, childTransform, depth + 1);
            }
        }
    }
}