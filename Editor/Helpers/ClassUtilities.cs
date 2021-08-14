using System;
using System.Linq;
#if ECS_EXISTS
using Unity.Transforms;
#endif
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
    public static class ClassUtilities
    {
        public static GenericMenu GetMenu(object o)
        {
            var menu = new GenericMenu();

            if (TypeUtility.IsNull(o))
            {
                return menu;
            }

            if (o is MonoScript monoScript)
            {
                Type classType = monoScript.GetClass();
                if (classType != null && classType.IsSubclassOf(typeof(ScriptableObject)))
                {
                    menu.AddItem(new GUIContent("Instantiate Asset"), false, InstantiateScriptableObject, classType);
                }
            }

            if (o is GameObject gameObject)
            {
                menu.AddItem(new GUIContent("Set Name From First Script"), false, SetNameFromFirstScript, gameObject);
            }

            if (o is Object unityObject)
            {
                menu.AddItem(new GUIContent("Set Name From Type"), false, () => unityObject.name = o.GetType().Name);
            }

            if (o is Texture2D)
            {
                menu.AddItem(new GUIContent("Export Texture"), false, ExportTexture, o);
            }
            
#if ECS_EXISTS
            if (o is Translation translation)
            {
                menu.AddItem(new GUIContent("Focus In Scene View"), false, () =>
                {
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                        sceneView = EditorWindow.GetWindow<SceneView>();
                    
                    sceneView.pivot = translation.Value;
                    sceneView.Show();
                });
            }
            if (o is LocalToWorld localToWorld)
            {
                menu.AddItem(new GUIContent("Focus In Scene View"), false, () =>
                {
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                        sceneView = EditorWindow.GetWindow<SceneView>();
                    
                    sceneView.pivot = localToWorld.Position;
                    sceneView.Show();
                });
            }
#endif
            MonoScript targetMonoScript = GetMonoScriptForType(o.GetType());
            if (targetMonoScript != null)
            {
                menu.AddItem(new GUIContent("Select Script"), false, _ => Selection.activeObject = targetMonoScript, targetMonoScript);
                menu.AddItem(new GUIContent("Edit Script"), false, _ => AssetDatabase.OpenAsset(targetMonoScript), targetMonoScript);
            }
            
            menu.AddItem(new GUIContent("Copy As JSON (Unity JsonUtility)"), false, CopyAsJSON, o);

            return menu;
        }

        static MonoScript GetMonoScriptForType(Type type)
        {
            var monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>();

            return monoScripts.FirstOrDefault(monoScript => monoScript.GetClass() == type);
        }

        private static void InstantiateScriptableObject(object userData)
        {
            Type classType = (Type) userData;
            ScriptableObject asset = ScriptableObject.CreateInstance(classType);

            string fullPath = AssetDatabase.GenerateUniqueAssetPath("Assets/" + classType + ".asset");

            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
        }

        private static void SetNameFromFirstScript(object userData)
        {
            GameObject gameObject = (GameObject) userData;
            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            // Attempt to name it after the first script
            if (behaviours.Length >= 1)
            {
                gameObject.name = behaviours[0].GetType().Name;
            }
            else
            {
                // No scripts found, so name after the first optional component
                Component[] components = gameObject.GetComponents<Component>();
                if (components.Length >= 2) // Ignore Transform
                {
                    gameObject.name = components[1].GetType().Name;
                }
            }
        }

        private static void CopyAsJSON(object userData)
        {
            string json = JsonUtility.ToJson(userData, true);
            EditorGUIUtility.systemCopyBuffer = json;
        }

        private static void ExportTexture(object texture)
        {
            if (texture is Texture2D texture2D)
            {
                string path = EditorUtility.SaveFilePanel("Save Texture", "Assets", texture2D.name + ".png", "png");
                if (!string.IsNullOrEmpty(path))
                {
                    byte[] bytes = texture2D.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, bytes);
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}