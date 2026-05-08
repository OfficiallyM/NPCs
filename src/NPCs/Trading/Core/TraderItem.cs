using UnityEngine;

namespace NPCs.Trading.Core
{
	/// <summary>
	/// Represents a single item in a trader's inventory with condition and colour state.
	/// </summary>
	public struct TraderItem
	{
		/// <summary>
		/// The source prefab for this item.
		/// </summary>
		public GameObject Prefab;

		/// <summary>
		/// Item data including value, category and display name.
		/// </summary>
		public ItemData Data;

		/// <summary>
		/// Condition state 0-4. 0 is pristine, 4 is rusty.
		/// </summary>
		public int? Condition;

		/// <summary>
		/// Colour to apply on spawn. Picked from the item's available colours at generate time.
		/// </summary>
		public Color? Color;

		/// <summary>
		/// Number of this item in stock at this condition and colour.
		/// </summary>
		public int Quantity;
	}
}
