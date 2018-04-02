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

		public void DrawEvents(Type componentType, object component, FieldInfo[] events)//EventInfo[] events)
		{
			for (int j = 0; j < events.Length; j++)
			{
				FieldInfo eventInfo = events[j];
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


				/*
				//				object[] customAttributes = method.GetCustomAttributes(false);
				EditorGUILayout.BeginHorizontal();
				ParameterInfo[] parameters = method.GetParameters();

				if(GUILayout.Button(TypeUtility.NameForType(method.ReturnType) + " " + method.Name + " " + parameters.Length))
				{
					object[] arguments = null;
					if(parameters.Length > 0)
					{
						arguments = new object[parameters.Length];
						for (int i = 0; i < parameters.Length; i++) 
						{
							arguments[i] = GetDefaultValue(parameters[i].ParameterType);
						}
					}
					object returnedObject = method.Invoke(component, arguments);

					if(method.ReturnType == typeof(void))
					{
						methodOutput = "Method fired, no return type";
					}
					else
					{
						methodOutput = "Method returned:\n" + returnedObject.ToString();
					}
				}
				EditorGUILayout.EndHorizontal();
				*/
			}
		}
	}
}
