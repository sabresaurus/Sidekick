using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
    public class UnityObjectSelectDropdown : AdvancedDropdown
    {
        private readonly Action<Object> onObjectSelected;

        class UnityObjectDropdownItem : AdvancedDropdownItem
        {
            public Object UnityObject { get; }

            public UnityObjectDropdownItem(Object unityObject) : base(GetDisplayName(unityObject))
            {
                UnityObject = unityObject;
            }

            private static string GetDisplayName(Object unityObject)
            {
                if (unityObject is EditorWindow window)
                {
                    return $"{window.titleContent.text} ({unityObject.GetType().FullName})";
                }

                return $"{unityObject.name} ({unityObject.GetType().FullName})";
            }
        }

        public UnityObjectSelectDropdown(AdvancedDropdownState state, Action<Object> onObjectSelectedCallback) : base(state)
        {
            Vector2 customMinimumSize = minimumSize;
            customMinimumSize.y = 250;
            minimumSize = customMinimumSize;
            
            onObjectSelected = onObjectSelectedCallback;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Unity Objects");

            AdvancedDropdownItem editorWindowsItem = new AdvancedDropdownItem("Editor Windows");
            root.AddChild(editorWindowsItem);

            var editorWindows= Resources.FindObjectsOfTypeAll<EditorWindow>().OrderBy(window => window.titleContent.text);
            
            foreach (EditorWindow editorWindow in editorWindows)
            {
                if (editorWindow.GetType().FullName == "UnityEditor.IMGUI.Controls.AdvancedDropdownWindow")
                {
                    // No point selecting the current dropdown as the selection closes it making it null
                    continue;
                }
                editorWindowsItem.AddChild(new UnityObjectDropdownItem(editorWindow));
            }
            
            AdvancedDropdownItem editorsItem = new AdvancedDropdownItem("Editors");
            root.AddChild(editorsItem);

            var editors = Resources.FindObjectsOfTypeAll<Editor>().OrderBy(editor => editor.name);
            
            foreach (Editor editor in editors)
            {
                editorsItem.AddChild(new UnityObjectDropdownItem(editor));
            }
            
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is UnityObjectDropdownItem windowDropdownItem)
            {
                onObjectSelected(windowDropdownItem.UnityObject);
            }
        }
    }
}