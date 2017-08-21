using UnityEngine;
using System.Collections;
using System;

namespace Mobcast.Coffee.Transition
{
	[DisallowMultipleComponent]
	public class UIAnimation : MonoBehaviour
	{
		[SerializeField]
		public bool m_PlayOnAwake = false;

		[SerializeField]
		public bool m_IgnoreTimeScale = true;

		[SerializeField]
		public UIAnimationData m_AnimationData = new UIAnimationData();

		public UIAnimationHelper helper = new UIAnimationHelper();

		public bool isPlaying { get; set; }

		void Awake()
		{
			if (m_PlayOnAwake)
				PlayForward();
		}

		void LateUpdate()
		{
			if (!isPlaying)
				return;
			
			helper.Update(this, m_AnimationData.tweenDatas, m_IgnoreTimeScale, false, false);
			isPlaying = helper.isPlaying;
		}

		public void PlayForward()
		{
			Play(PlayDirection.Forward);
		}

		public void PlayReverse()
		{
			Play(PlayDirection.Reverse);
		}

		public void Play(PlayDirection dir, PlayMode mode = PlayMode.Play, Action callback = null, float delay = 0)
		{
			helper.Play(dir, mode, callback, delay);
			isPlaying = helper.isPlaying;
		}

		public void Stop()
		{
			helper.Stop();
			isPlaying = false;
		}

		public void Pause()
		{
			isPlaying = false;
		}

		public void Resume()
		{
			isPlaying = true;
		}
	}
}