using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    public static class TransformHelper
    {
        static Scene dontDestroyOnLoadScene;

        public static Scene DontDestroyOnLoadScene
        {
            get
            {
                if (!dontDestroyOnLoadScene.IsValid())
                {
                    GameObject tempObject = new GameObject("[TEMP]DontDestroyOnLoadProxy");
                    Object.DontDestroyOnLoad(tempObject);
                    // Cache the scene ref
                    dontDestroyOnLoadScene = tempObject.scene;

                    Object.Destroy(tempObject);
                }
                return dontDestroyOnLoadScene;
            }
        }

        public static List<Scene> GetAllScenes()
        {
            List<Scene> scenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if(scene.IsValid())
                {
                    scenes.Add(scene);
                }
            }

            if(Application.isPlaying)
            {
                if(DontDestroyOnLoadScene.IsValid())
                {
                    scenes.Add(DontDestroyOnLoadScene);
                }
            }

            return scenes;
        }

        public static Transform GetFromPath(string path)
        {
            int firstIndex = path.IndexOf("//", System.StringComparison.InvariantCultureIgnoreCase);

            if (firstIndex == -1)
            {
#if UNITY_EDITOR
                if (path.StartsWith("Assets/", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Transform>(path);
                }
#endif
                return null;
            }
            string sceneName = path.Substring(0, firstIndex);

            string pathInScene = path.Substring(firstIndex + 2);

            int secondIndex = pathInScene.IndexOf('/');

            string rootGameObject = "";
            string remainingPath = "";
            if (secondIndex != -1)
            {
                rootGameObject = pathInScene.Substring(0, secondIndex);
                remainingPath = pathInScene.Substring(secondIndex + 1);
            }
            else
            {
                rootGameObject = pathInScene;
            }

            Scene scene;
            if (sceneName == "DontDestroyOnLoad")
            {
                scene = DontDestroyOnLoadScene;
            }
            else
            {
                scene = SceneManager.GetSceneByName(sceneName);
            }
            if (scene.IsValid())
            {
                var rootGameObjects = scene.GetRootGameObjects();
                for (int i = 0; i < rootGameObjects.Length; i++)
                {
                    if (rootGameObjects[i].name == rootGameObject)
                    {
                        if (remainingPath.Length > 0)
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
