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
            Props,
            Methods,
            Events,
        }

        SidekickSettings settings = new SidekickSettings();

        FieldPane fieldPane = new FieldPane();
        PropertyPane propertyPane = new PropertyPane();
        MethodPane methodPane = new MethodPane();
        EventPane eventPane = new EventPane();

        static SidekickWindow current;

        private SearchField searchField;

        private readonly struct SelectionInfo : IEquatable<SelectionInfo>
        {
            // Note we need to be able to differentiate selecting a RuntimeType from wanting to see static methods on the type it represents 
            public readonly Type Type;
            public readonly object Object;

            public SelectionInfo(Type type)
            {
                Type = type;
                Object = null;
            }

            public SelectionInfo(object o)
            {
                Object = o;
                Type = null;
            }

            public bool IsEmpty => Type == null && Object == null;

            public bool Equals(SelectionInfo other)
            {
                return Type == other.Type && Equals(Object, other.Object);
            }

            public override bool Equals(object obj)
            {
                return obj is SelectionInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Object != null ? Object.GetHashCode() : 0);
                }
            }
        }

        SelectionInfo activeSelection = new SelectionInfo();

        private bool selectionLocked = false;

        List<SelectionInfo> backStack = new List<SelectionInfo>();
        List<SelectionInfo> forwardStack = new List<SelectionInfo>();

        PersistentData persistentData = new PersistentData();
        InspectorMode mode = InspectorMode.Fields;
        Vector2 scrollPosition;

        private bool suppressNextSelectionDetection = false;

        List<KeyValuePair<Type, bool>> typesHidden = new List<KeyValuePair<Type, bool>>()
        {
            new KeyValuePair<Type, bool>(typeof(Transform), true),
            new KeyValuePair<Type, bool>(typeof(GameObject), true),
        };


        public static SidekickWindow Current => current;

        public PersistentData PersistentData => persistentData;

        public SidekickSettings Settings => settings;

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
        }

        void UpdateTitleContent()
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

        void OnGUI()
        {
            // Frame rate tracking
            if (Event.current.type == EventType.Repaint)
            {
                AnimationHelper.UpdateTime();
            }

            current = this;

            DrawToolbar();

            Type[] inspectedTypes = null;
            object[] inspectedContexts = null;

            GUILayout.Space(9);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Selection Helpers");
            var popupRect = GUILayoutUtility.GetLastRect();
            popupRect.width = EditorGUIUtility.currentViewWidth;
            
            var selectTypeButtonLabel = new GUIContent("Type From Assembly");
            if (GUILayout.Button(selectTypeButtonLabel, EditorStyles.miniButton))
            {
                TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }

            var selectObjectButtonLabel = new GUIContent("Loaded Unity Object");
            if (GUILayout.Button(selectObjectButtonLabel, EditorStyles.miniButton))
            {
                UnityObjectSelectDropdown dropdown = new UnityObjectSelectDropdown(new AdvancedDropdownState(), SetSelection);
                dropdown.Show(popupRect);
            }
            EditorGUILayout.EndHorizontal();

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
            settings.SearchTerm = searchField.OnToolbarGUI(settings.SearchTerm);
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
                bool foldout = EditorGUILayout.BeginFoldoutHeaderGroup(!typesHidden[index].Value, content, null, rect => ClassUtilities.GetMenu(inspectedContext).DropDown(rect));
                Rect headerRect = GUILayoutUtility.GetLastRect();
                bool? activeOrEnabled = inspectedContexts[i] switch
                {
                    GameObject gameObject => gameObject.activeSelf,
                    Behaviour behaviour => behaviour.enabled,
                    _ => null
                };

                if (activeOrEnabled.HasValue)
                {
                    headerRect.xMin += 34;
                    headerRect.width = 20;

                    EditorGUI.BeginChangeCheck();
                    activeOrEnabled = EditorGUI.Toggle(headerRect, activeOrEnabled.Value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        switch (inspectedContexts[i])
                        {
                            case GameObject gameObject:
                                gameObject.SetActive(activeOrEnabled.Value);
                                break;
                            case Behaviour behaviour:
                                behaviour.enabled = activeOrEnabled.Value;
                                break;
                        }
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();

                typesHidden[index] = new KeyValuePair<Type, bool>(type, !foldout);

                if (!typesHidden[index].Value)
                {
                    SidekickEditorGUI.DrawSplitter(0.5f);

                    EditorGUI.indentLevel++;

                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

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
                        if (settings.HideAutoGenerated)
                        {
                            List<MethodInfo> methodList = new List<MethodInfo>(methods.Length);

                            for (int j = 0; j < methods.Length; j++)
                            {
                                if (!TypeUtility.IsPropertyMethod(methods[j], typeScope))
                                {
                                    methodList.Add(methods[j]);
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
                            fieldPane.DrawFields(inspectedTypes[i], inspectedContexts[i], fields);
                        }
                        else if (mode == InspectorMode.Props)
                        {
                            propertyPane.DrawProperties(inspectedTypes[i], inspectedContexts[i], properties);
                        }
                        else if (mode == InspectorMode.Methods)
                        {
                            methodPane.DrawMethods(inspectedTypes[i], inspectedContexts[i], methods);
                        }
                        else if (mode == InspectorMode.Events)
                        {
                            eventPane.DrawEvents(inspectedTypes[i], inspectedContexts[i], events);
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

            settings.RotationsAsEuler = EditorGUILayout.Toggle("Rotations as euler", settings.RotationsAsEuler);
            settings.HideAutoGenerated = EditorGUILayout.Toggle("Hide auto-generated", settings.HideAutoGenerated);
            settings.TreatEnumsAsInts = EditorGUILayout.Toggle("Enums as ints", settings.TreatEnumsAsInts);

            //if(AnimationHelper.AnimationActive)
            {
                // Cause repaint on next frame
                Repaint();
                if (Event.current.type == EventType.Repaint)
                {
                    //AnimationHelper.ClearAnimationActive();
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.enabled = (backStack.Count > 0);
            if (GUILayout.Button(new GUIContent(SidekickEditorGUI.BackIcon, "Back"), EditorStyles.toolbarButton)
                || (Event.current.type == EventType.MouseDown && Event.current.button == 3)
                || (Event.current.type == EventType.KeyDown && SidekickUtility.EventsMatch(Event.current, Event.KeyboardEvent("Backspace"), false, true)))
            {
                SelectionInfo backStackLast = backStack.Last();
                backStack.RemoveAt(backStack.Count - 1);
                forwardStack.Add(activeSelection);
                activeSelection = backStackLast;
                
                
                if (backStackLast.Object is UnityEngine.Object unityObject)
                {
                    suppressNextSelectionDetection = true;
                    Selection.activeObject = unityObject;
                }
            }

            GUI.enabled = (forwardStack.Count > 0);
            if (GUILayout.Button(new GUIContent(SidekickEditorGUI.ForwardIcon, "Forward"), EditorStyles.toolbarButton)
                || (Event.current.type == EventType.MouseDown && Event.current.button == 4)
                || (Event.current.type == EventType.KeyDown && SidekickUtility.EventsMatch(Event.current, Event.KeyboardEvent("#Backspace"), false, true)))
            {
                SelectionInfo forwardStackLast = forwardStack.Last();
                forwardStack.RemoveAt(forwardStack.Count - 1);
                backStack.Add(activeSelection);
                activeSelection = forwardStackLast;
                
                if (forwardStackLast.Object is UnityEngine.Object unityObject)
                {
                    suppressNextSelectionDetection = true;
                    Selection.activeObject = unityObject;
                }
            }

            GUI.enabled = true;

            var lockIcon = selectionLocked ? SidekickEditorGUI.LockIconOn : SidekickEditorGUI.LockIconOff;
            if (GUILayout.Button(lockIcon, EditorStyles.toolbarButton))
            {
                selectionLocked = !selectionLocked;
                if (selectionLocked == false && Selection.activeObject != null)
                {
                    SetSelection(Selection.activeObject);
                }
            }

            // Spacer
            GUILayout.Label("", EditorStyles.toolbarButton, GUILayout.Width(6));

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                // TODO: Implement settings in Preferences or Project Settings
            }

            EditorGUILayout.EndHorizontal();
        }

        
        public void SetSelection(object newSelection)
        {
            if (!activeSelection.IsEmpty && !backStack.LastOrDefault().Equals(activeSelection))
            {
                backStack.Add(activeSelection);
                if (backStack.Count > BACK_STACK_LIMIT)
                {
                    backStack.RemoveAt(0);
                }
            }
            forwardStack.Clear();

            activeSelection = new SelectionInfo(newSelection);

            if (newSelection is UnityEngine.Object unityObject)
            {
                Selection.activeObject = unityObject;
            }
        }

        public void SetSelection(Type newSelection)
        {
            if (!activeSelection.IsEmpty && !backStack.LastOrDefault().Equals(activeSelection))
            {
                backStack.Add(activeSelection);
                if (backStack.Count > BACK_STACK_LIMIT)
                {
                    backStack.RemoveAt(0);
                }
            }
            forwardStack.Clear();

            activeSelection = new SelectionInfo(newSelection);
        }

        void OnSelectionChange()
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