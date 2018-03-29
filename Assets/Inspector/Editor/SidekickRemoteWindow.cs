using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Sabresaurus.Sidekick.Responses;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace Sabresaurus.Sidekick
{
    public class SidekickRemoteWindow : EditorWindow
    {
        const float AUTO_REFRESH_FREQUENCY = 2f;

        bool localDevMode = false;
		bool autoRefresh = false;
  
        string lastDebugText = "";

        Vector2 scrollPosition = Vector2.zero;

        TreeViewState treeViewState;

        SimpleTreeView treeView;
        SearchField searchField;

        GetGameObjectResponse gameObjectResponse;

        double timeLastRefreshed = 0;

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
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();

            }

            treeView = new SimpleTreeView(treeViewState);
            treeView.OnSelectionChanged += OnHierarchySelectionChanged;
            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
        }

        void OnDisable()
        {
            EditorConnection.instance.Unregister(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);
            EditorConnection.instance.DisconnectAll();
        }

        void FetchSelectionComponents()
        {
            IList<int> selectedIds = treeView.GetSelection();
            if (selectedIds.Count >= 1)
            {
                IList<TreeViewItem> items = treeView.GetRows();
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].id == selectedIds[0])
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

        void OnHierarchySelectionChanged(IList<int> selectedIds)
        {
            FetchSelectionComponents();
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

                treeView.SetDisplays(displays);

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



        private void OnInspectorUpdate()
        {
            if(autoRefresh)
            {
                if(EditorApplication.timeSinceStartup > timeLastRefreshed + AUTO_REFRESH_FREQUENCY)
                {
                    timeLastRefreshed = EditorApplication.timeSinceStartup;
					SendToPlayers(APIRequest.GetHierarchy);
                    FetchSelectionComponents();
                }

            }
        }


        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(position.width/2f));

            localDevMode = EditorGUILayout.Toggle("Local Dev Mode", localDevMode);
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);

            int playerCount = EditorConnection.instance.ConnectedPlayers.Count;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("{0} players connected.", playerCount));
            int count = 0;
            foreach (ConnectedPlayer p in EditorConnection.instance.ConnectedPlayers)
            {
                builder.AppendLine(string.Format("[{0}] - {1} {2}", count++, p.name, p.playerId));
            }
            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
            if (GUILayout.Button("List Assemblies"))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Debug.Log(assembly.FullName);
                }
                // Generate a link.xml
                XmlSerializer a;
            }
            if (GUILayout.Button("Refresh Hierarchy"))
            {
                SendToPlayers(APIRequest.GetHierarchy);
            }


            //EditorGUILayout.TextArea(lastDebugText, GUILayout.ExpandHeight(true), GUILayout.MinHeight(300));
            DoToolbar();
            DoTreeView();
                
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if(gameObjectResponse != null)
            {
				foreach (var component in gameObjectResponse.Components)
				{
                    GUIStyle style = new GUIStyle(EditorStyles.foldout);
                    style.fontStyle = FontStyle.Bold;

                    Texture2D icon = IconLookup.GetIcon(component.TypeName);
                    GUIContent content = new GUIContent(component.TypeName + " " + component.InstanceID, icon);

                    EditorGUILayout.Foldout(true, content, style);

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

                    Rect rect = GUILayoutUtility.GetRect(new GUIContent(), GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    rect.xMin -= 10;
                    rect.xMax += 10;
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
				}
            }
            EditorGUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
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
            treeView.searchString = searchField.OnToolbarGUI(treeView.searchString);
            GUILayout.EndHorizontal();
        }

        void DoTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(200, 300, 300, 300);
            treeView.OnGUI(rect);
        }
    }
}