using UnityEngine;

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

		// Override if the state remembers a world position. Most states re-read what they need every tick and can ignore this.
		public virtual void OnWorldShifted(Vector3 delta) { }
	}
}
