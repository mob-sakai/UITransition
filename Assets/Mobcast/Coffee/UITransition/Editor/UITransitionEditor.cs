using System;
using System.Collections.Generic;
using Mobcast.Coffee.Transition;
using UnityEditor;
using UnityEngine;

namespace Mobcast.Coffee.Transition
{
	/// <summary>
	/// Tweenエディタ.
	/// </summary>
	[CustomEditor(typeof(UITransition), true)]
	public class UITransitionEditor : UIAnimationEditor
	{
		const string kPrefsKeyExpandedTags = "UITransition_ExpandedTags";
		static State s_ExpandedTags;
		static bool s_ExpandedOption;

		static readonly Dictionary<int,Color> s_BgColor = new Dictionary<int, Color>()
		{
			{ 0, new Color(1.0f, 1.0f, 1.0f) },
			{ (int)State.Show, new Color(1.0f, 0.8f, 0.8f) },
			{ (int)State.Idle, new Color(0.8f, 1.0f, 0.8f) },
			{ (int)State.Hide, new Color(0.8f, 0.8f, 1.0f) },
			{ (int)State.Press, new Color(1.0f, 0.8f, 1.0f) },
			{ (int)State.Click, new Color(0.8f, 1.0f, 1.0f) },
		};


		//==== ▼ Monoコールバック ▼ ====
		protected virtual void OnEnable()
		{
			s_ExpandedTags = (State)EditorPrefs.GetInt("UITransition_ExpandedTags");
			s_ExpandedOption = EditorPrefs.GetBool("UITransition_ExpandedOption");
			(target as UITransition).OnTransformParentChanged();
		}

		/// <summary>
		/// インスペクタGUIコールバック.
		/// Inspectorウィンドウを表示するときにコールされます.
		/// </summary>
		public override void OnInspectorGUI()
		{
			CacheGUI();

			serializedObject.Update();
			RelatableEditor.DrawRelations<UITransition>(serializedObject);

			var spTransitionData = serializedObject.FindProperty("m_TransitionData");
			var spPreset = spTransitionData.FindPropertyRelative("m_Preset");
			var tr = target as UITransition;




			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{

				if (GUILayout.Toggle(s_ExpandedOption, "<b>Advanced Option</b>", "IN LABEL") != s_ExpandedOption)
				{
					s_ExpandedOption = !s_ExpandedOption;
					EditorPrefs.SetBool("UITransition_ExpandedOption", s_ExpandedOption);
				}

				if (s_ExpandedOption)
				{
					using (new EditorGUI.DisabledGroupScope(tr.parent))
					{
						EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StateOnEnable"));
					}

					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(10);
						EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ShowStateOption"), true);
					}

					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(10);
						EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HideStateOption"), true);
					}
				}
			}

			GUILayout.Space(10);
			EditorGUILayout.LabelField("Transition Setting", EditorStyles.boldLabel);
			DrawTransitionPreset(spTransitionData, tr);

			using (new EditorGUI.DisabledGroupScope(spPreset.objectReferenceValue && !enableEdit))
			{
				DrawTransitionTag(spTransitionData, tr);
				DrawTransitionData(spTransitionData, tr);
			}


			DrawController();

