using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class IconLookup
{
    static Dictionary<string, Texture> cachedIcons = new Dictionary<string, Texture>();

    public static Texture GetIcon(string typeName)
    {
        if(cachedIcons.ContainsKey(typeName))
        {
            Texture cachedIcon = cachedIcons[typeName];
            if(cachedIcon != null)
            {
                return cachedIcon;
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if(type != null)
            {
                
                Debug.Log(type.FullName);
				GUIContent objectContent = EditorGUIUtility.ObjectContent(null, type);
				Texture2D icon = objectContent.image as Texture2D;
                // Cache it so we don't need to do expensive lookups next time
                cachedIcons[typeName] = icon;
				return icon;
            }
        }

        // None matched
        return null;
    }

}
