using System;
using System.Collections.Generic;
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
    public class SidekickInspectorWindow : BaseWindow
    {
        const float AUTO_REFRESH_FREQUENCY = 2f;

        RemoteHierarchyWindow remoteHierarchyWindow = null;

        WrappedMethod expandedMethod = null;
        List<WrappedVariable> arguments = null;

        Vector2 scrollPosition = Vector2.zero;
        SearchField searchField2;

        GetGameObjectResponse gameObjectResponse;

        double timeLastRefreshed = 0;

        string methodOutput = "";
        float opacity = 0f;

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

        [MenuItem("Tools/Sidekick")]
        public static void OpenWindow()
        {
            SidekickInspectorWindow window = EditorWindow.GetWindow<SidekickInspectorWindow>();
            window.Show();
            window.titleContent = new GUIContent("Sidekick");
            window.UpdateTitleContent();
        }

        void FindOrCreateRemoteHierarchyWindow()
        {
            remoteHierarchyWindow = EditorWindow.GetWindow<RemoteHierarchyWindow>("Remote");
            remoteHierarchyWindow.Show();
            remoteHierarchyWindow.UpdateTitleContent();
        }

        public void SetConnectionMode(InspectionConnection newConnectionMode)
        {
            Settings.InspectionConnection = newConnectionMode;

            // Reset
            gameObjectResponse = null;
            SelectionManager.SelectedPath = null;

            if (newConnectionMode == InspectionConnection.RemotePlayer)
            {
                EnableRemoteMode();
                FindOrCreateRemoteHierarchyWindow();
            }
            else
            {
                DisableRemoteMode();
                SelectionManager.RefreshEditorSelection();
            }
        }

        protected void UpdateTitleContent()
        {
            string[] guids = AssetDatabase.FindAssets("SidekickIcon t:Texture");
            if (guids.Length >= 1)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                titleContent = new GUIContent("Sidekick", texture);
            }
            else
            {
                titleContent = new GUIContent("Sidekick");
            }
        }

        private void OnSelectionChanged(string newPath)
        {
            if (!string.IsNullOrEmpty(newPath) && newPath.Contains("/")) // Valid path?
            {
                APIManager.SendToPlayers(new GetGameObjectRequest(newPath, Settings.GetGameObjectFlags, Settings.IncludeInherited));
            }
            else
            {
                APIManager.ResponseReceived(new GetGameObjectResponse());
            }
            Repaint();
        }

        void EnableRemoteMode()
        {
            EditorConnection.instance.Initialize();

            EditorConnection.instance.Register(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);
        }

        void DisableRemoteMode()
        {
            EditorConnection.instance.Unregister(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);
        }

        void OnEnable()
        {
            //Debug.Log("SidekickInspectorWindow OnEnable()");
            UpdateTitleContent();

            searchField2 = new SearchField();
            SelectionManager.SelectionChanged -= OnSelectionChanged;
            SelectionManager.SelectionChanged += OnSelectionChanged;
            APIManager.ResponseReceived -= OnResponseReceived;
            APIManager.ResponseReceived += OnResponseReceived;

            EnableRemoteMode();
        }

        void OnDisable()
        {
            //Debug.Log("SidekickInspectorWindow OnDisable()");

            SelectionManager.SelectionChanged -= OnSelectionChanged;
            APIManager.ResponseReceived -= OnResponseReceived;
            DisableRemoteMode();
        }

        private void OnMessageEvent(MessageEventArgs args)
        {
            BaseResponse response = SidekickResponseProcessor.Process(args.data);
            APIManager.ResponseReceived(response);
        }

        void OnResponseReceived(BaseResponse response)
        {
            Repaint();

            if (response is GetGameObjectResponse)
            {
                gameObjectResponse = (GetGameObjectResponse)response;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(gameObjectResponse.GameObjectName);
                foreach (var component in gameObjectResponse.Components)
                {
                    stringBuilder.Append(" ");
                    stringBuilder.AppendLine(component.TypeFullName);
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
                        stringBuilder.Append(" ");
                        if (method.Parameters.Count > 0)
                        {
                            stringBuilder.Append(method.Parameters[0].DataType);
                        }
                        stringBuilder.AppendLine();
                    }
                }
                //Debug.Log(stringBuilder);
            }
            else if (response is InvokeMethodResponse)
            {
                InvokeMethodResponse invokeMethodResponse = (InvokeMethodResponse)response;
                methodOutput = invokeMethodResponse.MethodName + " () returned:\n" + invokeMethodResponse.ReturnedVariable.Value;

                UnityEngine.Object returnedUnityObject = invokeMethodResponse.ReturnedVariable.Value as UnityEngine.Object;
                if (returnedUnityObject != null)
                {
                    EditorGUIUtility.PingObject(returnedUnityObject);
                }
                opacity = 1f;
                Repaint();
            }
            else if (response is GetUnityObjectsResponse)
            {
                GetUnityObjectsResponse castResponse = (GetUnityObjectsResponse)response;

                RemoteObjectPickerWindow.Show(castResponse.ComponentGuid, castResponse.ObjectDescriptions, castResponse.Variable, OnObjectPickerChanged);
            }
        }



        private void OnInspectorUpdate()
        {
            if (Settings.InspectionConnection == InspectionConnection.LocalEditor
                || Settings.AutoRefreshRemote)
            {
                if (EditorApplication.timeSinceStartup > timeLastRefreshed + AUTO_REFRESH_FREQUENCY)
                {
                    timeLastRefreshed = EditorApplication.timeSinceStartup;
                    APIManager.SendToPlayers(new GetHierarchyRequest());
                    if (!string.IsNullOrEmpty(SelectionManager.SelectedPath)) // Valid path?
                    {
                        APIManager.SendToPlayers(new GetGameObjectRequest(SelectionManager.SelectedPath, Settings.GetGameObjectFlags, Settings.IncludeInherited));
                    }
                }
            }
        }

        void OnGUI()
        {
            // Frame rate tracking
            if (Event.current.type == EventType.Repaint)
            {
                AnimationHelper.UpdateTime();
            }

            GUILayout.Space(9);

            SidekickSettings settings = Settings;


            EditorGUI.BeginChangeCheck();
            InspectionConnection newConnectionMode = (InspectionConnection)GUILayout.Toolbar((int)settings.InspectionConnection, new string[] { "Local", "Remote" }, new GUIStyle("LargeButton"));
            if (EditorGUI.EndChangeCheck())
            {
                SetConnectionMode(newConnectionMode);                
            }

            settings.SearchTerm = searchField2.OnGUI(settings.SearchTerm);
            GUILayout.Space(3);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Display");
            settings.GetGameObjectFlags = SidekickEditorGUI.EnumFlagsToggle(settings.GetGameObjectFlags, InfoFlags.Fields, "Fields");
            settings.GetGameObjectFlags = SidekickEditorGUI.EnumFlagsToggle(settings.GetGameObjectFlags, InfoFlags.Properties, "Properties");
            settings.GetGameObjectFlags = SidekickEditorGUI.EnumFlagsToggle(settings.GetGameObjectFlags, InfoFlags.Methods, "Methods");
			EditorGUILayout.EndHorizontal();

            if(EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(SelectionManager.SelectedPath)) // Valid path?
                {
                    APIManager.SendToPlayers(new GetGameObjectRequest(SelectionManager.SelectedPath, Settings.GetGameObjectFlags, Settings.IncludeInherited));
                }
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (gameObjectResponse != null)
            {
                string activeSearchTerm = settings.SearchTerm;

                foreach (ComponentDescription component in gameObjectResponse.Components)
                {
                    SidekickEditorGUI.DrawSplitter();
                    GUIStyle style = new GUIStyle(EditorStyles.foldout);
                    style.fontStyle = FontStyle.Bold;

                    Texture icon = IconLookup.GetIcon(component.TypeFullName);
                    GUIContent content = new GUIContent(component.TypeShortName, icon, "Object Map ID: " + component.Guid.ToString());
                    float labelWidth = EditorGUIUtility.labelWidth; // Cache label width
                    // Temporarily set the label width to full width so the icon is not squashed with long strings
                    EditorGUIUtility.labelWidth = position.width / 2f;

                    bool wasComponentExpanded = !settings.CollapsedTypeNames.Contains(component.TypeFullName);
                    bool isComponentExpanded = wasComponentExpanded;
                    if (SidekickEditorGUI.DrawHeaderWithFoldout(content, isComponentExpanded))
                        isComponentExpanded = !isComponentExpanded;
                    EditorGUIUtility.labelWidth = labelWidth; // Restore label width
                    if (isComponentExpanded != wasComponentExpanded)
                    {
                        if (isComponentExpanded == false)
                        {
                            // Not expanded, so collapse it
                            settings.CollapsedTypeNames.Add(component.TypeFullName);
                        }
                        else
                        {
                            // Expanded, remove it from collapse list
                            settings.CollapsedTypeNames.Remove(component.TypeFullName);
                        }
                    }

                    if (isComponentExpanded)
                    {
                        foreach (var field in component.Fields)
                        {
                            if (!string.IsNullOrEmpty(activeSearchTerm) && !field.VariableName.Contains(activeSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Active search term not matched, skip it
                                continue;
                            }

                            if (settings.IgnoreObsolete && (field.Attributes & VariableAttributes.Obsolete) == VariableAttributes.Obsolete)
                            {
                                // Skip obsolete entries if that setting is enabled
                                continue;
                            }
                            EditorGUI.BeginChangeCheck();
                            object newValue = VariableDrawer.Draw(component, field, OnOpenObjectPicker);
                            if (EditorGUI.EndChangeCheck() && (field.Attributes & VariableAttributes.ReadOnly) == VariableAttributes.None && field.DataType != DataType.Unknown)
                            {
                                if (newValue != field.Value || field.Attributes.HasFlagByte(VariableAttributes.IsList) || field.Attributes.HasFlagByte(VariableAttributes.IsArray))
                                {
                                    field.Value = newValue;
                                    APIManager.SendToPlayers(new SetVariableRequest(component.Guid, field));
                                }

                                //Debug.Log("Value changed in " + field.VariableName);
                            }
                        }
                        foreach (var property in component.Properties)
                        {
                            if (!string.IsNullOrEmpty(activeSearchTerm) && !property.VariableName.Contains(activeSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Active search term not matched, skip it
                                continue;
                            }

                            if (settings.IgnoreObsolete && (property.Attributes & VariableAttributes.Obsolete) == VariableAttributes.Obsolete)
                            {
                                // Skip obsolete entries if that setting is enabled
                                continue;
                            }

                            EditorGUI.BeginChangeCheck();
                            object newValue = VariableDrawer.Draw(component, property, OnOpenObjectPicker);
                            if (EditorGUI.EndChangeCheck() && (property.Attributes & VariableAttributes.ReadOnly) == VariableAttributes.None && property.DataType != DataType.Unknown)
                            {
                                if (newValue != property.Value || property.Attributes.HasFlagByte(VariableAttributes.IsList) || property.Attributes.HasFlagByte(VariableAttributes.IsArray))
                                {
                                    property.Value = newValue;
                                    APIManager.SendToPlayers(new SetVariableRequest(component.Guid, property));
                                }
                                //Debug.Log("Value changed in " + property.VariableName);
                            }
                        }

                        GUIStyle expandButtonStyle = new GUIStyle(GUI.skin.button);
                        RectOffset padding = expandButtonStyle.padding;
                        padding.left = 0;
                        padding.right = 1;
                        expandButtonStyle.padding = padding;

                        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                        labelStyle.alignment = TextAnchor.MiddleRight;
                        GUIStyle normalButtonStyle = new GUIStyle(GUI.skin.button);
                        normalButtonStyle.padding = normalButtonStyle.padding.SetLeft(100);
                        normalButtonStyle.alignment = TextAnchor.MiddleLeft;

                        foreach (var method in component.Methods)
                        {
                            if (!string.IsNullOrEmpty(activeSearchTerm) && !method.MethodName.Contains(activeSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Active search term not matched, skip it
                                continue;
                            }

                            if (settings.IgnoreObsolete && (method.MethodAttributes & MethodAttributes.Obsolete) == MethodAttributes.Obsolete)
                            {
                                // Skip obsolete entries if that setting is enabled
                                continue;
                            }

                            if(method.SafeToFire == false)
                            {
                                EditorGUI.BeginDisabledGroup(true);
                            }

                            GUILayout.BeginHorizontal();
                            if (method.ReturnType == DataType.Void)
                                labelStyle.normal.textColor = Color.grey;
                            else if ((method.ReturnTypeAttributes & VariableAttributes.IsValueType) == VariableAttributes.IsValueType)
                                labelStyle.normal.textColor = new Color(0, 0, 1);
                            else
                                labelStyle.normal.textColor = new Color32(255, 130, 0, 255);

                            string displayText = method.MethodName + " (" + method.ParameterCount + ")";

                            if((method.MethodAttributes & MethodAttributes.Static) == MethodAttributes.Static)
                            {
                                displayText += " [Static]";
                            }

                            if (method.SafeToFire == false)
                            {
                                displayText += " [Unsupported]";
                            }

                            bool wasMethodExpanded = (method.Equals(expandedMethod));

                            if (GUILayout.Button(displayText, normalButtonStyle))
                            {
                                if(wasMethodExpanded)
                                {
									APIManager.SendToPlayers(new InvokeMethodRequest(component.Guid, method.MethodName, arguments.ToArray()));
                                }
                                else
                                {
                                    // Not expanded, just use the default values
                                    List<WrappedVariable> defaultArguments = new List<WrappedVariable>();

                                    for (int i = 0; i < method.ParameterCount; i++)
                                    {
                                        WrappedParameter parameter = method.Parameters[i];
                                        defaultArguments.Add(new WrappedVariable(parameter));
                                    }

                                    APIManager.SendToPlayers(new InvokeMethodRequest(component.Guid, method.MethodName, defaultArguments.ToArray()));
                                }
                            }

                            Rect lastRect = GUILayoutUtility.GetLastRect();
                            lastRect.xMax = normalButtonStyle.padding.left;
                            GUI.Label(lastRect, TypeUtility.NameForType(method.ReturnType), labelStyle);

                            if(method.ParameterCount > 0)
                            {
								bool isMethodExpanded = GUILayout.Toggle(wasMethodExpanded, "▼", expandButtonStyle, GUILayout.Width(20));
                                GUILayout.EndHorizontal();

								if (isMethodExpanded != wasMethodExpanded) // has changed
								{
									if (isMethodExpanded)
									{
                                        // Reset the keyboard control as we don't want old text carrying over
                                        GUIUtility.keyboardControl = 0;

										expandedMethod = method;
										arguments = new List<WrappedVariable>(method.ParameterCount);
										for (int i = 0; i < method.ParameterCount; i++)
										{
                                            WrappedParameter parameter = method.Parameters[i];
                                            arguments.Add(new WrappedVariable(parameter));
										}
									}
									else
									{
										expandedMethod = null;
										arguments = null;
									}
								}
								else if (isMethodExpanded)
								{
									EditorGUI.indentLevel++;
									foreach (var argument in arguments)
									{
                                        argument.Value = VariableDrawer.Draw(null, argument, OnOpenObjectPicker);
										//argument.Value = VariableDrawer.DrawIndividualVariable(null, argument, argument.VariableName, DataTypeHelper.GetSystemTypeFromWrappedDataType(argument.DataType), argument.Value, OnOpenObjectPicker);
									}
									
									//Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(), GUI.skin.button);
									//buttonRect = EditorGUI.IndentedRect(buttonRect);
									
									
									EditorGUI.indentLevel--;
									
									GUILayout.Space(10);
								}
                            }
                            else
                            {
                                GUILayout.EndHorizontal();
                            }

                            if (method.SafeToFire == false)
                            {
                                EditorGUI.EndDisabledGroup();
                            }
                        }
                    }
                }
                SidekickEditorGUI.DrawSplitter();
            }
            EditorGUILayout.EndScrollView();

            DrawOutputBox();
        }

        public void DrawOutputBox()
        {
            GUILayout.TextArea(methodOutput, GUILayout.Height(50));

            if (opacity > 0)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                //              Color baseColor = new Color(1,0.9f,0);
                Color baseColor = new Color(0, 0, 1);
                baseColor.a = 0.3f * opacity;
                GUI.color = baseColor;
                GUI.DrawTexture(lastRect, EditorGUIUtility.whiteTexture);

                baseColor.a = 0.8f * opacity;
                GUI.color = baseColor;
                float lineThickness = 2;

                GUI.DrawTexture(new Rect(lastRect.xMin, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lastRect.xMax - lineThickness, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);

                GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMin, lastRect.width - lineThickness * 2, lineThickness), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMax - lineThickness, lastRect.width - lineThickness * 2, lineThickness), EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
                opacity -= AnimationHelper.DeltaTime;

                AnimationHelper.SetAnimationActive();
                Repaint();
            }
        }


        public void OnOpenObjectPicker(Guid componentInstanceGuid, WrappedVariable variable)
        {
            APIManager.SendToPlayers(new GetUnityObjectsRequest(variable, componentInstanceGuid));
        }

        public void OnObjectPickerChanged(Guid componentInstanceGuid, WrappedVariable variable, UnityObjectDescription objectDescription)
        {
            Debug.Log("OnObjectPickerChanged");
            variable.Value = (objectDescription != null) ? objectDescription.Guid : Guid.Empty;
            APIManager.SendToPlayers(new SetVariableRequest(componentInstanceGuid, variable));

            //SendToPlayers(APIRequest.GetUnityObjects, componentDescription, variable.TypeFullName, variable.AssemblyName);
        }
    }
}