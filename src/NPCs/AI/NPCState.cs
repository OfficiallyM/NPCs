namespace NPCs.AI
{
	public abstract class NPCState
	{
		public NPCAi Ai { get; private set; }

		protected NPCState(NPCAi ai)
		{
			Ai = ai;
		}

		public virtual void Enter() { }

		public virtual void Tick() { }

		public virtual void Exit() { }
	}
}
