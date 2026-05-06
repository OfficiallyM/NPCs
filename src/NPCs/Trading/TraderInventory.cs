using NPCs.Common;
using NPCs.Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCs.Trading
{
	/// <summary>
	/// Holds and manages a trader's current stock.
	/// Stock is generated procedurally at spawn using the trader's seeded RNG.
	/// </summary>
	internal class TraderInventory
	{
		private readonly System.Random _rng;

		public TraderInventory(System.Random rng)
		{
			_rng = rng;
		}

		/// <summary>
		/// The trader's current stock..
		/// </summary>
		public Dictionary<GameObject, ItemData> Items { get; private set; } = new Dictionary<GameObject, ItemData>();
		private static Dictionary<GameObject, ItemData> _pool = new Dictionary<GameObject, ItemData>();

		/// <summary>
		/// Generates stock for this trader.
		/// </summary>
		/// <param name="count">Number of items to generate.</param>
		public void Generate(int count = 6)
		{
			Items.Clear();

			if (_pool.Count == 0)
				BuildItemPool();

			if (_pool.Count == 0) return;

			for (int i = 0; i < count; i++)
			{
				var entry = _pool.ElementAt(_rng.Next(_pool.Count));
				Items.Add(entry.Key, entry.Value);
			}
		}

		/// <summary>
		/// Removes an item from stock after a successful trade.
		/// </summary>
		/// <param name="item">Item to remove.</param>
		public void Remove(GameObject item)
		{
			Items.Remove(item);
		}

		private void BuildItemPool()
		{
			// Exclude vehicles, NPCs, and zero-value items from the tradeable pool.
			foreach (GameObject item in itemdatabase.d.items)
			{
				if (item == null) continue;
				var data = ItemRegistry.GetData(item);
				if (data.Category == ItemCategory.BigChassis) continue;
				if (data.Category == ItemCategory.SmallChassis) continue;
				if (data.Category == ItemCategory.BikeChassis) continue;
				if (data.Category == ItemCategory.Trailer) continue;
				if (data.Category == ItemCategory.Excluded) continue;
				if (data.Category == ItemCategory.Currency) continue;
				if (data.Value <= 0f) continue;

				_pool.Add(item, data);
			}
		}
	}
}