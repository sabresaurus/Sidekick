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
    public enum InspectionConnection { LocalEditor, RemotePlayer };

    public class SidekickInspectorWindow : BaseWindow
    {
        const float AUTO_REFRESH_FREQUENCY = 2f;

        RemoteHierarchyWindow remoteHierarchyWindow = null;

        WrappedMethod expandedMethod = null;
        List<WrappedVariable> arguments = null;

        // Shared serialized context that persists through recompiles
        CommonContext commonContext = null;

        Vector2 scrollPosition = Vector2.zero;
        SearchField searchField2;

        GetGameObjectResponse gameObjectResponse;

        double timeLastRefreshed = 0;


        public CommonContext CommonContext
        {
            get
            {
                return commonContext;
            }
        }

        [MenuItem("Tools/Sidekick")]
        static void Init()
        {
            SidekickInspectorWindow window = EditorWindow.GetWindow<SidekickInspectorWindow>();
            window.Show();
            window.titleContent = new GUIContent("Sidekick");
            window.UpdateTitleContent();
        }

        void FindOrCreateRemoteHierarchyWindow()
        {
            remoteHierarchyWindow = EditorWindow.GetWindow<RemoteHierarchyWindow>("Hierarchy");
            remoteHierarchyWindow.Show();
        }

        void OnEnable()
        {
            UpdateTitleContent();

            searchField2 = new SearchField();

            if(commonContext == null)
            {
                commonContext = new CommonContext();
            }

            commonContext.OnEnable();
            commonContext.SelectionManager.SelectionChanged += OnSelectionChanged;
            commonContext.APIManager.ResponseReceived += OnResponseReceived;

            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);

        }

        private void OnSelectionChanged(string newPath)
        {
            if (!string.IsNullOrEmpty(newPath)) // Valid path?
            {
                commonContext.APIManager.SendToPlayers(new GetGameObjectRequest(newPath, commonContext.Settings.GetGameObjectFlags));
            }
            Repaint();
        }

        void OnDisable()
        {
			commonContext.SelectionManager.SelectionChanged -= OnSelectionChanged;
            commonContext.APIManager.ResponseReceived -= OnResponseReceived;
            EditorConnection.instance.Unregister(RuntimeSidekick.kMsgSendPlayerToEditor, OnMessageEvent);
            EditorConnection.instance.DisconnectAll();
        }

        private void OnMessageEvent(MessageEventArgs args)
        {
            BaseResponse response = SidekickResponseProcessor.Process(args.data);
            commonContext.APIManager.ResponseReceived(response);
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
                Debug.Log(invokeMethodResponse.MethodName + "() returned " + invokeMethodResponse.ReturnedVariable.Value);
            }
            else if (response is GetUnityObjectsResponse)
            {
                GetUnityObjectsResponse castResponse = (GetUnityObjectsResponse)response;

                RemoteObjectPickerWindow.Show(castResponse.ComponentDescription, castResponse.ObjectDescriptions, castResponse.Variable, OnObjectPickerChanged);
            }
        }



        private void OnInspectorUpdate()
        {
            if (commonContext.Settings.InspectionConnection == InspectionConnection.LocalEditor
                || commonContext.Settings.AutoRefreshRemote)
            {
                if (EditorApplication.timeSinceStartup > timeLastRefreshed + AUTO_REFRESH_FREQUENCY)
                {
                    timeLastRefreshed = EditorApplication.timeSinceStartup;
                    commonContext.APIManager.SendToPlayers(new GetHierarchyRequest());
                    if (!string.IsNullOrEmpty(commonContext.SelectionManager.SelectedPath)) // Valid path?
                    {
                        commonContext.APIManager.SendToPlayers(new GetGameObjectRequest(commonContext.SelectionManager.SelectedPath, commonContext.Settings.GetGameObjectFlags));
                    }   
                }
            }
        }

        void OnGUI()
        {
            GUILayout.Space(9);

            SidekickSettings settings = commonContext.Settings;

            EditorGUI.BeginChangeCheck();
            settings.InspectionConnection = (InspectionConnection)GUILayout.Toolbar((int)settings.InspectionConnection, new string[] { "Local", "Remote" }, new GUIStyle("LargeButton"));
            if (EditorGUI.EndChangeCheck())
            {
                if (settings.InspectionConnection == InspectionConnection.RemotePlayer)
                {
                    FindOrCreateRemoteHierarchyWindow();
                }
            }

            settings.SearchTerm = searchField2.OnGUI(settings.SearchTerm);
            GUILayout.Space(3);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (gameObjectResponse != null)
            {
                string activeSearchTerm = settings.SearchTerm;

                foreach (var component in gameObjectResponse.Components)
                {
                    GUIStyle style = new GUIStyle(EditorStyles.foldout);
                    style.fontStyle = FontStyle.Bold;

                    Texture icon = IconLookup.GetIcon(component.TypeFullName);
                    GUIContent content = new GUIContent(component.TypeShortName, icon, "Instance ID: " + component.InstanceID.ToString());
                    float labelWidth = EditorGUIUtility.labelWidth; // Cache label width
                    // Temporarily set the label width to full width so the icon is not squashed with long strings
                    EditorGUIUtility.labelWidth = position.width / 2f;

                    bool wasComponentExpanded = !settings.CollapsedTypeNames.Contains(component.TypeFullName);
                    bool isComponentExpanded = EditorGUILayout.Foldout(wasComponentExpanded, content, style);
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
                            EditorGUI.BeginChangeCheck();
                            object newValue = VariableDrawer.Draw(component, field, OnOpenObjectPicker);
                            if (EditorGUI.EndChangeCheck() && (field.Attributes & VariableAttributes.ReadOnly) == VariableAttributes.None)
                            {
                                if (newValue != field.Value)
                                {
                                    field.Value = newValue;
                                    commonContext.APIManager.SendToPlayers(new SetVariableRequest(component.InstanceID, field));
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
                            EditorGUI.BeginChangeCheck();
                            object newValue = VariableDrawer.Draw(component, property, OnOpenObjectPicker);
                            if (EditorGUI.EndChangeCheck() && (property.Attributes & VariableAttributes.ReadOnly) == VariableAttributes.None)
                            {
                                if (newValue != property.Value)
                                {
                                    property.Value = newValue;
                                    commonContext.APIManager.SendToPlayers(new SetVariableRequest(component.InstanceID, property));
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
                            GUILayout.BeginHorizontal();
                            if (method.ReturnType == DataType.Void)
                                labelStyle.normal.textColor = Color.grey;
                            else if ((method.ReturnTypeAttributes & VariableAttributes.IsValueType) == VariableAttributes.IsValueType)
                                labelStyle.normal.textColor = new Color(0, 0, 1);
                            else
                                labelStyle.normal.textColor = new Color32(255, 130, 0, 255);

                            if (GUILayout.Button(method.MethodName + " (" + method.ParameterCount + ")", normalButtonStyle))
                            {
                                List<WrappedVariable> defaultArguments = new List<WrappedVariable>();

                                for (int i = 0; i < method.ParameterCount; i++)
                                {
                                    Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(method.Parameters[i].DataType);
                                    object defaultValue = TypeUtility.GetDefaultValue(type);

                                    WrappedParameter parameter = method.Parameters[i];
                                    defaultArguments.Add(new WrappedVariable(parameter.VariableName, defaultValue, type, false));
                                }

                                commonContext.APIManager.SendToPlayers(new InvokeMethodRequest(component.InstanceID, method.MethodName, defaultArguments.ToArray()));
                            }

                            Rect lastRect = GUILayoutUtility.GetLastRect();
                            lastRect.xMax = normalButtonStyle.padding.left;
                            GUI.Label(lastRect, TypeUtility.NameForType(method.ReturnType), labelStyle);

                            bool wasMethodExpanded = (expandedMethod == method);
                            bool isMethodExpanded = GUILayout.Toggle(wasMethodExpanded, "▼", expandButtonStyle, GUILayout.Width(20));
                            GUILayout.EndHorizontal();
                            if (isMethodExpanded != wasMethodExpanded) // has changed
                            {
                                if (isMethodExpanded)
                                {
                                    expandedMethod = method;
                                    arguments = new List<WrappedVariable>(method.ParameterCount);
                                    for (int i = 0; i < method.ParameterCount; i++)
                                    {
                                        Type type = DataTypeHelper.GetSystemTypeFromWrappedDataType(method.Parameters[i].DataType);
                                        object defaultValue = TypeUtility.GetDefaultValue(type);

                                        WrappedParameter parameter = method.Parameters[i];
                                        arguments.Add(new WrappedVariable(parameter.VariableName, defaultValue, type, false));
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
                                    argument.Value = VariableDrawer.DrawIndividualVariable(null, argument, argument.VariableName, DataTypeHelper.GetSystemTypeFromWrappedDataType(argument.DataType), argument.Value, OnOpenObjectPicker);
                                }

                                Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(), GUI.skin.button);
                                buttonRect = EditorGUI.IndentedRect(buttonRect);

                                if (GUI.Button(buttonRect, "Fire"))
                                {
                                    commonContext.APIManager.SendToPlayers(new InvokeMethodRequest(component.InstanceID, method.MethodName, arguments.ToArray()));
                                }
                                EditorGUI.indentLevel--;

                                GUILayout.Space(10);
                            }
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
        }


        public void OnOpenObjectPicker(ComponentDescription componentDescription, WrappedVariable variable)
        {
            commonContext.APIManager.SendToPlayers(new GetUnityObjectsRequest(variable, componentDescription));
        }

        public void OnObjectPickerChanged(ComponentDescription componentDescription, WrappedVariable variable, UnityObjectDescription objectDescription)
        {
            Debug.Log("OnObjectPickerChanged");
            variable.Value = (objectDescription != null) ? objectDescription.InstanceID : 0;
            commonContext.APIManager.SendToPlayers(new SetVariableRequest(componentDescription.InstanceID, variable));

            //SendToPlayers(APIRequest.GetUnityObjects, componentDescription, variable.TypeFullName, variable.AssemblyName);
        }
    }
}