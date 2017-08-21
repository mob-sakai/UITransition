using UnityEngine;
using System.Collections;

namespace Mobcast.Coffee.Transition
{
	public class UITransitionPreset : ScriptableObject
	{
		public UITransitionData m_TransitionData = new UITransitionData();
	}

	[System.Serializable]
	public class UITransitionData
	{
		public UITransitionPreset m_Preset;
		public UIAnimationData[] m_AnimationDatas = new UIAnimationData[0];

		public UIAnimationData[] animationDatas { get { return m_Preset ? m_Preset.m_TransitionData.m_AnimationDatas : m_AnimationDatas; } }
	}
}