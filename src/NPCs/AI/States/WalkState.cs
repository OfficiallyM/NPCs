using NPCs.AI.Enums;
using UnityEngine;

namespace NPCs.AI.States
{
	/// <summary>
	/// Walks to a point and then goes back to resting. The body faces the way it's travelling and the head looks ahead.
	/// </summary>
	public class WalkState : NPCState
	{
		private readonly Vector3 _destination;
		private readonly NPCSpeed _speed;

		public WalkState(NPCAi ai, Vector3 destination, NPCSpeed speed) : base(ai)
		{
			_destination = destination;
			_speed = speed;
		}

		public override void Enter()
		{
			Ai.Animation.LookAhead();
			Ai.Locomotion.Arrived += OnFinished;
			Ai.Locomotion.Stuck += OnFinished;
			Ai.Locomotion.MoveTo(_destination, _speed);
		}

		// The look point is a fixed spot in the world, so it has to be refreshed every frame or the head ends up staring at ground the NPC has already walked past.
		public override void Tick()
		{
			Ai.Animation.LookAhead();
		}

		public override void Exit()
		{
			Ai.Locomotion.Arrived -= OnFinished;
			Ai.Locomotion.Stuck -= OnFinished;

			// Another state may be taking over mid-walk, in which case the NPC should slow down rather than carry on to the old destination.
			if (Ai.Locomotion.HasDestination)
				Ai.Locomotion.Stop();
		}

		private void OnFinished()
		{
			Ai.Rest();
		}
	}
}
