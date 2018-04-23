using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace Sabresaurus.Sidekick
{
    public class RemoteHierarchyWindow : BaseWindow
    {
        SidekickInspectorWindow parentWindow;

        TreeViewState treeViewState;

        SimpleTreeView treeView;
        SearchField hierarchySearchField;

        void OnEnable()
        {
            SidekickInspectorWindow[] windows = Resources.FindObjectsOfTypeAll<SidekickInspectorWindow>();
            if (windows.Length > 0)
            {
                parentWindow = windows[0];

                UpdateTitleContent();

                // Check if we already had a serialized view state (state 
                // that survived assembly reloading)
                if (treeViewState == null)
                {
                    treeViewState = new TreeViewState();

                }

                treeView = new SimpleTreeView(treeViewState);
                treeView.OnSelectionChanged += OnHierarchySelectionChanged;

                hierarchySearchField = new SearchField();
                hierarchySearchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;

                parentWindow.CommonContext.APIManager.ResponseReceived += OnResponseReceived;
            }
        }

        void OnDisable()
        {
            if(parentWindow != null)
            {
				parentWindow.CommonContext.APIManager.ResponseReceived -= OnResponseReceived;
            }
        }

        void OnHierarchySelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count >= 1)
            {
                IList<TreeViewItem> items = treeView.GetRows();
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].id == selectedIds[0])
                    {
                        // Get the path of the selection
                        string path = GetPathForTreeViewItem(items[i]);
                        parentWindow.CommonContext.SelectionManager.SelectedPath = path;

                        break;
                    }
                }
            }
        }

        void OnResponseReceived(BaseResponse response)
        {
            Repaint();
            Debug.Log("Hierarchy OnResponseReceived");

            if (response is GetHierarchyResponse)
            {
                GetHierarchyResponse hierarchyResponse = (GetHierarchyResponse)response;
                List<TreeViewItem> displays = new List<TreeViewItem>();
                int index = 0;
                foreach (var scene in hierarchyResponse.Scenes)
                {
                    displays.Add(new TreeViewItem { id = index, depth = 0, displayName = scene.SceneName });
                    index++;

                    foreach (var node in scene.HierarchyNodes)
                    {
                        displays.Add(new TreeViewItem { id = index, depth = node.Depth + 1, displayName = node.ObjectName });
                        index++;
                    }
                }

                treeView.SetDisplays(displays);
            }
        }

        void OnGUI()
        {
            GUIStyle centerMessageStyle = new GUIStyle(GUI.skin.label);
            centerMessageStyle.alignment = TextAnchor.MiddleCenter;
            centerMessageStyle.wordWrap = true;

            if (parentWindow == null)
            {
                SidekickInspectorWindow[] windows = Resources.FindObjectsOfTypeAll<SidekickInspectorWindow>();
                if(windows.Length > 0)
                {
                    parentWindow = windows[0];
                }
                else
                {
                    GUILayout.Label("Sidekick Inspector window must be open to use remote hierarchy", centerMessageStyle, GUILayout.ExpandHeight(true));

                    return;
                }
            }

            if (parentWindow.CommonContext.Settings.InspectionConnection == InspectionConnection.RemotePlayer)
            {
                GUILayout.Space(9);

                SidekickSettings settings = parentWindow.CommonContext.Settings;

                if (settings.InspectionConnection == InspectionConnection.RemotePlayer)
                {
                    int playerCount = EditorConnection.instance.ConnectedPlayers.Count;


                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine(string.Format("{0} players connected.", playerCount));
                    int count = 0;
                    foreach (ConnectedPlayer p in EditorConnection.instance.ConnectedPlayers)
                    {
                        builder.AppendLine(string.Format("[{0}] - {1} {2}", count++, p.name, p.playerId));
                    }
                    EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
                    settings.AutoRefreshRemote = EditorGUILayout.Toggle("Auto Refresh Remote", settings.AutoRefreshRemote);
                }

                settings.LocalDevMode = EditorGUILayout.Toggle("Local Dev Mode", settings.LocalDevMode);



                if (GUILayout.Button("Refresh Hierarchy"))
                {
                    parentWindow.CommonContext.APIManager.SendToPlayers(new GetHierarchyRequest());
                }


                DoToolbar();
                DoTreeView();
            }
            else
            {                
                GUILayout.Label("Remote hierarchy is only visible in remote mode", centerMessageStyle, GUILayout.ExpandHeight(true));
            }
        }

        static string GetPathForTreeViewItem(TreeViewItem item)
        {
            string path = item.displayName;

            item = item.parent;
            while (item != null && item.depth >= 0)
            {
                path = item.displayName + "/" + path;
                item = item.parent;
            }

            return path;
        }

        void DoToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(100);
            GUILayout.FlexibleSpace();
            treeView.searchString = hierarchySearchField.OnToolbarGUI(treeView.searchString);
            GUILayout.EndHorizontal();
        }

        void DoTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(200, 300, 300, 300);
            treeView.OnGUI(rect);
        }
    }
}