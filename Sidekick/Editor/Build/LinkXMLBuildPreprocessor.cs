using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
class LinkXMLBuildPreprocessor : IPreprocessBuildWithReport
#else
class LinkXMLBuildPreprocessor : IPreprocessBuild
#endif
{
    public int callbackOrder { get { return 0; } }

#if UNITY_2018_1_OR_NEWER
    public void OnPreprocessBuild(BuildReport report)
#else
    public void OnPreprocessBuild(BuildTarget target, string path)
#endif
    {
        if (Debug.isDebugBuild)
        {
            Debug.Log("Preprocessing link.xml file");
            List<Type> typesToProtect = new List<Type>();// LinkXMLFactory.GetUnityComponentTypes();
            typesToProtect.AddRange(LinkXMLFactory.DEFAULT_TYPES);

            // TextMeshPro may be in the project and doesn't have a fixed assembly name, scan for it
            Type textMeshProBaseType = FindTypeOrDefault("TMPro.TMP_Text");
            Type textMeshProUGUIType = FindTypeOrDefault("TMPro.TextMeshProUGUI");

            if (textMeshProBaseType != null)
            {
                typesToProtect.Add(textMeshProBaseType);
            }
            if (textMeshProUGUIType != null)
            {
                typesToProtect.Add(textMeshProUGUIType);
            }

            LinkXMLFactory.Generate(typesToProtect);
        }
        else
        {
            LinkXMLFactory.Generate(new List<Type>());
        }
    }

    static Type FindTypeOrDefault(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.FullName == typeName)
                {
                    return type;
                }
            }
        }

        // None matched
        return null;
    }
}