#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Wraps selection logic for local and remote targets
    /// </summary>
    [System.Serializable]
    public class SelectionManager
    {
        string selectedPath;

        public Action<string> SelectionChanged;

        public string SelectedPath
        {
            get
            {
                return selectedPath;
            }
        }

        public void SetSelectedPath(string newPath, bool fireCallbacks = true)
        {
            selectedPath = newPath;
            if (fireCallbacks && SelectionChanged != null)
            {
                SelectionChanged(selectedPath);
            }
        }

        public void RefreshCallbacks()
        {
            Selection.selectionChanged -= OnEditorSelectionChanged;
            Selection.selectionChanged += OnEditorSelectionChanged;
        }

        public void RefreshEditorSelection(bool fireCallbacks = true)
        {
            SetSelectedPath(GetFullPath(Selection.activeGameObject), fireCallbacks);
        }

        private void OnEditorSelectionChanged()
        {
            SidekickNetworkSettings networkSettings = BridgingContext.Instance.container.NetworkSettings;
            if (networkSettings.InspectionConnection == InspectionConnection.LocalEditor)
            {
                SetSelectedPath(GetFullPath(Selection.activeObject));
            }
        }

        private static string GetFullPath(UnityEngine.Object targetObject)
        {
            if(targetObject == null)
            {
                return "";
            }

            if(targetObject is GameObject && ((GameObject)targetObject).scene.IsValid())
            {
                return ((GameObject)targetObject).scene.name + "//" + GetPathForSceneTransform(((GameObject)targetObject).transform);
            }
            else
            {
                return AssetDatabase.GetAssetPath(targetObject);
            }
        }

        public static string GetPathForSceneTransform(Transform transform)
        {
            string path = transform.name;

            Transform activeTransform = transform.parent;
            while (activeTransform != null)
            {
                path = activeTransform.name + "/" + path;
                activeTransform = activeTransform.parent;
            }

            return path;
        }
    }
}
#endif