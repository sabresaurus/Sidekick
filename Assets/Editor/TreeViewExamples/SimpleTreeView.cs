using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;


namespace Sabresaurus.Sidekick
{
	class SimpleTreeView : TreeView
	{
        List<TreeViewItem> displays = new List<TreeViewItem>();
        public Action<IList<int>> OnSelectionChanged;

        public SimpleTreeView(TreeViewState treeViewState)
			: base(treeViewState)
		{
            
			Reload();
		}

        public void SetDisplays(List<TreeViewItem> displays)
        {
            this.displays = displays;
            Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
		
		protected override TreeViewItem BuildRoot ()
		{
			// BuildRoot is called every time Reload is called to ensure that TreeViewItems 
			// are created from data. Here we just create a fixed set of items, in a real world example
			// a data model should be passed into the TreeView and the items created from the model.

			// This section illustrates that IDs should be unique and that the root item is required to 
			// have a depth of -1 and the rest of the items increment from that.
			var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
			//var allItems = new List<TreeViewItem> 
			//{
			//	new TreeViewItem {id = 1, depth = 0, displayName = "Animals"},
			//	new TreeViewItem {id = 2, depth = 1, displayName = "Mammals"},
			//	new TreeViewItem {id = 3, depth = 2, displayName = "Tiger"},
			//	new TreeViewItem {id = 4, depth = 2, displayName = "Elephant"},
			//	new TreeViewItem {id = 5, depth = 2, displayName = "Okapi"},
			//	new TreeViewItem {id = 6, depth = 2, displayName = "Armadillo"},
			//	new TreeViewItem {id = 7, depth = 1, displayName = "Reptiles"},
			//	new TreeViewItem {id = 8, depth = 2, displayName = "Crocodile"},
			//	new TreeViewItem {id = 9, depth = 2, displayName = "Lizard"},
			//};
			
			// Utility method that initializes the TreeViewItem.children and -parent for all items.
            SetupParentsAndChildrenFromDepths (root, displays);
			
			// Return root of the tree
			return root;
		}

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            OnSelectionChanged(selectedIds);
        }
	}
}

