using NPCs.Enums;
using NPCs.Trading.Core;
using NPCs.Utilities;
using System.Collections.Generic;
using TLDLoader;
using UnityEngine;

namespace NPCs.Trading.Value
{
	public static class ItemRegistry
	{
		private static Dictionary<GameObject, ItemData> _registry;

		public static void Initialise()
		{
			_registry = new Dictionary<GameObject, ItemData>();

			Logging.LogDebug("Item values:");
			foreach (var item in itemdatabase.d.items)
			{
				if (item == null) continue;
				Register(item);
			}

			foreach (var item in ModLoader.Database.GetAllItems())
			{
				if (item == null) continue;
				Register(item);
			}
		}

		public static ItemData GetData(GameObject obj)
		{
			if (obj == null)
				return null;

			// TODO: Doesn't work for starter house items.
			string name = obj.name.Replace("(Clone)", "").Trim();

			foreach (var entry in _registry)
			{
				if (entry.Key.name == name)
				{
					return entry.Value;
				}
			}
			return null;
		}

		public static float GetValue(GameObject obj)
		{
			float baseValue = GetData(obj)?.Value ?? 0;

			if (baseValue == 0)
				return 0;

			var tank = obj.GetComponent<tankscript>();
			if (tank?.F != null)
			{
				foreach (var fluid in tank.F.fluids)
				{
					float fluidValue = BaseValues.GetFluidValue(fluid.type) * fluid.amount;
					baseValue += fluidValue;
				}
			}

			return Maths.RoundToNearestHalf(baseValue);
		}

		private static void Register(GameObject item)
		{
			var rawName = item.name.ToLowerInvariant();
			if (rawName.Contains("cell") || rawName.Contains("gib"))
			{
				_registry[item] = new ItemData()
				{
					Category = ItemCategory.Excluded,
					Value = 0,
					DisplayName = item.name,
				};
				return;
			}
			ItemData data = GetItemData(item);
			data.Value = Maths.RoundToNearestHalf(data.Value);
			_registry[item] = data;
			Logging.LogDebug($"{item.name} [{data.Category}]: {data.Value}g");
		}

		private static ItemData GetItemData(GameObject item)
		{
			// Check for any hardcoded values first.
			if (BaseValues.SetValues.ContainsKey(item.name))
			{
				return new ItemData
				{
					Value = BaseValues.SetValues[item.name].Item1,
					Category = BaseValues.SetValues[item.name].Item2,
					DisplayName = item.name,
				};
			}

			// TODO:
			// - Car part modifier (fury parts might be worth more than 2105 parts for example).

			if (item.GetComponentInChildren<carscript>() != null)
				return new ItemData
				{
					Value = BaseValues.GetChassisValue(item.name),
					Category = BaseValues.GetChassisCategory(item.name),
					DisplayName = item.name,
				};
			if (item.GetComponent<utanfutoscript>() != null)
				return new ItemData { Value = BaseValues.Trailer, Category = ItemCategory.Trailer, DisplayName = item.name };

			// Tank size bonus shared across engines, radiators, and standalone tanks.
			var tank = item.GetComponent<tankscript>();
			float tankValue = 0f;
			if (tank?.F?.maxC != null)
				tankValue = TankCapacityValue(tank.F.maxC);

			if (item.GetComponent<enginescript>() != null)
				return new ItemData { Value = BaseValues.Engine + tankValue, Category = ItemCategory.Engine, DisplayName = item.name };

			if (item.GetComponent<coolantTankscript>() != null)
				return new ItemData { Value = BaseValues.Radiator + tankValue, Category = ItemCategory.Radiator, DisplayName = item.name };

			if (tank != null)
				return new ItemData { Value = BaseValues.Tank + tankValue, Category = ItemCategory.Tank, DisplayName = item.name };

			if (item.GetComponent<wheelscript>() != null)
				return new ItemData { Value = BaseValues.Wheel, Category = ItemCategory.Wheel, DisplayName = item.name };

			if (item.GetComponent<gumiscript>() != null)
				return new ItemData { Value = BaseValues.Tire, Category = ItemCategory.Tire, DisplayName = item.name };

			if (item.GetComponent<meterscript>() != null)
				return new ItemData { Value = BaseValues.Meter, Category = ItemCategory.Meter, DisplayName = item.name };

			if (item.GetComponent<headlightscript>() != null)
				return new ItemData { Value = BaseValues.Light, Category = ItemCategory.Light, DisplayName = item.name };

			if (item.GetComponent<partscript>() != null)
				return new ItemData { Value = BaseValues.Part, Category = ItemCategory.Part, DisplayName = item.name };

			if (item.GetComponent<weaponscript>() != null)
				return new ItemData { Value = BaseValues.Gun, Category = ItemCategory.Gun, DisplayName = item.name };

			if (item.GetComponent<meleeweaponscript>() != null)
				return new ItemData { Value = BaseValues.Melee, Category = ItemCategory.Melee, DisplayName = item.name };

			if (item.GetComponent<drotkefescript>() != null || item.GetComponent<spricniscript>() != null)
				return new ItemData { Value = BaseValues.Cleaning, Category = ItemCategory.Cleaning, DisplayName = item.name };

			if (item.GetComponent<ediblescript>() != null)
				return new ItemData { Value = BaseValues.Food, Category = ItemCategory.Food, DisplayName = item.name };

			if (item.GetComponent<wearable>() != null)
				return new ItemData { Value = BaseValues.Wearable, Category = ItemCategory.Wearable, DisplayName = item.name };

			if (item.GetComponent<usablescript>() != null)
				return new ItemData { Value = BaseValues.Usable, Category = ItemCategory.Usable, DisplayName = item.name };

			return new ItemData { Value = BaseValues.Fallback, Category = ItemCategory.Unknown, DisplayName = item.name };
		}

		private static float TankCapacityValue(float litres, float maxBonus = 1.5f)
		{
			if (litres <= 0f) return 0f;

			// Normalise against a reference size (jerry can = 1.0).
			float normalised = litres / 20f;
			float bonus = Mathf.Log(normalised + 1f, 2f) * (maxBonus / 2f);
			return Mathf.Min(bonus, maxBonus);
		}
	}
}
