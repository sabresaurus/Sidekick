using UnityEngine;
using System;

namespace Sabresaurus.Sidekick
{
	public static class AnimationHelper
	{
		private static float currentFrameTimestamp = 0;
		private static float currentFrameDelta = 0;

		private static bool animationActive = false;

		public static void UpdateTime()
		{
			currentFrameDelta = Time.realtimeSinceStartup - currentFrameTimestamp;
			currentFrameTimestamp = Time.realtimeSinceStartup;
		}

		public static float DeltaTime
		{
			get
			{
                // Cap delta time in case there's a long frame
				return Mathf.Min(currentFrameDelta, 0.1f);
			}
		}


		public static bool AnimationActive 
		{
			get
			{
				return animationActive;
			}
		}

		public static void SetAnimationActive()
		{
			animationActive = true;
		}

		public static void ClearAnimationActive()
		{
			animationActive = false;
		}
	}
}