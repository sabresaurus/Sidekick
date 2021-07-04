using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class OldInspectorSidekick : EditorWindow
    {
        enum InspectedType { Selection, AssemblyClass };
        enum InspectorMode { Fields, Props, Methods, Events, Misc };

        OldSettings settings = new OldSettings();

        FieldPane fieldPane = new FieldPane();
        PropertyPane propertyPane = new PropertyPane();
        MethodPane methodPane = new MethodPane();
        EventPane eventPane = new EventPane();
        UtilityPane utilityPane = new UtilityPane();

        static OldInspectorSidekick current;

        private SearchField searchField;

        object selectionOverride = null;

        List<object> backStack = new List<object>();
        List<object> forwardStack = new List<object>();

        PersistentData persistentData = new PersistentData();
        InspectedType inspectedType = InspectedType.Selection;
        InspectorMode mode = InspectorMode.Fields;
        Vector2 scrollPosition;

        List<KeyValuePair<Type, bool>> typesHidden = new List<KeyValuePair<Type, bool>>()
        {
            new KeyValuePair<Type, bool>(typeof(Transform), true),
            new KeyValuePair<Type, bool>(typeof(GameObject), true),
        };

        int selectedAssemblyIndex = 0;
        int selectedTypeIndex = 0;
        string[] assemblyNames;
        List<Assembly> assemblies = new List<Assembly>();
        Dictionary<Assembly, List<Type>> assemblyTypes = new Dictionary<Assembly, List<Type>>();


        public static OldInspectorSidekick Current
        {
            get
            {
                return current;
            }
        }

        public PersistentData PersistentData
        {
            get
            {
                return persistentData;
            }
        }

        public OldSettings Settings
        {
            get
            {
                return settings;
            }
        }

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
            OldInspectorSidekick sidekick = EditorWindow.GetWindow<OldInspectorSidekick>();
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

        void ConstructAssembliesAndTypes()
        {
            assemblies.Clear();
            assemblies.Add(Assembly.GetAssembly(typeof(UnityEditor.Editor)));
            assemblies.Add(Assembly.GetAssembly(typeof(UnityEngine.Application)));
            assemblies.Add(Assembly.GetAssembly(typeof(UnityEditor.Graphs.Edge)));
            //			assemblies.Add(Assembly.GetAssembly(typeof(InspectorSidekick)));

            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in allAssemblies)
            {
                // Walk through all the types in the main assembly
                if (assembly.FullName.StartsWith("Assembly-CSharp"))
                {
                    assemblies.Add(assembly);
                }
            }

            //assemblies.Add(Assembly.GetAssembly(typeof(EditorHelper)));
            assemblyNames = new string[assemblies.Count];

            for (int i = 0; i < assemblies.Count; i++)
            {
                assemblyNames[i] = assemblies[i].GetName().Name;

                assemblyTypes.Add(assemblies[i], assemblies[i].GetTypes().ToList());
            }

            for (int i = 0; i < assemblies.Count; i++)
            {
                for (int j = 0; j < assemblyTypes.Count; j++)
                {
                    Type type = assemblyTypes[assemblies[i]][j];
                    if (!type.IsClass || type.IsAbstract)
                    {
                        assemblyTypes[assemblies[i]].RemoveAt(j);
                        j--;
                    }
                }

                assemblyTypes[assemblies[i]].Sort((x, y) => x.Name.CompareTo(y.Name));
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
            // Make sure we have a valid set of assemblies
            if (assemblies == null || assemblies.Count == 0)
            {
                ConstructAssembliesAndTypes();
            }

            Rect windowRect = position;

            Type[] inspectedTypes = null;
            object[] inspectedContexts = null;

            GUILayout.Space(9);

            inspectedType = SidekickUtility.EnumToolbar(inspectedType, "LargeButton");//, GUILayout.Width(windowRect.width - 60));

            if (inspectedType == InspectedType.Selection)
            {
                object selectedObject = ActiveSelection;

                if (selectedObject == null)
                {
                    GUILayout.Space(windowRect.height / 2 - 40);
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("No object selected", style);
                    return;
                }

                if (selectedObject is GameObject)
                {
                    List<object> components = ((GameObject)selectedObject).GetComponents<Component>().Cast<object>().ToList();
                    components.RemoveAll(item => item == null);
                    components.Insert(0, selectedObject);
                    inspectedContexts = components.ToArray();
                }
                else
                {
                    inspectedContexts = new object[] { selectedObject };
                }
                inspectedTypes = inspectedContexts.Select(x => x.GetType()).ToArray();
            }
            else if (inspectedType == InspectedType.AssemblyClass)
            {
                int newSelectedAssemblyIndex = EditorGUILayout.Popup(selectedAssemblyIndex, assemblyNames);
                if (newSelectedAssemblyIndex != selectedAssemblyIndex)
                {
                    selectedTypeIndex = 0;
                    selectedAssemblyIndex = newSelectedAssemblyIndex;
                }

                Assembly activeAssembly = assemblies[selectedAssemblyIndex];
                List<Type> types = assemblyTypes[activeAssembly];
                string[] typeNames = new string[types.Count];
                for (int i = 0; i < types.Count; i++)
                {
                    typeNames[i] = types[i].FullName;
                }
                selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeNames);

                inspectedTypes = new Type[] { assemblyTypes[activeAssembly][selectedTypeIndex] };
                inspectedContexts = new Type[] { null };
            }
            else
            {
                throw new NotImplementedException("Unhandled InspectedType");
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

                if(toggled)
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
                            utilityPane.Draw(inspectedTypes[i], inspectedContexts[i]);
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

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = (backStack.Count > 0);
            if (GUILayout.Button("<-")
                || (Event.current.type == EventType.MouseDown && Event.current.button == 3)
                || (Event.current.type == EventType.KeyDown && SidekickUtility.EventsMatch(Event.current, Event.KeyboardEvent("Backspace"), false, true)))
            {
                object backStackLast = backStack.Last();
                backStack.RemoveAt(backStack.Count - 1);
                forwardStack.Add(ActiveSelection);
                SetSelection(backStackLast, false);
            }
            GUI.enabled = (forwardStack.Count > 0);
            if (GUILayout.Button("->")
                || (Event.current.type == EventType.MouseDown && Event.current.button == 4)
                || (Event.current.type == EventType.KeyDown && SidekickUtility.EventsMatch(Event.current, Event.KeyboardEvent("#Backspace"), false, true)))
            {
                object forwardStackLast = forwardStack.Last();
                forwardStack.RemoveAt(forwardStack.Count - 1);
                backStack.Add(ActiveSelection);
                SetSelection(forwardStackLast, false);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Pin"))
            {
                selectionOverride = ActiveSelection;
            }

            EditorGUILayout.EndHorizontal();
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

        public void SetSelection(object newSelection, bool updateStack)
        {
            if (updateStack)
            {
                backStack.Add(ActiveSelection);
                forwardStack.Clear();
            }

            if (newSelection is UnityEngine.Object)
            {
                Selection.activeObject = (UnityEngine.Object)newSelection;
                selectionOverride = null;
            }
            else
            {
                selectionOverride = newSelection;
            }
        }

        void OnSelectionChange()
        {
            //			selectionOverride = null;
            Repaint();
        }
    }
}