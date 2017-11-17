using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Mobcast.Coffee.Transition;


namespace Mobcast.Coffee.Transition
{
	/// <summary>
	/// UI遷移コンポーネント.
	/// TweenUIを利用した遷移モーションを定義/再生します.
	/// 遷移は親子関係を持ち、ignoreParentGroupが設定されていないかぎり親UIの操作は子UIにも反映されます.
	/// 例えば、親UIをShowした時、子UIもShowされます.
	/// </summary>
	[DisallowMultipleComponent]
	public class UITransition : Relatable<UITransition>, IPointerClickHandler, ISubmitHandler, IPointerDownHandler, IPointerUpHandler, IEndDragHandler, IDragHandler, IBeginDragHandler
	{
		[Serializable]
		public class StateOption
		{
			public enum SortChildrenBy
			{
				None,
				Hierarchy,
				HierarchyDesc,
				PositionX,
				PositionXDesc,
				PositionY,
				PositionYDesc,
			}

			/// <summary>
			/// 追加ディレイ.
			/// プリセットを利用している場合において、アニメーションのディレイ値の変更をこのコンポーネントのみに限定したい時に利用できます.
			/// </summary>
			public float m_AdditionalDelay = 0;
			/// <summary>
			/// 親UITransitionが指定する追加ディレイ値を無視します.
			/// </summary>
			public bool m_IgnoreParent = false;
			/// <summary>
			/// 子UITransitionのステートを逐次的に変更する場合のソート基準.
			/// 例えば、PositionXを選択すると、ワールドX座標が小さいものからステートを変更していきます.
			/// </summary>
			public SortChildrenBy m_SortChildrenBy = SortChildrenBy.None;
			/// <summary>
			/// 子UITransitionのステートを逐次的に変更する場合のディレイ値.
			/// </summary>
			public float m_ChildDelaySequencial = 0;
		}

		public enum StateOnEnable
		{
			None,
			Hide,
			HideAlways,
			HideSkipped,
			Show,
			ShowAlways,
			ShowSkipped,
		}

		/// <summary>現在、表示されているかどうかを取得します.</summary>
		public bool isShow { get { return state != State.Hide; } }

		/// <summary>遷移中かどうか取得します.子UIが遷移中の場合はfalseを返します.</summary>
		public bool isPlaying { get; private set; }

		public State state { get; set; }

		/// <summary>UI遷移データ.</summary>
		public UITransitionData m_TransitionData = new UITransitionData();
		public UIAnimationHelper helper = new UIAnimationHelper();

		public StateOnEnable m_StateOnEnable = StateOnEnable.None;

		public StateOption m_ShowStateOption;
		public StateOption m_HideStateOption;

		static readonly List<UITransition> m_DoOnNextUpdate = new List<UITransition>();

		protected override void Awake()
		{
			base.Awake();
			m_Selectable = GetComponent<Selectable>();
		}


		protected virtual void OnEnable()
		{
			if (Application.isPlaying && !parent && m_StateOnEnable != StateOnEnable.None)
			{
				m_DoOnNextUpdate.Add(this);
			}
		}

		protected virtual void OnDisable()
		{
			m_DoOnNextUpdate.Remove(this);
		}


		void LateUpdate()
		{
			if (0 < m_DoOnNextUpdate.Count)
			{
				var array = m_DoOnNextUpdate.ToArray();
				m_DoOnNextUpdate.Clear();

				foreach (var t in array)
				{
					switch (t.m_StateOnEnable)
					{
						case StateOnEnable.Hide:
							t.Hide(PlayMode.Play);
							break;
						case StateOnEnable.HideAlways:
							t.Hide(PlayMode.Replay);
							break;
						case StateOnEnable.HideSkipped:
							t.Hide(PlayMode.Skip);
							break;
						case StateOnEnable.Show:
							t.Show(PlayMode.Play);
							break;
						case StateOnEnable.ShowAlways:
							t.Show(PlayMode.Replay);
							break;
						case StateOnEnable.ShowSkipped:
							t.Show(PlayMode.Skip);
							break;
					}
				}
			}


			if (!isPlaying)
				return;

			var anim = m_TransitionData.animationDatas.FirstOrDefault(x => x.m_State == state);
			if (anim != null)
			{
				bool onceOnly = state != State.Idle;
				helper.Update(this, anim.tweenDatas, false, state == State.Show, onceOnly);
				isPlaying = helper.isPlaying;
			}
			else
			{
				helper.Stop();
				isPlaying = false;
			}
		}

		/// <summary>
		/// UIを表示します.
		/// </summary>
		public virtual void Idle()
		{
			state = State.Idle;
			helper.Play(PlayDirection.Forward, PlayMode.Replay, null);
			PlayPrepare(State.Idle, null);
		}

