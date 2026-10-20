using UnityEngine;

namespace NPCs.AI.Routes
{
	/// <summary>
	/// An ordered list of points for an NPC to walk through.
	/// Points are looked up each time they're needed rather than kept to avoid having to keep up with origin shifts.
	/// </summary>
	public abstract class NPCRoute
	{
		/// <summary>
		/// True once there's nothing further along the route and never will be. A route that is only waiting for more of itself to load isn't finished.
		/// </summary>
		public abstract bool IsFinished { get; }

		/// <summary>
		/// Looks at a point on the route without moving along it.
		/// </summary>
		/// <param name="ahead">How many points past the current one to look, where 0 is the current point</param>
		/// <param name="point">The point in world space</param>
		/// <returns>False if there's no such point yet</returns>
		public abstract bool TryPeek(int ahead, out Vector3 point);

		/// <summary>
		/// Moves on to the next point. Does nothing if there isn't one yet.
		/// </summary>
		public abstract void Advance();
	}
}
