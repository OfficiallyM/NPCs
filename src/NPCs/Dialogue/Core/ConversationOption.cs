namespace NPCs.Dialogue.Core
{
	/// <summary>
	/// A single player response option within a conversation node.
	/// </summary>
	public sealed class ConversationOption
	{
		/// <summary>
		/// The text displayed to the player for this option.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// The next node ID to advance to automatically.
		/// </summary>
		/// <remarks>Null closes the conversation</remarks>
		public string Next { get; set; }

		/// <summary>
		/// Optional named action to fire when this option is selected.
		/// </summary>
		/// <remarks>Resolved against registered action handlers by ConversationRunner.</remarks>
		public string Action { get; set; }
	}
}
