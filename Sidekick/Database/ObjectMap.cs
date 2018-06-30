using System;
using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    public static class ObjectMap
    {
        static Dictionary<Guid, object> map1 = new Dictionary<Guid, object>();
        static Dictionary<object, Guid> map2 = new Dictionary<object, Guid>();

        public static Guid AddOrGetObject(object targetObject)
        {
            if(!map2.ContainsKey(targetObject))
            {
                // Not contained, make it with a new Guid
                Guid guid = Guid.NewGuid();
                     
                map1[guid] = targetObject;
                map2[targetObject] = guid;
                return guid;
            }
            else
            {
                return map2[targetObject];
            }
        }

        public static void AddObjects(List<object> objects)
        {
            foreach (object targetObject in objects)
            {
                AddOrGetObject(targetObject);
            }
        }

        public static object GetObjectFromGUID(Guid guid)
        {
            if (map1.ContainsKey(guid))
            {
                return map1[guid];
            }
            else
            {
                return null;
            }
        }
    }
}