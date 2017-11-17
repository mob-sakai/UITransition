using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mobcast.Coffee.Transition;
using UnityEditor;
using UnityEngine;

namespace Mobcast.Coffee.Transition
{
	using PropertyType = UITweenData.PropertyType;

	/// <summary>
	/// UIAnimationエディタ.
	/// </summary>
	[CustomEditor(typeof(UIAnimation), true)]
	public class UIAnimationEditor : Editor
	{
		protected const float LEFT_WIDTH_RATE = 0.45f;
		public static bool enableEdit = false;

		static Dictionary<UITweenData.PropertyType, Texture2D> icons;

		/// <summary>非アクティブになっているアイコンの色.</summary>
		static readonly Color DEACTIVE_ICON_COLOR = new Color(1, 1, 1, 0.3f);

		/// <summary>
		/// GUIキャッシュ.
		/// </summary>
		protected virtual void CacheGUI()
		{
			if (icons != null)
				return;

			icons = new Dictionary<PropertyType, Texture2D>()
			{
				{ PropertyType.Position, EditorGUIUtility.FindTexture("MoveTool") },
				{ PropertyType.Rotation, EditorGUIUtility.FindTexture("RotateTool") },
				{ PropertyType.Scale, EditorGUIUtility.FindTexture("ScaleTool") },
				{ PropertyType.Size, EditorGUIUtility.FindTexture("RectTool") },
				{ PropertyType.Alpha, EditorGUIUtility.FindTexture("ViewToolOrbit") },
				{ PropertyType.Custom, EditorGUIUtility.FindTexture("d_editicon.sml") },
			};

			contentPlay = new GUIContent(EditorGUIUtility.FindTexture("Profiler.NextFrame"), "Play");
			contentReverse = new GUIContent(EditorGUIUtility.FindTexture("Profiler.PrevFrame"), "Reverse");

			contentStop = new GUIContent(EditorGUIUtility.FindTexture("Profiler.FirstFrame"), "Stop");
			contentPause = new GUIContent(EditorGUIUtility.FindTexture("pausebutton"), "Toggle Pause");

			styleHeader = new GUIStyle("RL Header");
			styleHeader.alignment = TextAnchor.MiddleLeft;
			styleHeader.fontSize = 11;
			styleHeader.margin = new RectOffset(0, 0, 0, 0);
			styleHeader.padding = new RectOffset(6, 0, 0, 0);
			styleHeader.normal.textColor = EditorStyles.label.normal.textColor;

			styleInner = new GUIStyle("RL Background");
			styleInner.margin = new RectOffset(0, 0, 0, 0);
			styleInner.padding = new RectOffset(4, 4, 3, 6);
			styleInner.stretchHeight = false;
		}

		//---- ▼ GUIキャッシュ ▼ ----
		static GUIContent contentPlay;
		static GUIContent contentReverse;
		static GUIContent contentToggle;
		static GUIContent contentStop;
		static GUIContent contentPause;
		protected static GUIStyle styleHeader;
		protected static GUIStyle styleInner;

		/// <summary>
		/// インスペクタGUIコールバック.
		/// Inspectorウィンドウを表示するときにコールされます.
		/// </summary>
		public override void OnInspectorGUI()
		{
			var tw = (target as UIAnimation);
			tw.helper.Cache(tw);

			CacheGUI();
			serializedObject.Update();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PlayOnAwake"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_IgnoreTimeScale"));

			var spAnimationData = serializedObject.FindProperty("m_AnimationData");

			// Preset
			var spPreset = spAnimationData.FindPropertyRelative("m_Preset");
			DrawAnimationPresetField(EditorGUILayout.GetControlRect(), null, spPreset, spAnimationData);

			// Draw AnimationData
			DrawAnimationData(spAnimationData, tw.helper);

			//Tweener controller.
			DrawController();

