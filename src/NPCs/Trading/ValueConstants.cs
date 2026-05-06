using NPCs.Enums;
using System.Collections.Generic;

namespace NPCs.Trading
{
	public static class BaseValues
	{
		public const float Fallback = 0.5f;

		// Vehicle/Parts.
		public const float BigChassis = 10f;
		public const float SmallChassis = 7f;
		public const float BikeChassis = 5f;
		public const float Trailer = 2.5f;
		public const float Part = 1f;
		public const float Engine = 3.5f;
		public const float Radiator = 2.5f;
		public const float Tank = 2f;
		public const float Wheel = 1.5f;
		public const float Tire = 1.5f;
		public const float Meter = 0.5f;
		public const float Light = 0.5f;

		// Other.
		public const float Gun = 2f;
		public const float Melee = 1.5f;
		public const float Cleaning = 0.5f;
		public const float Food = 0.5f;
		public const float Wearable = 0.5f;
		public const float Usable = 0.5f;

		// Fluids.
		public const float Gas = 0.25f;
		public const float Oil = 0.2f;
		public const float Alcohol = 0.1f;
		public const float Water = 0.175f;
		public const float Blood = 0.35f;
		public const float Diesel = 0.225f;

		// Hardcoded values.
		public static Dictionary<string, (float, ItemCategory)> SetValues = new Dictionary<string, (float, ItemCategory)>()
		{
			{ "gold", (1f, ItemCategory.Currency) },
			{ "silver", (0.5f, ItemCategory.Currency) },
			{ "Nyul", (0f, ItemCategory.Excluded) },
			{ "Munkas01", (0f, ItemCategory.Excluded) },
			{ "Trader", (0f, ItemCategory.Excluded) },
			{ "Broom", (2.5f, ItemCategory.Usable) },
		};

		// Values to determine constants.
		private static string[] _bigVehicles = new string[]
		{
			"bus01",
			"bus02",
			"bus03",
			"car07",
			"car09T",
			"car11",
		};
		private static string[] _bikes = new string[]
		{
			"bike01",
			"bike03",
		};

		public static float GetChassisValue(string name)
		{
			name = name.ToLowerInvariant();
			foreach (var big in _bigVehicles)
				if (name.Contains(big))
					return BigChassis;
			foreach (var bike in _bikes)
				if (name.Contains(bike))
					return BikeChassis;
			return SmallChassis;
		}

		public static ItemCategory GetChassisCategory(string name)
		{
			name = name.ToLowerInvariant();
			foreach (string big in _bigVehicles)
				if (name.Contains(big)) return ItemCategory.BigChassis;
			foreach (string bike in _bikes)
				if (name.Contains(bike)) return ItemCategory.BikeChassis;
			return ItemCategory.SmallChassis;
		}

		public static float GetFluidValue(mainscript.fluidenum fluid)
		{
			switch (fluid)
			{
				case mainscript.fluidenum.gas:	   return Gas;
				case mainscript.fluidenum.oil:	   return Oil;
				case mainscript.fluidenum.alcohol: return Alcohol;
				case mainscript.fluidenum.water:   return Water;
				case mainscript.fluidenum.blood:   return Blood;
				case mainscript.fluidenum.diesel:  return Diesel;
				default:						   return 0;
			}
		}
	}
}
