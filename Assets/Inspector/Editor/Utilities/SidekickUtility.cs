using UnityEngine;
using UnityEditor;
using System;
//using System.Linq;
using System.Text;

namespace Sabresaurus.Sidekick
{
	public static class SidekickUtility
	{
		/// <summary>
		/// Find the path to where InspectorSidekick is in the project
		/// </summary>
		/// <returns>The installed path of InspectorSidekick.</returns>
		public static string GetInstallPath()
		{
			// Find all the scripts with CSGModel in their name
			string[] guids = AssetDatabase.FindAssets("InspectorSidekick t:Script");

			foreach (string guid in guids) 
			{
				// Find the path of the file
				string path = AssetDatabase.GUIDToAssetPath(guid);

				string suffix = "Editor/InspectorSidekick.cs";
				// If it is the target file, i.e. CSGModel.cs not CSGModelInspector
				if(path.EndsWith(suffix))
				{
					// Remove the suffix, to get for example Assets/SabreCSG
					path = path.Remove(path.Length-suffix.Length, suffix.Length);

					return path;
				}
			}

			// None matched
			return string.Empty;
		}

		public static T EnumToolbar<T>(T value, GUIStyle style = null, params GUILayoutOption[] options) where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("EnumToolbar must be passed an enum");
			}

			if(style == null)
			{
				style = GUI.skin.button;
			}

			string[] names = Enum.GetNames(typeof(T));
			int oldValue = Array.IndexOf(names, value.ToString());

			for (int i = 0; i < names.Length; i++) 
			{
				names[i] = ParseDisplayString(names[i]);
			}
			int newValue = GUILayout.Toolbar(oldValue, names, style, options);

			return (T)Enum.ToObject(typeof(T), newValue);
		}

		public static string ParseDisplayString(string input)
		{
			StringBuilder stringBuilder = new StringBuilder();

			for (int i = 0; i < input.Length; i++) 
			{
				char currentChar = input[i];
				// If we've just started an uppercase (not at the start of the string) then prepend a space
				if(i > 0 && Char.IsUpper(currentChar) && !Char.IsUpper(input[i-1]))
				{
					stringBuilder.Append(' ');
				}
				// Make sure the first character is capitalised
				if(i == 0)
				{
					currentChar = Char.ToUpper(currentChar);
				}
				stringBuilder.Append(currentChar);
			}

			return stringBuilder.ToString();
		}

		public static bool EventsMatch(Event event1, Event event2, bool ignoreShift, bool ignoreFunction)
		{
			EventModifiers modifiers1 = event1.modifiers;
			EventModifiers modifiers2 = event2.modifiers;

			// Ignore capslock from either modifier
			modifiers1 &= (~EventModifiers.CapsLock);
			modifiers2 &= (~EventModifiers.CapsLock);

			if(ignoreShift)
			{
				// Ignore shift from either modifier
				modifiers1 &= (~EventModifiers.Shift);
				modifiers2 &= (~EventModifiers.Shift);
			}

			// If key code and modifier match
			if(event1.keyCode == event2.keyCode
				&& (modifiers1 == modifiers2))
			{
				return true;
			}

			return false;
		}
	}
}