using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace Mobcast.Coffee.Transition
{
	public class UIAnimationPreset : ScriptableObject
	{
		public UIAnimationData m_AnimationData = new UIAnimationData();
	}

	[System.Serializable]
	public class UIAnimationData
	{
		[FormerlySerializedAs("m_Tag")]
		public State m_State;
		public UIAnimationPreset m_Preset;
		public UITweenData[] m_TweenDatas = new UITweenData[0];

		public UITweenData[] tweenDatas { get { return m_Preset ? m_Preset.m_AnimationData.m_TweenDatas : m_TweenDatas; } }
	}

	public enum State
	{
		Show = 1 << 0,
		Idle = 1 << 1,
		Hide = 1 << 2,
//		Disable = 1 << 3,
		Press = 1 << 4,
		Click = 1 << 5,
	}


	[System.Serializable]
	public class UITweenData
	{
		/// <summary>
		/// Loop style.
		/// </summary>
		public enum LoopMode
		{
			Once,
			Loop,
			DelayedLoop,
			PingPong,
			DelayedPingPong,
		}

		/// <summary>
		/// Tweenに利用するプロパティバインド.
		/// </summary>
		public enum PropertyType
		{
			Position,
			Rotation,
			Scale,
			Size,
			Alpha,
			Custom,
		}

		/// <summary>相対データかどうか.</summary>
		public bool relative = true;

		/// <summary>時間.</summary>
		public float duration = 0.2f;

		/// <summary>ディレイ時間.</summary>
		public float delay = 0f;

		/// <summary>アニメーションカーブ.</summary>
		public AnimationCurve curve;

		/// <summary>ループタイプ.</summary>
		public LoopMode loop = LoopMode.Once;

		[FormerlySerializedAs("type")]
		public PropertyType propertyType = PropertyType.Custom;

		/// <summary>移動量.</summary>
		public Vector3 movement;
	}
}