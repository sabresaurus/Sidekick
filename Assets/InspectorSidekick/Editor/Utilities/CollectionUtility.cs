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
			if(list.IsFixedSize)
			{
			}
			else
			{
				if(newSize > oldSize)
				{
					object objectToAdd;
					if(oldSize >= 1)
					{
						objectToAdd = list[oldSize-1];
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
				else if(newSize < oldSize)
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