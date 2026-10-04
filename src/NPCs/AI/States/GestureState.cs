namespace NPCs.AI.States
{
	using UnityEngine;

	namespace NPCs.AI.States
	{
		public enum GestureType { Nod, Shake }

		/// <summary>
		/// Briefly swings the NPC's gaze around the player's head, then returns to being engaged.
		/// </summary>
		public class GestureState : NPCState
		{
			// Degrees and seconds. The animator lerps the head towards the target, so the real swing is softer than this.
			private const float NOD_AMPLITUDE = 18f;
			private const float NOD_DURATION = 1.2f;
			private const float SHAKE_AMPLITUDE = 35f;
			private const float SHAKE_DURATION = 1.4f;
			private const float CYCLES = 2f;

			private readonly GestureType _type;
			private float _elapsed;

			private float Duration => _type == GestureType.Nod ? NOD_DURATION : SHAKE_DURATION;
			private float Amplitude => _type == GestureType.Nod ? NOD_AMPLITUDE : SHAKE_AMPLITUDE;

			public GestureState(NPCAi ai, GestureType type) : base(ai)
			{
				_type = type;
			}

			public override void Enter()
			{
				// Apply immediately so the target never snaps somewhere else for a frame.
				Apply();
			}

			public override void Tick()
			{
				_elapsed += Time.deltaTime;

				if (_elapsed >= Duration)
				{
					Ai.Engage();
					return;
				}

				Apply();
			}

			private void Apply()
			{
				// Whole cycles, so the swing starts and ends exactly on the player's head.
				float swing = Amplitude * Mathf.Sin(_elapsed / Duration * Mathf.PI * 2f * CYCLES);
				Vector3 playerHead = mainscript.M.player.Th.position;

				if (_type == GestureType.Nod)
					Ai.LookAtOffset(playerHead, swing, 0f);
				else
					Ai.LookAtOffset(playerHead, 0f, swing);
			}
		}
	}
}
