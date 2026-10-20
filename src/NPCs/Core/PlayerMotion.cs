using UnityEngine;

namespace NPCs.Core
{
	/// <summary>
	/// How fast the player is moving, measured from how far they travel each frame.
	/// They might be on foot or in a vehicle, so the distance covered is used rather than any one object's velocity.
	/// </summary>
	public static class PlayerMotion
	{
		// Frames that move the player faster than this are teleports or world shifts rather than real movement, so they're skipped.
		private const float JUMP_SPEED = 100f;

		// How long the reading takes to settle, so a single hitchy frame doesn't spike it.
		private const float SMOOTHING_TIME = 0.3f;

		private static bool _hasLast;
		private static Vector3 _last;

		/// <summary>
		/// The player's horizontal speed in metres per second.
		/// </summary>
		public static float Speed { get; private set; }

		/// <summary>
		/// Call once per frame so every NPC reads the same value instead of measuring it themselves.
		/// </summary>
		public static void Tick()
		{
			if (mainscript.M == null || mainscript.M.player == null)
			{
				_hasLast = false;
				Speed = 0f;
				return;
			}

			Vector3 position = mainscript.M.player.transform.position;
			float deltaTime = Time.deltaTime;

			if (_hasLast && deltaTime > 0f)
			{
				// Height is ignored so jumping and falling don't read as travelling.
				Vector3 moved = position - _last;
				moved.y = 0f;

				float instant = moved.magnitude / deltaTime;
				if (instant <= JUMP_SPEED)
					Speed = Mathf.Lerp(Speed, instant, 1f - Mathf.Exp(-deltaTime / SMOOTHING_TIME));
			}

			_last = position;
			_hasLast = true;
		}
	}
}
