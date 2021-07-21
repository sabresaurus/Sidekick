using System.Collections;
using System;

namespace Sabresaurus.Sidekick
{
    public static class CollectionUtility
    {
        public static IList Resize(IList list, bool isArray, Type fieldType, Type elementType, int newSize)
        {
            var oldSize = list != null ? list.Count : 0;

            // Unity's default behaviour when expanding a collection is to use the last element as the new element value
            object objectToAdd = null;
            if (newSize > oldSize)
            {
                if (oldSize >= 1)
                {
                    objectToAdd = list[oldSize - 1];
                }
                else
                {
                    objectToAdd = Activator.CreateInstance(elementType);
                }
            }

            if (isArray)
            {
                Array newArray = Array.CreateInstance(elementType, newSize);
                if (oldSize != 0)
                {
                    Array.Copy((Array)list, newArray, Math.Min(oldSize, newArray.Length));
                }

                if (newSize > oldSize)
                {
                    int toAdd = newSize - oldSize;
                    for (int i = 0; i < toAdd; i++)
                    {
                        newArray.SetValue(objectToAdd, oldSize + i);
                    }
                }

                return newArray;
            }
            else
            {
                if (oldSize == 0)
                {
                    list = (IList) Activator.CreateInstance(fieldType);
                }
                if (newSize > oldSize)
                {
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

                return list;
            }
        }
    }
}