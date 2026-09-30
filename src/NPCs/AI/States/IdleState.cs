namespace NPCs.AI.States
{
	public class IdleState : NPCState
	{
		public IdleState(NPCAi ai) : base(ai) { }

		public override void Enter()
		{
			Ai.LookAhead();
		}

		public override void Tick()
		{
			if (Ai.DistanceToPlayer <= Ai.NoticeRange)
				Ai.Engage();
		}
	}
}
