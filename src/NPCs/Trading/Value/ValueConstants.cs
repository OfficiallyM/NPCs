using NPCs.Enums;
using System.Collections.Generic;

namespace NPCs.Trading.Value
{
	public static class ValueConstants
	{
		public const float Fallback = 0.5f;

		// Vehicle/Parts.
		public const float BigChassis = 40f;
		public const float SmallChassis = 25f;
		public const float BikeChassis = 15f;
		public const float Trailer = 10f;
		public const float Part = 4f;
		public const float Engine = 15f;
		public const float Radiator = 8f;
		public const float Tank = 6f;
		public const float Wheel = 5f;
		public const float Tire = 4f;
		public const float Meter = 2f;
		public const float Light = 2f;

		// Other.
		public const float Gun = 8f;
		public const float Melee = 5f;
		public const float Cleaning = 2f;
		public const float Food = 2f;
		public const float Wearable = 2f;
		public const float Usable = 2f;

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