		/// <summary>
		/// UIを表示します.
		/// </summary>
		/// <param name="mode">再生モード.</param>
		/// <param name="delay">ディレイ.</param>
		/// <param name="callback">コールバック.</param>
		public virtual void Show(PlayMode mode, float delay = 0, Action callback = null)
		{
			if (mode == PlayMode.Play && state != State.Hide)
				return;

			var stateDelay = m_ShowStateOption;
			helper.Stop();
			helper.Play(PlayDirection.Reverse, (mode == PlayMode.Skip) ? PlayMode.Skip : PlayMode.Replay, Idle, stateDelay.m_AdditionalDelay + delay);
			PlayPrepare(State.Show, callback);

			float childDelay = 0;
			foreach (var c in GetSortedChildren(stateDelay.m_SortChildrenBy))
			{
				if (c.m_ShowStateOption.m_IgnoreParent || stateDelay.m_SortChildrenBy == StateOption.SortChildrenBy.None || mode == PlayMode.Skip)
				{
					c.Show(mode);
				}
				else
				{
					childDelay += stateDelay.m_ChildDelaySequencial;
					c.Show(mode, childDelay);
				}
			}
		}

		public void Show()
		{
			Show(PlayMode.Play, 0);
		}

		/// <summary>
		/// UIを非表示します.
		/// </summary>
		/// <param name="mode">再生モード.</param>
		/// <param name="delay">ディレイ.</param>
		/// <param name="callback">コールバック.</param>
		public virtual void Hide(PlayMode mode, float delay = 0, Action callback = null)
		{
			if (mode == PlayMode.Play && state == State.Hide)
				return;

			var stateDelay = m_HideStateOption;
			helper.Stop();
			helper.Play(PlayDirection.Forward, (mode == PlayMode.Skip) ? PlayMode.Skip : PlayMode.Replay, null, stateDelay.m_AdditionalDelay + delay);
			PlayPrepare(State.Hide, callback);

			float childDelay = 0;
			foreach (var c in GetSortedChildren(stateDelay.m_SortChildrenBy))
			{
				if (c.m_HideStateOption.m_IgnoreParent || stateDelay.m_SortChildrenBy == StateOption.SortChildrenBy.None || mode == PlayMode.Skip)
				{
					c.Hide(mode);
				}
				else
				{
					childDelay += stateDelay.m_ChildDelaySequencial;
					c.Hide(mode, childDelay);
				}
			}
		}

		public void Hide()
		{
			Hide(PlayMode.Play, 0);
		}


		public void Press(PlayDirection dir)
		{
			if (dir == PlayDirection.Forward)
				helper.Play(PlayDirection.Forward, PlayMode.Play, null);
			else
				helper.Play(PlayDirection.Reverse, PlayMode.Play, Idle);
			
			PlayPrepare(State.Press, null);

			foreach (var c in children)
			{
				c.Press(dir);
			}
		}

		public void Click(Action callback)
		{
			if (state != State.Click)
			{
				state = State.Click;
				helper.Play(PlayDirection.Forward, PlayMode.Replay, Idle);
			}
			PlayPrepare(State.Click, callback);

			foreach (var c in children)
			{
				c.Click(null);
			}
		}

		void PlayPrepare(State nextState, Action callback)
		{
			state = nextState;
			if (callback != null)
				helper.onFinished += callback;

			isPlaying = true;
			m_DoOnNextUpdate.Remove(this);
		}

		IEnumerable<UITransition> GetSortedChildren(StateOption.SortChildrenBy method)
		{
			switch (method)
			{
				case StateOption.SortChildrenBy.None:
					return children;
				case StateOption.SortChildrenBy.Hierarchy:
					return children.OrderBy(x => x.cachedTransform.GetSiblingIndex());
				case StateOption.SortChildrenBy.HierarchyDesc:
					return children.OrderByDescending(x => x.cachedTransform.GetSiblingIndex());
				case StateOption.SortChildrenBy.PositionX:
					return children.OrderBy(x => x.cachedTransform.position.x);
				case StateOption.SortChildrenBy.PositionXDesc:
					return children.OrderByDescending(x => x.cachedTransform.position.x);
				case StateOption.SortChildrenBy.PositionY:
					return children.OrderBy(x => x.cachedTransform.position.y);
				case StateOption.SortChildrenBy.PositionYDesc:
					return children.OrderByDescending(x => x.cachedTransform.position.y);
				default:
					return children;
			}
		}

		/// <summary>ゲームオブジェクトが持つSelectable.</summary>
		Selectable m_Selectable;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;

			Click(null);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;
			
			Press(PlayDirection.Forward);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;

			if (!eventData.dragging && !eventData.eligibleForClick && state == State.Press)
			{
				Press(PlayDirection.Reverse);
			}
		}


		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;
//			m_ScrollRect = this.GetComponentInParent<ScrollRect>();
//			if (m_ScrollRect)
//				m_ScrollRect.OnBeginDrag(eventData);
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;
//			if (m_ScrollRect)
//				m_ScrollRect.OnDrag(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
//			if (m_ScrollRect)
//				m_ScrollRect.OnEndDrag(eventData);
//			m_ScrollRect = null;

			if (!m_Selectable)
				return;


			if (!eventData.eligibleForClick && state == State.Press)
			{
				Press(PlayDirection.Reverse);
			}
		}

		public void OnSubmit(BaseEventData eventData)
		{
			if (!m_Selectable)
				return;
			Click(null);
		}
	}
}