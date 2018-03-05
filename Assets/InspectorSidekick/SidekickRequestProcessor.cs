using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
   * GetHierarchy
   * GetComponents
   * GetFields
   * SetField
   * 
   *
   */

namespace Sabresaurus.Sidekick
{
    public static class SidekickRequestProcessor
    {
        public static string Process(string request)
        {
            string[] split = request.Split(' ');

            string action = split[0];

            if (action.Equals("GetHierarchy"))
            {
                return GetHierarchy();
            }
            else if (action.Equals("GetGameObject"))
            {
                string path = split[1];

                return GetGameObject(path);
            }
            else if (action.Equals("SetColor"))
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(split[1], out color))
                {
                    Camera.main.backgroundColor = color;
                    return "Color applied";
                }
                else
                {
                    return "Unrecognised color string";
                }
            }
            else
            {
                return "None Matched";
            }
        }

        public static string GetHierarchy()
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

            return stringBuilder.ToString();
        }

        public static string GetGameObject(string path)
        {
            Transform foundTransform = TransformHelper.GetFromPath(path);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Found");
            stringBuilder.AppendLine(foundTransform.name);
                                     
            Component[] components = foundTransform.GetComponents<Component>();
            foreach (Component component in components)
            {
                stringBuilder.AppendLine(component.GetType().Name);
            }

            return stringBuilder.ToString();

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
