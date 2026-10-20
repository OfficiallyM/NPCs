namespace NPCs.AI.Enums
{
	/// <summary>
	/// What an NPC following a route is doing.
	/// </summary>
	public enum NPCRouteStatus
	{
		/// <summary>
		/// Walking along the route.
		/// </summary>
		Following,

		/// <summary>
		/// Standing still because the next part of the route doesn't exist yet.
		/// </summary>
		Waiting,

		/// <summary>
		/// Reached the end of the route.
		/// </summary>
		Finished,

		/// <summary>
		/// Gave up after failing to make progress.
		/// </summary>
		Stuck,
	}
}
