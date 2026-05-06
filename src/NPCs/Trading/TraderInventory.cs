using NPCs.Common;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Trading
{
	/// <summary>
	/// Holds and manages a trader's current stock.
	/// Stock is generated procedurally at spawn using the trader's seeded RNG.
	/// </summary>
	internal class TraderInventory
	{
		private System.Random _rng;

		public TraderInventory(System.Random rng)
		{
			_rng = rng;
		}

		/// <summary>
		/// The trader's current stock as live scene instances.
		/// </summary>
		public List<GameObject> Items { get; private set; } = new List<GameObject>();

		/// <summary>
		/// Generates stock for this trader.
		/// </summary>
		/// <param name="count">Number of items to generate.</param>
		public void Generate(int count = 6)
		{
			Items.Clear();

			var pool = BuildItemPool();
			if (pool.Count == 0) return;

			for (int i = 0; i < count; i++)
			{
				Items.Add(pool[_rng.Next(pool.Count)]);
			}
		}

		/// <summary>
		/// Destroys all stock instances. Call on trader despawn.
		/// </summary>
		public void Destroy()
		{
			foreach (GameObject item in Items)
			{
				if (item != null)
					Object.Destroy(item);
			}

			Items.Clear();
		}

		/// <summary>
		/// Removes an item from stock after a successful trade.
		/// </summary>
		/// <param name="item">Item to remove.</param>
		public void Remove(GameObject item)
		{
			Items.Remove(item);
		}

		private List<GameObject> BuildItemPool()
		{
			// Exclude vehicles, NPCs, and zero-value items from the tradeable pool.
			var pool = new List<GameObject>();

			foreach (GameObject item in itemdatabase.d.items)
			{
				if (item == null) continue;
				if (item.GetComponentInChildren<carscript>() != null) continue;
				if (item.GetComponentInChildren<utanfutoscript>() != null) continue;
				if (item.GetComponent<NPC>() != null) continue;
				if (ItemValue.GetValue(item) <= 0f) continue;

				pool.Add(item);
			}

			return pool;
		}
	}
}