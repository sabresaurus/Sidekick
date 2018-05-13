using UnityEngine;
using System.Collections;
using System;

namespace Sabresaurus.Sidekick
{
    public static class CollectionUtility
    {
        public static void Resize(ref IList list, Type elementType, int newSize)
        {
            int oldSize = list.Count;
            if (list.IsFixedSize)
            {
                Debug.LogError("Resizing fixed arrays is not currently implemented - see https://github.com/sabresaurus/Sidekick/issues/24");
            }
            else
            {
                if (newSize > oldSize)
                {
                    object objectToAdd;
                    if (oldSize >= 1)
                    {
                        objectToAdd = list[oldSize - 1];
                    }
                    else
                    {
                        objectToAdd = TypeUtility.GetDefaultValue(elementType);
                    }

                    int toAdd = newSize - oldSize;
                    for (int i = 0; i < toAdd; i++)
                    {
                        list.Add(objectToAdd);
                    }
                }
                else if (newSize < oldSize)
                {
                    int toRemove = oldSize - newSize;
                    for (int i = 0; i < toRemove; i++)
                    {
                        list.RemoveAt(oldSize - i - 1);
                    }
                }
            }
        }

        public static object ConvertArrayOrList(WrappedVariable wrappedVariable, Type type)
        {
            // TODO: Investigate if this array copying could be simplified
            IList sourceList = (IList)wrappedVariable.Value;
            int count = sourceList.Count;
            if (type.IsArray)
            {
                // Copying to an array
                object newArray = Activator.CreateInstance(type, new object[] { count });
                for (int i = 0; i < count; i++)
                {
                    ((Array)newArray).SetValue(sourceList[i], i);
                }
                return newArray;
            }
            else
            {
                object newList = Activator.CreateInstance(type, new object[] { 0 });
                for (int i = 0; i < count; i++)
                {
                    ((IList)newList).Add(sourceList[i]);
                }
                return newList;
            }
        }
    }
}