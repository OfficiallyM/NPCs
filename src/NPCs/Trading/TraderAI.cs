using NPCs.AI;

namespace NPCs.Trading
{
	internal class TraderAi : NPCAi
	{
		// Traders turn to face the player well before chat range so they're ready when the player arrives.
		public override float NoticeRange => 15f;

		public override float LoseRange => 20f;

		// Puts the trader on the edge of the road, to the right of the way they're walking.
		public override float RoadLaneOffset => 4.1f;
	}
}
