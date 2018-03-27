using UnityEngine;
using System;
using UnityEditor;

public class IconLookup
{
    public static Texture2D GetIcon(string typeName)
    {
        // TODO cache
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if(type != null)
            {
                
				//Debug.Log(assembly.FullName);
				GUIContent objectContent = EditorGUIUtility.ObjectContent(null, type);
				Texture2D icon = objectContent.image as Texture2D;
				return icon;
            }
        }

        // None matched
        return null;
    }

}