			serializedObject.ApplyModifiedProperties();
		}
		//==== ▲ Monoコールバック ▲ ====


		public static void DrawStateOption(SerializedProperty property)
		{
			float lw = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 70;
//			var property = property.serializedObject.FindProperty(tag == State.Show ? "m_ShowStateOption" : "m_HideStateOption");

//			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField(property.displayName);
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Space(16);
					EditorGUILayout.PropertyField(property.FindPropertyRelative("m_AddDelay"), GUILayout.Width(100));
					var spIgnoreParent = property.FindPropertyRelative("m_IgnoreParent");
					spIgnoreParent.boolValue = EditorGUILayout.ToggleLeft("Ignore Parent", spIgnoreParent.boolValue, GUILayout.Width(100));
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Space(16);
					EditorGUILayout.PropertyField(property.FindPropertyRelative("m_ChildDelay"), GUILayout.Width(100));
					EditorGUIUtility.labelWidth = 45;
					EditorGUILayout.PropertyField(property.FindPropertyRelative("m_SortBy"));
				}
			}

			EditorGUIUtility.labelWidth = lw;
		}


		public static void DrawTransitionPreset(SerializedProperty property, UITransition transition)
		{
			var spPreset = property.FindPropertyRelative("m_Preset");

			using (new EditorGUILayout.HorizontalScope())
			{
				var oldPreset = spPreset.objectReferenceValue as UITransitionPreset;
				DrawAssetField<UITransitionPreset>(EditorGUILayout.GetControlRect(), null, spPreset, (asset, created) =>
					{
						if (created)
						{
							var so = new SerializedObject(asset);
							so.CopyFromSerializedProperty(property);
							property.FindPropertyRelative("m_Preset").objectReferenceValue = null;
							so.ApplyModifiedProperties();
						}
						else if (oldPreset && !asset)
						{
							var so = new SerializedObject(oldPreset);
							property.serializedObject.CopyFromSerializedProperty(so.FindProperty("m_TransitionData"));
							property.serializedObject.ApplyModifiedProperties();
						}
						else if (asset)
						{
							property.FindPropertyRelative("m_AnimationDatas").ClearArray();
							property.serializedObject.ApplyModifiedProperties();
						}
						spPreset.objectReferenceValue = asset;
						spPreset.serializedObject.ApplyModifiedProperties();
					});
			}
		}


		/// <summary>
		/// Transitionタグの描画
		/// </summary>
		public static void DrawTransitionTag(SerializedProperty property, UITransition transition)
		{
			var spPreset = property.FindPropertyRelative("m_Preset");
			var spAnimationDatas = spPreset.objectReferenceValue
				? new SerializedObject(spPreset.objectReferenceValue).FindProperty("m_TransitionData").FindPropertyRelative("m_AnimationDatas")
				: property.FindPropertyRelative("m_AnimationDatas");

			// Collect valid Tags. 
			State availableStates = 0;
			for (int i = 0; i < spAnimationDatas.arraySize; i++)
				availableStates += spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_State").intValue;

			for (int i = 0; i < spAnimationDatas.arraySize; i++)
			{
				if (spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_State").intValue <= 0)
				{
					spAnimationDatas.DeleteArrayElementAtIndex(i);
				}
			}

			EditorGUI.BeginChangeCheck();
			State selectedTag = (State)EditorGUILayout.EnumMaskField("Available States", availableStates);
			if (!EditorGUI.EndChangeCheck())
				return;

			foreach (State t in Enum.GetValues(typeof(State)))
			{
				//カテゴリ追加
				if (0 == (availableStates & t) && 0 != (selectedTag & t))
				{
					int index = spAnimationDatas.arraySize;
					for (int i = 0; i < spAnimationDatas.arraySize; i++)
					{
						if ((int)t < spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_State").intValue)
						{
							index = i;
							break;
						}
					}

					spAnimationDatas.InsertArrayElementAtIndex(index);
					var newData = spAnimationDatas.GetArrayElementAtIndex(index);
					newData.FindPropertyRelative("m_State").intValue = (int)t;
				}
				//カテゴリ削除
				else if (0 != (availableStates & t) && 0 == (selectedTag & t))
				{
					for (int i = 0; i < spAnimationDatas.arraySize; i++)
					{
						if (spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_State").intValue == (int)t)
							spAnimationDatas.DeleteArrayElementAtIndex(i);
					}
				}
			}
			spAnimationDatas.serializedObject.ApplyModifiedProperties();
		}

		public static void DrawTransitionData(SerializedProperty property, UITransition transition)
		{
			var spPreset = property.FindPropertyRelative("m_Preset");

//			EditorGUI.BeginDisabledGroup(spPreset.objectReferenceValue && !enableEdit);
			var spAnimationDatas = spPreset.objectReferenceValue
				? new SerializedObject(spPreset.objectReferenceValue).FindProperty("m_TransitionData").FindPropertyRelative("m_AnimationDatas")
				: property.FindPropertyRelative("m_AnimationDatas");

			// Animation datas.
			for (int i = 0; i < spAnimationDatas.arraySize; i++)
			{
				var spAnimationData = spAnimationDatas.GetArrayElementAtIndex(i);
				State tag = (State)spAnimationData.FindPropertyRelative("m_State").intValue;

				bool isCurrent = transition.state == tag && Application.isPlaying;
				using (new EditorGUILayout.VerticalScope(isCurrent ? "TL SelectionButton PreDropGlow" : EditorStyles.label))
				{
					// Header
					var backgroundColor = GUI.backgroundColor;
					GUI.backgroundColor = s_BgColor[(int)tag];
					var rHeader = GUILayoutUtility.GetRect(18, 18);
					GUI.Label(rHeader, GUIContent.none, styleHeader);


					Rect rToggle = new Rect(rHeader.x + 5, rHeader.y, 80, rHeader.height);
					bool expanded = 0 != (s_ExpandedTags & tag);

					bool enabled = GUI.enabled;
					GUI.enabled = true;
					if (GUI.Toggle(rToggle, expanded, tag.ToString(), EditorStyles.foldout) != expanded)
					{
						expanded = !expanded;
						s_ExpandedTags = expanded
							? s_ExpandedTags | tag
							: s_ExpandedTags & ~tag;
						
						EditorPrefs.SetInt("UITransition_ExpandedTags", (int)s_ExpandedTags);
					}
					GUI.enabled = enabled;


					// Preset
					var spAnimationPreset = spAnimationData.FindPropertyRelative("m_Preset");
					Rect rPreset = new Rect(rHeader.x + 85, rHeader.y + 1, rHeader.width - 85, 16);
					float labelWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 40;

//					DrawAnimationPresetField(rPreset, null, spAnimationPreset, spAnimationData);


					int index = i;
					var oldPreset = spAnimationPreset.objectReferenceValue as UIAnimationPreset;
					DrawAssetField<UIAnimationPreset>(rPreset, null, spAnimationPreset, (asset, created) =>
						{
							if (created)
							{
								asset.m_AnimationData = JsonUtility.FromJson<UIAnimationData>(JsonUtility.ToJson(transition.m_TransitionData.animationDatas[index]));
								asset.m_AnimationData.m_Preset = null;
								EditorUtility.SetDirty(asset);
							}
							else if (oldPreset && !asset)
							{
								if (transition.m_TransitionData.m_Preset)
								{
									var datas = transition.m_TransitionData.m_Preset.m_TransitionData.m_AnimationDatas;
									datas[index] = JsonUtility.FromJson<UIAnimationData>(JsonUtility.ToJson(oldPreset.m_AnimationData));
									datas[index].m_Preset = null;
									datas[index].m_State = tag;
									EditorUtility.SetDirty(transition.m_TransitionData.m_Preset);
								}
								else
								{
									var datas = transition.m_TransitionData.m_AnimationDatas;
									datas[index] = JsonUtility.FromJson<UIAnimationData>(JsonUtility.ToJson(oldPreset.m_AnimationData));
									datas[index].m_Preset = null;
									EditorUtility.SetDirty(transition);
								}
								EditorUtility.SetDirty(transition);
							}
							else if (asset)
							{
								spAnimationData.FindPropertyRelative("m_TweenDatas").ClearArray();
								spAnimationData.serializedObject.ApplyModifiedProperties();
							}
						});


					EditorGUIUtility.labelWidth = labelWidth;

					// Inner
					if (expanded)
					{
						using (new EditorGUILayout.VerticalScope(styleInner))
						{
							GUI.backgroundColor = backgroundColor;

							using (new EditorGUI.DisabledGroupScope(spAnimationData.FindPropertyRelative("m_Preset").objectReferenceValue && !enableEdit))
							{
								DrawAnimationData(spAnimationData, transition.helper, isCurrent);
							}
						}
					}
					GUI.backgroundColor = backgroundColor;
				}
			}
//			EditorGUI.EndDisabledGroup();

			spAnimationDatas.serializedObject.ApplyModifiedProperties();
		}


		/// <summary>
		/// </summary>
		protected void DrawRelation()
		{
			var tr = target as UITransition;

			using (new EditorGUI.DisabledGroupScope(true))
			using (new EditorGUILayout.VerticalScope("box"))
			{
				//リレーション一覧
				if (tr.parent)
				{
					EditorGUILayout.ObjectField("Parent", tr.parent, typeof(UITransition), true);
				}
				foreach (var child in tr.children)
				{
					EditorGUILayout.ObjectField("Child", child, typeof(UITransition), true);
				}
			}
		}


		/// <summary>
		/// Tweenの簡易コントローラを描画します.
		/// </summary>
		void DrawController()
		{
			var tr = target as UITransition;

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Toggle(enableEdit, new GUIContent("Edit"), EditorStyles.toolbarButton) != enableEdit)
				{
					enableEdit = !enableEdit;
				}

				EditorGUI.BeginDisabledGroup(!Application.isPlaying);
				if (GUILayout.Button("Show", EditorStyles.toolbarButton))
				{
					tr.Show();
				}
				if (GUILayout.Button("Hide", EditorStyles.toolbarButton))
				{
					tr.Hide();
				}
				EditorGUI.EndDisabledGroup();
			}
		}
	}
}