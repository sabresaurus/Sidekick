using System;
using System.Collections.Generic;
using System.Text;
using Sabresaurus.EditorNetworking;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class RemoteHierarchyWindow : BaseWindow
    {
        SidekickInspectorWindow parentWindow;

        TreeViewState treeViewState;

        HierarchyTreeView treeView;
        SearchField hierarchySearchField;
        int cachedPlayerCount = 0;

        DateTime lastSendHeartbeat = DateTime.MinValue;

        public APIManager APIManager
        {
            get
            {
                return BridgingContext.Instance.container.APIManager;
            }
        }
        public SelectionManager SelectionManager
        {
            get
            {
                return BridgingContext.Instance.container.SelectionManager;
            }
        }
        public SidekickSettings Settings
        {
            get
            {
                return BridgingContext.Instance.container.Settings;
            }
        }

        void OnEnable()
        {
            // Check if we already had a serialized view state (state 
            // that survived assembly reloading)
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }

            treeView = new HierarchyTreeView(treeViewState);
            treeView.OnSelectionChanged += OnHierarchySelectionChanged;

            hierarchySearchField = new SearchField();
            hierarchySearchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;

            //NetworkWrapper_Editor.RegisterConnection(OnPlayerConnected);
            //NetworkWrapper_Editor.RegisterDisconnection(OnPlayerDisconnected);

            APIManager.ResponseReceived -= OnResponseReceived;
            APIManager.ResponseReceived += OnResponseReceived;

            UpdateTitleContent();
        }

        private void OnPlayerConnected(int playerID)
        {
            Repaint();

            APIManager.SendToPlayers(new GetHierarchyRequest());
        }

        private void OnPlayerDisconnected(int playerID)
        {
            Repaint();
        }

        void OnDisable()
        {
            APIManager.ResponseReceived -= OnResponseReceived;
        }

        public void UpdateTitleContent()
        {
            string[] guids = AssetDatabase.FindAssets("HierarchyIcon t:Texture");
            if (guids.Length >= 1)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                titleContent = new GUIContent("Remote", texture);
            }
            else
            {
                titleContent = new GUIContent("Remote");
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
                        SelectionManager.SetSelectedPath(path);

                        break;
                    }
                }
            }
        }

        void OnResponseReceived(BaseResponse response)
        {
            Repaint();
            //Debug.Log("Hierarchy OnResponseReceived");

            if (response is GetHierarchyResponse)
            {
                GetHierarchyResponse hierarchyResponse = (GetHierarchyResponse)response;
                List<TreeViewItem> displays = new List<TreeViewItem>();
                List<HierarchyNode> allNodes = new List<HierarchyNode>();
                
                int index = 0;
                foreach (var scene in hierarchyResponse.Scenes)
                {
                    displays.Add(new TreeViewItem { id = index, depth = 0, displayName = scene.SceneName });
                    allNodes.Add(null);
                    treeView.SetExpanded(index, true);
                    index++;
                    foreach (var node in scene.HierarchyNodes)
                    {
                        displays.Add(new TreeViewItem { id = index, depth = node.Depth + 1, displayName = node.ObjectName });
                        allNodes.Add(node);
                        index++;
                    }
                }
                treeView.SetDisplays(displays, allNodes);
            }
        }

        bool AcquireParentWindowIfPossible()
        {
            SidekickInspectorWindow[] windows = Resources.FindObjectsOfTypeAll<SidekickInspectorWindow>();
            if (windows.Length > 0)
            {
                parentWindow = windows[0];

                return true;
            }
            else
            {
                return false;
            }
        }

        // Called at 10 frames per second
        void OnInspectorUpdate()
        {
            if(EditorMessaging.KnownEndpoints.Count != cachedPlayerCount)
            {
                cachedPlayerCount = EditorMessaging.KnownEndpoints.Count;
                Repaint();
            }

            if (EditorMessaging.IsConnected)
            {
                // If there's a valid connection send a heartbeat every second so the device knows we're still here
                if (DateTime.UtcNow - lastSendHeartbeat > TimeSpan.FromSeconds(1))
                {
                    APIManager.SendToPlayers(new HeartbeatRequest());
                    lastSendHeartbeat = DateTime.UtcNow;
                }
            }
        }

        void OnGUI()
        {
            GUIStyle centerMessageStyle = new GUIStyle(GUI.skin.label);
            centerMessageStyle.alignment = TextAnchor.MiddleCenter;
            centerMessageStyle.wordWrap = true;

            if (parentWindow == null)
            {
                AcquireParentWindowIfPossible();
            }

            if (parentWindow == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Sidekick Inspector window must be open to use remote hierarchy", centerMessageStyle);
                if (GUILayout.Button("Open Sidekick Inspector"))
                {
                    SidekickInspectorWindow.OpenWindow();
                    AcquireParentWindowIfPossible();
                }
                GUILayout.FlexibleSpace();


                return;
            }

            if (Settings.InspectionConnection == InspectionConnection.RemotePlayer)
            {
                GUILayout.Space(9);

                if (Settings.InspectionConnection == InspectionConnection.RemotePlayer)
                {
                    bool validConnection = (EditorMessaging.KnownEndpoints.Count >= 1);

                    if (validConnection == false)
                    {
                        EditorGUILayout.HelpBox("No player found, make sure both the editor and player are on the same network", MessageType.Warning);
                    }
                    else
                    {
                        List<string> displayNames = new List<string>();
                        int index = 0;
                        int selectionIndex = 0;
                        foreach (var pair in EditorMessaging.KnownEndpoints)
                        {
                            displayNames.Add(string.Format("{0} - {1}", pair.Key, pair.Value));
                            if(pair.Key == EditorMessaging.ConnectedIP)
                            {
                                selectionIndex = index;
                            }
                            index++;
                        }
                        EditorGUILayout.Popup(selectionIndex, displayNames.ToArray());
                    }
                    Settings.AutoRefreshRemote = EditorGUILayout.Toggle("Auto Refresh Remote", Settings.AutoRefreshRemote);

#if SIDEKICK_DEBUG
                    Settings.LocalDevMode = EditorGUILayout.Toggle("Local Dev Mode", Settings.LocalDevMode);
#endif
                    if (validConnection)
                    {
                        if (GUILayout.Button("Refresh Hierarchy"))
                        {
                            APIManager.SendToPlayers(new GetHierarchyRequest());
                        }

                        DoToolbar();
                        DoTreeView();
                    }
                }
            }
            else
            {
                treeView.ClearDisplays();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Remote hierarchy is only visible in remote mode", centerMessageStyle);
                if(GUILayout.Button("Set Remote Mode"))
                {
                    parentWindow.SetConnectionMode(InspectionConnection.RemotePlayer);
                }
                GUILayout.FlexibleSpace();
            }
        }

        static string GetPathForTreeViewItem(TreeViewItem item)
        {
            string path = item.displayName;

            item = item.parent;
            while (item != null && item.depth >= 0)
            {
                if(item.depth > 0)
                    path = item.displayName + "/" + path;
                else
                    path = item.displayName + "//" + path;
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
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(), GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeView.OnGUI(rect);
        }
    }
}