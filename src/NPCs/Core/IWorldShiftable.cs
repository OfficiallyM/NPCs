using UnityEngine;

namespace NPCs.Core
{
	/// <summary>
	/// Implemented by anything on an NPC that remembers a position in the world to cope
	/// with the game origin shifting.
	/// </summary>
	public interface IWorldShiftable
	{
		/// <summary>
		/// Called straight after the game has moved the NPC.
		/// </summary>
		/// <param name="delta">How far the NPC moved, and with it everything else in the world</param>
		void OnWorldShifted(Vector3 delta);
	}
}
