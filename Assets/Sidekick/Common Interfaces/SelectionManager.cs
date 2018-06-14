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
            SidekickSettings settings = BridgingContext.Instance.container.Settings;
            if (settings.InspectionConnection == InspectionConnection.LocalEditor)
            {
                SetSelectedPath(GetFullPath(Selection.activeGameObject));
            }
        }

        private static string GetFullPath(GameObject targetObject)
        {
            if(targetObject == null)
            {
                return "";
            }

            if(targetObject.scene.IsValid())
            {
                return targetObject.scene.name + "//" + GetPathForSceneTransform(targetObject.transform);
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