namespace NPCs.Utilities
{
	internal static class Trade
	{
		public static string ConditionTag(int? condition)
		{
			switch (condition)
			{
				case 0: return "[NEW]";
				case 1: return "[USED]";
				case 2: return "[WORN]";
				case 3: return "[OLD]";
				case 4: return "[RUSTY]";
				default: return "";
			}
		}
	}
}
