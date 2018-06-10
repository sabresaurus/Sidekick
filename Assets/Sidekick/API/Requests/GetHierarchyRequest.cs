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

            List<Scene> scenes = TransformHelper.GetAllScenes();
            foreach (Scene scene in scenes)
            {
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

		private static void RecurseHierarchy(List<HierarchyNode> nodes, Transform transform, int depth)
        {
            ObjectMap.AddOrGetObject(transform);
            ObjectMap.AddOrGetObject(transform.gameObject);

            nodes.Add(new HierarchyNode()
            {
                ObjectName = transform.name,
                Depth = depth,
                ActiveInHierarchy = transform.gameObject.activeInHierarchy
            });

            foreach (Transform childTransform in transform)
            {
                RecurseHierarchy(nodes, childTransform, depth + 1);
            }
        }
    }
}