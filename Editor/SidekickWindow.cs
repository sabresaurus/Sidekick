using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
#if ECS_EXISTS
using Unity.Entities;
using Unity.Entities.Editor;
#endif
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

        PersistentData persistentData = new PersistentData();
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
            
            OnSelectionChangeNonMessage();

            Selection.selectionChanged += OnSelectionChangeNonMessage;
            EditorApplication.playModeStateChanged += _ => OnSelectionChangeNonMessage(); 
        }

        private void ShowButton(Rect rect)
        {
            if (EditorGUI.Toggle(rect, selectionLocked, (GUIStyle)"IN LockButton") != selectionLocked)
            {
                selectionLocked = !selectionLocked;
                if (selectionLocked == false && Selection.activeObject != null && !activeSelection.Equals(new SelectionInfo(Selection.activeObject)))
                {
                    SetSelection(Selection.activeObject);
                }
            }
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
            ECSContext[] inspectedECSContexts = null;

            GUILayout.Space(9);

            string buttonPrefix = "";

#if ECS_EXISTS
            int selectionWrapWidth = 465;
#else
            int selectionWrapWidth = 400;
#endif
            if (EditorGUIUtility.currentViewWidth > selectionWrapWidth)
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

            if (GUILayout.Button(new GUIContent(buttonPrefix + "Type From Assembly"), EditorStyles.miniButton))
            {
                TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }

            if (GUILayout.Button(new GUIContent(buttonPrefix + "Loaded Unity Object"), EditorStyles.miniButton))
            {
                UnityObjectSelectDropdown dropdown = new UnityObjectSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }

#if ECS_EXISTS
            if (GUILayout.Button(new GUIContent(buttonPrefix + "ECS System"), EditorStyles.miniButton))
            {
                ECSSystemSelectDropdown dropdown = new ECSSystemSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }
#endif

            if (EditorGUIUtility.currentViewWidth > selectionWrapWidth)
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
#if ECS_EXISTS
                else if (activeSelection.Object is EntitySelectionProxy entitySelectionProxy)
                {
                    EntityManager currentEntityManager = entitySelectionProxy.World.EntityManager;
                    string name = currentEntityManager.GetName(entitySelectionProxy.Entity);

                    if (string.IsNullOrEmpty(name))
                    {
                        name = "Entity " + entitySelectionProxy.Entity.Index;
                    }

                    inspectedContexts = new object [1 + currentEntityManager.GetComponentCount(entitySelectionProxy.Entity)];
                    inspectedContexts[0] = activeSelection.Object;
                    inspectedECSContexts = new ECSContext[1 + currentEntityManager.GetComponentCount(entitySelectionProxy.Entity)];
                    inspectedECSContexts[0] = new ECSContext {EntityManager = currentEntityManager, Entity = entitySelectionProxy.Entity};

                    NativeArray<ComponentType> types = currentEntityManager.GetComponentTypes(entitySelectionProxy.Entity);
                    for (var index = 0; index < types.Length; index++)
                    {
                        object componentData = ECSAccess.GetComponentData(currentEntityManager, entitySelectionProxy.Entity, types[index]);

                        inspectedContexts[1 + index] = componentData;
                        inspectedECSContexts[1 + index] = new ECSContext {EntityManager = currentEntityManager, Entity = entitySelectionProxy.Entity, ComponentType = types[index]};
                    }
                    
                    types.Dispose();
                }
#endif                
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

            if (inspectedECSContexts == null)
            {
                inspectedECSContexts = new ECSContext[inspectedContexts.Length];
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
                
                var inspectedContext = inspectedContexts[i];
                var inspectedECSContext = inspectedECSContexts[i];

                bool? activeOrEnabled = inspectedContext switch
                {
                    GameObject gameObject => gameObject.activeSelf,
                    Behaviour behaviour => behaviour.enabled,
                    _ => null
                };
                
                if (typesHidden.All(row => row.Key != type))
                {
                    typesHidden.Add(new KeyValuePair<Type, bool>(type, false));
                }

                int index = typesHidden.FindIndex(row => row.Key == type);

                string name;
                if (inspectedContexts[0] != null)
                {
                    if (activeOrEnabled.HasValue)
                    {
                        name = "              " + type.Name;
                    }
                    else
                    {
                        name = "       " + type.Name;
                    }

                    if (i == 0 && inspectedContexts[i] is Object unityObject)
                    {
                        name += $" ({unityObject.name})";
                    }
                }
                else
                {
                    name = type.Name + " (Class)";
                }

                GUIContent content = new GUIContent(name, $"{type.FullName}, {type.Assembly.FullName}");
                
                Rect foldoutRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.foldoutHeader);
                
                Rect toggleRect = foldoutRect;
                toggleRect.xMin += 36;
                toggleRect.width = 20;
                
                Rect iconRect = foldoutRect;
                iconRect.xMin += 16;
                iconRect.yMin += 1;
                iconRect.height = iconRect.width = 16;
                
                // Have to do this before BeginFoldoutHeaderGroup otherwise it'll consume the mouse down event
                if (activeOrEnabled.HasValue && SidekickEditorGUI.DetectClickInRect(toggleRect))
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

                bool foldout = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, !typesHidden[index].Value, content, EditorStyles.foldoutHeader, rect => ClassUtilities.GetMenu(inspectedContext, inspectedECSContext).DropDown(rect));

                Texture icon = SidekickEditorGUI.GetIcon(inspectedContexts[i], type);
                if (icon != null)
                {
                    GUI.DrawTexture(iconRect, icon);
                }

                // Right click context menu
                if (SidekickEditorGUI.DetectClickInRect(foldoutRect, 1))
                {
                    ClassUtilities.GetMenu(inspectedContext, inspectedECSContext).DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                }

                if (activeOrEnabled.HasValue)
                {
                    EditorGUI.Toggle(toggleRect, activeOrEnabled.Value);
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
                            fieldPane.DrawFields(inspectedTypes[i], inspectedContexts[i], inspectedECSContext, searchTerm, fields);
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

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if(inspectedTypes[0] == typeof(GameObject)
#if ECS_EXISTS
               || inspectedTypes[0] == typeof(EntitySelectionProxy)
#endif
               )
            {
                bool pressed = GUILayout.Button("Add Component", GUILayout.Width(230), GUILayout.Height(24));
                var popupRect2 = GUILayoutUtility.GetLastRect();
                popupRect2.width = EditorGUIUtility.currentViewWidth;
                if (pressed)
                {
                    if (inspectedTypes[0] == typeof(GameObject))
                    {
                        TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), type =>
                        {
                            
                            ((GameObject) inspectedContexts[0]).AddComponent(type);
                        }, new[] {typeof(Component)});
                        dropdown.Show(popupRect2);
                    }
#if ECS_EXISTS
                    else if (inspectedTypes[0] == typeof(EntitySelectionProxy))
                    {
                        TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), type =>
                        {
                            inspectedECSContexts[0].EntityManager.AddComponent(inspectedECSContexts[0].Entity, ComponentType.ReadWrite(type));
                        }, null, new []{typeof(IComponentData)});
                        dropdown.Show(popupRect2);
                    }
#endif                    
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

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
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            HistoryButton(backContent, backStack, forwardStack);
            HistoryButton(forwardContent, forwardStack, backStack);

            GUI.enabled = true;
            
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