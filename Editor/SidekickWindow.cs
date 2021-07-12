using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class SidekickWindow : EditorWindow
    {
        enum InspectorMode
        {
            Fields,
            Props,
            Methods,
            Events,
            Misc
        };

        SidekickSettings settings = new SidekickSettings();

        FieldPane fieldPane = new FieldPane();
        PropertyPane propertyPane = new PropertyPane();
        MethodPane methodPane = new MethodPane();
        EventPane eventPane = new EventPane();
        UtilityPane utilityPane = new UtilityPane();

        static SidekickWindow current;

        private SearchField searchField;

        object selectionOverride = null;
        private Type typeToDisplay = null;

        List<object> backStack = new List<object>();
        List<object> forwardStack = new List<object>();

        PersistentData persistentData = new PersistentData();
        InspectorMode mode = InspectorMode.Fields;
        Vector2 scrollPosition;

        List<KeyValuePair<Type, bool>> typesHidden = new List<KeyValuePair<Type, bool>>()
        {
            new KeyValuePair<Type, bool>(typeof(Transform), true),
            new KeyValuePair<Type, bool>(typeof(GameObject), true),
        };


        public static SidekickWindow Current => current;

        public PersistentData PersistentData => persistentData;

        public SidekickSettings Settings => settings;

        private object ActiveSelection
        {
            get
            {
                object selectedObject = Selection.activeObject;
                if (selectionOverride != null)
                {
                    selectedObject = selectionOverride;
                }

                return selectedObject;
            }
        }

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

            DrawSelectionInfo();

            var selectTypeButtonLabel = new GUIContent("Select Type From Assembly");
            var selectTypeButtonRect = GUILayoutUtility.GetRect(selectTypeButtonLabel, EditorStyles.miniButton);
            if (GUI.Button(selectTypeButtonRect, selectTypeButtonLabel, EditorStyles.miniButton))
            {
                TypeSelectDropdown dropdown = new TypeSelectDropdown(new AdvancedDropdownState(), type => typeToDisplay = type);
                dropdown.Show(selectTypeButtonRect);
            }

            var selectObjectButtonLabel = new GUIContent("Select Loaded Unity Object");
            var selectObjectButtonRect = GUILayoutUtility.GetRect(selectObjectButtonLabel, EditorStyles.miniButton);
            if (GUI.Button(selectObjectButtonRect, selectObjectButtonLabel, EditorStyles.miniButton))
            {
                UnityObjectSelectDropdown dropdown = new UnityObjectSelectDropdown(new AdvancedDropdownState(), window => SetSelection(window, true));
                dropdown.Show(selectObjectButtonRect);
            }

            object selectedObject = ActiveSelection;

            if (selectedObject == null && typeToDisplay == null)
            {
                GUILayout.FlexibleSpace();
                GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel) {alignment = TextAnchor.MiddleCenter};
                GUILayout.Label("No object selected.\n\nSelect something in Unity or use one of the selection helper buttons.", style);
                GUILayout.FlexibleSpace();
                return;
            }

            if (selectedObject is GameObject selectedGameObject)
            {
                List<object> components = selectedGameObject.GetComponents<Component>().Cast<object>().ToList();
                components.RemoveAll(item => item == null);
                components.Insert(0, selectedGameObject);
                inspectedContexts = components.ToArray();
            }
            else
            {
                inspectedContexts = new[] {selectedObject};
            }

            inspectedTypes = inspectedContexts.Select(x => x.GetType()).ToArray();

            if (typeToDisplay != null)
            {
                inspectedTypes = new[] {typeToDisplay};

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
                GUIContent content = new GUIContent(type.Name, objectContent.image);

                bool? activeOrEnabled = inspectedContexts[i] switch
                {
                    GameObject gameObject => gameObject.activeSelf,
                    Behaviour behaviour => behaviour.enabled,
                    _ => null
                };

                bool toggled = SidekickEditorGUI.DrawHeaderWithFoldout(content, !typesHidden[index].Value, ref activeOrEnabled);

                if (toggled)
                {
                    typesHidden[index] = new KeyValuePair<Type, bool>(type, !typesHidden[index].Value);
                }

                if (!typesHidden[index].Value)
                {
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
                            SidekickEditorGUI.DrawHeader2(new GUIContent(": " + typeScope.Name));
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
                        else if (mode == InspectorMode.Misc)
                        {
                            utilityPane.Draw(inspectedTypes[i], inspectedContexts[i], typeScope);
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

            //			test += currentFrameDelta;
            //			Color color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(test, 1f));
            //			GUI.backgroundColor = color;
            //			GUILayout.Button(Mathf.PingPong(test, 1f).ToString());//test.ToString());

            //			if(AnimationHelper.AnimationActive)
            {
                // Cause repaint on next frame
                Repaint();
                if (Event.current.type == EventType.Repaint)
                {
                    //					AnimationHelper.ClearAnimationActive();
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.enabled = (backStack.Count > 0);
            if (GUILayout.Button(SidekickEditorGUI.BackIcon, EditorStyles.toolbarButton)
                || (Event.current.type == EventType.MouseDown && Event.current.button == 3)
                || (Event.current.type == EventType.KeyDown && SidekickUtility.EventsMatch(Event.current, Event.KeyboardEvent("Backspace"), false, true)))
            {
                object backStackLast = backStack.Last();
                backStack.RemoveAt(backStack.Count - 1);
                forwardStack.Add(ActiveSelection);
                SetSelection(backStackLast, false);
            }

            GUI.enabled = (forwardStack.Count > 0);
            if (GUILayout.Button(SidekickEditorGUI.ForwardIcon, EditorStyles.toolbarButton)
                || (Event.current.type == EventType.MouseDown && Event.current.button == 4)
                || (Event.current.type == EventType.KeyDown && SidekickUtility.EventsMatch(Event.current, Event.KeyboardEvent("#Backspace"), false, true)))
            {
                object forwardStackLast = forwardStack.Last();
                forwardStack.RemoveAt(forwardStack.Count - 1);
                backStack.Add(ActiveSelection);
                SetSelection(forwardStackLast, false);
            }

            GUI.enabled = true;

            bool locked = (selectionOverride != null);

            var lockIcon = locked ? SidekickEditorGUI.LockIconOn : SidekickEditorGUI.LockIconOff;
            if (GUILayout.Button(lockIcon, EditorStyles.toolbarButton))
            {
                selectionOverride = selectionOverride == null ? ActiveSelection : null;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionInfo()
        {
            if (typeToDisplay != null)
            {
                GUILayout.Label("Selection: Assembly Type");
            }
            else if (selectionOverride != null)
            {
                GUILayout.Label("Selection: Custom");
            }
            else
            {
                GUILayout.Label("Selection: Editor Selection");
            }
        }

        public void SetSelection(object newSelection, bool updateStack)
        {
            if (updateStack)
            {
                backStack.Add(ActiveSelection);
                forwardStack.Clear();
            }

            if (newSelection is UnityEngine.Object unityObject)
            {
                Selection.activeObject = unityObject;
                selectionOverride = null;
            }
            else
            {
                selectionOverride = newSelection;
            }
        }

        void OnSelectionChange()
        {
            if (selectionOverride != null)
            {
                return;
            }
            selectionOverride = null;
            typeToDisplay = null;

            Repaint();
        }
    }
}