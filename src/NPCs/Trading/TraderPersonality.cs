using NPCs.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Trading
{
	/// <summary>
	/// Holds all personality rolls for a trader instance.
	/// </summary>
	public class TraderPersonality
	{
		/// <summary>
		/// Per category price fluctuation. Positive values mean the trader values
		/// that category more than base, negative means less.
		/// </summary>
		public Dictionary<ItemCategory, float> ItemFluctuation { get; }

		/// <summary>
		/// Biases all condition tier discounts in the same direction.
		/// 0 = lenient, 1 = strict.
		/// </summary>
		public float ConditionSensitivity { get; }

		/// <summary>
		/// Per condition tier discount multipliers. Index matches condition tier 0-4.
		/// </summary>
		public float[] ConditionDiscounts { get; }

		/// <summary>
		/// The lowest percentage of fair value this trader will accept, 0-1.
		/// </summary>
		public float MinimumDealThreshold { get; }

		public TraderPersonality(System.Random rng)
		{
			ConditionSensitivity = (float)rng.NextDouble();
			ConditionDiscounts = RollConditionDiscounts(rng, ConditionSensitivity);
			MinimumDealThreshold = Mathf.Lerp(0.6f, 1.0f, (float)rng.NextDouble());
			ItemFluctuation = RollCategoryFluctuation(rng);
		}

		/// <summary>
		/// Rolls condition discounts for each tier, influenced by sensitivity.
		/// Higher sensitivity produces steeper discounts at lower tiers.
		/// </summary>
		private float[] RollConditionDiscounts(System.Random rng, float sensitivity)
		{
			// Discount ranges per tier.
			// Each entry is (minDiscount, maxDiscount).
			// Ranges widen at lower tiers.
			var ranges = new (float min, float max)[]
			{
				(0.00f, 0.00f), // Tier 0 - Pristine, always full value.
                (0.05f, 0.15f), // Tier 1 - Dull.
                (0.10f, 0.30f), // Tier 2 - Rough.
                (0.20f, 0.50f), // Tier 3 - Crusty.
                (0.35f, 0.70f), // Tier 4 - Rusty.
            };

			float[] discounts = new float[ranges.Length];
			for (int i = 0; i < ranges.Length; i++)
			{
				(float min, float max) = ranges[i];

				// Bias the roll towards the high end for sensitive traders.
				float raw = (float)rng.NextDouble();
				float biased = Mathf.Lerp(raw, 1f, sensitivity * 0.5f);
				discounts[i] = Mathf.Lerp(min, max, biased);
			}

			return discounts;
		}

		private Dictionary<ItemCategory, float> RollCategoryFluctuation(System.Random rng)
		{
			var fluctuation = new Dictionary<ItemCategory, float>();
			foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
			{
				// Roll between -0.5 and +0.5.
				float roll = Mathf.Lerp(-0.5f, 0.5f, (float)rng.NextDouble());
				fluctuation[category] = roll;
			}
			return fluctuation;
		}
	}
}
