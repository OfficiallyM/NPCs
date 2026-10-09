using NPCs.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Appearance
{
	public static class AppearanceGenerator
	{
		private const double SEGMENT_ENABLED_CHANCE = 0.75;
		private const double HEADWEAR_CHANCE = 0.35;
		private const double GLASSES_CHANCE = 0.2;
		private const double MASK_CHANCE = 0.2;

		/// <param name="outfit">The outfit script to read segment counts from.</param>
		/// <param name="seed">The same seed always gives the same appearance.</param>
		/// <param name="gender">Forces a gender, or leaves it to the seed when null.</param>
		public static NPCAppearance Generate(playermodeloutfitscript outfit, int seed, Gender? gender = null)
		{
			var rng = new System.Random(seed);

			// Always drawn, so a forced gender doesn't shift every roll after it.
			Gender rolled = (Gender)rng.Next(2);
			int character = (int)(gender ?? rolled);

			var segments = outfit.characters[character].ch.segments;
			var palettes = mainscript.M.randomRagdolColors;
			var palette = palettes[Mathf.Clamp(character, 0, palettes.Length - 1)];

			var appearance = new NPCAppearance
			{
				Character = character,
				Segments = new List<SegmentAppearance>(segments.Length),
			};

			for (int i = 0; i < segments.Length; i++)
			{
				bool enabled = rng.NextDouble() < SEGMENT_ENABLED_CHANCE;
				Color colour = RollColour(rng, i < palette.color.Length ? palette.color[i].color : null);

				appearance.Segments.Add(new SegmentAppearance
				{
					Enabled = enabled,
					R = colour.r,
					G = colour.g,
					B = colour.b,
				});
			}

			// Rolled last so adding wearables didn't change how anyone's outfit already looks.
			RollWearables(rng, appearance);

			return appearance;
		}

		private static void RollWearables(System.Random rng, NPCAppearance appearance)
		{
			// Only one thing goes on top of the head.
			var headwear = new List<GameObject>();
			headwear.AddRange(WearableCatalog.OfType(wearableType.hat));
			headwear.AddRange(WearableCatalog.OfType(wearableType.cap));
			headwear.AddRange(WearableCatalog.OfType(wearableType.helmet));

			RollWearable(rng, appearance, HEADWEAR_CHANCE, headwear);
			RollWearable(rng, appearance, GLASSES_CHANCE, WearableCatalog.OfType(wearableType.glasses));
			RollWearable(rng, appearance, MASK_CHANCE, WearableCatalog.OfType(wearableType.mask));
		}

		private static void RollWearable(System.Random rng, NPCAppearance appearance, double chance, List<GameObject> pool)
		{
			// All three draws always happen, so an empty pool or a miss doesn't shift the rolls after it.
			bool wears = rng.NextDouble() < chance;
			int index = rng.Next(System.Math.Max(pool.Count, 1));
			int variant = rng.Next();

			if (wears && pool.Count > 0)
				appearance.Wearables.Add(new WornItem { Item = pool[index].name, Variant = variant });
		}

		// Segments with no palette get any colour at all, like the game does.
		private static Color RollColour(System.Random rng, Gradient[] gradients)
		{
			if (gradients == null || gradients.Length == 0)
				return new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), 1f);

			return gradients[rng.Next(gradients.Length)].Evaluate((float)rng.NextDouble());
		}
	}
}
