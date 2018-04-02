﻿using System;
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
    public class SidekickWindow : EditorWindow
    {
        const float AUTO_REFRESH_FREQUENCY = 2f;

        bool localDevMode = false;
        bool autoRefresh = false;
        string searchTerm = "";

        Vector2 scrollPosition = Vector2.zero;


        InfoFlags getGameObjectFlags = InfoFlags.Fields | InfoFlags.Properties;

        TreeViewState treeViewState;

        SimpleTreeView treeView;
        SearchField treeViewSearchField;
        SearchField searchField2;

        GetGameObjectResponse gameObjectResponse;

        double timeLastRefreshed = 0;

        [MenuItem("Tools/Sidekick")]
        static void Init()
        {
            SidekickWindow window = (SidekickWindow)EditorWindow.GetWindow(typeof(SidekickWindow));
            window.Show();
            window.titleContent = new GUIContent("Sidekick");
            window.UpdateTitleContent();
        }

        void UpdateTitleContent()
        {
            string[] guids = AssetDatabase.FindAssets("SidekickIcon t:Texture");
            if(guids.Length >= 1)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
				titleContent = new GUIContent("Sidekick", texture);
            }
            else
            {
                titleContent = new GUIContent("Sidekick");
            }
        }

        void OnEnable()
        {
            UpdateTitleContent();

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

            searchField2 = new SearchField();

            treeViewSearchField = new SearchField();
            treeViewSearchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
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
                        SendToPlayers(APIRequest.GetGameObject, path, getGameObjectFlags);
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
                    foreach (var method in component.Methods)
                    {
                        stringBuilder.Append("  ");
                        stringBuilder.Append(method.MethodName);
                        stringBuilder.Append(" ");
                        stringBuilder.Append(method.ReturnType);
                        stringBuilder.Append(" ");
                        stringBuilder.Append(method.ParameterCount);
                        stringBuilder.AppendLine();
                    }
                }
                Debug.Log(stringBuilder);
            }
            else if (response is InvokeMethodResponse)
            {
                InvokeMethodResponse invokeMethodResponse = (InvokeMethodResponse)response;
                Debug.Log(invokeMethodResponse.MethodName + "() returned " + invokeMethodResponse.ReturnedVariable.Value);
            }
        }



        private void OnInspectorUpdate()
        {
            if (autoRefresh)
            {
                if (EditorApplication.timeSinceStartup > timeLastRefreshed + AUTO_REFRESH_FREQUENCY)
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
            // Column 1
            GUILayout.BeginVertical(GUILayout.Width(position.width / 2f));

            localDevMode = EditorGUILayout.Toggle("Local Dev Mode", localDevMode);
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            getGameObjectFlags = (InfoFlags)EditorGUILayout.EnumFlagsField(getGameObjectFlags);
            int playerCount = EditorConnection.instance.ConnectedPlayers.Count;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("{0} players connected.", playerCount));
            int count = 0;
            foreach (ConnectedPlayer p in EditorConnection.instance.ConnectedPlayers)
            {
                builder.AppendLine(string.Format("[{0}] - {1} {2}", count++, p.name, p.playerId));
            }
            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
            if (GUILayout.Button("Generate Test link.xml"))
            {
                LinkXMLFactory.Generate(LinkXMLFactory.DEFAULT_TYPES);
            }
            if (GUILayout.Button("Refresh Hierarchy"))
            {
                SendToPlayers(APIRequest.GetHierarchy);
            }


            //EditorGUILayout.TextArea(lastDebugText, GUILayout.ExpandHeight(true), GUILayout.MinHeight(300));
            DoToolbar();
            DoTreeView();

            GUILayout.EndVertical();
            Rect verticalLineRect = new Rect(position.width / 2f - 1, 0, 1, position.height);
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            GUI.DrawTexture(verticalLineRect, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            // Column 2
            GUILayout.BeginVertical();
            GUILayout.Space(2);
            searchTerm = searchField2.OnGUI(searchTerm);
            GUILayout.Space(3);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (gameObjectResponse != null)
            {
                foreach (var component in gameObjectResponse.Components)
                {
                    GUIStyle style = new GUIStyle(EditorStyles.foldout);
                    style.fontStyle = FontStyle.Bold;

                    Texture icon = IconLookup.GetIcon(component.TypeName);
                    GUIContent content = new GUIContent(component.TypeName, icon, "Instance ID: " + component.InstanceID.ToString());
                    float labelWidth = EditorGUIUtility.labelWidth; // Cache label width
                    // Temporarily set the label width to full width so the icon is not squashed with long strings
                    EditorGUIUtility.labelWidth = position.width / 2f;
                    EditorGUILayout.Foldout(true, content, style);

                    EditorGUIUtility.labelWidth = labelWidth; // Restore label width

                    foreach (var field in component.Fields)
                    {
                        EditorGUI.BeginChangeCheck();
                        object newValue = VariableDrawer.Draw(field);
                        if (EditorGUI.EndChangeCheck() && (field.Attributes & VariableAttributes.ReadOnly) == VariableAttributes.None)
                        {
                            field.Value = newValue;
                            SendToPlayers(APIRequest.SetVariable, component.InstanceID, field);

                            //Debug.Log("Value changed in " + field.VariableName);
                        }
                    }
                    foreach (var property in component.Properties)
                    {
                        EditorGUI.BeginChangeCheck();
                        object newValue = VariableDrawer.Draw(property);
                        if (EditorGUI.EndChangeCheck() && (property.Attributes & VariableAttributes.ReadOnly) == VariableAttributes.None)
                        {
                            property.Value = newValue;
                            SendToPlayers(APIRequest.SetVariable, component.InstanceID, property);

                            //Debug.Log("Value changed in " + property.VariableName);
                        }
                    }
                    foreach (var method in component.Methods)
                    {
                        if (GUILayout.Button(method.ReturnType + " " + method.MethodName + " (" + method.ParameterCount + ")"))
                        {
                            SendToPlayers(APIRequest.InvokeMethod, component.InstanceID, method.MethodName, 0);
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
                        else if (item is Enum)
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
            treeView.searchString = treeViewSearchField.OnToolbarGUI(treeView.searchString);
            GUILayout.EndHorizontal();
        }

        void DoTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(200, 300, 300, 300);
            treeView.OnGUI(rect);
        }
    }
}