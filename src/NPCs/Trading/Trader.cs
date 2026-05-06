using NPCs.Common;

namespace NPCs.Trading
{
	internal class Trader : NPC
	{
		public TraderPersonality Personality { get; private set; }
		public TraderInventory Inventory { get; private set; }
		public bool NaturalSpawned = false;

		protected override void Start()
		{
			base.Start();
			Personality = new TraderPersonality(Rng);
			Inventory = new TraderInventory(Rng);
			Inventory.Generate();
			GetComponent<TradeSession>().Init();
		}

		protected override string GenerateName()
		{
			return "Geoff";
		}
	}
}
