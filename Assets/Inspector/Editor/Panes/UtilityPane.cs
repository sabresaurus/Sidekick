using UnityEngine;
using System.Collections;
using System.Reflection;
using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.Linq;
using UnityEditor;
using System.IO;

namespace Sabresaurus.Sidekick
{
	public class UtilityPane : BasePane 
	{
        public void Draw(Type componentType, object component)
        {
            Settings settings = InspectorSidekick.Current.Settings; // Grab the active window's settings

            if (component is MonoScript)
            {
                MonoScript monoScript = (MonoScript)component;
                Type classType = monoScript.GetClass();
                if (classType != null)
                {
                    if (classType.IsSubclassOf(typeof(ScriptableObject)))
                    {
                        if (GUILayout.Button("Instantiate Asset"))
                        {
                            ScriptableObject asset = ScriptableObject.CreateInstance(classType);

                            string fullPath = AssetDatabase.GenerateUniqueAssetPath("Assets/" + classType + ".asset");

                            AssetDatabase.CreateAsset(asset, fullPath);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }
            else if (component is GameObject)
            {
                GameObject gameObject = (GameObject)component;

                if (GUILayout.Button("Set Name From First Script"))
                {
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
            }

            if (GUILayout.Button("Copy As JSON"))
            {
                string json = JsonUtility.ToJson(component, true);
                EditorGUIUtility.systemCopyBuffer = json;
            }
        }
	}
}