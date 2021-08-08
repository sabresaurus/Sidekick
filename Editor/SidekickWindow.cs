using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick
{
    public class SidekickWindow : EditorWindow
    {
        private const int BACK_STACK_LIMIT = 50;

        enum InspectorMode
        {
            Fields,
            Properties,
            Methods,
            Events,
        }

        private string searchTerm = "";

        readonly FieldPane fieldPane = new FieldPane();
        readonly PropertyPane propertyPane = new PropertyPane();
        readonly MethodPane methodPane = new MethodPane();
        readonly EventPane eventPane = new EventPane();

        static SidekickWindow current;

        private SearchField searchField;

        

        SelectionInfo activeSelection = new SelectionInfo();

        private bool selectionLocked = false;

        readonly List<SelectionInfo> backStack = new List<SelectionInfo>();
        readonly List<SelectionInfo> forwardStack = new List<SelectionInfo>();

        readonly PersistentData persistentData = new PersistentData();
        InspectorMode mode = InspectorMode.Fields;
        Vector2 scrollPosition;

        private bool suppressNextSelectionDetection = false;

        readonly List<KeyValuePair<Type, bool>> typesHidden = new List<KeyValuePair<Type, bool>>()
        {
            new KeyValuePair<Type, bool>(typeof(Transform), true),
            new KeyValuePair<Type, bool>(typeof(GameObject), true),
        };


        public static SidekickWindow Current => current;

        public PersistentData PersistentData => persistentData;

        [MenuItem("Window/Sidekick")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            SidekickWindow sidekick = GetWindow<SidekickWindow>();
            sidekick.UpdateTitleContent();
        }

        void OnEnable()
        {
            UpdateTitleContent();

            searchField = new SearchField();

            minSize = new Vector2(260, 100);
            
            if (selectionLocked == false && Selection.activeObject != null)
            {
                SetSelection(Selection.activeObject);
            }

            Selection.selectionChanged += OnSelectionChangeNonMessage;
        }

        void UpdateTitleContent()
        {
            titleContent = EditorGUIUtility.TrTextContentWithIcon("Sidekick", "Packages/com.sabresaurus.sidekick/Editor/SidekickIcon.png");
        }

        void OnGUI()
        {
            // Flexible width for the label based on overall width
            EditorGUIUtility.labelWidth = Mathf.Round(EditorGUIUtility.currentViewWidth * 0.4f);
            // Use inline controls if there is enough horizontal room 
            EditorGUIUtility.wideMode = EditorGUIUtility.currentViewWidth > 400;
            
            // Frame rate tracking
            if (Event.current.type == EventType.Repaint)
            {
                AnimationHelper.UpdateTime();
            }

            current = this;

            CleanStacks();
            
            DrawToolbar();

            Type[] inspectedTypes = null;
            object[] inspectedContexts = null;

            GUILayout.Space(9);

            string buttonPrefix = "";
            if (EditorGUIUtility.currentViewWidth > 400)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Selection Helpers");
            }
            else
            {
                buttonPrefix = "Select ";
            }

            var popupRect = GUILayoutUtility.GetLastRect();
            popupRect.width = EditorGUIUtility.currentViewWidth;

            var selectTypeButtonLabel = new GUIContent(buttonPrefix + "Type From Assembly");
            if (GUILayout.Button(selectTypeButtonLabel, EditorStyles.miniButton))
            {
                TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }

            var selectObjectButtonLabel = new GUIContent(buttonPrefix + "Loaded Unity Object");
            if (GUILayout.Button(selectObjectButtonLabel, EditorStyles.miniButton))
            {
                UnityObjectSelectDropdown dropdown = new UnityObjectSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }

            if (EditorGUIUtility.currentViewWidth > 400)
            {
                EditorGUILayout.EndHorizontal();
            }

            if (activeSelection.IsEmpty)
            {
                GUILayout.FlexibleSpace();
                GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel) {alignment = TextAnchor.MiddleCenter};
                GUILayout.Label("No object selected.\n\nSelect something in Unity or use one of the selection helper buttons.", style);
                GUILayout.FlexibleSpace();
                return;
            }

            if (activeSelection.Object != null)
            {
                if (activeSelection.Object is GameObject selectedGameObject)
                {
                    List<object> components = selectedGameObject.GetComponents<Component>().Cast<object>().ToList();
                    components.RemoveAll(item => item == null);
                    components.Insert(0, selectedGameObject);
                    inspectedContexts = components.ToArray();
                }
                else
                {
                    inspectedContexts = new[] {activeSelection.Object};
                }

                inspectedTypes = inspectedContexts.Select(x => x.GetType()).ToArray();
            }
            else
            {
                inspectedTypes = new[] {activeSelection.Type};

                inspectedContexts = new Type[] {null};
            }

            GUILayout.Space(5);
            searchTerm = searchField.OnToolbarGUI(searchTerm);
            
            SidekickEditorGUI.BeginLabelHighlight(searchTerm);
            
            mode = SidekickUtility.EnumToolbar(mode);

            GUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < inspectedTypes.Length; i++)
            {
                Type type = inspectedTypes[i];
                if (typesHidden.All(row => row.Key != type))
                {
                    typesHidden.Add(new KeyValuePair<Type, bool>(type, false));
                }


                int index = typesHidden.FindIndex(row => row.Key == type);

                GUIContent objectContent = EditorGUIUtility.ObjectContent(inspectedContexts[i] as UnityEngine.Object, type);
                
                string name;
                if (inspectedContexts[0] != null)
                {
                    const string TOGGLE_SPACER = "      ";
                    name = TOGGLE_SPACER + type.Name;

                    if (i == 0 && inspectedContexts[i] is Object unityObject)
                    {
                        name += $" ({unityObject.name})";
                    }
                }
                else
                {
                    name = type.Name + " (Class)";
                }

                GUIContent content = new GUIContent(name, objectContent.image);

                var inspectedContext = inspectedContexts[i];

                bool? activeOrEnabled = inspectedContexts[i] switch
                {
                    GameObject gameObject => gameObject.activeSelf,
                    Behaviour behaviour => behaviour.enabled,
                    _ => null
                };
                
                Rect foldoutRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.foldoutHeader);
                
                Rect headerRect = foldoutRect;
                headerRect.xMin += 34;
                headerRect.width = 20;
                
                // Have to do this before BeginFoldoutHeaderGroup otherwise it'll consume the mouse down event
                if (activeOrEnabled.HasValue && SidekickEditorGUI.DetectClickInRect(headerRect))
                {
                    switch (inspectedContexts[i])
                    {
                        case GameObject gameObject:
                            gameObject.SetActive(!gameObject.activeSelf);
                            break;
                        case Behaviour behaviour:
                            behaviour.enabled = !behaviour.enabled;
                            break;
                    }
                }
                
                bool foldout = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, !typesHidden[index].Value, content, EditorStyles.foldoutHeader, rect => ClassUtilities.GetMenu(inspectedContext).DropDown(rect));

                if (SidekickEditorGUI.DetectClickInRect(foldoutRect, 1))
                {
                    // Right click context menu
                    ClassUtilities.GetMenu(inspectedContext).DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                }

                if (activeOrEnabled.HasValue)
                {
                    EditorGUI.Toggle(headerRect, activeOrEnabled.Value);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();

                typesHidden[index] = new KeyValuePair<Type, bool>(type, !foldout);

                if (!typesHidden[index].Value)
                {
                    SidekickEditorGUI.DrawSplitter(0.5f);

                    EditorGUI.indentLevel++;

                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

                    if (inspectedContext != null) // Is this an object instance?
                    {
                        bindingFlags |= BindingFlags.Instance;
                    }

                    var typeScope = type;

                    while (typeScope != null)
                    {
                        if (InspectionExclusions.GetExcludedTypes().Contains(typeScope))
                        {
                            break;
                        }

                        if (typeScope != type)
                        {
                            SidekickEditorGUI.DrawTypeChainHeader(new GUIContent(": " + typeScope.Name));
                        }

                        FieldInfo[] fields = typeScope.GetFields(bindingFlags);
                        PropertyInfo[] properties = typeScope.GetProperties(bindingFlags);
                        MethodInfo[] methods = typeScope.GetMethods(bindingFlags);

                        // Hide methods and backing fields that have been generated for properties
                        if (SidekickSettings.HideAutoGenerated)
                        {
                            List<MethodInfo> methodList = new List<MethodInfo>(methods.Length);

                            foreach (MethodInfo method in methods)
                            {
                                if (!TypeUtility.IsPropertyMethod(method, typeScope))
                                {
                                    methodList.Add(method);
                                }
                            }

                            methods = methodList.ToArray();

                            List<FieldInfo> fieldList = new List<FieldInfo>(fields.Length);

                            for (int j = 0; j < fields.Length; j++)
                            {
                                if (!TypeUtility.IsBackingField(fields[j], typeScope))
                                {
                                    fieldList.Add(fields[j]);
                                }
                            }

                            fields = fieldList.ToArray();
                        }


                        FieldInfo[] events = typeScope.GetFields(bindingFlags);

                        if (mode == InspectorMode.Fields)
                        {
                            fieldPane.DrawFields(inspectedTypes[i], inspectedContexts[i], searchTerm, fields);
                        }
                        else if (mode == InspectorMode.Properties)
                        {
                            propertyPane.DrawProperties(inspectedTypes[i], inspectedContexts[i], searchTerm, properties);
                        }
                        else if (mode == InspectorMode.Methods)
                        {
                            methodPane.DrawMethods(inspectedTypes[i], inspectedContexts[i], searchTerm, methods);
                        }
                        else if (mode == InspectorMode.Events)
                        {
                            eventPane.DrawEvents(inspectedTypes[i], inspectedContexts[i], searchTerm, events);
                        }

                        typeScope = typeScope.BaseType;
                    }


                    EditorGUI.indentLevel--;
                }

                SidekickEditorGUI.DrawSplitter();
            }

            EditorGUILayout.EndScrollView();

            if (mode == InspectorMode.Methods)
            {
                methodPane.PostDraw();
            }

            
            //if(AnimationHelper.AnimationActive)
            {
                // Cause repaint on next frame
                Repaint();
                if (Event.current.type == EventType.Repaint)
                {
                    //AnimationHelper.ClearAnimationActive();
                }
            }
            
            SidekickEditorGUI.EndLabelHighlight();
        }

        private void DrawToolbar()
        {
            GUIContent backContent = new GUIContent(SidekickEditorGUI.BackIcon, "Back");
            GUIContent forwardContent = new GUIContent(SidekickEditorGUI.ForwardIcon, "Forward");
            GUIContent onLockContent = new GUIContent(SidekickEditorGUI.LockIconOn, "Selection is locked, click to unlock");
            GUIContent offLockContent = new GUIContent(SidekickEditorGUI.LockIconOff, "Selection is unlocked, click to lock");
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            HistoryButton(backContent, backStack, forwardStack);
            HistoryButton(forwardContent, forwardStack, backStack);

            GUI.enabled = true;
            
            GUIContent activeLockContent = selectionLocked ? onLockContent : offLockContent;
            
            if (GUILayout.Button(activeLockContent, EditorStyles.toolbarButton))
            {
                selectionLocked = !selectionLocked;
                if (selectionLocked == false && Selection.activeObject != null && !activeSelection.Equals(new SelectionInfo(Selection.activeObject)))
                {
                    SetSelection(Selection.activeObject);
                }
            }

            // Spacer
            GUILayout.Label("", EditorStyles.toolbarButton, GUILayout.Width(6));

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                SettingsService.OpenUserPreferences(SidekickSettingsRegister.SETTINGS_PATH);
            }
            

            EditorGUILayout.EndHorizontal();
        }

        private void HistoryButton(GUIContent content, List<SelectionInfo> stack, List<SelectionInfo> otherStack)
        {
            GUI.enabled = (stack.Count > 0);
            SidekickEditorGUI.ButtonWithOptions(content, out bool mainPressed, out bool optionsPressed);

            if (mainPressed)
            {
                SwapStackElements(stack, otherStack);
            }
            
            if (optionsPressed)
            {
                GenericMenu genericMenu = new GenericMenu();

                for (var index = 0; index < stack.Count; index++)
                {
                    SelectionInfo selectionInfo = stack[index];
                    genericMenu.AddItem(new GUIContent($"{index} - {selectionInfo.GetDisplayName()}"), false, userData =>
                    {
                        SwapStackElements(stack, otherStack, 1 + (int) userData);
                    }, index);
                }

                genericMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
            }
        }

        void SwapStackElements(List<SelectionInfo> stack, List<SelectionInfo> otherStack, int count = 1)
        {
            SelectionInfo stackPeek = stack[count - 1];

            otherStack.Insert(0, activeSelection);
            
            for (int i = 0; i < count; i++)
            {
                var temp = stack[0];
                stack.RemoveAt(0);
                if (i < count - 1)
                {
                    otherStack.Insert(0, temp);
                }
            }
            
            activeSelection = stackPeek;

            if (stackPeek.Object is Object unityObject)
            {
                suppressNextSelectionDetection = true;
                Selection.activeObject = unityObject;
            }
        }

        public void SetSelection(object newSelection)
        {
            if (!activeSelection.IsEmpty && !backStack.FirstOrDefault().Equals(activeSelection))
            {
                backStack.Insert(0, activeSelection);
                if (backStack.Count > BACK_STACK_LIMIT)
                {
                    backStack.RemoveAt(backStack.Count - 1);
                }
            }

            forwardStack.Clear();

            activeSelection = new SelectionInfo(newSelection);

            if (newSelection is Object unityObject)
            {
                Selection.activeObject = unityObject;
            }
        }

        public void SetSelection(Type newSelection)
        {
            if (!activeSelection.IsEmpty && !backStack.FirstOrDefault().Equals(activeSelection))
            {
                backStack.Insert(0, activeSelection);
                if (backStack.Count > BACK_STACK_LIMIT)
                {
                    backStack.RemoveAt(backStack.Count - 1);
                }
            }
            forwardStack.Clear();

            activeSelection = new SelectionInfo(newSelection);
        }

        /// <summary>
        /// Make sure there's no deleted objects as we wouldn't be able to select them
        /// </summary>
        void CleanStacks()
        {
            forwardStack.RemoveAll(info => info.IsEmpty);
            backStack.RemoveAll(info => info.IsEmpty);
        }

        /// <summary>
        /// Note this is not the EditorWindow.OnSelectionChange message as that is only called when the window is
        /// focused. Instead we subscribe to selection changes on enable so that even if the window is not visible we
        /// can still track changes.
        /// </summary>
        void OnSelectionChangeNonMessage()
        {
            if (suppressNextSelectionDetection)
            {
                // Do nothing this time if we've just been manipulating selection ourselves
                suppressNextSelectionDetection = false;
                return;
            }
            
            if (selectionLocked)
            {
                return;
            }

            if (Selection.activeObject == null)
            {
                return;
            }
            
            SetSelection(Selection.activeObject);

            Repaint();
        }
    }
}