using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;


namespace Mobcast.Coffee.Transition
{
	/// <summary>再生方向.</summary>
	public enum PlayDirection
	{
		/// <summary>順方向に再生します.</summary>
		Reverse = -1,
		/// <summary>逆方向に再生します.</summary>
		Forward = 1
	}

	/// <summary>再生モード.</summary>
	public enum PlayMode
	{
		/// <summary>アニメーションを再生します.</summary>
		Play,
		/// <summary>アニメーションを始めから再生します.</summary>
		Replay,
		/// <summary>アニメーションをスキップします.</summary>
		Skip,
	}

	public class UIAnimationHelper
	{
		public float[] m_Rates = new float[(int)UITweenData.PropertyType.Custom + 1];
		public float[] m_Delays = new float[(int)UITweenData.PropertyType.Custom + 1];

		public event Action onFinished;

		/// <summary>ローカル座標.</summary>
		public Vector3 position = Vector3.zero;
		/// <summary>ローカル回転.</summary>
		public Vector3 rotation = Vector3.zero;
		/// <summary>ローカルスケール.</summary>
		public Vector3 scale = Vector3.one;
		/// <summary>ローカルスケール.</summary>
		public Vector3 size = Vector3.zero;
		/// <summary>ローカルスケール.</summary>
		public float alpha = 1;

		public bool isPlaying { get; private set;}
		public bool m_Backuped = false;
		CanvasGroup m_CanvasGroup;
		Transform m_Transform;


		[System.NonSerialized]public PlayDirection playDirection = PlayDirection.Forward;

		public void Cache(MonoBehaviour target)
		{
			if (object.ReferenceEquals(m_Transform, null))
			{
				m_Transform = target.GetComponent<Transform>();
				m_CanvasGroup = target.GetComponent<CanvasGroup>();
			}
			if (!m_Backuped)
			{
#if UNITY_EDITOR
				m_Backuped = Application.isPlaying;
#else
				m_Backuped = true;
#endif
				StoreBackup();
			}
		}

		void StoreBackup()
		{
			rotation = m_Transform.eulerAngles;
			scale = m_Transform.localScale;

			RectTransform rt = m_Transform as RectTransform;
			if (rt)
			{
				size = rt.sizeDelta;
				position = rt.anchoredPosition3D;
			}
			else
			{
				size = Vector2.zero;
				position = m_Transform.localPosition;
			}

			if (m_CanvasGroup)
				alpha = m_CanvasGroup.alpha;
		}

		/// <summary>
		/// initial valueにもどす
		/// </summary>
		void RestoreBackup()
		{
			m_Transform.eulerAngles = rotation;
			m_Transform.localScale = scale;

			RectTransform rt = m_Transform as RectTransform;
			if (rt)
			{
				rt.sizeDelta = size;
				rt.anchoredPosition3D = position;
			}
			else
			{
				m_Transform.localPosition = position;
			}

			if (m_CanvasGroup)
				m_CanvasGroup.alpha = alpha;
		}


