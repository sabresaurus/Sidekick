using System.Collections;
using System;

namespace Sabresaurus.Sidekick
{
    public static class CollectionUtility
    {
        public static void Resize(ref IList list, object defaultElementValue, int newSize)
        {
            int oldSize = list.Count;

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
                    objectToAdd = defaultElementValue;
                }
            }

            if (list.IsFixedSize)
            {
                Array newArray = Array.CreateInstance(typeof(object), newSize);
                Array.Copy((Array)list, newArray, Math.Min(oldSize, newArray.Length));

                if (newSize > oldSize)
                {
                    int toAdd = newSize - oldSize;
                    for (int i = 0; i < toAdd; i++)
                    {
                        newArray.SetValue(objectToAdd, oldSize + i);
                    }
                }

                list = newArray;
            }
            else
            {
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
            }
        }
    }
}