			serializedObject.ApplyModifiedProperties();
		}


		public static void DrawAnimationPresetField(Rect position, GUIContent label, SerializedProperty presetProperty, SerializedProperty animationDataProperty)
		{
			var oldPreset = presetProperty.objectReferenceValue as UIAnimationPreset;
			DrawAssetField<UIAnimationPreset>(position, null, presetProperty, (asset, created) =>
				{
					if (created)
					{
						var so = new SerializedObject(asset);
						so.CopyFromSerializedProperty(animationDataProperty);
						so.FindProperty("m_AnimationData").FindPropertyRelative("m_Preset").objectReferenceValue = null;
						so.ApplyModifiedProperties();
					}
					else if (oldPreset && !asset)
					{
						var so = new SerializedObject(oldPreset);
						animationDataProperty.serializedObject.CopyFromSerializedProperty(so.FindProperty("m_AnimationData"));
						animationDataProperty.serializedObject.ApplyModifiedProperties();
					}
					else if (asset)
					{
						animationDataProperty.FindPropertyRelative("m_TweenDatas").ClearArray();
						animationDataProperty.serializedObject.ApplyModifiedProperties();
					}
				});
		}


		/// <summary>
		/// Draws the style asset field.
		/// </summary>
		/// <param name="property">Property.</param>
		/// <param name="onSelect">On select.</param>
		public static void DrawAssetField<T>(Rect position, GUIContent label, SerializedProperty property, Action<T,bool> onSelect) where T: UnityEngine.Object
		{
			// Object field.
			Rect rField = new Rect(position.x, position.y, position.width - 16, position.height);
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(rField, property, label);
			if (EditorGUI.EndChangeCheck())
			{
				property.serializedObject.ApplyModifiedProperties();
				onSelect(property.objectReferenceValue as T, false);
			}

			// Popup to select style asset in project.
			Rect rPopup = new Rect(position.x + rField.width, position.y + 4, 16, position.height - 4);
			if (GUI.Button(rPopup, EditorGUIUtility.FindTexture("icon dropdown"), EditorStyles.label))
			{
				// Create style asset.
				GenericMenu menu = new GenericMenu();

				// If asset is ScriptableObject, add item to create new one.
				if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				{
					menu.AddItem(new GUIContent(string.Format("Create New {0}", typeof(T).Name)), false, () =>
						{
							// Open save file dialog.
							string filename = AssetDatabase.GenerateUniqueAssetPath(string.Format("Assets/New {0}.asset", typeof(T).Name));
							string path = EditorUtility.SaveFilePanelInProject(string.Format("Create New {0}", typeof(T).Name), Path.GetFileName(filename), "asset", "");
							if (path.Length == 0)
								return;

							// Create and save a new builder asset.
							T asset = ScriptableObject.CreateInstance(typeof(T)) as T;
							AssetDatabase.CreateAsset(asset, path);
							AssetDatabase.SaveAssets();
							EditorGUIUtility.PingObject(asset);

							property.objectReferenceValue = asset;
							property.serializedObject.ApplyModifiedProperties();
							property.serializedObject.Update();
							onSelect(asset, true);
						});
					menu.AddSeparator("");
				}

				// Unselect style asset.
				menu.AddItem(
					new GUIContent("-"),
					!property.hasMultipleDifferentValues && !property.objectReferenceValue,
					() =>
					{
						property.objectReferenceValue = null;
						property.serializedObject.ApplyModifiedProperties();
						property.serializedObject.Update();
						onSelect(null, false);
					}
				);

				// Select style asset.
				foreach (string path in AssetDatabase.FindAssets ("t:" + typeof(T).Name).Select (x => AssetDatabase.GUIDToAssetPath (x)))
				{
					string assetName = Path.GetFileNameWithoutExtension(path);
					string displayedName = assetName.Replace(" - ", "/");
					bool active = !property.hasMultipleDifferentValues && property.objectReferenceValue && (property.objectReferenceValue.name == assetName);
					menu.AddItem(
						new GUIContent(displayedName),
						active,
						x =>
						{
							T asset = AssetDatabase.LoadAssetAtPath((string)x, typeof(T)) as T;
							property.objectReferenceValue = asset;
							property.serializedObject.ApplyModifiedProperties();
							property.serializedObject.Update();
							onSelect(asset, false);
						},
						path
					);
				}

				menu.ShowAsContext();
			}
		}

		public static void DrawAnimationData(SerializedProperty property, UIAnimationHelper helper, bool showProgress = true)
		{
			var spPreset = property.FindPropertyRelative("m_Preset");

			EditorGUI.BeginDisabledGroup(spPreset.objectReferenceValue && !enableEdit);

			var spAnimationData = spPreset.objectReferenceValue
				? new SerializedObject(spPreset.objectReferenceValue).FindProperty("m_AnimationData")
				: property;

			var spDatas = spAnimationData.FindPropertyRelative("m_TweenDatas");

			// Available type.
			using (new EditorGUILayout.VerticalScope("helpbox"))
			{
				DrawAvailableTypes(EditorGUILayout.GetControlRect(false, 20), new GUIContent("Type"), property);
				GUILayout.Space(-2);
			}

			var tag = (State)property.FindPropertyRelative("m_State").intValue;
			// Animation datas.
			for (int i = 0; i < spDatas.arraySize; i++)
			{
				var spData = spDatas.GetArrayElementAtIndex(i);
				var rate = helper != null ? helper.m_Rates[spData.FindPropertyRelative("propertyType").intValue] : 0;
				DrawTweenData(spData, helper, Mathf.Abs(Mathf.Repeat(rate + 1, 2) - 1), tag, showProgress);
			}
			EditorGUI.EndDisabledGroup();

			spDatas.serializedObject.ApplyModifiedProperties();
		}

		public static void DrawTweenData(SerializedProperty property, UIAnimationHelper helper, float rate, State tag, bool showProgress = true)
		{
			var labelwidth = EditorGUIUtility.labelWidth;

			// Header
			PropertyType type = (PropertyType)property.FindPropertyRelative("propertyType").intValue;
			var rHeader = GUILayoutUtility.GetRect(18, 18);
			GUI.Label(rHeader, new GUIContent(type.ToString(), icons[type]), styleHeader);

			// Progress
			if (showProgress)
			{
				Rect rProgress = new Rect(rHeader.x + 80, rHeader.y + 3, rHeader.width - 200, rHeader.height - 6);
				GUI.Label(rProgress, GUIContent.none, "ProgressBarBack");
				rProgress.width *= rate;
				GUI.Label(rProgress, GUIContent.none, "ProgressBarBar");
			}

			// LoopMode
			bool onceOnly = tag == State.Show || tag == State.Hide || tag == State.Click;
			var spLoop = property.FindPropertyRelative("loop");
			using (new EditorGUI.DisabledGroupScope(onceOnly))
			{
				EditorGUIUtility.labelWidth = 35;
				Rect r = new Rect(rHeader.x + rHeader.width - 115, rHeader.y + 1, 110, rHeader.height);
				if (onceOnly)
					EditorGUI.EnumPopup(r, spLoop.displayName, UITweenData.LoopMode.Once);
				else
					EditorGUI.PropertyField(r, spLoop);
			}

			// Inner
			using (new EditorGUILayout.HorizontalScope(styleInner))
			{
				using (new EditorGUILayout.VerticalScope())
				{
					// Movement
					DrawMovement(EditorGUILayout.GetControlRect(), property, helper);

					// Time
					DrawTime(EditorGUILayout.GetControlRect(), property);
				}

				// Curve
				float size = EditorGUIUtility.singleLineHeight * 2 + 2;
				DrawCurve(EditorGUILayout.GetControlRect(false, size, GUILayout.MaxWidth(size + 12)), property.FindPropertyRelative("curve"));
			}
			EditorGUIUtility.labelWidth = labelwidth;
		}


		/// <summary>
		/// Draw movement property.
		/// When relative was changed, switch the value.
		/// </summary>
		static void DrawMovement(Rect r, SerializedProperty spData, UIAnimationHelper helper)
		{
			var spType = spData.FindPropertyRelative("propertyType");
			var spRelative = spData.FindPropertyRelative("relative");
			var spMovement = spData.FindPropertyRelative("movement");

			// Relative/Absolute toggle.
			// When it was changed, switch the value.
			Rect rRelative = new Rect(r.x, r.y, 52, r.height);
			string label = spRelative.boolValue ? "Relative" : "Absolute";
			if (GUI.Toggle(rRelative, spRelative.boolValue, label, EditorStyles.miniButton) != spRelative.boolValue)
			{
				spRelative.boolValue = !spRelative.boolValue;

				switch ((PropertyType)spType.intValue)
				{
					case PropertyType.Position:
						spMovement.vector3Value += (spRelative.boolValue ? -1 : 1) * helper.position;
						break;
					case PropertyType.Rotation:
						spMovement.vector3Value += (spRelative.boolValue ? -1 : 1) * helper.rotation;
						break;
					case PropertyType.Scale:
						spMovement.vector3Value += (spRelative.boolValue ? -1 : 1) * helper.scale;
						break;
					case PropertyType.Size:
						spMovement.vector3Value += (spRelative.boolValue ? -1 : 1) * helper.size;
						break;
					case PropertyType.Alpha:
						spMovement.vector3Value += (spRelative.boolValue ? -1 : 1) * new Vector3(helper.alpha, 0);
						break;
					case PropertyType.Custom:
						break;
				}
			}

			// Movement property.
			var rField = new Rect(r.x + 52 + 2, r.y, r.width - 52 - 2, r.height);

			// Alpha
			if ((PropertyType)spType.intValue == PropertyType.Alpha)
			{
				EditorGUIUtility.labelWidth = 35;
				EditorGUI.PropertyField(rField, spMovement.FindPropertyRelative("x"), new GUIContent("Alpha"));
			}
			// Vector2/Vector3
			else
			{
				int split = (PropertyType)spType.intValue == PropertyType.Size ? 2 : 3;

				EditorGUIUtility.labelWidth = 11;
				rField.width /= split;

				EditorGUI.PropertyField(rField, spMovement.FindPropertyRelative("x"));
				rField.x += rField.width;
				EditorGUI.PropertyField(rField, spMovement.FindPropertyRelative("y"));
				rField.x += rField.width;
				if (split == 3)
					EditorGUI.PropertyField(rField, spMovement.FindPropertyRelative("z"));
			}
		}


		/// <summary>
		/// Draw movement property.
		/// When relative was changed, switch the value.
		/// </summary>
		static void DrawTime(Rect r, SerializedProperty property)
		{
			Rect rField = new Rect(r.x, r.y, r.width * 0.55f, r.height);
			EditorGUIUtility.labelWidth = 55;
			EditorGUI.PropertyField(rField, property.FindPropertyRelative("duration"));

			rField.x += rField.width;
			rField.width = r.width - rField.width;
			EditorGUIUtility.labelWidth = 40;
			EditorGUI.PropertyField(rField, property.FindPropertyRelative("delay"));
		}

		/// <summary>
		/// Draw the curve property.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="property">Property.</param>
		public static void DrawCurve(Rect position, SerializedProperty property)
		{
			// Curve field(square).
			const float BUTTON_SIZE = 14;
			Rect rField = new Rect(position.x, position.y - 1, position.width - BUTTON_SIZE + 1, position.height + 1);
			EditorGUI.PropertyField(rField, property, GUIContent.none);

			// Preset button.
			Rect rButton = new Rect(rField.x + rField.width, position.y, BUTTON_SIZE, position.height);
			if (GUI.Button(rButton, EditorGUIUtility.FindTexture("icon dropdown"), EditorStyles.label))
			{
				CurvePresetPopup.Dropdown(rField, "EasingCurves", curve =>
					{
						property.animationCurveValue = curve;
						property.serializedObject.ApplyModifiedProperties();
					});
			}
		}

		/// <summary>
		/// PropertyType選択を描画します.
		/// </summary>
		public static void DrawAvailableTypes(Rect position, GUIContent label, SerializedProperty spAnimationData)
		{
			var spAnimationPreset = spAnimationData.FindPropertyRelative("m_Preset");
			var spTweenDatas = spAnimationPreset.objectReferenceValue
				? new SerializedObject(spAnimationPreset.objectReferenceValue).FindProperty("m_AnimationData").FindPropertyRelative("m_TweenDatas")
				: spAnimationData.FindPropertyRelative("m_TweenDatas");

			const float ICON_SIZE = 18;
			var color = GUI.color;

			// Collect valid types. 
			int bind = 0;
			for (int i = 0; i < spTweenDatas.arraySize; i++)
				bind += 1 << spTweenDatas.GetArrayElementAtIndex(i).FindPropertyRelative("propertyType").intValue;

			// Label.
			if (label != null)
			{
				GUI.Label(position, label);
			}

			position.x = position.xMax - ICON_SIZE * icons.Count;
			position.width = 22;

			//全TweenカテゴリのPropertyTypeを描画.
			foreach (PropertyType propertyType in Enum.GetValues(typeof(PropertyType)))
			{
				bool flag = 0 != (bind & (1 << (int)propertyType));
				GUI.color = flag ? Color.white : DEACTIVE_ICON_COLOR;

				if (GUI.Toggle(position, flag, new GUIContent(icons[propertyType], propertyType.ToString()), EditorStyles.label) != flag)
				{
					//カテゴリ追加
					if (!flag)
					{
						int index = spTweenDatas.arraySize;
						for (int i = 0; i < spTweenDatas.arraySize; i++)
						{
							if ((int)propertyType < spTweenDatas.GetArrayElementAtIndex(i).FindPropertyRelative("propertyType").intValue)
							{
								index = i;
								break;
							}
						}

						spTweenDatas.InsertArrayElementAtIndex(index);
						var newData = spTweenDatas.GetArrayElementAtIndex(index);
						newData.FindPropertyRelative("relative").boolValue = true;
						newData.FindPropertyRelative("duration").floatValue = 0.2f;
						newData.FindPropertyRelative("movement").vector3Value = Vector3.zero;
						newData.FindPropertyRelative("propertyType").intValue = (int)propertyType;
						newData.FindPropertyRelative("loop").intValue = (int)UITweenData.LoopMode.Once;
						newData.FindPropertyRelative("curve").animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
					}
					//カテゴリ削除
					else
					{
						for (int i = 0; i < spTweenDatas.arraySize; i++)
						{
							if (spTweenDatas.GetArrayElementAtIndex(i).FindPropertyRelative("propertyType").intValue == (int)propertyType)
								spTweenDatas.DeleteArrayElementAtIndex(i);
						}
					}
					spTweenDatas.serializedObject.ApplyModifiedProperties();
				}
				position.x += ICON_SIZE;
			}

			GUI.color = color;
		}


		/// <summary>
		/// Tweenの簡易コントローラを描画します.
		/// </summary>
		void DrawController()
		{
			var tw = target as UIAnimation;
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Toggle(enableEdit, new GUIContent("Edit"), EditorStyles.toolbarButton) != enableEdit)
				{
					enableEdit = !enableEdit;
				}

				EditorGUI.BeginDisabledGroup(!Application.isPlaying);
				if (GUILayout.Button(contentPlay, EditorStyles.toolbarButton))
				{
					tw.PlayForward();
				}
				if (GUILayout.Button(contentReverse, EditorStyles.toolbarButton))
				{
					tw.PlayReverse();
				}
				if (GUILayout.Button(contentPause, EditorStyles.toolbarButton))
				{
					if (tw.isPlaying)
						tw.Pause();
					else
						tw.Resume();
				}
				if (GUILayout.Button(contentStop, EditorStyles.toolbarButton))
				{
					tw.Stop();
				}
				EditorGUI.EndDisabledGroup();
			}
		}


	}
}