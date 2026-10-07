using NPCs.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Appearance
{
	public static class AppearanceGenerator
	{
		private const double SEGMENT_ENABLED_CHANCE = 0.75;

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

			return appearance;
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
