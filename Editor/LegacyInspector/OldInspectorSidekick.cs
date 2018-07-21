using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
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

        public object ActiveSelection
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

#if SIDEKICK_DEBUG
        [MenuItem("Tools/Old Sidekick")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            OldInspectorSidekick sidekick = EditorWindow.GetWindow<OldInspectorSidekick>();
            sidekick.UpdateTitleContent();
        }
#endif

        void OnEnable()
        {
            UpdateTitleContent();

            minSize = new Vector2(260, 100);
        }

        void UpdateTitleContent()
        {
            titleContent = new GUIContent("Old Sidekick");
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
            EditorGUILayout.BeginHorizontal();
            GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField");
            GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");
            GUIStyle noCancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty");

            GUILayout.Space(10);
            settings.SearchTerm = EditorGUILayout.TextField(settings.SearchTerm, searchStyle);
            if (!string.IsNullOrEmpty(settings.SearchTerm))
            {
                if (GUILayout.Button("", cancelStyle))
                {
                    settings.SearchTerm = "";
                    GUIUtility.hotControl = 0;
                    EditorGUIUtility.editingTextField = false;
                }
            }
            else
            {
                GUILayout.Button("", noCancelStyle);
            }
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            mode = SidekickUtility.EnumToolbar(mode);
            //			mode = SabreGUILayout.DrawEnumGrid(mode);

            GUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < inspectedTypes.Length; i++)
            {
                Type type = inspectedTypes[i];
                if (!typesHidden.Any(row => row.Key == type))// ContainsKey(component))
                {
                    typesHidden.Add(new KeyValuePair<Type, bool>(type, false));
                }


                int index = typesHidden.FindIndex(row => row.Key == type);

                GUIStyle style = new GUIStyle(EditorStyles.foldout);
                style.fontStyle = FontStyle.Bold;
                //				Texture2D icon = AssetPreview.GetMiniTypeThumbnail(type);
                GUIContent objectContent = EditorGUIUtility.ObjectContent(inspectedContexts[i] as UnityEngine.Object, type);
                Texture2D icon = objectContent.image as Texture2D;
                GUIContent content = new GUIContent(type.Name, icon);

                bool newValue = !EditorGUILayout.Foldout(!typesHidden[index].Value, content, style);

                if (newValue != typesHidden[index].Value)
                {
                    typesHidden[index] = new KeyValuePair<Type, bool>(type, newValue);
                }
                if (!typesHidden[index].Value)
                {
                    EditorGUI.indentLevel = 1;

                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    if (!settings.IncludeInherited)
                    {
                        bindingFlags |= BindingFlags.DeclaredOnly;
                    }

                    FieldInfo[] fields = type.GetFields(bindingFlags);
                    PropertyInfo[] properties = type.GetProperties(bindingFlags);
                    MethodInfo[] methods = type.GetMethods(bindingFlags);

                    // Hide methods and backing fields that have been generated for properties
                    if (settings.HideAutoGenerated)
                    {
                        List<MethodInfo> methodList = new List<MethodInfo>(methods.Length);

                        for (int j = 0; j < methods.Length; j++)
                        {
                            if (!TypeUtility.IsPropertyMethod(methods[j], type))
                            {
                                methodList.Add(methods[j]);
                            }
                        }
                        methods = methodList.ToArray();

                        List<FieldInfo> fieldList = new List<FieldInfo>(fields.Length);

                        for (int j = 0; j < fields.Length; j++)
                        {
                            if (!TypeUtility.IsBackingField(fields[j], type))
                            {
                                fieldList.Add(fields[j]);
                            }
                        }
                        fields = fieldList.ToArray();
                    }


                    FieldInfo[] events = type.GetFields(bindingFlags);

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

                    EditorGUI.indentLevel = 0;
                }

                Rect rect = GUILayoutUtility.GetRect(new GUIContent(), GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                rect.xMin -= 10;
                rect.xMax += 10;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndScrollView();

            if (mode == InspectorMode.Methods)
            {
                methodPane.PostDraw();
            }

            settings.RotationsAsEuler = EditorGUILayout.Toggle("Rotations as euler", settings.RotationsAsEuler);
            settings.IncludeInherited = EditorGUILayout.Toggle("Include inherited", settings.IncludeInherited);
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