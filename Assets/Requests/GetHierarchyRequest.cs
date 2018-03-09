using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;

namespace Sabresaurus.Sidekick.Requests
{
    public class GetHierarchyRequest
    {
        string response;

        public string Response
        {
            get { return response; }
        }

        public GetHierarchyRequest()
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                stringBuilder.AppendLine(scene.name);

                var rootObjects = scene.GetRootGameObjects();
                foreach (var item in rootObjects)
                {
                    RecurseHierarchy(stringBuilder, item.transform, 1);
                }
            }

            response = stringBuilder.ToString();
        }

        private static void RecurseHierarchy(StringBuilder stringBuilder, Transform transform, int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                stringBuilder.Append("-");
            }

            stringBuilder.AppendLine(transform.name);
            foreach (Transform child in transform)
            {
                RecurseHierarchy(stringBuilder, child, depth + 1);
            }
        }
    }
}