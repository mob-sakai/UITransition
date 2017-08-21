using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Reflection;


/// <summary>
/// Curve preset selector.
/// </summary>
public class CurvePresetPopup : EditorWindow
{
	static readonly Type typeLib = Type.GetType("UnityEditor.CurvePresetLibrary, UnityEditor");
	static readonly MethodInfo miCount = typeLib.GetMethod("Count");
	static readonly MethodInfo miGetPreset = typeLib.GetMethod("GetPreset");
	static readonly MethodInfo miGetName = typeLib.GetMethod("GetName");
	static readonly MethodInfo miDraw = typeLib.GetMethod("Draw", new Type[]{ typeof(Rect), typeof(object) });

	static GUIStyle oddStyle;
	static GUIStyle evenStyle;

	UnityEngine.Object libraryAsset;

	Vector2 scrollPos;

	public Action<AnimationCurve> onSelect;

	public string libraryName;

	/// <summary>
	/// Open curve preset selector at specified rect. 
	/// </summary>
	/// <param name="buttonRect">Rect.</param>
	/// <param name="libraryName">Library name.</param>
	/// <param name="onSelect">On select curve callback.</param>
	public static void Dropdown(Rect buttonRect, string libraryName, Action<AnimationCurve> onSelect)
	{
		var selector = EditorWindow.CreateInstance<CurvePresetPopup>();// CreateInstance new CurvePresetPopup();
		selector.onSelect = onSelect;
		selector.libraryName = libraryName;

		Rect pos = new Rect(GUIUtility.GUIToScreenPoint(buttonRect.position), buttonRect.size);
		selector.ShowAsDropDown(pos, new Vector2(200, 400));
	}

	/// <summary>
	/// Callback for drawing GUI controls for the popup window.
	/// </summary>
	void OnGUI()
	{
		int count = (int)miCount.Invoke(libraryAsset, new object[0]);

		using(var sv = new EditorGUILayout.ScrollViewScope(scrollPos)){
			scrollPos = sv.scrollPosition;

			// Draw all curve presets.
			for (int i = 0; i < count; i++)
			{
				// Get preset curve and name.
				AnimationCurve presetCurve = (AnimationCurve)miGetPreset.Invoke(libraryAsset, new object[]{ i });
				string presetName = (string)miGetName.Invoke(libraryAsset, new object[]{ i });

				// On select a preset, close the window.
				Rect rect = EditorGUILayout.GetControlRect(false, 30);
				if (GUI.Button(rect, "", i % 2 == 0 ? evenStyle : oddStyle))
				{
					if (onSelect != null)
						onSelect(presetCurve);
					Close();
				}

				// Draw curve and label.
				miDraw.Invoke(libraryAsset, new object[]{ new Rect(rect.x, rect.y, 50, rect.height), presetCurve });
				GUI.Label(new Rect(rect.x + 55, rect.y + 8, rect.width - 50, 20), presetName);
			}
		}
	}

	/// <summary>
	/// Callback when the popup window is opened.
	/// </summary>
	void OnEnable()
	{
		// Element styles.
		oddStyle = new GUIStyle("IN BigTitle Inner");
		evenStyle = EditorStyles.label;

		// Find the specified library in project.
		libraryAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(libraryName + " t:CurvePresetLibrary")[0]), typeof(ScriptableObject));
	}
}
