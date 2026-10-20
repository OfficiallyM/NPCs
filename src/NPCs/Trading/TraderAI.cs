using NPCs.AI;
using NPCs.AI.Enums;
using NPCs.AI.States;
using NPCs.Utilities;

namespace NPCs.Trading
{
	internal class TraderAi : NPCAi
	{
		// Traders turn to face the player well before chat range so they're ready when the player arrives.
		public override float NoticeRange => 7.5f;

		public override float LoseRange => 15f;

		// Puts the trader on the edge of the road, to the right of the way they're walking.
		public override float RoadLaneOffset => 4.1f;

		// Traders hurry along the road and ease off as the player gets near.
		public override NPCSpeed CruiseSpeed => NPCSpeed.WalkFast;

		public override NPCSpeed SlowSpeed => NPCSpeed.Walk;

		public override bool StopsForPlayer => true;

		public override float SlowRange => 30f;

		// Traders spend their time walking the road. One that can't find any road stands where it is instead.
		public override void Rest()
		{
			if (TravelRoad(CruiseSpeed))
				return;

			Logging.LogWarning("No placed road near " + name + ", so it has nowhere to walk.");
			base.Rest();
		}

		// Stops and waves to invite the player over, rather than only turning to face them.
		public override void Engage() => SetState(new InviteState(this));
	}
}
