using System;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Wraps selection logic for local and remote targets
    /// </summary>
    [System.Serializable]
    public class SelectionManager : ICommonContextComponent
    {
		//CommonContext commonContext;
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
                SelectionChanged(selectedPath);
            }
        }

        public void OnEnable(CommonContext commonContext)
        {
            Debug.Log("SelectionManager OnEnable()");

            //this.commonContext = commonContext;
            Selection.selectionChanged += OnEditorSelectionChanged;
        }

        public void OnDisable()
        {
            Debug.Log("SelectionManager OnDisable()");

            Selection.selectionChanged -= OnEditorSelectionChanged;
        }

        private void OnEditorSelectionChanged()
        {
            // TODO Get path from Selection.activeObject
            if (Selection.activeGameObject != null)
            {
                SelectedPath = Selection.activeGameObject.scene.name + "/" + GetPath(Selection.activeGameObject.transform);
            }
            else
            {
                SelectedPath = "";
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