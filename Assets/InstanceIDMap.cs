using UnityEngine;
using System.Collections.Generic;

public static class InstanceIDMap
{
    static Dictionary<int, Object> map = new Dictionary<int, Object>();

    public static void AddObject(Object targetObject)
    {
        map[targetObject.GetInstanceID()] = targetObject;
    }

    public static void AddObjects(List<Object> objects)
    {
        foreach (Object targetObject in objects)
        {
            map[targetObject.GetInstanceID()] = targetObject;
        }
    }

    public static Object GetObjectFromInstanceID(int instanceID)
    {
        if (map.ContainsKey(instanceID))
        {
            return map[instanceID];
        }
        else
        {
            return null;
        }
    }
}