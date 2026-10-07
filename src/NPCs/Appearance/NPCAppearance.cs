using System.Collections.Generic;

namespace NPCs.Appearance
{
	/// <summary>
	/// Everything needed to dress an NPC. Rolled from a seed for random NPCs, or supplied directly for set ones.
	/// </summary>
	public class NPCAppearance
	{
		/// <summary>
		/// Index of the character model, which is also the <see cref="Enums.Gender"/> value.
		/// </summary>
		public int Character;

		/// <summary>
		/// One entry per segment of the character, in the outfit script's order.
		/// </summary>
		public List<SegmentAppearance> Segments = new List<SegmentAppearance>();
	}
}
