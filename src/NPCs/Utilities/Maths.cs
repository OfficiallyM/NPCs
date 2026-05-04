using UnityEngine;

namespace NPCs.Utilities
{
	internal static class Maths
	{
		public static float RoundToNearestHalf(float value)
		{
			return Mathf.Round(value * 2f) / 2f;
		}
	}
}