		public void Update(MonoBehaviour target, UITweenData[] datas, bool ignoreTimeScale, bool evaluateReverse, bool onceOnly)
		{
			Cache(target);

			bool isFinished = true;

			//Sampling by data.
			for (int i = 0; i < datas.Length; i++)
			{
				float delta = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
				var d = datas[i];
				int chIndex = (int)d.propertyType;

//				bool onceOnly = m_AnimationData.m_State == UIAnimationTag.Show || m_AnimationData.m_State == UIAnimationTag.Hide;
				var loop = onceOnly ? UITweenData.LoopMode.Once : d.loop;

				// Delay.
				if (m_Delays[chIndex] < d.delay)
				{
					// Update delay.
					m_Delays[chIndex] += delta;
					if (m_Delays[chIndex] < d.delay)
					{
						isFinished = false;
						continue;
					}

					// On finished delay, fix current rate.
					delta += m_Delays[chIndex] - d.delay;
					m_Rates[chIndex] =
						playDirection == PlayDirection.Forward
						? 0
						: UITweenData.LoopMode.PingPong <= loop ? 2 : 1;
				}

				// Update current rate.
				float ch = m_Rates[chIndex];
				ch += (float)playDirection * delta / Mathf.Max(0.01f, d.duration);

				isFinished &= (loop == UITweenData.LoopMode.Once);
				switch (loop)
				{
					case UITweenData.LoopMode.Once:
						isFinished &= (ch < 0 || 1 < ch);
						ch = Mathf.Clamp01(ch);
						break;
					case UITweenData.LoopMode.Loop:
						ch = Mathf.Repeat(ch, 1);
						break;
					case UITweenData.LoopMode.DelayedLoop:
						if (0 < d.delay && playDirection == PlayDirection.Forward ? 1 < ch : ch < 0)
						{
							m_Delays[chIndex] = 0;
							ch = playDirection == PlayDirection.Forward ? 1 : 0;
						}
						else
							ch = Mathf.Repeat(ch, 1);
						break;
					case UITweenData.LoopMode.PingPong:
						ch = Mathf.Repeat(ch, 2);
						break;
					case UITweenData.LoopMode.DelayedPingPong:
						if (0 < d.delay && (playDirection == PlayDirection.Forward ? 2 < ch : ch < 0))
						{
							m_Delays[chIndex] = 0;
							ch = 0;
						}
						else
							ch = Mathf.Repeat(ch, 2);
						break;
				}
				m_Rates[chIndex] = ch;
			}

			Apply(datas, evaluateReverse);

			isPlaying = !isFinished;
			if (isFinished)
			{
				if (onFinished != null)
				{
					onFinished();
					onFinished = null;
				}
			}
		}

		void Apply(UITweenData[] datas, bool evaluateReverse)
		{
			for (int i = 0; i < datas.Length; i++)
			{
				var data = datas[i];
				var rate = Mathf.Abs(Mathf.Repeat(m_Rates[(int)data.propertyType] + 1, 2) - 1);

				if (evaluateReverse)
					rate = 1 - data.curve.Evaluate(1 - rate);
				else
					rate = data.curve.Evaluate(rate);


				RectTransform rt = m_Transform as RectTransform;
				switch (data.propertyType)
				{
					case UITweenData.PropertyType.Position:
						{
							var from = position;
							var to = data.relative ? from + data.movement : data.movement;
							if (rt)
								rt.anchoredPosition3D = from + rate * (to - from);
							else
								m_Transform.localPosition = from + rate * (to - from);
						}
						break;
					case UITweenData.PropertyType.Rotation:
						{
							var from = rotation;
							var to = data.relative ? from + data.movement : data.movement;
							m_Transform.eulerAngles = from + rate * (to - from);
						}
						break;
					case UITweenData.PropertyType.Scale:
						{
							var from = scale;
							var to = data.relative ? from + data.movement : data.movement;
							m_Transform.localScale = from + rate * (to - from);
						}
						break;
					case UITweenData.PropertyType.Size:
						if (rt)
						{
							var from = size;
							var to = data.relative ? from + data.movement : data.movement;
							rt.sizeDelta = from + rate * (to - from);
						}
						break;
					case UITweenData.PropertyType.Alpha:
						if (m_CanvasGroup)
						{
							var from = alpha;
							var to = data.relative ? from + data.movement.x : data.movement.x;
							m_CanvasGroup.alpha = from + rate * (to - from);
						}
						break;
					case UITweenData.PropertyType.Custom:
						break;
				}
			}
		}

		public void Play(PlayDirection dir, PlayMode mode, Action callback, float delay = 0)
		{
			bool isForward = dir == PlayDirection.Forward;
			switch (mode)
			{
				case PlayMode.Replay:
					ResetTime(dir, isForward ? 0 : 1, delay);
					break;
				case PlayMode.Skip:
					ResetTime(dir, isForward ? 1 : 0, 0);
					break;
			}

			onFinished = callback;
			playDirection = dir;
			isPlaying = true;
		}

		public void ResetTime(PlayDirection dir, float rate, float delay)
		{
			playDirection = dir;

			for (int i = 0; i < m_Rates.Length; i++)
				m_Rates[i] = rate;

			for (int i = 0; i < m_Delays.Length; i++)
				m_Delays[i] = -delay;
		}

		public void Stop(bool withCallback = false)
		{
			isPlaying = false;
			ResetTime(PlayDirection.Forward, 0, 0);

			if (m_Backuped)
			{
				RestoreBackup();
				m_Backuped = false;
			}

			if (withCallback && onFinished != null)
			{
				onFinished();
			}
			onFinished = null;
		}
	}
}
