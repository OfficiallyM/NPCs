using NPCs.Common;

namespace NPCs.Trading
{
	internal class Trader : NPC
	{
		public TraderPersonality Personality { get; private set; }

		protected override void Start()
		{
			base.Start();
			Personality = new TraderPersonality(Rng);
		}

		protected override string GenerateName()
		{
			return "Geoff";
		}
	}
}
