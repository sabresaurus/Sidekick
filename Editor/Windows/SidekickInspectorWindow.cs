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

        GetObjectResponse gameObjectResponse;

        double timeLastRefreshed = 0;

        string methodOutput = "";
        float opacity = 0f;

        bool registered = false;

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
            SelectionManager.SetSelectedPath(null);

            if (newConnectionMode == InspectionConnection.RemotePlayer)
            {
                FindOrCreateRemoteHierarchyWindow();
            }
            else
            {
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
                APIManager.SendToPlayers(new GetObjectRequest(newPath, Settings.GetGameObjectFlags, Settings.IncludeInherited));
            }
            else
            {
                APIManager.ResponseReceived(new GetObjectResponse());
            }
            Repaint();
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

            EditorConnection.instance.Initialize();


            if(EditorApplication.isPlayingOrWillChangePlaymode == false)
                SelectionManager.RefreshEditorSelection(false);
        }

        void OnInspectorUpdate()
        {
            if (EditorConnection.instance.ConnectedPlayers.Count > 0)
            {
                if (!registered)
                {
                    EditorConnection.instance.Register(RuntimeSidekickBridge.SEND_PLAYER_TO_EDITOR, OnMessageEvent);
                    registered = true;
                }
            }
            else
            {
                if (registered)
                {
                    registered = false;
                    //EditorConnection.instance.Unregister(RuntimeSidekickBridge.SEND_PLAYER_TO_EDITOR, OnMessageEvent);
                }
            }

            if (Settings.InspectionConnection == InspectionConnection.LocalEditor
                || Settings.AutoRefreshRemote)
            {
                if (EditorApplication.timeSinceStartup > timeLastRefreshed + AUTO_REFRESH_FREQUENCY)
                {
                    timeLastRefreshed = EditorApplication.timeSinceStartup;
                    APIManager.SendToPlayers(new GetHierarchyRequest());
                    if (!string.IsNullOrEmpty(SelectionManager.SelectedPath)) // Valid path?
                    {
                        APIManager.SendToPlayers(new GetObjectRequest(SelectionManager.SelectedPath, Settings.GetGameObjectFlags, Settings.IncludeInherited));
                    }
                }
            }
        }

        void OnDisable()
        {
            //Debug.Log("SidekickInspectorWindow OnDisable()");

            SelectionManager.SelectionChanged -= OnSelectionChanged;
            APIManager.ResponseReceived -= OnResponseReceived;
        }

        private void OnMessageEvent(MessageEventArgs args)
        {
            BaseResponse response = SidekickResponseProcessor.Process(args.data);
            APIManager.ResponseReceived(response);
        }

        void OnResponseReceived(BaseResponse response)
        {
            Repaint();

            if (response is GetObjectResponse)
            {
                gameObjectResponse = (GetObjectResponse)response;
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
            else if (response is FindUnityObjectsResponse)
            {
                FindUnityObjectsResponse castResponse = (FindUnityObjectsResponse)response;

                RemoteObjectPickerWindow.Show(castResponse.Context, castResponse.ObjectDescriptions, castResponse.Variable, OnObjectPickerChanged);
            }
#if SIDEKICK_DEBUG
            string responseString = ResponseDebug.GetDebugStringForResponse(response);
            if (!string.IsNullOrEmpty(responseString))
            {
                Debug.Log(responseString);
            }
#endif
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
            GUILayout.Label("Display");
            settings.GetGameObjectFlags = SidekickEditorGUI.EnumFlagsToggle(settings.GetGameObjectFlags, InfoFlags.Fields, "Fields");
            settings.GetGameObjectFlags = SidekickEditorGUI.EnumFlagsToggle(settings.GetGameObjectFlags, InfoFlags.Properties, "Properties");
            settings.GetGameObjectFlags = SidekickEditorGUI.EnumFlagsToggle(settings.GetGameObjectFlags, InfoFlags.Methods, "Methods");
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(SelectionManager.SelectedPath)) // Valid path?
                {
                    APIManager.SendToPlayers(new GetObjectRequest(SelectionManager.SelectedPath, Settings.GetGameObjectFlags, Settings.IncludeInherited));
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


                    bool? activeOrEnabled = null;
                    if (component.TypeShortName == "GameObject" && (settings.GetGameObjectFlags & InfoFlags.Properties) != 0)
                    {
                        activeOrEnabled = (bool)component.Scopes[0].GetPropertyValue("activeSelf");
                    }
                    else
                    {
                        ComponentScope behaviourScope = component.BehaviourScope;
                        if (behaviourScope != null && (settings.GetGameObjectFlags & InfoFlags.Properties) != 0)
                        {
                            activeOrEnabled = (bool)behaviourScope.GetPropertyValue("enabled");
                        }
                    }

                    bool? oldActiveOrEnabled = activeOrEnabled;

                    if (SidekickEditorGUI.DrawHeaderWithFoldout(content, isComponentExpanded, ref activeOrEnabled))
                        isComponentExpanded = !isComponentExpanded;

                    if (activeOrEnabled.HasValue && activeOrEnabled != oldActiveOrEnabled)
                    {
                        if (component.TypeShortName == "GameObject")
                        {
                            // Update local cache (requires method call)
                            var property = component.Scopes[0].GetProperty("activeSelf");
                            property.Value = activeOrEnabled.Value;

                            // Update via method call
                            APIManager.SendToPlayers(new InvokeMethodRequest(component.Guid, "SetActive", new WrappedVariable[] { new WrappedVariable("", activeOrEnabled.Value, typeof(bool), false) }));
                        }
                        else if (component.BehaviourScope != null)
                        {
                            // Update local cache, then ship via SetVariable
                            var property = component.BehaviourScope.GetProperty("enabled");
                            property.Value = activeOrEnabled.Value;

                            APIManager.SendToPlayers(new SetVariableRequest(component.Guid, property));
                        }
                    }
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
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                foreach (ComponentScope scope in component.Scopes)
                                {
                                    if (scope.TypeFullName != component.TypeFullName)
                                    {
                                        SidekickEditorGUI.DrawHeader2(new GUIContent(": " + scope.TypeShortName));
                                    }

                                    ObjectPickerContext objectPickerContext = new ObjectPickerContext(component.Guid);
                                    foreach (var field in scope.Fields)
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
                                        object newValue = VariableDrawer.Draw(objectPickerContext, field, OnOpenObjectPicker);
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

                                    foreach (var property in scope.Properties)
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
                                        object newValue = VariableDrawer.Draw(objectPickerContext, property, OnOpenObjectPicker);
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

                                    foreach (var method in scope.Methods)
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

                                        if (method.SafeToFire == false)
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

                                        if ((method.MethodAttributes & MethodAttributes.Static) == MethodAttributes.Static)
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
                                            if (wasMethodExpanded)
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

                                        if (method.ParameterCount > 0)
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
                                                for (int i = 0; i < arguments.Count; i++)
                                                {
                                                    var argument = arguments[i];
                                                    argument.Value = VariableDrawer.Draw(new ObjectPickerContext(i), argument, OnOpenObjectPicker);
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
                                
                                GUILayout.Space(5);
                            }

                            {// Vertical Line and Thumb
                                Rect r = GUILayoutUtility.GetLastRect();
                                GUILayout.Space(2);
                                Rect thumbRect = SidekickEditorGUI.DrawVerticalLine(r.height);
                                thumbRect.x -= 3;
                                thumbRect.y -= 2;
                                thumbRect.width = 7;
                                thumbRect.height = thumbRect.width - 2;
                                GUI.DrawTexture(thumbRect, SidekickEditorGUI.thumb);
                                GUILayout.Space(6);
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

        public void OnOpenObjectPicker(ObjectPickerContext context, WrappedVariable variable)
        {
            APIManager.SendToPlayers(new FindUnityObjectsRequest(variable, context));
        }

        public void OnObjectPickerChanged(ObjectPickerContext context, WrappedVariable variable, UnityObjectDescription objectDescription)
        {
            if (context.ComponentGuid != Guid.Empty)
            {
                // Remote component GUID specified, send a SetVariableRequest to update a field or property
                variable.Value = (objectDescription != null) ? objectDescription.Guid : Guid.Empty;
                APIManager.SendToPlayers(new SetVariableRequest(context.ComponentGuid, variable));
            }
            else if (context.ArgumentIndex != -1)
            {
                // If an argument index is supplied, this is updating an editor driven method argument set
                arguments[context.ArgumentIndex].Value = (objectDescription != null) ? objectDescription.Guid : Guid.Empty;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}