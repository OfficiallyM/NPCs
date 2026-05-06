using System.Collections.Generic;
using TLDLoader;
using NPCs.Utilities;
using UnityEngine;

namespace NPCs.Trading
{
	public static class ItemValue
	{
		private static Dictionary<GameObject, float> _registry;

		public static void Initialise()
		{
			_registry = new Dictionary<GameObject, float>();

			Logging.LogDebug("Item values:");
			foreach (var item in itemdatabase.d.items)
			{
				if (item == null)
					continue;

				_registry[item] = Maths.RoundToNearestHalf(CalculateBaseValue(item));
				Logging.LogDebug($"{item.name}: {_registry[item]}");
			}

			foreach (var item in ModLoader.Database.GetAllItems())
			{
				if (item == null)
					continue;

				_registry[item] = Maths.RoundToNearestHalf(CalculateBaseValue(item));
				Logging.LogDebug($"{item.name}: {_registry[item]}");
			}
		}

		public static float GetValue(GameObject obj)
		{
			if (obj == null)
				return 0;

			// TODO: Doesn't work for starter house items.
			// TODO: Add up value for any parts in a child part slot.
			string name = obj.name.Replace("(Clone)", "").Trim();

			float baseValue = 0f;
			foreach (var entry in _registry)
			{
				if (entry.Key.name == name)
				{
					baseValue = entry.Value;
					break;
				}
			}

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

		private static float CalculateBaseValue(GameObject item)
		{
			// Check for any hardcoded values first.
			if (BaseValues.SetValues.ContainsKey(item.name))
				return BaseValues.SetValues[item.name];

			// TODO:
			// - Car part modifier (fury parts might be worth more than 2105 parts for example).

			if (item.GetComponentInChildren<carscript>() != null)
				return BaseValues.GetChassisValue(item.name);
			if (item.GetComponent<utanfutoscript>() != null)
				return BaseValues.Trailer;

			var tank = item.GetComponent<tankscript>();
			float tankValue = 0;
			if (tank?.F?.maxC != null)
				tankValue = TankCapacityValue(tank.F.maxC);

			if (item.GetComponent<enginescript>() != null)
				return BaseValues.Engine + tankValue;
			var coolant = item.GetComponent<coolantTankscript>();
			if (coolant != null) 
				return BaseValues.Radiator + tankValue;
			if (tank != null)
				return BaseValues.Tank + tankValue;
			if (item.GetComponent<wheelscript>() != null) 
				return BaseValues.Wheel;
			if (item.GetComponent<gumiscript>() != null)
				return BaseValues.Tire;
			if (item.GetComponent<meterscript>() != null) 
				return BaseValues.Meter;
			if (item.GetComponent<headlightscript>() != null) 
				return BaseValues.Light;
			if (item.GetComponent<partscript>() != null)
				return BaseValues.Part;

			if (item.GetComponent<weaponscript>() != null) 
				return BaseValues.Gun;
			if (item.GetComponent<meleeweaponscript>() != null) 
				return BaseValues.Melee;
			if (item.GetComponent<drotkefescript>() != null ||
				item.GetComponent<spricniscript>() != null) 
				return BaseValues.Cleaning;
			if (item.GetComponent<ediblescript>() != null) 
				return BaseValues.Food;
			if (item.GetComponent<wearable>() != null)
				return BaseValues.Wearable;
			if (item.GetComponent<usablescript>() != null)
				return BaseValues.Usable;

			return BaseValues.Fallback;
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
