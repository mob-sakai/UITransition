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
		static UIAnimationTag s_ExpandedTags;

		static readonly Dictionary<int,Color> s_BgColor = new Dictionary<int, Color>()
		{
			{ 0, new Color(1.0f, 1.0f, 1.0f) },
			{ (int)UIAnimationTag.Show, new Color(1.0f, 0.8f, 0.8f) },
			{ (int)UIAnimationTag.Idle, new Color(0.8f, 1.0f, 0.8f) },
			{ (int)UIAnimationTag.Hide, new Color(0.8f, 0.8f, 1.0f) },
//			{ (int)UIAnimationTag.Disable, new Color(1.0f, 1.0f, 0.8f) },
			{ (int)UIAnimationTag.Press, new Color(1.0f, 0.8f, 1.0f) },
			{ (int)UIAnimationTag.Click, new Color(0.8f, 1.0f, 1.0f) },
		};


		//==== ▼ Monoコールバック ▼ ====
		protected virtual void OnEnable()
		{
			s_ExpandedTags = (UIAnimationTag)EditorPrefs.GetInt("UITransition_ExpandedTags");
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

		public static void DrawTransitionPreset(SerializedProperty property, UITransition transition)
		{
			var spPreset = property.FindPropertyRelative("m_Preset");

			using (new EditorGUILayout.HorizontalScope())
			{
				var oldPreset = spPreset.objectReferenceValue as UITransitionPreset;
				DrawAssetField<UITransitionPreset>(EditorGUILayout.GetControlRect(), null, spPreset, (asset, created) =>
					{
						Debug.Log(asset + ", " + created + ", " + oldPreset);
						if (created)
						{
							Debug.Log("created");
							var so = new SerializedObject(asset);
							so.CopyFromSerializedProperty(property);
							property.FindPropertyRelative("m_Preset").objectReferenceValue = null;
							so.ApplyModifiedProperties();
						}
						else if (oldPreset && !asset)
						{
							Debug.Log("To Null");
							var so = new SerializedObject(oldPreset);
							property.serializedObject.CopyFromSerializedProperty(so.FindProperty("m_TransitionData"));
							property.serializedObject.ApplyModifiedProperties();
						}
						else if (asset)
						{
							Debug.Log("selected");
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
			UIAnimationTag currentValidTags = 0;
			for (int i = 0; i < spAnimationDatas.arraySize; i++)
				currentValidTags += spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_Tag").intValue;

			for (int i = 0; i < spAnimationDatas.arraySize; i++)
			{
				if (spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_Tag").intValue <= 0)
				{
					spAnimationDatas.DeleteArrayElementAtIndex(i);
				}
			}

			EditorGUI.BeginChangeCheck();
			UIAnimationTag selectedTag = (UIAnimationTag)EditorGUILayout.EnumMaskField("Tag", currentValidTags);
			if (!EditorGUI.EndChangeCheck())
				return;

			foreach (UIAnimationTag t in Enum.GetValues(typeof(UIAnimationTag)))
			{
				//カテゴリ追加
				if (0 == (currentValidTags & t) && 0 != (selectedTag & t))
				{
					int index = spAnimationDatas.arraySize;
					for (int i = 0; i < spAnimationDatas.arraySize; i++)
					{
						if ((int)t < spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_Tag").intValue)
						{
							index = i;
							break;
						}
					}

					spAnimationDatas.InsertArrayElementAtIndex(index);
					var newData = spAnimationDatas.GetArrayElementAtIndex(index);
					newData.FindPropertyRelative("m_Tag").intValue = (int)t;
				}
				//カテゴリ削除
				else if (0 != (currentValidTags & t) && 0 == (selectedTag & t))
				{
					for (int i = 0; i < spAnimationDatas.arraySize; i++)
					{
						if (spAnimationDatas.GetArrayElementAtIndex(i).FindPropertyRelative("m_Tag").intValue == (int)t)
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
				UIAnimationTag tag = (UIAnimationTag)spAnimationData.FindPropertyRelative("m_Tag").intValue;

				bool isCurrent = transition.currentTag == tag && Application.isPlaying;
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
								if(transition.m_TransitionData.m_Preset)
								{
									var datas = transition.m_TransitionData.m_Preset.m_TransitionData.m_AnimationDatas;
									datas[index] = JsonUtility.FromJson<UIAnimationData>(JsonUtility.ToJson(oldPreset.m_AnimationData));
									datas[index].m_Preset = null;
									datas[index].m_Tag = tag;
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


							enabled = GUI.enabled;
							GUI.enabled = true;
							if (tag == UIAnimationTag.Show)
								EditorGUILayout.PropertyField(property.serializedObject.FindProperty("m_AdditionalShowDelay"), new GUIContent("Additional Delay"));
							else if(tag == UIAnimationTag.Hide)
								EditorGUILayout.PropertyField(property.serializedObject.FindProperty("m_AdditionalHideDelay"), new GUIContent("Additional Delay"));
							GUI.enabled = enabled;
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