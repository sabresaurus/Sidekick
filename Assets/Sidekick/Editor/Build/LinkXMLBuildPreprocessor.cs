using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

class LinkXMLBuildPreprocessor : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildTarget target, string path)
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