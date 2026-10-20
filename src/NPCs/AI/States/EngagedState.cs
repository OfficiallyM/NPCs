namespace NPCs.AI.States
{
	public class EngagedState : NPCState
	{
		public EngagedState(NPCAi ai) : base(ai) { }

		public override void Tick()
		{
			var player = mainscript.M.player;
			Ai.Animation.LookAt(player.Th.position);
			Face(player.transform.position);

			// A conversation is ended by the runner's own range check, so don't walk off mid-sentence.
			if (!Ai.InConversation && Ai.DistanceToPlayer > Ai.LoseRange)
			{
				Ai.Rest();
				return;
			}

			OnEngaged();
		}

		// How the body is turned to face the player. The head leads by default, and states that need the body facing sooner can override it.
		protected virtual void Face(UnityEngine.Vector3 point) => Ai.FaceTowards(point);

		// Runs every tick while the player is still in range, for states that build on this one.
		protected virtual void OnEngaged() { }

		public override void Exit()
		{
			Ai.Animation.LookAhead();
		}
	}
}
