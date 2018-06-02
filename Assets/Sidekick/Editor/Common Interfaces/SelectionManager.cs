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

            set
            {
                selectedPath = value;
                if(SelectionChanged != null)
                {
					SelectionChanged(selectedPath);
                }
            }
        }

        public void RefreshCallbacks()
        {
            Selection.selectionChanged -= OnEditorSelectionChanged;
            Selection.selectionChanged += OnEditorSelectionChanged;
        }

        public void RefreshEditorSelection()
        {
            if (Selection.activeGameObject != null)
            {
                SelectedPath = Selection.activeGameObject.scene.name + "/" + GetPath(Selection.activeGameObject.transform);
            }
            else
            {
                SelectedPath = "";
            }
        }

        private void OnEditorSelectionChanged()
        {
            SidekickSettings settings = BridgingContext.Instance.container.Settings;
            if (settings.InspectionConnection == InspectionConnection.LocalEditor)
            {
                if (Selection.activeGameObject != null)
                {
                    SelectedPath = Selection.activeGameObject.scene.name + "/" + GetPath(Selection.activeGameObject.transform);
                }
                else
                {
                    SelectedPath = "";
                }
            }
        }

        public static string GetPath(Transform transform)
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