using System.Collections.Generic;

namespace Traders.Trading
{
	public static class BaseValues
	{
		public const float Fallback = 0.25f;

		// Vehicle/Parts.
		public const float BigChassis = 10f;
		public const float SmallChassis = 7f;
		public const float BikeChassis = 5f;
		public const float Trailer = 2.5f;
		public const float Part = 1f;
		public const float Engine = 3.5f;
		public const float Radiator = 2.5f;
		public const float Tank = 2f;
		public const float Wheel = 1.25f;
		public const float Tire = 1.25f;
		public const float Meter = 0.2f;
		public const float Light = 0.5f;

		// Other.
		public const float Gun = 2f;
		public const float Melee = 1.3f;
		public const float Cleaning = 0.4f;
		public const float Food = 0.5f;
		public const float Wearable = 0.2f;
		public const float Usable = 0.3f;

		// Fluids.
		public const float Gas = 1.5f;
		public const float Oil = 1.2f;
		public const float Alcohol = 0.6f;
		public const float Water = 1f;
		public const float Blood = 2f;
		public const float Diesel = 1.25f;

		// Hardcoded values.
		public static Dictionary<string, float> SetValues = new Dictionary<string, float>()
		{
			{ "gold", 1f },
			{ "Nyul", 0f },
			{ "Munkas01", 0f },
			{ "Trader", 0f },
			{ "Broom", 2.5f },
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

		public static float GetFluidValue(mainscript.fluidenum fluid)
		{
			return fluid switch
			{
				mainscript.fluidenum.gas => Gas,
				mainscript.fluidenum.oil => Oil,
				mainscript.fluidenum.alcohol => Alcohol,
				mainscript.fluidenum.water => Water,
				mainscript.fluidenum.blood => Blood,
				mainscript.fluidenum.diesel => Diesel,
				_ => 0,
			};
		}
	}
}
