using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
	public class EventPane : BasePane
	{
//		string methodOutput = "";

		public void DrawEvents(Type componentType, object component, string searchTerm, FieldInfo[] events)//EventInfo[] events)
		{
			foreach (FieldInfo eventInfo in events)
			{
				if(!SearchMatches(searchTerm, eventInfo.Name))
				{
					// Does not match search term, skip it
					continue;
				}
				
				if(eventInfo.FieldType.IsSubclassOf(typeof(Delegate)))
				{
					Delegate targetDelegate = (Delegate)eventInfo.GetValue(component);
					if(GUILayout.Button(eventInfo.Name))
					{
						if(targetDelegate != null)
						{
							targetDelegate.DynamicInvoke();
						}
					}
				}
			}
		}
	}
}
