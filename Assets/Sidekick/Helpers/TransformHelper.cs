using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Sabresaurus.Sidekick
{
	public static class TransformHelper
    {
        public static Transform GetFromPath(string path)
        {
            int firstIndex = path.IndexOf('/');
            if(firstIndex == -1)
            {
                return null;
            }
            string sceneName = path.Substring(0, firstIndex);

            string pathInScene = path.Substring(firstIndex + 1);

            int secondIndex = pathInScene.IndexOf('/');

            string rootGameObject = "";
            string remainingPath = "";
            if(secondIndex != -1)
            {
                rootGameObject = pathInScene.Substring(0, secondIndex);
                remainingPath = pathInScene.Substring(secondIndex + 1);    
            }
            else
            {
                rootGameObject = pathInScene;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if(scene.IsValid())
            {
                var rootGameObjects = scene.GetRootGameObjects();
                for (int i = 0; i < rootGameObjects.Length; i++)
                {
                    if(rootGameObjects[i].name == rootGameObject)
                    {
                        if(remainingPath.Length > 0)
                        {
                            return rootGameObjects[i].transform.Find(remainingPath);
                        }
                        else
                        {
                            return rootGameObjects[i].transform;
                        }
                    }
                }
            }

            return null;
        }
    }
}
