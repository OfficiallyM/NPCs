using NPCs.Common;
using NPCs.Enums;
using NPCs.Trading.Core;
using NPCs.Trading.Value;
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
		/// The trader's current stock.
		/// </summary>
		public List<TraderItem> Items { get; private set; } = new List<TraderItem>();
		private static Dictionary<GameObject, ItemData> _pool = new Dictionary<GameObject, ItemData>();

		/// <summary>
		/// Generates stock for this trader.
		/// </summary>
		/// <param name="count">Number of items to generate.</param>
		public void Generate(int count = 10)
		{
			Items.Clear();

			if (_pool.Count == 0)
				BuildItemPool();

			if (_pool.Count == 0) return;

			for (int i = 0; i < count; i++)
			{
				var entry = _pool.ElementAt(_rng.Next(_pool.Count));
				var conditionScript = entry.Key.GetComponentInChildren<partconditionscript>();
				int? condition = null;
				Color? color = null;
				if (conditionScript != null)
				{
					condition = _rng.Next(0, 5);
					if (conditionScript.CanPaint())
						color = RollColor(entry.Key);
				}
				int quantity = RollQuantity(entry.Value.Category);

				// Find existing entry with same prefab, condition and color to stack quantity.
				int existingIndex = Items.FindIndex(i => i.Prefab == entry.Key && i.Condition == condition);
				if (existingIndex >= 0)
				{
					TraderItem existing = Items[existingIndex];
					existing.Quantity += quantity;
					Items[existingIndex] = existing;
				}
				else
				{
					Items.Add(new TraderItem
					{
						Prefab = entry.Key,
						Data = entry.Value,
						Condition = condition,
						Color = color,
						Quantity = quantity,
					});
				}
			}
		}

		/// <summary>
		/// Adds a live scene instance to the trader's stock.
		/// Reads condition and colour from the instance's partconditionscript if present.
		/// </summary>
		/// <param name="item">Live scene instance to add.</param>
		/// <param name="data">Item data for the instance.</param>
		public void Add(GameObject item, ItemData data)
		{
			if (data.Category == ItemCategory.Currency) return;

			string name = item.name.Replace("(Clone)", "").Trim();
			GameObject prefab = _pool.Keys.FirstOrDefault(k => k.name == name);
			if (prefab == null) return;

			// Read condition and colour from live instance if available.
			partconditionscript condition = item.GetComponentInChildren<partconditionscript>();
			int? conditionState = condition?.state;
			Color? color = null;
			if (condition != null && condition.CanPaint())
				color = condition.color;

			int existingIndex = Items.FindIndex(i => i.Prefab == prefab && i.Condition == conditionState && i.Color == color);
			if (existingIndex >= 0)
			{
				TraderItem existing = Items[existingIndex];
				existing.Quantity++;
				Items[existingIndex] = existing;
			}
			else
			{
				Items.Add(new TraderItem
				{
					Prefab = prefab,
					Data = data,
					Condition = conditionState,
					Color = color,
					Quantity = 1,
				});
			}
		}

		/// <summary>
		/// Decrements the quantity of an item at a specific index.
		/// Removes the entry entirely if quantity reaches zero.
		/// </summary>
		/// <param name="index">Index of the item in the list.</param>
		/// <param name="quantity">Quantity to remove.</param>
		public void Remove(int index, int quantity = 1)
		{
			if (index < 0 || index >= Items.Count) return;

			TraderItem item = Items[index];
			int updated = item.Quantity - quantity;

			if (updated <= 0)
				Items.RemoveAt(index);
			else
			{
				item.Quantity = updated;
				Items[index] = item;
			}
		}

		private Color RollColor(GameObject prefab)
		{
			partconditionscript condition = prefab.GetComponentInChildren<partconditionscript>();
			if (condition == null) return Color.white;

			// Replicate FStart material lookup to get colors without requiring FStart to have run.
			string tipus = condition.useOnlyMaterialTipusForMaterial ? condition.materialTipus : condition.tipus;

			foreach (mainscript.conditionmaterial mat in mainscript.M.conditionmaterials)
			{
				if (mat.tipus != tipus) continue;
				if (mat.colors == null || mat.colors.Count == 0) return Color.white;
				return mat.colors[_rng.Next(mat.colors.Count)];
			}

			return Color.white;
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

		private int RollQuantity(ItemCategory category)
		{
			switch (category)
			{
				case ItemCategory.BigChassis:
				case ItemCategory.SmallChassis:
				case ItemCategory.BikeChassis:
				case ItemCategory.Trailer:
					return 1;

				case ItemCategory.Engine:
				case ItemCategory.Radiator:
				case ItemCategory.Tank:
				case ItemCategory.Wheel:
				case ItemCategory.Tire:
				case ItemCategory.Meter:
				case ItemCategory.Light:
				case ItemCategory.Part:
				case ItemCategory.Gun:
				case ItemCategory.Melee:
					return _rng.Next(1, 4);

				default:
					return _rng.Next(1, 7);
			}
		}
	}
}