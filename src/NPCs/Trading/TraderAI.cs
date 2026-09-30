using NPCs.AI;

namespace NPCs.Trading
{
	internal class TraderAi : NPCAi
	{
		// Traders turn to face the player well before chat range so they're ready when the player arrives.
		public override float NoticeRange => 15f;

		public override float LoseRange => 20f;
	}
}
