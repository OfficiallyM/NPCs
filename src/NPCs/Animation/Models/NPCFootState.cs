using UnityEngine;

namespace NPCs.Animation.Models
{
	/// <summary>
	/// What one foot is doing this frame. Only the gait writes it; the debugger reads it so the foot can be drawn.
	/// </summary>
	public sealed class NPCFootState
	{
		/// <summary>
		/// Where the ankle is being pulled to.
		/// </summary>
		public Vector3 Target { get; internal set; }

		/// <summary>
		/// The point on the ground under the foot.
		/// </summary>
		public Vector3 Ground { get; internal set; }

		/// <summary>
		/// Where the ankle bone actually ended up after solving.
		/// </summary>
		public Vector3 Position { get; internal set; }

		/// <summary>
		/// True while the foot is off the ground, swinging forward.
		/// </summary>
		public bool Swinging { get; internal set; }

		/// <summary>
		/// False when the ground ray found nothing, so the foot fell back to the NPC's own height.
		/// </summary>
		public bool GroundHit { get; internal set; }

		public bool SolveSucceeded { get; internal set; }
	}
}
