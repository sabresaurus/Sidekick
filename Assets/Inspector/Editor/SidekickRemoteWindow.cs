using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.IMGUI.Controls;
using System.IO;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick
{
    public class SidekickRemoteWindow : EditorWindow
    {
        bool localDevMode = false;
        string lastDebugText = "";

        Vector2 scrollPosition = Vector2.zero;

        [SerializeField] TreeViewState m_TreeViewState;

        SimpleTreeView m_TreeView;
        SearchField m_SearchField;

        GetGameObjectResponse gameObjectResponse;

        [MenuItem("Sidekick/Remote Window")]
        static void Init()
        {
            SidekickRemoteWindow window = (SidekickRemoteWindow)EditorWindow.GetWindow(typeof(SidekickRemoteWindow));
            window.Show();
            window.titleContent = new GUIContent("Sidekick Remote");
        }

        void OnEnable()
        {
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);

            // Check if we already had a serialized view state (state 
            // that survived assembly reloading)
            if (m_TreeViewState == null)
            {
                m_TreeViewState = new TreeViewState();

            }

            m_TreeView = new SimpleTreeView(m_TreeViewState);
            m_TreeView.OnSelectionChanged += OnHierarchySelectionChanged;
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
        }

        void OnDisable()
        {
            EditorConnection.instance.Unregister(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);
            EditorConnection.instance.DisconnectAll();
        }

        void OnHierarchySelectionChanged(IList<int> selectedIds)
        {
            foreach (int id in selectedIds)
            {
                Debug.Log(id);
            }
        }

        private void OnMessageEvent(MessageEventArgs args)
        {
            BaseResponse response = SidekickResponseProcessor.Process(args.data);

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

                m_TreeView.SetDisplays(displays);

            }
            else if (response is GetGameObjectResponse)
            {
                gameObjectResponse = (GetGameObjectResponse)response;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(gameObjectResponse.GameObjectName);
                foreach (var component in gameObjectResponse.Components)
                {
                    stringBuilder.Append(" ");
                    stringBuilder.AppendLine(component.TypeName);
                    foreach (var field in component.Fields)
                    {
                        stringBuilder.Append("  ");
                        stringBuilder.Append(field.VariableName);
                        stringBuilder.Append(" ");
                        stringBuilder.Append(field.DataType);
                        stringBuilder.Append(" = ");
                        stringBuilder.Append(field.Value);
                        stringBuilder.AppendLine();
                    }
                    foreach (var property in component.Properties)
                    {
                        stringBuilder.Append("  ");
                        stringBuilder.Append(property.VariableName);
                        stringBuilder.Append(" ");
                        stringBuilder.Append(property.DataType);
                        stringBuilder.Append(" = ");
                        stringBuilder.Append(property.Value);
                        stringBuilder.AppendLine();
                    }
                }
                Debug.Log(stringBuilder);
                lastDebugText = stringBuilder.ToString();
            }
        }


        void OnGUI()
        {
            localDevMode = EditorGUILayout.Toggle("Local Dev Mode", localDevMode);

            int playerCount = EditorConnection.instance.ConnectedPlayers.Count;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("{0} players connected.", playerCount));
            int count = 0;
            foreach (ConnectedPlayer p in EditorConnection.instance.ConnectedPlayers)
            {
                builder.AppendLine(string.Format("[{0}] - {1} {2}", count++, p.name, p.playerId));
            }
            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);

            if (GUILayout.Button("GetHierarchy"))
            {
                SendToPlayers(APIRequest.GetHierarchy);
            }

            if (GUILayout.Button("GetGameObject"))
            {
                IList<int> selection = m_TreeView.GetSelection();
                if (selection.Count >= 1)
                {
                    IList<TreeViewItem> items = m_TreeView.GetRows();
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i].id == selection[0])
                        {
                            // Get the path of the selection
                            string path = GetPathForTreeViewItem(items[i]);
                            //Debug.Log(TransformHelper.GetFromPath(path).name);
                            SendToPlayers(APIRequest.GetGameObject, path);
                            break;
                        }
                    }
                }
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.TextArea(lastDebugText, GUILayout.ExpandHeight(true), GUILayout.MinHeight(300));
            DoToolbar();
            DoTreeView();

            if(gameObjectResponse != null)
            {
				foreach (var component in gameObjectResponse.Components)
				{
					GUILayout.Label(component.TypeName + " " + component.InstanceID, EditorStyles.boldLabel);
					foreach (var field in component.Fields)
					{
						EditorGUI.BeginChangeCheck();
						object newValue = TempVariableDrawer.Draw(field);
						if (EditorGUI.EndChangeCheck())
						{
							field.Value = newValue;
							SendToPlayers(APIRequest.SetVariable, component.InstanceID, field);
							
							//Debug.Log("Value changed in " + field.VariableName);
						}
					}
					foreach (var property in component.Properties)
					{
						EditorGUI.BeginChangeCheck();
						object newValue = TempVariableDrawer.Draw(property);
						if (EditorGUI.EndChangeCheck())
						{
							property.Value = newValue;
							SendToPlayers(APIRequest.SetVariable, component.InstanceID, property);
							
							//Debug.Log("Value changed in " + property.VariableName);
						}
					}
				}
            }
            EditorGUILayout.EndScrollView();
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

        void SendToPlayers(APIRequest action, params object[] args)
        {
            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write("1");

                    bw.Write(action.ToString());
                    foreach (var item in args)
                    {
                        if (item is string)
                            bw.Write((string)item);
                        else if (item is int)
                            bw.Write((int)item);
                        else if (item is WrappedVariable)
                            ((WrappedVariable)item).Write(bw);
                        else
                            throw new NotSupportedException();
                    }
                }
                bytes = ms.ToArray();
            }
            if (localDevMode)
            {
                var testResponse = SidekickRequestProcessor.Process(bytes);
                MessageEventArgs messageEvent = new MessageEventArgs();
                messageEvent.data = testResponse;
                OnMessageEvent(messageEvent);
            }
            else
            {
                EditorConnection.instance.Send(RuntimeSidekick.kMsgSendEditorToPlayer, bytes);
            }
        }


        void DoToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(100);
            GUILayout.FlexibleSpace();
            m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);
            GUILayout.EndHorizontal();
        }

        void DoTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(200, 300, 300, 300);
            m_TreeView.OnGUI(rect);
        }
    }
}