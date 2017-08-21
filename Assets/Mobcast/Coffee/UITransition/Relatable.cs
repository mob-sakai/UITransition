using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Mobcast.Coffee.Transition
{
	#if UNITY_EDITOR
	using UnityEditor;

	public static class RelatableEditor
	{
		static GUIContent contentPermanentParent;
		static GUIContent contentPermanentParentNone;
		static GUIContent contentHierarchicalParent;
		static GUIContent contentPermanentChild;
		static GUIContent contentHierarchicalChild;

		static void CacheGUI()
		{
			if (contentPermanentParent == null)
			{
				contentPermanentParent = new GUIContent("Parent", EditorGUIUtility.FindTexture("collab"), "Permanent Parent");
				contentPermanentParentNone = new GUIContent("Parent", EditorGUIUtility.FindTexture("collabnew"), "Permanent Parent");
				contentHierarchicalParent = new GUIContent("Parent", "Hierarchical Parent");
				contentPermanentChild = new GUIContent("Child", EditorGUIUtility.FindTexture("collab"), "Permanent Child");
				contentHierarchicalChild = new GUIContent("Child", "Hierarchical Child");
			}
		}

		public static void DrawRelations<T>(SerializedObject so) where T : Relatable<T>
		{
			CacheGUI();
			if (so == null || !(so.targetObject is T))
				return;
			
			T target = so.targetObject as T;

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				var spIgnore = so.FindProperty("m_IgnoreHierarchy");
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("Relations", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth - 10));
					GUILayout.FlexibleSpace();
					spIgnore.boolValue = EditorGUILayout.ToggleLeft("Ignore Hierarchy", spIgnore.boolValue, GUILayout.Width(110));
				}
				GUILayout.Space(-4);

				using (new EditorGUI.DisabledGroupScope(true))
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					//リレーション一覧
					if (!spIgnore.boolValue)
					{
						EditorGUILayout.ObjectField(contentHierarchicalParent, target.parent, typeof(T), true);
					}
					else
					{
						GUI.enabled = true;
						var sp = so.FindProperty("m_PermanentParent");
						var content = sp.objectReferenceValue ? contentPermanentParent : contentPermanentParentNone;
						EditorGUILayout.PropertyField(sp, content);
						GUI.enabled = false;
					}

					foreach (var child in target.children)
					{
						var content = child.ignoreHierarchy ? contentPermanentChild : contentHierarchicalChild;
						EditorGUILayout.ObjectField(content, child, typeof(T), true);
					}
				}
				if (so.ApplyModifiedProperties())
				{
					EditorApplication.delayCall += () => target.SendMessage("OnTransformParentChanged");
				}
			}
		}
	}
	#endif

	/// <summary>
	/// Parent child relatable.
	/// </summary>
	[ExecuteInEditMode]
	public class Relatable<T> : MonoBehaviour where T : Relatable<T>
	{
		static List<T> s_TempRelatables = new List<T>();

		/// <summary>Parent.</summary>
		public T parent { get { return m_RuntimeParent; } }

		T m_RuntimeParent;

		[SerializeField] T m_PermanentParent;

		/// <summary>Children.</summary>
		public List<T> children { get { return m_Children; } }

		List<T> m_Children = new List<T>();

		public Transform cachedTransform
		{
			get
			{
				if (m_CachedTransform == null)
					m_CachedTransform = transform;
				return m_CachedTransform;
			}
		}

		protected Transform m_CachedTransform;

		/// <summary>Ignore hierarchy.</summary>
		public bool ignoreHierarchy
		{
			get { return m_IgnoreHierarchy; }
			set
			{
				if (m_IgnoreHierarchy == value)
					return;
				
				m_IgnoreHierarchy = value;
				OnTransformParentChanged();
			}
		}

		[SerializeField] bool m_IgnoreHierarchy = false;


		//==== v MonoBehavior Callbacks v ====
		protected virtual void Awake()
		{
			GetComponentsInChildren<T>(true, s_TempRelatables);
			for (int i = 0; i < s_TempRelatables.Count; i++)
			{
				T relatable = s_TempRelatables[i];
				if (relatable == this)
				{
					relatable.OnTransformParentChanged();
				}
			}
			s_TempRelatables.Clear();
		}

		/// <summary>
		/// コンポーネントの破棄コールバック.
		/// インスタンスが破棄された時にコールされます.
		/// This function is called when the MonoBehaviour will be destroyed.
		/// </summary>
		protected virtual void OnDestroy()
		{
			SetParent(null);
		}

		/// <summary>
		/// 親Transformが変更されたときのコールバック.
		/// groupByTransform=trueの時、Transformヒエラルキーから自動的に親子関係を結びます.
		/// This function is called when the parent property of the transform of the GameObject has changed.
		/// Set up a parent-child relationship from transform hierarchy or permanent parent.
		/// </summary>
		public virtual void OnTransformParentChanged()
		{
			T newParent = null;
			if (ignoreHierarchy)
			{
				newParent = m_PermanentParent;
			}
			else
			{
				m_PermanentParent = null;
				// 親階層から、一番近いコンポーネントを検索.
				// Find nearest parent component.
				var parentTransform = cachedTransform.parent;
				while (parentTransform && newParent == null)
				{
					newParent = parentTransform.GetComponent<T>();
					parentTransform = parentTransform.parent;
				}
			}

			SetParent(newParent);

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
		//==== ^ MonoBehavior Callbacks ^ ====

		/// <summary>
		/// 親子関係を設定します.
		/// Set up a parent-child relationship from new parent.
		/// </summary>
		protected void SetParent(T newParent)
		{
			// The parent has changed.
			if (m_RuntimeParent != newParent && this != newParent)
			{
				// Remove self from parent's children.
				if (m_RuntimeParent && m_RuntimeParent.m_Children.Contains(this as T))
				{
					m_RuntimeParent.m_Children.Remove(this as T);
					m_RuntimeParent.m_Children.RemoveAll(x => x == null);
				}

				m_RuntimeParent = newParent;
			}

			// Add self to parent's children.
			if (m_RuntimeParent && !m_RuntimeParent.m_Children.Contains(this as T))
			{
				m_RuntimeParent.m_Children.Add(this as T);
			}
		}
	}
}