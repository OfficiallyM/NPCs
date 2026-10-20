namespace NPCs.AI.States
{
	public class IdleState : NPCState
	{
		public IdleState(NPCAi ai) : base(ai) { }

		public override void Enter()
		{
			Ai.Animation.LookAhead();
		}

		public override void Tick()
		{
			if (!Ai.InviteSpent && Ai.DistanceToPlayer <= Ai.NoticeRange)
				Ai.Engage();
		}
	}
}
