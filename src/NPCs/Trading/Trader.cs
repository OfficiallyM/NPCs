using NPCs.Common;

namespace NPCs.Trading
{
	internal class Trader : NPC
	{
		public TraderPersonality Personality { get; private set; }
		public TraderInventory Inventory { get; private set; }

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
			string[] adjectives = new string[]
			{
				"Dodgy", "Shifty", "Crooked", "Sly", "Shady",
				"Slippery", "Lucky", "Greasy", "Honest"
			};

			string name = base.GenerateName();

			if (Rng.Next(3) == 0)
				name = $"{adjectives[Rng.Next(adjectives.Length)]} {name}";

			return name;
		}
	}
}
