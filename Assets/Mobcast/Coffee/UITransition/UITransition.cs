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

		/// <summary>現在、表示されているかどうかを取得します.</summary>
		public bool isShow { get { return currentTag != UIAnimationTag.Hide; } }

		/// <summary>遷移中かどうか取得します.子UIが遷移中の場合はfalseを返します.</summary>
		public bool isPlaying { get; set; }

		public UIAnimationTag currentTag { get; set; }

		/// <summary>UI遷移データ.</summary>
		public UITransitionData m_TransitionData = new UITransitionData();
		public UIAnimationHelper helper = new UIAnimationHelper();

		public float m_AdditionalShowDelay = 0;
		public float m_AdditionalHideDelay = 0;

		protected override void Awake()
		{
			base.Awake();
			m_Selectable = GetComponent<Selectable>();
		}


		void LateUpdate()
		{
			if (!isPlaying)
				return;

			var anim = m_TransitionData.animationDatas.FirstOrDefault(x => x.m_Tag == currentTag);
			if (anim != null)
			{
				bool onceOnly = currentTag != UIAnimationTag.Idle;
				helper.Update(this, anim.tweenDatas, false, currentTag == UIAnimationTag.Show, onceOnly);
				isPlaying = helper.isPlaying;
			}
			else
			{
				helper.Stop();
				isPlaying = false;
//				enabled = false;
			}
		}

		/// <summary>
		/// UIを表示します.
		/// </summary>
		/// <param name="skipAnimation">アニメーションをスキップします.</param>
		public virtual void Idle()
		{
			currentTag = UIAnimationTag.Idle;
			helper.Play(PlayDirection.Forward, PlayMode.Replay, null);
			isPlaying = true;
//			enabled = true;
		}

		/// <summary>
		/// UIを表示します.
		/// </summary>
		/// <param name="skipAnimation">アニメーションをスキップします.</param>
		public virtual void Show(bool skipAnimation = false)
		{
			if (currentTag != UIAnimationTag.Hide && !skipAnimation)
				return;

			currentTag = UIAnimationTag.Show;
			helper.Stop();
			helper.Play(PlayDirection.Reverse, skipAnimation ? PlayMode.Skip : PlayMode.Replay, Idle, m_AdditionalShowDelay);
//			enabled = true;
			isPlaying = true;

			foreach (var c in children)
			{
				c.Show(skipAnimation);
			}
		}


		/// <summary>
		/// UIを非表示します.
		/// </summary>
		/// <param name="skipAnimation">アニメーションをスキップします.</param>
		public virtual void Hide(bool skipAnimation = false)
		{
			if (currentTag == UIAnimationTag.Hide && !skipAnimation)
				return;
			
			currentTag = UIAnimationTag.Hide;
			helper.Play(PlayDirection.Forward, skipAnimation ? PlayMode.Skip : PlayMode.Replay, null, m_AdditionalHideDelay);
//			enabled = true;
			isPlaying = true;

			foreach (var c in children)
			{
				c.Hide(skipAnimation);
			}
		}



		/// <summary>ゲームオブジェクトが持つSelectable.</summary>
		Selectable m_Selectable;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;

			Click(null);
			//currentTag = UIAnimationTag.Click;
			//helper.Play(PlayDirection.Forward, PlayMode.Replay, Idle);
			//isPlaying = true;
//			enabled = true;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;
			
			currentTag = UIAnimationTag.Press;
			helper.Play(PlayDirection.Forward, PlayMode.Play, null);
//			enabled = true;
			isPlaying = true;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;

			if (!eventData.dragging && !eventData.eligibleForClick && currentTag == UIAnimationTag.Press)
			{
				helper.Play(PlayDirection.Reverse, PlayMode.Play, Idle);
				isPlaying = true;
			}
		}


		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			if (!m_Selectable)
				return;
//			if (m_ScrollRect)
//				m_ScrollRect.OnInitializePotentialDrag(eventData);
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
			if (!eventData.eligibleForClick && currentTag == UIAnimationTag.Press)
			{
				helper.Play(PlayDirection.Reverse, PlayMode.Play, Idle);
				isPlaying = true;
			}
		}

		public void OnSubmit(BaseEventData eventData)
		{
			if (!m_Selectable)
				return;
			Click(null);
		}

		public void Click(Action callback)
		{
			if (currentTag != UIAnimationTag.Click)
			{
				currentTag = UIAnimationTag.Click;
				helper.Play(PlayDirection.Forward, PlayMode.Replay, Idle);
				isPlaying = true;
			}
			if(callback != null)
				helper.onFinished += callback;
		}
	}
}