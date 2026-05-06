using NPCs.Enums;

namespace NPCs.Trading
{
	public class ItemData
	{
		/// <summary>
		/// Base gold bar value, excluding fluid contents.
		/// </summary>
		public float Value { get; set; }

		/// <summary>
		/// Item category.
		/// </summary>
		public ItemCategory Category { get; set; }

		/// <summary>
		/// Prettified display name.
		/// </summary>
		public string DisplayName { get; set; }
	}
}
