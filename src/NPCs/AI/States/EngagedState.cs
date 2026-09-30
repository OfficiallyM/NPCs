namespace NPCs.AI.States
{
	public class EngagedState : NPCState
	{
		public EngagedState(NPCAi ai) : base(ai) { }

		public override void Tick()
		{
			var player = mainscript.M.player;
			Ai.LookAt(player.Th.position);
			Ai.FaceTowards(player.transform.position);

			// A conversation is ended by the runner's own range check, so don't walk off mid-sentence.
			if (!Ai.InConversation && Ai.DistanceToPlayer > Ai.LoseRange)
				Ai.Rest();
		}

		public override void Exit()
		{
			Ai.LookAhead();
		}
	}
}
