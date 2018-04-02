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

        GUIContent objectContent;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if(type != null)
            {
				objectContent = EditorGUIUtility.ObjectContent(null, type);
                if(objectContent.image != null)
                {
					// Cache it so we don't need to do expensive lookups next time
					cachedIcons[typeName] = objectContent.image;
					return objectContent.image;
                }
            }
        }
        objectContent = EditorGUIUtility.ObjectContent(null, typeof(MonoScript));
        // Cache it so we don't need to do expensive lookups next time
        cachedIcons[typeName] = objectContent.image;
        return objectContent.image;
    }

}
