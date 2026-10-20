using NPCs.AI.Enums;
using NPCs.AI.Routes;

namespace NPCs.AI.States
{
	/// <summary>
	/// Walks along a route, then goes back to resting once it ends or the NPC gives up. The body faces the way it's travelling and the head looks ahead.
	/// </summary>
	public class TravelState : NPCState
	{
		private readonly NPCRoute _route;
		private readonly NPCSpeed _speed;

		public TravelState(NPCAi ai, NPCRoute route, NPCSpeed speed) : base(ai)
		{
			_route = route;
			_speed = speed;
		}

		/// <summary>
		/// The follower doing the walking, for the debugger. Null until the state has been entered.
		/// </summary>
		public NPCRouteFollower Follower { get; private set; }

		public override void Enter()
		{
			Ai.Animation.LookAhead();
			Follower = new NPCRouteFollower(Ai.Locomotion, _route, _speed);
			Follower.Start();
		}

		// The look point is a fixed spot in the world, so it has to be refreshed every frame or the head ends up staring at ground the NPC has already walked past.
		public override void Tick()
		{
			Ai.Animation.LookAhead();
			Follower.Tick(Ai.transform.position);

			if (Follower.Status == NPCRouteStatus.Finished || Follower.Status == NPCRouteStatus.Stuck)
				Ai.Rest();
		}

		public override void Exit()
		{
			Follower.Dispose();

			// Another state may be taking over mid-walk, in which case the NPC should slow down rather than carry on towards a target that's no longer being moved.
			if (Ai.Locomotion.HasDestination)
				Ai.Locomotion.Stop();
		}
	}
}
