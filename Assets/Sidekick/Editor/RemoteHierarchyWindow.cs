using System.Collections.Generic;
using System.Text;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

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

            UpdateTitleContent();
        }

        void OnDisable()
        {
            if (parentWindow != null)
            {
                parentWindow.CommonContext.APIManager.ResponseReceived -= OnResponseReceived;
                parentWindow = null;
            }
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
                        parentWindow.CommonContext.SelectionManager.SelectedPath = path;

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

        bool AcquireParentWindowIfPossible()
        {
            SidekickInspectorWindow[] windows = Resources.FindObjectsOfTypeAll<SidekickInspectorWindow>();
            if (windows.Length > 0 && windows[0].CommonContext.Enabled)
            {
                parentWindow = windows[0];

                parentWindow.CommonContext.APIManager.ResponseReceived += OnResponseReceived;
                return true;
            }
            else
            {
                return false;
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

            if (parentWindow == null || parentWindow.CommonContext.Enabled == false)
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

            if (parentWindow.CommonContext.Settings.InspectionConnection == InspectionConnection.RemotePlayer)
            {
                GUILayout.Space(9);

                SidekickSettings settings = parentWindow.CommonContext.Settings;

                if (settings.InspectionConnection == InspectionConnection.RemotePlayer)
                {
                    int playerCount = EditorConnection.instance.ConnectedPlayers.Count;


                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine(string.Format("{0} players connected.", playerCount));

                    bool validConnection = (playerCount > 0);

#if UNITY_2017_1_OR_NEWER
                    // If we're in Local Dev Mode also consider that a valid connection
                    validConnection |= settings.LocalDevMode;
#endif


                    if (validConnection == false)
                    {
#if UNITY_2017_1_OR_NEWER
                        EditorGUILayout.HelpBox("No player connected, selected a Connected Player in the Console window or attach the Profiler to a remote player", MessageType.Warning);
#else
                        EditorGUILayout.HelpBox("No player connected, attach the Profiler to a remote player", MessageType.Warning);
#endif
                    }
                    else
                    {
                        int count = 0;
                        foreach (ConnectedPlayer p in EditorConnection.instance.ConnectedPlayers)
                        {
#if UNITY_2017_3_OR_NEWER
                            // ConnectedPlayer interface changed in 2017.3
                            builder.AppendLine(string.Format("[{0}] - {1} {2}", count++, p.name, p.playerId));
#else
                            builder.AppendLine(string.Format("[{0}] - {1}", count++, p.PlayerId));
#endif
                        }

                        EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
                    }
                    settings.AutoRefreshRemote = EditorGUILayout.Toggle("Auto Refresh Remote", settings.AutoRefreshRemote);

#if SIDEKICK_DEBUG
                    settings.LocalDevMode = EditorGUILayout.Toggle("Local Dev Mode", settings.LocalDevMode);
#endif
                    if (validConnection)
                    {
                        if (GUILayout.Button("Refresh Hierarchy"))
                        {
                            parentWindow.CommonContext.APIManager.SendToPlayers(new GetHierarchyRequest());
                        }

                        DoToolbar();
                        DoTreeView();
                    }
                }
            }
            else
            {
                treeView.SetDisplays(new List<TreeViewItem>());
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