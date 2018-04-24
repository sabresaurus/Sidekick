using UnityEditor;
using UnityEngine;

public static class SidekickEditorGUI  {
	public static bool DrawHeader(GUIContent label, SerializedProperty activeField = null, bool active = true)
	{
		Rect contentRect = GUILayoutUtility.GetRect(1f, 17f);
		Rect labelRect = contentRect;
		labelRect.xMin += 16f;
		labelRect.xMax -= 20f;
		Rect toggleRect = contentRect;
		toggleRect.y += 2f;
		toggleRect.width = 13f;
		toggleRect.height = 13f;
		contentRect.xMin = 0.0f;
		contentRect.xMax = Screen.width;
		contentRect.width += 4f;
		DrawHeaderBackground(contentRect);
		using (new EditorGUI.DisabledScope(!active))
			EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);
		if (activeField != null)
		{
			activeField.serializedObject.Update();
			activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, smallTickbox);
			activeField.serializedObject.ApplyModifiedProperties();
		}
		else
			labelRect.xMin = 0;

		Event current = Event.current;
		if (current.type == EventType.MouseDown)
		{
			if (labelRect.Contains(current.mousePosition))
			{
				if (current.button == 0)
				{
					current.Use();
					return true;
				}
			}
		}

		return false;
	}
	
	public static bool DrawHeaderWithFoldout(GUIContent label, bool expanded)
	{
		bool ret = DrawHeader(label);
		if (Event.current.type == EventType.Repaint)
		{
			//Only draw the Foldout - don't use it as a button or get focus
			Rect r = GUILayoutUtility.GetLastRect();
			r.x += 3;
			r.x += EditorGUI.indentLevel * 15;
			r.y += 1.5f;
			GUI.enabled = false;
			EditorStyles.foldout.Draw(r, GUIContent.none, -1, expanded);
			GUI.enabled = true;
		}
		return ret;
	}

	public static void DrawSplitter()
	{
		Rect rect = GUILayoutUtility.GetRect(1f, 1f);
		rect.xMin = 0.0f;
		rect.width += 4f;
		if (Event.current.type != EventType.Repaint)
			return;
		EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f));
	}

	public static void DrawHeaderBackground(Rect r)
	{
		float color = !EditorGUIUtility.isProSkin ? 1f : 0.1f;
		EditorGUI.DrawRect(r, new Color(color, color, color, 0.2f));
	}
	
	private static GUIStyle _smallTickbox;
	public static GUIStyle smallTickbox
	{
		get { return _smallTickbox ?? (_smallTickbox = new GUIStyle("ShurikenCheckMark")); }
	}
}
